using System;
using System.Drawing;
using System.Windows.Forms;
using CinemaManagement.Models.Enums;
using CinemaManagement.UserControls.Admin;
using CinemaManagement.UserControls.Customer;
using CinemaManagement.UserControls.Staff;

namespace CinemaManagement;

public partial class MainForm : Form
{
    private Panel panelSidebar = null!;
    private Panel panelContent = null!;
    private Label lblHeaderTitle = null!;
    private UserRole currentRole;

    public MainForm() : this(UserRole.Admin) { }

    public MainForm(UserRole role)
    {
        InitializeComponent();
        currentRole = role;
        SetupFullUILayout();
    }

    private void SetupFullUILayout()
    {
        this.Text = $"HỆ THỐNG QUẢN LÝ RẠP CHIẾU PHIM - [{currentRole.ToString().ToUpper()}]";
        this.Size = new Size(1280, 760);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(245, 246, 250);

        // Sidebar
        panelSidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 240,
            BackColor = Color.FromArgb(30, 39, 46),
            AutoScroll = true
        };

        var lblBrand = new Label
        {
            Text = "🎬 CINEMA APP",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 60,
            TextAlign = ContentAlignment.MiddleCenter
        };
        panelSidebar.Controls.Add(lblBrand);

        // Header
        var panelHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.White
        };
        lblHeaderTitle = new Label
        {
            Text = "Trang làm việc",
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = Color.FromArgb(47, 53, 66),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(20, 0, 0, 0)
        };
        panelHeader.Controls.Add(lblHeaderTitle);

        // Content
        panelContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(241, 242, 246)
        };

        // Phân quyền hiển thị Menu theo Role
        if (currentRole == UserRole.Admin || currentRole == UserRole.Staff)
        {
            AddNavGroup("--- BÁN HÀNG & SOÁT VÉ ---");
            AddNavButton("🛒 Bán vé tại quầy (POS)", () => SwitchView("🛒 Bán vé tại quầy (POS)", new POSUC()));
            AddNavButton("📷 Quét mã QR soát vé", () => SwitchView("📷 Soát vé QR Code", new TicketScannerUC()));
        }

        if (currentRole == UserRole.Admin)
        {
            AddNavGroup("--- QUẢN TRỊ VIÊN (ADMIN) ---");
            AddNavButton("📊 Dashboard Thống kê", () => SwitchView("📊 Dashboard Doanh thu & Suất chiếu", new DashboardUC()));
            AddNavButton("🎬 Quản lý Phim", () => SwitchView("🎬 Danh sách phim", new MovieManagementUC()));
            AddNavButton("🚪 Quản lý Phòng chiếu", () => SwitchView("🚪 Quản lý phòng chiếu", new RoomManagementUC()));
            AddNavButton("⏰ Quản lý Suất chiếu", () => SwitchView("⏰ Lịch chiếu rạp", new ShowtimeManagementUC()));
            AddNavButton("🍿 Quản lý Bắp nước", () => SwitchView("🍿 Menu sản phẩm", new FoodManagementUC()));
            AddNavButton("👥 Quản lý Người dùng", () => SwitchView("👥 Tài khoản người dùng", new UserManagementUC()));
        }

        if (currentRole == UserRole.Admin || currentRole == UserRole.Customer)
        {
            AddNavGroup("--- KHÁCH HÀNG (CUSTOMER) ---");
            AddNavButton("🏠 Trang chủ Phim", () => SwitchView("🏠 Trang chủ", new HomeUC()));
            AddNavButton("📅 Lịch chiếu", () => SwitchView("📅 Lịch chiếu phim", new ShowtimeUC()));
            AddNavButton("💺 Chọn ghế đặt vé", () => SwitchView("💺 Sơ đồ chọn ghế", new SeatSelectionUC()));
            AddNavButton("🍿 Chọn bắp nước", () => SwitchView("🍿 Đặt bắp nước", new FoodSelectionUC()));
            AddNavButton("💳 Thanh toán", () => SwitchView("💳 Thanh toán hóa đơn", new CheckoutUC()));
            AddNavButton("🎟️ Vé của tôi", () => SwitchView("🎟️ Quản lý vé đã mua", new TicketUC()));
        }

        // Nút Đăng xuất
        AddNavGroup("--- HỆ THỐNG ---");
        AddNavButton("🚪 Đăng xuất", () => {
            this.Hide();
            new LoginForm().Show();
        });

        this.Controls.Add(panelContent);
        this.Controls.Add(panelHeader);
        this.Controls.Add(panelSidebar);

        // Mở view mặc định theo vai trò
        if (currentRole == UserRole.Customer)
            SwitchView("🏠 Trang chủ", new HomeUC());
        else if (currentRole == UserRole.Staff)
            SwitchView("🛒 Bán vé tại quầy (POS)", new POSUC());
        else
            SwitchView("📊 Dashboard Doanh thu", new DashboardUC());
    }

    private void AddNavGroup(string text)
    {
        var lblGroup = new Label
        {
            Text = text,
            ForeColor = Color.FromArgb(149, 175, 192),
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 35,
            TextAlign = ContentAlignment.BottomLeft,
            Padding = new Padding(15, 0, 0, 5)
        };
        panelSidebar.Controls.Add(lblGroup);
        lblGroup.BringToFront();
    }

    private void AddNavButton(string text, Action onClick)
    {
        var btn = new Button
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 40,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5F),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(20, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(87, 101, 116);
        btn.Click += (s, e) => onClick();

        panelSidebar.Controls.Add(btn);
        btn.BringToFront();
    }

    private void SwitchView(string title, UserControl control)
    {
        lblHeaderTitle.Text = title;
        panelContent.Controls.Clear();
        control.Dock = DockStyle.Fill;
        panelContent.Controls.Add(control);
    }
}