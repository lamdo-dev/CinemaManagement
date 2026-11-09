namespace CinemaManagement.UserControls.Customer;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using CinemaManagement.Models;

public partial class ProfileUC : UserControl
{
    // Bảng màu Modern Dark Cinema
    private readonly Color ColorBg = Color.FromArgb(14, 15, 22);
    private readonly Color ColorCard = Color.FromArgb(24, 25, 36);
    private readonly Color ColorGold = Color.FromArgb(245, 197, 24);
    private readonly Color ColorTextMuted = Color.FromArgb(150, 150, 170);
    private readonly Color ColorRowHover = Color.FromArgb(34, 36, 52);

    // Controls UI
    private Panel pnlProfileHeader;
    private Label lblCustomerName;
    private Label lblCustomerEmail;
    private Label lblMemberTag;

    private Panel pnlTableContainer;
    private FlowLayoutPanel flpOrderRows;
    private DateTimePicker dtpFilterDate;
    private Button btnFilter;

    // Sự kiện khi bấm nút Xem chi tiết đơn hàng (👁)
    public event EventHandler<string>? OnViewTicketDetailsClicked;

    public ProfileUC()
    {
        InitializeComponents();
        LoadOrderHistoryData();
    }

    private void InitializeComponents()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ColorBg;
        this.AutoScroll = true;

        // -------------------------------------------------------------
        // 1. KHỐI THÔNG TIN TÀI KHOẢN (PROFILE HEADER)
        // -------------------------------------------------------------
        pnlProfileHeader = new Panel
        {
            Location = new Point(20, 20),
            Size = new Size(1140, 90),
            BackColor = ColorCard,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Padding = new Padding(15)
        };

        PictureBox picAvatar = new PictureBox
        {
            Location = new Point(15, 15),
            Size = new Size(60, 60),
            SizeMode = PictureBoxSizeMode.CenterImage,
            BackColor = Color.FromArgb(45, 46, 65)
        };

        lblCustomerName = new Label
        {
            Text = "Nguyễn Văn A",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(90, 18),
            AutoSize = true
        };

        lblCustomerEmail = new Label
        {
            Text = "nguyenvana@gmail.com | SĐT: 0912 345 678",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorTextMuted,
            Location = new Point(90, 48),
            AutoSize = true
        };

