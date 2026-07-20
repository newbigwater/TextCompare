using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TextCompare.Alignment;
using TextCompare.Cli;
using TextCompare.Core;
using TextCompare.Git;
using TextCompare.Document;
using TextCompare.Intraline;
using TextCompare.IO;
using TextCompare.Structure;

namespace TextCompare.SelfTest
{
    internal static class Program
    {
        private static int _failures;

        private static int Main()
        {
            RunCoreTests();
            RunAlignmentTests();
            RunIntralineAndDocumentTests();
            RunEncodingTests();
            RunStructureParsingTests();
            RunNodeKeyStrategyTests();
            RunKeyedListMatcherTests();
            RunPrettyPrinterTests();
            RunStructureDifferEndToEndTests();
            RunExcludeFilterTests();
            RunMaskFilterTests();
            RunCommandLineTests();
            RunGitPathTests();

            Console.WriteLine();
            if (_failures == 0)
            {
                Console.WriteLine("ALL TESTS PASSED");
                return 0;
            }

            Console.WriteLine(_failures + " TEST(S) FAILED");
            return 1;
        }

        private static void RunCoreTests()
        {
            // 사용자가 겪던 핵심 문제: A,B,C vs A,C (B 삭제)
            Test("MyersDiff: A,B,C vs A,C (삭제)", () =>
            {
                var a = new List<string> { "A", "B", "C" };
                var b = new List<string> { "A", "C" };
                var changes = new MyersDiff<string>(a, b).Compute();

                AssertEqual(1, changes.Count, "변경 블록 개수");
                AssertEqual(new DiffChange(1, 1, 1, 0).ToString(), changes[0].ToString(), "블록 내용");
                Assert(changes[0].IsDelete, "삭제 블록이어야 함");
            });

            Test("MyersDiff: 완전 동일", () =>
            {
                var a = new List<string> { "A", "B", "C" };
                var b = new List<string> { "A", "B", "C" };
                var changes = new MyersDiff<string>(a, b).Compute();
                AssertEqual(0, changes.Count, "변경 없음");
            });

            Test("MyersDiff: 순수 삽입", () =>
            {
                var a = new List<string> { "A", "C" };
                var b = new List<string> { "A", "B", "C" };
                var changes = new MyersDiff<string>(a, b).Compute();
                AssertEqual(1, changes.Count, "변경 블록 개수");
                Assert(changes[0].IsInsert, "삽입 블록이어야 함");
                AssertEqual(1, changes[0].CountB, "삽입된 라인 수");
            });

            Test("MyersDiff: 치환(변경)", () =>
            {
                var a = new List<string> { "A", "B", "C" };
                var b = new List<string> { "A", "X", "C" };
                var changes = new MyersDiff<string>(a, b).Compute();
                AssertEqual(1, changes.Count, "변경 블록 개수");
                Assert(changes[0].IsReplace, "치환 블록이어야 함");
            });

            Test("MyersDiff: 앞뒤 여러 개 삭제/삽입 혼재", () =>
            {
                var a = new List<string> { "1", "2", "3", "4", "5" };
                var b = new List<string> { "1", "3", "5", "6" };
                var changes = new MyersDiff<string>(a, b).Compute();
                // 기대: "2" 삭제, "4"->없음(삭제), "6" 삽입 (형태는 여러가지로 coalesce 될 수 있음)
                int totalDeleted = 0, totalInserted = 0;
                foreach (var c in changes)
                {
                    totalDeleted += c.CountA;
                    totalInserted += c.CountB;
                }
                // A: 1,2,3,4,5 / B: 1,3,5,6 -> LCS = 1,3,5 (길이3) => 삭제 2, 삽입 1
                AssertEqual(2, totalDeleted, "총 삭제 라인 수");
                AssertEqual(1, totalInserted, "총 삽입 라인 수");
            });

            Test("MyersDiff: 빈 시퀀스 vs 빈 시퀀스", () =>
            {
                var a = new List<string>();
                var b = new List<string>();
                var changes = new MyersDiff<string>(a, b).Compute();
                AssertEqual(0, changes.Count, "빈 vs 빈은 변경 없음");
            });

            Test("MyersDiff: 빈 시퀀스 vs 3줄", () =>
            {
                var a = new List<string>();
                var b = new List<string> { "A", "B", "C" };
                var changes = new MyersDiff<string>(a, b).Compute();
                AssertEqual(1, changes.Count, "변경 블록 개수");
                AssertEqual(3, changes[0].CountB, "전체 삽입");
            });

            Test("LineHasher: 공백 무시 옵션", () =>
            {
                var opts = new DiffOptions { IgnoreWhitespace = true };
                var hasher = new LineHasher(opts);
                Assert(hasher.Equals("a  b", "a b"), "공백 축약 후 동일해야 함");
            });

            Test("LineHasher: 대소문자 무시 옵션", () =>
            {
                var opts = new DiffOptions { IgnoreCase = true };
                var hasher = new LineHasher(opts);
                Assert(hasher.Equals("Hello", "hello"), "대소문자 무시 시 동일해야 함");
            });

            Test("MyersDiff + LineHasher: 옵션 적용 비교", () =>
            {
                var opts = new DiffOptions { IgnoreCase = true, IgnoreWhitespace = true };
                var hasher = new LineHasher(opts);
                var a = new List<string> { "Hello World", "Foo" };
                var b = new List<string> { "hello   world", "Bar" };
                var changes = new MyersDiff<string>(a, b, hasher).Compute();
                AssertEqual(1, changes.Count, "옵션 적용 후 1번째 줄은 동일 취급");
                Assert(changes[0].IsReplace, "두번째 줄만 변경");
            });
        }

