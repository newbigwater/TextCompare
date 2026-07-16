using System;
using System.Drawing;
using System.Windows.Forms;
using TextCompare.Alignment;
using TextCompare.Document;

namespace TextCompare.Controls
{
    /// <summary>
    /// WinMerge의 위치 창(location pane)에 해당하는 전체 개요 맵.
    /// 문서 전체를 세로 막대로 압축 표시하고, 클릭하면 해당 위치로 이동 요청 이벤트를 발생시킨다.
    /// </summary>
    public partial class LocationPaneControl : UserControl
    {
        private static readonly Color ChangedColor = Color.FromArgb(253, 200, 70);
        private static readonly Color OnlyColor = Color.FromArgb(240, 140, 60);
        private static readonly Color ViewportColor = Color.FromArgb(120, 120, 120);

        private DiffDocument _document;
        private int _viewportTopRow;
        private int _viewportRowCount = 1;

        public event EventHandler<int> LocationClicked;
        public event EventHandler<string[]> FilesDropped;

        public LocationPaneControl()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                      ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;

            DragDropHelper.WireFileDrop(this, files => { if (FilesDropped != null) FilesDropped(this, files); });
        }

        public DiffDocument Document
        {
            get { return _document; }
            set { _document = value; Invalidate(); }
        }

        public void UpdateViewport(int topRow, int rowCount)
        {
            _viewportTopRow = topRow;
            _viewportRowCount = Math.Max(1, rowCount);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);

            if (_document == null || _document.Rows.Count == 0) return;

            int rowCount = _document.Rows.Count;
            float scale = ClientSize.Height / (float)rowCount;

            foreach (DiffBlock block in _document.Blocks)
            {
                Color color = GetBlockColor(block);
                float y = block.StartRow * scale;
                float h = Math.Max(1f, (block.EndRow - block.StartRow) * scale);

                using (SolidBrush brush = new SolidBrush(color))
                {
                    g.FillRectangle(brush, 1, y, Math.Max(1, ClientSize.Width - 2), h);
                }
            }

            float vy = _viewportTopRow * scale;
            float vh = Math.Max(2f, _viewportRowCount * scale);
            using (Pen pen = new Pen(ViewportColor, 1))
            {
                g.DrawRectangle(pen, 0, vy, ClientSize.Width - 1, vh);
            }
        }

        private Color GetBlockColor(DiffBlock block)
        {
            for (int i = block.StartRow; i < block.EndRow; i++)
            {
                if (_document.Rows[i].Kind == RowKind.Changed) return ChangedColor;
            }
            return OnlyColor;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_document == null || _document.Rows.Count == 0) return;

            float scale = ClientSize.Height / (float)_document.Rows.Count;
            int row = (int)(e.Y / scale);
            row = Math.Max(0, Math.Min(row, _document.Rows.Count - 1));

            if (LocationClicked != null) LocationClicked(this, row);
        }
    }
}
