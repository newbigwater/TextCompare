using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using TextCompare.Alignment;
using TextCompare.Document;

namespace TextCompare.Controls
{
    /// <summary>
    /// 현재 선택된(클릭/이동으로 탐색된) diff 블록의 원문을 잘림 없이 하단에 보여주는 미리보기 패널.
    /// 항상 읽기 전용이며, ghost 줄도 빈 줄로 유지해 Left/Right 대응이 메인 뷰와 동일하게 보이도록 한다.
    /// </summary>
    public partial class PreviewBarControl : UserControl
    {
        public PreviewBarControl()
        {
            InitializeComponent();
            _split.SplitterDistance = Math.Max(1, ClientSize.Height / 2);
        }

        public void Clear()
        {
            _leftBox.Clear();
            _rightBox.Clear();
        }

        public void ShowBlock(DiffDocument document, int blockIndex)
        {
            if (document == null || blockIndex < 0 || blockIndex >= document.Blocks.Count)
            {
                Clear();
                return;
            }

            DiffBlock block = document.Blocks[blockIndex];
            var rows = new List<AlignedRow>();
            for (int i = block.StartRow; i < block.EndRow; i++)
            {
                rows.Add(document.Rows[i]);
            }

            RenderSide(_leftBox, rows, true);
            RenderSide(_rightBox, rows, false);
        }

        private static void RenderSide(RichTextBox box, List<AlignedRow> rows, bool isLeft)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < rows.Count; i++)
            {
                if (i > 0) sb.Append("\r\n");
                sb.Append(isLeft ? rows[i].LeftText : rows[i].RightText);
            }

            box.Text = sb.ToString();
            DiffHighlighter.ApplyRowColors(box, rows, isLeft);
        }
    }
}
