namespace TextCompare.Structure
{
    /// <summary>StructuredNode 트리의 깊은 동등성 비교. 자연 키가 없는 형제 목록을 위치 기반(LCS)으로
    /// 매칭할 때 MyersDiff&lt;StructuredNode&gt;의 비교자로 사용된다.</summary>
    public static class StructureEquality
    {
        public static bool DeepEquals(StructuredNode a, StructuredNode b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            if (a.Kind != b.Kind) return false;
            if (a.Name != b.Name) return false;
            if (a.Value != b.Value) return false;
            if (a.ScalarKind != b.ScalarKind) return false;

            if (a.Attributes.Count != b.Attributes.Count) return false;
            for (int i = 0; i < a.Attributes.Count; i++)
            {
                if (a.Attributes[i].Key != b.Attributes[i].Key || a.Attributes[i].Value != b.Attributes[i].Value)
                    return false;
            }

            if (a.Children.Count != b.Children.Count) return false;
            for (int i = 0; i < a.Children.Count; i++)
            {
                if (!DeepEquals(a.Children[i], b.Children[i])) return false;
            }

            return true;
        }

        public static int DeepHashCode(StructuredNode node)
        {
            if (node == null) return 0;
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (node.Name != null ? node.Name.GetHashCode() : 0);
                hash = hash * 31 + (int)node.Kind;
                hash = hash * 31 + (node.Value != null ? node.Value.GetHashCode() : 0);
                foreach (var attr in node.Attributes)
                {
                    hash = hash * 31 + attr.Key.GetHashCode();
                    hash = hash * 31 + (attr.Value != null ? attr.Value.GetHashCode() : 0);
                }
                foreach (StructuredNode child in node.Children)
                {
                    hash = hash * 31 + DeepHashCode(child);
                }
                return hash;
            }
        }
    }
}