        private static void RunAlignmentTests()
        {
            Test("DocumentAligner: 사용자 시나리오 A,B,C vs A,C -> ghost 정렬", () =>
            {
                var left = new List<string> { "A", "B", "C" };
                var right = new List<string> { "A", "C" };
                var changes = new MyersDiff<string>(left, right).Compute();
                var rows = DocumentAligner.Align(left, right, changes);

                AssertEqual(3, rows.Count, "정렬 후 행 개수는 좌측 라인 수와 같아야 함 (1:1 정렬)");

                AssertEqual(RowKind.Same, rows[0].Kind, "0행: Same");
                AssertEqual("A", rows[0].LeftText, "0행 좌측");
                AssertEqual("A", rows[0].RightText, "0행 우측");

                AssertEqual(RowKind.LeftOnly, rows[1].Kind, "1행: LeftOnly (B 삭제)");
                AssertEqual("B", rows[1].LeftText, "1행 좌측 텍스트");
                Assert(rows[1].IsRightGhost, "1행 우측은 ghost여야 함");
                Assert(!rows[1].RightLineNo.HasValue, "1행 우측 라인번호 없음");

                AssertEqual(RowKind.Same, rows[2].Kind, "2행: Same (C)");
                AssertEqual("C", rows[2].LeftText, "2행 좌측");
                AssertEqual("C", rows[2].RightText, "2행 우측");
                AssertEqual(2, rows[2].RightLineNo.Value, "C의 우측 실제 라인번호는 2 (밀리지 않음)");
            });

            Test("DocumentAligner: 순수 삽입 -> 좌측 ghost", () =>
            {
                var left = new List<string> { "A", "C" };
                var right = new List<string> { "A", "B", "C" };
                var changes = new MyersDiff<string>(left, right).Compute();
                var rows = DocumentAligner.Align(left, right, changes);

                AssertEqual(3, rows.Count, "정렬 후 행 개수는 우측 라인 수와 같아야 함");
                AssertEqual(RowKind.RightOnly, rows[1].Kind, "1행: RightOnly (B 삽입)");
                Assert(rows[1].IsLeftGhost, "1행 좌측은 ghost여야 함");
                AssertEqual("B", rows[1].RightText, "1행 우측 텍스트");
            });

            Test("DocumentAligner: 치환 쌍은 Changed로 정렬", () =>
            {
                var left = new List<string> { "A", "B", "C" };
                var right = new List<string> { "A", "X", "C" };
                var changes = new MyersDiff<string>(left, right).Compute();
                var rows = DocumentAligner.Align(left, right, changes);

                AssertEqual(3, rows.Count, "행 개수");
                AssertEqual(RowKind.Changed, rows[1].Kind, "1행: Changed");
                AssertEqual("B", rows[1].LeftText, "좌측 원문 유지");
                AssertEqual("X", rows[1].RightText, "우측 원문 유지");
                Assert(!rows[1].IsLeftGhost && !rows[1].IsRightGhost, "Changed는 양쪽 모두 실라인");
            });
        }

        private static void RunIntralineAndDocumentTests()
        {
            Test("IntralineDiffer: 변경된 부분만 강조", () =>
            {
                var spans = IntralineDiffer.ComputeRightSpans("Hello World", "Hello Brave");
                bool anyDifferent = false;
                foreach (var s in spans)
                {
                    if (s.IsDifferent) anyDifferent = true;
                }
                Assert(anyDifferent, "일부 구간은 다른 것으로 표시되어야 함");
            });

            Test("DiffDocument: 통계 및 블록 계산", () =>
            {
                var left = new List<string> { "A", "B", "C", "D" };
                var right = new List<string> { "A", "X", "D" };
                var doc = DiffDocument.Build(left, right, DiffOptions.Default);

                Assert(doc.TotalDiffCount >= 1, "최소 1개의 diff 블록 존재");
                Assert(doc.ChangedLineCount + doc.LeftOnlyLineCount + doc.RightOnlyLineCount > 0, "변경 통계 존재");
                AssertEqual(4, doc.Rows.Count, "행 수는 max(좌,우) 이상 - 이 케이스는 4");
            });

            Test("DiffDocument: 완전 동일 파일은 diff 0개", () =>
            {
                var left = new List<string> { "A", "B" };
                var right = new List<string> { "A", "B" };
                var doc = DiffDocument.Build(left, right, DiffOptions.Default);
                AssertEqual(0, doc.TotalDiffCount, "동일 파일은 diff 없음");
            });

            Test("LineSplitter: 트레일링 개행은 빈 줄로 세지 않음", () =>
            {
                var lines = LineSplitter.Split("A\nB\n");
                AssertEqual(2, lines.Count, "마지막 개행 뒤 빈 줄은 제외");
                AssertEqual("B", lines[1], "마지막 실제 줄");
            });

            Test("LineSplitter: 개행 없는 마지막 줄도 포함", () =>
            {
                var lines = LineSplitter.Split("A\nB");
                AssertEqual(2, lines.Count, "개행 없어도 마지막 줄 포함");
            });

            Test("LineSplitter: CRLF/CR 모두 정규화", () =>
            {
                var lines = LineSplitter.Split("A\r\nB\rC");
                AssertEqual(3, lines.Count, "CRLF와 CR 모두 줄바꿈으로 처리");
            });
        }

        private static void RunEncodingTests()
        {
            Test("EncodingDetector: UTF-8 BOM 감지", () =>
            {
                var utf8Bom = new byte[] { 0xEF, 0xBB, 0xBF, 0x41, 0x42 };
                var enc = EncodingDetector.Detect(utf8Bom);
                Assert(enc is UTF8Encoding, "UTF-8 BOM은 UTF8Encoding으로 감지되어야 함");
            });

            Test("EncodingDetector: BOM 없는 UTF-8 한글", () =>
            {
                byte[] bytes = new UTF8Encoding(false).GetBytes("안녕하세요");
                var enc = EncodingDetector.Detect(bytes);
                Assert(enc is UTF8Encoding, "유효한 UTF-8 바이트는 UTF-8로 감지되어야 함");
            });

            Test("EncodingDetector: CP949(EUC-KR) 한글 폴백", () =>
            {
                Encoding cp949 = Encoding.GetEncoding(949);
                byte[] bytes = cp949.GetBytes("안녕하세요");
                var enc = EncodingDetector.Detect(bytes);
                AssertEqual(949, enc.CodePage, "UTF-8로 디코딩 불가능한 한글 바이트는 CP949로 폴백해야 함");
            });

            Test("EncodingDetector: ReadAllText 왕복 검증", () =>
            {
                string tempFile = System.IO.Path.GetTempFileName();
                try
                {
                    System.IO.File.WriteAllText(tempFile, "가나다\n라마바", new UTF8Encoding(false));
                    Encoding detected;
                    string text = EncodingDetector.ReadAllText(tempFile, out detected);
                    Assert(text.Contains("가나다"), "UTF-8 파일 내용을 올바르게 읽어야 함");
                }
                finally
                {
                    System.IO.File.Delete(tempFile);
                }
            });
        }

