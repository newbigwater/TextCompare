using System;
using System.IO;

namespace TextCompare.Git
{
    /// <summary>
    /// git show 결과를 담을 임시 파일의 생성/정리를 담당한다.
    /// %TEMP%\TextCompare\git\&lt;guid&gt;\&lt;원본파일명.확장자&gt; 구조로 만들어
    /// 확장자를 보존한다(.xml/.json 구조 비교 모드가 확장자로 분기하기 때문).
    /// 정리는 전부 best-effort — 잠긴 파일(백신 등) 때문에 절대 예외를 밖으로 던지지 않는다.
    /// </summary>
    public sealed class GitTempFileManager : IDisposable
    {
        private static readonly string BaseDir = Path.Combine(Path.GetTempPath(), "TextCompare", "git");

        private string _currentDir;

        public GitTempFileManager()
        {
            SweepStaleDirectories();
        }

        /// <summary>
        /// 원본 파일명을 유지한 새 임시 파일 경로를 만든다(디렉터리까지 생성).
        /// 이전에 만든 임시 디렉터리는 이 시점에 정리한다.
        /// </summary>
        public string CreateTempPathFor(string originalFileName)
        {
            CleanupCurrent();

            _currentDir = Path.Combine(BaseDir, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_currentDir);
            return Path.Combine(_currentDir, originalFileName);
        }

        /// <summary>현재 임시 디렉터리를 삭제한다(실패해도 무시).</summary>
        public void CleanupCurrent()
        {
            if (_currentDir == null) return;
            TryDeleteDirectory(_currentDir);
            _currentDir = null;
        }

        public void Dispose()
        {
            CleanupCurrent();
        }

        /// <summary>비정상 종료 등으로 남은 7일 이상 된 잔재 디렉터리를 청소한다.</summary>
        private static void SweepStaleDirectories()
        {
            try
            {
                if (!Directory.Exists(BaseDir)) return;
                DateTime cutoff = DateTime.UtcNow.AddDays(-7);
                foreach (string dir in Directory.GetDirectories(BaseDir))
                {
                    if (Directory.GetLastWriteTimeUtc(dir) < cutoff) TryDeleteDirectory(dir);
                }
            }
            catch (Exception)
            {
            }
        }

        private static void TryDeleteDirectory(string dir)
        {
            try
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
            catch (Exception)
            {
            }
        }
    }
}
