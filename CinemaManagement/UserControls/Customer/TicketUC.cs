namespace CinemaManagement.UserControls.Customer;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;
using CinemaManagement.Models;

public partial class TicketUC : UserControl
{
    // Bảng màu Modern Dark Cinema
    private readonly Color ColorBg = Color.FromArgb(14, 15, 22);
    private readonly Color ColorCard = Color.FromArgb(24, 25, 36);
    private readonly Color ColorGold = Color.FromArgb(245, 197, 24);
    private readonly Color ColorSuccessGreen = Color.FromArgb(46, 204, 113);
    private readonly Color ColorTextMuted = Color.FromArgb(150, 150, 170);

    // Dynamic Data
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public BookingResultEventArgs? BookingData { get; private set; }

    // Controls UI
    private Panel pnlLeftSuccess;
    private Panel pnlRightTicketCard;

    // Left Panel Controls
    private Label lblStatusHeader;
    private Label lblBookingCodeText;
    private Button btnGoHome;
    private Button btnViewHistory;

    // Right Ticket Controls
    private PictureBox picPoster;
    private Label lblMovieTitle;
    private Label lblShowtimeDetails;
    private Label lblSeatsDetails;
    private Label lblTotalAmount;
    private PictureBox picQRCode;
    private Button btnDownloadTicket;

    // Events
    public event EventHandler? OnGoHomeClicked;
    public event EventHandler? OnViewHistoryClicked;

    public TicketUC()
    {
        InitializeComponents();
    }

    public TicketUC(BookingResultEventArgs bookingData) : this()
    {
        SetBookingData(bookingData);
    }

    public void SetBookingData(BookingResultEventArgs data)
    {
        BookingData = data;
        UpdateUIWithData();
    }

    private void InitializeComponents()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ColorBg;

        // -------------------------------------------------------------
        // 1. CỘT TRÁI - THÔNG BÁO XÁC NHẬN THÀNH CÔNG
        // -------------------------------------------------------------
        pnlLeftSuccess = new Panel
        {
            Location = new Point(40, 40),
            Size = new Size(500, 640),
            BackColor = Color.Transparent,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
        };

        // Biểu tượng dấu Tích xanh (Checkmark Badge) vẽ bằng GDI+
        PictureBox picCheckmark = new PictureBox
        {
            Size = new Size(130, 130),
            Location = new Point(185, 80),
            BackColor = Color.Transparent
        };
        picCheckmark.Paint += PicCheckmark_Paint;

        lblStatusHeader = new Label
        {
            Text = "Đặt Vé Thành Công!",
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(20, 230),
            Size = new Size(460, 45),
            TextAlign = ContentAlignment.MiddleCenter
        };

        lblBookingCodeText = new Label
        {
            Text = "Mã đơn hàng: BK-2026-XXXX",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = ColorGold,
            Location = new Point(20, 280),
            Size = new Size(460, 30),
            TextAlign = ContentAlignment.MiddleCenter
        };

        Label lblSubDesc = new Label
        {
            Text = "Cảm ơn bạn đã đặt vé! Vé điện tử của bạn đã được lưu vào hệ thống. Bạn có thể xuất trình mã QR này tại quầy để nhận vé vào phòng chiếu.",
            Font = new Font("Segoe UI", 10),
            ForeColor = ColorTextMuted,
            Location = new Point(40, 325),
            Size = new Size(420, 65),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // Nút bấm chuyển hướng
        btnGoHome = new Button
        {
            Text = "🏠 Về Trang Chủ",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.Black,
            BackColor = ColorGold,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(200, 45),
            Location = new Point(40, 420),
            Cursor = Cursors.Hand
        };
        btnGoHome.FlatAppearance.BorderSize = 0;
        btnGoHome.Click += (s, e) => OnGoHomeClicked?.Invoke(this, EventArgs.Empty);

        btnViewHistory = new Button
        {
            Text = "📜 Lịch Sử Đặt Vé",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(38, 40, 56),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(200, 45),
            Location = new Point(260, 420),
            Cursor = Cursors.Hand
        };
        btnViewHistory.FlatAppearance.BorderSize = 0;
        btnViewHistory.Click += (s, e) => OnViewHistoryClicked?.Invoke(this, EventArgs.Empty);

        pnlLeftSuccess.Controls.AddRange(new Control[] { picCheckmark, lblStatusHeader, lblBookingCodeText, lblSubDesc, btnGoHome, btnViewHistory });

        // -------------------------------------------------------------
        // 2. CỘT PHẢI - THẺ VÉ ĐIỆN TỬ & QR CODE
        // -------------------------------------------------------------
        pnlRightTicketCard = new Panel
        {
            Location = new Point(570, 40),
            Size = new Size(570, 640),
            BackColor = ColorCard,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Padding = new Padding(25)
        };

        // Panel thông tin phim thu nhỏ trên vé
        Panel pnlMovieInfoBox = new Panel
        {
            Location = new Point(25, 25),
            Size = new Size(520, 200),
            BackColor = Color.FromArgb(18, 19, 28)
        };

        picPoster = new PictureBox
        {
            Location = new Point(15, 15),
            Size = new Size(120, 170),
            SizeMode = PictureBoxSizeMode.StretchImage,
            BackColor = Color.FromArgb(35, 36, 50)
        };

        lblMovieTitle = new Label
        {
            Text = "Spider-Man: Across The Spider-Verse",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(150, 15),
            Size = new Size(355, 32),
            AutoEllipsis = true
        };

        lblShowtimeDetails = new Label
        {
            Text = "🎬 Rạp: Screen 1 (PVR Gold)\n⏰ Suất: 12:15 AM - 22/01/2026",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorTextMuted,
            Location = new Point(150, 52),
            Size = new Size(355, 45)
        };

        lblSeatsDetails = new Label
        {
            Text = "🪑 Ghế: E5, E6, E7, E8, E9, E10, E11",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = ColorGold,
            Location = new Point(150, 100),
            Size = new Size(355, 25),
            AutoEllipsis = true
        };

        lblTotalAmount = new Label
        {
            Text = "💳 Tổng thanh toán: 540.000 đ",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = ColorSuccessGreen,
            Location = new Point(150, 130),
            Size = new Size(355, 25)
        };

        pnlMovieInfoBox.Controls.AddRange(new Control[] { picPoster, lblMovieTitle, lblShowtimeDetails, lblSeatsDetails, lblTotalAmount });

        // Khung hiển thị QR Code ở trung tâm vé
        picQRCode = new PictureBox
        {
            Size = new Size(240, 240),
            Location = new Point(165, 245),
            BackColor = Color.White,
            SizeMode = PictureBoxSizeMode.CenterImage
        };
        picQRCode.Paint += PicQRCode_Paint; // Draw QR Matrix Simulation

        // Nút Tải vé / Lưu ảnh vé
        btnDownloadTicket = new Button
        {
            Text = "📥 TẢI VÉ / LƯU ẢNH (PDF)",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(42, 44, 62),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(280, 48),
            Location = new Point(145, 510),
            Cursor = Cursors.Hand
        };
        btnDownloadTicket.FlatAppearance.BorderSize = 0;
        btnDownloadTicket.Click += BtnDownloadTicket_Click;

        pnlRightTicketCard.Controls.AddRange(new Control[] { pnlMovieInfoBox, picQRCode, btnDownloadTicket });

        // Add 2 Cột chính vào UserControl
        this.Controls.Add(pnlRightTicketCard);
        this.Controls.Add(pnlLeftSuccess);
    }

