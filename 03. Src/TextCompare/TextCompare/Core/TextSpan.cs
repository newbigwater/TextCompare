namespace TextCompare.Core
{
    /// <summary>
    /// 라인 내 문자 구간(Start/Length). 제외 필터 MaskMatch 모드가 비교에서 무시한 구간을
    /// UI(회색 표시)와 intraline 강조 클리핑에 전달할 때 쓴다.
    /// Intraline.CharSpan과 달리 diff 여부(IsDifferent) 의미가 없는 순수 구간이다.
    /// </summary>
    public struct TextSpan
    {
        public readonly int Start;
        public readonly int Length;

        public TextSpan(int start, int length)
        {
            Start = start;
            Length = length;
        }

        public int End
        {
            get { return Start + Length; }
        }
    }
}
