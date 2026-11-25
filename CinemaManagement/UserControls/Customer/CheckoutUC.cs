namespace CinemaManagement.UserControls.Customer;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Models;
using CinemaManagement.Services;

public partial class CheckoutUC : UserControl
{
    // =========================================================
    // 1. THEME & PAYMENT ENUM
    // =========================================================

    private enum PaymentMethodType
    {
        Card,
        BankQr,
        MoMo,
        Counter
    }

    private readonly Color ColorBg = Color.FromArgb(14, 15, 22);
    private readonly Color ColorCard = Color.FromArgb(24, 25, 36);
    private readonly Color ColorGold = Color.FromArgb(245, 197, 24);
    private readonly Color ColorGoldHover = Color.FromArgb(255, 214, 65);
    private readonly Color ColorMoMo = Color.FromArgb(165, 0, 100);
    private readonly Color ColorMoMoHover = Color.FromArgb(195, 15, 122);
    private readonly Color ColorTextMuted = Color.FromArgb(150, 150, 170);
    private readonly Color ColorInputBg = Color.FromArgb(34, 35, 48);

    private readonly CultureInfo ViCulture = new("vi-VN");
    private PaymentMethodType _selectedPaymentMethod = PaymentMethodType.Card;

    // =========================================================
    // 2. BOOKING DATA & SERVICES
    // =========================================================

    private BookingContext? _bookingContext;
    private readonly PromotionService _promotionService = new();
    private Promotion? _appliedPromotion;

    public Movie? MovieData { get; private set; }
    public Showtime? ShowtimeData { get; private set; }

    public List<string> SelectedSeats { get; private set; } = new();

    public Dictionary<int, CartItem> FoodCart { get; private set; } = new();

    private decimal ticketSubTotal;
    private decimal foodSubTotal;
    private decimal discountAmount;
    private decimal totalAmount;

    // =========================================================
    // 3. UI CONTROLS & ANIMATION
    // =========================================================

    private TableLayoutPanel tlpHeader = null!;
    private Button btnBack = null!;
    private FlowLayoutPanel flpStepper = null!;

    private TableLayoutPanel tlpMainGrid = null!;
    private Panel pnlLeftSummary = null!;
    private Panel pnlCenterPayment = null!;
    private Panel pnlRightTicketPreview = null!;

    // Summary
    private TextBox txtCoupon = null!;
    private Button btnApplyCoupon = null!;
    private Label lblCouponMsg = null!;

    private Label lblTicketPriceInfo = null!;
    private Label lblQtyInfo = null!;
    private Label lblTicketSubTotal = null!;
    private Label lblFoodSubTotal = null!;
    private Label lblDiscountVal = null!;
    private Label lblFinalTotal = null!;

    // Payment Method Grid & Sliding Indicator Animation
    private TableLayoutPanel tlpMethodGrid = null!;
    private Button btnMethodCard = null!;
    private Button btnMethodBankQR = null!;
    private Button btnMethodMoMo = null!;
    private Button btnMethodCounter = null!;

    private Panel pnlAnimTrack = null!;
    private Panel pnlAnimIndicator = null!;
    private System.Windows.Forms.Timer _animTimer = null!;
    private int _targetIndicatorX;
    private int _targetIndicatorW;
    private Color _targetIndicatorColor;

    // Payment Dynamic Panels
    private Panel pnlCardSection = null!;
    private Panel pnlQrSection = null!;
    private Panel pnlCounterSection = null!;

    // Card Controls
    private TextBox txtCardName = null!;
    private TextBox txtCardNumber = null!;
    private TextBox txtExpiry = null!;
    private TextBox txtCVV = null!;

    // QR & MoMo Controls
    private Label lblQrHeader = null!;
    private PictureBox picQrCode = null!;
    private Label lblQrAmount = null!;
    private Label lblQrNote = null!;

    // Counter Controls
    private TextBox txtCounterPhone = null!;

    // Common Checkout Footer
    private CheckBox chkTerms = null!;
    private Button btnPayNow = null!;

    // Ticket preview
    private PictureBox picPreviewPoster = null!;
    private Label lblPreviewMovieTitle = null!;
    private Label lblPreviewDateTime = null!;
    private Label lblPreviewVenueSeats = null!;
    private Label lblPreviewPrice = null!;

    // =========================================================
    // 4. EVENTS
    // =========================================================

    public event EventHandler<BookingResultEventArgs>? OnPaymentCompleted;
    public event EventHandler? OnBackClicked;

    // =========================================================
    // 5. CONSTRUCTOR
    // =========================================================

    public CheckoutUC()
    {
        AutoScaleMode = AutoScaleMode.Dpi;

        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint |
                 ControlStyles.ResizeRedraw, true);

        InitializeComponents();
        SetupAnimationTimer();
        SelectPaymentMethod(PaymentMethodType.Card, animate: false);

