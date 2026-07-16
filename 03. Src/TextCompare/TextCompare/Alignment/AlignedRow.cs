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

        public AlignedRow(RowKind kind, string leftText, string rightText, int? leftLineNo, int? rightLineNo, int diffBlockIndex)
        {
            Kind = kind;
            LeftText = leftText;
            RightText = rightText;
            LeftLineNo = leftLineNo;
            RightLineNo = rightLineNo;
            DiffBlockIndex = diffBlockIndex;
        }

        public bool IsLeftGhost { get { return LeftLineNo == null; } }
        public bool IsRightGhost { get { return RightLineNo == null; } }
    }
}
