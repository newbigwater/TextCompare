using System;
using System.Drawing;
using System.Windows.Forms;

namespace TextCompare.Controls
{
    /// <summary>이전/다음 diff 이동 버튼과 diff 개수 표시를 담당하는 툴바.</summary>
    public partial class NavigationBarControl : UserControl
    {
        public event EventHandler PreviousClicked;
        public event EventHandler NextClicked;
        public event EventHandler EditModeToggled;
        public event EventHandler SaveClicked;

        public NavigationBarControl()
        {
            InitializeComponent();

            // Designer가 배치한 초기 좌표는 AutoSize 버튼의 실제 측정 폭을 반영하지 못하므로,
            // 생성 시점에 실제 폭 기준으로 한 번 더 체이닝 배치한다.
            _nextButton.Location = new Point(_prevButton.Right + 6, 4);
            _countLabel.Location = new Point(_nextButton.Right + 16, 10);
            RepositionTrailingControls();
        }

        private void PrevButton_Click(object sender, EventArgs e)
        {
            if (PreviousClicked != null) PreviousClicked(this, EventArgs.Empty);
        }

        private void NextButton_Click(object sender, EventArgs e)
        {
            if (NextClicked != null) NextClicked(this, EventArgs.Empty);
        }

        private void EditModeButton_Click(object sender, EventArgs e)
        {
            if (EditModeToggled != null) EditModeToggled(this, EventArgs.Empty);
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (SaveClicked != null) SaveClicked(this, EventArgs.Empty);
        }

        public void SetDiffCount(int currentIndex, int totalCount)
        {
            bool enabled = totalCount > 0;
            _prevButton.Enabled = enabled;
            _nextButton.Enabled = enabled;

            _countLabel.Text = totalCount == 0
                ? "차이 없음"
                : string.Format("차이 {0} / {1}", currentIndex + 1, totalCount);

            RepositionTrailingControls();
        }

        public void SetEditModeState(bool isEditMode, bool editingAllowed)
        {
            _editModeButton.Text = isEditMode ? "편집 종료(비교 보기)" : "편집 모드";
            _editModeButton.Enabled = editingAllowed;
            _saveButton.Enabled = isEditMode;
            RepositionTrailingControls();
        }

        public void SetDirty(bool dirty)
        {
            _dirtyLabel.Text = dirty ? "● 수정됨 (저장 필요)" : string.Empty;
            RepositionTrailingControls();
        }

        private void RepositionTrailingControls()
        {
            _editModeButton.Location = new Point(_countLabel.Right + 16, 4);
            _saveButton.Location = new Point(_editModeButton.Right + 6, 4);
            _dirtyLabel.Location = new Point(_saveButton.Right + 10, 10);
        }
    }
}
