using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using TextCompare.Alignment;
using TextCompare.Builders;
using TextCompare.Controls;
using TextCompare.Core;
using TextCompare.Document;
using TextCompare.IO;
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

        public MainForm() : this(new string[0])
        {
        }

        public MainForm(string[] args)
        {
            InitializeComponent();

            _builder = new MainFormBuilder(this);
            _builder
                .AddFilePicker()
                .AddNavigationBar()
                .AddStatusBar()
                .AddComparisonArea();

            WireEvents();

            if (args != null && args.Length >= 2)
            {
                _builder.FilePicker.LeftPath = args[0];
                _builder.FilePicker.RightPath = args[1];
                Shown += (s, e) => OnCompareRequested(this, EventArgs.Empty);
            }

            DragDropHelper.WireFileDrop(this, files => _builder.FilePicker.AcceptDroppedFiles(files));
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
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void WireEvents()
        {
            _builder.FilePicker.CompareRequested += OnCompareRequested;
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

        private void ToggleEditMode()
        {
            if (_isStructureMode)
            {
                MessageBox.Show(this, "XML/JSON 구조 비교 모드에서는 편집을 지원하지 않습니다.", "TextCompare", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_document == null)
            {
                MessageBox.Show(this, "먼저 비교를 실행하세요.", "TextCompare", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool newState = !_builder.Viewer.EditMode;
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

            try
            {
                Encoding leftEnc = _leftEncoding ?? new UTF8Encoding(false);
                Encoding rightEnc = _rightEncoding ?? new UTF8Encoding(false);

                File.WriteAllText(_builder.FilePicker.LeftPath, _builder.Viewer.LeftEditorText, leftEnc);
                File.WriteAllText(_builder.FilePicker.RightPath, _builder.Viewer.RightEditorText, rightEnc);

                _dirty = false;
                _builder.NavigationBar.SetDirty(false);
                _builder.StatusLabel.Text = "저장 완료 (" + DateTime.Now.ToString("HH:mm:ss") + ")";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "저장 중 오류가 발생했습니다: " + ex.Message, "TextCompare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

            try
            {
                DiffOptions options = new DiffOptions
                {
                    IgnoreCase = _builder.FilePicker.IgnoreCase,
                    IgnoreWhitespace = _builder.FilePicker.IgnoreWhitespace
                };

                string leftExt = Path.GetExtension(leftPath).ToLowerInvariant();
                string rightExt = Path.GetExtension(rightPath).ToLowerInvariant();
                string modeLabel;

                _leftEncoding = null;
                _rightEncoding = null;

                if (leftExt == ".xml" && rightExt == ".xml")
                {
                    _isStructureMode = true;
                    StructuredNode leftNode = XmlStructureParser.ParseFile(leftPath);
                    StructuredNode rightNode = XmlStructureParser.ParseFile(rightPath);
                    List<AlignedRow> rows = StructureDiffer.Diff(leftNode, rightNode, new XmlPrettyPrinter(), options);
                    _document = DiffDocument.FromRows(rows);
                    modeLabel = "XML 구조(객체) 비교";
                }
                else if (leftExt == ".json" && rightExt == ".json")
                {
                    _isStructureMode = true;
                    Encoding leftJsonEncoding, rightJsonEncoding;
                    string leftJsonText = EncodingDetector.ReadAllText(leftPath, out leftJsonEncoding);
                    string rightJsonText = EncodingDetector.ReadAllText(rightPath, out rightJsonEncoding);
                    StructuredNode leftNode = JsonStructureParser.Parse(leftJsonText);
                    StructuredNode rightNode = JsonStructureParser.Parse(rightJsonText);
                    List<AlignedRow> rows = StructureDiffer.Diff(leftNode, rightNode, new JsonPrettyPrinter(), options);
                    _document = DiffDocument.FromRows(rows);
                    modeLabel = "JSON 구조(객체) 비교";
                }
                else
                {
                    _isStructureMode = false;
                    string leftText = EncodingDetector.ReadAllText(leftPath, out _leftEncoding);
                    string rightText = EncodingDetector.ReadAllText(rightPath, out _rightEncoding);

                    List<string> leftLines = LineSplitter.Split(leftText);
                    List<string> rightLines = LineSplitter.Split(rightText);

                    _document = DiffDocument.Build(leftLines, rightLines, options);
                    modeLabel = string.Format("텍스트 라인 비교 (Left 인코딩: {0}, Right 인코딩: {1})", _leftEncoding.EncodingName, _rightEncoding.EncodingName);
                }

                _builder.Viewer.SetDocument(_document, options);
                _builder.LocationPane.Document = _document;
                _builder.LocationPane.UpdateViewport(0, _builder.Viewer.RowsPerPage);
                _builder.NavigationBar.SetDiffCount(-1, _document.TotalDiffCount);
                // 버튼은 항상 클릭 가능하게 두고, 구조 비교 모드에서는 ToggleEditMode() 안의 안내 메시지가 뜨도록 한다.
                // (비활성화하면 WinForms 특성상 클릭 이벤트 자체가 발생하지 않아 안내 메시지도 못 보고 그냥 죽은 버튼처럼 보임)
                _builder.NavigationBar.SetEditModeState(false, true);
                _builder.NavigationBar.SetDirty(false);
                _builder.PreviewBar.Clear();
                _dirty = false;

                _builder.StatusLabel.Text = string.Format("차이 {0}개 | {1}", _document.TotalDiffCount, modeLabel);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "비교 중 오류가 발생했습니다: " + ex.Message, "TextCompare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
