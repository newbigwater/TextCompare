using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Forms;
using TextCompare.Alignment;
using TextCompare.Builders;
using TextCompare.Cli;
using TextCompare.Controls;
using TextCompare.Core;
using TextCompare.Document;
using TextCompare.Git;
using TextCompare.IO;
using TextCompare.Properties;
using TextCompare.Structure;

namespace TextCompare
{
    public partial class MainForm : Form
    {
        private readonly MainFormBuilder _builder;
        private DiffDocument _document;
        private int _currentTopRow;
        private Encoding _leftEncoding;
        private Encoding _rightEncoding;
        private bool _isStructureMode;
        private bool _dirty;
        private ExcludeFilterSet _excludeFilters;
        private readonly CommandLineOptions _cliOptions;
        private readonly GitTempFileManager _gitTempFiles = new GitTempFileManager();
        private bool _leftReadOnly;
        private bool _rightReadOnly;

        public MainForm() : this(new CommandLineOptions())
        {
        }

        public MainForm(string[] args) : this(CommandLineOptions.Parse(args))
        {
        }

        public MainForm(CommandLineOptions options)
        {
            InitializeComponent();

            _cliOptions = options ?? new CommandLineOptions();
            _leftReadOnly = _cliOptions.LeftReadOnly;
            _rightReadOnly = _cliOptions.RightReadOnly;

            _excludeFilters = ExcludeFilterSettingsAdapter.Load();

            _builder = new MainFormBuilder(this);
            _builder
                .AddFilePicker()
                .AddNavigationBar()
                .AddStatusBar()
                .AddComparisonArea();
            _builder.FilePicker.ExcludeFilterEnabled = _excludeFilters.Enabled;

            WireEvents();

            if (!string.IsNullOrEmpty(_cliOptions.LeftPath) && !string.IsNullOrEmpty(_cliOptions.RightPath))
            {
                _builder.FilePicker.SetSource(true, _cliOptions.LeftPath, _cliOptions.LeftLabel);
                _builder.FilePicker.SetSource(false, _cliOptions.RightPath, _cliOptions.RightLabel);
                Shown += (s, e) => OnCompareRequested(this, EventArgs.Empty);
            }

            DragDropHelper.WireFileDrop(this, files => _builder.FilePicker.AcceptDroppedFiles(files));
            FormClosed += (s, e) => _gitTempFiles.Dispose();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S))
            {
                SaveChanges();
                return true;
            }
            if (keyData == (Keys.Alt | Keys.Up))
            {
                NavigateRelative(-1);
                return true;
            }
            if (keyData == (Keys.Alt | Keys.Down))
            {
                NavigateRelative(1);
                return true;
            }
            if (keyData == Keys.Escape && _cliOptions.CloseWithEsc)
            {
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void WireEvents()
        {
            _builder.FilePicker.CompareRequested += OnCompareRequested;
            _builder.FilePicker.ExcludeFilterEditRequested += OnExcludeFilterEditRequested;
            _builder.FilePicker.GitCompareWithHeadRequested += OnGitCompareWithHeadRequested;
            _builder.FilePicker.GitRegisterRequested += OnGitRegisterRequested;
            _builder.FilePicker.GitUnregisterRequested += OnGitUnregisterRequested;
            _builder.NavigationBar.PreviousClicked += (s, e) => NavigateRelative(-1);
            _builder.NavigationBar.NextClicked += (s, e) => NavigateRelative(1);
            _builder.LocationPane.LocationClicked += (s, row) =>
            {
                int blockIndex = FindBlockContainingRow(row);
                if (blockIndex >= 0) _builder.Viewer.NavigateToBlock(blockIndex);
                else _builder.Viewer.ScrollToRow(row);
            };

            _builder.Viewer.FilesDropped += (s, files) => _builder.FilePicker.AcceptDroppedFiles(files);
            _builder.Viewer.LeftFilesDropped += (s, files) => { if (files.Length > 0) _builder.FilePicker.LeftPath = files[0]; };
            _builder.Viewer.RightFilesDropped += (s, files) => { if (files.Length > 0) _builder.FilePicker.RightPath = files[0]; };
            _builder.LocationPane.FilesDropped += (s, files) => _builder.FilePicker.AcceptDroppedFiles(files);

            _builder.Viewer.CurrentRowChanged += (s, row) =>
            {
                _currentTopRow = row;
                _builder.LocationPane.UpdateViewport(row, _builder.Viewer.RowsPerPage);
            };
            _builder.Viewer.CurrentBlockChanged += (s, blockIndex) =>
            {
                int total = _document == null ? 0 : _document.TotalDiffCount;
                _builder.NavigationBar.SetDiffCount(blockIndex, total);
                _builder.PreviewBar.ShowBlock(_document, blockIndex);
            };
            _builder.Viewer.Resize += (s, e) =>
                _builder.LocationPane.UpdateViewport(_currentTopRow, _builder.Viewer.RowsPerPage);

            _builder.NavigationBar.EditModeToggled += (s, e) => ToggleEditMode();
            _builder.NavigationBar.SaveClicked += (s, e) => SaveChanges();
            _builder.Viewer.ContentEdited += (s, e) =>
            {
                _document = _builder.Viewer.Document;
                _dirty = true;
                _builder.NavigationBar.SetDirty(true);
                _builder.NavigationBar.SetDiffCount(-1, _document.TotalDiffCount);
                _builder.StatusLabel.Text = string.Format("차이 {0}개 | 편집 중(실시간 재비교)", _document.TotalDiffCount);
            };
        }

        private void OnExcludeFilterEditRequested(object sender, EventArgs e)
        {
            using (var dialog = new ExcludeFilterForm(_excludeFilters.Patterns))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                _excludeFilters.Patterns.Clear();
                foreach (ExcludeFilterPattern pattern in dialog.ResultPatterns)
                {
                    _excludeFilters.Patterns.Add(pattern);
                }
                ExcludeFilterSettingsAdapter.Save(_excludeFilters);
            }
        }

