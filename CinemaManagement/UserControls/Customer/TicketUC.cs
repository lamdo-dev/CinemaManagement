
namespace CinemaManagement.UserControls.Customer;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CinemaManagement.Models;
using CinemaManagement.Utils;

public partial class TicketUC : UserControl
{
    // =========================================================
    // 1. THEME
    // =========================================================

    private readonly Color ColorBg = Color.FromArgb(14, 15, 22);
    private readonly Color ColorCard = Color.FromArgb(24, 25, 36);
    private readonly Color ColorGold = Color.FromArgb(245, 197, 24);
    private readonly Color ColorSuccessGreen = Color.FromArgb(46, 204, 113);
    private readonly Color ColorTextMuted = Color.FromArgb(150, 150, 170);

    // =========================================================
    // 2. BOOKING DATA
    // =========================================================

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public BookingResultEventArgs? BookingData { get; private set; }

    // =========================================================
    // 3. UI CONTROLS
    // =========================================================

    private Panel pnlLeftSuccess = null!;
    private Panel pnlRightTicketCard = null!;

    private Label lblStatusHeader = null!;
    private Label lblBookingCodeText = null!;

    private Button btnGoHome = null!;
    private Button btnViewHistory = null!;

    private PictureBox picPoster = null!;
    private Label lblMovieTitle = null!;
    private Label lblShowtimeDetails = null!;
    private Label lblSeatsDetails = null!;
    private Label lblFoodDetails = null!;
    private Label lblTotalAmount = null!;

    private PictureBox picQRCode = null!;
    private Button btnDownloadTicket = null!;

    // =========================================================
    // 4. EVENTS
    // =========================================================

    public event EventHandler? OnGoHomeClicked;
    public event EventHandler? OnViewHistoryClicked;

    public event EventHandler<BookingResultEventArgs?>?
        OnDownloadTicketClicked;

    // =========================================================
    // 5. CONSTRUCTOR
    // =========================================================

    public TicketUC()
    {
        InitializeComponents();

        // Chỉ sử dụng LatestBooking khi không truyền dữ liệu
        if (UserSession.LatestBooking != null)
        {
            SetBookingData(UserSession.LatestBooking);
        }
    }

    public TicketUC(BookingResultEventArgs bookingData) : this()
    {
        SetBookingData(bookingData);
    }

    // =========================================================
    // 6. SET BOOKING DATA
    // =========================================================

    public void SetBookingData(BookingResultEventArgs data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        BookingData = data;

        UpdateUIWithData();
    }

    // =========================================================
    // 7. INITIALIZE UI
    // =========================================================

    private void InitializeComponents()
    {
        Dock = DockStyle.Fill;
        BackColor = ColorBg;

        // -----------------------------------------------------
        // LEFT PANEL
        // -----------------------------------------------------

        pnlLeftSuccess = new Panel
        {
            Location = new Point(40, 40),
            Size = new Size(500, 640),
            BackColor = Color.Transparent,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                     AnchorStyles.Left
        };

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
            Text = "Mã đơn hàng: Chưa có",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = ColorGold,
            Location = new Point(20, 280),
            Size = new Size(460, 30),
            TextAlign = ContentAlignment.MiddleCenter
        };

