using System;
using System.Windows.Forms;

namespace TextCompare.Controls
{
    /// <summary>파일 드래그앤드롭을 받는 컨트롤에 공통으로 적용하는 배선 헬퍼.</summary>
    internal static class DragDropHelper
    {
        public static void WireFileDrop(Control control, Action<string[]> onDrop)
        {
            control.AllowDrop = true;
            control.DragEnter += (s, e) =>
            {
                e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            };
            control.DragDrop += (s, e) =>
            {
                if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
                onDrop((string[])e.Data.GetData(DataFormats.FileDrop));
            };
        }
    }
}
