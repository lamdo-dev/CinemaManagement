namespace CinemaManagement.UserControls.Customer;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Models;
using CinemaManagement.UserControls.Common;
using CinemaManagement.Utils;

public partial class SeatSelectionUC : UserControl
{
    private readonly Color ColorBg = Color.FromArgb(14, 15, 22);
    private readonly Color ColorCard = Color.FromArgb(24, 25, 36);
    private readonly Color ColorGold = Color.FromArgb(195, 155, 80);
    private readonly Color ColorTextMuted = Color.FromArgb(150, 150, 170);

    private Panel pnlHeader = null!;
    private Button btnBack = null!;
    private Panel pnlLeftLegend = null!;
    private Panel pnlBottomBar = null!;
    private Label lblContextInfo = null!;

    private SeatMatrixUC seatMatrix = null!;
    private Label lblSelectedSeats = null!;
    private Label lblTotalPrice = null!;
    private Button btnContinue = null!;

    private decimal currentTotal = 0m;

    public Movie? MovieData { get; private set; }
    public Showtime? ShowtimeData { get; private set; }

    // Sự kiện chuyển tiếp sang trang chọn bắp nước (FoodSelectionUC)
    public event EventHandler<(List<string> seats, decimal price)>? OnContinueToFood;
    public event EventHandler? OnBackClicked;

    public SeatSelectionUC()
    {
        InitializeComponents();
    }

    public SeatSelectionUC(Movie? movie, Showtime? showtime) : this()
    {
        SetBookingContext(movie, showtime);
    }

    public void SetBookingContext(Movie? movie, Showtime? showtime)
    {
        MovieData = movie ?? new Movie { Id = 1, Title = "Spider-Man: Across The Spider-Verse", Genre = "Action", DurationMinutes = 140 };
        ShowtimeData = showtime ?? new Showtime { Id = 1, MovieId = MovieData.Id, RoomId = 1, StartTime = DateTime.Today.AddHours(19).AddMinutes(30), BasePrice = 90000m };

        if (lblContextInfo != null)
        {
            lblContextInfo.Text = $"🎬 {MovieData.Title}\n⏰ Suất: {ShowtimeData.StartTime:HH:mm - dd/MM/yyyy}";
        }

        var extraOccupied = UserSession.GetBookedSeats(MovieData, ShowtimeData);
        seatMatrix.GenerateSeatMatrix(10, 14, extraOccupied);
    }

    private void InitializeComponents()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ColorBg;

