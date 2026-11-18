using System;
using System.Drawing;
using System.Windows.Forms;
using GateHelper.EcoSim.Controls;

namespace GateHelper.EcoSim
{
    public partial class EcoSimControl
    {
        private Panel _topBar;
        private Panel _sidePanel;
        private EcoViewportControl _viewport;
        private EcoTechTreeControl _techTree;
        private EcoChartControl _chart;

        private Button _btnPlayPause;
        private Button _btnReset;
        private Button _btnPreset;
        private Button _btnSpeed1x;
        private Button _btnSpeed2x;
        private Button _btnSpeed5x;
        private Button _btnSpeed10x;

        private Button _btnToolInspect;
        private Button _btnToolFood;
        private Button _btnToolPrey;
        private Button _btnToolPredator;
        private Button _btnToolBear;
        private Button _btnToolHuman;

        private Button _btnEventAbundance;
        private Button _btnEventDrought;
        private Button _btnEventMutation;
        private Button _btnPlayerMode;
        private Button _btnBgm;
        private Button _btnHallOfFame;
        private Button _btnTechTree;
        private Button _btnHelp;

        private BufferedLabel _lblStatsSummary;
        private BufferedLabel _lblEventStatus;
        private BufferedLabel _lblInspector;
        private InventoryBagPanel _inventoryBag;
        private CheckBox _chkShelters;
        private CheckBox _chkCompost;
        private CheckBox _chkSeasons;
        private CheckBox _chkWildMigration;
        private CheckBox _chkDisasters;

        private sealed class BufferedLabel : Control
        {
            private ContentAlignment _textAlign = ContentAlignment.TopLeft;
            private SolidBrush _bgBrush;
            private SolidBrush _fgBrush;

            public BufferedLabel()
            {
                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw,
                    true);
                TabStop = false;
            }

            public ContentAlignment TextAlign
            {
                get => _textAlign;
                set { if (_textAlign != value) { _textAlign = value; Invalidate(); } }
            }

            public override string Text
            {
                get => base.Text;
                set { if (base.Text != value) { base.Text = value; Invalidate(); } }
            }

            public override Color BackColor
            {
                get => base.BackColor;
                set { base.BackColor = value; _bgBrush?.Dispose(); _bgBrush = new SolidBrush(value); Invalidate(); }
            }

            public override Color ForeColor
            {
                get => base.ForeColor;
                set { base.ForeColor = value; _fgBrush?.Dispose(); _fgBrush = new SolidBrush(value); Invalidate(); }
            }

            protected override void OnPaintBackground(PaintEventArgs pevent) { }