        lblMemberTag = new Label
        {
            Text = "⭐ Thành viên Gold (350 điểm)",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = ColorGold,
            BackColor = Color.FromArgb(40, 35, 20),
            Location = new Point(930, 28),
            Size = new Size(180, 32),
            TextAlign = ContentAlignment.MiddleCenter,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        pnlProfileHeader.Controls.AddRange(new Control[] { picAvatar, lblCustomerName, lblCustomerEmail, lblMemberTag });

        // -------------------------------------------------------------
        // 2. KHỐI NGUYÊN BẢNG LỊCH SỬ ĐẶT VÉ (ORDER HISTORY TABLE)
        // -------------------------------------------------------------
        pnlTableContainer = new Panel
        {
            Location = new Point(20, 125),
            Size = new Size(1140, 580),
            BackColor = ColorCard,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Padding = new Padding(20)
        };

        // Header tiêu đề & Bộ lọc theo ngày
        Label lblTableTitle = new Label
        {
            Text = "Lịch Sử Đặt Vé (Recent Orders)",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(20, 15),
            AutoSize = true
        };

        dtpFilterDate = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Location = new Point(840, 15),
            Size = new Size(140, 30),
            Font = new Font("Segoe UI", 9.5f),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        btnFilter = new Button
        {
            Text = "🔍 Lọc",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.Black,
            BackColor = ColorGold,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(90, 30),
            Location = new Point(990, 15),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand
        };
        btnFilter.FlatAppearance.BorderSize = 0;
        btnFilter.Click += (s, e) => LoadOrderHistoryData();

        // Thanh tiêu đề các cột (Table Grid Header)
        Panel pnlTableHeader = new Panel
        {
            Location = new Point(20, 60),
            Size = new Size(1100, 40),
            BackColor = Color.FromArgb(18, 19, 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        AddHeaderColumn(pnlTableHeader, "Mã Đơn (Order ID)", 15, 160);
        AddHeaderColumn(pnlTableHeader, "Phòng (Screen)", 180, 100);
        AddHeaderColumn(pnlTableHeader, "Vị trí ghế (Seats)", 300, 180);
        AddHeaderColumn(pnlTableHeader, "Suất chiếu (Show time)", 500, 180);
        AddHeaderColumn(pnlTableHeader, "Tổng tiền (Payment)", 700, 140);
        AddHeaderColumn(pnlTableHeader, "HT Thanh toán", 860, 150);
        AddHeaderColumn(pnlTableHeader, "Chi tiết", 1020, 80);

        // Container danh sách hàng đơn hàng
        flpOrderRows = new FlowLayoutPanel
        {
            Location = new Point(20, 105),
            Size = new Size(1100, 450),
            AutoScroll = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        pnlTableContainer.Controls.AddRange(new Control[] { lblTableTitle, dtpFilterDate, btnFilter, pnlTableHeader, flpOrderRows });

        // Add vào UserControl
        this.Controls.Add(pnlTableContainer);
        this.Controls.Add(pnlProfileHeader);
    }

    private void AddHeaderColumn(Panel parent, string title, int xPos, int width)
    {
        Label lbl = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = ColorTextMuted,
            Location = new Point(xPos, 10),
            Size = new Size(width, 20),
            TextAlign = ContentAlignment.MiddleLeft
        };
        parent.Controls.Add(lbl);
    }

    // Tải danh sách các đơn hàng giả lập
    private void LoadOrderHistoryData()
    {
        flpOrderRows.Controls.Clear();

        var sampleOrders = new List<(string Id, string Screen, string Seats, string Showtime, decimal Amount, string PayMode)>
        {
            ("#BK6352635", "Phòng 04", "A2, A3, A4", "15:00 - 05/11/2026", 270000m, "Tiền mặt (Cash)"),
            ("#BK6325462", "Phòng 04", "F1", "15:00 - 02/11/2026", 90000m, "Ví QR Code"),
            ("#BK6356328", "Phòng 02", "G5, G6", "14:00 - 28/10/2026", 180000m, "Thẻ Visa/Master"),
            ("#BK6356329", "Phòng 02", "A1, A2", "16:30 - 25/10/2026", 180000m, "Tiền mặt (Cash)"),
            ("#BK6356330", "Phòng 02", "C1, C2, C3, C4", "16:30 - 20/10/2026", 360000m, "Tiền mặt (Cash)"),
            ("#BK6356331", "Phòng 01", "D2, D3", "16:30 - 15/10/2026", 180000m, "Thẻ Visa/Master")
        };

        foreach (var order in sampleOrders)
        {
            Panel row = CreateOrderRow(order.Id, order.Screen, order.Seats, order.Showtime, order.Amount, order.PayMode);
            flpOrderRows.Controls.Add(row);
        }
    }

    private Panel CreateOrderRow(string id, string screen, string seats, string showtime, decimal amount, string payMode)
    {
        Panel row = new Panel
        {
            Size = new Size(1080, 50),
            BackColor = Color.FromArgb(20, 21, 30),
            Margin = new Padding(0, 0, 0, 8),
            Cursor = Cursors.Hand
        };

        Label lblId = new Label { Text = id, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.White, Location = new Point(15, 15), Size = new Size(150, 20) };
        Label lblScreen = new Label { Text = screen, Font = new Font("Segoe UI", 9), ForeColor = ColorTextMuted, Location = new Point(180, 15), Size = new Size(100, 20) };
        Label lblSeats = new Label { Text = seats, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = ColorGold, Location = new Point(300, 15), Size = new Size(180, 20) };
        Label lblShowtime = new Label { Text = showtime, Font = new Font("Segoe UI", 9), ForeColor = Color.White, Location = new Point(500, 15), Size = new Size(180, 20) };
        Label lblAmount = new Label { Text = $"{amount.ToString("N0", new CultureInfo("vi-VN"))} đ", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.LightGreen, Location = new Point(700, 15), Size = new Size(140, 20) };
        Label lblPayMode = new Label { Text = payMode, Font = new Font("Segoe UI", 9), ForeColor = ColorTextMuted, Location = new Point(860, 15), Size = new Size(150, 20) };

        Button btnView = new Button
        {
            Text = "👁",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(40, 42, 60),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(36, 32),
            Location = new Point(1020, 9),
            Cursor = Cursors.Hand
        };
        btnView.FlatAppearance.BorderSize = 0;
        btnView.Click += (s, e) => OnViewTicketDetailsClicked?.Invoke(this, id);

        // Hiệu ứng rê chuột vào hàng (Hover effect)
        row.MouseEnter += (s, e) => row.BackColor = ColorRowHover;
        row.MouseLeave += (s, e) => row.BackColor = Color.FromArgb(20, 21, 30);

        row.Controls.AddRange(new Control[] { lblId, lblScreen, lblSeats, lblShowtime, lblAmount, lblPayMode, btnView });
        return row;
    }
}