        // -------------------------------------------------------------
        // 1. HEADER (STEPPER BAR)
        // -------------------------------------------------------------
        pnlHeader = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = Color.FromArgb(18, 19, 28) };
        btnBack = new Button
        {
            Text = "← Quay lại",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(35, 36, 50),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(100, 32),
            Location = new Point(20, 15),
            Cursor = Cursors.Hand
        };
        btnBack.FlatAppearance.BorderSize = 0;
        btnBack.Click += (s, e) => OnBackClicked?.Invoke(this, EventArgs.Empty);

        // Stepper: Step 2 active
        Panel pnlStepper = new Panel { Size = new Size(500, 40), Location = new Point(320, 12) };
        Label lblStep1 = new Label { Text = "① CHỌN SUẤT CHIẾU", Font = new Font("Segoe UI", 9), ForeColor = ColorTextMuted, Location = new Point(0, 10), AutoSize = true };
        Label lblDiv1 = new Label { Text = "───────", ForeColor = ColorTextMuted, Location = new Point(155, 10), AutoSize = true };
        Label lblStep2 = new Label { Text = "② CHỌN GHẾ", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = ColorGold, Location = new Point(230, 10), AutoSize = true };
        Label lblDiv2 = new Label { Text = "───────", ForeColor = ColorTextMuted, Location = new Point(335, 10), AutoSize = true };
        Label lblStep3 = new Label { Text = "③ THANH TOÁN", Font = new Font("Segoe UI", 9), ForeColor = ColorTextMuted, Location = new Point(410, 10), AutoSize = true };
        pnlStepper.Controls.AddRange(new Control[] { lblStep1, lblDiv1, lblStep2, lblDiv2, lblStep3 });

        pnlHeader.Controls.Add(btnBack);
        pnlHeader.Controls.Add(pnlStepper);

        // -------------------------------------------------------------
        // 2. CHÚ THÍCH GIÁ GHẾ BÊN TRÁI (LEFT PRICE LEGEND)
        // -------------------------------------------------------------
        pnlLeftLegend = new Panel
        {
            Dock = DockStyle.Left,
            Width = 260,
            BackColor = ColorCard,
            Padding = new Padding(15)
        };

        Label lblLegendTitle = new Label
        {
            Text = "BẢNG GIÁ VÉ",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 35
        };

        pnlLeftLegend.Controls.Add(lblLegendTitle);
        AddLegendItem(pnlLeftLegend, "Ghế Thường (Classic)", "90.000 đ", SeatMatrixUC.ColorStandard, 50);
        AddLegendItem(pnlLeftLegend, "Ghế VIP", "120.000 đ", SeatMatrixUC.ColorVIP, 120);
        AddLegendItem(pnlLeftLegend, "Ghế Premium/Plus", "110.000 đ", SeatMatrixUC.ColorPlus, 190);
        AddLegendItem(pnlLeftLegend, "Ghế Đang chọn", "Đã chọn", SeatMatrixUC.ColorSelected, 260);
        AddLegendItem(pnlLeftLegend, "Ghế Đã bán", "Không trống", SeatMatrixUC.ColorOccupied, 330);

        lblContextInfo = new Label
        {
            Text = "🎬 Suất chiếu đang chọn",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = ColorGold,
            Location = new Point(15, 405),
            Size = new Size(230, 65)
        };
        pnlLeftLegend.Controls.Add(lblContextInfo);

        // -------------------------------------------------------------
        // 3. THANH TỔNG TIỀN VÀ NÚT TIẾP TỤC BÊN DƯỚI (BOTTOM BAR)
        // -------------------------------------------------------------
        pnlBottomBar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 75,
            BackColor = Color.FromArgb(20, 21, 32),
            Padding = new Padding(20, 10, 20, 10)
        };

        lblSelectedSeats = new Label
        {
            Text = "Ghế đã chọn: Chưa chọn",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(20, 15),
            AutoSize = true
        };

        lblTotalPrice = new Label
        {
            Text = "Tổng tiền: 0 đ",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = ColorGold,
            Location = new Point(20, 40),
            AutoSize = true
        };

        btnContinue = new Button
        {
            Text = "TIẾP TỤC (BẮP NƯỚC) →",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.Black,
            BackColor = ColorGold,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(230, 45),
            Location = new Point(880, 15),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };
        btnContinue.FlatAppearance.BorderSize = 0;
        btnContinue.Click += BtnContinue_Click;

        pnlBottomBar.Controls.Add(lblSelectedSeats);
        pnlBottomBar.Controls.Add(lblTotalPrice);
        pnlBottomBar.Controls.Add(btnContinue);

        // -------------------------------------------------------------
        // 4. SƠ ĐỒ GHẾ CHÍNH (SEAT MATRIX)
        // -------------------------------------------------------------
        seatMatrix = new SeatMatrixUC
        {
            Dock = DockStyle.Fill
        };
        seatMatrix.OnSeatSelectionChanged += SeatMatrix_OnSeatSelectionChanged;

        // Add tất cả vào UserControl
        this.Controls.Add(seatMatrix);
        this.Controls.Add(pnlLeftLegend);
        this.Controls.Add(pnlHeader);
        this.Controls.Add(pnlBottomBar);
    }

    private void AddLegendItem(Panel container, string title, string price, Color color, int yPos)
    {
        Panel box = new Panel { Size = new Size(20, 20), Location = new Point(15, yPos), BackColor = color };
        Label lblT = new Label { Text = title, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.White, Location = new Point(45, yPos), AutoSize = true };
        Label lblP = new Label { Text = price, Font = new Font("Segoe UI", 8.5f), ForeColor = ColorTextMuted, Location = new Point(45, yPos + 20), AutoSize = true };

        container.Controls.Add(box);
        container.Controls.Add(lblT);
        container.Controls.Add(lblP);
    }

    private void SeatMatrix_OnSeatSelectionChanged(object? sender, List<string> selectedSeats)
    {
        if (selectedSeats.Count == 0)
        {
            lblSelectedSeats.Text = "Ghế đã chọn: Chưa chọn";
            currentTotal = 0m;
        }
        else
        {
            lblSelectedSeats.Text = $"Ghế đã chọn: {string.Join(", ", selectedSeats)}";
            currentTotal = selectedSeats.Sum(SeatMatrixUC.GetSeatPriceByCode);
        }

        lblTotalPrice.Text = $"Tổng tiền: {currentTotal.ToString("N0", new CultureInfo("vi-VN"))} đ";
    }

    private void BtnContinue_Click(object? sender, EventArgs e)
    {
        if (seatMatrix.SelectedSeatCodes.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn ít nhất 01 ghế để tiếp tục!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        OnContinueToFood?.Invoke(this, (new List<string>(seatMatrix.SelectedSeatCodes), currentTotal));
    }
}
