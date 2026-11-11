namespace CinemaManagement.UserControls.Customer;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Models;

public partial class CheckoutUC : UserControl
{
    // Bảng màu Modern Dark Cinema
    private readonly Color ColorBg = Color.FromArgb(14, 15, 22);
    private readonly Color ColorCard = Color.FromArgb(24, 25, 36);
    private readonly Color ColorGold = Color.FromArgb(245, 197, 24);
    private readonly Color ColorTextMuted = Color.FromArgb(150, 150, 170);
    private readonly Color ColorInputBg = Color.FromArgb(34, 35, 48);

    // Dynamic Data Input
    public Movie? MovieData { get; private set; }
    public Showtime? ShowtimeData { get; private set; }
    public List<string> SelectedSeats { get; private set; } = new List<string>();
    public Dictionary<int, (string Name, decimal Price, int Qty, string ImgPath)> FoodCart { get; private set; } 
        = new Dictionary<int, (string, decimal, int, string)>();

    private decimal ticketSubTotal = 0m;
    private decimal foodSubTotal = 0m;
    private decimal discountAmount = 0m;
    private decimal totalAmount = 0m;

    // Controls UI
    private Panel pnlHeader;
    private Button btnBack;
    private Panel pnlLeftSummary;
    private Panel pnlCenterPayment;
    private Panel pnlRightTicketPreview;

    // Controls Cột Trái (Summary & Coupon)
    private TextBox txtCoupon;
    private Button btnApplyCoupon;
    private Label lblCouponMsg;
    private Label lblTicketPriceInfo;
    private Label lblQtyInfo;
    private Label lblTicketSubTotal;
    private Label lblFoodSubTotal;
    private Label lblDiscountVal;
    private Label lblFinalTotal;

    // Controls Cột Giữa (Payment Method)
    private TextBox txtCardName;
    private TextBox txtCardNumber;
    private TextBox txtExpiry;
    private TextBox txtCVV;
    private CheckBox chkTerms;
    private Button btnPayNow;

    // Controls Cột Phải (Ticket Preview Card)
    private PictureBox picPreviewPoster;
    private Label lblPreviewMovieTitle;
    private Label lblPreviewDateTime;
    private Label lblPreviewVenueSeats;
    private Label lblPreviewPrice;

    // Events
    public event EventHandler<BookingResultEventArgs>? OnPaymentCompleted;
    public event EventHandler? OnBackClicked;

    public CheckoutUC()
    {
        InitializeComponents();
    }

    public void SetBookingData(Movie movie, Showtime showtime, List<string> seats, decimal ticketTotal, 
        Dictionary<int, (string Name, decimal Price, int Qty, string ImgPath)> foodCart, decimal foodTotal)
    {
        MovieData = movie;
        ShowtimeData = showtime;
        SelectedSeats = seats;
        ticketSubTotal = ticketTotal;
        FoodCart = foodCart;
        foodSubTotal = foodTotal;

        CalculateTotal();
        UpdateUIWithData();
    }

    private void InitializeComponents()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ColorBg;

