namespace TextCompare.Controls
{
    partial class DiffViewerControl
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
        private DiffPaneControl _leftPane;
        private DiffPaneControl _rightPane;
        private System.Windows.Forms.RichTextBox _leftEditor;
        private System.Windows.Forms.RichTextBox _rightEditor;
        private System.Windows.Forms.VScrollBar _vScroll;
        private System.Windows.Forms.HScrollBar _hScroll;
        private System.Windows.Forms.Timer _debounceTimer;

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this._split = new System.Windows.Forms.SplitContainer();
            this._leftEditor = new System.Windows.Forms.RichTextBox();
            this._leftPane = new TextCompare.Controls.DiffPaneControl();
            this._rightEditor = new System.Windows.Forms.RichTextBox();
            this._rightPane = new TextCompare.Controls.DiffPaneControl();
            this._vScroll = new System.Windows.Forms.VScrollBar();
            this._hScroll = new System.Windows.Forms.HScrollBar();
            this._debounceTimer = new System.Windows.Forms.Timer(this.components);
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
            // 메인 비교 뷰는 Left/Right를 좌우로 나란히 보여준다(Vertical = 세로 분할선).
            this._split.Orientation = System.Windows.Forms.Orientation.Vertical;
            //
            // _split.Panel1
            //
            this._split.Panel1.Controls.Add(this._leftEditor);
            this._split.Panel1.Controls.Add(this._leftPane);
            //
            // _split.Panel2
            //
            this._split.Panel2.Controls.Add(this._rightEditor);
            this._split.Panel2.Controls.Add(this._rightPane);
            this._split.Size = new System.Drawing.Size(800, 450);
            this._split.SplitterWidth = 4;
            this._split.TabIndex = 0;
            //
            // _leftEditor
            //
            this._leftEditor.AcceptsTab = true;
            this._leftEditor.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._leftEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this._leftEditor.Font = DiffPaneControl.MonoFont;
            this._leftEditor.HideSelection = false;
            this._leftEditor.Location = new System.Drawing.Point(0, 0);
            this._leftEditor.Name = "_leftEditor";
            this._leftEditor.Text = "";
            this._leftEditor.Visible = false;
            this._leftEditor.WordWrap = false;
            this._leftEditor.TextChanged += new System.EventHandler(this.OnEditorTextChanged);
            //
            // _leftPane
            //
            this._leftPane.Dock = System.Windows.Forms.DockStyle.Fill;
            this._leftPane.Location = new System.Drawing.Point(0, 0);
            this._leftPane.Name = "_leftPane";
            this._leftPane.Side = TextCompare.Controls.PaneSide.Left;
            this._leftPane.BlockClicked += new System.EventHandler<int>(this.OnPaneBlockClicked);
            this._leftPane.ScrollDeltaRequested += new System.EventHandler<int>(this.OnPaneScrollDeltaRequested);
            //
            // _rightEditor
            //
            this._rightEditor.AcceptsTab = true;
            this._rightEditor.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._rightEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this._rightEditor.Font = DiffPaneControl.MonoFont;
            this._rightEditor.HideSelection = false;
            this._rightEditor.Location = new System.Drawing.Point(0, 0);
            this._rightEditor.Name = "_rightEditor";
            this._rightEditor.Text = "";
            this._rightEditor.Visible = false;
            this._rightEditor.WordWrap = false;
            this._rightEditor.TextChanged += new System.EventHandler(this.OnEditorTextChanged);
            //
            // _rightPane
            //
            this._rightPane.Dock = System.Windows.Forms.DockStyle.Fill;
            this._rightPane.Location = new System.Drawing.Point(0, 0);
            this._rightPane.Name = "_rightPane";
            this._rightPane.Side = TextCompare.Controls.PaneSide.Right;
            this._rightPane.BlockClicked += new System.EventHandler<int>(this.OnPaneBlockClicked);
            this._rightPane.ScrollDeltaRequested += new System.EventHandler<int>(this.OnPaneScrollDeltaRequested);
            //
            // _vScroll
            //
            this._vScroll.Dock = System.Windows.Forms.DockStyle.Right;
            this._vScroll.Location = new System.Drawing.Point(796, 0);
            this._vScroll.Name = "_vScroll";
            this._vScroll.Size = new System.Drawing.Size(4, 450);
            this._vScroll.TabIndex = 1;
            this._vScroll.ValueChanged += new System.EventHandler(this.OnVScrollValueChanged);
            //
            // _hScroll
            //
            this._hScroll.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._hScroll.Location = new System.Drawing.Point(0, 446);
            this._hScroll.Name = "_hScroll";
            this._hScroll.Size = new System.Drawing.Size(800, 4);
            this._hScroll.TabIndex = 2;
            this._hScroll.ValueChanged += new System.EventHandler(this.OnHScrollValueChanged);
            //
            // _debounceTimer
            //
            this._debounceTimer.Interval = 600;
            this._debounceTimer.Tick += new System.EventHandler(this.DebounceTimer_Tick);
            //
            // DiffViewerControl
            //
            this.Controls.Add(this._vScroll);
            this.Controls.Add(this._hScroll);
            this.Controls.Add(this._split);
            this.Name = "DiffViewerControl";
            this.Size = new System.Drawing.Size(800, 450);
            this.Resize += new System.EventHandler(this.DiffViewerControl_Resize);
            this._split.Panel1.ResumeLayout(false);
            this._split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._split)).EndInit();
            this._split.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