        /// <summary>
        /// 현재 선택된 파일(작업본)을 git HEAD 버전과 비교한다.
        /// HEAD 버전은 임시 파일로 추출해 왼쪽(읽기 전용)에, 작업본은 오른쪽에 배치한다.
        /// </summary>
        private void OnGitCompareWithHeadRequested(object sender, EventArgs e)
        {
            string target = _builder.FilePicker.RightPath;
            if (string.IsNullOrWhiteSpace(target) || !File.Exists(target)) target = _builder.FilePicker.LeftPath;

            if (string.IsNullOrWhiteSpace(target))
            {
                MessageBox.Show(this, "먼저 비교할 파일을 선택하세요.", "TextCompare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!File.Exists(target))
            {
                MessageBox.Show(this, "선택한 파일을 찾을 수 없습니다.", "TextCompare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string repoRoot = GitService.FindRepositoryRoot(target);
            if (repoRoot == null)
            {
                MessageBox.Show(this, "이 파일은 git 저장소 안에 있지 않습니다.", "TextCompare", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string relPath = GitPaths.GetRepoRelativePath(repoRoot, Path.GetFullPath(target));
            if (relPath == null)
            {
                MessageBox.Show(this, "저장소 기준 상대 경로를 계산할 수 없습니다.", "TextCompare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string fileName = Path.GetFileName(target);
            string tempPath;
            GitResult result;
            try
            {
                tempPath = _gitTempFiles.CreateTempPathFor(fileName);
                result = GitService.ShowHead(repoRoot, relPath, tempPath);
            }
            catch (IOException ex)
            {
                MessageBox.Show(this, "임시 파일을 만들 수 없습니다: " + ex.Message, "TextCompare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!result.Success)
            {
                _gitTempFiles.CleanupCurrent();
                MessageBox.Show(this, DescribeShowHeadError(result), "TextCompare", MessageBoxButtons.OK,
                    result.GitNotFound || result.TimedOut ? MessageBoxIcon.Error : MessageBoxIcon.Information);
                return;
            }

            _builder.FilePicker.SetSource(true, tempPath, "HEAD: " + fileName);
            _builder.FilePicker.SetSource(false, Path.GetFullPath(target), null);
            _leftReadOnly = true;
            _rightReadOnly = false;

            OnCompareRequested(this, EventArgs.Empty);
        }

        private static string DescribeShowHeadError(GitResult result)
        {
            if (result.GitNotFound) return "git이 설치되어 있지 않거나 PATH에서 찾을 수 없습니다.";
            if (result.TimedOut) return "git 응답이 없어 중단했습니다.";

            string stdErr = result.StdErr ?? string.Empty;
            if (result.ExitCode == 128)
            {
                if (stdErr.Contains("exists on disk, but not in") || stdErr.Contains("does not exist"))
                    return "이 파일은 아직 커밋된 적이 없어 HEAD 버전이 없습니다.";
                if (stdErr.Contains("bad revision") || stdErr.Contains("unknown revision"))
                    return "이 저장소에는 아직 커밋이 없습니다.";
            }

            string message = "git 실행 중 오류가 발생했습니다.";
            if (stdErr.Length > 0)
            {
                message += "\r\n" + (stdErr.Length > 300 ? stdErr.Substring(0, 300) + "..." : stdErr);
            }
            return message;
        }

        private void OnGitRegisterRequested(object sender, EventArgs e)
        {
            GitResult result = GitService.RegisterAsDifftool(Application.ExecutablePath);
            if (result.Success)
            {
                MessageBox.Show(this, "git difftool로 등록되었습니다.\r\n이제 git difftool 명령으로 이 프로그램이 열립니다.",
                    "TextCompare", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            MessageBox.Show(this, DescribeShowHeadError(result), "TextCompare", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void OnGitUnregisterRequested(object sender, EventArgs e)
        {
            GitResult result = GitService.UnregisterDifftool();
            if (result.Success)
            {
                MessageBox.Show(this, "git difftool 등록이 해제되었습니다.", "TextCompare", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            MessageBox.Show(this, DescribeShowHeadError(result), "TextCompare", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ToggleEditMode()
        {
            if (_isStructureMode)
            {
                MessageBox.Show(this, "XML/JSON 구조 비교 모드에서는 편집을 지원하지 않습니다.", "TextCompare", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            // "라인 전체 제외" 패턴은 줄을 물리적으로 제거해 편집·저장 라운드트립이 불가능하므로 편집을 막는다.
            // "매치 부분만 제외"(MaskMatch)만 있으면 줄이 모두 보존되므로 편집해도 안전하다.
            if (_excludeFilters.HasLineExclusions)
            {
                MessageBox.Show(this, "'라인 전체 제외' 패턴이 켜진 상태에서는 편집을 지원하지 않습니다. 필터를 끄거나 '매치 부분만 제외' 모드로 바꾸고 다시 비교한 뒤 편집하세요.", "TextCompare", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_document == null)
            {
                MessageBox.Show(this, "먼저 비교를 실행하세요.", "TextCompare", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool newState = !_builder.Viewer.EditMode;
            _builder.Viewer.SetPaneReadOnly(_leftReadOnly, _rightReadOnly);
            _builder.Viewer.SetEditMode(newState);
            _builder.NavigationBar.SetEditModeState(newState, true);

            if (!newState)
            {
                _document = _builder.Viewer.Document;
                _builder.LocationPane.Document = _document;
                _builder.LocationPane.UpdateViewport(0, _builder.Viewer.RowsPerPage);
                _builder.NavigationBar.SetDiffCount(-1, _document.TotalDiffCount);
                _builder.PreviewBar.Clear();
                _builder.StatusLabel.Text = string.Format("차이 {0}개 | 비교 보기", _document.TotalDiffCount);
            }
        }

        private int FindBlockContainingRow(int row)
        {
            if (_document == null) return -1;
            for (int i = 0; i < _document.Blocks.Count; i++)
            {
                DiffBlock block = _document.Blocks[i];
                if (row >= block.StartRow && row < block.EndRow) return i;
            }
            return -1;
        }

        private void SaveChanges()
        {
            if (!_builder.Viewer.EditMode)
            {
                return;
            }
            if (!_dirty)
            {
                MessageBox.Show(this, "저장할 변경 사항이 없습니다.", "TextCompare", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_leftReadOnly && _rightReadOnly)
            {
                MessageBox.Show(this, "양쪽 모두 읽기 전용이라 저장할 수 없습니다.", "TextCompare", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                Encoding leftEnc = _leftEncoding ?? new UTF8Encoding(false);
                Encoding rightEnc = _rightEncoding ?? new UTF8Encoding(false);

                if (!_leftReadOnly) File.WriteAllText(_builder.FilePicker.LeftPath, _builder.Viewer.LeftEditorText, leftEnc);
                if (!_rightReadOnly) File.WriteAllText(_builder.FilePicker.RightPath, _builder.Viewer.RightEditorText, rightEnc);

                _dirty = false;
                _builder.NavigationBar.SetDirty(false);
                string statusText = "저장 완료 (" + DateTime.Now.ToString("HH:mm:ss") + ")";
                if (_leftReadOnly) statusText += " | 왼쪽은 읽기 전용이라 저장하지 않았습니다.";
                if (_rightReadOnly) statusText += " | 오른쪽은 읽기 전용이라 저장하지 않았습니다.";
                _builder.StatusLabel.Text = statusText;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "저장 중 오류가 발생했습니다: " + ex.Message, "TextCompare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>백그라운드 스레드에서 계산한 비교 결과를 UI 스레드로 넘기기 위한 운반체.</summary>
        private sealed class CompareResult
        {
            public DiffDocument Document;
            public DiffOptions Options;
            public bool IsStructureMode;
            public string ModeLabel;
            public Encoding LeftEncoding;
            public Encoding RightEncoding;
        }

        private void OnCompareRequested(object sender, EventArgs e)
        {
            string leftPath = _builder.FilePicker.LeftPath;
            string rightPath = _builder.FilePicker.RightPath;

            if (string.IsNullOrWhiteSpace(leftPath) || string.IsNullOrWhiteSpace(rightPath))
            {
                MessageBox.Show(this, "Left, Right 파일을 모두 선택하세요.", "TextCompare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!File.Exists(leftPath) || !File.Exists(rightPath))
            {
                MessageBox.Show(this, "선택한 파일을 찾을 수 없습니다.", "TextCompare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _excludeFilters.Enabled = _builder.FilePicker.ExcludeFilterEnabled;
            ExcludeFilterSettingsAdapter.Save(_excludeFilters);

            // 라벨 표시 모드가 해제된(사용자가 직접 파일을 바꾼) 쪽은 읽기 전용도 CLI 기본값으로 되돌린다.
            if (_builder.FilePicker.LeftLabel == null) _leftReadOnly = _cliOptions.LeftReadOnly;
            if (_builder.FilePicker.RightLabel == null) _rightReadOnly = _cliOptions.RightReadOnly;

            DiffOptions options = new DiffOptions
            {
                IgnoreCase = _builder.FilePicker.IgnoreCase,
                IgnoreWhitespace = _builder.FilePicker.IgnoreWhitespace,
                ExcludeFilters = _excludeFilters
            };

            RunCompareWithProgress(leftPath, rightPath, options);
        }

        /// <summary>
        /// 비교 연산을 백그라운드 스레드(BackgroundWorker)에서 실행하며, 실제로 완료된 단계마다
        /// 진행 창(CompareProgressForm)의 진행 사항/진행율을 갱신한다. 모달로 띄우므로 연산 중
        /// 다른 조작(재클릭 등)은 자연스럽게 막힌다.
        /// </summary>
        private void RunCompareWithProgress(string leftPath, string rightPath, DiffOptions options)
        {
            using (var progressForm = new CompareProgressForm())
            using (var worker = new BackgroundWorker { WorkerReportsProgress = true })
            {
                worker.DoWork += (s, e) =>
                {
                    e.Result = ComputeCompareResult(leftPath, rightPath, options, worker);
                };

                worker.ProgressChanged += (s, e) =>
                {
                    progressForm.UpdateProgress(e.ProgressPercentage, (string)e.UserState);
                };

                worker.RunWorkerCompleted += (s, e) =>
                {
                    progressForm.Close();

                    if (e.Error != null)
                    {
                        Environment.ExitCode = 2;
                        MessageBox.Show(this, "비교 중 오류가 발생했습니다: " + e.Error.Message, "TextCompare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    ApplyCompareResult((CompareResult)e.Result);
                };

                worker.RunWorkerAsync();
                progressForm.ShowDialog(this);
            }
        }

        /// <summary>
        /// 백그라운드 스레드에서 실행된다 — WinForms 컨트롤을 직접 건드리지 않고 worker.ReportProgress로만
        /// 진행 상황을 알린다(BackgroundWorker가 UI 스레드로 자동 마샬링).
        /// </summary>
        private static CompareResult ComputeCompareResult(string leftPath, string rightPath, DiffOptions options, BackgroundWorker worker)
        {
            var result = new CompareResult { Options = options };

            string leftExt = Path.GetExtension(leftPath).ToLowerInvariant();
            string rightExt = Path.GetExtension(rightPath).ToLowerInvariant();

            if (leftExt == ".xml" && rightExt == ".xml")
            {
                result.IsStructureMode = true;
                worker.ReportProgress(5, "Left XML 파싱 중...");
                StructuredNode leftNode = XmlStructureParser.ParseFile(leftPath);
                worker.ReportProgress(35, "Right XML 파싱 중...");
                StructuredNode rightNode = XmlStructureParser.ParseFile(rightPath);
                worker.ReportProgress(55, "구조 비교 연산 중...");
                List<AlignedRow> rows = StructureDiffer.Diff(leftNode, rightNode, new XmlPrettyPrinter(), options);
                worker.ReportProgress(90, "결과 구성 중...");
                result.Document = DiffDocument.FromRows(rows);
                result.ModeLabel = "XML 구조(객체) 비교";
            }
            else if (leftExt == ".json" && rightExt == ".json")
            {
                result.IsStructureMode = true;
                worker.ReportProgress(5, "Left JSON 읽는 중...");
                Encoding leftJsonEncoding, rightJsonEncoding;
                string leftJsonText = EncodingDetector.ReadAllText(leftPath, out leftJsonEncoding);
                worker.ReportProgress(20, "Right JSON 읽는 중...");
                string rightJsonText = EncodingDetector.ReadAllText(rightPath, out rightJsonEncoding);
                worker.ReportProgress(35, "JSON 파싱 중...");
                StructuredNode leftNode = JsonStructureParser.Parse(leftJsonText);
                StructuredNode rightNode = JsonStructureParser.Parse(rightJsonText);
                worker.ReportProgress(55, "구조 비교 연산 중...");
                List<AlignedRow> rows = StructureDiffer.Diff(leftNode, rightNode, new JsonPrettyPrinter(), options);
                worker.ReportProgress(90, "결과 구성 중...");
                result.Document = DiffDocument.FromRows(rows);
                result.ModeLabel = "JSON 구조(객체) 비교";
            }
            else
            {
                result.IsStructureMode = false;
                worker.ReportProgress(5, "Left 파일 읽는 중...");
                string leftText = EncodingDetector.ReadAllText(leftPath, out result.LeftEncoding);
                worker.ReportProgress(20, "Right 파일 읽는 중...");
                string rightText = EncodingDetector.ReadAllText(rightPath, out result.RightEncoding);
                worker.ReportProgress(35, "줄 단위 분리 중...");
                List<string> leftLines = LineSplitter.Split(leftText);
                List<string> rightLines = LineSplitter.Split(rightText);

                result.Document = DiffDocument.Build(leftLines, rightLines, options, stage =>
                {
                    switch (stage)
                    {
                        case "filter": worker.ReportProgress(45, "제외 필터 적용 중..."); break;
                        case "diff": worker.ReportProgress(55, "라인 비교 연산 중..."); break;
                        case "align": worker.ReportProgress(85, "정렬 처리 중..."); break;
                        case "blocks": worker.ReportProgress(95, "결과 구성 중..."); break;
                    }
                });
                result.ModeLabel = string.Format("텍스트 라인 비교 (Left 인코딩: {0}, Right 인코딩: {1})", result.LeftEncoding.EncodingName, result.RightEncoding.EncodingName);
            }

            return result;
        }

        /// <summary>UI 스레드에서 실행된다(BackgroundWorker.RunWorkerCompleted). 결과를 화면에 반영한다.</summary>
        private void ApplyCompareResult(CompareResult result)
        {
            _isStructureMode = result.IsStructureMode;
            _leftEncoding = result.LeftEncoding;
            _rightEncoding = result.RightEncoding;
            _document = result.Document;

            _builder.Viewer.SetDocument(_document, result.Options);
            _builder.LocationPane.Document = _document;
            _builder.LocationPane.UpdateViewport(0, _builder.Viewer.RowsPerPage);
            _builder.NavigationBar.SetDiffCount(-1, _document.TotalDiffCount);
            // 버튼은 항상 클릭 가능하게 두고, 구조 비교 모드에서는 ToggleEditMode() 안의 안내 메시지가 뜨도록 한다.
            // (비활성화하면 WinForms 특성상 클릭 이벤트 자체가 발생하지 않아 안내 메시지도 못 보고 그냥 죽은 버튼처럼 보임)
            _builder.NavigationBar.SetEditModeState(false, true);
            _builder.NavigationBar.SetDirty(false);
            _builder.PreviewBar.Clear();
            _dirty = false;

            // git difftool 호환 종료 코드: 0=차이 없음, 1=차이 있음 (difftool.trustExitCode용)
            Environment.ExitCode = _document.TotalDiffCount > 0 ? 1 : 0;

            string modeLabel = result.ModeLabel;
            string leftLabel = _builder.FilePicker.LeftLabel;
            string rightLabel = _builder.FilePicker.RightLabel;
            if (leftLabel != null || rightLabel != null)
            {
                modeLabel = string.Format("{0} ↔ {1} | {2}", leftLabel ?? "Left", rightLabel ?? "Right", modeLabel);
            }
            _builder.StatusLabel.Text = string.Format("차이 {0}개 | {1}", _document.TotalDiffCount, modeLabel);
        }

        private void NavigateRelative(int direction)
        {
            if (_document == null || _document.TotalDiffCount == 0) return;

            int current = _builder.Viewer.CurrentBlockIndex;
            int next;
            if (current < 0)
            {
                next = direction > 0 ? 0 : _document.TotalDiffCount - 1;
            }
            else
            {
                next = current + direction;
                if (next < 0) next = _document.TotalDiffCount - 1;
                if (next >= _document.TotalDiffCount) next = 0;
            }

            _builder.Viewer.NavigateToBlock(next);
        }

    }
}
