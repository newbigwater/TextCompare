using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using TextCompare.Alignment;
using TextCompare.Core;
using TextCompare.Intraline;

namespace TextCompare.Controls
{
    /// <summary>
    /// RichTextBox에 AlignedRow 목록 기준으로 줄 단위 배경색을 적용하는 공통 헬퍼.
    /// 편집 모드 실시간 하이라이트(DiffViewerControl)와 하단 미리보기(PreviewBarControl)가 공유한다.
    /// 호출자는 box.Lines가 correspondingRows와 정확히 1:1로 대응하도록 미리 채워둬야 한다
    /// (편집기는 ghost를 건너뛰고 채우고, 미리보기는 ghost도 빈 줄로 채우는 등 정책이 다를 수 있어
    /// 이 헬퍼는 "몇 번째 줄이 어떤 AlignedRow에 해당하는가"만 그대로 받아 색칠에만 집중한다).
    /// </summary>
    internal static class DiffHighlighter
    {
        public static readonly Color ChangedColor = Color.FromArgb(253, 245, 184);
        public static readonly Color OnlyColor = Color.FromArgb(253, 200, 160);
        public static readonly Color GhostColor = Color.FromArgb(224, 224, 224);
        public static readonly Color IntralineColor = Color.FromArgb(245, 212, 0);
        /// <summary>제외 필터(MaskMatch)가 비교에서 무시한 구간의 배경색(DiffPaneControl.MaskedColor와 동일 값).</summary>
        public static readonly Color MaskedColor = Color.FromArgb(211, 211, 211);

        private const int WM_SETREDRAW = 0x000B;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, bool wParam, IntPtr lParam);

        public static void ApplyRowColors(RichTextBox box, IList<AlignedRow> correspondingRows, bool isLeft)
        {
            int savedStart = box.SelectionStart;
            int savedLength = box.SelectionLength;

            // RichTextBox.SelectionBackColor는 SuspendLayout/ResumeLayout으로 막히지 않고 호출마다 즉시 다시 그려진다.
            // 줄 수가 많으면(실제 XML 800줄+) 매 디바운스 재비교마다 눈에 보이는 깜빡임(리로드처럼 보임)이 생기므로
            // WM_SETREDRAW로 실제 화면 갱신 자체를 색칠 루프 동안 꺼두고, 끝나고 한 번만 다시 그린다.
            SendMessage(box.Handle, WM_SETREDRAW, false, IntPtr.Zero);
            try
            {
                box.SelectAll();
                box.SelectionBackColor = box.BackColor;

                int lineCount = Math.Min(correspondingRows.Count, box.Lines.Length);
                for (int lineIndex = 0; lineIndex < lineCount; lineIndex++)
                {
                    AlignedRow row = correspondingRows[lineIndex];
                    bool isGhost = isLeft ? row.IsLeftGhost : row.IsRightGhost;
                    TextSpan[] mask = isGhost ? null : (isLeft ? row.LeftMaskSpans : row.RightMaskSpans);

                    Color color = Color.Empty;
                    bool hasRowColor = true;
                    if (isGhost) color = GhostColor;
                    else if (row.Kind == RowKind.Changed) color = ChangedColor;
                    else if (row.Kind == RowKind.LeftOnly || row.Kind == RowKind.RightOnly) color = OnlyColor;
                    else hasRowColor = false; // Same 실라인은 기본 배경 유지

                    // Same 실라인이라도 마스크 구간(비교 제외로 인해 같아진 부분)은 회색으로 칠해야 하므로
                    // 행 배경색이 없고 마스크도 없을 때만 건너뛴다.
                    if (!hasRowColor && mask == null) continue;

                    int charStart = box.GetFirstCharIndexFromLine(lineIndex);
                    if (charStart < 0) continue;

                    int charEnd = (lineIndex + 1 < box.Lines.Length)
                        ? box.GetFirstCharIndexFromLine(lineIndex + 1)
                        : box.TextLength;
                    int length = Math.Max(0, charEnd - charStart);
                    if (length == 0) continue;

                    if (hasRowColor)
                    {
                        box.Select(charStart, length);
                        box.SelectionBackColor = color;
                    }

                    // 제외 필터(MaskMatch)가 비교에서 무시한 구간을 회색으로 표시한다.
                    if (mask != null)
                    {
                        string maskedText = isLeft ? row.LeftText : row.RightText;
                        foreach (TextSpan m in mask)
                        {
                            int maskLength = Math.Min(m.Length, (maskedText == null ? 0 : maskedText.Length) - m.Start);
                            if (maskLength <= 0) continue;

                            box.Select(charStart + m.Start, maskLength);
                            box.SelectionBackColor = MaskedColor;
                        }
                    }

                    // Changed 라인은 배경색 위에 실제로 달라진 부분만 문자 단위로 한 번 더 덧칠해
                    // WinMerge의 상세 비교 창처럼 어디가 다른지 정확히 짚어준다.
                    // 마스크 구간은 클리핑되어 IsDifferent=false가 되므로 회색 위에 노랑이 덮이지 않는다.
                    if (!isGhost && row.Kind == RowKind.Changed)
                    {
                        string text = isLeft ? row.LeftText : row.RightText;
                        List<CharSpan> spans = isLeft
                            ? IntralineDiffer.ComputeLeftSpans(row.LeftText, row.RightText, row.LeftMaskSpans)
                            : IntralineDiffer.ComputeRightSpans(row.LeftText, row.RightText, row.RightMaskSpans);

                        foreach (CharSpan span in spans)
                        {
                            if (!span.IsDifferent) continue;
                            int spanLength = Math.Min(span.Length, (text == null ? 0 : text.Length) - span.Start);
                            if (spanLength <= 0) continue;

                            box.Select(charStart + span.Start, spanLength);
                            box.SelectionBackColor = IntralineColor;
                        }
                    }
                }

                box.SelectionStart = savedStart;
                box.SelectionLength = savedLength;
                box.SelectionBackColor = box.BackColor;
            }
            finally
            {
                SendMessage(box.Handle, WM_SETREDRAW, true, IntPtr.Zero);
                box.Invalidate();
            }
        }
    }
}
