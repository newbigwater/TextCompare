using System.Collections.Generic;
using System.Text;

namespace TextCompare.Core
{
    /// <summary>
    /// 정규식 제외 필터 패턴 목록 + 활성화 여부. 비교 전에 좌/우 각각의 줄 목록에 적용되며(Apply),
    /// 패턴 모드에 따라 두 가지로 동작한다:
    /// - ExcludeLine: 일치하는 줄을 완전히 제거하고, 남은 줄의 원본 줄 번호를 함께 반환한다
    ///   (제거된 줄만큼 번호가 건너뛰도록 해서 실제 파일의 줄 번호와 화면 표시가 어긋나지 않게 한다).
    /// - MaskMatch: 줄은 유지하되 매치 구간을 센티널 문자(MaskChar)로 치환한 "비교용" 줄을 만들어,
    ///   diff 판정에서만 그 구간이 무시되게 한다(화면에는 원문이 그대로 표시되고 구간은 회색으로 칠해진다).
    /// </summary>
    public sealed class ExcludeFilterSet
    {
        /// <summary>
        /// MaskMatch 매치 구간을 비교용 문자열에서 대신하는 센티널 문자(U+E000, 사설 영역).
        /// 빈 문자열로 지우지 않고 1문자를 남기는 이유: 매치가 한쪽에만 있거나 매치 개수가 다른 두 줄을
        /// 여전히 "다름"으로 판정하기 위해서다. 원문에 U+E000이 실제로 존재하면 이론상 오판할 수 있으나
        /// 사설 영역 문자라 실사용 텍스트에서는 사실상 나타나지 않는다.
        /// </summary>
        public const char MaskChar = '\uE000';

        public bool Enabled { get; set; }
        public List<ExcludeFilterPattern> Patterns { get; private set; }

        public ExcludeFilterSet()
        {
            Patterns = new List<ExcludeFilterPattern>();
        }

        public static ExcludeFilterSet Empty
        {
            get { return new ExcludeFilterSet(); }
        }

        /// <summary>
        /// ExcludeLine 모드의 유효 패턴이 하나라도 활성인가. 편집 모드 차단 판단용 —
        /// 줄이 물리적으로 제거되면 편집·저장 라운드트립이 불가능하지만, MaskMatch만 있으면 편집해도 안전하다.
        /// </summary>
        public bool HasLineExclusions
        {
            get
            {
                if (!Enabled) return false;
                foreach (ExcludeFilterPattern pattern in Patterns)
                {
                    if (pattern.Mode == ExcludeFilterMode.ExcludeLine && pattern.IsValid) return true;
                }
                return false;
            }
        }

        /// <summary>이 줄이 ExcludeLine 모드 패턴에 걸려 통째로 제거되어야 하는가(MaskMatch 패턴은 관여하지 않는다).</summary>
        public bool IsExcluded(string line)
        {
            if (!Enabled) return false;
            foreach (ExcludeFilterPattern pattern in Patterns)
            {
                if (pattern.Mode == ExcludeFilterMode.ExcludeLine && pattern.IsMatch(line)) return true;
            }
            return false;
        }

        /// <summary>Apply의 결과 묶음. 네 목록은 모두 같은 길이의 병렬 목록이다.</summary>
        public sealed class FilterApplyResult
        {
            /// <summary>표시용 원본 줄(ExcludeLine 매치 줄은 제거됨).</summary>
            public List<string> Lines;

            /// <summary>각 줄의 1-based 원본 줄 번호(제거된 줄만큼 건너뜀).</summary>
            public List<int> OriginalLineNumbers;

            /// <summary>비교용 줄(MaskMatch 구간이 MaskChar 1문자로 치환됨. 마스크가 없으면 원본과 동일 참조).</summary>
            public List<string> ComparisonLines;

            /// <summary>줄별 마스크 구간(원문 좌표, 정렬·병합됨). 해당 줄에 마스크가 없으면 null.</summary>
            public List<TextSpan[]> MaskSpans;
        }

        /// <summary>
        /// lines에 필터를 적용한다. Enabled=false이거나 패턴이 없으면 원본을 그대로 복사하고
        /// ComparisonLines는 원본과 동일한 참조, MaskSpans는 전부 null이 된다.
        /// </summary>
        public FilterApplyResult Apply(IList<string> lines)
        {
            var result = new FilterApplyResult
            {
                Lines = new List<string>(lines.Count),
                OriginalLineNumbers = new List<int>(lines.Count),
                ComparisonLines = new List<string>(lines.Count),
                MaskSpans = new List<TextSpan[]>(lines.Count)
            };

            bool hasMaskPatterns = false;
            if (Enabled)
            {
                foreach (ExcludeFilterPattern pattern in Patterns)
                {
                    if (pattern.Mode == ExcludeFilterMode.MaskMatch && pattern.IsValid)
                    {
                        hasMaskPatterns = true;
                        break;
                    }
                }
            }

            List<TextSpan> spanBuffer = hasMaskPatterns ? new List<TextSpan>() : null;

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                if (IsExcluded(line)) continue;

                TextSpan[] masks = null;
                string comparison = line;
                if (hasMaskPatterns && !string.IsNullOrEmpty(line))
                {
                    spanBuffer.Clear();
                    foreach (ExcludeFilterPattern pattern in Patterns)
                    {
                        if (pattern.Mode == ExcludeFilterMode.MaskMatch)
                        {
                            pattern.AppendMatchSpans(line, spanBuffer);
                        }
                    }

                    if (spanBuffer.Count > 0)
                    {
                        masks = MergeSpans(spanBuffer);
                        comparison = MaskLine(line, masks);
                    }
                }

                result.Lines.Add(line);
                result.OriginalLineNumbers.Add(i + 1);
                result.ComparisonLines.Add(comparison);
                result.MaskSpans.Add(masks);
            }

            return result;
        }

        /// <summary>구간을 시작 위치로 정렬하고, 겹치거나 맞닿은 구간을 하나로 병합한다(여러 MaskMatch 패턴 중첩 대비).</summary>
        private static TextSpan[] MergeSpans(List<TextSpan> spans)
        {
            spans.Sort((x, y) => x.Start != y.Start ? x.Start.CompareTo(y.Start) : x.Length.CompareTo(y.Length));

            var merged = new List<TextSpan>(spans.Count);
            int start = spans[0].Start;
            int end = spans[0].End;

            for (int i = 1; i < spans.Count; i++)
            {
                if (spans[i].Start <= end)
                {
                    if (spans[i].End > end) end = spans[i].End;
                }
                else
                {
                    merged.Add(new TextSpan(start, end - start));
                    start = spans[i].Start;
                    end = spans[i].End;
                }
            }

            merged.Add(new TextSpan(start, end - start));
            return merged.ToArray();
        }

        /// <summary>병합된 마스크 구간 각각을 MaskChar 1문자로 치환한 비교용 문자열을 만든다.</summary>
        private static string MaskLine(string line, TextSpan[] masks)
        {
            var sb = new StringBuilder(line.Length);
            int cursor = 0;

            foreach (TextSpan mask in masks)
            {
                if (mask.Start > cursor) sb.Append(line, cursor, mask.Start - cursor);
                sb.Append(MaskChar);
                cursor = mask.End;
            }

            if (cursor < line.Length) sb.Append(line, cursor, line.Length - cursor);
            return sb.ToString();
        }
    }
}
