namespace CinemaManagement.UserControls.Customer;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

public partial class MembershipUC : UserControl
{
    // Bảng màu Dark Cinema kết hợp Cam Accent từ ảnh mẫu
    private readonly Color ColorBg = Color.FromArgb(14, 15, 22);
    private readonly Color ColorCard = Color.FromArgb(24, 25, 36);
    private readonly Color ColorOrangeAccent = Color.FromArgb(245, 130, 32); // Màu cam điểm thưởng
    private readonly Color ColorTextMuted = Color.FromArgb(150, 150, 170);
    private readonly Color ColorGreenPlus = Color.FromArgb(46, 204, 113);
    private readonly Color ColorRedMinus = Color.FromArgb(231, 76, 60);

    // Dynamic Data
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CurrentPoints { get; set; } = 2999;

    // Controls UI
    private Panel pnlPointsCard;
    private Label lblPointsValue;
    private Label lblMemberTier;
    private FlowLayoutPanel flpHistoryList;
    private FlowLayoutPanel flpVoucherCatalog;

    private Button btnTabRedeem;
    private Button btnTabEarn;
    private Button btnTabRules;

    public MembershipUC()
    {
        InitializeComponents();
        LoadPointHistoryData();
        LoadVoucherCatalogData();
    }

    private void InitializeComponents()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ColorBg;
        this.AutoScroll = true;

        // -------------------------------------------------------------
        // 1. KHỐI HIỂN THỊ TỔNG ĐIỂM THƯỞNG (ORANGE HEADER CARD)
        // -------------------------------------------------------------
        pnlPointsCard = new Panel
        {
            Location = new Point(20, 20),
            Size = new Size(1140, 150),
            BackColor = ColorOrangeAccent,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Padding = new Padding(25)
        };