        private static void RunStructureParsingTests()
        {
            Test("XmlStructureParser: 속성/자식/리프 파싱", () =>
            {
                StructuredNode root = XmlStructureParser.Parse("<Root a=\"1\"><Child x=\"2\" /><Child2>text</Child2></Root>");

                AssertEqual("Root", root.Name, "루트 태그명");
                AssertEqual("1", root.GetAttribute("a"), "루트 속성 a");
                AssertEqual(2, root.Children.Count, "자식 개수");

                StructuredNode child = root.Children[0];
                AssertEqual("Child", child.Name, "첫 자식 태그명");
                AssertEqual("2", child.GetAttribute("x"), "첫 자식 속성 x");
                Assert(child.IsLeaf, "Child는 자식이 없는 리프");

                StructuredNode child2 = root.Children[1];
                AssertEqual("text", child2.Value, "Child2의 텍스트 값");
            });

            Test("JsonStructureParser: 오브젝트/배열/스칼라 파싱", () =>
            {
                StructuredNode root = JsonStructureParser.Parse("{\"a\": 1, \"b\": [1,2,3], \"c\": {\"d\": \"e\"}, \"f\": true, \"g\": null}");

                AssertEqual(NodeKind.Element, root.Kind, "루트는 오브젝트");
                AssertEqual(5, root.Children.Count, "프로퍼티 5개");

                StructuredNode a = root.Children[0];
                AssertEqual("a", a.Name, "첫 프로퍼티명");
                AssertEqual("1", a.Value, "숫자 값 원문");
                AssertEqual("number", a.ScalarKind, "숫자 타입");

                StructuredNode b = root.Children[1];
                AssertEqual(NodeKind.Array, b.Kind, "b는 배열");
                AssertEqual(3, b.Children.Count, "배열 항목 3개");
                Assert(string.IsNullOrEmpty(b.Children[0].Name), "배열 항목은 Name이 없음");

                StructuredNode c = root.Children[2];
                AssertEqual("e", c.GetChildValue("d"), "중첩 오브젝트 프로퍼티 d");

                AssertEqual("true", root.Children[3].Value, "boolean 값 원문");
                AssertEqual("boolean", root.Children[3].ScalarKind, "boolean 타입");
                AssertEqual("null", root.Children[4].ScalarKind, "null 타입");
            });

            Test("JsonStructureParser: 이스케이프 문자열 디코딩", () =>
            {
                StructuredNode root = JsonStructureParser.Parse("{\"s\": \"line1\\nline2 \\\"q\\\" \\uAC00\"}");
                string decoded = root.Children[0].Value;
                Assert(decoded.Contains("\n"), "\\n이 실제 개행으로 디코딩됨");
                Assert(decoded.Contains("\"q\""), "이스케이프된 따옴표 디코딩");
                Assert(decoded.Contains("가"), "\\u 유니코드 이스케이프 디코딩");
            });
        }

        private static void RunNodeKeyStrategyTests()
        {
            Test("NodeKeyStrategy: id가 버전간 재사용돼도 name을 키로 채택", () =>
            {
                // 사용자 실제 사례 축소판: id는 재사용되지만 name은 안정적임
                var left = new List<StructuredNode> { MakeMessage("Common", "IF_001"), MakeMessage("Moved", "IF_003") };
                var right = new List<StructuredNode> { MakeMessage("Common", "IF_001"), MakeMessage("Moved", "IF_002") };

                Func<StructuredNode, string> selector = NodeKeyStrategy.ResolveKeySelector(left, right);
                Assert(selector != null, "키가 발견되어야 함");
                AssertEqual("Moved", selector(left[1]), "선택된 키는 id가 아니라 name 값이어야 함");
                AssertEqual(selector(left[1]), selector(right[1]), "좌우 Moved 노드의 키가 같아야 매칭됨");
            });

            Test("NodeKeyStrategy: 형제 태그명이 서로 다르면 태그명 자체를 키로 채택", () =>
            {
                var left = new List<StructuredNode> { new StructuredNode("Header", NodeKind.Element), new StructuredNode("Body", NodeKind.Element) };
                var right = new List<StructuredNode> { new StructuredNode("Header", NodeKind.Element), new StructuredNode("Body", NodeKind.Element) };

                Func<StructuredNode, string> selector = NodeKeyStrategy.ResolveKeySelector(left, right);
                Assert(selector != null, "키가 발견되어야 함");
                AssertEqual("Header", selector(left[0]), "태그명 자체가 키");
            });

            Test("NodeKeyStrategy: 자연 키가 전혀 없으면 null 반환", () =>
            {
                var left = new List<StructuredNode> { NewScalarNode(null, "1"), NewScalarNode(null, "2") };
                var right = new List<StructuredNode> { NewScalarNode(null, "1"), NewScalarNode(null, "2") };

                Func<StructuredNode, string> selector = NodeKeyStrategy.ResolveKeySelector(left, right);
                Assert(selector == null, "키 없는 스칼라 배열은 null을 반환해 positional 폴백으로 가야 함");
            });
        }

