using System;
using System.Collections.Generic;

namespace TextCompare.Cli
{
    /// <summary>
    /// 명령줄 인자 파서 (WinForms 의존성 없음 — SelfTest에서 직접 검증).
    ///
    /// 문법:
    ///   TextCompare.exe [플래그] [&lt;left&gt; &lt;right&gt;]
    ///     /dl &lt;label&gt;     왼쪽 제목 라벨 (git difftool의 임시 경로 대신 표시)
    ///     /dr &lt;label&gt;     오른쪽 제목 라벨
    ///     /e              Esc 한 번으로 창 닫기
    ///     /u              최근 파일 목록에 추가 안 함 (WinMerge 호환용 — MRU가 없어 no-op)
    ///     /wl             왼쪽 읽기 전용
    ///     /wr             오른쪽 읽기 전용
    ///     /register-git   git 전역 difftool로 등록 후 종료
    ///     /unregister-git 등록 해제 후 종료
    ///
    /// 규칙: '/' 또는 '-' 접두 + 알려진 플래그명(대소문자 무시)만 플래그로 취급하고,
    /// 나머지 토큰은 위치 인자(첫째→Left, 둘째→Right)로 처리해 기존 2-인자 호출과 완전 호환.
    /// 미지의 플래그는 Errors에 기록하되 파싱은 계속한다(WinMerge식 관용).
    /// </summary>
    public sealed class CommandLineOptions
    {
        public string LeftPath;
        public string RightPath;
        public string LeftLabel;                  // /dl
        public string RightLabel;                 // /dr
        public bool CloseWithEsc;                 // /e
        public bool NoRecent;                     // /u (호환용 no-op)
        public bool LeftReadOnly;                 // /wl
        public bool RightReadOnly;                // /wr
        public bool RegisterGit;                  // /register-git
        public bool UnregisterGit;                // /unregister-git
        public readonly List<string> Errors = new List<string>();

        public static CommandLineOptions Parse(string[] args)
        {
            var options = new CommandLineOptions();
            if (args == null) return options;

            var positionals = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                string token = args[i];
                if (string.IsNullOrEmpty(token)) continue;

                string flag = GetFlagName(token);
                switch (flag)
                {
                    case "dl":
                        if (i + 1 < args.Length) options.LeftLabel = args[++i];
                        else options.Errors.Add("/dl 다음에 라벨 값이 필요합니다.");
                        break;
                    case "dr":
                        if (i + 1 < args.Length) options.RightLabel = args[++i];
                        else options.Errors.Add("/dr 다음에 라벨 값이 필요합니다.");
                        break;
                    case "e":
                        options.CloseWithEsc = true;
                        break;
                    case "u":
                        options.NoRecent = true;
                        break;
                    case "wl":
                        options.LeftReadOnly = true;
                        break;
                    case "wr":
                        options.RightReadOnly = true;
                        break;
                    case "register-git":
                        options.RegisterGit = true;
                        break;
                    case "unregister-git":
                        options.UnregisterGit = true;
                        break;
                    default:
                        if (flag != null && !LooksLikePath(token))
                        {
                            options.Errors.Add("알 수 없는 옵션: " + token);
                        }
                        else
                        {
                            positionals.Add(token);
                        }
                        break;
                }
            }

            if (positionals.Count >= 1) options.LeftPath = positionals[0];
            if (positionals.Count >= 2) options.RightPath = positionals[1];

            return options;
        }

        /// <summary>'/' 또는 '-' 접두 토큰이면 소문자 플래그명을, 아니면 null을 반환한다.</summary>
        private static string GetFlagName(string token)
        {
            if (token.Length < 2) return null;
            if (token[0] != '/' && token[0] != '-') return null;
            return token.Substring(1).ToLowerInvariant();
        }

        /// <summary>
        /// '/'로 시작하지만 실제로는 경로일 수 있는 토큰 판별.
        /// 경로 구분자나 드라이브 콜론이 포함되면 경로로 취급해 위치 인자로 보낸다.
        /// (예: "/tmp/a.txt" 같은 Git Bash식 경로)
        /// </summary>
        private static bool LooksLikePath(string token)
        {
            return token.IndexOf('\\') >= 0 || token.IndexOf(':') >= 0 ||
                   token.LastIndexOf('/') > 0;
        }
    }
}
