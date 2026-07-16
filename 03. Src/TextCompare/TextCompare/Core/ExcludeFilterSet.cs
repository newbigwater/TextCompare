using System.Collections.Generic;

namespace TextCompare.Core
{
    /// <summary>
    /// 정규식 제외 필터 패턴 목록 + 활성화 여부. 비교 전에 좌/우 각각의 줄 목록에서
    /// 패턴에 일치하는 줄을 완전히 제거하고(Apply), 남은 줄들이 원래 몇 번째 줄이었는지도 함께 반환한다
    /// (제거된 줄만큼 번호가 건너뛰도록 해서 실제 파일의 줄 번호와 화면 표시가 어긋나지 않게 한다).
    /// </summary>
    public sealed class ExcludeFilterSet
    {
        public bool Enabled { get; set; }
        public List<ExcludeFilterPattern> Patterns { get; private set; }

        public ExcludeFilterSet()
        {
            Patterns = new List<ExcludeFilterPattern>();
        }

        public static ExcludeFilterSet Empty
        {
            get { return new ExcludeFilterSet(); }
        }

        public bool IsExcluded(string line)
        {
            if (!Enabled) return false;
            foreach (ExcludeFilterPattern pattern in Patterns)
            {
                if (pattern.IsMatch(line)) return true;
            }
            return false;
        }

        /// <summary>
        /// lines에서 제외 대상이 아닌 줄만 filteredLines에 담고, 각 줄의 1-based 원본 인덱스를
        /// originalLineNumbers에 같은 순서로 담는다(Enabled=false이거나 패턴이 없으면 원본을 그대로 복사).
        /// </summary>
        public void Apply(IList<string> lines, out List<string> filteredLines, out List<int> originalLineNumbers)
        {
            filteredLines = new List<string>(lines.Count);
            originalLineNumbers = new List<int>(lines.Count);

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                if (IsExcluded(line)) continue;

                filteredLines.Add(line);
                originalLineNumbers.Add(i + 1);
            }
        }
    }
}
