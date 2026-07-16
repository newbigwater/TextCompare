namespace TextCompare.Document
{
    /// <summary>연속된 변경 행들의 그룹. "이전/다음 diff 이동" 및 위치 창(개요 맵)의 단위.</summary>
    public struct DiffBlock
    {
        public readonly int StartRow; // AlignedRow 목록 내 시작 인덱스 (포함)
        public readonly int EndRow;   // 끝 인덱스 (미포함)

        public DiffBlock(int startRow, int endRow)
        {
            StartRow = startRow;
            EndRow = endRow;
        }
    }
}
