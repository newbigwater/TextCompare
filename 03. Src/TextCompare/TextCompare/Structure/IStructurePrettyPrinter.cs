namespace TextCompare.Structure
{
    /// <summary>
    /// StructuredNode 트리를 실제 문법(XML/JSON)에 맞는 한 줄짜리 텍스트로 렌더링한다.
    /// StructureDiffer는 이 인터페이스만 알면 되므로 매칭/비교 로직과 문법별 표현이 완전히 분리된다.
    /// isLastSibling은 JSON의 트레일링 콤마 생략에 쓰이며 XML 구현체는 무시한다.
    /// </summary>
    public interface IStructurePrettyPrinter
    {
        /// <summary>자식이 있는 노드(엘리먼트/배열)의 여는 줄. 예: "&lt;Message name=\"...\"&gt;", "\"Body\": {"</summary>
        string RenderOpenTag(StructuredNode node, int depth, bool isLastSibling);

        /// <summary>자식이 있는 노드의 닫는 줄. 예: "&lt;/Message&gt;", "},"</summary>
        string RenderCloseTag(StructuredNode node, int depth, bool isLastSibling);

        /// <summary>리프 노드(값만 있는 노드) 한 줄 전체. 예: "&lt;Field name=\"COMMAND\" /&gt;", "\"key\": \"value\","</summary>
        string RenderLeaf(StructuredNode node, int depth, bool isLastSibling);
    }
}
