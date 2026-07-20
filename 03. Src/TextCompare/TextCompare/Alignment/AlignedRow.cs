using TextCompare.Core;

namespace TextCompare.Alignment
{
    /// <summary>
    /// 좌/우 페인이 항상 1:1로 매핑되도록 정렬된 한 행.
    /// LeftLineNo/RightLineNo가 null이면 해당 편은 ghost(빈 placeholder) 라인이다.
    /// </summary>
    public struct AlignedRow
    {
        public readonly RowKind Kind;
        public readonly string LeftText;
        public readonly string RightText;
        public readonly int? LeftLineNo;   // 1-based 원본 라인 번호, ghost면 null
        public readonly int? RightLineNo;  // 1-based 원본 라인 번호, ghost면 null

        /// <summary>같은 Changed 블록에 속한 행들을 그룹핑하기 위한 diff 블록 인덱스. Same 행은 -1.</summary>
        public readonly int DiffBlockIndex;

        /// <summary>제외 필터(MaskMatch)가 비교에서 무시한 좌측 텍스트 구간(원문 좌표). 없으면 null.</summary>
        public readonly TextSpan[] LeftMaskSpans;

        /// <summary>제외 필터(MaskMatch)가 비교에서 무시한 우측 텍스트 구간(원문 좌표). 없으면 null.</summary>
        public readonly TextSpan[] RightMaskSpans;

        public AlignedRow(RowKind kind, string leftText, string rightText, int? leftLineNo, int? rightLineNo, int diffBlockIndex)
            : this(kind, leftText, rightText, leftLineNo, rightLineNo, diffBlockIndex, null, null)
        {
        }

        public AlignedRow(RowKind kind, string leftText, string rightText, int? leftLineNo, int? rightLineNo, int diffBlockIndex,
            TextSpan[] leftMaskSpans, TextSpan[] rightMaskSpans)
        {
            Kind = kind;
            LeftText = leftText;
            RightText = rightText;
            LeftLineNo = leftLineNo;
            RightLineNo = rightLineNo;
            DiffBlockIndex = diffBlockIndex;
            LeftMaskSpans = leftMaskSpans;
            RightMaskSpans = rightMaskSpans;
        }

        public bool IsLeftGhost { get { return LeftLineNo == null; } }
        public bool IsRightGhost { get { return RightLineNo == null; } }
    }
}