        Label lblSubDesc = new Label
        {
            Text =
                "Cảm ơn bạn đã đặt vé! Thông tin vé điện tử " +
                "đã được tạo. Bạn có thể xem lại mã đặt vé " +
                "và thông tin suất chiếu tại đây.",
            Font = new Font("Segoe UI", 10),
            ForeColor = ColorTextMuted,
            Location = new Point(40, 325),
            Size = new Size(420, 65),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // Button Home
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

        btnGoHome.Click += (s, e) =>
        {
            OnGoHomeClicked?.Invoke(this, EventArgs.Empty);
        };

        // Button History
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
        btnViewHistory.Click += BtnViewHistory_Click;

        pnlLeftSuccess.Controls.AddRange(new Control[]
        {
            picCheckmark,
            lblStatusHeader,
            lblBookingCodeText,
            lblSubDesc,
            btnGoHome,
            btnViewHistory
        });

        // -----------------------------------------------------
        // RIGHT PANEL
        // -----------------------------------------------------

        pnlRightTicketCard = new Panel
        {
            Location = new Point(570, 40),
            Size = new Size(570, 640),
            BackColor = ColorCard,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                     AnchorStyles.Left | AnchorStyles.Right,
            Padding = new Padding(25)
        };

        // Movie information
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

        picPoster.Paint += (s, e) =>
        {
            if (picPoster.Image != null)
                return;

            using var font = new Font("Segoe UI Emoji", 28f);
            using var brush = new SolidBrush(
                Color.FromArgb(140, 140, 165));

            var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            e.Graphics.DrawString(
                "🎬",
                font,
                brush,
                picPoster.ClientRectangle,
                format);
        };

        lblMovieTitle = new Label
        {
            Text = "Thông tin phim",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(150, 12),
            Size = new Size(355, 30),
            AutoEllipsis = true
        };

        lblShowtimeDetails = new Label
        {
            Text = "Chưa có thông tin suất chiếu",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorTextMuted,
            Location = new Point(150, 45),
            Size = new Size(355, 42)
        };

        lblSeatsDetails = new Label
        {
            Text = "🪑 Ghế: Chưa chọn",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = ColorGold,
            Location = new Point(150, 92),
            Size = new Size(355, 22),
            AutoEllipsis = true
        };

        lblFoodDetails = new Label
        {
            Text = "🍿 Bắp nước: Không kèm bắp nước",
            Font = new Font("Segoe UI", 9f),
            ForeColor = ColorTextMuted,
            Location = new Point(150, 118),
            Size = new Size(355, 22),
            AutoEllipsis = true
        };

        lblTotalAmount = new Label
        {
            Text = "💳 Tổng thanh toán: 0 đ",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = ColorSuccessGreen,
            Location = new Point(150, 148),
            Size = new Size(355, 25)
        };

        pnlMovieInfoBox.Controls.AddRange(new Control[]
        {
            picPoster,
            lblMovieTitle,
            lblShowtimeDetails,
            lblSeatsDetails,
            lblFoodDetails,
            lblTotalAmount
        });

        // QR Code
        picQRCode = new PictureBox
        {
            Size = new Size(240, 240),
            Location = new Point(165, 245),
            BackColor = Color.White,
            SizeMode = PictureBoxSizeMode.CenterImage
        };

        picQRCode.Paint += PicQRCode_Paint;

        // Download ticket
        btnDownloadTicket = new Button
        {
            Text = "📥 XUẤT THÔNG TIN VÉ",
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

        pnlRightTicketCard.Controls.AddRange(new Control[]
        {
            pnlMovieInfoBox,
            picQRCode,
            btnDownloadTicket
        });

        // -----------------------------------------------------
        // ADD TO USER CONTROL
        // -----------------------------------------------------

        Controls.Add(pnlRightTicketCard);
        Controls.Add(pnlLeftSuccess);

        Resize += TicketUC_Resize;
    }

    // =========================================================
    // 8. RESPONSIVE LAYOUT
    // =========================================================

    private void TicketUC_Resize(object? sender, EventArgs e)
    {
        if (pnlLeftSuccess == null ||
            pnlRightTicketCard == null)
            return;

        int margin = 30;
        int gap = 30;

        int availableWidth = ClientSize.Width - margin * 2 - gap;

        if (availableWidth <= 0)
            return;

        int leftWidth = Math.Min(500, availableWidth / 2);
        int rightWidth = availableWidth - leftWidth;

        if (rightWidth < 400)
        {
            pnlLeftSuccess.Location = new Point(margin, 20);
            pnlLeftSuccess.Size = new Size(
                ClientSize.Width - margin * 2, 500);

            pnlRightTicketCard.Location = new Point(
                margin, 530);

            pnlRightTicketCard.Size = new Size(
                ClientSize.Width - margin * 2,
                Math.Max(600, ClientSize.Height - 550));
        }
        else
        {
            pnlLeftSuccess.Location = new Point(margin, 40);
            pnlLeftSuccess.Size = new Size(leftWidth, 640);

            pnlRightTicketCard.Location = new Point(
                margin + leftWidth + gap, 40);

            pnlRightTicketCard.Size = new Size(
                rightWidth, Math.Max(640, ClientSize.Height - 80));
        }

        // Center checkmark and buttons
        Control? checkmark = pnlLeftSuccess.Controls
            .OfType<PictureBox>()
            .FirstOrDefault();

        if (checkmark != null)
        {
            checkmark.Location = new Point(
                (pnlLeftSuccess.Width - checkmark.Width) / 2,
                80);
        }

        lblStatusHeader.Width = pnlLeftSuccess.Width - 40;
        lblStatusHeader.Location = new Point(20, 230);

        lblBookingCodeText.Width = pnlLeftSuccess.Width - 40;
        lblBookingCodeText.Location = new Point(20, 280);

        int buttonWidth = Math.Max(
            140, (pnlLeftSuccess.Width - 80) / 2);

        btnGoHome.Size = new Size(buttonWidth, 45);
        btnViewHistory.Size = new Size(buttonWidth, 45);

        btnGoHome.Location = new Point(20, 420);
        btnViewHistory.Location = new Point(
            40 + buttonWidth, 420);

        // Center QR
        if (picQRCode != null && pnlRightTicketCard != null)
        {
            picQRCode.Location = new Point(
                Math.Max(20, (pnlRightTicketCard.ClientSize.Width -
                               picQRCode.Width) / 2),
                245);
        }

        if (btnDownloadTicket != null && pnlRightTicketCard != null)
        {
            btnDownloadTicket.Location = new Point(
                Math.Max(20, (pnlRightTicketCard.ClientSize.Width -
                               btnDownloadTicket.Width) / 2),
                510);
        }
    }

    // =========================================================
    // 9. DRAW CHECKMARK
    // =========================================================

    private void PicCheckmark_Paint(
        object? sender,
        PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using (Brush brush = new SolidBrush(ColorSuccessGreen))
        {
            e.Graphics.FillEllipse(
                brush, 5, 5, 120, 120);
        }

        using (Pen pen = new Pen(Color.White, 8))
        {
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;

            Point[] points =
            {
                new Point(38, 65),
                new Point(55, 82),
                new Point(90, 48)
            };

            e.Graphics.DrawLines(pen, points);
        }
    }

    // =========================================================
    // 10. DRAW SIMULATED QR CODE
    // =========================================================

    private void PicQRCode_Paint(
        object? sender,
        PaintEventArgs e)
    {
        Graphics g = e.Graphics;

        g.SmoothingMode = SmoothingMode.None;
        g.Clear(Color.White);

        if (BookingData == null)
        {
            using var font = new Font("Segoe UI", 10);
            using var brush = new SolidBrush(Color.Gray);

            g.DrawString(
                "Chưa có mã đặt vé",
                font,
                brush,
                new PointF(55, 110));

            return;
        }

        int offsetX = 20;
        int offsetY = 20;

        using Brush blackBrush = new SolidBrush(Color.Black);

        DrawQRFinderPattern(g, offsetX, offsetY);
        DrawQRFinderPattern(g, offsetX + 140, offsetY);
        DrawQRFinderPattern(g, offsetX, offsetY + 140);

        // Đây chỉ là ma trận mô phỏng, không phải QR chuẩn.
        int seed = StringComparer.Ordinal.GetHashCode(
            BookingData.BookingCode ?? "BOOKING");

        Random random = new Random(seed);

        for (int r = 0; r < 20; r++)
        {
            for (int c = 0; c < 20; c++)
            {
                if ((r < 7 && c < 7) ||
                    (r < 7 && c > 12) ||
                    (r > 12 && c < 7))
                    continue;

                if (random.Next(0, 2) == 1)
                {
                    g.FillRectangle(
                        blackBrush,
                        offsetX + c * 10,
                        offsetY + r * 10,
                        10,
                        10);
                }
            }
        }
    }

    private void DrawQRFinderPattern(
        Graphics g,
        int x,
        int y)
    {
        g.FillRectangle(Brushes.Black, x, y, 60, 60);
        g.FillRectangle(Brushes.White, x + 10, y + 10, 40, 40);
        g.FillRectangle(Brushes.Black, x + 20, y + 20, 20, 20);
    }

    // =========================================================
    // 11. UPDATE TICKET INFORMATION
    // =========================================================

    private void UpdateUIWithData()
    {
        if (BookingData == null)
            return;

        lblBookingCodeText.Text =
            $"Mã đơn hàng: {BookingData.BookingCode}";

        lblMovieTitle.Text =
            BookingData.Movie?.Title ?? "Không có thông tin phim";

        // Release old poster
        if (picPoster.Image != null)
        {
            Image oldImage = picPoster.Image;
            picPoster.Image = null;
            oldImage.Dispose();
        }

        // Load poster safely
        string? posterPath = BookingData.Movie?.PosterUrl;

        if (!string.IsNullOrWhiteSpace(posterPath) &&
            File.Exists(posterPath))
        {
            try
            {
                using var stream = new FileStream(
                    posterPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite);

                using var image = Image.FromStream(stream);

                picPoster.Image = new Bitmap(image);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Không thể tải poster: {ex.Message}");
            }
        }

        // Showtime
        if (BookingData.Showtime != null)
        {
            lblShowtimeDetails.Text =
                $"🎬 Phòng chiếu: {BookingData.Showtime.RoomId}\n" +
                $"⏰ Suất: {BookingData.Showtime.StartTime:HH:mm - dd/MM/yyyy}";
        }
        else
        {
            lblShowtimeDetails.Text =
                "Chưa có thông tin suất chiếu";
        }

        // Seats
        lblSeatsDetails.Text =
            $"🪑 Ghế: {(BookingData.Seats != null &&
                       BookingData.Seats.Count > 0
                ? string.Join(", ", BookingData.Seats)
                : "Chưa chọn")}";