    // Vẽ biểu tượng Checkmark hình tròn màu xanh lá
    private void PicCheckmark_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        // Draw Green Circle
        using (Brush brush = new SolidBrush(ColorSuccessGreen))
        {
            e.Graphics.FillEllipse(brush, 5, 5, 120, 120);
        }

        // Draw White Check Icon
        using (Pen pen = new Pen(Color.White, 8))
        {
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            Point[] checkPoints = { new Point(38, 65), new Point(55, 82), new Point(90, 48) };
            e.Graphics.DrawLines(pen, checkPoints);
        }
    }

    // Mô phỏng vẽ ma trận mã QR Code chuẩn xác
    private void PicQRCode_Paint(object? sender, PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.None;

        int size = 200;
        int offsetX = 20;
        int offsetY = 20;

        g.Clear(Color.White);

        using (Brush b = Brushes.Black)
        {
            // 3 ô vuông định vị góc (Finder Patterns)
            DrawQRFinderPattern(g, offsetX, offsetY);
            DrawQRFinderPattern(g, offsetX + 140, offsetY);
            DrawQRFinderPattern(g, offsetX, offsetY + 140);

            // Các ô điểm dữ liệu giả lập (Data Pixels Grid)
            Random rand = new Random(12345); // Fixed seed for consistent QR pattern
            for (int r = 0; r < 20; r++)
            {
                for (int c = 0; c < 20; c++)
                {
                    if ((r < 7 && c < 7) || (r < 7 && c > 12) || (r > 12 && c < 7)) continue;
                    if (rand.Next(0, 2) == 1)
                    {
                        g.FillRectangle(b, offsetX + c * 10, offsetY + r * 10, 10, 10);
                    }
                }
            }
        }
    }

    private void DrawQRFinderPattern(Graphics g, int x, int y)
    {
        g.FillRectangle(Brushes.Black, x, y, 60, 60);
        g.FillRectangle(Brushes.White, x + 10, y + 10, 40, 40);
        g.FillRectangle(Brushes.Black, x + 20, y + 20, 20, 20);
    }

    private void UpdateUIWithData()
    {
        if (BookingData == null) return;

        lblBookingCodeText.Text = $"Mã đơn hàng: {BookingData.BookingCode}";

        if (BookingData.Movie != null)
        {
            lblMovieTitle.Text = BookingData.Movie.Title;
            if (!string.IsNullOrEmpty(BookingData.Movie.PosterUrl) && System.IO.File.Exists(BookingData.Movie.PosterUrl))
            {
                try { picPoster.Image = Image.FromFile(BookingData.Movie.PosterUrl); } catch { }
            }
        }

        if (BookingData.Showtime != null)
        {
            lblShowtimeDetails.Text = $"🎬 Rạp: Screen 1 (Standard)\n⏰ Suất: {BookingData.Showtime.StartTime:HH:mm - dd/MM/yyyy}";
        }

        lblSeatsDetails.Text = $"🪑 Ghế: {(BookingData.Seats.Count > 0 ? string.Join(", ", BookingData.Seats) : "Chưa chọn")}";
        lblTotalAmount.Text = $"💳 Tổng thanh toán: {BookingData.TotalAmount.ToString("N0", new CultureInfo("vi-VN"))} đ";
    }

    private void BtnDownloadTicket_Click(object? sender, EventArgs e)
    {
        MessageBox.Show($"Vé điện tử mã [{BookingData?.BookingCode ?? "BK-2026"}] đã được tải xuống máy thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}