        // -------------------------------------------------------------
        // 1. HEADER (STEPPER & BACK BUTTON)
        // -------------------------------------------------------------
        pnlHeader = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(18, 19, 28) };
        btnBack = new Button
        {
            Text = "← Quay lại",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(35, 36, 50),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(100, 32),
            Location = new Point(20, 14),
            Cursor = Cursors.Hand
        };
        btnBack.FlatAppearance.BorderSize = 0;
        btnBack.Click += (s, e) => OnBackClicked?.Invoke(this, EventArgs.Empty);

        Panel pnlStepper = new Panel { Size = new Size(500, 40), Location = new Point(350, 10) };
        Label lblStep1 = new Label { Text = "① CHỌN SUẤT", Font = new Font("Segoe UI", 9), ForeColor = ColorTextMuted, Location = new Point(0, 10), AutoSize = true };
        Label lblDiv1 = new Label { Text = "───", ForeColor = ColorTextMuted, Location = new Point(105, 10), AutoSize = true };
        Label lblStep2 = new Label { Text = "② CHỌN GHẾ", Font = new Font("Segoe UI", 9), ForeColor = ColorTextMuted, Location = new Point(145, 10), AutoSize = true };
        Label lblDiv2 = new Label { Text = "───", ForeColor = ColorTextMuted, Location = new Point(245, 10), AutoSize = true };
        Label lblStep3 = new Label { Text = "③ THANH TOÁN", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = ColorGold, Location = new Point(285, 10), AutoSize = true };
        pnlStepper.Controls.AddRange(new Control[] { lblStep1, lblDiv1, lblStep2, lblDiv2, lblStep3 });

        pnlHeader.Controls.Add(btnBack);
        pnlHeader.Controls.Add(pnlStepper);

        // -------------------------------------------------------------
        // 2. CỘT TRÁI - PAYMENT SUMMARY & VOUCHER
        // -------------------------------------------------------------
        pnlLeftSummary = new Panel
        {
            Location = new Point(20, 75),
            Size = new Size(340, 640),
            BackColor = ColorCard,
            Padding = new Padding(15)
        };

        Label lblSummaryHeader = new Label { Text = "Tóm Tắt Đơn Hàng", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.White, Location = new Point(15, 15), AutoSize = true };

        // Ô Mã giảm giá
        txtCoupon = new TextBox { Location = new Point(15, 50), Size = new Size(200, 32), Font = new Font("Segoe UI", 10), BackColor = ColorInputBg, ForeColor = Color.White, PlaceholderText = "Nhập mã giảm giá..." };
        btnApplyCoupon = new Button { Text = "Áp dụng", Location = new Point(222, 49), Size = new Size(100, 32), Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = ColorGold, ForeColor = Color.Black, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        btnApplyCoupon.FlatAppearance.BorderSize = 0;
        btnApplyCoupon.Click += BtnApplyCoupon_Click;

        lblCouponMsg = new Label { Text = "", Font = new Font("Segoe UI", 8.5f), ForeColor = Color.LightGreen, Location = new Point(15, 87), AutoSize = true };

        // Bảng tính tiền
        int startY = 120;
        lblTicketPriceInfo = AddSummaryRow(pnlLeftSummary, "Giá vé cơ bản:", "0 đ", ref startY);
        lblQtyInfo = AddSummaryRow(pnlLeftSummary, "Số lượng vé:", "0 vé", ref startY);
        lblTicketSubTotal = AddSummaryRow(pnlLeftSummary, "Tổng tiền vé:", "0 đ", ref startY);
        lblFoodSubTotal = AddSummaryRow(pnlLeftSummary, "Bắp nước & Combo:", "0 đ", ref startY);
        lblDiscountVal = AddSummaryRow(pnlLeftSummary, "Giảm giá Voucher:", "-0 đ", ref startY, Color.OrangeRed);

        Panel divider = new Panel { Location = new Point(15, startY + 10), Size = new Size(310, 1), BackColor = Color.FromArgb(60, 60, 80) };
        pnlLeftSummary.Controls.Add(divider);

        Label lblFinalLabel = new Label { Text = "TỔNG THÀNH TIỀN:", Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.White, Location = new Point(15, startY + 25), AutoSize = true };
        lblFinalTotal = new Label { Text = "0 đ", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = ColorGold, Location = new Point(15, startY + 50), AutoSize = true };

        pnlLeftSummary.Controls.AddRange(new Control[] { lblSummaryHeader, txtCoupon, btnApplyCoupon, lblCouponMsg, lblFinalLabel, lblFinalTotal });

        // -------------------------------------------------------------
        // 3. CỘT GIỮA - PHƯƠNG THỨC THANH TOÁN (PAYMENT FORM)
        // -------------------------------------------------------------
        pnlCenterPayment = new Panel
        {
            Location = new Point(380, 75),
            Size = new Size(420, 640),
            BackColor = ColorCard,
            Padding = new Padding(20)
        };

        Label lblPaymentHeader = new Label { Text = "Phương Thức Thanh Toán", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.White, Location = new Point(20, 15), AutoSize = true };

        // Tabs Phương thức thanh toán (Visa, QR Code, ATM)
        FlowLayoutPanel flpMethods = new FlowLayoutPanel { Location = new Point(20, 50), Size = new Size(380, 45) };
        Button btnMethodCard = CreateMethodButton("💳 Thẻ Quốc Tế / ATM", true);
        Button btnMethodQR = CreateMethodButton("📱 Quét Mã QR", false);
        flpMethods.Controls.AddRange(new Control[] { btnMethodCard, btnMethodQR });

        // Form Nhập thông tin thẻ (Mô phỏng)
        Panel pnlCardForm = new Panel { Location = new Point(20, 105), Size = new Size(380, 440), BackColor = Color.FromArgb(20, 21, 30), Padding = new Padding(15) };
        
        Label lblName = new Label { Text = "Tên chủ thẻ", Font = new Font("Segoe UI", 9), ForeColor = ColorTextMuted, Location = new Point(15, 15), AutoSize = true };
        txtCardName = new TextBox { Text = "NGUYEN VAN A", Location = new Point(15, 38), Size = new Size(350, 30), Font = new Font("Segoe UI", 10), BackColor = ColorInputBg, ForeColor = Color.White };

        Label lblCardNum = new Label { Text = "Số thẻ thanh toán", Font = new Font("Segoe UI", 9), ForeColor = ColorTextMuted, Location = new Point(15, 80), AutoSize = true };
        txtCardNumber = new TextBox { Text = "9704 1234 5678 9012", Location = new Point(15, 103), Size = new Size(350, 30), Font = new Font("Segoe UI", 10), BackColor = ColorInputBg, ForeColor = Color.White };

        Label lblExp = new Label { Text = "Ngày hết hạn (MM/YY)", Font = new Font("Segoe UI", 9), ForeColor = ColorTextMuted, Location = new Point(15, 145), AutoSize = true };
        txtExpiry = new TextBox { Text = "12/28", Location = new Point(15, 168), Size = new Size(160, 30), Font = new Font("Segoe UI", 10), BackColor = ColorInputBg, ForeColor = Color.White };

        Label lblCVV = new Label { Text = "Mã CVV / CVC", Font = new Font("Segoe UI", 9), ForeColor = ColorTextMuted, Location = new Point(205, 145), AutoSize = true };
        txtCVV = new TextBox { Text = "888", Location = new Point(205, 168), Size = new Size(160, 30), Font = new Font("Segoe UI", 10), BackColor = ColorInputBg, ForeColor = Color.White, PasswordChar = '*' };

        chkTerms = new CheckBox { Text = "Tôi đồng ý với Quy định & Điều khoản đặt vé rạp phim.", Font = new Font("Segoe UI", 8.5f), ForeColor = ColorTextMuted, Location = new Point(15, 230), Size = new Size(350, 40), Checked = true };

        btnPayNow = new Button
        {
            Text = "🔒 THANH TOÁN NGAY",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.Black,
            BackColor = ColorGold,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(350, 48),
            Location = new Point(15, 290),
            Cursor = Cursors.Hand
        };
        btnPayNow.FlatAppearance.BorderSize = 0;
        btnPayNow.Click += BtnPayNow_Click;

        pnlCardForm.Controls.AddRange(new Control[] { lblName, txtCardName, lblCardNum, txtCardNumber, lblExp, txtExpiry, lblCVV, txtCVV, chkTerms, btnPayNow });

        pnlCenterPayment.Controls.AddRange(new Control[] { lblPaymentHeader, flpMethods, pnlCardForm });

        // -------------------------------------------------------------
        // 4. CỘT PHẢI - THẺ XEM TRƯỚC VÉ (TICKET PREVIEW CARD)
        // -------------------------------------------------------------
        pnlRightTicketPreview = new Panel
        {
            Location = new Point(820, 75),
            Size = new Size(340, 640),
            BackColor = ColorCard,
            Padding = new Padding(15)
        };

        Label lblPreviewHeader = new Label { Text = "Xem Trước Vé Điện Tử", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.White, Location = new Point(15, 15), AutoSize = true };

        // Vé xem phim dạng Thẻ Vé Vàng (Yellow Ticket Pass)
        Panel pnlTicketPass = new Panel
        {
            Location = new Point(15, 50),
            Size = new Size(310, 560),
            BackColor = Color.FromArgb(245, 230, 180), // Vàng Kem Vintage
            Padding = new Padding(15)
        };

        picPreviewPoster = new PictureBox { Location = new Point(15, 15), Size = new Size(280, 260), SizeMode = PictureBoxSizeMode.StretchImage, BackColor = Color.FromArgb(40, 40, 50) };
        lblPreviewMovieTitle = new Label { Text = "TÊN PHIM", Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = Color.Black, Location = new Point(15, 285), Size = new Size(280, 30), AutoEllipsis = true };

        lblPreviewDateTime = new Label { Text = "Ngày: --/--/---- | Giờ: --:--", Font = new Font("Segoe UI", 9.5f), ForeColor = Color.FromArgb(60, 60, 60), Location = new Point(15, 320), Size = new Size(280, 22) };
        lblPreviewVenueSeats = new Label { Text = "Ghế: Chưa chọn", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.Black, Location = new Point(15, 345), Size = new Size(280, 25) };
        lblPreviewPrice = new Label { Text = "Tổng: 0 đ", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.DarkRed, Location = new Point(15, 375), Size = new Size(280, 30) };

        // Mã vạch Barcode mô phỏng ở đáy vé
        Panel pnlBarcodeMock = new Panel { Location = new Point(15, 420), Size = new Size(280, 65), BackColor = Color.White };
        Label lblBarcodeText = new Label { Text = "||||||||||||||||||||||||||||||||||||||\n*CINEMA-2026-TICKET*", Font = new Font("Libre Barcode 128", 11, FontStyle.Bold), ForeColor = Color.Black, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
        pnlBarcodeMock.Controls.Add(lblBarcodeText);

        pnlTicketPass.Controls.AddRange(new Control[] { picPreviewPoster, lblPreviewMovieTitle, lblPreviewDateTime, lblPreviewVenueSeats, lblPreviewPrice, pnlBarcodeMock });

        pnlRightTicketPreview.Controls.AddRange(new Control[] { lblPreviewHeader, pnlTicketPass });

        // Add 3 cột vào Main Controls
        this.Controls.Add(pnlRightTicketPreview);
        this.Controls.Add(pnlCenterPayment);
        this.Controls.Add(pnlLeftSummary);
        this.Controls.Add(pnlHeader);
    }

    private Button CreateMethodButton(string text, bool isActive)
    {
        Button btn = new Button
        {
            Text = text,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = isActive ? Color.Black : Color.White,
            BackColor = isActive ? ColorGold : ColorInputBg,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(180, 36),
            Margin = new Padding(0, 0, 10, 0),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private Label AddSummaryRow(Panel parent, string labelText, string valueText, ref int yPos, Color? valColor = null)
    {
        Label lblTitle = new Label { Text = labelText, Font = new Font("Segoe UI", 9), ForeColor = ColorTextMuted, Location = new Point(15, yPos), AutoSize = true };
        Label lblVal = new Label { Text = valueText, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = valColor ?? Color.White, Location = new Point(180, yPos), Size = new Size(145, 20), TextAlign = ContentAlignment.MiddleRight };

        parent.Controls.Add(lblTitle);
        parent.Controls.Add(lblVal);
        yPos += 28;
        return lblVal;
    }

    private void CalculateTotal()
    {
        totalAmount = (ticketSubTotal + foodSubTotal) - discountAmount;
        if (totalAmount < 0) totalAmount = 0m;
    }

    private void UpdateUIWithData()
    {
        int seatCount = SelectedSeats.Count;
        decimal basePricePerSeat = seatCount > 0 ? ticketSubTotal / seatCount : 0m;

        lblTicketPriceInfo.Text = $"{basePricePerSeat.ToString("N0", new CultureInfo("vi-VN"))} đ";
        lblQtyInfo.Text = $"{seatCount} vé";
        lblTicketSubTotal.Text = $"{ticketSubTotal.ToString("N0", new CultureInfo("vi-VN"))} đ";
        lblFoodSubTotal.Text = $"{foodSubTotal.ToString("N0", new CultureInfo("vi-VN"))} đ";
        lblDiscountVal.Text = $"-{discountAmount.ToString("N0", new CultureInfo("vi-VN"))} đ";
        lblFinalTotal.Text = $"{totalAmount.ToString("N0", new CultureInfo("vi-VN"))} đ";

        // Update Ticket Preview (Cột Phải)
        if (MovieData != null)
        {
            lblPreviewMovieTitle.Text = MovieData.Title;
            if (!string.IsNullOrEmpty(MovieData.PosterUrl) && System.IO.File.Exists(MovieData.PosterUrl))
            {
                try { picPreviewPoster.Image = Image.FromFile(MovieData.PosterUrl); } catch { }
            }
        }

        if (ShowtimeData != null)
        {
            lblPreviewDateTime.Text = $"Ngày: {ShowtimeData.StartTime:dd/MM/yyyy} | Giờ: {ShowtimeData.StartTime:HH:mm}";
        }

        lblPreviewVenueSeats.Text = $"Ghế: {(SelectedSeats.Count > 0 ? string.Join(", ", SelectedSeats) : "Chưa chọn")}";
        lblPreviewPrice.Text = $"Tổng tiền: {totalAmount.ToString("N0", new CultureInfo("vi-VN"))} đ";
    }

    private void BtnApplyCoupon_Click(object? sender, EventArgs e)
    {
        string code = txtCoupon.Text.Trim().ToUpper();
        if (code == "MYTICKET20" || code == "CINEMA2026")
        {
            discountAmount = 20000m; // Giảm 20.000đ
            lblCouponMsg.ForeColor = Color.LightGreen;
            lblCouponMsg.Text = "✓ Áp dụng mã giảm giá 20.000đ thành công!";
        }
        else if (string.IsNullOrEmpty(code))
        {
            discountAmount = 0m;
            lblCouponMsg.Text = "";
        }
        else
        {
            discountAmount = 0m;
            lblCouponMsg.ForeColor = Color.OrangeRed;
            lblCouponMsg.Text = "✕ Mã giảm giá không hợp lệ hoặc đã hết hạn.";
        }

        CalculateTotal();
        UpdateUIWithData();
    }

    private void BtnPayNow_Click(object? sender, EventArgs e)
    {
        if (!chkTerms.Checked)
        {
            MessageBox.Show("Vui lòng tích chọn đồng ý với Điều khoản sử dụng trước khi thanh toán!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Tạo mã đơn hàng duy nhất (Booking Code)
        string bookingCode = $"BK{DateTime.Now:yyyyMMddHHmmss}";

        var result = new BookingResultEventArgs
        {
            BookingCode = bookingCode,
            Movie = MovieData,
            Showtime = ShowtimeData,
            Seats = SelectedSeats,
            FoodCart = FoodCart,
            TotalAmount = totalAmount,
            BookingTime = DateTime.Now
        };

        // Gửi sự kiện thanh toán thành công sang TicketUC (Màn 8)
        OnPaymentCompleted?.Invoke(this, result);
    }
}

// Class chứa kết quả đơn hàng sau khi thanh toán
public class BookingResultEventArgs : EventArgs
{
    public string BookingCode { get; set; } = string.Empty;
    public Movie? Movie { get; set; }
    public Showtime? Showtime { get; set; }
    public List<string> Seats { get; set; } = new List<string>();
    public Dictionary<int, (string Name, decimal Price, int Qty, string ImgPath)> FoodCart { get; set; } = new Dictionary<int, (string, decimal, int, string)>();
    public decimal TotalAmount { get; set; }
    public DateTime BookingTime { get; set; }

    // Kênh bán: "Online" (khách tự đặt) hoặc "POS" (bán tại quầy)
    public string Channel { get; set; } = "Online";
    public string PaymentMethod { get; set; } = "Thẻ / Ví điện tử";
    public string CustomerName { get; set; } = string.Empty;
}