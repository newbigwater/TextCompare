namespace TextCompare.Core
{
    /// <summary>라인 비교 시 정규화 옵션.</summary>
    public sealed class DiffOptions
    {
        public bool IgnoreCase { get; set; }
        public bool IgnoreWhitespace { get; set; }
        public bool IgnoreTrailingWhitespace { get; set; } = true;

        public static DiffOptions Default
        {
            get { return new DiffOptions(); }
        }
    }
}
