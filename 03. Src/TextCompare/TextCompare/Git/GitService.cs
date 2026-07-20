using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace TextCompare.Git
{
    /// <summary>git 프로세스 실행 결과.</summary>
    public sealed class GitResult
    {
        public int ExitCode;
        public string StdErr;
        public bool GitNotFound;
        public bool TimedOut;

        public bool Success
        {
            get { return !GitNotFound && !TimedOut && ExitCode == 0; }
        }
    }

    /// <summary>
    /// git 외부 프로세스 실행을 전담하는 서비스.
    /// UI(MainForm)는 여기서 반환된 GitResult만 보고 메시지를 결정한다.
    /// </summary>
    public static class GitService
    {
        private const int TimeoutMs = 15000;

        /// <summary>git 실행 파일이 PATH에 있는지 확인한다.</summary>
        public static bool IsGitAvailable()
        {
            GitResult result = Run(null, "--version", null);
            return result.Success;
        }

        /// <summary>
        /// 파일이 속한 git 저장소 루트를 상향 탐색으로 찾는다. 없으면 null.
        /// .git 디렉터리뿐 아니라 .git 파일(worktree/서브모듈)도 인정한다.
        /// 프로세스 실행 없이 파일 시스템만 사용.
        /// </summary>
        public static string FindRepositoryRoot(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;

            string dir;
            try
            {
                dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
            }
            catch (ArgumentException)
            {
                return null;
            }

            while (!string.IsNullOrEmpty(dir))
            {
                string gitPath = Path.Combine(dir, ".git");
                if (Directory.Exists(gitPath) || File.Exists(gitPath)) return dir;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }

        /// <summary>
        /// git show HEAD:&lt;relPath&gt; 출력을 outputPath에 raw 바이트로 저장한다.
        /// stdout을 텍스트로 읽으면 인코딩이 훼손되므로 BaseStream을 그대로 복사해
        /// EncodingDetector가 원본 바이트(BOM/EUC-KR 등)를 감지할 수 있게 한다.
        /// </summary>
        public static GitResult ShowHead(string repoRoot, string relPath, string outputPath)
        {
            return Run(repoRoot, string.Format("show \"HEAD:{0}\"", relPath), outputPath);
        }

        /// <summary>
        /// 전역 git config에 이 앱을 difftool로 등록한다. 재실행해도 같은 값을 덮어써 멱등.
        /// </summary>
        public static GitResult RegisterAsDifftool(string exePath)
        {
            GitResult result = Run(null, "config --global diff.tool " + GitPaths.ToolName, null);
            if (!result.Success) return result;

            string cmdValue = GitPaths.BuildDifftoolCmdValue(exePath);
            result = Run(null, string.Format("config --global difftool.{0}.cmd {1}", GitPaths.ToolName, QuoteArgument(cmdValue)), null);
            if (!result.Success) return result;

            return Run(null, "config --global difftool.prompt false", null);
        }

        /// <summary>
        /// 등록을 해제한다. diff.tool은 현재 값이 이 앱일 때만 지우고(다른 도구 설정 보호),
        /// difftool.textcompare 섹션은 통째로 제거한다(없으면 무시).
        /// </summary>
        public static GitResult UnregisterDifftool()
        {
            string currentTool;
            GitResult getResult = RunCapture(null, "config --global --get diff.tool", out currentTool);
            if (getResult.GitNotFound || getResult.TimedOut) return getResult;

            if (getResult.ExitCode == 0 && string.Equals(currentTool, GitPaths.ToolName, StringComparison.OrdinalIgnoreCase))
            {
                GitResult unset = Run(null, "config --global --unset diff.tool", null);
                if (!unset.Success) return unset;
            }

            GitResult remove = Run(null, "config --global --remove-section difftool." + GitPaths.ToolName, null);
            // exit 5 = 섹션 없음(git 128 이전 버전은 다른 코드일 수 있으나 non-fatal) — 이미 해제된 상태로 취급.
            if (!remove.Success && remove.ExitCode != 5 && remove.ExitCode != 128) return remove;

            return new GitResult { ExitCode = 0, StdErr = string.Empty };
        }

        /// <summary>git을 실행한다. outputPath가 있으면 stdout raw 바이트를 해당 파일에 저장한다.</summary>
        private static GitResult Run(string workingDirectory, string arguments, string outputPath)
        {
            string ignored;
            return RunCore(workingDirectory, arguments, outputPath, false, out ignored);
        }

        /// <summary>git을 실행하고 stdout을 텍스트(트림)로 캡처한다.</summary>
        private static GitResult RunCapture(string workingDirectory, string arguments, out string stdOut)
        {
            return RunCore(workingDirectory, arguments, null, true, out stdOut);
        }

        private static GitResult RunCore(string workingDirectory, string arguments, string outputPath, bool captureText, out string stdOut)
        {
            stdOut = null;
            var result = new GitResult { StdErr = string.Empty };

            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardErrorEncoding = Encoding.UTF8
            };
            if (!string.IsNullOrEmpty(workingDirectory)) startInfo.WorkingDirectory = workingDirectory;

            try
            {
                using (var process = Process.Start(startInfo))
                {
                    // 데드락 방지: stderr는 비동기로 읽으면서 stdout을 동기로 소비한다.
                    var stdErrBuilder = new StringBuilder();
                    process.ErrorDataReceived += (s, e) => { if (e.Data != null) stdErrBuilder.AppendLine(e.Data); };
                    process.BeginErrorReadLine();

                    if (outputPath != null)
                    {
                        using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                        {
                            process.StandardOutput.BaseStream.CopyTo(fileStream);
                        }
                    }
                    else if (captureText)
                    {
                        stdOut = process.StandardOutput.ReadToEnd().Trim();
                    }
                    else
                    {
                        process.StandardOutput.BaseStream.CopyTo(Stream.Null);
                    }

                    if (!process.WaitForExit(TimeoutMs))
                    {
                        try { process.Kill(); }
                        catch (Exception) { }
                        result.TimedOut = true;
                        return result;
                    }

                    result.ExitCode = process.ExitCode;
                    result.StdErr = stdErrBuilder.ToString().Trim();
                }
            }
            catch (Win32Exception)
            {
                result.GitNotFound = true;
            }

            return result;
        }

        /// <summary>
        /// ProcessStartInfo.Arguments용 인자 인용(MSVCRT 규칙): 전체를 따옴표로 감싸고
        /// 내부 따옴표는 \"로, 따옴표 앞 백슬래시는 두 배로 이스케이프한다.
        /// </summary>
        private static string QuoteArgument(string value)
        {
            var builder = new StringBuilder("\"");
            int backslashes = 0;
            foreach (char c in value)
            {
                if (c == '\\')
                {
                    backslashes++;
                    continue;
                }
                if (c == '"')
                {
                    builder.Append('\\', backslashes * 2 + 1);
                    builder.Append('"');
                }
                else
                {
                    builder.Append('\\', backslashes);
                    builder.Append(c);
                }
                backslashes = 0;
            }
            builder.Append('\\', backslashes * 2);
            builder.Append('"');
            return builder.ToString();
        }
    }
}
