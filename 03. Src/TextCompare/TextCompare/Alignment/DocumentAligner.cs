using System;
using System.Collections.Generic;
using TextCompare.Core;

namespace TextCompare.Alignment
{
    /// <summary>
    /// MyersDiff가 계산한 DiffChange 목록을 좌/우 1:1 정렬된 AlignedRow 목록으로 변환한다.
    /// 삭제/삽입 구간에는 반대편에 ghost 행을 삽입해서, "라인이 밀려 보이는" WinMerge류 문제를 근본적으로 없앤다.
    /// 예) 좌 A,B,C / 우 A,C (B 삭제) -> A|A(Same), B|ghost(LeftOnly), C|C(Same)
    /// </summary>
    public static class DocumentAligner
    {
        public static List<AlignedRow> Align(IList<string> left, IList<string> right, List<DiffChange> changes)
        {
            return Align(left, right, changes, null, null);
        }

        /// <summary>
        /// leftLineNumbers/rightLineNumbers를 넘기면 a/b 커서 위치 대신 그 배열에서 실제 줄 번호를 조회한다.
        /// 제외 필터로 일부 줄이 미리 제거된 목록을 넘길 때, 화면에 표시되는 줄 번호가 원본 파일 기준을
        /// 유지하도록(제외된 만큼 건너뛰도록) 하기 위함이다. null이면 기존과 동일하게 1-based 커서 위치를 사용한다.
        /// </summary>
        public static List<AlignedRow> Align(IList<string> left, IList<string> right, List<DiffChange> changes,
            IList<int> leftLineNumbers, IList<int> rightLineNumbers)
        {
            return Align(left, right, changes, leftLineNumbers, rightLineNumbers, null, null);
        }

        /// <summary>
        /// leftMaskSpans/rightMaskSpans를 넘기면 각 행에 제외 필터(MaskMatch)가 비교에서 무시한 구간을 실어준다.
        /// UI는 이 구간을 회색 배경으로 표시하고 intraline 강조에서 제외한다. null이면 마스크 없는 행이 된다.
        /// </summary>
        public static List<AlignedRow> Align(IList<string> left, IList<string> right, List<DiffChange> changes,
            IList<int> leftLineNumbers, IList<int> rightLineNumbers,
            IList<TextSpan[]> leftMaskSpans, IList<TextSpan[]> rightMaskSpans)
        {
            if (left == null) throw new ArgumentNullException("left");
            if (right == null) throw new ArgumentNullException("right");
            if (changes == null) throw new ArgumentNullException("changes");

            var rows = new List<AlignedRow>();
            int a = 0; // left 커서 (0-based)
            int b = 0; // right 커서 (0-based)
            int blockIndex = 0;

            foreach (var change in changes)
            {
                // 이 변경 블록 이전의 공통(Same) 구간을 채운다.
                // Same 행에도 마스크 구간을 실어야 "마스크 덕분에 같아진" 구간이 회색으로 표시된다.
                while (a < change.StartA && b < change.StartB)
                {
                    rows.Add(new AlignedRow(RowKind.Same, left[a], right[b], LineNo(leftLineNumbers, a), LineNo(rightLineNumbers, b), -1,
                        Spans(leftMaskSpans, a), Spans(rightMaskSpans, b)));
                    a++; b++;
                }

                int countA = change.CountA;
                int countB = change.CountB;
                int paired = Math.Min(countA, countB);

                // 1) 겹치는 부분은 Changed 쌍으로 처리
                for (int i = 0; i < paired; i++)
                {
                    rows.Add(new AlignedRow(RowKind.Changed, left[a], right[b], LineNo(leftLineNumbers, a), LineNo(rightLineNumbers, b), blockIndex,
                        Spans(leftMaskSpans, a), Spans(rightMaskSpans, b)));
                    a++; b++;
                }

                // 2) 남은 삭제분(LeftOnly, 우측 ghost)
                for (int i = paired; i < countA; i++)
                {
                    rows.Add(new AlignedRow(RowKind.LeftOnly, left[a], null, LineNo(leftLineNumbers, a), null, blockIndex,
                        Spans(leftMaskSpans, a), null));
                    a++;
                }

                // 3) 남은 삽입분(RightOnly, 좌측 ghost)
                for (int i = paired; i < countB; i++)
                {
                    rows.Add(new AlignedRow(RowKind.RightOnly, null, right[b], null, LineNo(rightLineNumbers, b), blockIndex,
                        null, Spans(rightMaskSpans, b)));
                    b++;
                }

                if (countA > 0 || countB > 0)
                    blockIndex++;
            }

            // 마지막 변경 이후 남은 공통 구간
            while (a < left.Count && b < right.Count)
            {
                rows.Add(new AlignedRow(RowKind.Same, left[a], right[b], LineNo(leftLineNumbers, a), LineNo(rightLineNumbers, b), -1,
                    Spans(leftMaskSpans, a), Spans(rightMaskSpans, b)));
                a++; b++;
            }

            return rows;
        }

        private static int LineNo(IList<int> lineNumbers, int cursor)
        {
            return lineNumbers != null ? lineNumbers[cursor] : cursor + 1;
        }

        private static TextSpan[] Spans(IList<TextSpan[]> maskSpans, int cursor)
        {
            return maskSpans != null ? maskSpans[cursor] : null;
        }
    }
}
