using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using CinemaManagement.Data;
using CinemaManagement.Utils;
using Dapper;

namespace CinemaManagement.UserControls.Admin;


public partial class DashboardUC : UserControl
{
    // =========================================================
    // 1. PREMIUM NEON THEME PALETTE
    // =========================================================
    private Color ColBgMain        = Color.FromArgb(11, 16, 32);
    private Color ColCardBg        = Color.FromArgb(21, 29, 48);
    private Color ColCardElevated  = Color.FromArgb(27, 38, 59);
    private Color ColCardBorder    = Color.FromArgb(42, 56, 86);
    private Color ColNeonCyan      = Color.FromArgb(0, 245, 212);
    private Color ColElectricBlue  = Color.FromArgb(59, 130, 246);
    private Color ColNeonPurple    = Color.FromArgb(168, 85, 247);
    private Color ColEmerald       = Color.FromArgb(16, 185, 129);
    private Color ColAmber         = Color.FromArgb(245, 158, 11);
    private Color ColRose          = Color.FromArgb(244, 63, 94);
    private Color ColTextPrimary   = Color.FromArgb(241, 245, 249);
    private Color ColTextMuted     = Color.FromArgb(148, 163, 184);
    private static bool _isDarkTheme = true;

    public static bool IsDarkTheme => _isDarkTheme;
    public event Action<bool>? ThemeChanged;

    public static void SetThemePreference(bool isDark)
    {
        _isDarkTheme = isDark;
    }

    public void SetTheme(bool isDark)
    {
        ApplyTheme(isDark);
    }

    private readonly CultureInfo ViCulture = new("vi-VN");

    // =========================================================
    // 2. ANALYTICS DATA MODELS
    // =========================================================
    private sealed class AnalyticsSnapshot
    {
        public string? DatabaseError { get; set; }
        public decimal TotalRevenue { get; set; }
        public double RevenueGrowthPct { get; set; }
        public float[] RevenueSpark { get; set; } = Array.Empty<float>();

        public int TicketsSold { get; set; }
        public double TicketGrowthPct { get; set; }
        public float[] TicketSpark { get; set; } = Array.Empty<float>();

        public int SeatsSold { get; set; }
        public int TotalSeatCapacity { get; set; }
        public double OccupancyPct => TotalSeatCapacity > 0 ? Math.Round(SeatsSold * 100.0 / TotalSeatCapacity, 1) : 0;

        public int TicketsCanceled { get; set; }
        public double CancelRatePct => (TicketsSold + TicketsCanceled) > 0
            ? Math.Round(TicketsCanceled * 100.0 / (TicketsSold + TicketsCanceled), 1) : 0;

        public int TotalShowtimes { get; set; }
        public int ActiveShowtimes { get; set; }

        public int TotalCustomers { get; set; }
        public int NewCustomers { get; set; }

        // Biểu đồ 1: Doanh thu theo thời gian (Kỳ này vs Kỳ trước)
        public List<string> TimeLabels { get; set; } = new();
        public List<decimal> CurrentPeriodRevenue { get; set; } = new();
        public List<decimal> PreviousPeriodRevenue { get; set; } = new();

        // Biểu đồ 2: Vé bán vs Vé hủy
        public List<int> SoldSeries { get; set; } = new();
        public List<int> CanceledSeries { get; set; } = new();

        // Biểu đồ 3 & Top Movies: Doanh thu theo phim
        public List<MovieRankItem> TopMovies { get; set; } = new();

        // Biểu đồ 5: Doanh thu theo phòng chiếu
        public List<RoomPerfItem> RoomPerformances { get; set; } = new();

        // Biểu đồ 6: Phương thức thanh toán
        public List<(string Name, decimal Amount, int TxCount, Color Accent)> PaymentMethods { get; set; } = new();

        // Biểu đồ 7: Hiệu suất suất chiếu theo khung giờ
        public List<(string SlotName, int Tickets, decimal Revenue)> TimeSlotPerfs { get; set; } = new();

        // Hoạt động gần đây
        public List<ActivityItem> RecentActivities { get; set; } = new();
    }

    private sealed class MovieRankItem
    {
        public string Title { get; set; } = "";
        public string Genre { get; set; } = "Hành động / Viễn tưởng";
        public int Tickets { get; set; }
        public decimal Revenue { get; set; }
        public double SharePct { get; set; }
        public Color BadgeColor { get; set; }
    }

    private sealed class RoomPerfItem
    {
        public string RoomName { get; set; } = "";
        public int Tickets { get; set; }
        public decimal Revenue { get; set; }
    }

    private sealed class ActivityItem
    {
        public string Icon { get; set; } = "🎫";
        public string Content { get; set; } = "";
        public string TimeAgo { get; set; } = "";
        public string Status { get; set; } = "";
        public Color StatusColor { get; set; }
    }

    private sealed class RoomFilterItem
    {
        public RoomFilterItem() { }

        public int? Id { get; set; }
        public string Name { get; set; } = "";

        public override string ToString() => Name;
    }

    private sealed class DailyRevenueRow
    {
        public DailyRevenueRow() { }

        public string Day { get; set; } = "";
        public decimal Revenue { get; set; }
    }

    private sealed class DailyTicketRow
    {
        public DailyTicketRow() { }
        public string Day { get; set; } = "";
        public int Sold { get; set; }
        public int Canceled { get; set; }
    }

    private sealed class PeriodBucket
    {
        public DateTime Start { get; init; }
        public DateTime End { get; init; }
        public string Label { get; init; } = "";
        public decimal Revenue { get; set; }
        public int Sold { get; set; }
        public int Canceled { get; set; }
    }

    private sealed class MovieSummaryRow
    {
        public MovieSummaryRow() { }
        public string Title { get; set; } = "";
        public string Genre { get; set; } = "";
        public int Tickets { get; set; }
        public decimal Revenue { get; set; }
    }

    private sealed class RoomSummaryRow
    {
        public RoomSummaryRow() { }
        public string Name { get; set; } = "";
        public int Tickets { get; set; }
        public decimal Revenue { get; set; }
    }

    private sealed class PaymentSummaryRow
    {
        public PaymentSummaryRow() { }
        public string Name { get; set; } = "";
        public decimal Amount { get; set; }
        public int Transactions { get; set; }
    }

    private sealed class TimeSlotSummaryRow
    {
        public TimeSlotSummaryRow() { }
        public int Slot { get; set; }
        public int Tickets { get; set; }
        public decimal Revenue { get; set; }
    }

    private sealed class RecentBookingRow
    {
        public RecentBookingRow() { }
        public string BookingCode { get; set; } = "";
        public string BookingTime { get; set; } = "";
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "";
        public string BookingStatus { get; set; } = "";
        public int TicketCount { get; set; }
        public string Movies { get; set; } = "";
    }

    // =========================================================
    // 3. UI & ANIMATION STATE
    // =========================================================
    private AnalyticsSnapshot _data = new();
    private float _animProgress = 0f;
    private System.Windows.Forms.Timer _animTimer = null!;

    // Filter Controls
    private ComboBox cboTimeRange = null!;
    private DateTimePicker dtpFrom = null!;
    private DateTimePicker dtpTo = null!;
    private Panel pnlCustomDates = null!;
    private ComboBox cboCinemaFilter = null!;
    private Button btnRefresh = null!;
    private NeonThemeButton btnTheme = null!;
    private Label lblLastUpdated = null!;
    private Label lblDemoBadge = null!;
    private Label lblMovieTitle = null!;
    private TableLayoutPanel _dashboardRoot = null!;

    // KPI Cards
    private readonly Panel[] _kpiCards = new Panel[6];
    private readonly int[] _hoveredKpi = new int[6];

    // Interactive Chart Panels & Hover Tracking
    private Panel pnlChartRevenue = null!;
    private int _hoverRevIdx = -1;

    private Panel pnlChartOccupancy = null!;

    private Panel pnlChartSoldCancel = null!;
    private int _hoverBarIdx = -1;

    private Panel pnlChartPayment = null!;
    private int _hoverPayIdx = -1;

    private Panel pnlChartTopMovies = null!;
    private Panel pnlChartRooms = null!;
    private int _hoverRoomIdx = -1;

    private Panel pnlChartTimeSlots = null!;
    private int _hoverSlotIdx = -1;

    private Panel pnlRecentActivities = null!;

