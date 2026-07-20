using System;
using System.Windows.Forms;
using TextCompare.Cli;
using TextCompare.Git;

namespace TextCompare
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            CommandLineOptions options = CommandLineOptions.Parse(args);

            if (options.RegisterGit) return RunRegisterGit();
            if (options.UnregisterGit) return RunUnregisterGit();

            Application.Run(new MainForm(options));
            // 종료 코드: 0=차이 없음, 1=차이 있음, 2=오류 (WinMerge 관례; MainForm이 설정)
            return Environment.ExitCode;
        }

        private static int RunRegisterGit()
        {
            GitResult result = GitService.RegisterAsDifftool(Application.ExecutablePath);
            if (result.Success)
            {
                MessageBox.Show("git difftool로 등록되었습니다.\r\n이제 git difftool 명령으로 이 프로그램이 열립니다.",
                    "TextCompare", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return 0;
            }

            MessageBox.Show(BuildGitErrorMessage("git difftool 등록에 실패했습니다.", result),
                "TextCompare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 2;
        }

        private static int RunUnregisterGit()
        {
            GitResult result = GitService.UnregisterDifftool();
            if (result.Success)
            {
                MessageBox.Show("git difftool 등록이 해제되었습니다.",
                    "TextCompare", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return 0;
            }

            MessageBox.Show(BuildGitErrorMessage("git difftool 등록 해제에 실패했습니다.", result),
                "TextCompare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 2;
        }

        private static string BuildGitErrorMessage(string prefix, GitResult result)
        {
            if (result.GitNotFound) return prefix + "\r\ngit이 설치되어 있지 않거나 PATH에서 찾을 수 없습니다.";
            if (result.TimedOut) return prefix + "\r\ngit 응답이 없어 중단했습니다.";
            if (!string.IsNullOrEmpty(result.StdErr)) return prefix + "\r\n" + result.StdErr;
            return prefix;
        }
    }
}
