using System.Collections.Specialized;
using TextCompare.Core;

namespace TextCompare.Properties
{
    /// <summary>
    /// TextCompare.Core.ExcludeFilterSet과 로컬(App Config 기반) 사용자 설정 사이의 변환.
    /// System.Configuration 의존성을 Core에서 격리하기 위해 이 어댑터로만 접근한다.
    /// </summary>
    internal static class ExcludeFilterSettingsAdapter
    {
        public static ExcludeFilterSet Load()
        {
            var set = new ExcludeFilterSet { Enabled = Settings.Default.ExcludeFilterEnabled };

            StringCollection saved = Settings.Default.ExcludeFilterPatterns;
            if (saved != null)
            {
                foreach (string pattern in saved)
                {
                    if (!string.IsNullOrEmpty(pattern))
                    {
                        set.Patterns.Add(new ExcludeFilterPattern(pattern));
                    }
                }
            }

            return set;
        }

        public static void Save(ExcludeFilterSet set)
        {
            Settings.Default.ExcludeFilterEnabled = set.Enabled;

            var col = new StringCollection();
            foreach (ExcludeFilterPattern pattern in set.Patterns)
            {
                col.Add(pattern.Pattern);
            }
            Settings.Default.ExcludeFilterPatterns = col;

            Settings.Default.Save();
        }
    }
}
