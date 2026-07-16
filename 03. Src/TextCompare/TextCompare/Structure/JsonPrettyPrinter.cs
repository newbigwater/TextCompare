using System.Text;

namespace TextCompare.Structure
{
    /// <summary>StructuredNode를 JSON 문법으로 예쁘프린트한다(트레일링 콤마는 isLastSibling으로 생략).</summary>
    public sealed class JsonPrettyPrinter : IStructurePrettyPrinter
    {
        private const int IndentSize = 2;

        public string RenderOpenTag(StructuredNode node, int depth, bool isLastSibling)
        {
            string bracket = node.Kind == NodeKind.Array ? "[" : "{";
            return Indent(depth) + KeyPrefix(node) + bracket;
        }

        public string RenderCloseTag(StructuredNode node, int depth, bool isLastSibling)
        {
            string bracket = node.Kind == NodeKind.Array ? "]" : "}";
            return Indent(depth) + bracket + (isLastSibling ? string.Empty : ",");
        }

        public string RenderLeaf(StructuredNode node, int depth, bool isLastSibling)
        {
            return Indent(depth) + KeyPrefix(node) + RenderScalarValue(node) + (isLastSibling ? string.Empty : ",");
        }

        private static string Indent(int depth)
        {
            return new string(' ', depth * IndentSize);
        }

        private static string KeyPrefix(StructuredNode node)
        {
            return string.IsNullOrEmpty(node.Name) ? string.Empty : "\"" + EscapeJsonString(node.Name) + "\": ";
        }

        private static string RenderScalarValue(StructuredNode node)
        {
            switch (node.ScalarKind)
            {
                case "number":
                case "boolean":
                case "null":
                    return node.Value ?? "null";
                case "string":
                default:
                    return "\"" + EscapeJsonString(node.Value) + "\"";
            }
        }

        private static string EscapeJsonString(string s)
        {
            if (s == null) return string.Empty;
            var sb = new StringBuilder();
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
