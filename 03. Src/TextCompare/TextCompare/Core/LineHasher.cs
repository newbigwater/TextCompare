using System;
using System.Collections.Generic;
using System.Text;

namespace TextCompare.Core
{
    /// <summary>
    /// DiffOptions(공백/대소문자 무시)를 반영하여 두 라인이 같은지 비교하는 IEqualityComparer.
    /// MyersDiff&lt;string&gt;에 그대로 주입해서 사용한다.
    /// </summary>
    public sealed class LineHasher : IEqualityComparer<string>
    {
        private readonly DiffOptions _options;

        public LineHasher(DiffOptions options)
        {
            _options = options ?? DiffOptions.Default;
        }

        public string Normalize(string line)
        {
            if (line == null) return string.Empty;
            string s = line;

            if (_options.IgnoreTrailingWhitespace)
                s = s.TrimEnd();

            if (_options.IgnoreWhitespace)
                s = CollapseWhitespace(s);

            if (_options.IgnoreCase)
                s = s.ToUpperInvariant();

            return s;
        }

        public bool Equals(string x, string y)
        {
            return string.Equals(Normalize(x), Normalize(y), StringComparison.Ordinal);
        }

        public int GetHashCode(string obj)
        {
            return Normalize(obj).GetHashCode();
        }

        private static string CollapseWhitespace(string s)
        {
            var sb = new StringBuilder(s.Length);
            bool lastWasSpace = false;
            foreach (char c in s.Trim())
            {
                if (char.IsWhiteSpace(c))
                {
                    if (!lastWasSpace)
                    {
                        sb.Append(' ');
                        lastWasSpace = true;
                    }
                }
                else
                {
                    sb.Append(c);
                    lastWasSpace = false;
                }
            }
            return sb.ToString();
        }
    }
}
