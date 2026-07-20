using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TextCompare.Core;

namespace TextCompare
{
    /// <summary>비교 제외 정규식 패턴 목록을 추가/삭제/편집하는 모달 다이얼로그.</summary>
    public partial class ExcludeFilterForm : Form
    {
        private static readonly Color ErrorColor = Color.FromArgb(180, 0, 0);
        private static readonly Color OkColor = Color.FromArgb(0, 120, 0);

        // _modeColumn.Items에 등록된 표시 문자열과 반드시 일치해야 한다(불일치 시 DataGridView가 DataError를 던진다).
        private const string ModeLineDisplay = "라인 전체 제외";
        private const string ModeMaskDisplay = "매치 부분만 제외";

        public IReadOnlyList<ExcludeFilterPattern> ResultPatterns { get; private set; }

        public ExcludeFilterForm(IEnumerable<ExcludeFilterPattern> initialPatterns)
        {
            InitializeComponent();

            if (initialPatterns != null)
            {
                foreach (ExcludeFilterPattern pattern in initialPatterns)
                {
                    int rowIndex = _patternGrid.Rows.Add(pattern.Pattern, ModeToDisplay(pattern.Mode), string.Empty);
                    RefreshStatus(rowIndex);
                }
            }
        }

        private static string ModeToDisplay(ExcludeFilterMode mode)
        {
            return mode == ExcludeFilterMode.MaskMatch ? ModeMaskDisplay : ModeLineDisplay;
        }

        private static ExcludeFilterMode DisplayToMode(string display)
        {
            return display == ModeMaskDisplay ? ExcludeFilterMode.MaskMatch : ExcludeFilterMode.ExcludeLine;
        }

        private void PatternGrid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == _patternColumn.Index)
            {
                RefreshStatus(e.RowIndex);
            }
        }

        private void RefreshStatus(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _patternGrid.Rows.Count) return;

            DataGridViewRow row = _patternGrid.Rows[rowIndex];
            string pattern = Convert.ToString(row.Cells[_patternColumn.Index].Value);
            DataGridViewCell statusCell = row.Cells[_statusColumn.Index];

            if (string.IsNullOrEmpty(pattern))
            {
                statusCell.Value = string.Empty;
                statusCell.Style.ForeColor = _patternGrid.DefaultCellStyle.ForeColor;
                return;
            }

            var probe = new ExcludeFilterPattern(pattern);
            if (probe.IsValid)
            {
                statusCell.Value = "OK";
                statusCell.Style.ForeColor = OkColor;
            }
            else
            {
                statusCell.Value = probe.ErrorMessage;
                statusCell.Style.ForeColor = ErrorColor;
            }
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            int rowIndex = _patternGrid.Rows.Add(string.Empty, ModeLineDisplay, string.Empty);
            _patternGrid.CurrentCell = _patternGrid.Rows[rowIndex].Cells[_patternColumn.Index];
            _patternGrid.BeginEdit(true);
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in _patternGrid.SelectedRows.Cast<DataGridViewRow>().ToList())
            {
                _patternGrid.Rows.Remove(row);
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (_patternGrid.IsCurrentCellInEditMode)
            {
                _patternGrid.EndEdit();
            }

            var patterns = new List<ExcludeFilterPattern>();
            foreach (DataGridViewRow row in _patternGrid.Rows)
            {
                string pattern = Convert.ToString(row.Cells[_patternColumn.Index].Value);
                if (!string.IsNullOrEmpty(pattern))
                {
                    ExcludeFilterMode mode = DisplayToMode(Convert.ToString(row.Cells[_modeColumn.Index].Value));
                    patterns.Add(new ExcludeFilterPattern(pattern, mode));
                }
            }

            ResultPatterns = patterns;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
