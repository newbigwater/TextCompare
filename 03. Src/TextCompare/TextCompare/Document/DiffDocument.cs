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

        /// <summary>
        /// onStage가 주어지면 이 메서드가 실제로 완료한 내부 단계마다 그 이름("filter"/"diff"/"align"/"blocks")을
        /// 콜백으로 알려준다. 진행율 표시(%)로의 매핑은 호출자가 담당한다 — 이 메서드는 자신이 아는 "실제 단계
        /// 경계"만 정확히 보고할 뿐, 화면에 몇 %로 보일지는 관여하지 않는다.
        /// </summary>
        public static DiffDocument Build(IList<string> left, IList<string> right, DiffOptions options, Action<string> onStage = null)
        {
            if (left == null) throw new ArgumentNullException("left");
            if (right == null) throw new ArgumentNullException("right");

            DiffOptions opts = options ?? DiffOptions.Default;
            ExcludeFilterSet filters = opts.ExcludeFilters ?? ExcludeFilterSet.Empty;

            // 제외 필터를 비교 전에 좌/우 각각에 적용한다. ExcludeLine 패턴에 걸린 줄은 목록에서 제거되고,
            // MaskMatch 패턴의 매치 구간은 센티널 문자로 치환된 "비교용 줄"(ComparisonLines)로만 diff 판정에
            // 반영된다 — 화면에는 원문(Lines)이 그대로 표시된다. 텍스트 라인 비교(Build)에서만 이 필터를
            // 적용하므로 XML/JSON 구조 비교(StructureDiffer)는 전혀 영향받지 않는다.
            ExcludeFilterSet.FilterApplyResult fl = filters.Apply(left);
            ExcludeFilterSet.FilterApplyResult fr = filters.Apply(right);
            if (onStage != null) onStage("filter");

            var hasher = new LineHasher(opts);
            var changes = new MyersDiff<string>(fl.ComparisonLines, fr.ComparisonLines, hasher).Compute();
            if (onStage != null) onStage("diff");

            var rows = DocumentAligner.Align(fl.Lines, fr.Lines, changes,
                fl.OriginalLineNumbers, fr.OriginalLineNumbers, fl.MaskSpans, fr.MaskSpans);
            if (onStage != null) onStage("align");

            DiffDocument document = FromRows(rows);
            if (onStage != null) onStage("blocks");

            return document;
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