            protected override void OnPaint(PaintEventArgs e)
            {
                if (_bgBrush == null) _bgBrush = new SolidBrush(BackColor);
                if (_fgBrush == null) _fgBrush = new SolidBrush(ForeColor);

                e.Graphics.FillRectangle(_bgBrush, ClientRectangle);

                var rect = new RectangleF(
                    Padding.Left,
                    Padding.Top,
                    Math.Max(1, Width - Padding.Horizontal),
                    Math.Max(1, Height - Padding.Vertical));

                if (_textAlign == ContentAlignment.MiddleLeft)
                {
                    float textH = e.Graphics.MeasureString(Text ?? "", Font).Height;
                    rect.Y = Math.Max(0, (Height - textH) * 0.5f);
                }

                e.Graphics.DrawString(Text ?? "", Font, _fgBrush, rect);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _bgBrush?.Dispose();
                    _fgBrush?.Dispose();
                }
                base.Dispose(disposing);
            }
        }

        private void InitializeLayout()
        {
            BackColor = Color.FromArgb(15, 19, 26);
            Dock = DockStyle.Fill;

            // 1. 상단 컨트롤 바
            _topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Color.FromArgb(24, 30, 41),
                Padding = new Padding(10, 7, 10, 7)
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            _btnPlayPause = CreateBarButton("❚❚ 정지", 72, Color.FromArgb(37, 99, 235));
            _btnReset = CreateBarButton("↻ 초기화", 68, Color.FromArgb(51, 65, 85));
            _btnPreset = CreateBarButton("✦ 프리셋", 68, Color.FromArgb(67, 56, 202));

            _btnSpeed1x = CreateBarButton("1x", 36, Color.FromArgb(37, 99, 235));
            _btnSpeed2x = CreateBarButton("2x", 36, Color.FromArgb(51, 65, 85));
            _btnSpeed5x = CreateBarButton("5x", 36, Color.FromArgb(51, 65, 85));
            _btnSpeed10x = CreateBarButton("10x", 40, Color.FromArgb(51, 65, 85));

            _btnToolInspect = CreateBarButton("관찰", 48, Color.FromArgb(14, 116, 144));
            _btnToolFood = CreateBarButton("먹이+", 54, Color.FromArgb(51, 65, 85));
            _btnToolPrey = CreateBarButton("초식+", 54, Color.FromArgb(51, 65, 85));
            _btnToolPredator = CreateBarButton("육식+", 54, Color.FromArgb(51, 65, 85));
            _btnToolBear = CreateBarButton("곰+", 44, Color.FromArgb(51, 65, 85));
            _btnToolHuman = CreateBarButton("인간+", 54, Color.FromArgb(51, 65, 85));

            _btnEventAbundance = CreateBarButton("풍년", 48, Color.FromArgb(51, 65, 85));
            _btnEventDrought = CreateBarButton("가뭄", 48, Color.FromArgb(51, 65, 85));
            _btnEventMutation = CreateBarButton("변이3x", 58, Color.FromArgb(51, 65, 85));

            _btnPlayerMode = CreateBarButton("★ 내개체", 74, Color.FromArgb(51, 65, 85));
            _btnPlayerMode.Dock = DockStyle.Right;
            _btnPlayerMode.FlatAppearance.BorderSize = 1;
            _btnPlayerMode.FlatAppearance.BorderColor = Color.FromArgb(71, 85, 105);

            _btnBgm = CreateBarButton("♪ BGM", 62, Color.FromArgb(22, 101, 52));
            _btnBgm.Dock = DockStyle.Right;
            _btnBgm.FlatAppearance.BorderSize = 1;
            _btnBgm.FlatAppearance.BorderColor = Color.FromArgb(34, 197, 94);

            _btnHallOfFame = CreateBarButton("★ 명예", 66, Color.FromArgb(161, 98, 7));
            _btnHallOfFame.Dock = DockStyle.Right;
            _btnHallOfFame.FlatAppearance.BorderSize = 1;
            _btnHallOfFame.FlatAppearance.BorderColor = Color.FromArgb(202, 138, 4);

            _btnTechTree = CreateBarButton("📜 기술", 68, Color.FromArgb(30, 58, 138));
            _btnTechTree.Dock = DockStyle.Right;
            _btnTechTree.FlatAppearance.BorderSize = 1;
            _btnTechTree.FlatAppearance.BorderColor = Color.FromArgb(59, 130, 246);

            _btnHelp = CreateBarButton("? 도움말", 72, Color.FromArgb(30, 41, 59));
            _btnHelp.Dock = DockStyle.Right;
            _btnHelp.FlatAppearance.BorderSize = 1;
            _btnHelp.FlatAppearance.BorderColor = Color.FromArgb(71, 85, 105);

            flow.Controls.AddRange(new Control[]
            {
                _btnPlayPause, _btnReset, _btnPreset,
                CreateDivider(),
                _btnSpeed1x, _btnSpeed2x, _btnSpeed5x, _btnSpeed10x,
                CreateDivider(),
                _btnToolInspect, _btnToolFood, _btnToolPrey, _btnToolPredator, _btnToolBear, _btnToolHuman,
                CreateDivider(),
                _btnEventAbundance, _btnEventDrought, _btnEventMutation
            });
            _topBar.Controls.Add(flow);
            _topBar.Controls.Add(_btnPlayerMode);
            _topBar.Controls.Add(_btnBgm);
            _topBar.Controls.Add(_btnHallOfFame);
            _topBar.Controls.Add(_btnTechTree);
            _topBar.Controls.Add(_btnHelp);

            // 2. 우측 사이드바 (통계, 그래프, 개체 DNA 인스펙터)
            _sidePanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 306,
                BackColor = Color.FromArgb(20, 25, 35),
                Padding = new Padding(10)
            };

            _lblStatsSummary = new BufferedLabel
            {
                Dock = DockStyle.Top,
                Height = 98,
                ForeColor = Color.FromArgb(226, 232, 240),
                Font = new Font("Malgun Gothic", 9.2f, FontStyle.Regular),
                BackColor = Color.FromArgb(24, 30, 41),
                Padding = new Padding(10, 8, 10, 8),
                Text = "초기화 중..."
            };

            _lblEventStatus = new BufferedLabel
            {
                Dock = DockStyle.Top,
                Height = 28,
                ForeColor = Color.FromArgb(250, 204, 21),
                Font = new Font("Malgun Gothic", 8.8f, FontStyle.Bold),
                BackColor = Color.FromArgb(20, 25, 35),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "환경 상태: 평온"
            };

            var rulesPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 76,
                ColumnCount = 2,
                RowCount = 3,
                BackColor = Color.FromArgb(24, 30, 41),
                Padding = new Padding(8, 4, 8, 4)
            };
            rulesPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            rulesPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            rulesPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));
            rulesPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));
            rulesPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));

            _chkShelters = CreateRuleCheckBox("은신처(수풀)", true);
            _chkCompost = CreateRuleCheckBox("사체-비료 순환", true);
            _chkSeasons = CreateRuleCheckBox("사계절 순환", true);
            _chkWildMigration = CreateRuleCheckBox("영구 생태계(이주)", true);
            _chkDisasters = CreateRuleCheckBox("랜덤 자연재해", true);

            rulesPanel.Controls.Add(_chkShelters, 0, 0);
            rulesPanel.Controls.Add(_chkCompost, 1, 0);
            rulesPanel.Controls.Add(_chkSeasons, 0, 1);
            rulesPanel.Controls.Add(_chkWildMigration, 1, 1);
            rulesPanel.Controls.Add(_chkDisasters, 0, 2);

            _chart = new EcoChartControl
            {
                Dock = DockStyle.Top,
                Height = 165
            };

            var inspectorHeader = new Label
            {
                Dock = DockStyle.Top,
                Height = 26,
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Malgun Gothic", 8.8f, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomLeft,
                Text = "선택 개체 DNA 인스펙터"
            };

            var inspectorBox = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 55, 72),
                Padding = new Padding(1)
            };

            _lblInspector = new BufferedLabel
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(203, 213, 225),
                Font = new Font("Malgun Gothic", 8.8f, FontStyle.Regular),
                BackColor = Color.FromArgb(24, 30, 41),
                Padding = new Padding(10, 8, 6, 8),
                Text = "뷰포트에서 개체를 클릭하면\n유전자(속도/시야/크기)와\n세대 정보를 추적합니다."
            };

            _inventoryBag = new InventoryBagPanel
            {
                Dock = DockStyle.Right,
                Width = 88,
                BackColor = Color.FromArgb(24, 30, 41),
                Visible = false
            };

            inspectorBox.Controls.Add(_lblInspector);
            inspectorBox.Controls.Add(_inventoryBag);

            // Dock 순서상 역순으로 추가
            _sidePanel.Controls.Add(inspectorBox);
            _sidePanel.Controls.Add(inspectorHeader);
            _sidePanel.Controls.Add(_chart);
            _sidePanel.Controls.Add(rulesPanel);
            _sidePanel.Controls.Add(_lblEventStatus);
            _sidePanel.Controls.Add(_lblStatsSummary);

            // 3. 메인 뷰포트 & 기술 트리 탭
            _viewport = new EcoViewportControl
            {
                Dock = DockStyle.Fill
            };
            _techTree = new EcoTechTreeControl
            {
                Dock = DockStyle.Fill,
                Visible = false
            };

            Controls.Add(_techTree);
            Controls.Add(_viewport);
            Controls.Add(_sidePanel);
            Controls.Add(_topBar);
        }

        private static Button CreateBarButton(string text, int width, Color backColor)
        {
            var btn = new Button
            {
                Text = text,
                Width = width,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = Color.White,
                Font = new Font("Malgun Gothic", 8.6f, FontStyle.Bold),
                Margin = new Padding(0, 0, 4, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private static CheckBox CreateRuleCheckBox(string text, bool isChecked)
        {
            return new CheckBox
            {
                Text = text,
                Checked = isChecked,
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(203, 213, 225),
                Font = new Font("Malgun Gothic", 8.4f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
        }

        private static Panel CreateDivider()
        {
            return new Panel
            {
                Width = 1,
                Height = 24,
                BackColor = Color.FromArgb(51, 65, 85),
                Margin = new Padding(6, 3, 10, 0)
            };
        }

        private sealed class InventoryBagPanel : Control
        {
            private int _meatCount;
            private int _woodCount;
            private int _stoneCount;
            private bool _isPlayerHuman;
            private bool _houseUnlocked;
            private int _hoveredSlot = -1;
            private readonly ToolTip _toolTip;

            public InventoryBagPanel()
            {
                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw,
                    true);
                _toolTip = new ToolTip
                {
                    InitialDelay = 150,
                    ReshowDelay = 80,
                    AutoPopDelay = 5500
                };
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) _toolTip.Dispose();
                base.Dispose(disposing);
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                const float slotSize = 34f;
                const float gap = 4f;
                const float padX = 8f;
                const float gridTop = 28f;

                int hit = -1;
                for (int r = 0; r < 3 && hit < 0; r++)
                {
                    for (int c = 0; c < 2; c++)
                    {
                        float sx = padX + c * (slotSize + gap);
                        float sy = gridTop + r * (slotSize + gap);
                        if (e.X >= sx && e.X <= sx + slotSize && e.Y >= sy && e.Y <= sy + slotSize)
                        {
                            hit = r * 2 + c;
                            break;
                        }
                    }
                }

                if (hit == _hoveredSlot) return;
                _hoveredSlot = hit;
                if (hit == 0)
                {
                    _toolTip.SetToolTip(this, $"고기 ({_meatCount}/{Services.EcoSimulationEngine.MaxInventoryStack})\n체력 38% 미만 또는 겨울 모닥불 휴식 시 자동 섭취 (+85 에너지)");
                }
                else if (hit == 1)
                {
                    _toolTip.SetToolTip(this, $"나뭇가지 ({_woodCount}/{Services.EcoSimulationEngine.MaxInventoryStack})\n5개 보유 시 [F] 키로 모닥불 건설 (AI는 겨울에 자동 건설)");
                }
                else if (hit == 2)
                {
                    string lockNote = _houseUnlocked ? "보유 시 [B] 키 또는 협동으로 집 건설" : "(3년차 정착기 도달 시 집 해금)";
                    _toolTip.SetToolTip(this, $"돌 ({_stoneCount}/{Services.EcoSimulationEngine.MaxInventoryStack})\n나뭇가지 {Services.EcoSimulationEngine.HouseWoodCost} + 돌 {Services.EcoSimulationEngine.HouseStoneCost} {lockNote}");
                }
                else if (hit >= 3)
                {
                    _toolTip.SetToolTip(this, "빈 슬롯\n추후 업데이트용 여분 칸");
                }
                else
                {
                    _toolTip.SetToolTip(this, string.Empty);
                }
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                _hoveredSlot = -1;
                _toolTip.SetToolTip(this, string.Empty);
            }

            public void SetState(bool visible, int meatCount, int woodCount, int stoneCount, bool isPlayerHuman, bool houseUnlocked)
            {
                if (Visible != visible) Visible = visible;
                if (_meatCount == meatCount && _woodCount == woodCount && _stoneCount == stoneCount && _isPlayerHuman == isPlayerHuman && _houseUnlocked == houseUnlocked) return;
                _meatCount = meatCount;
                _woodCount = woodCount;
                _stoneCount = stoneCount;
                _isPlayerHuman = isPlayerHuman;
                _houseUnlocked = houseUnlocked;
                if (visible) Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(BackColor);
                g.DrawLine(Rendering.EcoRenderCache.BorderPen, 0, 8, 0, Height - 8);

                const int cols = 2;
                const int rows = 3;
                const float slotSize = 34f;
                const float gap = 4f;
                const float padX = 8f;
                const float padTop = 8f;
                const float headerH = 20f;

                bool canBuildFire = _woodCount >= Services.EcoSimulationEngine.CampfireWoodCost;
                bool canBuildHouse = _houseUnlocked && _woodCount >= Services.EcoSimulationEngine.HouseWoodCost && _stoneCount >= Services.EcoSimulationEngine.HouseStoneCost;
                string title = (_isPlayerHuman && canBuildHouse) ? "[B] 집!" : ((_isPlayerHuman && canBuildFire) ? "[F] 불!" : "가방 (3×2)");
                g.DrawString(
                    title,
                    Rendering.EcoRenderCache.BoldSmallFont,
                    (canBuildFire || canBuildHouse) ? Rendering.EcoRenderCache.LegendStarBrush : Rendering.EcoRenderCache.TextMutedBrush,
                    padX,
                    padTop);

                float gridTop = padTop + headerH;
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        int idx = r * cols + c;
                        float sx = padX + c * (slotSize + gap);
                        float sy = gridTop + r * (slotSize + gap);

                        g.FillRectangle(Rendering.EcoRenderCache.InvSlotBgBrush, sx, sy, slotSize, slotSize);
                        Pen border = ((idx == 1 && canBuildFire) || (idx == 2 && canBuildHouse))
                            ? Rendering.EcoRenderCache.InvSlotReadyPen
                            : Rendering.EcoRenderCache.InvSlotBorderPen;
                        g.DrawRectangle(border, sx, sy, slotSize, slotSize);

                        if (idx == 0 && _meatCount > 0)
                        {
                            g.DrawImage(Rendering.EcoRenderCache.MeatSprite, sx + 5f, sy + 4f, 24f, 24f);
                            g.DrawString(_meatCount.ToString(), Rendering.EcoRenderCache.BoldSmallFont, Rendering.EcoRenderCache.TextPrimaryBrush, sx + 19f, sy + 19f);
                        }
                        else if (idx == 1 && _woodCount > 0)
                        {
                            g.DrawImage(Rendering.EcoRenderCache.WoodSprite, sx + 5f, sy + 4f, 24f, 24f);
                            g.DrawString(_woodCount.ToString(), Rendering.EcoRenderCache.BoldSmallFont, Rendering.EcoRenderCache.TextPrimaryBrush, sx + 19f, sy + 19f);
                        }
                        else if (idx == 2 && _stoneCount > 0)
                        {
                            g.DrawImage(Rendering.EcoRenderCache.StoneSprite, sx + 5f, sy + 4f, 24f, 24f);
                            g.DrawString(_stoneCount.ToString(), Rendering.EcoRenderCache.BoldSmallFont, Rendering.EcoRenderCache.TextPrimaryBrush, sx + 19f, sy + 19f);
                        }
                    }
                }
            }
        }
    }
}
