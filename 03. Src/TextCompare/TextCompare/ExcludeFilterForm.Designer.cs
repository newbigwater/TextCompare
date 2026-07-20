namespace TextCompare
{
    partial class ExcludeFilterForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private System.Windows.Forms.TableLayoutPanel _root;
        private System.Windows.Forms.Label _infoLabel;
        private System.Windows.Forms.DataGridView _patternGrid;
        private System.Windows.Forms.DataGridViewTextBoxColumn _patternColumn;
        private System.Windows.Forms.DataGridViewComboBoxColumn _modeColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn _statusColumn;
        private System.Windows.Forms.TableLayoutPanel _buttonPanel;
        private System.Windows.Forms.Button _addButton;
        private System.Windows.Forms.Button _removeButton;
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _cancelButton;

        private void InitializeComponent()
        {
            this._root = new System.Windows.Forms.TableLayoutPanel();
            this._infoLabel = new System.Windows.Forms.Label();
            this._patternGrid = new System.Windows.Forms.DataGridView();
            this._patternColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._modeColumn = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this._statusColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._buttonPanel = new System.Windows.Forms.TableLayoutPanel();
            this._addButton = new System.Windows.Forms.Button();
            this._removeButton = new System.Windows.Forms.Button();
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this._patternGrid)).BeginInit();
            this._root.SuspendLayout();
            this._buttonPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // _root
            //
            this._root.Dock = System.Windows.Forms.DockStyle.Fill;
            this._root.ColumnCount = 1;
            this._root.RowCount = 3;
            this._root.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this._root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this._root.Padding = new System.Windows.Forms.Padding(10);
            //
            // _infoLabel
            //
            this._infoLabel.AutoSize = true;
            this._infoLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this._infoLabel.Text = "라인 전체 제외: 패턴에 일치하는 줄을 비교·화면에서 통째로 제거합니다.\r\n" +
                "매치 부분만 제외: 줄은 그대로 두고, 일치한 구간만 비교에서 무시합니다(회색 배경으로 표시).";
            this._infoLabel.Padding = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this._root.Controls.Add(this._infoLabel, 0, 0);
            //
            // _patternGrid
            //
            this._patternGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._patternGrid.AllowUserToAddRows = false;
            this._patternGrid.AllowUserToDeleteRows = false;
            this._patternGrid.AutoGenerateColumns = false;
            this._patternGrid.RowHeadersVisible = false;
            this._patternGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._patternGrid.MultiSelect = false;
            this._patternGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this._patternColumn,
            this._modeColumn,
            this._statusColumn});
            this._patternGrid.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.PatternGrid_CellEndEdit);
            this._root.Controls.Add(this._patternGrid, 0, 1);
            //
            // _patternColumn
            //
            this._patternColumn.HeaderText = "패턴 (정규식)";
            this._patternColumn.Name = "_patternColumn";
            this._patternColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            //
            // _modeColumn
            //
            this._modeColumn.HeaderText = "모드";
            this._modeColumn.Name = "_modeColumn";
            this._modeColumn.Items.AddRange(new object[] {
            "라인 전체 제외",
            "매치 부분만 제외"});
            this._modeColumn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._modeColumn.Width = 130;
            //
            // _statusColumn
            //
            this._statusColumn.HeaderText = "상태";
            this._statusColumn.Name = "_statusColumn";
            this._statusColumn.ReadOnly = true;
            this._statusColumn.Width = 160;
            //
            // _buttonPanel
            //
            this._buttonPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this._buttonPanel.ColumnCount = 5;
            this._buttonPanel.RowCount = 1;
            this._buttonPanel.Margin = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this._buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this._buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this._buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this._buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            //
            // _addButton
            //
            this._addButton.AutoSize = true;
            this._addButton.Text = "추가";
            this._addButton.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this._addButton.Click += new System.EventHandler(this.AddButton_Click);
            this._buttonPanel.Controls.Add(this._addButton, 0, 0);
            //
            // _removeButton
            //
            this._removeButton.AutoSize = true;
            this._removeButton.Text = "삭제";
            this._removeButton.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this._removeButton.Click += new System.EventHandler(this.RemoveButton_Click);
            this._buttonPanel.Controls.Add(this._removeButton, 1, 0);
            //
            // _okButton
            //
            this._okButton.AutoSize = true;
            this._okButton.Text = "확인";
            this._okButton.Margin = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this._okButton.Click += new System.EventHandler(this.OkButton_Click);
            this._buttonPanel.Controls.Add(this._okButton, 3, 0);
            //
            // _cancelButton
            //
            this._cancelButton.AutoSize = true;
            this._cancelButton.Text = "취소";
            this._cancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            this._buttonPanel.Controls.Add(this._cancelButton, 4, 0);
            //
            // _root (buttonPanel row)
            //
            this._root.Controls.Add(this._buttonPanel, 0, 2);
            //
            // ExcludeFilterForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(560, 380);
            this.MinimumSize = new System.Drawing.Size(420, 260);
            this.Controls.Add(this._root);
            this.Name = "ExcludeFilterForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "비교 제외 필터";
            ((System.ComponentModel.ISupportInitialize)(this._patternGrid)).EndInit();
            this._buttonPanel.ResumeLayout(false);
            this._buttonPanel.PerformLayout();
            this._root.ResumeLayout(false);
            this._root.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
