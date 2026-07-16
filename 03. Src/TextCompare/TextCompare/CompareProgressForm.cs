using System.Windows.Forms;

namespace TextCompare
{
    /// <summary>
    /// 비교 연산 중 진행 사항(단계 설명)과 진행율(%)을 보여주는 모달 진행 창.
    /// 실제 완료된 단계에서만 값을 갱신한다(타이머 기반의 가짜 애니메이션이 아님) — MainForm이
    /// BackgroundWorker.ReportProgress를 통해 실제 계산 단계가 끝날 때마다 UpdateProgress를 호출한다.
    /// </summary>
    public partial class CompareProgressForm : Form
    {
        public CompareProgressForm()
        {
            InitializeComponent();
        }

        public void UpdateProgress(int percent, string status)
        {
            int clamped = percent < 0 ? 0 : (percent > 100 ? 100 : percent);
            _progressBar.Value = clamped;
            _statusLabel.Text = string.Format("{0} ({1}%)", status, clamped);
        }
    }
}