        private static void RunKeyedListMatcherTests()
        {
            Test("KeyedListMatcher.MatchByKey: 삭제/추가/이동+id재사용 시나리오 (실제 사례 축소판)", () =>
            {
                var left = new List<StructuredNode>
                {
                    MakeMessage("Common", "IF_001"),
                    MakeMessage("ToDelete", "IF_002"),
                    MakeMessage("Moved", "IF_003")
                };
                var right = new List<StructuredNode>
                {
                    MakeMessage("Common", "IF_001"),
                    MakeMessage("Moved", "IF_002"),
                    MakeMessage("ToAdd", "IF_004")
                };

                Func<StructuredNode, string> selector = NodeKeyStrategy.ResolveKeySelector(left, right);
                List<ChildMatch> matches = KeyedListMatcher.MatchByKey(left, right, selector);

                int matchedCount = matches.Count(m => m.Kind == ChildMatchKind.Matched);
                int leftOnlyCount = matches.Count(m => m.Kind == ChildMatchKind.LeftOnly);
                int rightOnlyCount = matches.Count(m => m.Kind == ChildMatchKind.RightOnly);

                AssertEqual(2, matchedCount, "Common, Moved 2개가 매칭돼야 함");
                AssertEqual(1, leftOnlyCount, "ToDelete 1개만 LeftOnly");
                AssertEqual(1, rightOnlyCount, "ToAdd 1개만 RightOnly");

                ChildMatch movedMatch = matches.First(m => m.Kind == ChildMatchKind.Matched && m.Left.GetAttribute("name") == "Moved");
                AssertEqual("IF_003", movedMatch.Left.GetAttribute("id"), "Moved 좌측 id는 IF_003");
                AssertEqual("IF_002", movedMatch.Right.GetAttribute("id"), "Moved 우측 id는 IF_002 (재사용됐지만 name으로 정확히 매칭)");

                ChildMatch deleteMatch = matches.First(m => m.Kind == ChildMatchKind.LeftOnly);
                AssertEqual("ToDelete", deleteMatch.Left.GetAttribute("name"), "삭제된 건 ToDelete여야 함(Moved와 혼동되면 안됨)");
            });

            Test("KeyedListMatcher.MatchPositional: 키 없는 스칼라 목록의 LCS 매칭", () =>
            {
                var left = new List<StructuredNode> { NewScalarNode(null, "a"), NewScalarNode(null, "b"), NewScalarNode(null, "c") };
                var right = new List<StructuredNode> { NewScalarNode(null, "a"), NewScalarNode(null, "c") };

                List<ChildMatch> matches = KeyedListMatcher.MatchPositional(left, right);

                int leftOnlyCount = matches.Count(m => m.Kind == ChildMatchKind.LeftOnly);
                AssertEqual(1, leftOnlyCount, "'b'만 삭제로 판정돼야 함");
                Assert(matches.Any(m => m.Kind == ChildMatchKind.LeftOnly && m.Left.Value == "b"), "삭제된 값은 b");
            });
        }

        private static void RunPrettyPrinterTests()
        {
            Test("XmlPrettyPrinter: 여는태그/리프 렌더링", () =>
            {
                var printer = new XmlPrettyPrinter();
                StructuredNode msg = MakeMessage("Common", "IF_001");
                string openLine = printer.RenderOpenTag(msg, 1, true);
                Assert(openLine.Contains("<Message"), "태그명 포함");
                Assert(openLine.Contains("name=\"Common\""), "name 속성 포함");
                Assert(openLine.Contains("id=\"IF_001\""), "id 속성 포함");

                StructuredNode leaf = new StructuredNode("Field", NodeKind.Element);
                leaf.Attributes.Add(new KeyValuePair<string, string>("name", "COMMAND"));
                string leafLine = printer.RenderLeaf(leaf, 2, true);
                AssertEqual("    <Field name=\"COMMAND\" />", leafLine, "자기닫힘 태그 렌더링");
            });

            Test("JsonPrettyPrinter: 콤마는 마지막 항목에서만 생략", () =>
            {
                var printer = new JsonPrettyPrinter();
                StructuredNode value = NewScalarNode("key", "v");

                string notLast = printer.RenderLeaf(value, 1, false);
                string isLast = printer.RenderLeaf(value, 1, true);

                Assert(notLast.EndsWith(","), "마지막이 아니면 콤마 있음");
                Assert(!isLast.EndsWith(","), "마지막이면 콤마 없음");
                Assert(notLast.Contains("\"key\": \"v\""), "키:값 형태로 렌더링");
            });

            Test("JsonPrettyPrinter: 배열/오브젝트 괄호", () =>
            {
                var printer = new JsonPrettyPrinter();
                var arr = new StructuredNode("items", NodeKind.Array);
                Assert(printer.RenderOpenTag(arr, 0, true).EndsWith("["), "배열은 대괄호로 시작");
                Assert(printer.RenderCloseTag(arr, 0, true).TrimEnd() == "]", "배열은 대괄호로 닫힘, 마지막이면 콤마 없음");
                Assert(printer.RenderCloseTag(arr, 0, false).TrimEnd() == "],", "마지막이 아니면 콤마 있음");
            });
        }

        private static void RunStructureDifferEndToEndTests()
        {
            Test("StructureDiffer: 실제 사례 축소판 end-to-end (삭제/추가/id재사용 매칭)", () =>
            {
                string leftXml =
                    "<InterfaceConfig>" +
                    "<Message name=\"Common\" id=\"IF_001\"><Header><Field name=\"COMMAND\" type=\"string\" /></Header></Message>" +
                    "<Message name=\"ToDelete\" id=\"IF_002\"><Header><Field name=\"COMMAND\" type=\"string\" /></Header></Message>" +
                    "<Message name=\"Moved\" id=\"IF_003\"><Header><Field name=\"COMMAND\" type=\"string\" /></Header></Message>" +
                    "</InterfaceConfig>";

                string rightXml =
                    "<InterfaceConfig>" +
                    "<Message name=\"Common\" id=\"IF_001\"><Header><Field name=\"COMMAND\" type=\"string\" /></Header></Message>" +
                    "<Message name=\"Moved\" id=\"IF_002\"><Header><Field name=\"COMMAND\" type=\"string\" /></Header></Message>" +
                    "<Message name=\"ToAdd\" id=\"IF_004\"><Header><Field name=\"COMMAND\" type=\"string\" /></Header></Message>" +
                    "</InterfaceConfig>";

                StructuredNode left = XmlStructureParser.Parse(leftXml);
                StructuredNode right = XmlStructureParser.Parse(rightXml);

                List<AlignedRow> rows = StructureDiffer.Diff(left, right, new XmlPrettyPrinter(), DiffOptions.Default);
                DiffDocument doc = DiffDocument.FromRows(rows);

                AssertEqual(3, doc.TotalDiffCount, "차이 3개: ToDelete 블록, Moved id변경 블록, ToAdd 블록");

                bool anyRowMentionsCommon = rows.Any(r =>
                    (r.LeftText != null && r.LeftText.Contains("Common") && r.Kind != RowKind.Same) ||
                    (r.RightText != null && r.RightText.Contains("Common") && r.Kind != RowKind.Same));
                Assert(!anyRowMentionsCommon, "Common 메시지는 완전히 동일하므로 diff에 걸리면 안 됨");

                AlignedRow movedRow = rows.First(r => r.Kind == RowKind.Changed && r.LeftText != null && r.LeftText.Contains("Moved"));
                Assert(movedRow.LeftText.Contains("IF_003"), "Moved 좌측 id는 IF_003");
                Assert(movedRow.RightText.Contains("IF_002"), "Moved 우측 id는 IF_002");
                Assert(movedRow.RightText.Contains("Moved"), "id가 재사용됐어도 우측도 Moved로 정확히 매칭되어야 함(정합성 비교 핵심)");

                Assert(rows.Any(r => r.Kind == RowKind.LeftOnly && r.LeftText != null && r.LeftText.Contains("ToDelete")),
                    "ToDelete가 LeftOnly로 표시되어야 함");
                Assert(rows.Any(r => r.Kind == RowKind.RightOnly && r.RightText != null && r.RightText.Contains("ToAdd")),
                    "ToAdd가 RightOnly로 표시되어야 함");
                Assert(!rows.Any(r => r.Kind == RowKind.LeftOnly && r.LeftText != null && r.LeftText.Contains("Moved")),
                    "Moved가 삭제로 오판되면 안 됨(정합성 비교 실패 재현 방지 확인)");
            });

            Test("StructureDiffer: JSON 배열 재정렬/증감도 name 없이 값 기준으로 매칭", () =>
            {
                string leftJson = "{\"PARALIST\": [{\"PARAMETERID\": \"P1\", \"VALUE\": \"1\"}, {\"PARAMETERID\": \"P2\", \"VALUE\": \"2\"}]}";
                string rightJson = "{\"PARALIST\": [{\"PARAMETERID\": \"P1\", \"VALUE\": \"1\"}, {\"PARAMETERID\": \"P3\", \"VALUE\": \"3\"}]}";

                StructuredNode left = JsonStructureParser.Parse(leftJson);
                StructuredNode right = JsonStructureParser.Parse(rightJson);

                List<AlignedRow> rows = StructureDiffer.Diff(left, right, new JsonPrettyPrinter(), DiffOptions.Default);
                DiffDocument doc = DiffDocument.FromRows(rows);

                Assert(doc.TotalDiffCount >= 1, "P2->P3 변경으로 최소 1개 이상의 차이가 있어야 함");
                Assert(rows.Any(r => r.LeftText != null && r.LeftText.Contains("P1")) &&
                       rows.Any(r => r.RightText != null && r.RightText.Contains("P1")),
                    "P1 항목은 양쪽 모두 존재해야 함(공통 항목 보존 확인)");
            });
        }

