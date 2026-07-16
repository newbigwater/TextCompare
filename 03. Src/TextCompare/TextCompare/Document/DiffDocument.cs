using System;
using System.Collections.Generic;
using TextCompare.Alignment;
using TextCompare.Core;

namespace TextCompare.Document
{
    /// <summary>
    /// 비교 결과 전체를 담는 뷰모델. AlignedRow 목록 + diff 블록 인덱스 + 통계를 제공하며
    /// UI 레이어(Controls)는 이 클래스만 바라보고 렌더링한다.
    /// </summary>
    public sealed class DiffDocument
    {
        public IList<AlignedRow> Rows { get; private set; }
        public IList<DiffBlock> Blocks { get; private set; }

        public int ChangedLineCount { get; private set; }
        public int LeftOnlyLineCount { get; private set; }
        public int RightOnlyLineCount { get; private set; }

        private DiffDocument(IList<AlignedRow> rows, IList<DiffBlock> blocks)
        {
            Rows = rows;
            Blocks = blocks;

            foreach (var row in rows)
            {
                switch (row.Kind)
                {
                    case RowKind.Changed: ChangedLineCount++; break;
                    case RowKind.LeftOnly: LeftOnlyLineCount++; break;
                    case RowKind.RightOnly: RightOnlyLineCount++; break;
                }
            }
        }

        public int TotalDiffCount
        {
            get { return Blocks.Count; }
        }

        public static DiffDocument Build(IList<string> left, IList<string> right, DiffOptions options)
        {
            if (left == null) throw new ArgumentNullException("left");
            if (right == null) throw new ArgumentNullException("right");

            var hasher = new LineHasher(options ?? DiffOptions.Default);
            var changes = new MyersDiff<string>(left, right, hasher).Compute();
            var rows = DocumentAligner.Align(left, right, changes);

            return FromRows(rows);
        }

        /// <summary>이미 정렬된 AlignedRow 목록(예: 구조적 XML/JSON 비교 결과)으로부터 바로 문서를 만든다.
        /// 라인 비교(Build)와 구조 비교(Structure.StructureDiffer) 양쪽이 공유하는 진입점.</summary>
        public static DiffDocument FromRows(IList<AlignedRow> rows)
        {
            if (rows == null) throw new ArgumentNullException("rows");

            var blocks = BuildBlocks(rows);
            return new DiffDocument(rows, blocks);
        }

        private static List<DiffBlock> BuildBlocks(IList<AlignedRow> rows)
        {
            var blocks = new List<DiffBlock>();
            int i = 0;
            while (i < rows.Count)
            {
                if (rows[i].DiffBlockIndex < 0)
                {
                    i++;
                    continue;
                }

                int currentBlockIndex = rows[i].DiffBlockIndex;
                int start = i;
                while (i < rows.Count && rows[i].DiffBlockIndex == currentBlockIndex)
                {
                    i++;
                }
                blocks.Add(new DiffBlock(start, i));
            }
            return blocks;
        }
    }
}