        Label lblRewardsTitle = new Label
        {
            Text = "THÀNH VIÊN & TÍCH ĐIỂM (REWARDS)",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 235, 215),
            Location = new Point(20, 15),
            AutoSize = true
        };

        lblPointsValue = new Label
        {
            Text = "2,999",
            Font = new Font("Segoe UI", 36, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(20, 40),
            AutoSize = true
        };

        Label lblPointsUnit = new Label
        {
            Text = "Points",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 235, 215),
            Location = new Point(190, 68),
            AutoSize = true
        };

        lblMemberTier = new Label
        {
            Text = "👑 HẠNG VÀNG (GOLD MEMBER)",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = Color.Black,
            BackColor = Color.White,
            Location = new Point(880, 50),
            Size = new Size(230, 40),
            TextAlign = ContentAlignment.MiddleCenter,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        pnlPointsCard.Controls.AddRange(new Control[] { lblRewardsTitle, lblPointsValue, lblPointsUnit, lblMemberTier });

        // -------------------------------------------------------------
        // 2. THANH CHỌN TÍNH NĂNG (ACTION BUTTONS BAR)
        // -------------------------------------------------------------
        Panel pnlActions = new Panel
        {
            Location = new Point(20, 185),
            Size = new Size(1140, 45),
            BackColor = ColorCard,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        btnTabRedeem = CreateActionButton("🎁 Đổi Ưu Đãi (Redeem)", 15, true);
        btnTabEarn = CreateActionButton("📈 Lịch Sử Tích Điểm (Earn)", 230, false);
        btnTabRules = CreateActionButton("ℹ️ Thể Lệ Hạng Thẻ (Exchange)", 465, false);

        pnlActions.Controls.AddRange(new Control[] { btnTabRedeem, btnTabEarn, btnTabRules });

        // -------------------------------------------------------------
        // 3. CỘT TRÁI - DANH SÁCH LỊCH SỬ ĐIỂM (POINT HISTORY)
        // -------------------------------------------------------------
        Panel pnlHistoryContainer = new Panel
        {
            Location = new Point(20, 245),
            Size = new Size(620, 460),
            BackColor = ColorCard,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left,
            Padding = new Padding(15)
        };

        Label lblHistoryTitle = new Label
        {
            Text = "Lịch Sử Biến Động Điểm",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(15, 15),
            AutoSize = true
        };

        flpHistoryList = new FlowLayoutPanel
        {
            Location = new Point(15, 50),
            Size = new Size(590, 390),
            AutoScroll = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        pnlHistoryContainer.Controls.AddRange(new Control[] { lblHistoryTitle, flpHistoryList });

        // -------------------------------------------------------------
        // 4. CỘT PHẢI - KHO VOUCHER ĐỔI THƯỞNG (CATALOG)
        // -------------------------------------------------------------
        Panel pnlCatalogContainer = new Panel
        {
            Location = new Point(660, 245),
            Size = new Size(500, 460),
            BackColor = ColorCard,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Padding = new Padding(15)
        };

        Label lblCatalogTitle = new Label
        {
            Text = "Kho Quà Tặng Đổi Điểm",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(15, 15),
            AutoSize = true
        };

        flpVoucherCatalog = new FlowLayoutPanel
        {
            Location = new Point(15, 50),
            Size = new Size(470, 390),
            AutoScroll = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        pnlCatalogContainer.Controls.AddRange(new Control[] { lblCatalogTitle, flpVoucherCatalog });

        // Add tất cả vào UserControl
        this.Controls.Add(pnlCatalogContainer);
        this.Controls.Add(pnlHistoryContainer);
        this.Controls.Add(pnlActions);
        this.Controls.Add(pnlPointsCard);
    }

    private Button CreateActionButton(string text, int xPos, bool isActive)
    {
        Button btn = new Button
        {
            Text = text,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = isActive ? Color.Black : Color.White,
            BackColor = isActive ? ColorOrangeAccent : Color.FromArgb(35, 36, 50),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(200, 35),
            Location = new Point(xPos, 5),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    // Tải dữ liệu Lịch sử điểm (Khớp đúng dữ liệu trong ảnh 10.jpg)
    private void LoadPointHistoryData()
    {
        flpHistoryList.Controls.Clear();

        var historyItems = new List<(string Description, string Time, int Points, bool IsBonus)>
        {
            ("Dark Phoenix (Tích điểm xem phim)", "12:30 PM, 14 Oct", 500, true),
            ("Redeem Combo Set A (Đổi bắp nước)", "12:30 PM, 1 Oct", 50, false),
            ("Redeem Movie Ticket (Đổi vé xem phim)", "12:30 PM, 1 Oct", 200, false),
            ("Referral (Giới thiệu bạn bè)", "01:30 PM, 23 Sep", 300, true),
            ("Referral (Giới thiệu bạn bè)", "11:15 AM, 15 Sep", 300, true),
            ("Tích điểm sinh nhật khách hàng", "09:00 AM, 01 Sep", 500, true)
        };

        foreach (var item in historyItems)
        {
            Panel row = CreateHistoryRow(item.Description, item.Time, item.Points, item.IsBonus);
            flpHistoryList.Controls.Add(row);
        }
    }

    private Panel CreateHistoryRow(string desc, string time, int points, bool isBonus)
    {
        Panel row = new Panel
        {
            Size = new Size(560, 55),
            BackColor = Color.FromArgb(18, 19, 28),
            Margin = new Padding(0, 0, 0, 8)
        };

        // Vạch màu chỉ thị Xanh (Cộng) / Đỏ (Trừ)
        Panel indicator = new Panel
        {
            Size = new Size(4, 35),
            Location = new Point(10, 10),
            BackColor = isBonus ? ColorGreenPlus : ColorRedMinus
        };

        Label lblDesc = new Label
        {
            Text = desc,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(25, 8),
            Size = new Size(360, 20),
            AutoEllipsis = true
        };

        Label lblTime = new Label
        {
            Text = time,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = ColorTextMuted,
            Location = new Point(25, 28),
            AutoSize = true
        };

        Label lblPoints = new Label
        {
            Text = isBonus ? $"+ {points}" : $"- {points}",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = isBonus ? ColorGreenPlus : ColorRedMinus,
            Location = new Point(420, 15),
            Size = new Size(120, 25),
            TextAlign = ContentAlignment.MiddleRight
        };

        row.Controls.AddRange(new Control[] { indicator, lblDesc, lblTime, lblPoints });
        return row;
    }

    // Tải danh sách Voucher đổi thưởng
    private void LoadVoucherCatalogData()
    {
        flpVoucherCatalog.Controls.Clear();

        var vouchers = new List<(string Title, int RequiredPoints, string Expiry)>
        {
            ("Voucher Giảm 50.000đ Vé Phim", 500, "Hạn SD: 31/12/2026"),
            ("Miễn Phí 01 Bắp Rang Bơ L", 300, "Hạn SD: 15/12/2026"),
            ("Miễn Phí 01 Nước Ngoọt Regular", 200, "Hạn SD: 20/12/2026"),
            ("Combo Đôi 2 Vé + 1 Bắp 2 Nước", 1200, "Hạn SD: 31/12/2026")
        };

        foreach (var v in vouchers)
        {
            Panel card = CreateVoucherCard(v.Title, v.RequiredPoints, v.Expiry);
            flpVoucherCatalog.Controls.Add(card);
        }
    }

    private Panel CreateVoucherCard(string title, int points, string expiry)
    {
        Panel card = new Panel
        {
            Size = new Size(440, 75),
            BackColor = Color.FromArgb(20, 21, 30),
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(10)
        };

        Label lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(10, 12),
            Size = new Size(280, 22),
            AutoEllipsis = true
        };

        Label lblExpiry = new Label
        {
            Text = expiry,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = ColorTextMuted,
            Location = new Point(10, 40),
            AutoSize = true
        };

        Button btnRedeem = new Button
        {
            Text = $"Đổi ({points} Pts)",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.Black,
            BackColor = ColorOrangeAccent,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(120, 36),
            Location = new Point(305, 18),
            Cursor = Cursors.Hand
        };
        btnRedeem.FlatAppearance.BorderSize = 0;
        btnRedeem.Click += (s, e) =>
        {
            if (CurrentPoints >= points)
            {
                CurrentPoints -= points;
                lblPointsValue.Text = CurrentPoints.ToString("N0");
                MessageBox.Show($"Đã đổi quà [{title}] thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Bạn không đủ điểm để đổi quà tặng này!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        card.Controls.AddRange(new Control[] { lblTitle, lblExpiry, btnRedeem });
        return card;
    }
}