        private static void RunExcludeFilterTests()
        {
            Test("ExcludeFilterSet: 패턴에 일치하는 줄은 좌/우에서 완전히 제거된다", () =>
            {
                var left = new List<string> { "# 2024-01-01", "A", "B" };
                var right = new List<string> { "# 2024-01-02", "A", "X" };
                var filters = new ExcludeFilterSet { Enabled = true };
                filters.Patterns.Add(new ExcludeFilterPattern("^#"));

                var doc = DiffDocument.Build(left, right, new DiffOptions { ExcludeFilters = filters });

                Assert(!doc.Rows.Any(r =>
                    (r.LeftText != null && r.LeftText.StartsWith("#")) ||
                    (r.RightText != null && r.RightText.StartsWith("#"))),
                    "제외 패턴에 일치하는 줄은 결과에 전혀 나타나면 안 됨");
                AssertEqual(2, doc.Rows.Count, "제외된 줄을 뺀 나머지(A,B / A,X)만 남아야 함");
            });

            Test("ExcludeFilterSet: 매칭 안 되는 패턴은 기존 동작과 동일하다", () =>
            {
                var left = new List<string> { "A", "B" };
                var right = new List<string> { "A", "X" };
                var filters = new ExcludeFilterSet { Enabled = true };
                filters.Patterns.Add(new ExcludeFilterPattern("ZZZ_NO_MATCH"));

                var withFilter = DiffDocument.Build(left, right, new DiffOptions { ExcludeFilters = filters });
                var withoutFilter = DiffDocument.Build(left, right, DiffOptions.Default);

                AssertEqual(withoutFilter.Rows.Count, withFilter.Rows.Count, "행 개수 동일");
                AssertEqual(withoutFilter.TotalDiffCount, withFilter.TotalDiffCount, "diff 개수 동일");
            });

            Test("ExcludeFilterPattern: 잘못된 정규식은 예외 없이 무시된다", () =>
            {
                var pattern = new ExcludeFilterPattern("(unclosed");
                Assert(!pattern.IsValid, "잘못된 정규식은 IsValid=false");
                Assert(!string.IsNullOrEmpty(pattern.ErrorMessage), "에러 메시지가 있어야 함");
                Assert(!pattern.IsMatch("anything"), "무효 패턴의 IsMatch는 항상 false, 예외를 던지면 안 됨");
            });

            Test("ExcludeFilterSet: Enabled=false면 패턴이 있어도 제외되지 않는다", () =>
            {
                var left = new List<string> { "# comment", "A" };
                var right = new List<string> { "# comment", "A" };
                var filters = new ExcludeFilterSet { Enabled = false };
                filters.Patterns.Add(new ExcludeFilterPattern("^#"));

                var doc = DiffDocument.Build(left, right, new DiffOptions { ExcludeFilters = filters });
                AssertEqual(2, doc.Rows.Count, "필터 비활성화 시 원본 그대로 2줄 모두 남아야 함");
            });

            Test("ExcludeFilterSet + IgnoreCase 옵션은 서로 독립적이다", () =>
            {
                var left = new List<string> { "HELLO", "A" };
                var right = new List<string> { "hello", "A" };
                var filters = new ExcludeFilterSet { Enabled = true };
                filters.Patterns.Add(new ExcludeFilterPattern("^ZZZ_NEVER_MATCH$")); // HELLO/hello와 무관한 패턴

                var doc = DiffDocument.Build(left, right, new DiffOptions { IgnoreCase = true, ExcludeFilters = filters });

                AssertEqual(2, doc.Rows.Count, "HELLO/hello 줄은 제외 패턴과 무관하게 살아남아야 함");
                AssertEqual(0, doc.TotalDiffCount, "IgnoreCase 덕분에 HELLO/hello는 동일 취급되어야 함(제외 필터가 이 옵션을 방해하면 안 됨)");
            });

            Test("DocumentAligner: 필터로 제거된 줄 주변에서도 ghost 정렬이 정상 동작한다", () =>
            {
                var left = new List<string> { "A", "#c", "B", "C" };
                var right = new List<string> { "A", "B", "X" };
                var filters = new ExcludeFilterSet { Enabled = true };
                filters.Patterns.Add(new ExcludeFilterPattern("^#"));

                var filtered = DiffDocument.Build(left, right, new DiffOptions { ExcludeFilters = filters });
                var reference = DiffDocument.Build(new List<string> { "A", "B", "C" }, right, DiffOptions.Default);

                AssertEqual(reference.Rows.Count, filtered.Rows.Count, "필터 적용 결과는 제외 줄이 애초에 없었던 것과 같은 행 수를 가져야 함");
                for (int i = 0; i < reference.Rows.Count; i++)
                {
                    AssertEqual(reference.Rows[i].Kind, filtered.Rows[i].Kind, "행 " + i + " Kind 일치");
                    AssertEqual(reference.Rows[i].LeftText, filtered.Rows[i].LeftText, "행 " + i + " LeftText 일치");
                    AssertEqual(reference.Rows[i].RightText, filtered.Rows[i].RightText, "행 " + i + " RightText 일치");
                }
            });

            Test("DocumentAligner: 필터 적용 후에도 원본 줄 번호가 보존된다(건너뜀 있음)", () =>
            {
                var left = new List<string> { "A", "#c", "B", "C" }; // 원본 줄 번호: A=1, #c=2, B=3, C=4
                var right = new List<string> { "A", "B", "C" };
                var filters = new ExcludeFilterSet { Enabled = true };
                filters.Patterns.Add(new ExcludeFilterPattern("^#"));

                var doc = DiffDocument.Build(left, right, new DiffOptions { ExcludeFilters = filters });

                string leftLineNumbers = string.Join(",", doc.Rows.Where(r => r.LeftLineNo.HasValue).Select(r => r.LeftLineNo.Value));
                AssertEqual("1,3,4", leftLineNumbers, "좌측 줄 번호는 제외된 2번을 건너뛰고 1,3,4가 되어야 함");
            });
        }

