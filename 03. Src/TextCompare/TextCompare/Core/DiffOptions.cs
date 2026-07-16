namespace TextCompare.Core
{
    /// <summary>라인 비교 시 정규화 옵션.</summary>
    public sealed class DiffOptions
    {
        public bool IgnoreCase { get; set; }
        public bool IgnoreWhitespace { get; set; }
        public bool IgnoreTrailingWhitespace { get; set; } = true;

        /// <summary>텍스트 라인 비교(DiffDocument.Build)에서만 적용되는 제외 필터. XML/JSON 구조 비교는 영향받지 않는다.</summary>
        public ExcludeFilterSet ExcludeFilters { get; set; } = ExcludeFilterSet.Empty;

        public static DiffOptions Default
        {
            get { return new DiffOptions(); }
        }
    }
}
