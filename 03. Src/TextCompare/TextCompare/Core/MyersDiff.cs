using System;
using System.Collections.Generic;

namespace TextCompare.Core
{
    /// <summary>
    /// Myers O(ND) 알고리즘 기반 최단 편집 스크립트(SES) 계산기.
    /// 라인 시퀀스, 문자 시퀀스 등 임의의 IList&lt;T&gt;에 재사용 가능한 제네릭 구현.
    /// </summary>
    public sealed class MyersDiff<T>
    {
        private readonly IList<T> _a;
        private readonly IList<T> _b;
        private readonly IEqualityComparer<T> _comparer;

        public MyersDiff(IList<T> a, IList<T> b)
            : this(a, b, EqualityComparer<T>.Default)
        {
        }

        public MyersDiff(IList<T> a, IList<T> b, IEqualityComparer<T> comparer)
        {
            if (a == null) throw new ArgumentNullException("a");
            if (b == null) throw new ArgumentNullException("b");
            _a = a;
            _b = b;
            _comparer = comparer ?? EqualityComparer<T>.Default;
        }

        /// <summary>변경 블록(DiffChange) 목록을 A 순서대로 반환한다.</summary>
        public List<DiffChange> Compute()
        {
            int aLo = 0, aHi = _a.Count, bLo = 0, bHi = _b.Count;

            // 공통 prefix / suffix를 먼저 잘라내어 탐색 범위(D)를 최소화한다.
            while (aLo < aHi && bLo < bHi && _comparer.Equals(_a[aLo], _b[bLo]))
            {
                aLo++; bLo++;
            }
            while (aHi > aLo && bHi > bLo && _comparer.Equals(_a[aHi - 1], _b[bHi - 1]))
            {
                aHi--; bHi--;
            }

            List<Edit> edits = ShortestEditScript(aLo, aHi, bLo, bHi);
            return Coalesce(edits);
        }

        private enum EditKind { Diag, Down, Right }

        private struct Edit
        {
            public readonly EditKind Kind;
            public readonly int X1, Y1, X2, Y2;

            public Edit(EditKind kind, int x1, int y1, int x2, int y2)
            {
                Kind = kind;
                X1 = x1; Y1 = y1; X2 = x2; Y2 = y2;
            }
        }

        /// <summary>
        /// [aLo,aHi) vs [bLo,bHi) 구간에 대해 표준 Myers 알고리즘(전진 탐색 + trace 기록 + 역추적)으로
        /// 편집 경로를 계산한다. 실무에서 흔한 "일부만 다른 파일" 시나리오에서는 D가 작아 빠르게 끝난다.
        /// </summary>
        private List<Edit> ShortestEditScript(int aLo, int aHi, int bLo, int bHi)
        {
            var result = new List<Edit>();
            int n = aHi - aLo;
            int m = bHi - bLo;
            int max = n + m;
            if (max == 0) return result;

            // 한쪽이 완전히 비어있는 자명한 경우(순수 삽입 또는 순수 삭제)는 편집 스크립트가 이미
            // 명백하므로 아래 일반 D-loop/역추적을 타지 않고 직접 구성한다. 이 경우 D-loop을 실제로
            // 실행하지 않으므로 v[]가 전부 0으로 남는데, 이 비어있는 v[]를 일반 역추적 로직에 그대로
            // 사용하면(과거 버전의 버그) 음수 CountA/CountB가 나오는 잘못된 DiffChange가 만들어진다
            // (실사례: 동일한 접두/접미 사이에서 문자열 일부가 순수 삭제되는 흔한 실사용 케이스에서 재현됨).
            if (n == 0)
            {
                result.Add(new Edit(EditKind.Right, aLo, bLo, aLo, bHi));
                return result;
            }
            if (m == 0)
            {
                result.Add(new Edit(EditKind.Down, aLo, bLo, aHi, bLo));
                return result;
            }

            int offset = max;
            int size = 2 * max + 1;
            var v = new int[size];
            var trace = new List<int[]>();

            bool found = false;
            int foundD = 0;

            for (int d = 0; d <= max; d++)
            {
                trace.Add((int[])v.Clone());

                for (int k = -d; k <= d; k += 2)
                {
                    int x;
                    if (k == -d || (k != d && v[offset + k - 1] < v[offset + k + 1]))
                        x = v[offset + k + 1];
                    else
                        x = v[offset + k - 1] + 1;

                    int y = x - k;

                    while (x < n && y < m && _comparer.Equals(_a[aLo + x], _b[bLo + y]))
                    {
                        x++; y++;
                    }

                    v[offset + k] = x;

                    if (x >= n && y >= m)
                    {
                        found = true;
                        foundD = d;
                        break;
                    }
                }
                if (found) break;
            }

            // 역추적: (n,m) -> (0,0)
            int cx = n, cy = m;
            for (int d = foundD; d >= 0; d--)
            {
                int[] vd = d < trace.Count ? trace[d] : v;
                int k = cx - cy;
                int prevK;
                if (k == -d || (k != d && vd[offset + k - 1] < vd[offset + k + 1]))
                    prevK = k + 1;
                else
                    prevK = k - 1;

                int prevX = d == 0 ? 0 : vd[offset + prevK];
                int prevY = prevX - prevK;

                while (cx > prevX && cy > prevY)
                {
                    result.Add(new Edit(EditKind.Diag, aLo + cx - 1, bLo + cy - 1, aLo + cx, bLo + cy));
                    cx--; cy--;
                }

                if (d > 0)
                {
                    if (cx == prevX)
                        result.Add(new Edit(EditKind.Right, aLo + prevX, bLo + prevY, aLo + cx, bLo + cy));
                    else
                        result.Add(new Edit(EditKind.Down, aLo + prevX, bLo + prevY, aLo + cx, bLo + cy));
                }

                cx = prevX; cy = prevY;
            }

            result.Reverse();
            return result;
        }

        /// <summary>연속된 Down/Right 편집을 하나의 DiffChange 블록으로 병합한다.</summary>
        private static List<DiffChange> Coalesce(List<Edit> edits)
        {
            var changes = new List<DiffChange>();
            int i = 0;
            while (i < edits.Count)
            {
                if (edits[i].Kind == EditKind.Diag)
                {
                    i++;
                    continue;
                }

                int runStart = i;
                while (i < edits.Count && edits[i].Kind != EditKind.Diag)
                {
                    i++;
                }
                int runEnd = i - 1;

                int sA = edits[runStart].X1;
                int sB = edits[runStart].Y1;
                int eA = edits[runEnd].X2;
                int eB = edits[runEnd].Y2;

                changes.Add(new DiffChange(sA, eA - sA, sB, eB - sB));
            }
            return changes;
        }
    }
}
