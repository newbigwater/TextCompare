namespace TextCompare.Intraline
{
    /// <summary>라인 내부의 문자 구간 하나. Changed 라인 쌍에서 서로 다른 부분을 강조하는 데 사용.</summary>
    public struct CharSpan
    {
        public readonly int Start;
        public readonly int Length;
        public readonly bool IsDifferent;

        public CharSpan(int start, int length, bool isDifferent)
        {
            Start = start;
            Length = length;
            IsDifferent = isDifferent;
        }
    }
}
