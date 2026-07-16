namespace TextCompare.Controls
{
    partial class FilePickerControl
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

        private System.Windows.Forms.Label _leftLabel;
        private System.Windows.Forms.Label _rightLabel;
        private System.Windows.Forms.TextBox _leftBox;
        private System.Windows.Forms.TextBox _rightBox;
        private System.Windows.Forms.Button _leftBrowse;
        private System.Windows.Forms.Button _rightBrowse;
        private System.Windows.Forms.Button _compareButton;
        private System.Windows.Forms.CheckBox _ignoreCaseCheck;
        private System.Windows.Forms.CheckBox _ignoreWhitespaceCheck;

        private void InitializeComponent()
        {
            this._leftLabel = new System.Windows.Forms.Label();
            this._rightLabel = new System.Windows.Forms.Label();
            this._leftBox = new System.Windows.Forms.TextBox();
            this._rightBox = new System.Windows.Forms.TextBox();
            this._leftBrowse = new System.Windows.Forms.Button();
            this._rightBrowse = new System.Windows.Forms.Button();
            this._ignoreCaseCheck = new System.Windows.Forms.CheckBox();
            this._ignoreWhitespaceCheck = new System.Windows.Forms.CheckBox();
            this._compareButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // _leftLabel
            //
            this._leftLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this._leftLabel.Location = new System.Drawing.Point(6, 10);
            this._leftLabel.Name = "_leftLabel";
            this._leftLabel.Size = new System.Drawing.Size(100, 20);
            this._leftLabel.Text = "Left(As-Is):";
            //
            // _rightLabel
            //
            this._rightLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this._rightLabel.Location = new System.Drawing.Point(6, 38);
            this._rightLabel.Name = "_rightLabel";
            this._rightLabel.Size = new System.Drawing.Size(100, 20);
            this._rightLabel.Text = "Right(To-Be):";
            //
            // _leftBox
            //
            this._leftBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this._leftBox.Location = new System.Drawing.Point(112, 8);
            this._leftBox.Name = "_leftBox";
            this._leftBox.Size = new System.Drawing.Size(300, 21);
            //
            // _rightBox
            //
            this._rightBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this._rightBox.Location = new System.Drawing.Point(112, 36);
            this._rightBox.Name = "_rightBox";
            this._rightBox.Size = new System.Drawing.Size(300, 21);
            //
            // _leftBrowse
            //
            this._leftBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._leftBrowse.Location = new System.Drawing.Point(420, 8);
            this._leftBrowse.Name = "_leftBrowse";
            this._leftBrowse.Size = new System.Drawing.Size(84, 23);
            this._leftBrowse.Text = "찾아보기...";
            this._leftBrowse.UseVisualStyleBackColor = true;
            this._leftBrowse.Click += new System.EventHandler(this.LeftBrowse_Click);
            //
            // _rightBrowse
            //
            this._rightBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._rightBrowse.Location = new System.Drawing.Point(420, 36);
            this._rightBrowse.Name = "_rightBrowse";
            this._rightBrowse.Size = new System.Drawing.Size(84, 23);
            this._rightBrowse.Text = "찾아보기...";
            this._rightBrowse.UseVisualStyleBackColor = true;
            this._rightBrowse.Click += new System.EventHandler(this.RightBrowse_Click);
            //
            // _ignoreCaseCheck
            //
            this._ignoreCaseCheck.AutoSize = true;
            this._ignoreCaseCheck.Location = new System.Drawing.Point(112, 66);
            this._ignoreCaseCheck.Name = "_ignoreCaseCheck";
            this._ignoreCaseCheck.Text = "대소문자 무시";
            this._ignoreCaseCheck.UseVisualStyleBackColor = true;
            //
            // _ignoreWhitespaceCheck
            //
            this._ignoreWhitespaceCheck.AutoSize = true;
            this._ignoreWhitespaceCheck.Location = new System.Drawing.Point(232, 66);
            this._ignoreWhitespaceCheck.Name = "_ignoreWhitespaceCheck";
            this._ignoreWhitespaceCheck.Text = "공백 무시";
            this._ignoreWhitespaceCheck.UseVisualStyleBackColor = true;
            //
            // _compareButton
            //
            this._compareButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._compareButton.Location = new System.Drawing.Point(420, 64);
            this._compareButton.Name = "_compareButton";
            this._compareButton.Size = new System.Drawing.Size(84, 26);
            this._compareButton.Text = "비교";
            this._compareButton.UseVisualStyleBackColor = true;
            this._compareButton.Click += new System.EventHandler(this.CompareButton_Click);
            //
            // FilePickerControl
            //
            this.Controls.Add(this._leftLabel);
            this.Controls.Add(this._rightLabel);
            this.Controls.Add(this._leftBox);
            this.Controls.Add(this._rightBox);
            this.Controls.Add(this._leftBrowse);
            this.Controls.Add(this._rightBrowse);
            this.Controls.Add(this._ignoreCaseCheck);
            this.Controls.Add(this._ignoreWhitespaceCheck);
            this.Controls.Add(this._compareButton);
            this.Name = "FilePickerControl";
            this.Size = new System.Drawing.Size(800, 96);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
