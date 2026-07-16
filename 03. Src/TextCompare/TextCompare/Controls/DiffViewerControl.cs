using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TextCompare.Alignment;
using TextCompare.Core;
using TextCompare.Document;

namespace TextCompare.Controls
{
    /// <summary>
    /// 좌/우 DiffPaneControl을 합성하고, 하나의 수직/수평 스크롤바로 두 페인을 동기화한다.
    /// AlignedRow 정렬 덕분에 좌/우가 항상 1:1 대응이므로 스크롤바 하나로 양쪽을 동시에 제어할 수 있다.
    /// 편집 모드에서는 owner-drawn 페인 대신 실제 편집 가능한 RichTextBox(IME/캐럿/선택영역이 OS 기본 지원)로
    /// 전환하고, 입력이 멈추면(디바운스) 실시간으로 재비교해 배경색으로 diff를 다시 칠한다.
    /// </summary>
    public partial class DiffViewerControl : UserControl
    {
        private DiffDocument _document;
        private DiffOptions _lastOptions = DiffOptions.Default;
        private int _selectedBlockIndex = -1;
        private bool _editMode;
        private bool _suppressEditorEvents;

        public event EventHandler<int> CurrentRowChanged;
        public event EventHandler<int> CurrentBlockChanged;

        /// <summary>위치가 애매한 영역(컨트롤 배경/스크롤바)에 드롭됐을 때. 기존의 "빈 칸 우선" 배정 폴백용.</summary>
        public event EventHandler<string[]> FilesDropped;

        /// <summary>Left 페인에 정확히 드롭됐을 때 — 첫 번째 파일을 무조건 Left로.</summary>
        public event EventHandler<string[]> LeftFilesDropped;

        /// <summary>Right 페인에 정확히 드롭됐을 때 — 첫 번째 파일을 무조건 Right로.</summary>
        public event EventHandler<string[]> RightFilesDropped;

        /// <summary>편집 모드에서 실시간 재비교가 끝날 때마다(디바운스 이후) 발생. MainForm이 "수정됨" 상태 갱신에 사용.</summary>
        public event EventHandler ContentEdited;

        public DiffViewerControl()
        {
            InitializeComponent();

            _split.SplitterDistance = Math.Max(1, ClientSize.Width / 2);

            Action<string[]> onDrop = files => { if (FilesDropped != null) FilesDropped(this, files); };
            Action<string[]> onLeftDrop = files => { if (LeftFilesDropped != null) LeftFilesDropped(this, files); };
            Action<string[]> onRightDrop = files => { if (RightFilesDropped != null) RightFilesDropped(this, files); };

            DragDropHelper.WireFileDrop(this, onDrop);
            DragDropHelper.WireFileDrop(_leftPane, onLeftDrop);
            DragDropHelper.WireFileDrop(_rightPane, onRightDrop);
            DragDropHelper.WireFileDrop(_leftEditor, onLeftDrop);
            DragDropHelper.WireFileDrop(_rightEditor, onRightDrop);
        }

        private void DiffViewerControl_Resize(object sender, EventArgs e)
        {
            RecalculateScrollBars();
        }

        private void DebounceTimer_Tick(object sender, EventArgs e)
        {
            _debounceTimer.Stop();
            RediffFromEditors();
        }

        public DiffDocument Document
        {
            get { return _document; }
        }

        public int CurrentBlockIndex
        {
            get { return _selectedBlockIndex; }
        }

        public int RowsPerPage
        {
            get { return _leftPane.RowsPerPage; }
        }

        public bool EditMode
        {
            get { return _editMode; }
        }

        public string LeftEditorText
        {
            get { return _leftEditor.Text; }
        }

        public string RightEditorText
        {
            get { return _rightEditor.Text; }
        }

        public void SetDocument(DiffDocument document, DiffOptions options)
        {
            if (_editMode)
            {
                ExitEditModeInternal();
            }

            _lastOptions = options ?? DiffOptions.Default;
            _document = document;
            _leftPane.Document = document;
            _rightPane.Document = document;
            _selectedBlockIndex = -1;
            _leftPane.SelectedBlockIndex = -1;
            _rightPane.SelectedBlockIndex = -1;

            _vScroll.Value = 0;
            _hScroll.Value = 0;
            RecalculateScrollBars();
        }

        /// <summary>편집 모드로 전환하거나(true) 비교 보기로 되돌아간다(false). 구조(XML/JSON) 비교 모드 허용 여부는
        /// 호출자(MainForm)가 판단해서 아예 호출하지 않는 방식으로 막는다.</summary>
        public void SetEditMode(bool enabled)
        {
            if (enabled == _editMode) return;

            if (enabled)
            {
                PopulateEditorsFromDocument();
                _leftPane.Visible = false;
                _rightPane.Visible = false;
                _vScroll.Visible = false;
                _hScroll.Visible = false;
                _leftEditor.Visible = true;
                _rightEditor.Visible = true;
                _editMode = true;
                _leftEditor.Focus();
            }
            else
            {
                ExitEditModeInternal();
            }
        }

        private void ExitEditModeInternal()
        {
            _debounceTimer.Stop();
            RediffFromEditors(); // 마지막 디바운스를 기다리지 않고 즉시 반영해 비교 보기로 넘어간다.

            _leftEditor.Visible = false;
            _rightEditor.Visible = false;
            _vScroll.Visible = true;
            _hScroll.Visible = true;
            _leftPane.Visible = true;
            _rightPane.Visible = true;
            _editMode = false;

            _leftPane.Document = _document;
            _rightPane.Document = _document;
            RecalculateScrollBars();
        }

