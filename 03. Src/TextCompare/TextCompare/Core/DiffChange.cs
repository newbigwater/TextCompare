namespace TextCompare.Core
{
    /// <summary>
    /// A(좌측) 구간 [StartA, StartA+CountA) 가 B(우측) 구간 [StartB, StartB+CountB) 로 바뀐 변경 블록.
    /// CountA==0 이면 순수 삽입, CountB==0 이면 순수 삭제, 둘 다 &gt;0 이면 치환(변경).
    /// </summary>
    public struct DiffChange
    {
        public int StartA;
        public int CountA;
        public int StartB;
        public int CountB;

        public DiffChange(int startA, int countA, int startB, int countB)
        {
            StartA = startA;
            CountA = countA;
            StartB = startB;
            CountB = countB;
        }

        public bool IsDelete { get { return CountA > 0 && CountB == 0; } }
        public bool IsInsert { get { return CountA == 0 && CountB > 0; } }
        public bool IsReplace { get { return CountA > 0 && CountB > 0; } }

        public override string ToString()
        {
            return string.Format("A[{0},{1}) -> B[{2},{3})", StartA, StartA + CountA, StartB, StartB + CountB);
        }
    }
}
