namespace TextCompare.Controls
{
    partial class PreviewBarControl
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

        private System.Windows.Forms.SplitContainer _split;
        private System.Windows.Forms.RichTextBox _leftBox;
        private System.Windows.Forms.RichTextBox _rightBox;

        private void InitializeComponent()
        {
            this._split = new System.Windows.Forms.SplitContainer();
            this._leftBox = new System.Windows.Forms.RichTextBox();
            this._rightBox = new System.Windows.Forms.RichTextBox();
            ((System.ComponentModel.ISupportInitialize)(this._split)).BeginInit();
            this._split.Panel1.SuspendLayout();
            this._split.Panel2.SuspendLayout();
            this._split.SuspendLayout();
            this.SuspendLayout();
            //
            // _split
            //
            this._split.Dock = System.Windows.Forms.DockStyle.Fill;
            this._split.FixedPanel = System.Windows.Forms.FixedPanel.None;
            this._split.Location = new System.Drawing.Point(0, 0);
            this._split.Name = "_split";
            // 메인 비교 뷰와 동일하게 Left는 상단, Right는 하단에 표시한다.
            this._split.Orientation = System.Windows.Forms.Orientation.Horizontal;
            //
            // _split.Panel1
            //
            this._split.Panel1.Controls.Add(this._leftBox);
            //
            // _split.Panel2
            //
            this._split.Panel2.Controls.Add(this._rightBox);
            this._split.Size = new System.Drawing.Size(800, 160);
            this._split.SplitterWidth = 4;
            this._split.TabIndex = 0;
            //
            // _leftBox
            //
            this._leftBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._leftBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this._leftBox.Font = DiffPaneControl.MonoFont;
            this._leftBox.Location = new System.Drawing.Point(0, 0);
            this._leftBox.Name = "_leftBox";
            this._leftBox.ReadOnly = true;
            this._leftBox.TabStop = false;
            this._leftBox.Text = "";
            this._leftBox.WordWrap = false;
            //
            // _rightBox
            //
            this._rightBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._rightBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this._rightBox.Font = DiffPaneControl.MonoFont;
            this._rightBox.Location = new System.Drawing.Point(0, 0);
            this._rightBox.Name = "_rightBox";
            this._rightBox.ReadOnly = true;
            this._rightBox.TabStop = false;
            this._rightBox.Text = "";
            this._rightBox.WordWrap = false;
            //
            // PreviewBarControl
            //
            this.Controls.Add(this._split);
            this.Name = "PreviewBarControl";
            this.Size = new System.Drawing.Size(800, 160);
            this._split.Panel1.ResumeLayout(false);
            this._split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._split)).EndInit();
            this._split.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
