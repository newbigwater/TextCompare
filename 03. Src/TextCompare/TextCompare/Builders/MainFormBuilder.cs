using System;
using System.Windows.Forms;
using TextCompare.Controls;

namespace TextCompare.Builders
{
    /// <summary>
    /// MainForm에 UserControl들을 단계적으로 조립하는 빌더.
    /// 각 Add* 메서드는 자기 자신을 반환하여 체이닝 방식으로 레이어를 쌓아 올릴 수 있다.
    ///
    /// 최상위 배치는 TableLayoutPanel의 고정된 행(Row)에 컨트롤을 배정하는 방식을 사용한다.
    /// Dock=Top/Bottom 컨트롤을 Controls.Add() 순서에 의존해 쌓으면, 추가 순서가 꼬이거나
    /// 다른 개발자가 순서를 바꾸는 순간 상단 바가 본문 영역을 덮어써 버리는 버그가 재현되기 쉽다
    /// (실제로 이 문제가 발생해 1~7번째 줄이 화면에 보이지 않는 버그로 나타난 적이 있다).
    /// TableLayoutPanel은 각 행이 서로 겹치지 않는 영역을 명시적으로 보장하므로 이 클래스의 구조적 근본 해결책이다.
    /// </summary>
    public sealed class MainFormBuilder
    {
        private readonly Form _form;
        private readonly TableLayoutPanel _root;

        private const int FilePickerRow = 0;
        private const int NavigationBarRow = 1;
        private const int ComparisonAreaRow = 2;
        private const int StatusBarRow = 3;

        public FilePickerControl FilePicker { get; private set; }
        public NavigationBarControl NavigationBar { get; private set; }
        public DiffViewerControl Viewer { get; private set; }
        public LocationPaneControl LocationPane { get; private set; }
        public PreviewBarControl PreviewBar { get; private set; }
        public StatusStrip StatusBar { get; private set; }
        public ToolStripStatusLabel StatusLabel { get; private set; }

        public MainFormBuilder(Form form)
        {
            _form = form;

            _root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4
            };
            _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            // FilePicker/NavigationBar는 자기 컨트롤의 실제 Height를 그대로 행 높이로 사용한다(접기/펼치기 시 자동 반영).
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            // 비교 영역만 남는 공간을 모두 차지한다.
            _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _form.Controls.Add(_root);
        }

        public MainFormBuilder AddFilePicker()
        {
            FilePicker = new FilePickerControl { Dock = DockStyle.Fill };
            _root.Controls.Add(FilePicker, 0, FilePickerRow);
            return this;
        }

        public MainFormBuilder AddNavigationBar()
        {
            NavigationBar = new NavigationBarControl { Dock = DockStyle.Fill };
            _root.Controls.Add(NavigationBar, 0, NavigationBarRow);
            return this;
        }

        public MainFormBuilder AddStatusBar()
        {
            StatusBar = new StatusStrip { Dock = DockStyle.Fill };
            StatusLabel = new ToolStripStatusLabel { Text = "파일을 선택하세요." };
            StatusBar.Items.Add(StatusLabel);
            _root.Controls.Add(StatusBar, 0, StatusBarRow);
            return this;
        }

        /// <summary>
        /// 메인 비교 뷰(위)와 하단 미리보기 패널(아래)을 사용자가 드래그로 크기 조절할 수 있는
        /// 가로 SplitContainer로 묶는다. 이전엔 미리보기 패널이 고정 높이(Dock=Bottom)라
        /// 툴바+미리보기가 화면을 너무 많이 차지해도 조절할 방법이 없었다.
        /// </summary>
        public MainFormBuilder AddComparisonArea()
        {
            SplitContainer mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 4,
                FixedPanel = FixedPanel.Panel2,
                Panel1MinSize = 120,
                Panel2MinSize = 40
            };

            Panel viewerContainer = new Panel { Dock = DockStyle.Fill };
            // WinMerge 위치 창은 파일 내용 좌측에 위치한다(요청된 참고 화면 기준).
            LocationPane = new LocationPaneControl { Dock = DockStyle.Left, Width = 26 };
            Viewer = new DiffViewerControl { Dock = DockStyle.Fill };
            // Dock 규칙: Left로 먼저 추가해 좌측 폭을 확보한 뒤, Fill 컨트롤을 마지막에 추가해 나머지 공간을 채운다.
            viewerContainer.Controls.Add(LocationPane);
            viewerContainer.Controls.Add(Viewer);

            PreviewBar = new PreviewBarControl { Dock = DockStyle.Fill };

            mainSplit.Panel1.Controls.Add(viewerContainer);
            mainSplit.Panel2.Controls.Add(PreviewBar);

            _root.Controls.Add(mainSplit, 0, ComparisonAreaRow);

            // 생성 시점엔 mainSplit.Height가 실제 최종 크기를 아직 모르므로(레이아웃 전),
            // 처음으로 실제 크기가 잡히는 순간에 한 번만 미리보기 패널에 적당한 초기 높이(~160px)를 배정한다.
            // 그 이후는 사용자가 드래그로 자유롭게 조절.
            bool initialized = false;
            mainSplit.Layout += (s, e) =>
            {
                if (initialized || mainSplit.Height <= 0) return;
                int desiredPreviewHeight = 160;
                mainSplit.SplitterDistance = Math.Max(mainSplit.Panel1MinSize, mainSplit.Height - desiredPreviewHeight - mainSplit.SplitterWidth);
                initialized = true;
            };

            return this;
        }
    }
}
