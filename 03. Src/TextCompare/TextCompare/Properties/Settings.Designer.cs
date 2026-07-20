namespace TextCompare.Properties
{
    /// <summary>
    /// 로컬(사용자 단위) 저장 설정. Visual Studio의 SettingsSingleFileGenerator가 Settings.settings로부터
    /// 생성하는 것과 동일한 형태를 손으로 작성했다(이 프로젝트는 Designer.cs 전반을 직접 작성하는 관례를 따른다).
    /// </summary>
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase
    {
        private static readonly Settings defaultInstance =
            ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));

        public static Settings Default
        {
            get { return defaultInstance; }
        }

        [global::System.Configuration.UserScopedSetting]
        [global::System.Configuration.DefaultSettingValue("False")]
        public bool ExcludeFilterEnabled
        {
            get { return ((bool)(this["ExcludeFilterEnabled"])); }
            set { this["ExcludeFilterEnabled"] = value; }
        }

        [global::System.Configuration.UserScopedSetting]
        [global::System.Configuration.DefaultSettingValue(
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n" +
            "<ArrayOfString xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" />")]
        public global::System.Collections.Specialized.StringCollection ExcludeFilterPatterns
        {
            get { return ((global::System.Collections.Specialized.StringCollection)(this["ExcludeFilterPatterns"])); }
            set { this["ExcludeFilterPatterns"] = value; }
        }

        /// <summary>ExcludeFilterPatterns와 인덱스가 병렬인 모드 목록("Line"/"Mask"). 항목이 없거나 목록이
        /// 짧으면 해당 패턴은 "Line"(라인 전체 제외)으로 해석된다 — 구버전 설정의 자동 마이그레이션.</summary>
        [global::System.Configuration.UserScopedSetting]
        [global::System.Configuration.DefaultSettingValue(
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n" +
            "<ArrayOfString xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" />")]
        public global::System.Collections.Specialized.StringCollection ExcludeFilterPatternModes
        {
            get { return ((global::System.Collections.Specialized.StringCollection)(this["ExcludeFilterPatternModes"])); }
            set { this["ExcludeFilterPatternModes"] = value; }
        }
    }
}