        private static void RunMaskFilterTests()
        {
            Test("MaskMatch: 매치 부분만 다른 두 줄은 Same으로 판정되고 원문·마스크 좌표가 보존된다", () =>
            {
                var left = new List<string> { "<item address=\"aaa\" name=\"n\"/>" };
                var right = new List<string> { "<item address=\"bbb\" name=\"n\"/>" };
                var filters = new ExcludeFilterSet { Enabled = true };
                filters.Patterns.Add(new ExcludeFilterPattern("address=\"[^\"]*\"", ExcludeFilterMode.MaskMatch));

                var doc = DiffDocument.Build(left, right, new DiffOptions { ExcludeFilters = filters });

                AssertEqual(1, doc.Rows.Count, "줄은 제거되지 않고 1행이 남아야 함");
                AssertEqual(RowKind.Same, doc.Rows[0].Kind, "마스크 구간만 다르므로 Same이어야 함");
                AssertEqual(0, doc.TotalDiffCount, "diff 개수 0");
                AssertEqual("<item address=\"aaa\" name=\"n\"/>", doc.Rows[0].LeftText, "화면용 원문은 마스킹되지 않아야 함");
                Assert(doc.Rows[0].LeftMaskSpans != null && doc.Rows[0].LeftMaskSpans.Length == 1, "좌측 마스크 스팬 1개");
                AssertEqual(6, doc.Rows[0].LeftMaskSpans[0].Start, "마스크 시작 위치");
                AssertEqual(13, doc.Rows[0].LeftMaskSpans[0].Length, "마스크 길이");
            });

            Test("MaskMatch: 매치가 한쪽에만 있으면 Same이 되면 안 된다", () =>
            {
                var left = new List<string> { "<t addr=\"x\">" };
                var right = new List<string> { "<t>" };
                var filters = new ExcludeFilterSet { Enabled = true };
                filters.Patterns.Add(new ExcludeFilterPattern("addr=\"[^\"]*\"", ExcludeFilterMode.MaskMatch));

                var doc = DiffDocument.Build(left, right, new DiffOptions { ExcludeFilters = filters });

                AssertEqual(1, doc.TotalDiffCount, "센티널 문자 덕분에 여전히 다름으로 판정되어야 함");
                AssertEqual(RowKind.Changed, doc.Rows[0].Kind, "Changed여야 함");
            });

            Test("MaskMatch: 매치 개수가 다르면 Changed로 판정된다", () =>
            {
                var left = new List<string> { "a=1 b=2" };
                var right = new List<string> { "a=9" };
                var filters = new ExcludeFilterSet { Enabled = true };
                filters.Patterns.Add(new ExcludeFilterPattern("\\d+", ExcludeFilterMode.MaskMatch));

                var doc = DiffDocument.Build(left, right, new DiffOptions { ExcludeFilters = filters });
                AssertEqual(RowKind.Changed, doc.Rows[0].Kind, "매치 개수(2 vs 1)가 다르므로 Changed여야 함");
            });

            Test("MaskMatch: 겹치는 여러 패턴의 매치 구간은 병합된다", () =>
            {
                var left = new List<string> { "xabbcy" };
                var right = new List<string> { "xabcy" };
                var filters = new ExcludeFilterSet { Enabled = true };
                filters.Patterns.Add(new ExcludeFilterPattern("ab+", ExcludeFilterMode.MaskMatch));
                filters.Patterns.Add(new ExcludeFilterPattern("b+c", ExcludeFilterMode.MaskMatch));

                var doc = DiffDocument.Build(left, right, new DiffOptions { ExcludeFilters = filters });

                AssertEqual(RowKind.Same, doc.Rows[0].Kind, "마스크 병합 후 양쪽 비교용 줄이 같아 Same이어야 함");
                Assert(doc.Rows[0].LeftMaskSpans != null && doc.Rows[0].LeftMaskSpans.Length == 1, "겹친 두 매치는 1개 스팬으로 병합");
                AssertEqual(1, doc.Rows[0].LeftMaskSpans[0].Start, "병합 스팬 시작(abbc)");
                AssertEqual(4, doc.Rows[0].LeftMaskSpans[0].Length, "병합 스팬 길이(abbc)");
            });

            Test("ExcludeLine + MaskMatch 혼합: 줄 제거·줄 번호 건너뜀과 부분 마스킹이 함께 동작한다", () =>
            {
                var left = new List<string> { "A", "#c", "B time=5", "C" };
                var right = new List<string> { "A", "B time=9", "C" };
                var filters = new ExcludeFilterSet { Enabled = true };
                filters.Patterns.Add(new ExcludeFilterPattern("^#", ExcludeFilterMode.ExcludeLine));
                filters.Patterns.Add(new ExcludeFilterPattern("time=\\d+", ExcludeFilterMode.MaskMatch));

                var doc = DiffDocument.Build(left, right, new DiffOptions { ExcludeFilters = filters });

                AssertEqual(0, doc.TotalDiffCount, "time 차이는 마스크로 무시되어 diff 0이어야 함");
                AssertEqual(3, doc.Rows.Count, "#c 제거 후 3행");
                string leftLineNumbers = string.Join(",", doc.Rows.Where(r => r.LeftLineNo.HasValue).Select(r => r.LeftLineNo.Value));
                AssertEqual("1,3,4", leftLineNumbers, "제거된 2번을 건너뛴 원본 줄 번호 유지");
            });

            Test("MaskMatch + IgnoreCase: 마스킹 후 정규화가 적용된다(마스킹 먼저, 정규화 나중)", () =>
            {
                var left = new List<string> { "Hello time=1" };
                var right = new List<string> { "HELLO time=2" };
                var filters = new ExcludeFilterSet { Enabled = true };
                filters.Patterns.Add(new ExcludeFilterPattern("time=\\d+", ExcludeFilterMode.MaskMatch));

                var doc = DiffDocument.Build(left, right, new DiffOptions { IgnoreCase = true, ExcludeFilters = filters });
                AssertEqual(0, doc.TotalDiffCount, "마스크(time) + IgnoreCase(Hello/HELLO)로 완전 동일 판정이어야 함");
            });

            Test("IntralineDiffer: 마스크 구간과 겹치는 문자 단위 차이는 강조되지 않는다(클리핑)", () =>
            {
                var left = new List<string> { "name=\"a\" id=1" };
                var right = new List<string> { "name=\"b\" id=2" };
                var filters = new ExcludeFilterSet { Enabled = true };
                filters.Patterns.Add(new ExcludeFilterPattern("id=\\d", ExcludeFilterMode.MaskMatch));

                var doc = DiffDocument.Build(left, right, new DiffOptions { ExcludeFilters = filters });
                var row = doc.Rows[0];
                AssertEqual(RowKind.Changed, row.Kind, "마스크 밖(a/b) 차이 때문에 Changed여야 함");
                Assert(row.LeftMaskSpans != null, "좌측 마스크 스팬 존재");

                var spans = IntralineDiffer.ComputeLeftSpans(row.LeftText, row.RightText, row.LeftMaskSpans);
                foreach (var span in spans.Where(s => s.IsDifferent))
                {
                    foreach (var mask in row.LeftMaskSpans)
                    {
                        Assert(span.Start + span.Length <= mask.Start || span.Start >= mask.End,
                            "IsDifferent 스팬(" + span.Start + "," + span.Length + ")이 마스크 구간과 겹치면 안 됨");
                    }
                }
                Assert(spans.Any(s => s.IsDifferent), "마스크 밖의 진짜 차이(a/b)는 여전히 강조되어야 함");

                int covered = spans.Sum(s => s.Length);
                AssertEqual(row.LeftText.Length, covered, "클리핑 후에도 스팬들이 라인 전체를 빈틈없이 덮어야 함(렌더러 계약)");
            });

            Test("MaskMatch: 잘못된 정규식 패턴은 예외 없이 무시된다", () =>
            {
                var left = new List<string> { "A", "B" };
                var right = new List<string> { "A", "X" };
                var filters = new ExcludeFilterSet { Enabled = true };
                filters.Patterns.Add(new ExcludeFilterPattern("(unclosed", ExcludeFilterMode.MaskMatch));

                var doc = DiffDocument.Build(left, right, new DiffOptions { ExcludeFilters = filters });
                AssertEqual(1, doc.TotalDiffCount, "무효 패턴은 무시되고 기존 비교와 동일해야 함");
                Assert(doc.Rows.All(r => r.LeftMaskSpans == null && r.RightMaskSpans == null), "무효 패턴은 마스크 스팬을 만들면 안 됨");
            });

            Test("MaskMatch: Enabled=false면 마스킹되지 않는다", () =>
            {
                var left = new List<string> { "time=1" };
                var right = new List<string> { "time=2" };
                var filters = new ExcludeFilterSet { Enabled = false };
                filters.Patterns.Add(new ExcludeFilterPattern("time=\\d+", ExcludeFilterMode.MaskMatch));

                var doc = DiffDocument.Build(left, right, new DiffOptions { ExcludeFilters = filters });
                AssertEqual(1, doc.TotalDiffCount, "필터 비활성 시 time 차이는 그대로 diff여야 함");
                Assert(doc.Rows.All(r => r.LeftMaskSpans == null && r.RightMaskSpans == null), "마스크 스팬이 없어야 함");
            });

            Test("HasLineExclusions: ExcludeLine 패턴이 있을 때만 true(편집 모드 게이트)", () =>
            {
                var maskOnly = new ExcludeFilterSet { Enabled = true };
                maskOnly.Patterns.Add(new ExcludeFilterPattern("x", ExcludeFilterMode.MaskMatch));
                Assert(!maskOnly.HasLineExclusions, "MaskMatch만 있으면 false(편집 허용)");

                var withLine = new ExcludeFilterSet { Enabled = true };
                withLine.Patterns.Add(new ExcludeFilterPattern("x", ExcludeFilterMode.MaskMatch));
                withLine.Patterns.Add(new ExcludeFilterPattern("^#"));
                Assert(withLine.HasLineExclusions, "ExcludeLine 패턴이 있으면 true(편집 차단)");
                AssertEqual(ExcludeFilterMode.ExcludeLine, withLine.Patterns[1].Mode, "1-인자 생성자의 기본 모드는 ExcludeLine(하위호환)");

                var disabled = new ExcludeFilterSet { Enabled = false };
                disabled.Patterns.Add(new ExcludeFilterPattern("^#"));
                Assert(!disabled.HasLineExclusions, "Enabled=false면 항상 false");
            });
        }

