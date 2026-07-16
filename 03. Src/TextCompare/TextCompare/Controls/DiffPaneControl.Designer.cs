namespace TextCompare.Controls
{
    partial class DiffPaneControl
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

        private void InitializeComponent()
        {
            this.SuspendLayout();
            //
            // DiffPaneControl
            //
            this.BackColor = System.Drawing.Color.White;
            this.Name = "DiffPaneControl";
            this.Size = new System.Drawing.Size(400, 300);
            this.ResumeLayout(false);
        }
    }
}
