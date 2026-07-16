using System.Collections.Generic;
using TextCompare.Core;

namespace TextCompare.Intraline
{
    /// <summary>
    /// Changed로 판정된 좌/우 라인 쌍에 대해 문자 단위 MyersDiff를 적용,
    /// 어느 부분이 실제로 다른지 CharSpan 목록으로 반환한다(라인 내부 하이라이트용).
    /// </summary>
    public static class IntralineDiffer
    {
        public static List<CharSpan> ComputeLeftSpans(string left, string right)
        {
            return Compute(left, right, useA: true);
        }

        public static List<CharSpan> ComputeRightSpans(string left, string right)
        {
            return Compute(left, right, useA: false);
        }

        private static List<CharSpan> Compute(string left, string right, bool useA)
        {
            left = left ?? string.Empty;
            right = right ?? string.Empty;

            var leftChars = left.ToCharArray();
            var rightChars = right.ToCharArray();
            var changes = new MyersDiff<char>(leftChars, rightChars).Compute();

            string target = useA ? left : right;
            var spans = new List<CharSpan>();
            int cursor = 0;

            foreach (var c in changes)
            {
                int diffStart = useA ? c.StartA : c.StartB;
                int diffCount = useA ? c.CountA : c.CountB;

                if (diffCount == 0)
                    continue;

                if (diffStart > cursor)
                {
                    spans.Add(new CharSpan(cursor, diffStart - cursor, false));
                }

                spans.Add(new CharSpan(diffStart, diffCount, true));
                cursor = diffStart + diffCount;
            }

            if (cursor < target.Length)
            {
                spans.Add(new CharSpan(cursor, target.Length - cursor, false));
            }

            return spans;
        }
    }
}
