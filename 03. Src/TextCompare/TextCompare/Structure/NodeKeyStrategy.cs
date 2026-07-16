using System;
using System.Collections.Generic;

namespace TextCompare.Structure
{
    /// <summary>
    /// 형제 노드 목록(좌/우 양쪽)에서 안정적인 자연 키를 찾는다.
    /// 우선순위:
    ///  1) 노드 자신의 Name이 양쪽에서 충분히 유일 → JSON 오브젝트 프로퍼티(각 프로퍼티명이 곧 키)나
    ///     서로 다른 태그명을 가진 XML 형제들(Header/Body/Return 등)에 해당.
    ///  2) 후보 속성/자식값 이름(name/id/key/code 등)을 우선순위대로 검증 → 같은 태그가 반복되는
    ///     XML 형제들(여러 개의 Message, Field 등)에 해당. id가 버전 간 재사용되는 경우에도 name이
    ///     후보 목록에서 먼저 시도되므로 자연스럽게 우선 채택된다.
    ///  3) 전수조사: 양쪽 90% 이상에 값이 있는 모든 속성/자식명을 모아 유일성 점수가 가장 높은 것 채택.
    ///  4) 그래도 없으면 null 반환(호출자는 KeyedListMatcher.MatchPositional로 폴백).
    /// </summary>
    public static class NodeKeyStrategy
    {
        private const double UniquenessThreshold = 0.9;

        private static readonly string[] CandidateNames =
        {
            "name", "Name", "id", "ID", "key", "Key", "code", "Code"
        };

        public static Func<StructuredNode, string> ResolveKeySelector(IList<StructuredNode> left, IList<StructuredNode> right)
        {
            if (left == null || right == null || left.Count == 0 || right.Count == 0) return null;

            Func<StructuredNode, string> ownName = n => n.Name;
            if (Qualifies(left, ownName) && Qualifies(right, ownName))
            {
                return ownName;
            }

            foreach (string candidate in CandidateNames)
            {
                string capturedCandidate = candidate;
                Func<StructuredNode, string> selector = n => n.GetAttribute(capturedCandidate) ?? n.GetChildValue(capturedCandidate);
                if (Qualifies(left, selector) && Qualifies(right, selector))
                {
                    return selector;
                }
            }

            string bestName = null;
            double bestScore = 0;
            foreach (string candidateName in CollectCommonFieldNames(left, right))
            {
                string capturedName = candidateName;
                Func<StructuredNode, string> selector = n => n.GetAttribute(capturedName) ?? n.GetChildValue(capturedName);
                if (!AllHaveValue(left, selector) || !AllHaveValue(right, selector)) continue;

                double score = Math.Min(UniquenessRatio(left, selector), UniquenessRatio(right, selector));
                if (score >= UniquenessThreshold && score > bestScore)
                {
                    bestScore = score;
                    bestName = candidateName;
                }
            }

            if (bestName != null)
            {
                string capturedBest = bestName;
                return n => n.GetAttribute(capturedBest) ?? n.GetChildValue(capturedBest);
            }

            return null;
        }

        /// <summary>모든 노드가 값을 갖고 있으면서(누락 없음) 유일성 임계치를 만족하는지.</summary>
        private static bool Qualifies(IList<StructuredNode> nodes, Func<StructuredNode, string> selector)
        {
            return AllHaveValue(nodes, selector) && UniquenessRatio(nodes, selector) >= UniquenessThreshold;
        }

        private static bool AllHaveValue(IList<StructuredNode> nodes, Func<StructuredNode, string> selector)
        {
            foreach (StructuredNode n in nodes)
            {
                if (string.IsNullOrEmpty(selector(n))) return false;
            }
            return true;
        }

        private static double UniquenessRatio(IList<StructuredNode> nodes, Func<StructuredNode, string> selector)
        {
            var seen = new HashSet<string>();
            int valid = 0;
            foreach (StructuredNode n in nodes)
            {
                string v = selector(n);
                if (string.IsNullOrEmpty(v)) continue;
                valid++;
                seen.Add(v);
            }
            if (valid == 0) return 0;
            return (double)seen.Count / valid;
        }

        private static IEnumerable<string> CollectCommonFieldNames(IList<StructuredNode> left, IList<StructuredNode> right)
        {
            var names = new HashSet<string>();
            CollectFieldNames(left, names);
            CollectFieldNames(right, names);
            return names;
        }

        private static void CollectFieldNames(IList<StructuredNode> nodes, HashSet<string> names)
        {
            foreach (StructuredNode n in nodes)
            {
                foreach (var attr in n.Attributes)
                {
                    names.Add(attr.Key);
                }
                foreach (StructuredNode child in n.Children)
                {
                    if (child.Kind == NodeKind.Value && !string.IsNullOrEmpty(child.Name))
                    {
                        names.Add(child.Name);
                    }
                }
            }
        }
    }
}
