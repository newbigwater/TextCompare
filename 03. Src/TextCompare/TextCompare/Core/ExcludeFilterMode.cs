namespace TextCompare.Core
{
    /// <summary>제외 필터 패턴 1개의 동작 모드.</summary>
    public enum ExcludeFilterMode
    {
        /// <summary>패턴이 일치하는 줄을 비교·화면에서 통째로 제거한다(기존 동작).</summary>
        ExcludeLine = 0,

        /// <summary>줄은 그대로 두고, 정규식이 매치된 구간만 비교에서 무시한다(화면에는 회색 배경으로 표시).</summary>
        MaskMatch = 1
    }
}
