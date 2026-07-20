using System;

namespace TextCompare.Git
{
    /// <summary>
    /// git 관련 경로/문자열 변환 순수 헬퍼 (Process/WinForms 의존성 없음 — SelfTest에서 직접 검증).
    /// </summary>
    public static class GitPaths
    {
        /// <summary>difftool 등록 시 사용하는 도구 이름 (difftool.textcompare.* 섹션).</summary>
        public const string ToolName = "textcompare";

        /// <summary>백슬래시를 git이 기대하는 forward slash로 바꾼다.</summary>
        public static string ToGitSlashes(string path)
        {
            if (path == null) return null;
            return path.Replace('\\', '/');
        }

        /// <summary>
        /// 저장소 루트 기준 상대 경로를 forward slash로 반환한다.
        /// Windows 특성상 대소문자를 무시하고 비교하며, 루트 밖 경로면 null.
        /// </summary>
        public static string GetRepoRelativePath(string repoRoot, string filePath)
        {
            if (string.IsNullOrEmpty(repoRoot) || string.IsNullOrEmpty(filePath)) return null;

            string root = ToGitSlashes(repoRoot).TrimEnd('/');
            string file = ToGitSlashes(filePath);

            if (!file.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase)) return null;

            return file.Substring(root.Length + 1);
        }

        /// <summary>
        /// difftool.textcompare.cmd에 저장할 명령 문자열을 만든다.
        /// "$LOCAL"/"$REMOTE"는 리터럴로 유지해 git의 sh가 실행 시점에 확장하게 하고,
        /// exe 경로는 forward slash로 바꿔 sh의 백슬래시 해석 문제를 회피한다.
        /// (경로에 '$'나 '"'가 포함된 exe는 지원하지 않음 — sh 인용 한계)
        /// </summary>
        public static string BuildDifftoolCmdValue(string exePath)
        {
            if (exePath == null) throw new ArgumentNullException("exePath");
            return string.Format(
                "\"{0}\" /dl \"이전 버전\" /dr \"작업본\" /wl /u /e \"$LOCAL\" \"$REMOTE\"",
                ToGitSlashes(exePath));
        }
    }
}