        Disposed += (s, e) =>
        {
            _animTimer?.Stop();
            _animTimer?.Dispose();
        };
    }

    // =========================================================
    // 6. SET BOOKING CONTEXT
    // =========================================================

    public void SetBookingContext(BookingContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        _bookingContext = context;

        if (!context.CanCheckout)
        {
            ClearDisplayData();

            ShowWarning(
                "Thông tin đặt vé chưa đầy đủ.\n" +
                "Vui lòng chọn phim, suất chiếu và ít nhất một ghế.");

            return;
        }

        MovieData = context.Movie;
        ShowtimeData = context.Showtime;

        SelectedSeats = context.Seats.ToList();

        FoodCart = new Dictionary<int, CartItem>(context.FoodCart);

        ticketSubTotal = context.TicketAmount;
        foodSubTotal = context.FoodAmount;

        discountAmount = 0m;
        _appliedPromotion = null;

        txtCoupon.Clear();
        lblCouponMsg.Text = string.Empty;

        CalculateTotal();
        UpdateUIWithData();

        btnPayNow.Enabled = true;
    }

    private void ClearDisplayData()
    {
        MovieData = null;
        ShowtimeData = null;

        SelectedSeats.Clear();
        FoodCart.Clear();

        ticketSubTotal = 0m;
        foodSubTotal = 0m;
        discountAmount = 0m;
        totalAmount = 0m;
        _appliedPromotion = null;

        if (txtCoupon != null)
            txtCoupon.Clear();

        if (lblCouponMsg != null)
            lblCouponMsg.Text = string.Empty;

        if (btnPayNow != null)
            btnPayNow.Enabled = false;

        CalculateTotal();
        UpdateUIWithData();
    }

    // =========================================================
    // 8. INITIALIZE RESPONSIVE UI
    // =========================================================

    private void InitializeComponents()
    {
        Dock = DockStyle.Fill;
        BackColor = ColorBg;
        AutoScroll = true;

        // -----------------------------------------------------
        // HEADER RESPONSIVE (KHÔNG BAO GIỜ CẮT NÚT HOẶC STEPPER)
        // -----------------------------------------------------

        tlpHeader = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.FromArgb(18, 19, 28),
            Padding = new Padding(20, 12, 20, 12),
            Margin = new Padding(0)
        };
        tlpHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tlpHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpHeader.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        btnBack = new Button
        {
            Text = "← Quay lại",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(35, 36, 50),
            FlatStyle = FlatStyle.Flat,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(12, 6, 12, 6),
            Margin = new Padding(0, 0, 16, 0),
            Cursor = Cursors.Hand
        };
        btnBack.FlatAppearance.BorderSize = 0;
        btnBack.MouseEnter += (s, e) => btnBack.BackColor = Color.FromArgb(52, 54, 74);
        btnBack.MouseLeave += (s, e) => btnBack.BackColor = Color.FromArgb(35, 36, 50);
        btnBack.Click += (s, e) => OnBackClicked?.Invoke(this, EventArgs.Empty);

        flpStepper = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Anchor = AnchorStyles.None,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };

        Label lblStep1 = new Label
        {
            Text = "①  CHỌN SUẤT CHIẾU",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = ColorTextMuted,
            AutoSize = true,
            Margin = new Padding(4, 6, 4, 6)
        };

        Label lblDiv1 = new Label
        {
            Text = "────",
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(70, 72, 95),
            AutoSize = true,
            Margin = new Padding(4, 6, 4, 6)
        };

        Label lblStep2 = new Label
        {
            Text = "②  CHỌN GHẾ",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = ColorTextMuted,
            AutoSize = true,
            Margin = new Padding(4, 6, 4, 6)
        };

        Label lblDiv2 = new Label
        {
            Text = "────",
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(70, 72, 95),
            AutoSize = true,
            Margin = new Padding(4, 6, 4, 6)
        };

        Label lblStep3 = new Label
        {
            Text = "③  BẮP NƯỚC",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = ColorTextMuted,
            AutoSize = true,
            Margin = new Padding(4, 6, 4, 6)
        };

        Label lblDiv3 = new Label
        {
            Text = "────",
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(70, 72, 95),
            AutoSize = true,
            Margin = new Padding(4, 6, 4, 6)
        };

        Label lblStep4 = new Label
        {
            Text = "④  THANH TOÁN",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = ColorGold,
            AutoSize = true,
            Margin = new Padding(4, 6, 4, 6)
        };

        flpStepper.Controls.AddRange(new Control[]
        {
            lblStep1, lblDiv1, lblStep2, lblDiv2, lblStep3, lblDiv3, lblStep4
        });

        tlpHeader.Controls.Add(btnBack, 0, 0);
        tlpHeader.Controls.Add(flpStepper, 1, 0);

        // -----------------------------------------------------
        // MAIN 3-COLUMN RESPONSIVE GRID (29% - 42% - 29%)
        // -----------------------------------------------------

        tlpMainGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(16, 14, 16, 16),
            BackColor = ColorBg
        };
        tlpMainGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29F));
        tlpMainGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
        tlpMainGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29F));
        tlpMainGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        // -----------------------------------------------------
        // LEFT COLUMN - ORDER SUMMARY
        // -----------------------------------------------------

        pnlLeftSummary = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 8, 0),
            BackColor = ColorCard,
            Padding = new Padding(16)
        };

        Label lblSummaryHeader = new Label
        {
            Text = "Tóm Tắt Đơn Hàng",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 32
        };

        TableLayoutPanel tlpCouponRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 36,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 4, 0, 0)
        };
        tlpCouponRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68F));
        tlpCouponRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
        tlpCouponRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        txtCoupon = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10),
            BackColor = ColorInputBg,
            ForeColor = Color.White,
            Margin = new Padding(0, 2, 6, 0),
            PlaceholderText = "Mã giảm giá..."
        };

        btnApplyCoupon = new Button
        {
            Text = "Áp dụng",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            BackColor = ColorGold,
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 1, 0, 1),
            Cursor = Cursors.Hand
        };
        btnApplyCoupon.FlatAppearance.BorderSize = 0;
        btnApplyCoupon.MouseEnter += (s, e) => btnApplyCoupon.BackColor = ColorGoldHover;
        btnApplyCoupon.MouseLeave += (s, e) => btnApplyCoupon.BackColor = ColorGold;
        btnApplyCoupon.Click += BtnApplyCoupon_Click;

        tlpCouponRow.Controls.Add(txtCoupon, 0, 0);
        tlpCouponRow.Controls.Add(btnApplyCoupon, 1, 0);

        lblCouponMsg = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.LightGreen,
            Dock = DockStyle.Top,
            Height = 32,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft
        };

        Panel pnlSummaryRows = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 8, 0, 0)
        };

        int startY = 8;
        lblTicketPriceInfo = AddSummaryRow(pnlSummaryRows, "Giá vé trung bình:", "0 đ", ref startY);
        lblQtyInfo = AddSummaryRow(pnlSummaryRows, "Số lượng vé:", "0 vé", ref startY);
        lblTicketSubTotal = AddSummaryRow(pnlSummaryRows, "Tổng tiền vé:", "0 đ", ref startY);
        lblFoodSubTotal = AddSummaryRow(pnlSummaryRows, "Bắp nước & Combo:", "0 đ", ref startY);
        lblDiscountVal = AddSummaryRow(pnlSummaryRows, "Giảm giá Voucher:", "-0 đ", ref startY, Color.OrangeRed);

        Panel divider = new Panel
        {
            Location = new Point(0, startY + 10),
            Height = 1,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Width = pnlSummaryRows.Width,
            BackColor = Color.FromArgb(60, 60, 80)
        };

        Label lblFinalLabel = new Label
        {
            Text = "TỔNG THÀNH TIỀN:",
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(0, startY + 24),
            AutoSize = true
        };

        lblFinalTotal = new Label
        {
            Text = "0 đ",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = ColorGold,
            Location = new Point(0, startY + 48),
            AutoSize = true
        };

        pnlSummaryRows.Controls.AddRange(new Control[]
        {
            divider,
            lblFinalLabel,
            lblFinalTotal
        });

        pnlLeftSummary.Controls.Add(pnlSummaryRows);
        pnlLeftSummary.Controls.Add(lblCouponMsg);
        pnlLeftSummary.Controls.Add(tlpCouponRow);
        pnlLeftSummary.Controls.Add(lblSummaryHeader);

        // -----------------------------------------------------
        // CENTER COLUMN - PAYMENT (2x2 GRID + ANIMATION)
        // -----------------------------------------------------

        pnlCenterPayment = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(8, 0, 8, 0),
            BackColor = ColorCard,
            Padding = new Padding(16)
        };

        Label lblPaymentHeader = new Label
        {
            Text = "Phương Thức Thanh Toán",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 32
        };

        tlpMethodGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 84,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        tlpMethodGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        tlpMethodGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        tlpMethodGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        tlpMethodGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        btnMethodCard = CreateMethodButton("💳 Thẻ Quốc Tế / ATM", true, new Padding(0, 0, 4, 4));
        btnMethodCard.Click += (s, e) => SelectPaymentMethod(PaymentMethodType.Card);

        btnMethodBankQR = CreateMethodButton("🏦 QR Pay Ngân Hàng", false, new Padding(4, 0, 0, 4));
        btnMethodBankQR.Click += (s, e) => SelectPaymentMethod(PaymentMethodType.BankQr);

        btnMethodMoMo = CreateMethodButton("🟣 Ví Điện Tử MoMo", false, new Padding(0, 4, 4, 0));
        btnMethodMoMo.Click += (s, e) => SelectPaymentMethod(PaymentMethodType.MoMo);

        btnMethodCounter = CreateMethodButton("🏪 Thanh Toán Tại Quầy", false, new Padding(4, 4, 0, 0));
        btnMethodCounter.Click += (s, e) => SelectPaymentMethod(PaymentMethodType.Counter);

        tlpMethodGrid.Controls.Add(btnMethodCard, 0, 0);
        tlpMethodGrid.Controls.Add(btnMethodBankQR, 1, 0);
        tlpMethodGrid.Controls.Add(btnMethodMoMo, 0, 1);
        tlpMethodGrid.Controls.Add(btnMethodCounter, 1, 1);

        pnlAnimTrack = new Panel
        {
            Dock = DockStyle.Top,
            Height = 4,
            BackColor = Color.FromArgb(36, 38, 52),
            Margin = new Padding(0, 6, 0, 8)
        };

        pnlAnimIndicator = new Panel
        {
            Height = 4,
            Width = 100,
            Location = new Point(0, 0),
            BackColor = ColorGold
        };
        pnlAnimTrack.Controls.Add(pnlAnimIndicator);
        pnlAnimTrack.Resize += (s, e) => UpdateIndicatorTarget(animate: false);

        Panel pnlPaymentBody = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(20, 21, 30),
            Padding = new Padding(14)
        };

        Panel pnlPaymentFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 96,
            BackColor = Color.Transparent
        };

        chkTerms = new CheckBox
        {
            Text = "Tôi đồng ý với Quy định & Điều khoản đặt vé rạp phim.",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = ColorTextMuted,
            Dock = DockStyle.Top,
            Height = 36,
            Checked = false
        };

        btnPayNow = new Button
        {
            Text = "🔒 THANH TOÁN THẺ NGAY",
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.Black,
            BackColor = ColorGold,
            FlatStyle = FlatStyle.Flat,
            Dock = DockStyle.Bottom,
            Height = 46,
            Cursor = Cursors.Hand
        };
        btnPayNow.FlatAppearance.BorderSize = 0;
        btnPayNow.MouseEnter += (s, e) =>
        {
            btnPayNow.BackColor = _selectedPaymentMethod == PaymentMethodType.MoMo ? ColorMoMoHover : ColorGoldHover;
        };
        btnPayNow.MouseLeave += (s, e) =>
        {
            btnPayNow.BackColor = _selectedPaymentMethod == PaymentMethodType.MoMo ? ColorMoMo : ColorGold;
        };
        btnPayNow.Click += BtnPayNow_Click;

        pnlPaymentFooter.Controls.Add(btnPayNow);
        pnlPaymentFooter.Controls.Add(chkTerms);

        Panel pnlMethodContentHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            AutoScroll = true
        };

        // --- 1. PANEL THẺ QUỐC TẾ / ATM ---
        pnlCardSection = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };

        Label lblName = new Label
        {
            Text = "Tên chủ thẻ",
            Font = new Font("Segoe UI", 9),
            ForeColor = ColorTextMuted,
            Location = new Point(0, 6),
            AutoSize = true
        };

        txtCardName = new TextBox
        {
            Location = new Point(0, 28),
            Width = 340,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Segoe UI", 10),
            BackColor = ColorInputBg,
            ForeColor = Color.White,
            PlaceholderText = "VD: NGUYEN VAN A"
        };

        Label lblCardNum = new Label
        {
            Text = "Số thẻ thanh toán",
            Font = new Font("Segoe UI", 9),
            ForeColor = ColorTextMuted,
            Location = new Point(0, 68),
            AutoSize = true
        };

        txtCardNumber = new TextBox
        {
            Location = new Point(0, 90),
            Width = 340,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Segoe UI", 10),
            BackColor = ColorInputBg,
            ForeColor = Color.White,
            PlaceholderText = "Nhập 16 số trên thẻ"
        };

        TableLayoutPanel tlpCardSubRow = new TableLayoutPanel
        {
            Location = new Point(0, 130),
            Width = 340,
            Height = 58,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ColumnCount = 2,
            RowCount = 2
        };
        tlpCardSubRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        tlpCardSubRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        tlpCardSubRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
        tlpCardSubRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));

        Label lblExp = new Label
        {
            Text = "Ngày hết hạn (MM/YY)",
            Font = new Font("Segoe UI", 8.8F),
            ForeColor = ColorTextMuted,
            Dock = DockStyle.Fill
        };

        Label lblCVV = new Label
        {
            Text = "Mã CVV / CVC",
            Font = new Font("Segoe UI", 8.8F),
            ForeColor = ColorTextMuted,
            Dock = DockStyle.Fill,
            Margin = new Padding(8, 0, 0, 0)
        };

        txtExpiry = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10),
            BackColor = ColorInputBg,
            ForeColor = Color.White,
            Margin = new Padding(0, 0, 8, 0),
            PlaceholderText = "MM/YY"
        };

        txtCVV = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10),
            BackColor = ColorInputBg,
            ForeColor = Color.White,
            Margin = new Padding(8, 0, 0, 0),
            PlaceholderText = "CVV",
            PasswordChar = '*'
        };

        tlpCardSubRow.Controls.Add(lblExp, 0, 0);
        tlpCardSubRow.Controls.Add(lblCVV, 1, 0);
        tlpCardSubRow.Controls.Add(txtExpiry, 0, 1);
        tlpCardSubRow.Controls.Add(txtCVV, 1, 1);

        Label lblCardHint = new Label
        {
            Text = "✓ Hỗ trợ Visa, Mastercard, JCB và thẻ ATM nội địa Napas.",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(120, 200, 140),
            Location = new Point(0, 198),
            Width = 340,
            Height = 36,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        pnlCardSection.Controls.AddRange(new Control[]
        {
            lblName, txtCardName,
            lblCardNum, txtCardNumber,
            tlpCardSubRow,
            lblCardHint
        });

        // --- 2. PANEL QR PAY NGÂN HÀNG & VÍ MOMO ---
        pnlQrSection = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Visible = false
        };

        lblQrHeader = new Label
        {
            Text = "QUÉT MÃ VIETQR - TẤT CẢ NGÂN HÀNG",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = ColorGold,
            Dock = DockStyle.Top,
            Height = 26,
            TextAlign = ContentAlignment.MiddleCenter
        };

        Panel pnlQrCenterWrap = new Panel
        {
            Dock = DockStyle.Top,
            Height = 166,
            BackColor = Color.Transparent
        };

        picQrCode = new PictureBox
        {
            Size = new Size(156, 156),
            BackColor = Color.White,
            SizeMode = PictureBoxSizeMode.CenterImage
        };
        pnlQrCenterWrap.Controls.Add(picQrCode);
        pnlQrCenterWrap.Resize += (s, e) =>
        {
            picQrCode.Location = new Point(
                (pnlQrCenterWrap.ClientSize.Width - picQrCode.Width) / 2,
                (pnlQrCenterWrap.ClientSize.Height - picQrCode.Height) / 2);
        };

        lblQrAmount = new Label
        {
            Text = "Số tiền thanh toán: 0 đ",
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 26,
            TextAlign = ContentAlignment.MiddleCenter
        };

        lblQrNote = new Label
        {
            Text = "Mở ứng dụng Ngân hàng bất kỳ để quét mã thanh toán tự động.",
            Font = new Font("Segoe UI", 8.3F),
            ForeColor = ColorTextMuted,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopCenter
        };

        pnlQrSection.Controls.Add(lblQrNote);
        pnlQrSection.Controls.Add(lblQrAmount);
        pnlQrSection.Controls.Add(pnlQrCenterWrap);
        pnlQrSection.Controls.Add(lblQrHeader);

        // --- 3. PANEL THANH TOÁN TẠI QUẦY ---
        pnlCounterSection = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Visible = false
        };

        Label lblCounterTitle = new Label
        {
            Text = "🏪 ĐẶT GIỮ CHỖ & THANH TOÁN TẠI QUẦY",
            Font = new Font("Segoe UI", 9.8F, FontStyle.Bold),
            ForeColor = ColorGold,
            Dock = DockStyle.Top,
            Height = 28
        };

        Label lblCounterDesc = new Label
        {
            Text = "• Hệ thống sẽ xuất mã đặt vé giữ ghế ngay sau khi xác nhận.\n" +
                   "• Quý khách vui lòng đến quầy vé rạp Cineworld thanh toán bằng Tiền mặt / Thẻ / QR trước giờ chiếu ít nhất 30 phút.\n" +
                   "• Quá thời gian giữ chỗ, ghế sẽ tự động mở lại cho khách khác.",
            Font = new Font("Segoe UI", 8.8F),
            ForeColor = Color.FromArgb(215, 220, 235),
            Dock = DockStyle.Top,
            Height = 115
        };

        Label lblCounterPhone = new Label
        {
            Text = "Số điện thoại nhận mã giữ vé tại quầy *",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = ColorTextMuted,
            Dock = DockStyle.Top,
            Height = 24
        };

        txtCounterPhone = new TextBox
        {
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI", 10.5F),
            BackColor = ColorInputBg,
            ForeColor = Color.White,
            PlaceholderText = "Nhập số điện thoại của bạn (VD: 0901234567)"
        };

        pnlCounterSection.Controls.Add(txtCounterPhone);
        pnlCounterSection.Controls.Add(lblCounterPhone);
        pnlCounterSection.Controls.Add(lblCounterDesc);
        pnlCounterSection.Controls.Add(lblCounterTitle);

        pnlMethodContentHost.Controls.Add(pnlCardSection);
        pnlMethodContentHost.Controls.Add(pnlQrSection);
        pnlMethodContentHost.Controls.Add(pnlCounterSection);

        pnlPaymentBody.Controls.Add(pnlMethodContentHost);
        pnlPaymentBody.Controls.Add(pnlPaymentFooter);

        pnlCenterPayment.Controls.Add(pnlPaymentBody);
        pnlCenterPayment.Controls.Add(pnlAnimTrack);
        pnlCenterPayment.Controls.Add(tlpMethodGrid);
        pnlCenterPayment.Controls.Add(lblPaymentHeader);

        // -----------------------------------------------------
        // RIGHT COLUMN - TICKET PREVIEW
        // -----------------------------------------------------

        pnlRightTicketPreview = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(8, 0, 0, 0),
            BackColor = ColorCard,
            Padding = new Padding(16)
        };

        Label lblPreviewHeader = new Label
        {
            Text = "Xem Trước Vé Điện Tử",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 32
        };

        Panel pnlTicketPass = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(245, 230, 180),
            Padding = new Padding(14)
        };

        Panel pnlBarcodeMock = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 58,
            BackColor = Color.White
        };

        Label lblBarcodeText = new Label
        {
            Text = "||||||||||||||||||||||||||||||||\n*CINEMA-TICKET*",
            Font = new Font("Consolas", 9F, FontStyle.Bold),
            ForeColor = Color.Black,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlBarcodeMock.Controls.Add(lblBarcodeText);

        Panel pnlTicketMeta = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 122,
            Padding = new Padding(0, 8, 0, 4),
            BackColor = Color.Transparent
        };

        lblPreviewMovieTitle = new Label
        {
            Text = "TÊN PHIM",
            Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
            ForeColor = Color.Black,
            Dock = DockStyle.Top,
            Height = 28,
            AutoEllipsis = true
        };

        lblPreviewDateTime = new Label
        {
            Text = "Ngày: --/--/---- | Giờ: --:--",
            Font = new Font("Segoe UI", 9.2F),
            ForeColor = Color.FromArgb(60, 60, 60),
            Dock = DockStyle.Top,
            Height = 22
        };

        lblPreviewVenueSeats = new Label
        {
            Text = "Ghế: Chưa chọn",
            Font = new Font("Segoe UI", 9.8F, FontStyle.Bold),
            ForeColor = Color.Black,
            Dock = DockStyle.Top,
            Height = 24,
            AutoEllipsis = true
        };

        lblPreviewPrice = new Label
        {
            Text = "Tổng: 0 đ",
            Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
            ForeColor = Color.DarkRed,
            Dock = DockStyle.Top,
            Height = 28
        };

        pnlTicketMeta.Controls.Add(lblPreviewPrice);
        pnlTicketMeta.Controls.Add(lblPreviewVenueSeats);
        pnlTicketMeta.Controls.Add(lblPreviewDateTime);
        pnlTicketMeta.Controls.Add(lblPreviewMovieTitle);

        picPreviewPoster = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(40, 40, 50)
        };

        pnlTicketPass.Controls.Add(picPreviewPoster);
        pnlTicketPass.Controls.Add(pnlTicketMeta);
        pnlTicketPass.Controls.Add(pnlBarcodeMock);

        pnlRightTicketPreview.Controls.Add(pnlTicketPass);
        pnlRightTicketPreview.Controls.Add(lblPreviewHeader);

        // -----------------------------------------------------
        // ADD TO FORM
        // -----------------------------------------------------

        tlpMainGrid.Controls.Add(pnlLeftSummary, 0, 0);
        tlpMainGrid.Controls.Add(pnlCenterPayment, 1, 0);
        tlpMainGrid.Controls.Add(pnlRightTicketPreview, 2, 0);

        Controls.Add(tlpMainGrid);
        Controls.Add(tlpHeader);
    }

    // =========================================================
    // 9. CREATE, ANIMATION & SWITCH PAYMENT METHODS
    // =========================================================

    private void SetupAnimationTimer()
    {
        _animTimer = new System.Windows.Forms.Timer
        {
            Interval = 15
        };
        _animTimer.Tick += (s, e) =>
        {
            if (pnlAnimIndicator == null) return;

            int dx = _targetIndicatorX - pnlAnimIndicator.Left;
            int dw = _targetIndicatorW - pnlAnimIndicator.Width;

            if (Math.Abs(dx) <= 2 && Math.Abs(dw) <= 2)
            {
                pnlAnimIndicator.Left = _targetIndicatorX;
                pnlAnimIndicator.Width = _targetIndicatorW;
                pnlAnimIndicator.BackColor = _targetIndicatorColor;
                _animTimer.Stop();
                return;
            }

            pnlAnimIndicator.Left += (int)Math.Ceiling(dx * 0.32);
            pnlAnimIndicator.Width += (int)Math.Ceiling(dw * 0.32);
            pnlAnimIndicator.BackColor = _targetIndicatorColor;
        };
    }

    private Button CreateMethodButton(string text, bool isActive, Padding margin)
    {
        Button btn = new Button
        {
            Text = text,
            Font = new Font("Segoe UI", 8.8F, FontStyle.Bold),
            ForeColor = isActive ? Color.Black : Color.White,
            BackColor = isActive ? ColorGold : ColorInputBg,
            FlatStyle = FlatStyle.Flat,
            Dock = DockStyle.Fill,
            Margin = margin,
            AutoEllipsis = true,
            Cursor = Cursors.Hand
        };

        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private void SetButtonActiveState(Button btn, bool active, Color? customActiveBg = null, Color? customActiveFg = null)
    {
        if (active)
        {
            btn.BackColor = customActiveBg ?? ColorGold;
            btn.ForeColor = customActiveFg ?? Color.Black;
        }
        else
        {
            btn.BackColor = ColorInputBg;
            btn.ForeColor = Color.White;
        }
    }

    private void UpdateIndicatorTarget(bool animate)
    {
        if (pnlAnimTrack == null || pnlAnimIndicator == null) return;

        int halfWidth = pnlAnimTrack.ClientSize.Width / 2;
        bool isRightCol = _selectedPaymentMethod == PaymentMethodType.BankQr ||
                          _selectedPaymentMethod == PaymentMethodType.Counter;

        _targetIndicatorX = isRightCol ? halfWidth : 0;
        _targetIndicatorW = Math.Max(40, halfWidth);
        _targetIndicatorColor = _selectedPaymentMethod == PaymentMethodType.MoMo ? ColorMoMo : ColorGold;

        if (!animate)
        {
            pnlAnimIndicator.Left = _targetIndicatorX;
            pnlAnimIndicator.Width = _targetIndicatorW;
            pnlAnimIndicator.BackColor = _targetIndicatorColor;
        }
        else
        {
            _animTimer.Start();
        }
    }

    private void SelectPaymentMethod(PaymentMethodType method, bool animate = true)
    {
        _selectedPaymentMethod = method;

        SetButtonActiveState(btnMethodCard, method == PaymentMethodType.Card);
        SetButtonActiveState(btnMethodBankQR, method == PaymentMethodType.BankQr);
        SetButtonActiveState(btnMethodMoMo, method == PaymentMethodType.MoMo, ColorMoMo, Color.White);
        SetButtonActiveState(btnMethodCounter, method == PaymentMethodType.Counter);

        UpdateIndicatorTarget(animate);

        pnlCardSection.Visible = false;
        pnlQrSection.Visible = false;
        pnlCounterSection.Visible = false;

        switch (method)
        {
            case PaymentMethodType.Card:
                pnlCardSection.Visible = true;
                btnPayNow.BackColor = ColorGold;
                btnPayNow.ForeColor = Color.Black;
                btnPayNow.Text = "🔒 THANH TOÁN THẺ NGAY";
                break;

            case PaymentMethodType.BankQr:
                pnlQrSection.Visible = true;
                lblQrHeader.Text = "🏦 QUÉT MÃ VIETQR (TẤT CẢ NGÂN HÀNG)";
                lblQrHeader.ForeColor = ColorGold;
                lblQrNote.Text = "Hỗ trợ tất cả ngân hàng: Vietcombank, MB, Techcombank,\nACB, BIDV, VietinBank, TPBank, VPBank...\nNội dung CK: CINEWORLD VE PHIM";
                btnPayNow.BackColor = ColorGold;
                btnPayNow.ForeColor = Color.Black;
                btnPayNow.Text = "✓ XÁC NHẬN ĐÃ CHUYỂN KHOẢN QR";
                RefreshQrPreviewImage(Color.Black);
                break;

            case PaymentMethodType.MoMo:
                pnlQrSection.Visible = true;
                lblQrHeader.Text = "🟣 QUÉT MÃ VÍ ĐIỆN TỬ MOMO";
                lblQrHeader.ForeColor = Color.FromArgb(255, 110, 190);
                lblQrNote.Text = "Mở ứng dụng Ví MoMo -> Chọn 'Quét Mã'\nNgười nhận: RẠP CHIẾU PHIM CINEWORLD (0909.888.999)\nTự động điền đúng số tiền cần thanh toán.";
                btnPayNow.BackColor = ColorMoMo;
                btnPayNow.ForeColor = Color.White;
                btnPayNow.Text = "🟣 XÁC NHẬN THANH TOÁN MOMO";
                RefreshQrPreviewImage(ColorMoMo);
                break;

            case PaymentMethodType.Counter:
                pnlCounterSection.Visible = true;
                btnPayNow.BackColor = ColorGold;
                btnPayNow.ForeColor = Color.Black;
                btnPayNow.Text = "🏪 XÁC NHẬN ĐẶT VÉ TẠI QUẦY";
                break;
        }
    }

    private void RefreshQrPreviewImage(Color qrColor)
    {
        if (picQrCode == null) return;

        var oldImg = picQrCode.Image;
        picQrCode.Image = GenerateStyledQrBitmap(144, qrColor, $"{_selectedPaymentMethod}_{totalAmount}");
        oldImg?.Dispose();
    }

    private Bitmap GenerateStyledQrBitmap(int size, Color moduleColor, string seedText)
    {
        Bitmap bmp = new Bitmap(size, size);
        using Graphics g = Graphics.FromImage(bmp);
        g.Clear(Color.White);
        g.SmoothingMode = SmoothingMode.None;

        int gridCount = 21;
        int cellSize = size / gridCount;
        int offset = (size - gridCount * cellSize) / 2;

        using Brush darkBrush = new SolidBrush(moduleColor);
        using Brush whiteBrush = new SolidBrush(Color.White);

        int hash = seedText.GetHashCode();
        Random rnd = new Random(hash);

        for (int r = 0; r < gridCount; r++)
        {
            for (int c = 0; c < gridCount; c++)
            {
                bool isFinderZone =
                    (r < 7 && c < 7) ||
                    (r < 7 && c >= gridCount - 7) ||
                    (r >= gridCount - 7 && c < 7);

                if (isFinderZone) continue;

                if (rnd.Next(0, 100) < 48)
                {
                    g.FillRectangle(darkBrush, offset + c * cellSize, offset + r * cellSize, cellSize, cellSize);
                }
            }
        }

        void DrawFinderPattern(int startRow, int startCol)
        {
            g.FillRectangle(darkBrush, offset + startCol * cellSize, offset + startRow * cellSize, 7 * cellSize, 7 * cellSize);
            g.FillRectangle(whiteBrush, offset + (startCol + 1) * cellSize, offset + (startRow + 1) * cellSize, 5 * cellSize, 5 * cellSize);
            g.FillRectangle(darkBrush, offset + (startCol + 2) * cellSize, offset + (startRow + 2) * cellSize, 3 * cellSize, 3 * cellSize);
        }

        DrawFinderPattern(0, 0);
        DrawFinderPattern(0, gridCount - 7);
        DrawFinderPattern(gridCount - 7, 0);

        return bmp;
    }

    private string GetPaymentMethodDisplayName()
    {
        return _selectedPaymentMethod switch
        {
            PaymentMethodType.Card => "Thẻ Quốc Tế / ATM",
            PaymentMethodType.BankQr => "QR Pay Ngân Hàng (VietQR)",
            PaymentMethodType.MoMo => "Ví Điện Tử MoMo",
            PaymentMethodType.Counter => "Thanh toán tại quầy",
            _ => "Thanh toán mô phỏng"
        };
    }

    // =========================================================
    // 10. SUMMARY ROW
    // =========================================================

    private Label AddSummaryRow(
        Panel parent,
        string labelText,
        string valueText,
        ref int yPos,
        Color? valColor = null)
    {
        Label lblTitle = new Label
        {
            Text = labelText,
            UseMnemonic = false,
            Font = new Font("Segoe UI", 9),
            ForeColor = ColorTextMuted,
            Location = new Point(0, yPos),
            AutoSize = true
        };

        Label lblVal = new Label
        {
            Text = valueText,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = valColor ?? Color.White,
            Location = new Point(parent.Width - 150, yPos),
            Size = new Size(150, 20),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            TextAlign = ContentAlignment.MiddleRight
        };

        parent.Controls.Add(lblTitle);
        parent.Controls.Add(lblVal);

        yPos += 28;

        return lblVal;
    }

    // =========================================================
    // 11. CALCULATE TOTAL
    // =========================================================

    private void CalculateTotal()
    {
        decimal orderAmount = ticketSubTotal + foodSubTotal;

        if (_appliedPromotion != null)
        {
            try
            {
                (_, discountAmount) = PromotionService.CalculateDiscountForPromotion(
                    _appliedPromotion,
                    orderAmount);
            }
            catch
            {
                _appliedPromotion = null;
                discountAmount = 0m;
            }
        }

        totalAmount = orderAmount - discountAmount;

        if (totalAmount < 0)
            totalAmount = 0m;
    }

    // =========================================================
    // 12. UPDATE UI
    // =========================================================

    private void UpdateUIWithData()
    {
        if (IsDisposed || lblFinalTotal == null)
            return;

        int seatCount = SelectedSeats.Count;

        decimal basePricePerSeat = seatCount > 0
            ? ticketSubTotal / seatCount
            : 0m;

        lblTicketPriceInfo.Text =
            $"{basePricePerSeat.ToString("N0", ViCulture)} đ";

        lblQtyInfo.Text = $"{seatCount} vé";

        lblTicketSubTotal.Text =
            $"{ticketSubTotal.ToString("N0", ViCulture)} đ";

        lblFoodSubTotal.Text =
            $"{foodSubTotal.ToString("N0", ViCulture)} đ";

        lblDiscountVal.Text =
            $"-{discountAmount.ToString("N0", ViCulture)} đ";

        string formattedFinalTotal = $"{totalAmount.ToString("N0", ViCulture)} đ";
        lblFinalTotal.Text = formattedFinalTotal;

        if (lblQrAmount != null)
            lblQrAmount.Text = $"Số tiền thanh toán: {formattedFinalTotal}";

        if (_selectedPaymentMethod == PaymentMethodType.BankQr)
            RefreshQrPreviewImage(Color.Black);
        else if (_selectedPaymentMethod == PaymentMethodType.MoMo)
            RefreshQrPreviewImage(ColorMoMo);

        // Movie
        lblPreviewMovieTitle.Text =
            MovieData?.Title ?? "Chưa chọn phim";

        // Poster
        if (picPreviewPoster.Image != null)
        {
            var oldImage = picPreviewPoster.Image;
            picPreviewPoster.Image = null;
            oldImage.Dispose();
        }

        if (!string.IsNullOrWhiteSpace(MovieData?.PosterUrl) &&
            File.Exists(MovieData.PosterUrl))
        {
            try
            {
                using var stream = new FileStream(
                    MovieData.PosterUrl,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite);

                using var image = Image.FromStream(stream);
                picPreviewPoster.Image = new Bitmap(image);
            }
            catch
            {
                // Không làm gián đoạn thanh toán nếu ảnh lỗi.
            }
        }

        // Showtime
        if (ShowtimeData != null)
        {
            lblPreviewDateTime.Text =
                $"Ngày: {ShowtimeData.StartTime:dd/MM/yyyy} | " +
                $"Giờ: {ShowtimeData.StartTime:HH:mm}";
        }
        else
        {
            lblPreviewDateTime.Text = "Ngày: --/--/---- | Giờ: --:--";
        }

        // Seats
        lblPreviewVenueSeats.Text =
            $"Ghế: {(seatCount > 0 ? string.Join(", ", SelectedSeats) : "Chưa chọn")}";

        // Total
        lblPreviewPrice.Text =
            $"Tổng tiền: {formattedFinalTotal}";
    }

    // =========================================================
    // 13. APPLY COUPON (SQLITE PROMOTION SERVICE)
    // =========================================================

    private void BtnApplyCoupon_Click(object? sender, EventArgs e)
    {
        string code = txtCoupon.Text.Trim().ToUpperInvariant();

        _appliedPromotion = null;
        discountAmount = 0m;

        if (string.IsNullOrWhiteSpace(code))
        {
            lblCouponMsg.ForeColor = Color.OrangeRed;
            lblCouponMsg.Text = "Vui lòng nhập mã voucher.";

            CalculateTotal();
            UpdateUIWithData();
            return;
        }

        try
        {
            decimal orderAmount = ticketSubTotal + foodSubTotal;
            int? currentUserId = CinemaManagement.Utils.UserSession.CurrentUser?.Id;
            var (promotion, calculatedDiscount) =
                _promotionService.ValidateAndCalculateDiscount(
                    code,
                    orderAmount,
                    currentUserId);

            _appliedPromotion = promotion;
            discountAmount = calculatedDiscount;

            lblCouponMsg.ForeColor = Color.LightGreen;
            lblCouponMsg.Text = $"✓ Áp dụng {promotion.Code} thành công!";

            CalculateTotal();
            UpdateUIWithData();
        }
        catch (InvalidOperationException ex)
        {
            ShowCouponError(ex.Message);
        }
        catch (Exception ex)
        {
            ShowCouponError("Không thể kiểm tra voucher:\n" + ex.Message);
        }
    }

    private void ShowCouponError(string message)
    {
        _appliedPromotion = null;
        discountAmount = 0m;

        lblCouponMsg.ForeColor = Color.OrangeRed;
        lblCouponMsg.Text = message;

        CalculateTotal();
        UpdateUIWithData();
    }

    // =========================================================
    // 14. PAYMENT
    // =========================================================

    private void BtnPayNow_Click(object? sender, EventArgs e)
    {
        if (btnPayNow == null || !btnPayNow.Enabled)
            return;

        if (MovieData == null)
        {
            ShowWarning("Không tìm thấy thông tin phim.");
            return;
        }

        if (ShowtimeData == null)
        {
            ShowWarning("Không tìm thấy thông tin suất chiếu.");
            return;
        }

        if (ShowtimeData.StartTime <= DateTime.Now)
        {
            ShowWarning(
                "Suất chiếu này đã bắt đầu hoặc đã kết thúc.\n" +
                "Vui lòng quay lại chọn suất chiếu khác.");
            return;
        }

        if (SelectedSeats == null || SelectedSeats.Count == 0)
        {
            ShowWarning("Vui lòng chọn ít nhất một ghế.");
            return;
        }

        if (_bookingContext != null)
        {
            if (!_bookingContext.CanCheckout)
            {
                ShowWarning("Thông tin đặt vé không hợp lệ.");
                return;
            }

            if (_bookingContext.Movie?.Id != MovieData.Id ||
                _bookingContext.Showtime?.Id != ShowtimeData.Id ||
                !_bookingContext.Seats.SequenceEqual(SelectedSeats))
            {
                ShowWarning(
                    "Dữ liệu đặt vé đã thay đổi. " +
                    "Vui lòng quay lại kiểm tra thông tin.");

                return;
            }

            ticketSubTotal = _bookingContext.TicketAmount;
            foodSubTotal = _bookingContext.FoodAmount;
        }

        if (ticketSubTotal <= 0)
        {
            ShowWarning("Tổng tiền vé không hợp lệ.");
            return;
        }

        CalculateTotal();
        UpdateUIWithData();

        // Đơn hàng sau giảm giá luôn phải lớn hơn 0 đ để đảm bảo ngân sách
        if (totalAmount <= 0m)
        {
            ShowWarning("Tổng tiền thanh toán sau giảm giá phải lớn hơn 0 đ.");
            return;
        }

        if (_selectedPaymentMethod == PaymentMethodType.Card)
        {
            if (!ValidateCardInputs(out string cardError))
            {
                ShowWarning(cardError);
                return;
            }
        }
        else if (_selectedPaymentMethod == PaymentMethodType.Counter)
        {
            if (!ValidateCounterPhone(txtCounterPhone.Text, out string normalizedPhone, out string phoneError))
            {
                ShowWarning(phoneError);
                txtCounterPhone.Focus();
                return;
            }

            txtCounterPhone.Text = normalizedPhone;
        }

        if (!chkTerms.Checked)
        {
            ShowWarning("Vui lòng tích chọn đồng ý với Điều khoản sử dụng.");
            return;
        }

        string methodDisplayName = GetPaymentMethodDisplayName();
        string actionPrompt = _selectedPaymentMethod == PaymentMethodType.Counter
            ? "Bạn xác nhận đặt giữ vé và thanh toán trực tiếp tại quầy?"
            : "Bạn xác nhận hoàn tất thanh toán đơn hàng?";

        DialogResult confirm = MessageBox.Show(
            $"{actionPrompt}\n\n" +
            $"Phim: {MovieData.Title}\n" +
            $"Số ghế: {SelectedSeats.Count} ({string.Join(", ", SelectedSeats)})\n" +
            $"Tiền vé: {ticketSubTotal.ToString("N0", ViCulture)} đ\n" +
            $"Bắp nước: {foodSubTotal.ToString("N0", ViCulture)} đ\n" +
            $"Giảm giá: {discountAmount.ToString("N0", ViCulture)} đ\n" +
            $"Tổng tiền: {totalAmount.ToString("N0", ViCulture)} đ\n" +
            $"Phương thức: {methodDisplayName}",
            "Xác nhận giao dịch",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
            return;

        btnPayNow.Enabled = false;

        try
        {
            string bookingCode =
                $"BK{DateTime.Now:yyyyMMddHHmmssfff}";

            var result = new BookingResultEventArgs
            {
                BookingCode = bookingCode,
                Movie = MovieData,
                Showtime = ShowtimeData,
                Seats = SelectedSeats.ToList(),
                FoodCart = new Dictionary<int, CartItem>(FoodCart),
                TicketAmount = ticketSubTotal,
                FoodAmount = foodSubTotal,
                TotalAmount = totalAmount,
                PromotionCode = _appliedPromotion?.Code ?? string.Empty,
                DiscountAmount = discountAmount,
                BookingTime = DateTime.Now,
                Channel = _selectedPaymentMethod == PaymentMethodType.Counter ? "Tại quầy (Đặt trước)" : "Online",
                PaymentMethod = methodDisplayName,
                PaymentStatus = (_selectedPaymentMethod == PaymentMethodType.Counter && totalAmount > 0m)
                    ? "PENDING"
                    : "PAID",
                CustomerName = _selectedPaymentMethod == PaymentMethodType.Card
                    ? txtCardName.Text.Trim()
                    : (_selectedPaymentMethod == PaymentMethodType.Counter ? $"SĐT: {txtCounterPhone.Text.Trim()}" : string.Empty)
            };

            OnPaymentCompleted?.Invoke(this, result);
        }
        catch (Exception ex)
        {
            ShowWarning(
                "Không thể hoàn tất quá trình đặt vé:\n" + ex.Message);
        }
        finally
        {
            if (!IsDisposed && btnPayNow != null && !btnPayNow.IsDisposed)
            {
                btnPayNow.Enabled = true;
            }
        }
    }

    // =========================================================
    // 15. WARNING MESSAGE
    // =========================================================

    private bool ValidateCardInputs(out string errorMessage)
    {
        string cardName = txtCardName.Text.Trim();
        if (cardName.Length < 2 || !cardName.Any(char.IsLetter))
        {
            txtCardName.Focus();
            errorMessage = "Vui lòng nhập tên chủ thẻ hợp lệ (ít nhất 2 ký tự chữ).";
            return false;
        }

        string cardNumber = txtCardNumber.Text
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Trim();

        if (cardNumber.Length < 13 || cardNumber.Length > 19 || !cardNumber.All(char.IsDigit))
        {
            txtCardNumber.Focus();
            errorMessage = "Số thẻ thanh toán phải gồm từ 13 đến 19 chữ số.";
            return false;
        }

        string expiry = txtExpiry.Text.Trim();
        string[] expParts = expiry.Split('/');
        if (expParts.Length != 2 ||
            expParts[0].Length is < 1 or > 2 ||
            expParts[1].Length != 2 ||
            !int.TryParse(expParts[0], out int expMonth) ||
            !int.TryParse(expParts[1], out int expYearTwoDigits) ||
            expMonth < 1 || expMonth > 12)
        {
            txtExpiry.Focus();
            errorMessage = "Ngày hết hạn thẻ phải đúng định dạng MM/YY (ví dụ: 08/28).";
            return false;
        }

        int fullYear = 2000 + expYearTwoDigits;
        DateTime cardEndOfMonth = new DateTime(fullYear, expMonth, 1).AddMonths(1).AddDays(-1);
        if (cardEndOfMonth < DateTime.Today)
        {
            txtExpiry.Focus();
            errorMessage = "Thẻ thanh toán đã hết hạn.";
            return false;
        }

        string cvv = txtCVV.Text.Trim();
        if ((cvv.Length != 3 && cvv.Length != 4) || !cvv.All(char.IsDigit))
        {
            txtCVV.Focus();
            errorMessage = "Mã bảo mật CVV/CVC phải gồm 3 hoặc 4 chữ số.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private static bool ValidateCounterPhone(
        string rawPhone,
        out string normalizedPhone,
        out string errorMessage)
    {
        string cleaned = (rawPhone ?? string.Empty)
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace(".", string.Empty)
            .Trim();

        if (cleaned.StartsWith("+84", StringComparison.Ordinal))
        {
            cleaned = "0" + cleaned.Substring(3);
        }

        if (cleaned.Length < 9 || cleaned.Length > 11 || !cleaned.All(char.IsDigit))
        {
            normalizedPhone = string.Empty;
            errorMessage = "Số điện thoại nhận mã giữ vé phải gồm từ 9 đến 11 chữ số hợp lệ.";
            return false;
        }

        normalizedPhone = cleaned;
        errorMessage = string.Empty;
        return true;
    }

    private void ShowWarning(string message)
    {
        MessageBox.Show(
            message,
            "Thông báo",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }
}