using System.Collections.Generic;

namespace TextCompare.Core
{
    /// <summary>
    /// 텍스트를 라인 배열로 분리한다. 파일 최초 로드(MainForm)와 편집 모드 실시간 재비교(DiffViewerControl)가
    /// 동일한 규칙을 공유해야 두 경로 사이에 가짜 diff(트레일링 개행 처리 차이 등)가 생기지 않는다.
    /// </summary>
    public static class LineSplitter
    {
        public static List<string> Split(string text)
        {
            string normalized = text.Replace("\r\n", "\n").Replace("\r", "\n");
            string[] parts = normalized.Split('\n');
            var list = new List<string>(parts);

            if (list.Count > 0 && list[list.Count - 1].Length == 0 && normalized.EndsWith("\n"))
            {
                list.RemoveAt(list.Count - 1);
            }

            return list;
        }
    }
}
