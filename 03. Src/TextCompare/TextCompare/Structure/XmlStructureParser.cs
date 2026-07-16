using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace TextCompare.Structure
{
    /// <summary>
    /// .NET 내장 XDocument(System.Xml.Linq)로 XML을 파싱해 StructuredNode 트리로 변환한다. NuGet 불필요.
    /// 파일 로드는 XDocument.Load(path)를 사용해 XML 선언의 인코딩을 직접 신뢰한다(EncodingDetector의 휴리스틱보다 신뢰도가 높음).
    /// </summary>
    public static class XmlStructureParser
    {
        public static StructuredNode ParseFile(string path)
        {
            XDocument doc = XDocument.Load(path);
            return ConvertElement(doc.Root);
        }

        public static StructuredNode Parse(string xmlText)
        {
            XDocument doc = XDocument.Parse(xmlText);
            return ConvertElement(doc.Root);
        }

        private static StructuredNode ConvertElement(XElement element)
        {
            var node = new StructuredNode(element.Name.LocalName, NodeKind.Element);

            foreach (XAttribute attr in element.Attributes())
            {
                if (attr.IsNamespaceDeclaration) continue;
                node.Attributes.Add(new KeyValuePair<string, string>(attr.Name.LocalName, attr.Value));
            }

            List<XElement> childElements = element.Elements().ToList();
            if (childElements.Count > 0)
            {
                foreach (XElement child in childElements)
                {
                    node.Children.Add(ConvertElement(child));
                }
            }
            else
            {
                string text = element.Value;
                if (!string.IsNullOrEmpty(text))
                {
                    node.Value = text;
                }
            }

            return node;
        }
    }
}
