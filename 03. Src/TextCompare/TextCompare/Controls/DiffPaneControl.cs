using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using TextCompare.Alignment;
using TextCompare.Core;
using TextCompare.Document;
using TextCompare.Intraline;

namespace TextCompare.Controls
{
    public enum PaneSide { Left, Right }

    /// <summary>
    /// 단일 페인(좌측 또는 우측)을 owner-drawn으로 렌더링하는 가상 스크롤 컨트롤.
    /// 전체 라인 수에 관계없이 화면에 보이는 행만 그리므로 대용량 파일에서도 성능이 유지된다.
    /// 실제 스크롤 위치(FirstVisibleRow/HorizontalOffset)는 DiffViewerControl이 좌우 페인에 동일하게 주입한다.
    /// </summary>
    public partial class DiffPaneControl : UserControl
    {
        public static readonly Font MonoFont = new Font("Consolas", 9.75f, FontStyle.Regular);
        public static readonly int RowHeight = MonoFont.Height + 4;

        private static readonly Color ChangedColor = Color.FromArgb(253, 245, 184);
        private static readonly Color OnlyColor = Color.FromArgb(253, 200, 160);
        private static readonly Color GhostColor = Color.FromArgb(224, 224, 224);
        private static readonly Color IntralineColor = Color.FromArgb(245, 212, 0);
        /// <summary>제외 필터(MaskMatch)가 비교에서 무시한 구간의 배경색. GhostColor(224)보다 살짝 짙어 구분된다.</summary>
        private static readonly Color MaskedColor = Color.FromArgb(211, 211, 211);
        private static readonly Color SelectedBorderColor = Color.FromArgb(255, 140, 0);
        private static readonly Color GutterColor = Color.FromArgb(90, 90, 90);
        private static readonly Color GutterSeparatorColor = Color.FromArgb(200, 200, 200);

        private const int GutterPadding = 6;

        private DiffDocument _document;
        private PaneSide _side = PaneSide.Left;
        private int _firstVisibleRow;
        private int _horizontalOffset;
        private int _selectedBlockIndex = -1;
        private int _gutterWidth = 40;

        public event EventHandler<int> ScrollDeltaRequested;
        public event EventHandler<int> BlockClicked;

        public DiffPaneControl()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                      ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            TabStop = true;
        }

        public PaneSide Side
        {
            get { return _side; }
            set { _side = value; Invalidate(); }
        }

        public DiffDocument Document
        {
            get { return _document; }
            set
            {
                _document = value;
                _firstVisibleRow = 0;
                _selectedBlockIndex = -1;
                RecalculateGutterWidth();
                Invalidate();
            }
        }

        public int RowsPerPage
        {
            get { return Math.Max(1, ClientSize.Height / RowHeight); }
        }

        public int FirstVisibleRow
        {
            get { return _firstVisibleRow; }
            set
            {
                int rowCount = _document == null ? 0 : _document.Rows.Count;
                int max = Math.Max(0, rowCount - 1);
                int v = Math.Max(0, Math.Min(value, max));
                if (v != _firstVisibleRow)
                {
                    _firstVisibleRow = v;
                    Invalidate();
                }
            }
        }

        public int HorizontalOffset
        {
            get { return _horizontalOffset; }
            set
            {
                int v = Math.Max(0, value);
                if (v != _horizontalOffset)
                {
                    _horizontalOffset = v;
                    Invalidate();
                }
            }
        }

        public int SelectedBlockIndex
        {
            get { return _selectedBlockIndex; }
            set
            {
                if (_selectedBlockIndex != value)
                {
                    _selectedBlockIndex = value;
                    Invalidate();
                }
            }
        }

        public int GutterWidth
        {
            get { return _gutterWidth; }
        }

