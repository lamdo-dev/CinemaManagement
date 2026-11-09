using System;
using System.Drawing;
using System.Windows.Forms;
using CinemaManagement.Forms.Dialogs;
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

    // Constructor 1: Khởi tạo mặc định (Quyền Admin)
    public MainForm() : this(UserRole.Admin) { }

    // Constructor 2: Khởi tạo bằng Enum UserRole
    public MainForm(UserRole role)
    {
        InitializeComponent();
        currentRole = role;
        SetupFullUILayout();
    }

    // Constructor 3: Khởi tạo bằng chuỗi String (Tương thích khi gọi từ LoginForm)
    public MainForm(string roleName) 
        : this(Enum.TryParse<UserRole>(roleName, true, out var parsedRole) ? parsedRole : UserRole.Admin)
    {
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

        // Content Area
        panelContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(241, 242, 246)
        };

        // =========================================================
        // PHÂN QUYỀN MENU SIDEBAR ACCORDING TO USER ROLE
        // =========================================================

        // 1. NHÂN VIÊN & ADMIN: BÁN HÀNG VÀ SOÁT VÉ
        if (currentRole == UserRole.Admin || currentRole == UserRole.Staff)
        {
            AddNavGroup("--- BÁN HÀNG & SOÁT VÉ ---");
            AddNavButton("🛒 Bán vé tại quầy (POS)", () => SwitchView("🛒 Bán vé tại quầy (POS)", new POSUC()));
            AddNavButton("📷 Quét mã QR soát vé", () => SwitchView("📷 Soát vé QR Code", new TicketScannerUC()));
        }

        // 2. QUẢN TRỊ VIÊN: DÀNH CHO ADMIN
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

        // 3. KHÁCH HÀNG & ADMIN: LUỒNG ĐẶT VÉ TRỰC TUYẾN
        if (currentRole == UserRole.Admin || currentRole == UserRole.Customer)
        {
            AddNavGroup("--- KHÁCH HÀNG (CUSTOMER) ---");
            AddNavButton("🏠 Trang chủ Phim", () => SwitchView("🏠 Trang chủ", new HomeUC()));
            AddNavButton("📅 Lịch chiếu", () => SwitchView("📅 Lịch chiếu phim", new ShowtimeUC()));
            AddNavButton("🎟️ Đặt vé liên hoàn (Flow)", () => StartBookingFlow());
            AddNavButton("💺 Chọn ghế đặt vé", () => SwitchView("💺 Sơ đồ chọn ghế", new SeatSelectionUC()));
            AddNavButton("🍿 Chọn bắp nước", () => OpenFoodSelectionView());
            AddNavButton("💳 Thanh toán", () => SwitchView("💳 Thanh toán hóa đơn", new CheckoutUC()));
            AddNavButton("🎟️ Vé của tôi", () => SwitchView("🎟️ Quản lý vé đã mua", new TicketUC()));
        }

        // 4. NÚT ĐĂNG XUẤT HỆ THỐNG
        AddNavGroup("--- HỆ THỐNG ---");
        AddNavButton("🚪 Đăng xuất", () => {
            this.Hide();
            new LoginForm().Show();
        });

        this.Controls.Add(panelContent);
        this.Controls.Add(panelHeader);
        this.Controls.Add(panelSidebar);

        // Mở màn hình mặc định theo vai trò khi vừa đăng nhập thành công
        if (currentRole == UserRole.Customer)
            SwitchView("🏠 Trang chủ", new HomeUC());
        else if (currentRole == UserRole.Staff)
            SwitchView("🛒 Bán vé tại quầy (POS)", new POSUC());
        else
            SwitchView("📊 Dashboard Doanh thu & Suất chiếu", new DashboardUC());
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

    // =========================================================
    // ĐIỀU HƯỚNG MÀN HÌNH CHÍNH (SWITCH VIEW)
    // =========================================================
    public void SwitchView(string title, UserControl control)
    {
        lblHeaderTitle.Text = title;
        
        // Giải phóng tài nguyên điều khiển cũ trước khi xóa
        foreach (Control c in panelContent.Controls)
        {
            c.Dispose();
        }
        panelContent.Controls.Clear();

        control.Dock = DockStyle.Fill;
        panelContent.Controls.Add(control);
        control.BringToFront();
    }

    // Nạp màn hình Chọn bắp nước độc lập
    private void OpenFoodSelectionView(decimal ticketTotalAmount = 0m)
    {
        var foodUC = new FoodSelectionUC(ticketTotalAmount);

        foodUC.OnContinueToCheckout += (s, cartData) =>
        {
            SwitchView("💳 Thanh toán hóa đơn", new CheckoutUC());
        };

        foodUC.OnBackClicked += (s, e) =>
        {
            SwitchView("💺 Chọn ghế đặt vé", new SeatSelectionUC());
        };

        SwitchView("🍿 Chọn bắp nước", foodUC);
    }
    
    // =========================================================
    // LUỒNG ĐẶT VÉ LIÊN HOÀN (BOOKING FLOW)
    // =========================================================
    private void StartBookingFlow()
    {
        // BƯỚC 1: BẮT ĐẦU VỚI MÀN HÌNH CHỌN GHẾ
        var seatUC = new SeatSelectionUC();

        SwitchView("💺 Bước 1: Chọn ghế ngồi", seatUC);

        // Tự động chuyển tiếp sang Bắp nước với số tiền vé ước tính (150.000đ)
        // Khi SeatSelectionUC hoàn thiện event chọn ghế, có thể đăng ký sự kiện tại đây:
        // OpenFoodStep(150000m);

        // BƯỚC 2: HÀM CHUYỂN SANG BẮP NƯỚC VÀ XÁC NHẬN THANH TOÁN
        void OpenFoodStep(decimal seatAmount)
        {
            var foodUC = new FoodSelectionUC(seatAmount);

            // Bấm "TIẾP TỤC THANH TOÁN" -> Mở Dialog Confirm
            foodUC.OnContinueToCheckout += (sender, cartData) =>
            {
                using var confirmDialog = new ConfirmBookingDialog();

                if (confirmDialog.ShowDialog(this) == DialogResult.OK)
                {
                    MessageBox.Show(
                        "Thanh toán thành công! Đã tạo hóa đơn và vé.", 
                        "Thông báo", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Information
                    );

                    SwitchView("🎟️ Vé của bạn", new TicketUC());
                }
            };

            // Bấm "← Quay lại" -> Về màn chọn ghế
            foodUC.OnBackClicked += (sender, e) =>
            {
                SwitchView("💺 Bước 1: Chọn ghế ngồi", seatUC);
            };

            SwitchView("🍿 Bước 2: Chọn bắp nước & Combo", foodUC);
        }
    }
}