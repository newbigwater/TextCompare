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
                while (a < change.StartA && b < change.StartB)
                {
                    rows.Add(new AlignedRow(RowKind.Same, left[a], right[b], a + 1, b + 1, -1));
                    a++; b++;
                }

                int countA = change.CountA;
                int countB = change.CountB;
                int paired = Math.Min(countA, countB);

                // 1) 겹치는 부분은 Changed 쌍으로 처리
                for (int i = 0; i < paired; i++)
                {
                    rows.Add(new AlignedRow(RowKind.Changed, left[a], right[b], a + 1, b + 1, blockIndex));
                    a++; b++;
                }

                // 2) 남은 삭제분(LeftOnly, 우측 ghost)
                for (int i = paired; i < countA; i++)
                {
                    rows.Add(new AlignedRow(RowKind.LeftOnly, left[a], null, a + 1, null, blockIndex));
                    a++;
                }

                // 3) 남은 삽입분(RightOnly, 좌측 ghost)
                for (int i = paired; i < countB; i++)
                {
                    rows.Add(new AlignedRow(RowKind.RightOnly, null, right[b], null, b + 1, blockIndex));
                    b++;
                }

                if (countA > 0 || countB > 0)
                    blockIndex++;
            }

            // 마지막 변경 이후 남은 공통 구간
            while (a < left.Count && b < right.Count)
            {
                rows.Add(new AlignedRow(RowKind.Same, left[a], right[b], a + 1, b + 1, -1));
                a++; b++;
            }

            return rows;
        }
    }
}
