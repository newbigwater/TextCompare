using System;
using System.Text;

namespace TextCompare.Structure
{
    /// <summary>
    /// 자체 구현 재귀하강 JSON 파서. NuGet/System.Web.Extensions 없이 직접 작성한 이유는
    /// JavaScriptSerializer의 Dictionary 기반 결과가 프로퍼티 순서를 보존하지 않기 때문(diff 정렬에 순서가 중요).
    /// </summary>
    public static class JsonStructureParser
    {
        public static StructuredNode Parse(string json)
        {
            int pos = 0;
            StructuredNode root = ParseValue(json, ref pos, "$root");
            SkipWhitespace(json, ref pos);
            if (pos != json.Length)
            {
                throw new FormatException("JSON 끝에 예상치 못한 문자가 있습니다 (위치 " + pos + ")");
            }
            return root;
        }

        private static StructuredNode ParseValue(string s, ref int pos, string name)
        {
            SkipWhitespace(s, ref pos);
            if (pos >= s.Length) throw new FormatException("예상치 못한 JSON 끝");

            char c = s[pos];
            if (c == '{') return ParseObject(s, ref pos, name);
            if (c == '[') return ParseArray(s, ref pos, name);
            if (c == '"') return ParseStringNode(s, ref pos, name);
            if (c == 't' || c == 'f') return ParseBooleanNode(s, ref pos, name);
            if (c == 'n') return ParseNullNode(s, ref pos, name);
            if (c == '-' || (c >= '0' && c <= '9')) return ParseNumberNode(s, ref pos, name);

            throw new FormatException(string.Format("예상치 못한 문자 '{0}' (위치 {1})", c, pos));
        }

        private static StructuredNode ParseObject(string s, ref int pos, string name)
        {
            Expect(s, ref pos, '{');
            var node = new StructuredNode(name, NodeKind.Element);
            SkipWhitespace(s, ref pos);
            if (Peek(s, pos) == '}') { pos++; return node; }

            while (true)
            {
                SkipWhitespace(s, ref pos);
                string key = ParseRawString(s, ref pos);
                SkipWhitespace(s, ref pos);
                Expect(s, ref pos, ':');
                StructuredNode value = ParseValue(s, ref pos, key);
                node.Children.Add(value);

                SkipWhitespace(s, ref pos);
                char next = Peek(s, pos);
                if (next == ',') { pos++; continue; }
                if (next == '}') { pos++; break; }
                throw new FormatException(string.Format("',' 또는 '}}' 예상 (위치 {0})", pos));
            }
            return node;
        }

        private static StructuredNode ParseArray(string s, ref int pos, string name)
        {
            Expect(s, ref pos, '[');
            var node = new StructuredNode(name, NodeKind.Array);
            SkipWhitespace(s, ref pos);
            if (Peek(s, pos) == ']') { pos++; return node; }

            while (true)
            {
                StructuredNode item = ParseValue(s, ref pos, null);
                node.Children.Add(item);

                SkipWhitespace(s, ref pos);
                char next = Peek(s, pos);
                if (next == ',') { pos++; continue; }
                if (next == ']') { pos++; break; }
                throw new FormatException(string.Format("',' 또는 ']' 예상 (위치 {0})", pos));
            }
            return node;
        }

        private static StructuredNode ParseStringNode(string s, ref int pos, string name)
        {
            string value = ParseRawString(s, ref pos);
            return NewScalar(name, value, "string");
        }

        private static StructuredNode ParseBooleanNode(string s, ref int pos, string name)
        {
            if (Match(s, pos, "true")) { pos += 4; return NewScalar(name, "true", "boolean"); }
            if (Match(s, pos, "false")) { pos += 5; return NewScalar(name, "false", "boolean"); }
            throw new FormatException("잘못된 boolean 리터럴 (위치 " + pos + ")");
        }

        private static StructuredNode ParseNullNode(string s, ref int pos, string name)
        {
            if (Match(s, pos, "null")) { pos += 4; return NewScalar(name, "null", "null"); }
            throw new FormatException("잘못된 null 리터럴 (위치 " + pos + ")");
        }

        private static StructuredNode ParseNumberNode(string s, ref int pos, string name)
        {
            int start = pos;
            if (Peek(s, pos) == '-') pos++;
            while (pos < s.Length && char.IsDigit(s[pos])) pos++;
            if (Peek(s, pos) == '.')
            {
                pos++;
                while (pos < s.Length && char.IsDigit(s[pos])) pos++;
            }
            if (Peek(s, pos) == 'e' || Peek(s, pos) == 'E')
            {
                pos++;
                if (Peek(s, pos) == '+' || Peek(s, pos) == '-') pos++;
                while (pos < s.Length && char.IsDigit(s[pos])) pos++;
            }
            if (pos == start) throw new FormatException("잘못된 숫자 리터럴 (위치 " + pos + ")");
            string numText = s.Substring(start, pos - start);
            return NewScalar(name, numText, "number");
        }

        private static StructuredNode NewScalar(string name, string value, string scalarKind)
        {
            var node = new StructuredNode(name, NodeKind.Value);
            node.Value = value;
            node.ScalarKind = scalarKind;
            return node;
        }

        private static string ParseRawString(string s, ref int pos)
        {
            Expect(s, ref pos, '"');
            var sb = new StringBuilder();
            while (true)
            {
                if (pos >= s.Length) throw new FormatException("문자열이 닫히지 않았습니다");
                char c = s[pos++];
                if (c == '"') break;

                if (c == '\\')
                {
                    if (pos >= s.Length) throw new FormatException("이스케이프 시퀀스가 불완전합니다");
                    char esc = s[pos++];
                    switch (esc)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (pos + 4 > s.Length) throw new FormatException("\\u 이스케이프가 불완전합니다");
                            string hex = s.Substring(pos, 4);
                            pos += 4;
                            sb.Append((char)Convert.ToInt32(hex, 16));
                            break;
                        default:
                            throw new FormatException("알 수 없는 이스케이프 '\\" + esc + "'");
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static void SkipWhitespace(string s, ref int pos)
        {
            while (pos < s.Length && char.IsWhiteSpace(s[pos])) pos++;
        }

        private static char Peek(string s, int pos)
        {
            return pos < s.Length ? s[pos] : '\0';
        }

        private static void Expect(string s, ref int pos, char c)
        {
            if (pos >= s.Length || s[pos] != c)
                throw new FormatException(string.Format("'{0}' 예상 (위치 {1})", c, pos));
            pos++;
        }

        private static bool Match(string s, int pos, string literal)
        {
            if (pos + literal.Length > s.Length) return false;
            return string.CompareOrdinal(s, pos, literal, 0, literal.Length) == 0;
        }
    }
}
