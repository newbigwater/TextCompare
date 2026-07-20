using System.Collections.Specialized;
using TextCompare.Core;

namespace TextCompare.Properties
{
    /// <summary>
    /// TextCompare.Core.ExcludeFilterSet과 로컬(App Config 기반) 사용자 설정 사이의 변환.
    /// System.Configuration 의존성을 Core에서 격리하기 위해 이 어댑터로만 접근한다.
    /// 모드는 패턴과 인덱스가 병렬인 별도 StringCollection("Line"/"Mask")으로 저장한다 —
    /// 모드 목록이 없거나 짧은 구버전 설정은 자동으로 전부 "Line"(기존 동작)으로 해석된다.
    /// </summary>
    internal static class ExcludeFilterSettingsAdapter
    {
        private const string ModeMaskValue = "Mask";
        private const string ModeLineValue = "Line";

        public static ExcludeFilterSet Load()
        {
            var set = new ExcludeFilterSet { Enabled = Settings.Default.ExcludeFilterEnabled };

            StringCollection saved = Settings.Default.ExcludeFilterPatterns;
            StringCollection modes = Settings.Default.ExcludeFilterPatternModes;
            if (saved != null)
            {
                for (int i = 0; i < saved.Count; i++)
                {
                    string pattern = saved[i];
                    if (string.IsNullOrEmpty(pattern)) continue;

                    ExcludeFilterMode mode = (modes != null && i < modes.Count && modes[i] == ModeMaskValue)
                        ? ExcludeFilterMode.MaskMatch
                        : ExcludeFilterMode.ExcludeLine; // 없거나 알 수 없는 값은 기존 동작으로 방어
                    set.Patterns.Add(new ExcludeFilterPattern(pattern, mode));
                }
            }

            return set;
        }

        public static void Save(ExcludeFilterSet set)
        {
            Settings.Default.ExcludeFilterEnabled = set.Enabled;

            var patternCol = new StringCollection();
            var modeCol = new StringCollection();
            foreach (ExcludeFilterPattern pattern in set.Patterns)
            {
                patternCol.Add(pattern.Pattern);
                modeCol.Add(pattern.Mode == ExcludeFilterMode.MaskMatch ? ModeMaskValue : ModeLineValue);
            }
            Settings.Default.ExcludeFilterPatterns = patternCol;
            Settings.Default.ExcludeFilterPatternModes = modeCol;

            Settings.Default.Save();
        }
    }
}
