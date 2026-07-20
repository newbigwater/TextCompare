using System;
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

        /// <summary>mask(제외 필터 MaskMatch 구간)와 겹치는 부분은 IsDifferent=false로 클리핑해서 반환한다 —
        /// 비교에서 무시된 구간이 문자 단위 강조(노랑)로 칠해지지 않게 한다.</summary>
        public static List<CharSpan> ComputeLeftSpans(string left, string right, TextSpan[] leftMask)
        {
            return ClipMask(Compute(left, right, useA: true), leftMask);
        }

        /// <summary>mask(제외 필터 MaskMatch 구간)와 겹치는 부분은 IsDifferent=false로 클리핑해서 반환한다.</summary>
        public static List<CharSpan> ComputeRightSpans(string left, string right, TextSpan[] rightMask)
        {
            return ClipMask(Compute(left, right, useA: false), rightMask);
        }

        /// <summary>
        /// IsDifferent=true인 스팬을 mask 경계에서 분할해, mask와 겹치는 조각을 IsDifferent=false로 바꾼다.
        /// 전체 커버리지(스팬들이 라인을 빈틈없이 덮는 구조)는 유지되므로 렌더러는 결과를 그대로 순회할 수 있다.
        /// mask는 Apply가 정렬·병합해 둔 상태를 전제한다.
        /// </summary>
        private static List<CharSpan> ClipMask(List<CharSpan> spans, TextSpan[] mask)
        {
            if (mask == null || mask.Length == 0) return spans;

            var result = new List<CharSpan>(spans.Count);
            foreach (CharSpan span in spans)
            {
                if (!span.IsDifferent)
                {
                    result.Add(span);
                    continue;
                }

                int cursor = span.Start;
                int spanEnd = span.Start + span.Length;
                foreach (TextSpan m in mask)
                {
                    if (m.End <= cursor) continue;
                    if (m.Start >= spanEnd) break;

                    int overlapStart = Math.Max(m.Start, cursor);
                    int overlapEnd = Math.Min(m.End, spanEnd);
                    if (overlapStart > cursor)
                    {
                        result.Add(new CharSpan(cursor, overlapStart - cursor, true));
                    }
                    result.Add(new CharSpan(overlapStart, overlapEnd - overlapStart, false));
                    cursor = overlapEnd;
                }

                if (cursor < spanEnd)
                {
                    result.Add(new CharSpan(cursor, spanEnd - cursor, true));
                }
            }

            return result;
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