        private static StructuredNode MakeMessage(string name, string id)
        {
            var node = new StructuredNode("Message", NodeKind.Element);
            node.Attributes.Add(new KeyValuePair<string, string>("name", name));
            node.Attributes.Add(new KeyValuePair<string, string>("id", id));
            node.Children.Add(new StructuredNode("Header", NodeKind.Element));
            return node;
        }

        private static StructuredNode NewScalarNode(string name, string value)
        {
            var node = new StructuredNode(name, NodeKind.Value);
            node.Value = value;
            node.ScalarKind = "string";
            return node;
        }

        private static void RunCommandLineTests()
        {
            Console.WriteLine("== CommandLineOptions ==");

            Test("위치 인자 2개만 (기존 호환)", () =>
            {
                var options = CommandLineOptions.Parse(new[] { @"C:\a\left.txt", @"C:\a\right.txt" });
                AssertEqual(@"C:\a\left.txt", options.LeftPath, "LeftPath");
                AssertEqual(@"C:\a\right.txt", options.RightPath, "RightPath");
                AssertEqual(0, options.Errors.Count, "오류 없음");
                Assert(!options.CloseWithEsc && !options.LeftReadOnly, "플래그 미설정");
            });

            Test("전체 플래그 조합", () =>
            {
                var options = CommandLineOptions.Parse(new[] { "/dl", "이전 버전", "/dr", "작업본", "/wl", "/e", "/u", @"C:\l.txt", @"C:\r.txt" });
                AssertEqual("이전 버전", options.LeftLabel, "LeftLabel");
                AssertEqual("작업본", options.RightLabel, "RightLabel");
                Assert(options.LeftReadOnly, "/wl");
                Assert(!options.RightReadOnly, "/wr 미지정");
                Assert(options.CloseWithEsc, "/e");
                Assert(options.NoRecent, "/u");
                AssertEqual(@"C:\l.txt", options.LeftPath, "LeftPath");
                AssertEqual(@"C:\r.txt", options.RightPath, "RightPath");
                AssertEqual(0, options.Errors.Count, "오류 없음");
            });

            Test("- 접두와 대소문자 무시", () =>
            {
                var options = CommandLineOptions.Parse(new[] { "-DL", "old", "/E", "-WR" });
                AssertEqual("old", options.LeftLabel, "-DL");
                Assert(options.CloseWithEsc, "/E");
                Assert(options.RightReadOnly, "-WR");
            });

            Test("/dl 값 누락은 오류 기록", () =>
            {
                var options = CommandLineOptions.Parse(new[] { @"C:\a.txt", @"C:\b.txt", "/dl" });
                AssertEqual(1, options.Errors.Count, "오류 1건");
                AssertEqual(@"C:\a.txt", options.LeftPath, "위치 인자는 정상 파싱");
            });

            Test("미지 플래그는 오류로 수집하되 계속 파싱", () =>
            {
                var options = CommandLineOptions.Parse(new[] { "/zz", @"C:\a.txt", @"C:\b.txt" });
                AssertEqual(1, options.Errors.Count, "미지 플래그 오류 1건");
                AssertEqual(@"C:\a.txt", options.LeftPath, "LeftPath");
                AssertEqual(@"C:\b.txt", options.RightPath, "RightPath");
            });

            Test("/register-git, /unregister-git", () =>
            {
                Assert(CommandLineOptions.Parse(new[] { "/register-git" }).RegisterGit, "register");
                Assert(CommandLineOptions.Parse(new[] { "-unregister-git" }).UnregisterGit, "unregister");
            });

            Test("슬래시로 시작하는 경로(Git Bash식)는 위치 인자로 취급", () =>
            {
                var options = CommandLineOptions.Parse(new[] { "/tmp/a.txt", "/tmp/b.txt" });
                AssertEqual("/tmp/a.txt", options.LeftPath, "LeftPath");
                AssertEqual("/tmp/b.txt", options.RightPath, "RightPath");
                AssertEqual(0, options.Errors.Count, "경로는 오류 아님");
            });
        }

