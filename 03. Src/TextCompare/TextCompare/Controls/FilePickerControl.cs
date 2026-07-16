using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TextCompare.Controls
{
    /// <summary>좌/우 비교 대상 파일 경로 입력, 찾아보기, 비교 옵션 및 비교 실행 버튼을 담당하는 상단 바.</summary>
    public partial class FilePickerControl : UserControl
    {
        private const int FixedHeight = 96;

        public event EventHandler CompareRequested;
        public event EventHandler ExcludeFilterEditRequested;

        public FilePickerControl()
        {
            InitializeComponent();
            Height = FixedHeight;

            // Designer가 배치한 초기 좌표는 AutoSize 체크박스의 실제 측정 폭을 반영하지 못하므로,
            // 생성 시점에 실제 폭 기준으로 한 번 더 체이닝 배치한다(NavigationBarControl과 동일한 패턴).
            _excludeFilterCheck.Location = new Point(_ignoreWhitespaceCheck.Right + 16, 66);
            _excludeFilterEditButton.Location = new Point(_excludeFilterCheck.Right + 6, 63);

            Resize += (s, e) => LayoutRightAlignedControls();
            LayoutRightAlignedControls();

            DragDropHelper.WireFileDrop(this, files => AcceptDroppedFiles(files));
            DragDropHelper.WireFileDrop(_leftBox, files => { if (files.Length > 0) LeftPath = files[0]; });
            DragDropHelper.WireFileDrop(_rightBox, files => { if (files.Length > 0) RightPath = files[0]; });
        }

        private void ExcludeFilterEditButton_Click(object sender, EventArgs e)
        {
            if (ExcludeFilterEditRequested != null) ExcludeFilterEditRequested(this, EventArgs.Empty);
        }

        private void LeftBrowse_Click(object sender, EventArgs e)
        {
            BrowseFor(_leftBox);
        }

        private void RightBrowse_Click(object sender, EventArgs e)
        {
            BrowseFor(_rightBox);
        }

        private void CompareButton_Click(object sender, EventArgs e)
        {
            if (CompareRequested != null) CompareRequested(this, EventArgs.Empty);
        }

        public string LeftPath
        {
            get { return _leftBox.Text; }
            set { _leftBox.Text = value; }
        }

        public string RightPath
        {
            get { return _rightBox.Text; }
            set { _rightBox.Text = value; }
        }

        public bool IgnoreCase
        {
            get { return _ignoreCaseCheck.Checked; }
        }

        public bool IgnoreWhitespace
        {
            get { return _ignoreWhitespaceCheck.Checked; }
        }

        public bool ExcludeFilterEnabled
        {
            get { return _excludeFilterCheck.Checked; }
            set { _excludeFilterCheck.Checked = value; }
        }

        private void LayoutRightAlignedControls()
        {
            const int margin = 6;
            int rightEdge = ClientSize.Width - margin;

            _leftBrowse.Location = new Point(rightEdge - _leftBrowse.Width, 8);
            _rightBrowse.Location = new Point(rightEdge - _rightBrowse.Width, 36);
            _compareButton.Location = new Point(rightEdge - _compareButton.Width, 64);

            _leftBox.Width = Math.Max(40, _leftBrowse.Left - margin - _leftBox.Left);
            _rightBox.Width = Math.Max(40, _rightBrowse.Left - margin - _rightBox.Left);
        }

        /// <summary>
        /// 드롭된 파일 목록을 좌/우 입력란에 배분한다.
        /// 파일 2개 이상 드롭 시 좌/우에 각각 배정 후 바로 비교를 실행하고,
        /// 1개만 드롭되면 비어있는 쪽(둘 다 채워져 있으면 좌측)에 채운다.
        /// </summary>
        public void AcceptDroppedFiles(string[] paths)
        {
            if (paths == null) return;
            string[] files = paths.Where(File.Exists).ToArray();
            if (files.Length == 0) return;

            if (files.Length >= 2)
            {
                LeftPath = files[0];
                RightPath = files[1];
                if (CompareRequested != null) CompareRequested(this, EventArgs.Empty);
                return;
            }

            if (string.IsNullOrEmpty(LeftPath)) LeftPath = files[0];
            else if (string.IsNullOrEmpty(RightPath)) RightPath = files[0];
            else LeftPath = files[0];
        }

        private static void BrowseFor(TextBox target)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "모든 파일 (*.*)|*.*";
                dialog.CheckFileExists = true;
                if (!string.IsNullOrEmpty(target.Text) && System.IO.File.Exists(target.Text))
                {
                    dialog.InitialDirectory = System.IO.Path.GetDirectoryName(target.Text);
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    target.Text = dialog.FileName;
                }
            }
        }
    }
}