        private void PopulateEditorsFromDocument()
        {
            _suppressEditorEvents = true;
            _leftEditor.Text = string.Join("\r\n", GetRawLines(true));
            _rightEditor.Text = string.Join("\r\n", GetRawLines(false));
            _suppressEditorEvents = false;

            ApplyHighlighting(_leftEditor, true);
            ApplyHighlighting(_rightEditor, false);
        }

        private List<string> GetRawLines(bool isLeft)
        {
            var lines = new List<string>();
            foreach (AlignedRow row in GetRealRows(isLeft))
            {
                lines.Add(isLeft ? row.LeftText : row.RightText);
            }
            return lines;
        }

        /// <summary>ghost가 아닌(실제로 그 side에 존재하는) 행만 순서대로 반환한다. 편집기 박스의 각 줄과 1:1로 대응.</summary>
        private List<AlignedRow> GetRealRows(bool isLeft)
        {
            var rows = new List<AlignedRow>();
            if (_document == null) return rows;

            foreach (AlignedRow row in _document.Rows)
            {
                bool isGhost = isLeft ? row.IsLeftGhost : row.IsRightGhost;
                if (isGhost) continue;
                rows.Add(row);
            }
            return rows;
        }

        private void OnEditorTextChanged(object sender, EventArgs e)
        {
            if (_suppressEditorEvents) return;
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private void RediffFromEditors()
        {
            List<string> leftLines = LineSplitter.Split(_leftEditor.Text);
            List<string> rightLines = LineSplitter.Split(_rightEditor.Text);

            _document = DiffDocument.Build(leftLines, rightLines, _lastOptions);

            ApplyHighlighting(_leftEditor, true);
            ApplyHighlighting(_rightEditor, false);

            if (ContentEdited != null) ContentEdited(this, EventArgs.Empty);
        }

        private void ApplyHighlighting(RichTextBox box, bool isLeft)
        {
            if (_document == null) return;
            DiffHighlighter.ApplyRowColors(box, GetRealRows(isLeft), isLeft);
        }

        public void ScrollToRow(int row)
        {
            SetVScrollValueClamped(row);
        }

        public void NavigateToBlock(int blockIndex)
        {
            if (_document == null || blockIndex < 0 || blockIndex >= _document.Blocks.Count) return;

            DiffBlock block = _document.Blocks[blockIndex];
            int targetRow = Math.Max(0, block.StartRow - 1);
            SetVScrollValueClamped(targetRow);

            _selectedBlockIndex = blockIndex;
            _leftPane.SelectedBlockIndex = blockIndex;
            _rightPane.SelectedBlockIndex = blockIndex;

            if (CurrentBlockChanged != null) CurrentBlockChanged(this, blockIndex);
        }

        private void OnPaneBlockClicked(object sender, int blockIndex)
        {
            _selectedBlockIndex = blockIndex;
            _leftPane.SelectedBlockIndex = blockIndex;
            _rightPane.SelectedBlockIndex = blockIndex;
            if (CurrentBlockChanged != null) CurrentBlockChanged(this, blockIndex);
        }

        private void OnPaneScrollDeltaRequested(object sender, int delta)
        {
            SetVScrollValueClamped(_vScroll.Value + delta);
        }

        private void SetVScrollValueClamped(int value)
        {
            int effectiveMax = Math.Max(_vScroll.Minimum, _vScroll.Maximum - _vScroll.LargeChange + 1);
            int v = Math.Max(_vScroll.Minimum, Math.Min(value, effectiveMax));
            _vScroll.Value = v;
        }

        private void OnVScrollValueChanged(object sender, EventArgs e)
        {
            _leftPane.FirstVisibleRow = _vScroll.Value;
            _rightPane.FirstVisibleRow = _vScroll.Value;
            if (CurrentRowChanged != null) CurrentRowChanged(this, _vScroll.Value);
        }

        private void OnHScrollValueChanged(object sender, EventArgs e)
        {
            _leftPane.HorizontalOffset = _hScroll.Value;
            _rightPane.HorizontalOffset = _hScroll.Value;
        }

        private void RecalculateScrollBars()
        {
            int rowCount = _document == null ? 0 : _document.Rows.Count;
            int rowsPerPage = Math.Max(1, _leftPane.RowsPerPage);

            // ScrollBar.LargeChange는 설정 시점의 현재 Maximum 값으로 즉시 클램핑되는 WinForms 특성이 있다.
            // 이전 호출에서 남은 작은 Maximum이 새 LargeChange를 잘라내지 않도록, Maximum을 넉넉히 키운 뒤
            // LargeChange를 설정하고, 그 다음에야 정확한 최종 Maximum을 계산한다.
            _vScroll.Minimum = 0;
            _vScroll.SmallChange = 1;
            _vScroll.Maximum = int.MaxValue / 2;
            _vScroll.LargeChange = Math.Max(1, rowsPerPage);
            _vScroll.Maximum = Math.Max(0, rowCount - 1 + (_vScroll.LargeChange - 1));
            _vScroll.Enabled = rowCount > rowsPerPage;

            int maxLineWidth = _document == null ? 0 : Math.Max(_leftPane.MeasureMaxLineWidth(), _rightPane.MeasureMaxLineWidth());
            int contentWidth = Math.Max(1, _leftPane.ClientSize.Width - _leftPane.GutterWidth);

            _hScroll.Minimum = 0;
            _hScroll.SmallChange = 20;
            _hScroll.Maximum = int.MaxValue / 2;
            _hScroll.LargeChange = Math.Max(1, contentWidth);
            _hScroll.Maximum = Math.Max(0, maxLineWidth - contentWidth) + _hScroll.LargeChange - 1;
            _hScroll.Enabled = maxLineWidth > contentWidth;
        }
    }
}