    // =========================================================
    // 4. CONSTRUCTOR
    // =========================================================
    public DashboardUC()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint |
                 ControlStyles.ResizeRedraw, true);

        BuildDashboardUI();
        ApplyTheme(_isDarkTheme);
        SetupAnimation();
        LoadAndAnimateData();

        this.Disposed += (s, e) =>
        {
            _animTimer?.Stop();
            _animTimer?.Dispose();
        };

        }

    // =========================================================
    // 5. XÂY DỰNG BỐ CỤC 6 HÀNG CHUẨN
    // =========================================================
    private void BuildDashboardUI()
    {
        Dock = DockStyle.Fill;
        AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = ColBgMain;
        this.AutoScroll = true;
        this.Padding = new Padding(20, 16, 20, 24);

        var tlpRoot = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = ColBgMain,
            Margin = new Padding(0)
        };
        _dashboardRoot = tlpRoot;
        tlpRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        // HÀNG 1: HEADER + BỘ LỌC
        tlpRoot.Controls.Add(BuildHeaderSection(), 0, 0);

        // HÀNG 2: 6 KPI CARDS
        tlpRoot.Controls.Add(BuildKpiGridSection(), 0, 1);

        // HÀNG 3: DOANH THU THEO THỜI GIAN (65%) + TỶ LỆ LẤP ĐẦY GHẾ (35%)
        var row3 = CreateSplitRow(65F, 35F, 340);
        pnlChartRevenue = CreateChartHostPanel();
        pnlChartRevenue.Paint += PaintRevenueTimeChart;
        pnlChartRevenue.MouseMove += OnRevenueMouseMove;
        pnlChartRevenue.MouseLeave += (s, e) => { _hoverRevIdx = -1; pnlChartRevenue.Invalidate(); };

        pnlChartOccupancy = CreateChartHostPanel();
        pnlChartOccupancy.Paint += PaintOccupancyDoughnutChart;

        row3.Controls.Add(pnlChartRevenue, 0, 0);
        row3.Controls.Add(pnlChartOccupancy, 1, 0);
        tlpRoot.Controls.Add(row3, 0, 2);

        // HÀNG 4: SO SÁNH VÉ BÁN / VÉ HỦY (58%) + PHÂN BỐ PHƯƠNG THỨC THANH TOÁN (42%)
        var row4 = CreateSplitRow(58F, 42F, 320);
        pnlChartSoldCancel = CreateChartHostPanel();
        pnlChartSoldCancel.Paint += PaintSoldVsCanceledChart;
        pnlChartSoldCancel.MouseMove += OnSoldCancelMouseMove;
        pnlChartSoldCancel.MouseLeave += (s, e) => { _hoverBarIdx = -1; pnlChartSoldCancel.Invalidate(); };

        pnlChartPayment = CreateChartHostPanel();
        pnlChartPayment.Paint += PaintPaymentMethodsChart;
        pnlChartPayment.MouseMove += OnPaymentMouseMove;
        pnlChartPayment.MouseLeave += (s, e) => { _hoverPayIdx = -1; pnlChartPayment.Invalidate(); };

        row4.Controls.Add(pnlChartSoldCancel, 0, 0);
        row4.Controls.Add(pnlChartPayment, 1, 0);
        tlpRoot.Controls.Add(row4, 0, 3);

        // HÀNG 5: TOP PHIM DOANH THU CAO (55%) + DOANH THU THEO PHÒNG CHIẾU (45%)
        var row5 = CreateSplitRow(55F, 45F, 360);
        pnlChartTopMovies = CreateChartHostPanel();
        pnlChartTopMovies.Paint += PaintTopMoviesLeaderboard;

        pnlChartRooms = CreateChartHostPanel();
        pnlChartRooms.Paint += PaintRoomRevenueChart;
        pnlChartRooms.MouseMove += OnRoomMouseMove;
        pnlChartRooms.MouseLeave += (s, e) => { _hoverRoomIdx = -1; pnlChartRooms.Invalidate(); };

        row5.Controls.Add(pnlChartTopMovies, 0, 0);
        row5.Controls.Add(pnlChartRooms, 1, 0);
        tlpRoot.Controls.Add(row5, 0, 4);

        // HÀNG 6: HIỆU SUẤT SUẤT CHIẾU THEO KHUNG GIỜ (48%) + HOẠT ĐỘNG GẦN ĐÂY (52%)
        var row6 = CreateSplitRow(48F, 52F, 320);
        pnlChartTimeSlots = CreateChartHostPanel();
        pnlChartTimeSlots.Paint += PaintTimeSlotsChart;
        pnlChartTimeSlots.MouseMove += OnSlotMouseMove;
        pnlChartTimeSlots.MouseLeave += (s, e) => { _hoverSlotIdx = -1; pnlChartTimeSlots.Invalidate(); };

        pnlRecentActivities = CreateChartHostPanel();
        pnlRecentActivities.Paint += PaintRecentActivitiesCard;

        row6.Controls.Add(pnlChartTimeSlots, 0, 0);
        row6.Controls.Add(pnlRecentActivities, 1, 0);
        tlpRoot.Controls.Add(row6, 0, 5);
        this.Controls.Add(tlpRoot);
    }

    private TableLayoutPanel CreateSplitRow(float leftPercent, float rightPercent, int height)
    {
        var tlp = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = height,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 16),
            BackColor = Color.Transparent
        };
        tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, leftPercent));
        tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, rightPercent));
        tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        return tlp;
    }

    private Panel CreateChartHostPanel()
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(6, 0, 6, 0),
            BackColor = ColCardBg
        };
        EnableDoubleBuffering(pnl);
        return pnl;
    }

    private static void EnableDoubleBuffering(Control ctrl)
    {
        typeof(Control).InvokeMember("DoubleBuffered",
            BindingFlags.SetProperty |
            BindingFlags.Instance |
            BindingFlags.NonPublic,
            null, ctrl, new object[] { true });
    }

    // =========================================================
    // 6. HÀNG 1: HEADER & BỘ LỌC HOẠT ĐỘNG THỰC TẾ
    // =========================================================
    private Control BuildHeaderSection()
    {
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 84,
            Margin = new Padding(6, 0, 6, 14),
            BackColor = ColCardBg
        };
        EnableDoubleBuffering(pnlHeader);

        pnlHeader.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, pnlHeader.Width - 1, pnlHeader.Height - 1);
            using var path = CreateRoundedPath(rect, 14);
            using var brush = new LinearGradientBrush(rect, ColCardBg, ColCardElevated, 0F);
            e.Graphics.FillPath(brush, path);
            using var pen = new Pen(ColCardBorder, 1.2F);
            e.Graphics.DrawPath(pen, path);

            using var accentBrush = new LinearGradientBrush(new Rectangle(16, 18, 4, 46), ColNeonCyan, ColNeonPurple, 90F);
            e.Graphics.FillRectangle(accentBrush, 16, 18, 4, 46);
        };
        

        lblMovieTitle = new Label
        {
            Text = "THE MAGICIAN'S ELEPHANT",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = ColTextPrimary,
            Dock = DockStyle.Top,
            Height = 40,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        var lblTitle = new Label
        {
            Text = "CINEMA ANALYTICS PRO",
            Font = new Font("Segoe UI", 15.5F, FontStyle.Bold),
            ForeColor = ColTextPrimary,
            Location = new Point(30, 14),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        lblDemoBadge = new Label
        {
            Text = "LIVE DB",
            Font = new Font("Segoe UI", 7.8F, FontStyle.Bold),
            ForeColor = ColBgMain,
            BackColor = ColNeonCyan,
            Location = new Point(295, 19),
            Size = new Size(76, 20),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblSub = new Label
        {
            Text = "Tổng quan hiệu suất kinh doanh, doanh thu & tỷ lệ lấp đầy rạp chiếu phim",
            Font = new Font("Segoe UI", 9F),
            ForeColor = ColTextMuted,
            Location = new Point(30, 46),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        var flpFilters = new FlowLayoutPanel
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            BackColor = Color.Transparent,
            Location = new Point(pnlHeader.Width - 650, 16)
        };

        cboTimeRange = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            BackColor = ColCardElevated,
            ForeColor = ColTextPrimary,
            FlatStyle = FlatStyle.Flat,
            Width = 135,
            Margin = new Padding(0, 2, 8, 0)
        };
        cboTimeRange.Items.AddRange(new object[]
        {
            "Hôm nay",
            "7 ngày qua",
            "30 ngày qua",
            "Tháng này",
            "Năm nay",
            "Tùy chỉnh ngày"
        });
        cboTimeRange.SelectedIndex = 2;
        cboTimeRange.SelectedIndexChanged += (s, e) =>
        {
            pnlCustomDates.Visible = cboTimeRange.SelectedItem?.ToString() == "Tùy chỉnh ngày";
            RepositionFilters(pnlHeader, flpFilters);
            LoadAndAnimateData();
        };

        pnlCustomDates = new Panel
        {
            Visible = false,
            Size = new Size(218, 28),
            BackColor = Color.Transparent,
            Margin = new Padding(0, 1, 8, 0)
        };
        dtpFrom = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Width = 102,
            Location = new Point(0, 1),
            Value = DateTime.Today.AddDays(-14)
        };
        dtpTo = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Width = 102,
            Location = new Point(110, 1),
            Value = DateTime.Today
        };
        dtpFrom.ValueChanged += (s, e) => LoadAndAnimateData();
        dtpTo.ValueChanged += (s, e) => LoadAndAnimateData();
        pnlCustomDates.Controls.Add(dtpFrom);
        pnlCustomDates.Controls.Add(dtpTo);

        cboCinemaFilter = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            BackColor = ColCardElevated,
            ForeColor = ColTextPrimary,
            FlatStyle = FlatStyle.Flat,
            Width = 165,
            Margin = new Padding(0, 2, 8, 0)
        };
        cboCinemaFilter.Items.Add(new RoomFilterItem { Name = "Tất cả phòng" });
        try
        {
            using var connection = DatabaseConnection.GetConnection();
            connection.Open();
            foreach (var room in connection.Query<RoomFilterItem>(
                         "SELECT Id, Name FROM Rooms ORDER BY Name;"))
                cboCinemaFilter.Items.Add(room);
        }
        catch
        {
            // The dashboard will report the database error when it loads its data.
        }
        cboCinemaFilter.SelectedIndex = 0;
        cboCinemaFilter.SelectedIndexChanged += (s, e) => LoadAndAnimateData();


        btnRefresh = new Button
        {
            Text = "⟳ Làm mới",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = ColBgMain,
            BackColor = ColNeonCyan,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(102, 28),
            Margin = new Padding(0, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        btnRefresh.FlatAppearance.BorderSize = 0;
        btnRefresh.Click += (s, e) => LoadAndAnimateData();

        btnTheme = new NeonThemeButton
        {
            Text = _isDarkTheme ? "☀ Chế độ sáng" : "☾ Chế độ tối",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = ColNeonCyan,
            NeonColor = ColNeonCyan,
            BackColor = ColCardElevated,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(122, 28),
            Margin = new Padding(8, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        btnTheme.FlatAppearance.BorderColor = ColCardBorder;
        btnTheme.Click += (s, e) => ApplyTheme(!_isDarkTheme);

        lblLastUpdated = new Label
        {
            Text = $"Cập nhật lúc: {DateTime.Now:HH:mm:ss dd/MM/yyyy}",
            Font = new Font("Segoe UI", 8F),
            ForeColor = ColTextMuted,
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = Color.Transparent
        };

        flpFilters.Controls.Add(cboTimeRange);
        flpFilters.Controls.Add(pnlCustomDates);
        flpFilters.Controls.Add(cboCinemaFilter);
        flpFilters.Controls.Add(btnRefresh);
        flpFilters.Controls.Add(btnTheme);

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblDemoBadge);
        pnlHeader.Controls.Add(lblSub);
        pnlHeader.Controls.Add(flpFilters);
        pnlHeader.Controls.Add(lblLastUpdated);

        pnlHeader.Resize += (s, e) => RepositionFilters(pnlHeader, flpFilters);
        RepositionFilters(pnlHeader, flpFilters);

        return pnlHeader;
    }

    private void RepositionFilters(Panel header, FlowLayoutPanel flp)
    {
        flp.Location = new Point(Math.Max(380, header.ClientSize.Width - flp.Width - 18), 16);
        lblLastUpdated.Location = new Point(Math.Max(380, header.ClientSize.Width - lblLastUpdated.Width - 20), 52);
    }

    private void ApplyTheme(bool isDark)
    {
        _isDarkTheme = isDark;
        if (isDark)
        {
            ColBgMain = Color.FromArgb(11, 16, 32);
            ColCardBg = Color.FromArgb(21, 29, 48);
            ColCardElevated = Color.FromArgb(27, 38, 59);
            ColCardBorder = Color.FromArgb(42, 56, 86);
            ColNeonCyan = Color.FromArgb(0, 245, 212);
            ColElectricBlue = Color.FromArgb(59, 130, 246);
            ColNeonPurple = Color.FromArgb(168, 85, 247);
            ColEmerald = Color.FromArgb(16, 185, 129);
            ColAmber = Color.FromArgb(245, 158, 11);
            ColRose = Color.FromArgb(244, 63, 94);
            ColTextPrimary = Color.FromArgb(241, 245, 249);
            ColTextMuted = Color.FromArgb(148, 163, 184);
        }
        else
        {
            ColBgMain = Color.FromArgb(241, 245, 249);
            ColCardBg = Color.White;
            ColCardElevated = Color.FromArgb(226, 232, 240);
            ColCardBorder = Color.FromArgb(203, 213, 225);
            ColNeonCyan = Color.FromArgb(8, 145, 178);
            ColElectricBlue = Color.FromArgb(37, 99, 235);
            ColNeonPurple = Color.FromArgb(124, 58, 237);
            ColEmerald = Color.FromArgb(5, 150, 105);
            ColAmber = Color.FromArgb(180, 83, 9);
            ColRose = Color.FromArgb(225, 29, 72);
            ColTextPrimary = Color.FromArgb(15, 23, 42);
            ColTextMuted = Color.FromArgb(71, 85, 105);
        }

        BackColor = ColBgMain;
        if (_dashboardRoot == null) return;
        _dashboardRoot.BackColor = ColBgMain;
        ApplyThemeToControl(_dashboardRoot);

        foreach (var card in _kpiCards)
            if (card != null) card.BackColor = ColCardBg;

        if (btnRefresh != null)
        {
            btnRefresh.BackColor = ColNeonCyan;
            btnRefresh.ForeColor = isDark ? Color.FromArgb(8, 30, 30) : Color.White;
        }
        if (btnTheme != null)
        {
            btnTheme.Text = isDark ? "☀ Chế độ sáng" : "☾ Chế độ tối";
            btnTheme.BackColor = ColCardElevated;
            btnTheme.NeonColor = ColNeonCyan;
            btnTheme.ForeColor = ColNeonCyan;
            btnTheme.Invalidate();
        }
        if (lblDemoBadge != null)
        {
            lblDemoBadge.BackColor = _data.DatabaseError == null ? ColNeonCyan : ColRose;
            lblDemoBadge.ForeColor = isDark ? Color.FromArgb(8, 30, 30) : Color.White;
        }
        if (lblLastUpdated != null) lblLastUpdated.ForeColor = ColTextMuted;

        _dashboardRoot.Invalidate(true);
        ThemeChanged?.Invoke(isDark);
    }

    private void ApplyThemeToControl(Control control)
    {
        if (control is ComboBox comboBox)
        {
            comboBox.BackColor = ColCardElevated;
            comboBox.ForeColor = ColTextPrimary;
        }
        else if (control is DateTimePicker dateTimePicker)
        {
            dateTimePicker.CalendarMonthBackground = ColCardBg;
            dateTimePicker.CalendarForeColor = ColTextPrimary;
            dateTimePicker.CalendarTitleBackColor = ColCardElevated;
            dateTimePicker.CalendarTitleForeColor = ColTextPrimary;
        }
        else if (control is Label label)
        {
            label.ForeColor = ColTextPrimary;
        }
        else if (control is TableLayoutPanel table && table.BackColor != Color.Transparent)
        {
            table.BackColor = ColBgMain;
        }
        else if (control is Panel panel && panel.BackColor != Color.Transparent)
        {
            panel.BackColor = ColCardBg;
        }

        foreach (Control child in control.Controls)
            ApplyThemeToControl(child);
    }

    // =========================================================
    // 7. HÀNG 2: 6 KPI CARDS CÓ SPARKLINE & HOVER LIFT
    // =========================================================
    private Control BuildKpiGridSection()
    {
        var tlpKpi = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 128,
            ColumnCount = 6,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 16),
            BackColor = Color.Transparent
        };

        for (int i = 0; i < 6; i++)
        {
            tlpKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.666F));
            int idx = i;
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(24, 29, 44),
                Padding = new Padding(20),
                Margin = new Padding(0, 5, 0, 0)
            };
            EnableDoubleBuffering(card);
            card.Paint += (s, e) => PaintSingleKpiCard(e.Graphics, card, idx);
            card.MouseEnter += (s, e) => { _hoveredKpi[idx] = 1; card.Invalidate(); };
            card.MouseLeave += (s, e) => { _hoveredKpi[idx] = 0; card.Invalidate(); };

            _kpiCards[i] = card;
            tlpKpi.Controls.Add(card, i, 0);
        }
        tlpKpi.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        return tlpKpi;
    }

    private void PaintSingleKpiCard(Graphics g, Panel card, int idx)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        bool isHover = _hoveredKpi[idx] == 1;
        int liftOffset = isHover ? -2 : 0;

        var rect = new Rectangle(1, 2 + liftOffset, card.Width - 3, card.Height - 5);
        using var path = CreateRoundedPath(rect, 14);

        using (var bgBrush = new LinearGradientBrush(rect,
            isHover ? ColCardElevated : ColCardBg,
            ColBgMain, 45F))
        {
            g.FillPath(bgBrush, path);
        }

        Color accent = idx switch
        {
            0 => ColNeonCyan,
            1 => ColElectricBlue,
            2 => ColEmerald,
            3 => ColRose,
            4 => ColNeonPurple,
            _ => ColAmber
        };

        using (var borderPen = new Pen(isHover ? accent : ColCardBorder, isHover ? 1.5F : 1.0F))
        {
            g.DrawPath(borderPen, path);
        }

        using (var topBar = new SolidBrush(accent))
        {
            g.FillRectangle(topBar, rect.X + 14, rect.Y, rect.Width - 28, 3);
        }

        float p = EaseOutCubic(_animProgress);
        string title = "";
        string mainValue = "";
        string subBadge = "";
        bool isPositive = true;
        string iconGlyph = "";
        float[]? spark = null;

        switch (idx)
        {
            case 0:
                title = "TỔNG DOANH THU";
                decimal rev = _data.TotalRevenue * (decimal)p;
                mainValue = FormatCompactVnd(rev);
                subBadge = $"{(_data.RevenueGrowthPct >= 0 ? "+" : "")}{_data.RevenueGrowthPct:0.0}%";
                isPositive = _data.RevenueGrowthPct >= 0;
                iconGlyph = "💰";
                spark = _data.RevenueSpark;
                break;

            case 1:
                title = "VÉ ĐÃ BÁN";
                int sold = (int)Math.Round(_data.TicketsSold * p);
                mainValue = sold.ToString("N0", ViCulture);
                subBadge = $"{(_data.TicketGrowthPct >= 0 ? "+" : "")}{_data.TicketGrowthPct:0.0}%";
                isPositive = _data.TicketGrowthPct >= 0;
                iconGlyph = "🎟";
                spark = _data.TicketSpark;
                break;

            case 2:
                title = "GHẾ ĐÃ LẤP ĐẦY";
                int seats = (int)Math.Round(_data.SeatsSold * p);
                mainValue = seats.ToString("N0", ViCulture);
                subBadge = $"{(_data.OccupancyPct * p):0.0}% lấp đầy";
                isPositive = _data.OccupancyPct >= 50;
                iconGlyph = "💺";
                break;

            case 3:
                title = "VÉ ĐÃ HỦY";
                int canceled = (int)Math.Round(_data.TicketsCanceled * p);
                mainValue = canceled.ToString("N0", ViCulture);
                subBadge = $"{(_data.CancelRatePct * p):0.0}% hủy";
                isPositive = false;
                iconGlyph = "⚠";
                break;

            case 4:
                title = "SUẤT CHIẾU";
                int shows = (int)Math.Round(_data.TotalShowtimes * p);
                mainValue = shows.ToString("N0", ViCulture);
                subBadge = $"{_data.ActiveShowtimes} đang chiếu";
                isPositive = true;
                iconGlyph = "🎬";
                break;

            case 5:
                title = "KHÁCH HÀNG";
                int cust = (int)Math.Round(_data.TotalCustomers * p);
                mainValue = cust.ToString("N0", ViCulture);
                subBadge = $"+{_data.NewCustomers} mới";
                isPositive = true;
                iconGlyph = "👥";
                break;
        }

        using var fontTitle = new Font("Segoe UI", 8F, FontStyle.Bold);
        using var brushMuted = new SolidBrush(ColTextMuted);
        g.DrawString(title, fontTitle, brushMuted, rect.X + 14, rect.Y + 14);

        using var fontIcon = new Font("Segoe UI Emoji", 12F);
        using var brushIcon = new SolidBrush(accent);
        g.DrawString(iconGlyph, fontIcon, brushIcon, rect.Right - 36, rect.Y + 10);

        using var fontVal = new Font("Segoe UI", 16.5F, FontStyle.Bold);
        using var brushWhite = new SolidBrush(ColTextPrimary);
        g.DrawString(mainValue, fontVal, brushWhite, rect.X + 14, rect.Y + 38);

        Color badgeColor = isPositive ? ColEmerald : ColRose;
        using var fontBadge = new Font("Segoe UI", 8.2F, FontStyle.Bold);
        using var brushBadge = new SolidBrush(badgeColor);
        g.DrawString(subBadge, fontBadge, brushBadge, rect.X + 14, rect.Bottom - 26);

        if (spark != null && spark.Length >= 2)
        {
            var sparkRect = new Rectangle(rect.Right - 86, rect.Bottom - 34, 72, 22);
            DrawMiniSparkline(g, sparkRect, spark, accent, p);
        }
    }

    private static void DrawMiniSparkline(Graphics g, Rectangle bounds, float[] values, Color color, float progress)
    {
        float min = values.Min();
        float max = values.Max();
        float range = Math.Max(1f, max - min);

        int visiblePoints = Math.Max(2, (int)Math.Ceiling(values.Length * progress));
        var pts = new List<PointF>();
        for (int i = 0; i < visiblePoints; i++)
        {
            float x = bounds.X + (bounds.Width * i / (float)(values.Length - 1));
            float norm = (values[i] - min) / range;
            float y = bounds.Bottom - (norm * bounds.Height);
            pts.Add(new PointF(x, y));
        }

        if (pts.Count < 2) return;

        using var pen = new Pen(color, 2F);
        g.DrawCurve(pen, pts.ToArray(), 0.35F);
    }

    // =========================================================
    // 8. BIỂU ĐỒ 1: DOANH THU THEO THỜI GIAN (AREA + LINE SO SÁNH)
    // =========================================================
    private void PaintRevenueTimeChart(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var cardRect = DrawChartCardFrame(g, pnlChartRevenue,
            "DOANH THU THEO THỜI GIAN",
            "So sánh doanh thu kỳ hiện tại và kỳ trước (VNĐ)");

        DrawLegendDot(g, cardRect.Right - 230, cardRect.Y + 18, ColNeonCyan, "Kỳ hiện tại");
        DrawLegendDot(g, cardRect.Right - 115, cardRect.Y + 18, ColNeonPurple, "Kỳ trước");

        var plot = new Rectangle(cardRect.X + 64, cardRect.Y + 56, cardRect.Width - 84, cardRect.Height - 94);
        if (plot.Width <= 40 || plot.Height <= 40 || _data.CurrentPeriodRevenue.Count < 2) return;

        if (_data.CurrentPeriodRevenue.Count == 0 || _data.PreviousPeriodRevenue.Count == 0) return;

        decimal maxVal = Math.Max(100000m,
            Math.Max(_data.CurrentPeriodRevenue.Max(), _data.PreviousPeriodRevenue.Max()) * 1.15m);

        using var gridPen = new Pen(ColCardBorder, 1F) { DashStyle = DashStyle.Dash };
        using var fontAxis = new Font("Segoe UI", 8F);
        using var brushAxis = new SolidBrush(ColTextMuted);

        for (int i = 0; i <= 4; i++)
        {
            int y = plot.Bottom - (plot.Height * i / 4);
            g.DrawLine(gridPen, plot.X, y, plot.Right, y);
            decimal tickVal = maxVal * i / 4m;
            string lbl = FormatShortNumber(tickVal);
            var sz = g.MeasureString(lbl, fontAxis);
            g.DrawString(lbl, fontAxis, brushAxis, plot.X - sz.Width - 8, y - sz.Height / 2);
        }

        int n = _data.CurrentPeriodRevenue.Count;
        float p = EaseOutCubic(_animProgress);

        PointF[] curPts = new PointF[n];
        PointF[] prevPts = new PointF[n];

        for (int i = 0; i < n; i++)
        {
            float x = plot.X + (plot.Width * i / (float)(n - 1));
            float curRatio = (float)(_data.CurrentPeriodRevenue[i] / maxVal) * p;
            float prevRatio = (float)(_data.PreviousPeriodRevenue[i] / maxVal) * p;

            curPts[i] = new PointF(x, plot.Bottom - curRatio * plot.Height);
            prevPts[i] = new PointF(x, plot.Bottom - prevRatio * plot.Height);

            if (n <= 10 || i % 2 == 0 || i == n - 1)
            {
                string xLbl = _data.TimeLabels[i];
                var szX = g.MeasureString(xLbl, fontAxis);
                g.DrawString(xLbl, fontAxis, brushAxis, x - szX.Width / 2, plot.Bottom + 8);
            }
        }

        using (var prevPen = new Pen(Color.FromArgb(190, ColNeonPurple), 2F) { DashStyle = DashStyle.Dash })
        {
            g.DrawCurve(prevPen, prevPts, 0.35F);
        }

        using (var areaPath = new GraphicsPath())
        {
            areaPath.AddCurve(curPts, 0.35F);
            areaPath.AddLine(curPts[^1].X, plot.Bottom, curPts[0].X, plot.Bottom);
            areaPath.CloseFigure();

            using var fillBrush = new LinearGradientBrush(plot,
                Color.FromArgb(95, ColNeonCyan),
                Color.FromArgb(5, ColElectricBlue), 90F);
            g.FillPath(fillBrush, areaPath);
        }

        using (var curPen = new Pen(ColNeonCyan, 2.8F))
        {
            g.DrawCurve(curPen, curPts, 0.35F);
        }

        for (int i = 0; i < n; i++)
        {
            bool isHover = i == _hoverRevIdx;
            float r = isHover ? 6F : 3.5F;
            using var dotBrush = new SolidBrush(isHover ? ColCardBg : ColNeonCyan);
            g.FillEllipse(dotBrush, curPts[i].X - r, curPts[i].Y - r, r * 2, r * 2);
            if (isHover)
            {
                using var ringPen = new Pen(ColNeonCyan, 2F);
                g.DrawEllipse(ringPen, curPts[i].X - 9, curPts[i].Y - 9, 18, 18);
            }
        }

        if (_hoverRevIdx >= 0 && _hoverRevIdx < n)
        {
            var pt = curPts[_hoverRevIdx];
            using var vLine = new Pen(Color.FromArgb(120, ColNeonCyan), 1F) { DashStyle = DashStyle.Dot };
            g.DrawLine(vLine, pt.X, plot.Top, pt.X, plot.Bottom);

            string tipTitle = $"Ngày {_data.TimeLabels[_hoverRevIdx]}";
            string tipLine1 = $"Kỳ này: {_data.CurrentPeriodRevenue[_hoverRevIdx]:N0} đ";
            string tipLine2 = $"Kỳ trước: {_data.PreviousPeriodRevenue[_hoverRevIdx]:N0} đ";
            DrawFloatingTooltip(g, pnlChartRevenue.ClientRectangle, new Point((int)pt.X, (int)pt.Y - 12),
                tipTitle, tipLine1, tipLine2, ColNeonCyan);
        }
    }

    private void OnRevenueMouseMove(object? sender, MouseEventArgs e)
    {
        int n = _data.CurrentPeriodRevenue.Count;
        if (n < 2) return;
        int plotLeft = 70;
        int plotWidth = pnlChartRevenue.Width - 96;
        if (e.X < plotLeft - 15 || e.X > plotLeft + plotWidth + 15)
        {
            if (_hoverRevIdx != -1) { _hoverRevIdx = -1; pnlChartRevenue.Invalidate(); }
            return;
        }
        float step = plotWidth / (float)(n - 1);
        int idx = Math.Clamp((int)Math.Round((e.X - plotLeft) / step), 0, n - 1);
        if (idx != _hoverRevIdx)
        {
            _hoverRevIdx = idx;
            pnlChartRevenue.Invalidate();
        }
    }

    // =========================================================
    // 9. BIỂU ĐỒ 4: TỶ LỆ LẤP ĐẦY GHẾ (DOUGHNUT + RADIAL RING)
    // =========================================================
    private void PaintOccupancyDoughnutChart(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var cardRect = DrawChartCardFrame(g, pnlChartOccupancy,
            "TỶ LỆ LẤP ĐẦY GHẾ",
            "Hiệu suất khai thác ghế toàn rạp");

        float p = EaseOutCubic(_animProgress);
        float pct = (float)_data.OccupancyPct * p;
        float sweepAngle = 360F * (pct / 100F);

        int dia = Math.Min(cardRect.Width - 60, cardRect.Height - 135);
        dia = Math.Max(110, dia);
        var ringRect = new Rectangle(
            cardRect.X + (cardRect.Width - dia) / 2,
            cardRect.Y + 56,
            dia, dia);

        using (var bgPen = new Pen(ColCardElevated, 18F))
        {
            g.DrawEllipse(bgPen, ringRect);
        }

        if (sweepAngle > 1F)
        {
            using var arcBrush = new LinearGradientBrush(ringRect, ColNeonCyan, ColElectricBlue, 45F);
            using var arcPen = new Pen(arcBrush, 18F)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            g.DrawArc(arcPen, ringRect, -90F, sweepAngle);
        }

        using var fontCenter = new Font("Segoe UI", 20F, FontStyle.Bold);
        using var brushWhite = new SolidBrush(ColTextPrimary);
        string pctText = $"{pct:0.0}%";
        var szPct = g.MeasureString(pctText, fontCenter);
        g.DrawString(pctText, fontCenter, brushWhite,
            ringRect.X + (ringRect.Width - szPct.Width) / 2,
            ringRect.Y + (ringRect.Height - szPct.Height) / 2 - 8);

        using var fontSubCenter = new Font("Segoe UI", 8.5F);
        using var brushMuted = new SolidBrush(ColTextMuted);
        string subTxt = "Đã lấp đầy";
        var szSub = g.MeasureString(subTxt, fontSubCenter);
        g.DrawString(subTxt, fontSubCenter, brushMuted,
            ringRect.X + (ringRect.Width - szSub.Width) / 2,
            ringRect.Y + (ringRect.Height / 2) + 14);

        int emptySeats = Math.Max(0, _data.TotalSeatCapacity - _data.SeatsSold);
        int legendY = ringRect.Bottom + 18;
        DrawLegendDot(g, cardRect.X + 24, legendY, ColNeonCyan, $"Đã bán: {_data.SeatsSold:N0} ghế");
        DrawLegendDot(g, cardRect.X + cardRect.Width / 2 + 8, legendY, ColCardElevated, $"Còn trống: {emptySeats:N0} ghế");
    }

    // =========================================================
    // 10. BIỂU ĐỒ 2: SO SÁNH VÉ BÁN VÀ VÉ HỦY (GROUPED BAR CHART)
    // =========================================================
    private void PaintSoldVsCanceledChart(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var cardRect = DrawChartCardFrame(g, pnlChartSoldCancel,
            "SO SÁNH VÉ BÁN & VÉ HỦY",
            "Tương quan vé thành công và vé hủy theo ngày");

        DrawLegendDot(g, cardRect.Right - 210, cardRect.Y + 18, ColEmerald, "Vé bán");
        DrawLegendDot(g, cardRect.Right - 105, cardRect.Y + 18, ColRose, "Vé hủy");

        var plot = new Rectangle(cardRect.X + 48, cardRect.Y + 56, cardRect.Width - 68, cardRect.Height - 92);
        int n = Math.Min(_data.TimeLabels.Count, _data.SoldSeries.Count);
        if (plot.Width <= 40 || plot.Height <= 40 || n == 0) return;
        if (_data.SoldSeries.All(value => value == 0) && _data.CanceledSeries.All(value => value == 0)) return;

        int maxVal = Math.Max(10, (int)(_data.SoldSeries.Max() * 1.18));
        float p = EaseOutCubic(_animProgress);

        using var gridPen = new Pen(ColCardBorder, 1F) { DashStyle = DashStyle.Dash };
        using var fontAxis = new Font("Segoe UI", 8F);
        using var fontVal = new Font("Segoe UI", 7.5F, FontStyle.Bold);
        using var brushMuted = new SolidBrush(ColTextMuted);

        for (int i = 0; i <= 4; i++)
        {
            int y = plot.Bottom - (plot.Height * i / 4);
            g.DrawLine(gridPen, plot.X, y, plot.Right, y);
            string lbl = (maxVal * i / 4).ToString();
            var sz = g.MeasureString(lbl, fontAxis);
            g.DrawString(lbl, fontAxis, brushMuted, plot.X - sz.Width - 6, y - sz.Height / 2);
        }

        float groupWidth = plot.Width / (float)n;
        int barW = Math.Clamp((int)(groupWidth * 0.30F), 6, 22);

        for (int i = 0; i < n; i++)
        {
            float centerX = plot.X + groupWidth * i + groupWidth / 2F;
            bool isHover = i == _hoverBarIdx;

            if (isHover)
            {
                using var hoverBg = new SolidBrush(Color.FromArgb(25, 255, 255, 255));
                g.FillRectangle(hoverBg, plot.X + groupWidth * i + 2, plot.Y, groupWidth - 4, plot.Height);
            }

            int hSold = Math.Max(2, (int)((_data.SoldSeries[i] / (float)maxVal) * plot.Height * p));
            var rSold = new Rectangle((int)(centerX - barW - 2), plot.Bottom - hSold, barW, hSold);
            using (var bSold = new LinearGradientBrush(rSold, ColEmerald, ColEmerald, 90F))
            {
                g.FillRectangle(bSold, rSold);
            }

            int hCan = Math.Max(2, (int)((_data.CanceledSeries[i] / (float)maxVal) * plot.Height * p));
            var rCan = new Rectangle((int)(centerX + 2), plot.Bottom - hCan, barW, hCan);
            using (var bCan = new LinearGradientBrush(rCan, ColRose, ColRose, 90F))
            {
                g.FillRectangle(bCan, rCan);
            }

            if (n <= 8)
            {
                string sTxt = _data.SoldSeries[i].ToString();
                var szS = g.MeasureString(sTxt, fontVal);
                g.DrawString(sTxt, fontVal, brushMuted, rSold.X + (barW - szS.Width) / 2, rSold.Y - 14);
            }

            string xLbl = _data.TimeLabels[i];
            var szX = g.MeasureString(xLbl, fontAxis);
            g.DrawString(xLbl, fontAxis, brushMuted, centerX - szX.Width / 2, plot.Bottom + 8);
        }

        if (_hoverBarIdx >= 0 && _hoverBarIdx < n)
        {
            int cx = (int)(plot.X + groupWidth * _hoverBarIdx + groupWidth / 2F);
            DrawFloatingTooltip(g, pnlChartSoldCancel.ClientRectangle, new Point(cx, plot.Y + 28),
                $"Ngày {_data.TimeLabels[_hoverBarIdx]}",
                $"Vé đã bán: {_data.SoldSeries[_hoverBarIdx]:N0} vé",
                $"Vé đã hủy: {_data.CanceledSeries[_hoverBarIdx]:N0} vé",
                ColEmerald);
        }
    }

    private void OnSoldCancelMouseMove(object? sender, MouseEventArgs e)
    {
        int n = _data.SoldSeries.Count;
        if (n == 0) return;
        int plotLeft = 54;
        int plotW = pnlChartSoldCancel.Width - 80;
        if (e.X < plotLeft || e.X > plotLeft + plotW)
        {
            if (_hoverBarIdx != -1) { _hoverBarIdx = -1; pnlChartSoldCancel.Invalidate(); }
            return;
        }
        int idx = Math.Clamp((int)((e.X - plotLeft) / (plotW / (float)n)), 0, n - 1);
        if (idx != _hoverBarIdx) { _hoverBarIdx = idx; pnlChartSoldCancel.Invalidate(); }
    }

    // =========================================================
    // 11. BIỂU ĐỒ 6: PHÂN BỐ PHƯƠNG THỨC THANH TOÁN (DOUGHNUT)
    // =========================================================
    private void PaintPaymentMethodsChart(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var cardRect = DrawChartCardFrame(g, pnlChartPayment,
            "PHƯƠNG THỨC THANH TOÁN",
            "Tỷ trọng doanh thu & giao dịch theo kênh");

        if (_data.PaymentMethods.Count == 0) return;

        decimal sum = Math.Max(1m, _data.PaymentMethods.Sum(x => x.Amount));
        int totalTx = _data.PaymentMethods.Sum(x => x.TxCount);
        float p = EaseOutCubic(_animProgress);

        int dia = Math.Min(cardRect.Width / 2 - 20, cardRect.Height - 95);
        dia = Math.Clamp(dia, 110, 175);
        var pieRect = new Rectangle(cardRect.X + 24, cardRect.Y + (cardRect.Height - dia) / 2 + 12, dia, dia);

        float startAngle = -90F;
        for (int i = 0; i < _data.PaymentMethods.Count; i++)
        {
            var item = _data.PaymentMethods[i];
            float sweep = (float)(item.Amount / sum) * 360F * p;
            bool isHover = i == _hoverPayIdx;

            var drawRect = isHover
                ? new Rectangle(pieRect.X - 4, pieRect.Y - 4, pieRect.Width + 8, pieRect.Height + 8)
                : pieRect;

            using var pen = new Pen(item.Accent, isHover ? 26F : 22F);
            if (sweep > 1F)
                g.DrawArc(pen, drawRect, startAngle, Math.Max(1F, sweep - 2F));

            startAngle += sweep;
        }

        using var fontTx = new Font("Segoe UI", 13F, FontStyle.Bold);
        using var fontTxSub = new Font("Segoe UI", 7.8F);
        using var brushWhite = new SolidBrush(ColTextPrimary);
        using var brushMuted = new SolidBrush(ColTextMuted);

        string txNum = totalTx.ToString("N0", ViCulture);
        var szNum = g.MeasureString(txNum, fontTx);
        g.DrawString(txNum, fontTx, brushWhite,
            pieRect.X + (pieRect.Width - szNum.Width) / 2,
            pieRect.Y + (pieRect.Height - szNum.Height) / 2 - 7);

        string txLbl = "giao dịch";
        var szLbl = g.MeasureString(txLbl, fontTxSub);
        g.DrawString(txLbl, fontTxSub, brushMuted,
            pieRect.X + (pieRect.Width - szLbl.Width) / 2,
            pieRect.Y + (pieRect.Height / 2) + 8);

        int legendX = pieRect.Right + 26;
        int legendY = pieRect.Y + 6;
        using var fontItem = new Font("Segoe UI", 8.8F, FontStyle.Bold);
        using var fontPct = new Font("Segoe UI", 8.5F);

        for (int i = 0; i < _data.PaymentMethods.Count; i++)
        {
            var item = _data.PaymentMethods[i];
            double share = (double)(item.Amount * 100m / sum);
            int rowY = legendY + i * 42;

            using var dotBrush = new SolidBrush(item.Accent);
            g.FillEllipse(dotBrush, legendX, rowY + 4, 10, 10);

            g.DrawString(item.Name, fontItem, brushWhite, legendX + 16, rowY);
            g.DrawString($"{share:0.0}%  •  {item.TxCount} GD", fontPct, brushMuted, legendX + 16, rowY + 17);
        }
    }

    private void OnPaymentMouseMove(object? sender, MouseEventArgs e)
    {
        int idx = (e.Y - 75) / 42;
        if (e.X > 180 && idx >= 0 && idx < _data.PaymentMethods.Count)
        {
            if (_hoverPayIdx != idx) { _hoverPayIdx = idx; pnlChartPayment.Invalidate(); }
        }
        else if (_hoverPayIdx != -1)
        {
            _hoverPayIdx = -1;
            pnlChartPayment.Invalidate();
        }
    }

    // =========================================================
    // 12. BIỂU ĐỒ 3: TOP PHIM DOANH THU CAO (LEADERBOARD + BARS)
    // =========================================================
    private void PaintTopMoviesLeaderboard(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var cardRect = DrawChartCardFrame(g, pnlChartTopMovies,
            "TOP PHIM DOANH THU CAO NHẤT",
            "Xếp hạng hiệu suất bán vé & tỷ lệ đóng góp doanh thu");

        int count = Math.Min(5, _data.TopMovies.Count);
        if (count == 0) return;

        float p = EaseOutCubic(_animProgress);
        int startY = cardRect.Y + 56;
        int rowHeight = (cardRect.Height - 68) / count;

        using var fontRank = new Font("Segoe UI", 8F, FontStyle.Bold);
        using var fontMovie = new Font("Segoe UI", 9.8F, FontStyle.Bold);
        using var fontSub = new Font("Segoe UI", 8.2F);
        using var brushWhite = new SolidBrush(ColTextPrimary);
        using var brushMuted = new SolidBrush(ColTextMuted);

        for (int i = 0; i < count; i++)
        {
            var m = _data.TopMovies[i];
            int y = startY + i * rowHeight;

            var badgeRect = new Rectangle(cardRect.X + 16, y + 6, 52, 24);
            using (var badgePath = CreateRoundedPath(badgeRect, 8))
            using (var badgeBrush = new SolidBrush(Color.FromArgb(45, m.BadgeColor)))
            using (var badgePen = new Pen(m.BadgeColor, 1.2F))
            {
                g.FillPath(badgeBrush, badgePath);
                g.DrawPath(badgePen, badgePath);
            }
            string rankTxt = $"TOP {i + 1}";
            var szRank = g.MeasureString(rankTxt, fontRank);
            using var brushBadgeTxt = new SolidBrush(m.BadgeColor);
            g.DrawString(rankTxt, fontRank, brushBadgeTxt,
                badgeRect.X + (badgeRect.Width - szRank.Width) / 2,
                badgeRect.Y + (badgeRect.Height - szRank.Height) / 2);

            var posterRect = new Rectangle(badgeRect.Right + 10, y + 2, 28, 36);
            using (var posterBrush = new LinearGradientBrush(posterRect, m.BadgeColor, ColCardElevated, 45F))
            {
                g.FillRectangle(posterBrush, posterRect);
            }
            g.DrawString("🎬", fontSub, brushWhite, posterRect.X + 5, posterRect.Y + 10);

            int textX = posterRect.Right + 10;
            g.DrawString(m.Title, fontMovie, brushWhite, textX, y + 2);
            g.DrawString($"{m.Genre}  •  {m.Tickets:N0} vé", fontSub, brushMuted, textX, y + 20);

            string revStr = FormatCompactVnd(m.Revenue * (decimal)p);
            var szRev = g.MeasureString(revStr, fontMovie);
            g.DrawString(revStr, fontMovie, brushWhite, cardRect.Right - szRev.Width - 18, y + 2);

            string shareStr = $"{(m.SharePct * p):0.0}%";
            var szShare = g.MeasureString(shareStr, fontSub);
            g.DrawString(shareStr, fontSub, brushBadgeTxt, cardRect.Right - szShare.Width - 18, y + 20);

            int barX = textX;
            int barW = Math.Max(80, cardRect.Right - textX - 115);
            var trackRect = new Rectangle(barX, y + 38, barW, 5);
            using (var trackBrush = new SolidBrush(Color.FromArgb(38, 48, 74)))
            {
                g.FillRectangle(trackBrush, trackRect);
            }

            int fillW = Math.Max(4, (int)(barW * Math.Min(1.0, m.SharePct / 55.0) * p));
            var fillRect = new Rectangle(barX, y + 38, fillW, 5);
            using (var fillBrush = new LinearGradientBrush(fillRect, m.BadgeColor, ColNeonCyan, 0F))
            {
                g.FillRectangle(fillBrush, fillRect);
            }
        }
    }

    // =========================================================
    // 13. BIỂU ĐỒ 5: DOANH THU THEO PHÒNG CHIẾU (BAR CHART)
    // =========================================================
    private void PaintRoomRevenueChart(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var cardRect = DrawChartCardFrame(g, pnlChartRooms,
            "DOANH THU THEO PHÒNG CHIẾU",
            "So sánh hiệu suất kinh doanh giữa các phòng chiếu");

        var plot = new Rectangle(cardRect.X + 58, cardRect.Y + 56, cardRect.Width - 78, cardRect.Height - 92);
        int n = _data.RoomPerformances.Count;
        if (plot.Width <= 40 || plot.Height <= 40 || n == 0) return;

        decimal maxVal = Math.Max(100000m, _data.RoomPerformances.Max(r => r.Revenue) * 1.18m);
        float p = EaseOutCubic(_animProgress);

        using var gridPen = new Pen(ColCardBorder, 1F) { DashStyle = DashStyle.Dash };
        using var fontAxis = new Font("Segoe UI", 8F);
        using var fontVal = new Font("Segoe UI", 8F, FontStyle.Bold);
        using var brushMuted = new SolidBrush(ColTextMuted);
        using var brushWhite = new SolidBrush(ColTextPrimary);

        for (int i = 0; i <= 4; i++)
        {
            int y = plot.Bottom - (plot.Height * i / 4);
            g.DrawLine(gridPen, plot.X, y, plot.Right, y);
            string lbl = FormatShortNumber(maxVal * i / 4m);
            var sz = g.MeasureString(lbl, fontAxis);
            g.DrawString(lbl, fontAxis, brushMuted, plot.X - sz.Width - 8, y - sz.Height / 2);
        }

        float slotW = plot.Width / (float)n;
        int barW = Math.Clamp((int)(slotW * 0.45F), 20, 48);

        for (int i = 0; i < n; i++)
        {
            var rm = _data.RoomPerformances[i];
            float centerX = plot.X + slotW * i + slotW / 2F;
            int h = Math.Max(4, (int)((float)(rm.Revenue / maxVal) * plot.Height * p));
            var barRect = new Rectangle((int)(centerX - barW / 2F), plot.Bottom - h, barW, h);

            bool isHover = i == _hoverRoomIdx;
            Color cTop = isHover ? ColNeonCyan : ColElectricBlue;
            Color cBot = isHover ? ColElectricBlue : ColElectricBlue;

            using (var barPath = CreateRoundedPath(barRect, 6))
            using (var barBrush = new LinearGradientBrush(barRect, cTop, cBot, 90F))
            {
                g.FillPath(barBrush, barPath);
            }

            string valStr = FormatShortNumber(rm.Revenue * (decimal)p);
            var szV = g.MeasureString(valStr, fontVal);
            g.DrawString(valStr, fontVal, brushWhite, centerX - szV.Width / 2, barRect.Y - 16);

            var szN = g.MeasureString(rm.RoomName, fontAxis);
            g.DrawString(rm.RoomName, fontAxis, brushMuted, centerX - szN.Width / 2, plot.Bottom + 8);
        }

        if (_hoverRoomIdx >= 0 && _hoverRoomIdx < n)
        {
            var rm = _data.RoomPerformances[_hoverRoomIdx];
            int cx = (int)(plot.X + slotW * _hoverRoomIdx + slotW / 2F);
            DrawFloatingTooltip(g, pnlChartRooms.ClientRectangle, new Point(cx, plot.Y + 30),
                rm.RoomName,
                $"Doanh thu: {rm.Revenue:N0} đ",
                $"Số vé đã bán: {rm.Tickets:N0} vé",
                ColElectricBlue);
        }
    }

    private void OnRoomMouseMove(object? sender, MouseEventArgs e)
    {
        int n = _data.RoomPerformances.Count;
        if (n == 0) return;
        int plotLeft = 64;
        int plotW = pnlChartRooms.Width - 90;
        if (e.X < plotLeft || e.X > plotLeft + plotW)
        {
            if (_hoverRoomIdx != -1) { _hoverRoomIdx = -1; pnlChartRooms.Invalidate(); }
            return;
        }
        int idx = Math.Clamp((int)((e.X - plotLeft) / (plotW / (float)n)), 0, n - 1);
        if (idx != _hoverRoomIdx) { _hoverRoomIdx = idx; pnlChartRooms.Invalidate(); }
    }

    // =========================================================
    // 14. BIỂU ĐỒ 7: HIỆU SUẤT SUẤT CHIẾU THEO KHUNG GIỜ (COLUMN CHART)
    // =========================================================
    private void PaintTimeSlotsChart(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var cardRect = DrawChartCardFrame(g, pnlChartTimeSlots,
            "HIỆU SUẤT SUẤT CHIẾU THEO KHUNG GIỜ",
            "Phân tích lượng vé bán & khung giờ vàng doanh thu cao");

        int n = _data.TimeSlotPerfs.Count;
        var plot = new Rectangle(cardRect.X + 48, cardRect.Y + 56, cardRect.Width - 68, cardRect.Height - 92);
        if (plot.Width <= 40 || plot.Height <= 40 || n == 0) return;
        if (_data.TimeSlotPerfs.All(slot => slot.Tickets == 0)) return;

        int maxTickets = Math.Max(10, (int)(_data.TimeSlotPerfs.Max(s => s.Tickets) * 1.2));
        int peakIdx = 0;
        for (int i = 1; i < n; i++)
            if (_data.TimeSlotPerfs[i].Revenue > _data.TimeSlotPerfs[peakIdx].Revenue) peakIdx = i;

        float p = EaseOutCubic(_animProgress);

        using var gridPen = new Pen(ColCardBorder, 1F) { DashStyle = DashStyle.Dash };
        using var fontAxis = new Font("Segoe UI", 8.2F);
        using var fontVal = new Font("Segoe UI", 8.2F, FontStyle.Bold);
        using var brushMuted = new SolidBrush(ColTextMuted);
        using var brushWhite = new SolidBrush(ColTextPrimary);

        for (int i = 0; i <= 4; i++)
        {
            int y = plot.Bottom - (plot.Height * i / 4);
            g.DrawLine(gridPen, plot.X, y, plot.Right, y);
            string lbl = (maxTickets * i / 4).ToString();
            var sz = g.MeasureString(lbl, fontAxis);
            g.DrawString(lbl, fontAxis, brushMuted, plot.X - sz.Width - 6, y - sz.Height / 2);
        }

        float slotW = plot.Width / (float)n;
        int barW = Math.Clamp((int)(slotW * 0.42F), 24, 52);

        for (int i = 0; i < n; i++)
        {
            var slot = _data.TimeSlotPerfs[i];
            float centerX = plot.X + slotW * i + slotW / 2F;
            int h = Math.Max(4, (int)((slot.Tickets / (float)maxTickets) * plot.Height * p));
            var barRect = new Rectangle((int)(centerX - barW / 2F), plot.Bottom - h, barW, h);

            bool isPeak = i == peakIdx;
            bool isHover = i == _hoverSlotIdx;

            Color topCol = isPeak ? ColNeonPurple : (isHover ? ColNeonCyan : ColElectricBlue);
            Color botCol = isPeak ? ColNeonPurple : ColElectricBlue;

            using (var path = CreateRoundedPath(barRect, 6))
            using (var brush = new LinearGradientBrush(barRect, topCol, botCol, 90F))
            {
                g.FillPath(brush, path);
            }

            string tStr = isPeak ? $"★ {slot.Tickets}" : slot.Tickets.ToString();
            var szT = g.MeasureString(tStr, fontVal);
            using var txtBrush = new SolidBrush(isPeak ? ColNeonCyan : ColTextPrimary);
            g.DrawString(tStr, fontVal, txtBrush, centerX - szT.Width / 2, barRect.Y - 16);

            var szName = g.MeasureString(slot.SlotName, fontAxis);
            g.DrawString(slot.SlotName, fontAxis, brushMuted, centerX - szName.Width / 2, plot.Bottom + 8);
        }

        if (_hoverSlotIdx >= 0 && _hoverSlotIdx < n)
        {
            var slot = _data.TimeSlotPerfs[_hoverSlotIdx];
            int cx = (int)(plot.X + slotW * _hoverSlotIdx + slotW / 2F);
            DrawFloatingTooltip(g, pnlChartTimeSlots.ClientRectangle, new Point(cx, plot.Y + 28),
                $"Khung giờ: {slot.SlotName}",
                $"Số vé bán: {slot.Tickets:N0} vé",
                $"Doanh thu: {slot.Revenue:N0} đ",
                ColNeonPurple);
        }
    }

    private void OnSlotMouseMove(object? sender, MouseEventArgs e)
    {
        int n = _data.TimeSlotPerfs.Count;
        if (n == 0) return;
        int plotLeft = 54;
        int plotW = pnlChartTimeSlots.Width - 80;
        if (e.X < plotLeft || e.X > plotLeft + plotW)
        {
            if (_hoverSlotIdx != -1) { _hoverSlotIdx = -1; pnlChartTimeSlots.Invalidate(); }
            return;
        }
        int idx = Math.Clamp((int)((e.X - plotLeft) / (plotW / (float)n)), 0, n - 1);
        if (idx != _hoverSlotIdx) { _hoverSlotIdx = idx; pnlChartTimeSlots.Invalidate(); }
    }

    // =========================================================
    // 15. HOẠT ĐỘNG GẦN ĐÂY (RECENT ACTIVITIES FEED)
    // =========================================================
    private void PaintRecentActivitiesCard(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var cardRect = DrawChartCardFrame(g, pnlRecentActivities,
            "HOẠT ĐỘNG HỆ THỐNG GẦN ĐÂY",
            "Nhật ký giao dịch vé, hủy vé & suất chiếu mới nhất");

        int count = Math.Min(5, _data.RecentActivities.Count);
        int startY = cardRect.Y + 54;
        int rowH = (cardRect.Height - 62) / Math.Max(1, count);

        using var fontIcon = new Font("Segoe UI Emoji", 11F);
        using var fontText = new Font("Segoe UI", 9F, FontStyle.Bold);
        using var fontTime = new Font("Segoe UI", 8F);
        using var fontStatus = new Font("Segoe UI", 7.8F, FontStyle.Bold);
        using var brushWhite = new SolidBrush(ColTextPrimary);
        using var brushMuted = new SolidBrush(ColTextMuted);
        using var linePen = new Pen(ColCardBorder, 1F);

        for (int i = 0; i < count; i++)
        {
            var act = _data.RecentActivities[i];
            int y = startY + i * rowH;

            if (i > 0)
                g.DrawLine(linePen, cardRect.X + 16, y - 2, cardRect.Right - 16, y - 2);

            var iconCircle = new Rectangle(cardRect.X + 16, y + 6, 30, 30);
            using (var cBrush = new SolidBrush(Color.FromArgb(35, act.StatusColor)))
            {
                g.FillEllipse(cBrush, iconCircle);
            }
            g.DrawString(act.Icon, fontIcon, brushWhite, iconCircle.X + 4, iconCircle.Y + 4);

            int contentX = iconCircle.Right + 10;
            var stRect = new Rectangle(cardRect.Right - 104, y + 9, 88, 22);
            int contentWidth = Math.Max(0, stRect.Left - contentX - 8);
            var contentRect = new Rectangle(contentX, y + 4, contentWidth, 18);
            var timeRect = new Rectangle(contentX, y + 22, contentWidth, 16);
            const TextFormatFlags activityTextFlags = TextFormatFlags.Left |
                                                        TextFormatFlags.Top |
                                                        TextFormatFlags.SingleLine |
                                                        TextFormatFlags.EndEllipsis |
                                                        TextFormatFlags.NoPadding |
                                                        TextFormatFlags.NoPrefix;
            TextRenderer.DrawText(g, act.Content, fontText, contentRect, ColTextPrimary, activityTextFlags);
            TextRenderer.DrawText(g, act.TimeAgo, fontTime, timeRect, ColTextMuted, activityTextFlags);

            using (var stPath = CreateRoundedPath(stRect, 8))
            using (var stBg = new SolidBrush(Color.FromArgb(38, act.StatusColor)))
            using (var stBorder = new Pen(act.StatusColor, 1F))
            {
                g.FillPath(stBg, stPath);
                g.DrawPath(stBorder, stPath);
            }
            TextRenderer.DrawText(g, act.Status, fontStatus, stRect, act.StatusColor,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPadding |
                TextFormatFlags.NoPrefix);
        }
    }

    // =========================================================
    // 16. HELPER VẼ KHUNG CARD, TOOLTIP & BO GÓC
    // =========================================================
    private Rectangle DrawChartCardFrame(Graphics g, Panel pnl, string title, string subtitle)
    {
        var rect = new Rectangle(1, 1, pnl.Width - 3, pnl.Height - 3);
        using var path = CreateRoundedPath(rect, 16);
        using var bgBrush = new SolidBrush(ColCardBg);
        g.FillPath(bgBrush, path);

        using var borderPen = new Pen(ColCardBorder, 1.1F);
        g.DrawPath(borderPen, path);

        using var fontTitle = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        using var fontSub = new Font("Segoe UI", 8.3F);
        using var brushWhite = new SolidBrush(ColTextPrimary);
        using var brushMuted = new SolidBrush(ColTextMuted);

        g.DrawString(title, fontTitle, brushWhite, rect.X + 16, rect.Y + 12);
        g.DrawString(subtitle, fontSub, brushMuted, rect.X + 16, rect.Y + 32);

        return rect;
    }

    private static void DrawLegendDot(Graphics g, int x, int y, Color color, string label)
    {
        using var b = new SolidBrush(color);
        g.FillEllipse(b, x, y + 3, 9, 9);
        using var font = new Font("Segoe UI", 8.2F, FontStyle.Bold);
        using var txtBrush = new SolidBrush(Color.FromArgb(203, 213, 225));
        g.DrawString(label, font, txtBrush, x + 13, y);
    }

    private void DrawFloatingTooltip(Graphics g, Rectangle bounds, Point anchor,
        string header, string line1, string line2, Color accent)
    {
        int boxW = 195;
        int boxH = 66;
        int x = Math.Clamp(anchor.X - boxW / 2, bounds.X + 8, bounds.Right - boxW - 8);
        int y = Math.Clamp(anchor.Y - boxH, bounds.Y + 46, bounds.Bottom - boxH - 8);

        var tipRect = new Rectangle(x, y, boxW, boxH);
        using var path = CreateRoundedPath(tipRect, 10);
        using var bg = new SolidBrush(ColCardElevated);
        using var border = new Pen(accent, 1.3F);
        g.FillPath(bg, path);
        g.DrawPath(border, path);

        using var fHead = new Font("Segoe UI", 8.2F, FontStyle.Bold);
        using var fBody = new Font("Segoe UI", 8F);
        using var bAccent = new SolidBrush(accent);
        using var bWhite = new SolidBrush(ColTextPrimary);

        g.DrawString(header, fHead, bAccent, x + 10, y + 7);
        g.DrawString(line1, fBody, bWhite, x + 10, y + 25);
        g.DrawString(line2, fBody, bWhite, x + 10, y + 42);
    }

    private static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        float d = radius * 2F;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static string FormatCompactVnd(decimal amount)
    {
        if (amount >= 1_000_000_000m) return $"{amount / 1_000_000_000m:0.##} Tỷ đ";
        if (amount >= 1_000_000m) return $"{amount / 1_000_000m:0.##} Tr đ";
        return $"{amount:N0} đ";
    }

    private static string FormatShortNumber(decimal val)
    {
        if (val >= 1_000_000m) return $"{val / 1_000_000m:0.#}M";
        if (val >= 1_000m) return $"{val / 1_000m:0.#}K";
        return $"{val:0}";
    }

    private static float EaseOutCubic(float t) => 1F - (float)Math.Pow(1F - Math.Clamp(t, 0F, 1F), 3);

    // =========================================================
    // 17. ANIMATION CONTROLLER (60 FPS)
    // =========================================================
    private void SetupAnimation()
    {
        _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _animTimer.Tick += (s, e) =>
        {
            _animProgress += 0.045F;
            if (_animProgress >= 1F)
            {
                _animProgress = 1F;
                _animTimer.Stop();
            }

            foreach (var c in _kpiCards) c?.Invalidate();
            pnlChartRevenue?.Invalidate();
            pnlChartOccupancy?.Invalidate();
            pnlChartSoldCancel?.Invalidate();
            pnlChartPayment?.Invalidate();
            pnlChartTopMovies?.Invalidate();
            pnlChartRooms?.Invalidate();
            pnlChartTimeSlots?.Invalidate();
        };
    }

    // =========================================================
    // 18. TẢI DỮ LIỆU THẬT TỪ SQLITE (REFLECTION AN TOÀN 100%)
    // =========================================================
    private void LoadAndAnimateData()
    {
        var (fromDate, toDate) = ResolveFilterDateRange();
        int? roomId = (cboCinemaFilter?.SelectedItem as RoomFilterItem)?.Id;

        try
        {
            _data = QueryDatabaseSnapshot(fromDate, toDate, roomId);
            lblDemoBadge.Text = "LIVE DB";
            lblDemoBadge.BackColor = ColNeonCyan;
            lblLastUpdated.Text = $"Cập nhật lúc: {DateTime.Now:HH:mm:ss dd/MM/yyyy}";
            lblLastUpdated.ForeColor = ColTextMuted;
        }
        catch (Exception ex)
        {
            _data = new AnalyticsSnapshot { DatabaseError = ex.Message };
            lblDemoBadge.Text = "LỖI DB";
            lblDemoBadge.BackColor = ColRose;
            lblLastUpdated.Text = "Không thể tải dữ liệu từ SQLite";
            lblLastUpdated.ForeColor = ColRose;
            lblLastUpdated.Tag = ex.Message;
        }

        _animProgress = 1F;
        _animTimer.Start();
        pnlRecentActivities?.Invalidate();
    }

    private (DateTime from, DateTime to) ResolveFilterDateRange()
    {
        string mode = cboTimeRange?.SelectedItem?.ToString() ?? "30 ngày qua";
        DateTime today = DateTime.Today;
        var (from, to) = mode switch
        {
            "Hôm nay" => (today, today.AddDays(1).AddSeconds(-1)),
            "7 ngày qua" => (today.AddDays(-6), today.AddDays(1).AddSeconds(-1)),
            "30 ngày qua" => (today.AddDays(-29), today.AddDays(1).AddSeconds(-1)),
            "Tháng này" => (new DateTime(today.Year, today.Month, 1), today.AddDays(1).AddSeconds(-1)),
            "Năm nay" => (new DateTime(today.Year, 1, 1), today.AddDays(1).AddSeconds(-1)),
            _ => (dtpFrom.Value.Date, dtpTo.Value.Date.AddDays(1).AddSeconds(-1))
        };
        if (from.Date <= to.Date) return (from.Date, to);
        return (to.Date, from.Date.AddDays(1).AddSeconds(-1));
    }

    private AnalyticsSnapshot QueryDatabaseSnapshot(DateTime from, DateTime to, int? roomId)
    {
        using var connection = DatabaseConnection.GetConnection();
        connection.Open();

        from = from.Date;
        int periodDays = Math.Max(1, (to.Date - from).Days + 1);
        DateTime currentTo = from.AddDays(periodDays).AddSeconds(-1);
        DateTime previousFrom = from.AddDays(-periodDays);
        DateTime previousTo = from.AddSeconds(-1);
        var currentParameters = new
        {
            FromDate = from.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            ToDate = currentTo.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            RoomId = roomId
        };
        var previousParameters = new
        {
            FromDate = previousFrom.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            ToDate = previousTo.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            RoomId = roomId
        };

        const string revenueSql = @"
            SELECT date(b.BookingTime) AS Day,
                   COALESCE(SUM(b.FinalAmount), 0) AS Revenue
            FROM Bookings b
            WHERE datetime(b.BookingTime) >= datetime(@FromDate)
              AND datetime(b.BookingTime) <= datetime(@ToDate)
              AND UPPER(COALESCE(b.BookingStatus, '')) NOT IN ('CANCELLED', 'CANCELED', 'REFUNDED')
              AND (@RoomId IS NULL OR EXISTS (
                    SELECT 1
                    FROM Tickets t
                    INNER JOIN Showtimes s ON s.Id = t.ShowtimeId
                    WHERE t.BookingId = b.Id AND s.RoomId = @RoomId
              ))
            GROUP BY date(b.BookingTime)
            ORDER BY date(b.BookingTime);";
        const string ticketSql = @"
            SELECT date(b.BookingTime) AS Day,
                   COALESCE(SUM(CASE
                       WHEN UPPER(COALESCE(t.Status, '')) NOT IN ('CANCELLED', 'CANCELED')
                        AND UPPER(COALESCE(b.BookingStatus, '')) NOT IN ('CANCELLED', 'CANCELED', 'REFUNDED')
                       THEN 1 ELSE 0 END), 0) AS Sold,
                   COALESCE(SUM(CASE
                       WHEN UPPER(COALESCE(t.Status, '')) IN ('CANCELLED', 'CANCELED')
                         OR UPPER(COALESCE(b.BookingStatus, '')) IN ('CANCELLED', 'CANCELED', 'REFUNDED')
                       THEN 1 ELSE 0 END), 0) AS Canceled
            FROM Tickets t
            INNER JOIN Bookings b ON b.Id = t.BookingId
            INNER JOIN Showtimes s ON s.Id = t.ShowtimeId
            WHERE datetime(b.BookingTime) >= datetime(@FromDate)
              AND datetime(b.BookingTime) <= datetime(@ToDate)
              AND (@RoomId IS NULL OR s.RoomId = @RoomId)
            GROUP BY date(b.BookingTime)
            ORDER BY date(b.BookingTime);";

        var currentRevenueRows = connection.Query<DailyRevenueRow>(revenueSql, currentParameters).ToList();
        var previousRevenueRows = connection.Query<DailyRevenueRow>(revenueSql, previousParameters).ToList();
        var currentTicketRows = connection.Query<DailyTicketRow>(ticketSql, currentParameters).ToList();
        var previousTicketRows = connection.Query<DailyTicketRow>(ticketSql, previousParameters).ToList();

        var currentBuckets = CreatePeriodBuckets(from, periodDays);
        var previousBuckets = CreatePeriodBuckets(previousFrom, periodDays);
        AddRevenueRowsToBuckets(currentRevenueRows, currentBuckets, from, periodDays);
        AddRevenueRowsToBuckets(previousRevenueRows, previousBuckets, previousFrom, periodDays);
        AddTicketRowsToBuckets(currentTicketRows, currentBuckets, from, periodDays);
        AddTicketRowsToBuckets(previousTicketRows, previousBuckets, previousFrom, periodDays);

        var snapshot = new AnalyticsSnapshot
        {
            TotalRevenue = currentRevenueRows.Sum(row => row.Revenue),
            RevenueGrowthPct = CalculateGrowth(SumRevenueRows(currentRevenueRows), SumRevenueRows(previousRevenueRows)),
            TicketsSold = currentTicketRows.Sum(row => row.Sold),
            TicketGrowthPct = CalculateGrowth(
                currentTicketRows.Sum(row => row.Sold), previousTicketRows.Sum(row => row.Sold)),
            TicketsCanceled = currentTicketRows.Sum(row => row.Canceled),
            TimeLabels = currentBuckets.Select(bucket => bucket.Label).ToList(),
            CurrentPeriodRevenue = currentBuckets.Select(bucket => bucket.Revenue).ToList(),
            PreviousPeriodRevenue = previousBuckets.Select(bucket => bucket.Revenue).ToList(),
            SoldSeries = currentBuckets.Select(bucket => bucket.Sold).ToList(),
            CanceledSeries = currentBuckets.Select(bucket => bucket.Canceled).ToList()
        };
        snapshot.SeatsSold = snapshot.TicketsSold;
        snapshot.RevenueSpark = CreateSparkline(snapshot.CurrentPeriodRevenue);
        snapshot.TicketSpark = CreateSparkline(snapshot.SoldSeries.Select(value => (decimal)value).ToList());

        snapshot.TotalShowtimes = connection.ExecuteScalar<int>(@"
            SELECT COUNT(*) FROM Showtimes s
            WHERE datetime(s.StartTime) >= datetime(@FromDate)
              AND datetime(s.StartTime) <= datetime(@ToDate)
              AND (@RoomId IS NULL OR s.RoomId = @RoomId);", currentParameters);
        snapshot.ActiveShowtimes = connection.ExecuteScalar<int>(@"
            SELECT COUNT(*) FROM Showtimes s
            WHERE datetime(s.StartTime) >= datetime('now', 'localtime')
              AND datetime(s.StartTime) >= datetime(@FromDate)
              AND datetime(s.StartTime) <= datetime(@ToDate)
              AND (@RoomId IS NULL OR s.RoomId = @RoomId);", currentParameters);
        snapshot.TotalSeatCapacity = connection.ExecuteScalar<int>(@"
            SELECT COUNT(*)
            FROM Showtimes s
            INNER JOIN Seats seat ON seat.RoomId = s.RoomId
            WHERE datetime(s.StartTime) >= datetime(@FromDate)
              AND datetime(s.StartTime) <= datetime(@ToDate)
              AND (@RoomId IS NULL OR s.RoomId = @RoomId);", currentParameters);
        snapshot.TotalCustomers = connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM Users WHERE Role = 2;");
        snapshot.NewCustomers = connection.ExecuteScalar<int>(@"
            SELECT COUNT(*) FROM Users
            WHERE Role = 2
              AND datetime(CreatedAt) >= datetime(@FromDate)
              AND datetime(CreatedAt) <= datetime(@ToDate);", currentParameters);

        const string ticketRevenueFilter = @"
            datetime(b.BookingTime) >= datetime(@FromDate)
            AND datetime(b.BookingTime) <= datetime(@ToDate)
            AND UPPER(COALESCE(b.BookingStatus, '')) NOT IN ('CANCELLED', 'CANCELED', 'REFUNDED')
            AND UPPER(COALESCE(t.Status, '')) NOT IN ('CANCELLED', 'CANCELED')
            AND (@RoomId IS NULL OR s.RoomId = @RoomId)";
        var movieRows = connection.Query<MovieSummaryRow>($@"
            SELECT m.Title, COALESCE(m.Genre, '') AS Genre,
                   COUNT(t.Id) AS Tickets, COALESCE(SUM(t.Price), 0) AS Revenue
            FROM Tickets t
            INNER JOIN Bookings b ON b.Id = t.BookingId
            INNER JOIN Showtimes s ON s.Id = t.ShowtimeId
            INNER JOIN Movies m ON m.Id = s.MovieId
            WHERE {ticketRevenueFilter}
            GROUP BY m.Id, m.Title, m.Genre
            ORDER BY Revenue DESC
            LIMIT 5;", currentParameters).ToList();
        decimal movieRevenueTotal = connection.ExecuteScalar<decimal>($@"
            SELECT COALESCE(SUM(t.Price), 0)
            FROM Tickets t
            INNER JOIN Bookings b ON b.Id = t.BookingId
            INNER JOIN Showtimes s ON s.Id = t.ShowtimeId
            WHERE {ticketRevenueFilter};", currentParameters);
        Color[] movieColors = { ColNeonCyan, ColNeonPurple, ColEmerald, ColElectricBlue, ColAmber };
        snapshot.TopMovies = movieRows.Select((row, index) => new MovieRankItem
        {
            Title = row.Title,
            Genre = row.Genre,
            Tickets = row.Tickets,
            Revenue = row.Revenue,
            SharePct = movieRevenueTotal > 0 ? (double)(row.Revenue * 100m / movieRevenueTotal) : 0,
            BadgeColor = movieColors[index % movieColors.Length]
        }).ToList();

        snapshot.RoomPerformances = connection.Query<RoomSummaryRow>($@"
            SELECT r.Name, COUNT(t.Id) AS Tickets, COALESCE(SUM(t.Price), 0) AS Revenue
            FROM Tickets t
            INNER JOIN Bookings b ON b.Id = t.BookingId
            INNER JOIN Showtimes s ON s.Id = t.ShowtimeId
            INNER JOIN Rooms r ON r.Id = s.RoomId
            WHERE {ticketRevenueFilter}
            GROUP BY r.Id, r.Name
            ORDER BY Revenue DESC
            LIMIT 6;", currentParameters)
            .Select(row => new RoomPerfItem
            {
                RoomName = row.Name,
                Tickets = row.Tickets,
                Revenue = row.Revenue
            }).ToList();

        Color[] paymentColors = { ColNeonPurple, ColNeonCyan, ColElectricBlue, ColEmerald, ColAmber };
        snapshot.PaymentMethods = connection.Query<PaymentSummaryRow>(@"
            SELECT COALESCE(NULLIF(TRIM(b.PaymentMethod), ''), 'Chưa xác định') AS Name,
                   COALESCE(SUM(b.FinalAmount), 0) AS Amount,
                   COUNT(*) AS Transactions
            FROM Bookings b
            WHERE datetime(b.BookingTime) >= datetime(@FromDate)
              AND datetime(b.BookingTime) <= datetime(@ToDate)
              AND UPPER(COALESCE(b.BookingStatus, '')) NOT IN ('CANCELLED', 'CANCELED', 'REFUNDED')
              AND (@RoomId IS NULL OR EXISTS (
                    SELECT 1 FROM Tickets t
                    INNER JOIN Showtimes s ON s.Id = t.ShowtimeId
                    WHERE t.BookingId = b.Id AND s.RoomId = @RoomId
              ))
            GROUP BY COALESCE(NULLIF(TRIM(b.PaymentMethod), ''), 'Chưa xác định')
            ORDER BY Amount DESC
            LIMIT 5;", currentParameters)
            .Select((row, index) => (row.Name, row.Amount, row.Transactions, paymentColors[index % paymentColors.Length]))
            .ToList();

        var timeSlotRows = connection.Query<TimeSlotSummaryRow>($@"
            SELECT CASE
                       WHEN CAST(strftime('%H', s.StartTime) AS INTEGER) < 12 THEN 0
                       WHEN CAST(strftime('%H', s.StartTime) AS INTEGER) < 16 THEN 1
                       WHEN CAST(strftime('%H', s.StartTime) AS INTEGER) < 19 THEN 2
                       ELSE 3
                   END AS Slot,
                   COUNT(t.Id) AS Tickets, COALESCE(SUM(t.Price), 0) AS Revenue
            FROM Tickets t
            INNER JOIN Bookings b ON b.Id = t.BookingId
            INNER JOIN Showtimes s ON s.Id = t.ShowtimeId
            WHERE {ticketRevenueFilter}
            GROUP BY Slot
            ORDER BY Slot;", currentParameters).ToDictionary(row => row.Slot);
        string[] slotNames = { "Sáng (08h-12h)", "Trưa (12h-16h)", "Chiều (16h-19h)", "Tối (19h-23h30)" };
        snapshot.TimeSlotPerfs = Enumerable.Range(0, slotNames.Length)
            .Select(slot => timeSlotRows.TryGetValue(slot, out var row)
                ? (slotNames[slot], row.Tickets, row.Revenue)
                : (slotNames[slot], 0, 0m))
            .ToList();

        snapshot.RecentActivities = connection.Query<RecentBookingRow>(@"
            SELECT b.BookingCode, b.BookingTime,
                   COALESCE(b.FinalAmount, 0) AS Amount,
                   COALESCE(b.PaymentMethod, '') AS PaymentMethod,
                   COALESCE(b.BookingStatus, '') AS BookingStatus,
                   COUNT(t.Id) AS TicketCount,
                   COALESCE(GROUP_CONCAT(DISTINCT m.Title), 'Đơn đặt vé') AS Movies
            FROM Bookings b
            LEFT JOIN Tickets t ON t.BookingId = b.Id
            LEFT JOIN Showtimes s ON s.Id = t.ShowtimeId
            LEFT JOIN Movies m ON m.Id = s.MovieId
            WHERE datetime(b.BookingTime) >= datetime(@FromDate)
              AND datetime(b.BookingTime) <= datetime(@ToDate)
              AND (@RoomId IS NULL OR EXISTS (
                    SELECT 1 FROM Tickets roomTicket
                    INNER JOIN Showtimes roomShowtime ON roomShowtime.Id = roomTicket.ShowtimeId
                    WHERE roomTicket.BookingId = b.Id AND roomShowtime.RoomId = @RoomId
              ))
            GROUP BY b.Id
            ORDER BY datetime(b.BookingTime) DESC
            LIMIT 5;", currentParameters)
            .Select(row =>
            {
                bool canceled = IsCanceled(row.BookingStatus);
                DateTime bookingTime = DateTime.TryParse(row.BookingTime, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal, out var parsedTime) ? parsedTime : DateTime.Now;
                return new ActivityItem
                {
                    Icon = canceled ? "⚠" : "🎟",
                    Content = $"{row.BookingCode} • {row.Movies} ({row.TicketCount} vé)",
                    TimeAgo = $"{GetTimeAgo(bookingTime)} • {row.PaymentMethod} • {row.Amount:N0} đ",
                    Status = canceled ? "Đã hủy" : row.BookingStatus,
                    StatusColor = canceled ? ColRose : ColEmerald
                };
            }).ToList();

        return snapshot;
    }

    private static decimal SumRevenueRows(IEnumerable<DailyRevenueRow> rows) => rows.Sum(row => row.Revenue);

    private static double CalculateGrowth(decimal current, decimal previous)
    {
        if (previous == 0) return current > 0 ? 100 : 0;
        return Math.Round((double)((current - previous) * 100m / previous), 1);
    }

    private static double CalculateGrowth(int current, int previous)
    {
        if (previous == 0) return current > 0 ? 100 : 0;
        return Math.Round((current - previous) * 100.0 / previous, 1);
    }

    private static List<PeriodBucket> CreatePeriodBuckets(DateTime start, int days)
    {
        int count = Math.Min(7, days);
        return Enumerable.Range(0, count).Select(index =>
        {
            int startOffset = index * days / count;
            int endOffset = ((index + 1) * days / count) - 1;
            DateTime bucketStart = start.Date.AddDays(startOffset);
            DateTime bucketEnd = start.Date.AddDays(endOffset);
            return new PeriodBucket
            {
                Start = bucketStart,
                End = bucketEnd,
                Label = bucketStart == bucketEnd
                    ? bucketStart.ToString("dd/MM", CultureInfo.InvariantCulture)
                    : $"{bucketStart:dd/MM}-{bucketEnd:dd/MM}"
            };
        }).ToList();
    }

    private static int GetBucketIndex(DateTime date, DateTime start, int days, int count)
    {
        int dayOffset = Math.Clamp((date.Date - start.Date).Days, 0, days - 1);
        return Math.Min(count - 1, dayOffset * count / days);
    }

    private static void AddRevenueRowsToBuckets(IEnumerable<DailyRevenueRow> rows,
        IList<PeriodBucket> buckets, DateTime start, int days)
    {
        foreach (var row in rows)
        {
            if (!DateTime.TryParseExact(row.Day, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date)) continue;
            buckets[GetBucketIndex(date, start, days, buckets.Count)].Revenue += row.Revenue;
        }
    }

    private static void AddTicketRowsToBuckets(IEnumerable<DailyTicketRow> rows,
        IList<PeriodBucket> buckets, DateTime start, int days)
    {
        foreach (var row in rows)
        {
            if (!DateTime.TryParseExact(row.Day, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date)) continue;
            var bucket = buckets[GetBucketIndex(date, start, days, buckets.Count)];
            bucket.Sold += row.Sold;
            bucket.Canceled += row.Canceled;
        }
    }

    private static float[] CreateSparkline(IReadOnlyCollection<decimal> values)
    {
        decimal max = values.Count == 0 ? 0 : values.Max();
        if (values.Count < 2 || max <= 0) return Array.Empty<float>();
        return values.Select(value => (float)(value * 100m / max)).ToArray();
    }

    private static bool IsCanceled(string? status) =>
        string.Equals(status, "CANCELLED", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "CANCELED", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "REFUNDED", StringComparison.OrdinalIgnoreCase);

    private static string GetTimeAgo(DateTime time)
    {
        TimeSpan elapsed = DateTime.Now - time;
        if (elapsed.TotalMinutes < 1) return "Vừa xong";
        if (elapsed.TotalHours < 1) return $"{(int)elapsed.TotalMinutes} phút trước";
        if (elapsed.TotalDays < 1) return $"{(int)elapsed.TotalHours} giờ trước";
        return $"{(int)elapsed.TotalDays} ngày trước";
    }

}