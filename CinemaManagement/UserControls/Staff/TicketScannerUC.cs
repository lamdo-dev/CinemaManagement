
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Models;
using CinemaManagement.Repositories;
using CinemaManagement.Utils;

namespace CinemaManagement.UserControls.Staff
{
    public partial class TicketScannerUC : UserControl
    {
        private static readonly CultureInfo VN = new CultureInfo("vi-VN");

        private readonly Color ColorValid =
            Color.FromArgb(46, 204, 113);

        private readonly Color ColorWarning =
            Color.FromArgb(241, 196, 15);

        private readonly Color ColorError =
            Color.FromArgb(231, 76, 60);

        private readonly Color ColorIdle =
            Color.FromArgb(52, 152, 219);

        // Controls
        private Panel qrFrame = null!;
        private TextBox txtCode = null!;
        private Label lblStatus = null!;
        private Label lblTicketDetail = null!;
        private Panel pnlStatusBar = null!;
        private Label lblHeader = null!;
        private DataGridView dgvLogs = null!;
        private ListBox lstPending = null!;
        private Label lblPendingEmpty = null!;
        private readonly BookingRepository _bookingRepository = new();
        private readonly List<BookingResultEventArgs> _doorBookings = new();
        private readonly HashSet<string> _checkedInCodes = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DateTime> _checkInTimes = new(StringComparer.OrdinalIgnoreCase);
        private readonly System.Windows.Forms.Timer _refreshTimer = new();
        private TextBox txtSearch = null!;
        private ComboBox cboFilter = null!;
        private ComboBox cboShowtimeFilter = null!;
        private readonly List<int?> _showtimeFilterIds = new();
        private bool _updatingShowtimeFilters;
        private Label[] _kpiValues = Array.Empty<Label>();
        private Label lblDatabaseStatus = null!;
        private Label lblCheckInRate = null!;
        private Panel pnlProgressTrack = null!;
        private Panel pnlProgressFill = null!;
        private Button btnCheckTicket = null!;
        private int _rejectedCount;
        private double _checkInPercent;

        private Color _frameColor;
        private Panel pnlDashboardHeader = null!;
        private Label lblDashboardTitle = null!;
        private Label lblDashboardSubtitle = null!;

        // Events
        public event EventHandler<TicketScanResult>? OnTicketScanned;
        public event EventHandler<TicketScanResult>? OnTicketAccepted;
        public event EventHandler<TicketScanResult>? OnTicketRejected;
        public event EventHandler? OnScanNextClicked;
        public event EventHandler? OnOpenPOSClicked;

        public TicketScannerUC()
        {
            _frameColor = ColorIdle;

            InitializeDashboard();
            Disposed += (s, e) => _refreshTimer.Dispose();

            RefreshLogs();
            ResetResult();

            _refreshTimer.Interval = 12000;
            _refreshTimer.Tick += (s, e) => RefreshLogs();
            _refreshTimer.Start();
        }

        // Mở màn hình soát vé và điền sẵn mã vé
        public TicketScannerUC(string? prefillCode) : this()
        {
            if (!string.IsNullOrWhiteSpace(prefillCode))
            {
                txtCode.Text = prefillCode.Trim().ToUpperInvariant();
            }
        }