        public int MeasureMaxLineWidth()
        {
            if (_document == null) return 0;
            int max = 0;
            using (Graphics g = CreateGraphics())
            {
                foreach (AlignedRow row in _document.Rows)
                {
                    string text = _side == PaneSide.Left ? row.LeftText : row.RightText;
                    if (string.IsNullOrEmpty(text)) continue;
                    int w = TextRenderer.MeasureText(g, text, MonoFont, new Size(int.MaxValue, RowHeight),
                        TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
                    if (w > max) max = w;
                }
            }
            return max;
        }

        private void RecalculateGutterWidth()
        {
            int maxLineNo = 1;
            if (_document != null)
            {
                foreach (AlignedRow row in _document.Rows)
                {
                    int? no = _side == PaneSide.Left ? row.LeftLineNo : row.RightLineNo;
                    if (no.HasValue && no.Value > maxLineNo) maxLineNo = no.Value;
                }
            }
            string sample = new string('9', Math.Max(3, maxLineNo.ToString().Length));
            int textWidth = TextRenderer.MeasureText(sample, MonoFont, new Size(int.MaxValue, RowHeight),
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
            _gutterWidth = textWidth + GutterPadding * 2;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);

            if (_document == null)
            {
                DrawPlaceholder(g);
                return;
            }

            IList<AlignedRow> rows = _document.Rows;
            int rowsPerPage = RowsPerPage;

            for (int i = 0; i <= rowsPerPage; i++)
            {
                int rowIndex = _firstVisibleRow + i;
                if (rowIndex >= rows.Count) break;
                int y = i * RowHeight;
                DrawRow(g, rows[rowIndex], y);
            }

            using (Pen pen = new Pen(GutterSeparatorColor))
            {
                g.DrawLine(pen, _gutterWidth, 0, _gutterWidth, ClientSize.Height);
            }
        }

        private void DrawPlaceholder(Graphics g)
        {
            const string message = "파일을 선택하고 비교를 실행하세요.";
            Size size = TextRenderer.MeasureText(message, MonoFont);
            int x = Math.Max(0, (ClientSize.Width - size.Width) / 2);
            int y = Math.Max(0, (ClientSize.Height - size.Height) / 2);
            TextRenderer.DrawText(g, message, MonoFont, new Point(x, y), Color.Gray);
        }

        private void DrawRow(Graphics g, AlignedRow row, int y)
        {
            bool isGhost = _side == PaneSide.Left ? row.IsLeftGhost : row.IsRightGhost;
            string text = _side == PaneSide.Left ? row.LeftText : row.RightText;
            int? lineNo = _side == PaneSide.Left ? row.LeftLineNo : row.RightLineNo;

            Color bg = GetBackColor(row, isGhost);
            using (SolidBrush brush = new SolidBrush(bg))
            {
                g.FillRectangle(brush, 0, y, ClientSize.Width, RowHeight);
            }

            if (!isGhost && lineNo.HasValue)
            {
                Rectangle gutterRect = new Rectangle(0, y, _gutterWidth - GutterPadding, RowHeight);
                TextRenderer.DrawText(g, lineNo.Value.ToString(), MonoFont, gutterRect, GutterColor,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            }

            if (!isGhost)
            {
                int textX = _gutterWidth + GutterPadding - _horizontalOffset;

                // 제외 필터(MaskMatch) 구간을 텍스트보다 먼저 회색으로 깔아준다. Changed 행의 intraline 강조는
                // 마스크 구간과 겹치지 않게 클리핑되므로(IntralineDiffer.ClipMask) 회색 위에 노랑이 덮이지 않는다.
                TextSpan[] masks = _side == PaneSide.Left ? row.LeftMaskSpans : row.RightMaskSpans;
                if (masks != null)
                {
                    DrawMaskBackgrounds(g, text, masks, textX, y);
                }

                if (row.Kind == RowKind.Changed)
                {
                    DrawIntralineHighlighted(g, row, text, textX, y);
                }
                else
                {
                    Rectangle textRect = new Rectangle(textX, y, Math.Max(0, ClientSize.Width - textX), RowHeight);
                    TextRenderer.DrawText(g, text ?? string.Empty, MonoFont, textRect, ForeColor,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
                }
            }

            if (_selectedBlockIndex >= 0 && row.DiffBlockIndex == _selectedBlockIndex)
            {
                using (Pen pen = new Pen(SelectedBorderColor))
                {
                    g.DrawRectangle(pen, 0, y, ClientSize.Width - 1, RowHeight - 1);
                }
            }
        }

        private void DrawMaskBackgrounds(Graphics g, string text, TextSpan[] masks, int textX, int y)
        {
            if (string.IsNullOrEmpty(text)) return;

            using (SolidBrush brush = new SolidBrush(MaskedColor))
            {
                foreach (TextSpan mask in masks)
                {
                    int start = Math.Min(mask.Start, text.Length);
                    int length = Math.Min(mask.Length, text.Length - start);
                    if (length <= 0) continue;

                    int x = textX + MeasureTextWidth(g, text.Substring(0, start));
                    int width = MeasureTextWidth(g, text.Substring(start, length));
                    g.FillRectangle(brush, x, y, width, RowHeight);
                }
            }
        }

        private static int MeasureTextWidth(Graphics g, string part)
        {
            if (part.Length == 0) return 0;
            return TextRenderer.MeasureText(g, part, MonoFont, new Size(int.MaxValue, RowHeight),
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
        }

        private void DrawIntralineHighlighted(Graphics g, AlignedRow row, string text, int textX, int y)
        {
            if (string.IsNullOrEmpty(text)) return;

            TextSpan[] mask = _side == PaneSide.Left ? row.LeftMaskSpans : row.RightMaskSpans;
            List<CharSpan> spans = _side == PaneSide.Left
                ? IntralineDiffer.ComputeLeftSpans(row.LeftText, row.RightText, mask)
                : IntralineDiffer.ComputeRightSpans(row.LeftText, row.RightText, mask);

            int x = textX;
            foreach (CharSpan span in spans)
            {
                string part = text.Substring(span.Start, span.Length);
                Size size = TextRenderer.MeasureText(g, part, MonoFont, new Size(int.MaxValue, RowHeight),
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

                if (span.IsDifferent)
                {
                    using (SolidBrush hbrush = new SolidBrush(IntralineColor))
                    {
                        g.FillRectangle(hbrush, x, y, size.Width, RowHeight);
                    }
                }

                Rectangle partRect = new Rectangle(x, y, size.Width, RowHeight);
                TextRenderer.DrawText(g, part, MonoFont, partRect, ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

                x += size.Width;
            }
        }

        private Color GetBackColor(AlignedRow row, bool isGhost)
        {
            if (isGhost) return GhostColor;

            switch (row.Kind)
            {
                case RowKind.Changed: return ChangedColor;
                case RowKind.LeftOnly:
                case RowKind.RightOnly:
                    return OnlyColor;
                default:
                    return BackColor;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            int lines = SystemInformation.MouseWheelScrollLines <= 0 ? 3 : SystemInformation.MouseWheelScrollLines;
            int delta = -(e.Delta / 120) * lines;
            RaiseScrollDelta(delta);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

            if (_document == null) return;
            int rowIndex = _firstVisibleRow + (e.Y / RowHeight);
            if (rowIndex < 0 || rowIndex >= _document.Rows.Count) return;

            int blockIndex = _document.Rows[rowIndex].DiffBlockIndex;
            if (blockIndex >= 0 && BlockClicked != null)
            {
                BlockClicked(this, blockIndex);
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.Home:
                case Keys.End:
                    return true;
                default:
                    return base.IsInputKey(keyData);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.KeyCode)
            {
                case Keys.Up: RaiseScrollDelta(-1); break;
                case Keys.Down: RaiseScrollDelta(1); break;
                case Keys.PageUp: RaiseScrollDelta(-RowsPerPage); break;
                case Keys.PageDown: RaiseScrollDelta(RowsPerPage); break;
                case Keys.Home: RaiseScrollDelta(int.MinValue / 2); break;
                case Keys.End: RaiseScrollDelta(int.MaxValue / 2); break;
            }
        }

        private void RaiseScrollDelta(int delta)
        {
            if (ScrollDeltaRequested != null) ScrollDeltaRequested(this, delta);
        }
    }
}
