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
        public event EventHandler GitCompareWithHeadRequested;
        public event EventHandler GitRegisterRequested;
        public event EventHandler GitUnregisterRequested;

        // 라벨 표시 모드일 때 실제 경로를 보관한다(텍스트박스에는 라벨이 표시됨).
        private string _leftPathOverride;
        private string _rightPathOverride;

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

        private void GitButton_Click(object sender, EventArgs e)
        {
            _gitMenu.Show(_gitButton, new Point(0, _gitButton.Height));
        }

        private void GitCompareHeadItem_Click(object sender, EventArgs e)
        {
            if (GitCompareWithHeadRequested != null) GitCompareWithHeadRequested(this, EventArgs.Empty);
        }

        private void GitRegisterItem_Click(object sender, EventArgs e)
        {
            if (GitRegisterRequested != null) GitRegisterRequested(this, EventArgs.Empty);
        }

        private void GitUnregisterItem_Click(object sender, EventArgs e)
        {
            if (GitUnregisterRequested != null) GitUnregisterRequested(this, EventArgs.Empty);
        }

        private void LeftBrowse_Click(object sender, EventArgs e)
        {
            string selected = BrowseFor(LeftPath);
            if (selected != null) LeftPath = selected; // 프로퍼티 경유로 라벨 표시 모드도 해제
        }

        private void RightBrowse_Click(object sender, EventArgs e)
        {
            string selected = BrowseFor(RightPath);
            if (selected != null) RightPath = selected;
        }

        private void CompareButton_Click(object sender, EventArgs e)
        {
            if (CompareRequested != null) CompareRequested(this, EventArgs.Empty);
        }

        public string LeftPath
        {
            get { return _leftPathOverride ?? _leftBox.Text; }
            set
            {
                _leftPathOverride = null;
                _leftBox.ReadOnly = false;
                _leftBox.Text = value;
            }
        }

        public string RightPath
        {
            get { return _rightPathOverride ?? _rightBox.Text; }
            set
            {
                _rightPathOverride = null;
                _rightBox.ReadOnly = false;
                _rightBox.Text = value;
            }
        }

        /// <summary>현재 라벨 표시 모드일 때의 좌측 라벨(없으면 null).</summary>
        public string LeftLabel
        {
            get { return _leftPathOverride != null ? _leftBox.Text : null; }
        }

        /// <summary>현재 라벨 표시 모드일 때의 우측 라벨(없으면 null).</summary>
        public string RightLabel
        {
            get { return _rightPathOverride != null ? _rightBox.Text : null; }
        }

        /// <summary>
        /// 비교 대상 경로를 지정한다. displayLabel이 있으면 텍스트박스에는 라벨을 표시하고
        /// 실제 경로는 감춘다(git difftool의 임시 경로 은닉용). 라벨 모드의 텍스트박스는 읽기 전용이 되며,
        /// 이후 사용자가 찾아보기/드롭/직접 입력으로 경로를 바꾸면 자동으로 일반 모드로 돌아온다.
        /// </summary>
        public void SetSource(bool isLeft, string path, string displayLabel)
        {
            TextBox box = isLeft ? _leftBox : _rightBox;

            if (string.IsNullOrEmpty(displayLabel))
            {
                if (isLeft) LeftPath = path;
                else RightPath = path;
                return;
            }

            if (isLeft) _leftPathOverride = path;
            else _rightPathOverride = path;
            box.Text = displayLabel;
            box.ReadOnly = true;
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
            _gitButton.Location = new Point(_compareButton.Left - margin - _gitButton.Width, 64);

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

        private static string BrowseFor(string currentPath)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "모든 파일 (*.*)|*.*";
                dialog.CheckFileExists = true;
                if (!string.IsNullOrEmpty(currentPath) && System.IO.File.Exists(currentPath))
                {
                    dialog.InitialDirectory = System.IO.Path.GetDirectoryName(currentPath);
                }

                return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
            }
        }
    }
}
