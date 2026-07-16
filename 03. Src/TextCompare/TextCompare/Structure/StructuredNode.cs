using System.Collections.Generic;

namespace TextCompare.Structure
{
    /// <summary>노드 종류. XML 엘리먼트/JSON 오브젝트는 Element, JSON 배열은 Array, 리프 값은 Value.</summary>
    public enum NodeKind
    {
        Element,
        Array,
        Value
    }

    /// <summary>
    /// XML과 JSON을 동일한 형태로 표현하는 공용 트리 노드.
    /// XML: Name=태그명, Attributes=속성(순서 보존), Children=자식 엘리먼트, Value=텍스트 콘텐츠(리프인 경우).
    /// JSON: Name=프로퍼티명(배열 항목은 부모가 부여), Attributes=사용 안 함, Children=오브젝트 프로퍼티/배열 항목, Value=스칼라 값의 원문 표현.
    /// </summary>
    public sealed class StructuredNode
    {
        public string Name;
        public NodeKind Kind;
        public List<KeyValuePair<string, string>> Attributes;
        public string Value;
        public List<StructuredNode> Children;

        /// <summary>JSON 파서가 채우는 스칼라 원본 타입("string"/"number"/"boolean"/"null"). XML에서는 사용 안 함(null).
        /// JsonPrettyPrinter가 문자열만 따옴표로 감싸는 등 재직렬화 시 타입을 보존하기 위해 필요.</summary>
        public string ScalarKind;

        public StructuredNode(string name, NodeKind kind)
        {
            Name = name;
            Kind = kind;
            Attributes = new List<KeyValuePair<string, string>>();
            Children = new List<StructuredNode>();
        }

        public bool IsLeaf
        {
            get { return Children.Count == 0; }
        }

        public string GetAttribute(string name)
        {
            foreach (var kv in Attributes)
            {
                if (kv.Key == name) return kv.Value;
            }
            return null;
        }

        /// <summary>자식 중 Name이 일치하는 리프(Value) 노드의 값을 찾는다. JSON 프로퍼티 키를 후보 키로 쓸 때 사용.</summary>
        public string GetChildValue(string childName)
        {
            foreach (var child in Children)
            {
                if (child.Name == childName && child.Kind == NodeKind.Value) return child.Value;
            }
            return null;
        }
    }
}
