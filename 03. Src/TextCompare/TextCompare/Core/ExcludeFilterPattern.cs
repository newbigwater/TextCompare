using System.Text.RegularExpressions;

namespace TextCompare.Core
{
    /// <summary>
    /// 제외 필터 패턴 1개. 정규식 컴파일에 실패해도 예외를 던지지 않고 무효 상태로 남아,
    /// 이후 IsMatch가 항상 false를 반환한다(잘못된 패턴이 비교 자체를 막지 않도록).
    /// </summary>
    public sealed class ExcludeFilterPattern
    {
        private readonly Regex _regex;

        public string Pattern { get; private set; }
        public bool IsValid { get; private set; }
        public string ErrorMessage { get; private set; }

        public ExcludeFilterPattern(string pattern)
        {
            Pattern = pattern ?? string.Empty;

            try
            {
                _regex = new Regex(Pattern, RegexOptions.Compiled);
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
            return _regex.IsMatch(line);
        }
    }
}