        private static void RunGitPathTests()
        {
            Console.WriteLine("== GitPaths ==");

            Test("상대 경로: 백슬래시 → 슬래시, 중첩 디렉터리", () =>
            {
                AssertEqual("src/sub/a.cs", GitPaths.GetRepoRelativePath(@"C:\repo", @"C:\repo\src\sub\a.cs"), "중첩 경로");
            });

            Test("상대 경로: 대소문자 무시(Windows)", () =>
            {
                AssertEqual("a.txt", GitPaths.GetRepoRelativePath(@"c:\Repo", @"C:\repo\a.txt"), "대소문자 다른 루트");
            });

            Test("상대 경로: 루트 밖 파일은 null", () =>
            {
                Assert(GitPaths.GetRepoRelativePath(@"C:\repo", @"C:\other\a.txt") == null, "루트 밖");
                Assert(GitPaths.GetRepoRelativePath(@"C:\repo", @"C:\repository\a.txt") == null, "이름이 접두만 같은 형제 디렉터리");
            });

            Test("상대 경로: 루트 뒤 슬래시 허용", () =>
            {
                AssertEqual("a.txt", GitPaths.GetRepoRelativePath(@"C:\repo\", @"C:\repo\a.txt"), "trailing slash");
            });

            Test("BuildDifftoolCmdValue 형식 고정", () =>
            {
                string cmd = GitPaths.BuildDifftoolCmdValue(@"C:\Program Files\TextCompare\TextCompare.exe");
                Assert(cmd.StartsWith("\"C:/Program Files/TextCompare/TextCompare.exe\""), "forward-slash exe 경로를 따옴표로 감쌈");
                Assert(cmd.Contains("\"$LOCAL\" \"$REMOTE\""), "$LOCAL/$REMOTE 리터럴 유지");
                Assert(cmd.Contains("/wl"), "왼쪽 읽기 전용 플래그");
                Assert(cmd.Contains("/dl \"이전 버전\"") && cmd.Contains("/dr \"작업본\""), "좌우 라벨");
            });
        }

        private static void Test(string name, Action action)
        {
            try
            {
                action();
                Console.WriteLine("[PASS] " + name);
            }
            catch (Exception ex)
            {
                _failures++;
                Console.WriteLine("[FAIL] " + name + " :: " + ex.Message);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception("Assert 실패: " + message);
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception(string.Format("{0} :: 기대값={1}, 실제값={2}", message, expected, actual));
        }
    }
}
