namespace TextCompare
{
    partial class CompareProgressForm
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
        private System.Windows.Forms.Label _statusLabel;
        private System.Windows.Forms.ProgressBar _progressBar;

        private void InitializeComponent()
        {
            this._root = new System.Windows.Forms.TableLayoutPanel();
            this._statusLabel = new System.Windows.Forms.Label();
            this._progressBar = new System.Windows.Forms.ProgressBar();
            this._root.SuspendLayout();
            this.SuspendLayout();
            //
            // _root
            //
            this._root.Dock = System.Windows.Forms.DockStyle.Fill;
            this._root.ColumnCount = 1;
            this._root.RowCount = 2;
            this._root.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this._root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this._root.Padding = new System.Windows.Forms.Padding(16);
            //
            // _statusLabel
            //
            this._statusLabel.AutoSize = true;
            this._statusLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this._statusLabel.Text = "준비 중...";
            this._statusLabel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this._root.Controls.Add(this._statusLabel, 0, 0);
            //
            // _progressBar
            //
            this._progressBar.Dock = System.Windows.Forms.DockStyle.Top;
            this._progressBar.Minimum = 0;
            this._progressBar.Maximum = 100;
            this._progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this._root.Controls.Add(this._progressBar, 0, 1);
            //
            // CompareProgressForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(420, 100);
            this.ControlBox = false;
            this.Controls.Add(this._root);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CompareProgressForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "비교 진행 중...";
            this._root.ResumeLayout(false);
            this._root.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
