using System.Text;

namespace TextCompare.Structure
{
    /// <summary>StructuredNode를 XML 문법으로 예쁘프린트한다.</summary>
    public sealed class XmlPrettyPrinter : IStructurePrettyPrinter
    {
        private const int IndentSize = 2;

        public string RenderOpenTag(StructuredNode node, int depth, bool isLastSibling)
        {
            return Indent(depth) + "<" + node.Name + RenderAttributes(node) + ">";
        }

        public string RenderCloseTag(StructuredNode node, int depth, bool isLastSibling)
        {
            return Indent(depth) + "</" + node.Name + ">";
        }

        public string RenderLeaf(StructuredNode node, int depth, bool isLastSibling)
        {
            string attrs = RenderAttributes(node);
            if (string.IsNullOrEmpty(node.Value))
            {
                return Indent(depth) + "<" + node.Name + attrs + " />";
            }
            return Indent(depth) + "<" + node.Name + attrs + ">" + EscapeXmlText(node.Value) + "</" + node.Name + ">";
        }

        private static string Indent(int depth)
        {
            return new string(' ', depth * IndentSize);
        }

        private static string RenderAttributes(StructuredNode node)
        {
            if (node.Attributes.Count == 0) return string.Empty;
            var sb = new StringBuilder();
            foreach (var attr in node.Attributes)
            {
                sb.Append(' ').Append(attr.Key).Append("=\"").Append(EscapeXmlAttr(attr.Value)).Append('"');
            }
            return sb.ToString();
        }

        private static string EscapeXmlAttr(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private static string EscapeXmlText(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }
    }
}
