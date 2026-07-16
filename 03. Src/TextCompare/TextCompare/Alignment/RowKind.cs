namespace TextCompare.Alignment
{
    /// <summary>정렬된 한 행(Row)의 종류.</summary>
    public enum RowKind
    {
        /// <summary>좌우 라인이 동일함.</summary>
        Same,
        /// <summary>좌우 라인이 존재하지만 내용이 다름(치환).</summary>
        Changed,
        /// <summary>좌측에만 라인이 존재함(우측은 ghost). WinMerge의 삭제 라인 밀림 문제를 해결하는 핵심 케이스.</summary>
        LeftOnly,
        /// <summary>우측에만 라인이 존재함(좌측은 ghost).</summary>
        RightOnly
    }
}