        // Food
        if (BookingData.FoodCart != null &&
            BookingData.FoodCart.Count > 0)
        {
            lblFoodDetails.Text =
                "🍿 Bắp nước: " +
                string.Join(", ",
                    BookingData.FoodCart
                        .Where(x => x.Value.Qty > 0)
                        .Select(x =>
                            $"{x.Value.Name} x{x.Value.Qty}"));
        }
        else
        {
            lblFoodDetails.Text =
                "🍿 Bắp nước: Không kèm bắp nước";
        }

        // Total
        lblTotalAmount.Text =
            $"💳 Tổng thanh toán: " +
            $"{BookingData.TotalAmount.ToString(
                "N0", new CultureInfo("vi-VN"))} đ";

        picQRCode.Invalidate();
    }

    // =========================================================
    // 12. VIEW BOOKING HISTORY
    // =========================================================

    private void BtnViewHistory_Click(
        object? sender,
        EventArgs e)
    {
        OnViewHistoryClicked?.Invoke(this, EventArgs.Empty);

        if (UserSession.BookingHistory.Count == 0)
        {
            MessageBox.Show(
                "Bạn chưa có đơn đặt vé nào trong phiên làm việc này.",
                "Lịch sử đặt vé",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        StringBuilder sb = new StringBuilder();

        sb.AppendLine(
            $"LỊCH SỬ ĐẶT VÉ ({UserSession.BookingHistory.Count} đơn):");

        sb.AppendLine();

        for (int i = UserSession.BookingHistory.Count - 1;
             i >= 0;
             i--)
        {
            var booking = UserSession.BookingHistory[i];

            sb.AppendLine(
                $"• [{booking.BookingCode}] " +
                $"{booking.Movie?.Title ?? "Phim"}");

            sb.AppendLine(
                $"  Suất: {booking.Showtime?.StartTime:HH:mm dd/MM}");

            sb.AppendLine(
                $"  Ghế: {string.Join(", ", booking.Seats)}");

            sb.AppendLine(
                $"  Tổng: {booking.TotalAmount:N0} đ");

            sb.AppendLine();
        }

        MessageBox.Show(
            sb.ToString(),
            "Lịch sử đặt vé của tôi",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    // =========================================================
    // 13. DOWNLOAD / EXPORT TICKET
    // =========================================================

    private void BtnDownloadTicket_Click(
        object? sender,
        EventArgs e)
    {
        if (BookingData == null)
        {
            MessageBox.Show(
                "Không có dữ liệu vé để xuất.",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        OnDownloadTicketClicked?.Invoke(this, BookingData);

        MessageBox.Show(
            "Thông tin vé đã sẵn sàng.\n\n" +
            $"Mã đặt vé: {BookingData.BookingCode}\n" +
            $"Phim: {BookingData.Movie?.Title ?? "Không có"}\n" +
            $"{lblSeatsDetails.Text}\n" +
            $"{lblTotalAmount.Text}\n\n" +
            "Chức năng xuất vé thành ảnh hoặc PDF " +
            "sẽ được triển khai riêng.",
            "Thông tin vé",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
    

   

    
}
