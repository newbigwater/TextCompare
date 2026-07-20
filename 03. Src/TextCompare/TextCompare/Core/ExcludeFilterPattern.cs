using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TextCompare.Core
{
    /// <summary>
    /// 제외 필터 패턴 1개(정규식 + 동작 모드). 정규식 컴파일에 실패해도 예외를 던지지 않고 무효 상태로 남아,
    /// 이후 IsMatch/AppendMatchSpans가 아무 동작도 하지 않는다(잘못된 패턴이 비교 자체를 막지 않도록).
    /// </summary>
    public sealed class ExcludeFilterPattern
    {
        /// <summary>파국적 백트래킹으로 비교가 멈추지 않도록 하는 매칭 시간 상한.</summary>
        private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(2);

        private readonly Regex _regex;

        public string Pattern { get; private set; }
        public ExcludeFilterMode Mode { get; private set; }
        public bool IsValid { get; private set; }
        public string ErrorMessage { get; private set; }

        public ExcludeFilterPattern(string pattern)
            : this(pattern, ExcludeFilterMode.ExcludeLine)
        {
        }

        public ExcludeFilterPattern(string pattern, ExcludeFilterMode mode)
        {
            Pattern = pattern ?? string.Empty;
            Mode = mode;

            try
            {
                _regex = new Regex(Pattern, RegexOptions.Compiled, MatchTimeout);
                IsValid = true;
                ErrorMessage = null;
            }
            catch (System.ArgumentException ex)
            {
                _regex = null;
                IsValid = false;
                ErrorMessage = ex.Message;
            }
        }

        public bool IsMatch(string line)
        {
            if (!IsValid || line == null) return false;
            try
            {
                return _regex.IsMatch(line);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        /// <summary>
        /// MaskMatch 모드용: line 안의 모든 정규식 매치 구간을 spans에 추가한다.
        /// 길이 0 매치는 무시하고, 무효 패턴이거나 타임아웃이면 아무것도 추가하지 않는다
        /// (해당 줄은 마스크 없이 원문 그대로 비교되는 안전한 폴백).
        /// </summary>
        public void AppendMatchSpans(string line, List<TextSpan> spans)
        {
            if (!IsValid || string.IsNullOrEmpty(line)) return;

            try
            {
                foreach (Match match in _regex.Matches(line))
                {
                    if (match.Length > 0)
                    {
                        spans.Add(new TextSpan(match.Index, match.Length));
                    }
                }
            }
            catch (RegexMatchTimeoutException)
            {
            }
        }
    }
}
