using System;
using System.Collections.Generic;
using TextCompare.Core;

namespace TextCompare.Structure
{
    /// <summary>매칭된 자식 한 쌍(또는 한쪽만 존재)의 결과.</summary>
    public enum ChildMatchKind { Matched, LeftOnly, RightOnly }

    public struct ChildMatch
    {
        public readonly ChildMatchKind Kind;
        public readonly StructuredNode Left;
        public readonly StructuredNode Right;

        public ChildMatch(ChildMatchKind kind, StructuredNode left, StructuredNode right)
        {
            Kind = kind;
            Left = left;
            Right = right;
        }
    }

    /// <summary>
    /// 형제 노드 목록(자식들)을 좌/우 사이에서 짝짓는다.
    /// DocumentAligner(라인용)와 달리, 키가 있는 경우 교체 구간을 절대 페어링하지 않는다 —
    /// 키가 다른 두 노드는 "값이 바뀐 같은 개체"가 아니라 "서로 다른 개체"이기 때문(정합성 비교의 핵심).
    /// </summary>
    public static class KeyedListMatcher
    {
        /// <summary>자연 키가 있을 때: 키 배열에 MyersDiff&lt;string&gt;을 재사용해 공통/삽입/삭제 구간을 구하고,
        /// 교체 구간은 페어링 없이 좌측 전부 LeftOnly, 우측 전부 RightOnly로 분리한다.</summary>
        public static List<ChildMatch> MatchByKey(IList<StructuredNode> left, IList<StructuredNode> right, Func<StructuredNode, string> keySelector)
        {
            var leftKeys = new List<string>(left.Count);
            foreach (StructuredNode n in left) leftKeys.Add(keySelector(n));

            var rightKeys = new List<string>(right.Count);
            foreach (StructuredNode n in right) rightKeys.Add(keySelector(n));

            List<DiffChange> changes = new MyersDiff<string>(leftKeys, rightKeys).Compute();

            var result = new List<ChildMatch>();
            int a = 0, b = 0;

            foreach (DiffChange change in changes)
            {
                while (a < change.StartA && b < change.StartB)
                {
                    result.Add(new ChildMatch(ChildMatchKind.Matched, left[a], right[b]));
                    a++; b++;
                }

                for (int i = 0; i < change.CountA; i++)
                {
                    result.Add(new ChildMatch(ChildMatchKind.LeftOnly, left[a], null));
                    a++;
                }
                for (int i = 0; i < change.CountB; i++)
                {
                    result.Add(new ChildMatch(ChildMatchKind.RightOnly, null, right[b]));
                    b++;
                }
            }

            while (a < left.Count && b < right.Count)
            {
                result.Add(new ChildMatch(ChildMatchKind.Matched, left[a], right[b]));
                a++; b++;
            }

            return result;
        }

        /// <summary>자연 키가 없을 때: 노드 전체를 값으로 보고 깊은 동등성 비교자로 MyersDiff&lt;StructuredNode&gt;를
        /// 실행한다. 텍스트 라인 비교와 동일한 최선 추정 방식으로, 교체 구간은 위치 기준으로 페어링한다.</summary>
        public static List<ChildMatch> MatchPositional(IList<StructuredNode> left, IList<StructuredNode> right)
        {
            List<DiffChange> changes = new MyersDiff<StructuredNode>(left, right, new DeepEqualityComparer()).Compute();

            var result = new List<ChildMatch>();
            int a = 0, b = 0;

            foreach (DiffChange change in changes)
            {
                while (a < change.StartA && b < change.StartB)
                {
                    result.Add(new ChildMatch(ChildMatchKind.Matched, left[a], right[b]));
                    a++; b++;
                }

                int paired = Math.Min(change.CountA, change.CountB);
                for (int i = 0; i < paired; i++)
                {
                    result.Add(new ChildMatch(ChildMatchKind.Matched, left[a], right[b]));
                    a++; b++;
                }
                for (int i = paired; i < change.CountA; i++)
                {
                    result.Add(new ChildMatch(ChildMatchKind.LeftOnly, left[a], null));
                    a++;
                }
                for (int i = paired; i < change.CountB; i++)
                {
                    result.Add(new ChildMatch(ChildMatchKind.RightOnly, null, right[b]));
                    b++;
                }
            }

            while (a < left.Count && b < right.Count)
            {
                result.Add(new ChildMatch(ChildMatchKind.Matched, left[a], right[b]));
                a++; b++;
            }

            return result;
        }

        private sealed class DeepEqualityComparer : IEqualityComparer<StructuredNode>
        {
            public bool Equals(StructuredNode x, StructuredNode y)
            {
                return StructureEquality.DeepEquals(x, y);
            }

            public int GetHashCode(StructuredNode obj)
            {
                return StructureEquality.DeepHashCode(obj);
            }
        }
    }
}
