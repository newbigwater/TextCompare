using System;
using System.Collections.Generic;
using TextCompare.Alignment;
using TextCompare.Core;

namespace TextCompare.Structure
{
    /// <summary>
    /// 루트 노드부터 재귀적으로 두 트리를 비교해 List&lt;AlignedRow&gt;를 만든다.
    /// 결과는 TextCompare.Alignment.AlignedRow 계약을 그대로 따르므로,
    /// 이후 DiffDocument.FromRows부터 Controls 레이어까지 라인 비교와 완전히 동일한 경로로 렌더링된다.
    /// </summary>
    public static class StructureDiffer
    {
        public static List<AlignedRow> Diff(StructuredNode left, StructuredNode right, IStructurePrettyPrinter printer, DiffOptions options)
        {
            if (left == null) throw new ArgumentNullException("left");
            if (right == null) throw new ArgumentNullException("right");
            if (printer == null) throw new ArgumentNullException("printer");

            var ctx = new DiffContext(printer, options ?? DiffOptions.Default);
            DiffNodePair(left, right, 0, true, true, ctx);
            return ctx.Rows;
        }

        private sealed class DiffContext
        {
            public readonly IStructurePrettyPrinter Printer;
            public readonly LineHasher Hasher;
            public readonly List<AlignedRow> Rows = new List<AlignedRow>();
            public int NextBlockIndex;
            public int LeftLineCounter;
            public int RightLineCounter;

            public DiffContext(IStructurePrettyPrinter printer, DiffOptions options)
            {
                Printer = printer;
                Hasher = new LineHasher(options);
            }
        }

        private static void DiffNodePair(StructuredNode left, StructuredNode right, int depth, bool isLastLeft, bool isLastRight, DiffContext ctx)
        {
            bool leftHasChildren = left.Children.Count > 0;
            bool rightHasChildren = right.Children.Count > 0;

            if (leftHasChildren != rightHasChildren)
            {
                // 같은 키로 매칭됐지만 한쪽만 자식을 갖는 특이 케이스(리프↔컨테이너 형태 변화).
                // 줄 단위 페어링이 불가능하므로 안전하게 완전 교체(제거+추가)로 처리한다.
                EmitWholeSubtree(left, depth, isLastLeft, true, ctx);
                EmitWholeSubtree(right, depth, isLastRight, false, ctx);
                return;
            }

            if (!leftHasChildren)
            {
                EmitPairedRender(
                    isLast => ctx.Printer.RenderLeaf(left, depth, isLast),
                    isLast => ctx.Printer.RenderLeaf(right, depth, isLast),
                    isLastLeft, isLastRight, ctx);
                return;
            }

            EmitPairedRender(
                isLast => ctx.Printer.RenderOpenTag(left, depth, isLast),
                isLast => ctx.Printer.RenderOpenTag(right, depth, isLast),
                isLastLeft, isLastRight, ctx);

            Func<StructuredNode, string> keySelector = NodeKeyStrategy.ResolveKeySelector(left.Children, right.Children);
            List<ChildMatch> matches = keySelector != null
                ? KeyedListMatcher.MatchByKey(left.Children, right.Children, keySelector)
                : KeyedListMatcher.MatchPositional(left.Children, right.Children);

            foreach (ChildMatch match in matches)
            {
                switch (match.Kind)
                {
                    case ChildMatchKind.Matched:
                        DiffNodePair(match.Left, match.Right, depth + 1,
                            IsLastChild(left, match.Left), IsLastChild(right, match.Right), ctx);
                        break;
                    case ChildMatchKind.LeftOnly:
                        EmitWholeSubtree(match.Left, depth + 1, IsLastChild(left, match.Left), true, ctx);
                        break;
                    case ChildMatchKind.RightOnly:
                        EmitWholeSubtree(match.Right, depth + 1, IsLastChild(right, match.Right), false, ctx);
                        break;
                }
            }

            EmitPairedRender(
                isLast => ctx.Printer.RenderCloseTag(left, depth, isLast),
                isLast => ctx.Printer.RenderCloseTag(right, depth, isLast),
                isLastLeft, isLastRight, ctx);
        }

        /// <summary>좌/우 각각 렌더링(실제 표시용은 실제 isLast, 동일성 판정용은 콤마 유무에 흔들리지 않도록 isLast=true로 별도 렌더링).</summary>
        private static void EmitPairedRender(Func<bool, string> renderLeft, Func<bool, string> renderRight, bool isLastLeft, bool isLastRight, DiffContext ctx)
        {
            string leftDisplay = renderLeft(isLastLeft);
            string rightDisplay = renderRight(isLastRight);
            string leftCanonical = renderLeft(true);
            string rightCanonical = renderRight(true);
            EmitPairedLine(leftDisplay, rightDisplay, leftCanonical, rightCanonical, ctx);
        }

        private static void EmitPairedLine(string leftDisplay, string rightDisplay, string leftCanonical, string rightCanonical, DiffContext ctx)
        {
            bool same = ctx.Hasher.Equals(leftCanonical, rightCanonical);
            RowKind kind = same ? RowKind.Same : RowKind.Changed;
            int blockIndex = same ? -1 : ctx.NextBlockIndex++;

            ctx.LeftLineCounter++;
            ctx.RightLineCounter++;
            ctx.Rows.Add(new AlignedRow(kind, leftDisplay, rightDisplay, ctx.LeftLineCounter, ctx.RightLineCounter, blockIndex));
        }

        private static void EmitWholeSubtree(StructuredNode node, int depth, bool isLast, bool isLeft, DiffContext ctx)
        {
            int blockIndex = ctx.NextBlockIndex++;
            EmitSubtreeLines(node, depth, isLast, isLeft, blockIndex, ctx);
        }

        private static void EmitSubtreeLines(StructuredNode node, int depth, bool isLast, bool isLeft, int blockIndex, DiffContext ctx)
        {
            if (node.Children.Count == 0)
            {
                string line = ctx.Printer.RenderLeaf(node, depth, isLast);
                EmitSingleSidedLine(line, isLeft, blockIndex, ctx);
                return;
            }

            string openLine = ctx.Printer.RenderOpenTag(node, depth, isLast);
            EmitSingleSidedLine(openLine, isLeft, blockIndex, ctx);

            for (int i = 0; i < node.Children.Count; i++)
            {
                StructuredNode child = node.Children[i];
                bool childIsLast = i == node.Children.Count - 1;
                EmitSubtreeLines(child, depth + 1, childIsLast, isLeft, blockIndex, ctx);
            }

            string closeLine = ctx.Printer.RenderCloseTag(node, depth, isLast);
            EmitSingleSidedLine(closeLine, isLeft, blockIndex, ctx);
        }

        private static void EmitSingleSidedLine(string text, bool isLeft, int blockIndex, DiffContext ctx)
        {
            RowKind kind = isLeft ? RowKind.LeftOnly : RowKind.RightOnly;
            int? leftNo = null, rightNo = null;
            string leftText = null, rightText = null;

            if (isLeft)
            {
                ctx.LeftLineCounter++;
                leftNo = ctx.LeftLineCounter;
                leftText = text;
            }
            else
            {
                ctx.RightLineCounter++;
                rightNo = ctx.RightLineCounter;
                rightText = text;
            }

            ctx.Rows.Add(new AlignedRow(kind, leftText, rightText, leftNo, rightNo, blockIndex));
        }

        private static bool IsLastChild(StructuredNode parent, StructuredNode child)
        {
            if (parent.Children.Count == 0) return true;
            return ReferenceEquals(parent.Children[parent.Children.Count - 1], child);
        }
    }
}
