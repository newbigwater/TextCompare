namespace TextCompare.Controls
{
    partial class NavigationBarControl
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

        private System.Windows.Forms.Button _prevButton;
        private System.Windows.Forms.Button _nextButton;
        private System.Windows.Forms.Label _countLabel;
        private System.Windows.Forms.Button _editModeButton;
        private System.Windows.Forms.Button _saveButton;
        private System.Windows.Forms.Label _dirtyLabel;

        private void InitializeComponent()
        {
            this._prevButton = new System.Windows.Forms.Button();
            this._nextButton = new System.Windows.Forms.Button();
            this._countLabel = new System.Windows.Forms.Label();
            this._editModeButton = new System.Windows.Forms.Button();
            this._saveButton = new System.Windows.Forms.Button();
            this._dirtyLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // _prevButton
            //
            this._prevButton.AutoSize = true;
            this._prevButton.Location = new System.Drawing.Point(6, 4);
            this._prevButton.Name = "_prevButton";
            this._prevButton.Text = "◀ 이전 차이";
            this._prevButton.UseVisualStyleBackColor = true;
            this._prevButton.Click += new System.EventHandler(this.PrevButton_Click);
            //
            // _nextButton
            //
            this._nextButton.AutoSize = true;
            this._nextButton.Location = new System.Drawing.Point(88, 4);
            this._nextButton.Name = "_nextButton";
            this._nextButton.Text = "다음 차이 ▶";
            this._nextButton.UseVisualStyleBackColor = true;
            this._nextButton.Click += new System.EventHandler(this.NextButton_Click);
            //
            // _countLabel
            //
            this._countLabel.AutoSize = true;
            this._countLabel.Location = new System.Drawing.Point(186, 10);
            this._countLabel.Name = "_countLabel";
            this._countLabel.Text = "차이 없음";
            //
            // _editModeButton
            //
            this._editModeButton.AutoSize = true;
            this._editModeButton.Location = new System.Drawing.Point(258, 4);
            this._editModeButton.Name = "_editModeButton";
            this._editModeButton.Text = "편집 모드";
            this._editModeButton.UseVisualStyleBackColor = true;
            this._editModeButton.Click += new System.EventHandler(this.EditModeButton_Click);
            //
            // _saveButton
            //
            this._saveButton.AutoSize = true;
            this._saveButton.Enabled = false;
            this._saveButton.Location = new System.Drawing.Point(340, 4);
            this._saveButton.Name = "_saveButton";
            this._saveButton.Text = "저장";
            this._saveButton.UseVisualStyleBackColor = true;
            this._saveButton.Click += new System.EventHandler(this.SaveButton_Click);
            //
            // _dirtyLabel
            //
            this._dirtyLabel.AutoSize = true;
            this._dirtyLabel.ForeColor = System.Drawing.Color.FromArgb(200, 90, 0);
            this._dirtyLabel.Location = new System.Drawing.Point(400, 10);
            this._dirtyLabel.Name = "_dirtyLabel";
            this._dirtyLabel.Text = "";
            //
            // NavigationBarControl
            //
            this.Controls.Add(this._prevButton);
            this.Controls.Add(this._nextButton);
            this.Controls.Add(this._countLabel);
            this.Controls.Add(this._editModeButton);
            this.Controls.Add(this._saveButton);
            this.Controls.Add(this._dirtyLabel);
            this.Name = "NavigationBarControl";
            this.Size = new System.Drawing.Size(800, 32);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