        private void InitializeDashboard()
        {
            SuspendLayout();
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(245, 247, 251);
            Padding = new Padding(12);

            var shell = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = BackColor,
                Margin = Padding.Empty
            };
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            pnlDashboardHeader = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(8, 2, 8, 7)
            };
            lblDashboardTitle = new Label
            {
                Text = "SOÁT VÉ & CHECK-IN",
                Dock = DockStyle.Left,
                Width = 290,
                ForeColor = Color.FromArgb(23, 32, 51),
                Font = new Font("Segoe UI", 17F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            lblDashboardSubtitle = new Label
            {
                Text = "Xác thực vé trực tiếp với hệ thống rạp",
                Dock = DockStyle.Left,
                Width = 360,
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 9.5F),
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlDashboardHeader.Controls.Add(lblDashboardTitle);
            pnlDashboardHeader.Controls.Add(lblDashboardSubtitle);
            lblDatabaseStatus = new Label
            {
                Text = "ĐANG KẾT NỐI DATABASE",
                Dock = DockStyle.Right,
                Width = 230,
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };
            pnlDashboardHeader.Controls.Add(lblDatabaseStatus);

            var columns = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = Padding.Empty
            };
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31F));
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24F));
            columns.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            columns.Controls.Add(CreateScannerPanel(), 0, 0);
            columns.Controls.Add(CreateLivePanel(), 1, 0);
            columns.Controls.Add(CreateTicketPanel(), 2, 0);

            shell.Controls.Add(pnlDashboardHeader, 0, 0);
            shell.Controls.Add(columns, 0, 1);
            Controls.Add(shell);
            ResumeLayout(true);
        }

        public void SetTheme(bool isDark)
        {
            if (pnlDashboardHeader == null || pnlDashboardHeader.IsDisposed)
                return;

            pnlDashboardHeader.BackColor = isDark
                ? Color.FromArgb(11, 16, 32)
                : Color.FromArgb(245, 247, 251);
            lblDashboardTitle.ForeColor = isDark
                ? Color.FromArgb(248, 250, 252)
                : Color.FromArgb(23, 32, 51);
            lblDashboardSubtitle.ForeColor = isDark
                ? Color.FromArgb(203, 213, 225)
                : Color.FromArgb(71, 85, 105);
        }

        private Panel CreateScannerPanel()
        {
            var card = CreateSurfaceCard();
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 7,
                BackColor = Color.White,
                Padding = new Padding(16, 14, 16, 12)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

            layout.Controls.Add(CreateSectionTitle("QR SCANNER"), 0, 0);
            qrFrame = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(17, 24, 39),
                Margin = new Padding(0, 2, 0, 8)
            };
            qrFrame.Paint += QrFrame_Paint;
            layout.Controls.Add(qrFrame, 0, 1);

            layout.Controls.Add(new Label
            {
                Text = "Camera chưa được cấu hình trên thiết bị này.",
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 8.5F),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 2);

            var cameraButton = new Button
            {
                Text = "CAMERA CHƯA KHẢ DỤNG",
                Dock = DockStyle.Fill,
                Enabled = false,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(100, 116, 139),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Margin = new Padding(0, 2, 0, 8)
            };
            cameraButton.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            layout.Controls.Add(cameraButton, 0, 3);

            layout.Controls.Add(new Label
            {
                Text = "NHẬP MÃ VÉ THỦ CÔNG",
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomLeft
            }, 0, 4);

            txtCode = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11F),
                BorderStyle = BorderStyle.FixedSingle,
                CharacterCasing = CharacterCasing.Upper,
                Margin = new Padding(0, 2, 0, 8)
            };
            txtCode.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    PerformScan(txtCode.Text);
                }
            };
            layout.Controls.Add(txtCode, 0, 5);

            btnCheckTicket = new Button
            {
                Text = "KIỂM TRA VÉ",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(109, 61, 245),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 3, 0, 0)
            };
            btnCheckTicket.FlatAppearance.BorderSize = 0;
            btnCheckTicket.Click += (s, e) => PerformScan(txtCode.Text);
            layout.Controls.Add(btnCheckTicket, 0, 6);

            card.Controls.Add(layout);
            return card;
        }

        private Panel CreateLivePanel()
        {
            var card = CreateSurfaceCard();
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = Color.White,
                Padding = new Padding(15, 14, 15, 12)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            lblHeader = CreateSectionTitle("CHECK-IN LIVE");
            layout.Controls.Add(lblHeader, 0, 0);

            var kpis = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Margin = Padding.Empty
            };
            for (int i = 0; i < 4; i++)
                kpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            string[] names = { "TỔNG VÉ", "ĐÃ VÀO", "CHƯA VÀO", "TỪ CHỐI" };
            _kpiValues = new Label[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                var metric = CreateMetricCard(names[i], i, out Label value);
                _kpiValues[i] = value;
                kpis.Controls.Add(metric, i, 0);
            }
            layout.Controls.Add(kpis, 0, 1);

            var progressPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(0, 4, 0, 3)
            };
            pnlProgressTrack = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(226, 232, 240)
            };
            pnlProgressFill = new Panel
            {
                Dock = DockStyle.Left,
                Width = 0,
                BackColor = Color.FromArgb(34, 197, 94)
            };
            pnlProgressTrack.Controls.Add(pnlProgressFill);
            pnlProgressTrack.Resize += (s, e) => UpdateProgressWidth();
            lblCheckInRate = new Label
            {
                Dock = DockStyle.Right,
                Width = 58,
                Text = "0%",
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };
            progressPanel.Controls.Add(pnlProgressTrack);
            progressPanel.Controls.Add(lblCheckInRate);
            layout.Controls.Add(progressPanel, 0, 2);

            var filters = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 4, 0, 5)
            };
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126));
            txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "Tìm mã vé, khách, phim hoặc ghế...",
                Margin = new Padding(0, 0, 8, 0)
            };
            txtSearch.TextChanged += (s, e) => RenderDoorList();
            cboShowtimeFilter = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 8.5F),
                Margin = new Padding(0, 0, 6, 0)
            };
            cboShowtimeFilter.SelectedIndexChanged += (s, e) =>
            {
                if (!_updatingShowtimeFilters)
                    RefreshLogs();
            };
            cboFilter = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 8.5F),
                Margin = Padding.Empty
            };
            cboFilter.Items.AddRange(new object[] { "Tất cả", "Đã vào", "Chưa vào", "Bị từ chối" });
            cboFilter.SelectedIndex = 0;
            cboFilter.SelectedIndexChanged += (s, e) => RenderDoorList();
            filters.Controls.Add(txtSearch, 0, 0);
            filters.Controls.Add(cboShowtimeFilter, 1, 0);
            filters.Controls.Add(cboFilter, 2, 0);
            layout.Controls.Add(filters, 0, 3);

            dgvLogs = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 38,
                RowTemplate = { Height = 38 },
                GridColor = Color.FromArgb(238, 242, 247),
                DefaultCellStyle =
                {
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(23, 32, 51),
                    SelectionBackColor = Color.FromArgb(237, 233, 254),
                    SelectionForeColor = Color.FromArgb(23, 32, 51),
                    Font = new Font("Segoe UI", 8F)
                },
                ColumnHeadersDefaultCellStyle =
                {
                    BackColor = Color.FromArgb(248, 250, 252),
                    ForeColor = Color.FromArgb(100, 116, 139),
                    Font = new Font("Segoe UI", 7.5F, FontStyle.Bold)
                }
            };
            dgvLogs.Columns.Add("colCode", "MÃ VÉ");
            dgvLogs.Columns.Add("colCustomer", "KHÁCH");
            dgvLogs.Columns.Add("colMovie", "PHIM / SUẤT");
            dgvLogs.Columns.Add("colSeat", "GHẾ");
            dgvLogs.Columns.Add("colTime", "CHECK-IN");
            dgvLogs.Columns.Add("colStatus", "TRẠNG THÁI");
            dgvLogs.Columns[0].FillWeight = 18;
            dgvLogs.Columns[1].FillWeight = 17;
            dgvLogs.Columns[2].FillWeight = 23;
            dgvLogs.Columns[3].FillWeight = 14;
            dgvLogs.Columns[4].FillWeight = 12;
            dgvLogs.Columns[5].FillWeight = 16;
            dgvLogs.CellClick += DoorList_CellClick;
            dgvLogs.CellDoubleClick += DoorList_CellClick;
            layout.Controls.Add(dgvLogs, 0, 4);

            card.Controls.Add(layout);
            return card;
        }

        private Panel CreateTicketPanel()
        {
            var card = CreateSurfaceCard();
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.White,
                Padding = new Padding(16, 14, 16, 12)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

            layout.Controls.Add(CreateSectionTitle("THÔNG TIN VÉ"), 0, 0);
            pnlStatusBar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(239, 246, 255),
                Margin = new Padding(0, 4, 0, 8)
            };
            lblStatus = new Label
            {
                Text = "SẴN SÀNG",
                Dock = DockStyle.Fill,
                ForeColor = ColorIdle,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlStatusBar.Controls.Add(lblStatus);
            layout.Controls.Add(pnlStatusBar, 0, 1);

            var detailsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(1, 8, 1, 4)
            };
            lblTicketDetail = new Label
            {
                Text = "Nhập mã vé để xác thực thông tin đặt chỗ.",
                Dock = DockStyle.Top,
                AutoSize = true,
                MaximumSize = new Size(280, 0),
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 9.2F),
                Padding = new Padding(5),
                TextAlign = ContentAlignment.TopLeft
            };
            detailsPanel.Controls.Add(lblTicketDetail);
            layout.Controls.Add(detailsPanel, 0, 2);

            var actions = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty
            };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            var nextButton = new Button
            {
                Text = "VÉ TIẾP THEO",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Margin = new Padding(0, 0, 6, 0)
            };
            nextButton.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            nextButton.Click += (s, e) =>
            {
                ResetResult();
                OnScanNextClicked?.Invoke(this, EventArgs.Empty);
            };
            var posButton = new Button
            {
                Text = "MỞ POS",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(109, 61, 245),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Margin = new Padding(4, 0, 0, 0)
            };
            posButton.FlatAppearance.BorderSize = 0;
            posButton.Click += (s, e) => OnOpenPOSClicked?.Invoke(this, EventArgs.Empty);
            actions.Controls.Add(nextButton, 0, 0);
            actions.Controls.Add(posButton, 1, 0);
            layout.Controls.Add(actions, 0, 3);

            card.Controls.Add(layout);
            return card;
        }

        private static Panel CreateSurfaceCard()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(6),
                Padding = new Padding(0)
            };
        }

        private static Label CreateSectionTitle(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(23, 32, 51),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private static Panel CreateMetricCard(string title, int index, out Label value)
        {
            Color[] accents =
            {
                Color.FromArgb(109, 61, 245),
                Color.FromArgb(34, 197, 94),
                Color.FromArgb(245, 158, 11),
                Color.FromArgb(239, 68, 68)
            };
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                Margin = new Padding(0, 0, 6, 0),
                Padding = new Padding(8, 6, 5, 5)
            };
            card.Controls.Add(new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 19,
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                AutoEllipsis = true
            });
            value = new Label
            {
                Text = "—",
                Dock = DockStyle.Fill,
                ForeColor = accents[index],
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            card.Controls.Add(value);
            return card;
        }

        // =========================================================
        // KHỞI TẠO GIAO DIỆN
        // =========================================================

        private void InitializeComponentManual()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(15, 20, 30);

            // =========================
            // CỘT TRÁI
            // =========================

            Panel pnlCamera = new Panel
            {
                Dock = DockStyle.Left,
                Width = 430,
                Padding = new Padding(25),
                BackColor = Color.FromArgb(20, 26, 38)
            };

            qrFrame = new Panel
            {
                Height = 210,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(10, 15, 25)
            };

            qrFrame.Paint += QrFrame_Paint;

            // INPUT
            Panel pnlInput = new Panel
            {
                Dock = DockStyle.Top,
                Height = 92,
                Padding = new Padding(0, 12, 0, 0)
            };

            Label lblInput = new Label
            {
                Text = "Mã vé (quét bằng máy đọc hoặc nhập tay rồi nhấn Enter):",
                ForeColor = Color.FromArgb(150, 160, 180),
                Font = new Font("Segoe UI", 8.5f),
                Location = new Point(0, 12),
                AutoSize = true
            };

            txtCode = new TextBox
            {
                Location = new Point(0, 34),
                Size = new Size(260, 32),
                Font = new Font("Consolas", 12f, FontStyle.Bold),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                CharacterCasing = CharacterCasing.Upper,
                PlaceholderText = "BK-... / POS-..."
            };

            txtCode.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    PerformScan(txtCode.Text);
                }
            };

            Button btnCheck = new Button
            {
                Text = "KIỂM TRA",
                Location = new Point(268, 33),
                Size = new Size(112, 32),
                BackColor = ColorIdle,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnCheck.FlatAppearance.BorderSize = 0;
            btnCheck.Click += (s, e) => PerformScan(txtCode.Text);

            pnlInput.Controls.AddRange(new Control[]
            {
                lblInput,
                txtCode,
                btnCheck
            });

            // RESULT
            Panel pnlResult = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 0, 0)
            };

            pnlStatusBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 6,
                BackColor = ColorIdle
            };

            lblStatus = new Label
            {
                Text = "SẴN SÀNG QUÉT",
                ForeColor = ColorIdle,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblTicketDetail = new Label
            {
                Text = "",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Dock = DockStyle.Fill,
                AutoEllipsis = true
            };

            Panel pnlButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 45
            };

            Button btnScanNext = new Button
            {
                Text = "Quét vé tiếp theo",
                Location = new Point(0, 0),
                Size = new Size(185, 45),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnScanNext.FlatAppearance.BorderSize = 0;

            btnScanNext.Click += (s, e) =>
            {
                ResetResult();
                OnScanNextClicked?.Invoke(this, EventArgs.Empty);
            };

            Button btnOpenPOS = new Button
            {
                Text = "Về quầy bán vé",
                Location = new Point(195, 0),
                Size = new Size(185, 45),
                BackColor = Color.FromArgb(124, 58, 237),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnOpenPOS.FlatAppearance.BorderSize = 0;

            btnOpenPOS.Click += (s, e) =>
                OnOpenPOSClicked?.Invoke(this, EventArgs.Empty);

            pnlButtons.Controls.AddRange(new Control[]
            {
                btnScanNext,
                btnOpenPOS
            });

            pnlResult.Controls.Add(lblTicketDetail);
            pnlResult.Controls.Add(lblStatus);
            pnlResult.Controls.Add(pnlStatusBar);
            pnlResult.Controls.Add(pnlButtons);

            pnlCamera.Controls.Add(pnlResult);
            pnlCamera.Controls.Add(pnlInput);
            pnlCamera.Controls.Add(qrFrame);

            // =========================
            // CỘT PHẢI
            // =========================

            Panel pnlDoorList = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(25),
                BackColor = Color.FromArgb(15, 20, 30)
            };

            lblHeader = new Label
            {
                Text = "LIVE DOOR LIST",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 60
            };

            // DANH SÁCH VÉ CHỜ
            Panel pnlPending = new Panel
            {
                Dock = DockStyle.Top,
                Height = 170,
                Padding = new Padding(0, 0, 0, 12)
            };

            Label lblPending = new Label
            {
                Text = "VÉ CHƯA SOÁT (nhấp đúp để soát nhanh)",
                ForeColor = Color.FromArgb(150, 160, 180),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 24
            };

            lstPending = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 26, 38),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 9.5f)
            };

            lblPendingEmpty = new Label
            {
                Text = "Không có vé nào đang chờ soát.",
                ForeColor = Color.FromArgb(150, 160, 180),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };

            lstPending.DoubleClick += (s, e) =>
            {
                if (lstPending.SelectedItem is PendingItem item)
                {
                    txtCode.Text = item.Code;
                    PerformScan(item.Code);
                }
            };

            pnlPending.Controls.Add(lstPending);
            pnlPending.Controls.Add(lblPendingEmpty);
            pnlPending.Controls.Add(lblPending);

            // NHẬT KÝ
            dgvLogs = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(20, 26, 38),
                ForeColor = Color.White,
                GridColor = Color.FromArgb(35, 45, 60),
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 34,
                DefaultCellStyle =
                {
                    BackColor = Color.FromArgb(20, 26, 38),
                    ForeColor = Color.White,
                    SelectionBackColor = Color.FromArgb(40, 50, 75)
                },
                ColumnHeadersDefaultCellStyle =
                {
                    BackColor = Color.FromArgb(30, 41, 59),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold)
                }
            };

            dgvLogs.Columns.Add("colTime", "THỜI GIAN");
            dgvLogs.Columns.Add("colCode", "MÃ VÉ");
            dgvLogs.Columns.Add("colMovie", "PHIM / SUẤT");
            dgvLogs.Columns.Add("colSeat", "GHẾ");
            dgvLogs.Columns.Add("colStatus", "TRẠNG THÁI");

            dgvLogs.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0)
                    return;

                string? code = dgvLogs.Rows[e.RowIndex]
                    .Cells["colCode"].Value?.ToString();

                var booking = UserSession.FindBooking(
                    code ?? string.Empty
                );

                if (booking != null)
                {
                    lblTicketDetail.Text = BuildDetail(booking);
                }
            };

            pnlDoorList.Controls.Add(dgvLogs);
            pnlDoorList.Controls.Add(pnlPending);
            pnlDoorList.Controls.Add(lblHeader);

            Controls.Add(pnlDoorList);
            Controls.Add(pnlCamera);
        }

        // =========================================================
        // VẼ KHUNG QR
        // =========================================================

        private void QrFrame_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int size = Math.Min(qrFrame.Width, qrFrame.Height) - 40;

            if (size <= 0)
                return;

            int x = (qrFrame.Width - size) / 2;
            int y = (qrFrame.Height - size) / 2;
            int len = 28;

            using (Pen pen = new Pen(_frameColor, 5))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                g.DrawLine(pen, x, y, x + len, y);
                g.DrawLine(pen, x, y, x, y + len);

                g.DrawLine(pen, x + size, y, x + size - len, y);
                g.DrawLine(pen, x + size, y, x + size, y + len);

                g.DrawLine(pen, x, y + size, x + len, y + size);
                g.DrawLine(pen, x, y + size, x, y + size - len);

                g.DrawLine(pen, x + size, y + size,
                    x + size - len, y + size);

                g.DrawLine(pen, x + size, y + size,
                    x + size, y + size - len);
            }

            using (Font font = new Font("Segoe UI", 9f))
            using (Brush brush = new SolidBrush(Color.FromArgb(203, 213, 225)))
            using (StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Center
            })
            {
                g.DrawString(
                    "CAMERA CHƯA KẾT NỐI",
                    font,
                    brush,
                    new RectangleF(0, y + (size / 2F) - 10, qrFrame.Width, 20),
                    sf
                );
            }
        }

        // =========================================================
        // XỬ LÝ SOÁT VÉ
        // =========================================================

        public void PerformScan(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                lblStatus.Text = "VUI LÒNG NHẬP MÃ VÉ";
                lblStatus.ForeColor = ColorWarning;
                pnlStatusBar.BackColor = Color.FromArgb(255, 251, 235);
                txtCode.Focus();
                return;
            }

            btnCheckTicket.Enabled = false;
            btnCheckTicket.Text = "ĐANG XÁC THỰC...";
            try
            {
                code = code.Trim().ToUpperInvariant();

                TicketScanResult result = UserSession.ScanTicket(code);

                ShowResult(result);

                RefreshLogs();

                OnTicketScanned?.Invoke(this, result);

                if (result.IsValid)
                {
                    OnTicketAccepted?.Invoke(this, result);
                }
                else
                {
                    OnTicketRejected?.Invoke(this, result);
                }

                txtCode.SelectAll();
                txtCode.Focus();
            }
            catch (Exception ex)
            {
                lblDatabaseStatus.Text = "DATABASE KHÔNG KHẢ DỤNG";
                lblDatabaseStatus.ForeColor = ColorError;
                lblStatus.Text = "KHÔNG THỂ XÁC MINH";
                lblStatus.ForeColor = ColorError;
                pnlStatusBar.BackColor = Color.FromArgb(254, 242, 242);
                lblTicketDetail.Text =
                    "Không thể xác minh vé vì kết nối dữ liệu đang gặp lỗi. " +
                    "Vé chưa được đánh dấu đã vào.\n\n" + ex.Message;
                qrFrame.Invalidate();
            }
            finally
            {
                btnCheckTicket.Enabled = true;
                btnCheckTicket.Text = "KIỂM TRA VÉ";
            }
        }

        // =========================================================
        // HIỂN THỊ KẾT QUẢ
        // =========================================================

        private void ShowResult(TicketScanResult r)
        {
            if (r == null)
                return;

            Color color = r.Status switch
            {
                TicketScanStatus.Valid => ColorValid,
                TicketScanStatus.AlreadyUsed => ColorWarning,
                TicketScanStatus.Expired => ColorError,
                _ => ColorError
            };

            string title = r.Status switch
            {
                TicketScanStatus.Valid =>
                    "VÉ HỢP LỆ · CHECK-IN THÀNH CÔNG",

                TicketScanStatus.AlreadyUsed =>
                    "VÉ ĐÃ ĐƯỢC SỬ DỤNG",

                TicketScanStatus.WrongShowtime =>
                    "VÉ KHÔNG ĐÚNG SUẤT CHIẾU",

                TicketScanStatus.Expired =>
                    "VÉ SAI / HẾT SUẤT CHIẾU",

                _ => r.Booking == null
                    ? "KHÔNG TÌM THẤY VÉ"
                    : "VÉ KHÔNG HỢP LỆ"
            };

            _frameColor = color;

            pnlStatusBar.BackColor = r.Status switch
            {
                TicketScanStatus.Valid => Color.FromArgb(240, 253, 244),
                TicketScanStatus.AlreadyUsed => Color.FromArgb(255, 251, 235),
                _ => Color.FromArgb(254, 242, 242)
            };
            lblStatus.ForeColor = color;
            lblStatus.Text = title;

            lblTicketDetail.Text = r.Booking != null
                ? $"{r.Message}\n\n{BuildDetail(r.Booking)}\n" +
                  (r.Status == TicketScanStatus.Valid
                      ? $"CHECK-IN LÚC: {r.ScanTime:HH:mm:ss dd/MM/yyyy}"
                      : r.FirstCheckInTime.HasValue
                          ? $"ĐÃ VÀO LÚC: {r.FirstCheckInTime.Value:HH:mm:ss dd/MM/yyyy}"
                          : string.Empty)
                : $"{r.Message}\n\nMã vé: {r.Code}";

            qrFrame.Invalidate();

            if (!r.IsValid)
            {
                System.Media.SystemSounds.Exclamation.Play();
            }
        }

        // =========================================================
        // CHI TIẾT VÉ
        // =========================================================

        private static string BuildDetail(BookingResultEventArgs b)
        {
            string movie = b.Movie?.Title ?? "(Chỉ mua bắp nước)";

            string time = b.Showtime != null
                ? $"{b.Showtime.StartTime:HH:mm dd/MM/yyyy} - Phòng 0{b.Showtime.RoomId}"
                : "--";

            string seats = b.Seats != null && b.Seats.Count > 0
                ? string.Join(", ", b.Seats)
                : "--";

            string food = b.FoodCart != null && b.FoodCart.Count > 0
                ? string.Join(
                    ", ",
                    b.FoodCart.Select(x =>
                        $"{x.Value.Name} x{x.Value.Qty}")
                )
                : "Không";

            string customer = !string.IsNullOrWhiteSpace(b.CustomerName)
                ? b.CustomerName
                : UserSession.CurrentUser?.FullName ?? "Khách hàng";

            return
                $"MÃ VÉ: {b.BookingCode} [{b.Channel}]\n" +
                $"PHIM: {movie}\n" +
                $"SUẤT: {time}\n" +
                $"GHẾ: {seats}\n" +
                $"BẮP NƯỚC: {food}\n" +
                $"KHÁCH: {customer}\n" +
                $"TỔNG TIỀN: {b.TotalAmount.ToString("N0", VN)} đ";
        }

        // =========================================================
        // RESET KẾT QUẢ
        // =========================================================

        private void ResetResult()
        {
            _frameColor = ColorIdle;

            pnlStatusBar.BackColor = Color.FromArgb(239, 246, 255);
            lblStatus.ForeColor = ColorIdle;

            lblStatus.Text = "SẴN SÀNG QUÉT";

            lblTicketDetail.Text =
                "Quét mã QR trên vé điện tử hoặc nhập mã đơn hàng.\n" +
                "Ví dụ: POS-...";

            txtCode.Text = string.Empty;

            qrFrame.Invalidate();

            txtCode.Focus();
        }

        // =========================================================
        // CẬP NHẬT NHẬT KÝ
        // =========================================================

        private void RefreshLogs()
        {
            try
            {
                List<BookingResultEventArgs> bookings = _bookingRepository.GetAllTicketBookings();
                HashSet<string> checkedInCodes = _bookingRepository.GetCheckedInBookingCodes();
                Dictionary<string, DateTime> checkInTimes = _bookingRepository.GetBookingCheckInTimes();

                _doorBookings.Clear();
                _doorBookings.AddRange(bookings);
                UpdateShowtimeFilterOptions(bookings);
                _checkedInCodes.Clear();
                _checkedInCodes.UnionWith(checkedInCodes);
                _checkInTimes.Clear();
                foreach (var checkInTime in checkInTimes)
                    _checkInTimes[checkInTime.Key] = checkInTime.Value;
                int? showtimeId = SelectedShowtimeId;
                var counts = _bookingRepository.GetTicketCheckInCounts(showtimeId);
                _rejectedCount = UserSession.ScanLogs.Count(log =>
                    !log.IsValid &&
                    (!showtimeId.HasValue || log.Booking?.Showtime?.Id == showtimeId.Value));

                _kpiValues[0].Text = counts.Total.ToString("N0", VN);
                _kpiValues[1].Text = counts.CheckedIn.ToString("N0", VN);
                _kpiValues[2].Text = Math.Max(0, counts.Total - counts.CheckedIn).ToString("N0", VN);
                _kpiValues[3].Text = _rejectedCount.ToString("N0", VN);

                _checkInPercent = counts.Total > 0
                    ? Math.Clamp(counts.CheckedIn * 100.0 / counts.Total, 0, 100)
                    : 0;
                lblCheckInRate.Text = $"{_checkInPercent:0.#}%";
                UpdateProgressWidth();
                lblHeader.Text = $"DANH SÁCH VÉ · {bookings.Count:N0} đơn";
                lblDatabaseStatus.Text = "DATABASE ĐANG HOẠT ĐỘNG";
                lblDatabaseStatus.ForeColor = Color.FromArgb(22, 163, 74);
                RenderDoorList();
            }
            catch (Exception ex)
            {
                _doorBookings.Clear();
                _checkedInCodes.Clear();
                _checkInTimes.Clear();
                foreach (Label value in _kpiValues)
                    value.Text = "—";
                dgvLogs.Rows.Clear();
                lblDatabaseStatus.Text = "DATABASE KHÔNG KHẢ DỤNG";
                lblDatabaseStatus.ForeColor = ColorError;
                lblTicketDetail.Text =
                    "Không thể tải trạng thái check-in từ cơ sở dữ liệu. " +
                    "Các số liệu được để trống để tránh hiển thị thông tin sai.\n\n" +
                    ex.Message;
            }
        }

        private void UpdateProgressWidth()
        {
            if (pnlProgressTrack == null || pnlProgressFill == null)
                return;

            pnlProgressFill.Width = (int)Math.Round(
                pnlProgressTrack.ClientSize.Width * _checkInPercent / 100.0);
        }

        private int? SelectedShowtimeId =>
            cboShowtimeFilter != null &&
            cboShowtimeFilter.SelectedIndex > 0 &&
            cboShowtimeFilter.SelectedIndex < _showtimeFilterIds.Count
                ? _showtimeFilterIds[cboShowtimeFilter.SelectedIndex]
                : null;

        private void UpdateShowtimeFilterOptions(List<BookingResultEventArgs> bookings)
        {
            if (cboShowtimeFilter == null)
                return;

            int? previousShowtimeId = SelectedShowtimeId;
            _updatingShowtimeFilters = true;
            try
            {
                cboShowtimeFilter.BeginUpdate();
                cboShowtimeFilter.Items.Clear();
                _showtimeFilterIds.Clear();

                cboShowtimeFilter.Items.Add("Tất cả suất");
                _showtimeFilterIds.Add(null);

                var showtimes = bookings
                    .Where(booking => booking.Showtime != null)
                    .GroupBy(booking => booking.Showtime!.Id)
                    .Select(group => group.First())
                    .OrderBy(booking => booking.Showtime!.StartTime)
                    .ToList();

                foreach (BookingResultEventArgs booking in showtimes)
                {
                    Showtime showtime = booking.Showtime!;
                    string movieTitle = booking.Movie?.Title ?? "Phim";
                    cboShowtimeFilter.Items.Add(
                        $"{movieTitle} · {showtime.StartTime:dd/MM HH:mm} · P{showtime.RoomId}");
                    _showtimeFilterIds.Add(showtime.Id);
                }

                int restoreIndex = previousShowtimeId.HasValue
                    ? _showtimeFilterIds.FindIndex(id => id == previousShowtimeId.Value)
                    : 0;
                cboShowtimeFilter.SelectedIndex = restoreIndex >= 0 ? restoreIndex : 0;
            }
            finally
            {
                cboShowtimeFilter.EndUpdate();
                _updatingShowtimeFilters = false;
            }
        }

        private void RenderDoorList()
        {
            if (dgvLogs == null || cboFilter == null || txtSearch == null)
                return;

            string filter = cboFilter.SelectedItem?.ToString() ?? "Tất cả";
            string query = txtSearch.Text.Trim();
            int? showtimeId = SelectedShowtimeId;
            dgvLogs.Rows.Clear();

            if (filter != "Bị từ chối")
            {
                foreach (BookingResultEventArgs booking in _doorBookings)
                {
                    if (showtimeId.HasValue && booking.Showtime?.Id != showtimeId.Value)
                        continue;
                    bool checkedIn = _checkedInCodes.Contains(booking.BookingCode);
                    string status = checkedIn ? "ĐÃ VÀO" : "CHƯA VÀO";
                    if ((filter == "Đã vào" && !checkedIn) ||
                        (filter == "Chưa vào" && checkedIn))
                        continue;

                    string movie = booking.Movie?.Title ?? "—";
                    string showtime = booking.Showtime?.StartTime.ToString("HH:mm") ?? "—";
                    string seats = booking.Seats != null && booking.Seats.Count > 0
                        ? string.Join(", ", booking.Seats)
                        : "—";
                    string customer = string.IsNullOrWhiteSpace(booking.CustomerName)
                        ? "Khách lẻ"
                        : booking.CustomerName;
                    string checkInTime = _checkInTimes.TryGetValue(booking.BookingCode, out DateTime recordedTime)
                        ? recordedTime.ToString("HH:mm")
                        : UserSession.ScanLogs
                            .LastOrDefault(log => log.Status == TicketScanStatus.Valid &&
                                                  string.Equals(log.Code, booking.BookingCode,
                                                      StringComparison.OrdinalIgnoreCase))?
                            .ScanTime.ToString("HH:mm") ?? "—";

                    if (!MatchesSearch(query, booking.BookingCode, customer, movie, seats))
                        continue;

                    int rowIndex = dgvLogs.Rows.Add(
                        booking.BookingCode,
                        customer,
                        $"{movie} · {showtime}",
                        seats,
                        checkedIn ? checkInTime : "—",
                        status);
                    dgvLogs.Rows[rowIndex].Tag = booking;
                    dgvLogs.Rows[rowIndex].Cells["colStatus"].Style.ForeColor = checkedIn
                        ? Color.FromArgb(22, 163, 74)
                        : Color.FromArgb(217, 119, 6);
                }
            }

            if (filter == "Tất cả" || filter == "Bị từ chối")
            {
                foreach (TicketScanResult log in UserSession.ScanLogs.AsEnumerable().Reverse()
                             .Where(scan => !scan.IsValid))
                {
                    if (showtimeId.HasValue && log.Booking?.Showtime?.Id != showtimeId.Value)
                        continue;
                    string movie = log.Booking?.Movie?.Title ?? "—";
                    string seats = log.Booking?.Seats != null && log.Booking.Seats.Count > 0
                        ? string.Join(", ", log.Booking.Seats)
                        : "—";
                    string customer = log.Booking?.CustomerName ?? "—";
                    string status = log.Status switch
                    {
                        TicketScanStatus.AlreadyUsed => "TỪ CHỐI · ĐÃ DÙNG",
                        TicketScanStatus.WrongShowtime => "TỪ CHỐI · SAI SUẤT",
                        TicketScanStatus.Expired => "TỪ CHỐI · HẾT SUẤT",
                        _ => "TỪ CHỐI · KHÔNG HỢP LỆ"
                    };

                    if (!MatchesSearch(query, log.Code, customer, movie, seats))
                        continue;

                    int rowIndex = dgvLogs.Rows.Add(
                        log.Code,
                        customer,
                        movie,
                        seats,
                        log.ScanTime.ToString("HH:mm"),
                        status);
                    dgvLogs.Rows[rowIndex].Tag = log;
                    dgvLogs.Rows[rowIndex].Cells["colStatus"].Style.ForeColor = ColorError;
                }
            }
        }

        private static bool MatchesSearch(string query, params string[] values)
        {
            return string.IsNullOrWhiteSpace(query) || values.Any(value =>
                value.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        private void DoorList_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvLogs.Rows.Count)
                return;

            object? item = dgvLogs.Rows[e.RowIndex].Tag;
            if (item is BookingResultEventArgs booking)
            {
                bool checkedIn = _checkedInCodes.Contains(booking.BookingCode);
                lblStatus.Text = checkedIn ? "VÉ ĐÃ CHECK-IN" : "CHƯA CHECK-IN";
                lblStatus.ForeColor = checkedIn
                    ? Color.FromArgb(22, 163, 74)
                    : Color.FromArgb(217, 119, 6);
                pnlStatusBar.BackColor = checkedIn
                    ? Color.FromArgb(240, 253, 244)
                    : Color.FromArgb(255, 251, 235);
                string details = BuildDetail(booking);
                if (_checkInTimes.TryGetValue(booking.BookingCode, out DateTime checkInTime))
                    details += $"\nCHECK-IN LÚC: {checkInTime:HH:mm:ss dd/MM/yyyy}";
                lblTicketDetail.Text = details;
            }
            else if (item is TicketScanResult result)
            {
                ShowResult(result);
            }
        }

        // =========================================================
        // CẬP NHẬT DANH SÁCH VÉ CHỜ
        // =========================================================

        private void RefreshPending()
        {
            lstPending.Items.Clear();

            var pendingBookings = UserSession.GetAllTicketBookings()
                .Where(b =>
                    b.Seats != null &&
                    b.Seats.Count > 0 &&
                    !UserSession.IsCheckedIn(b.BookingCode))
                .ToList();

            foreach (var b in pendingBookings)
            {
                lstPending.Items.Add(
                    new PendingItem(
                        b.BookingCode,
                        $"{b.BookingCode,-24} " +
                        $"{b.Movie?.Title} | " +
                        $"{b.Showtime?.StartTime:HH:mm} | " +
                        $"{string.Join(",", b.Seats)}"
                    )
                );
            }

            lblPendingEmpty.Visible = pendingBookings.Count == 0;
        }

        // =========================================================
        // MODEL HIỂN THỊ VÉ CHỜ
        // =========================================================

        private sealed class PendingItem
        {
            public string Code { get; }

            private readonly string _display;

            public PendingItem(string code, string display)
            {
                Code = code;
                _display = display;
            }

            public override string ToString()
            {
                return _display;
            }
        }
    }
}
