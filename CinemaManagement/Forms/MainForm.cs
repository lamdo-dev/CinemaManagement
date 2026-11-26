using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

using CinemaManagement.Models;
using CinemaManagement.Models.Enums;
using CinemaManagement.Services;
using CinemaManagement.UserControls.Admin;
using CinemaManagement.UserControls.Customer;
using CinemaManagement.UserControls.Staff;
using CinemaManagement.Utils;

namespace CinemaManagement
{
    public partial class MainForm : Form
    {
        // =========================================================
        // 1. FIELDS & MODERN SIDEBAR THEME
        // =========================================================

        private enum NavIconType
        {
            GridMenu,
            Cart,
            QrScan,
            Chart,
            Movie,
            Room,
            Schedule,
            PriceTag,
            Food,
            Gift,
            Users,
            Home,
            Ticket,
            Seat,
            Payment,
            Logout
        }

        private sealed class NavItemState
        {
            public NavIconType IconType;
            public string Label = string.Empty;
            public bool IsActive;
            public bool IsHovered;
            public bool IsDanger;
        }

        private Color ColSidebarBg     = Color.FromArgb(255, 255, 255);
        private Color ColSidebarBorder = Color.FromArgb(229, 234, 242);
        private Color ColGroupLabel    = Color.FromArgb(148, 163, 184);
        private Color ColItemText      = Color.FromArgb(30, 41, 59);
        private Color ColItemActiveBg  = Color.FromArgb(241, 245, 249);
        private Color ColItemHoverBg   = Color.FromArgb(248, 250, 252);
        private Color ColEmeraldIcon   = Color.FromArgb(22, 163, 74);
        private Color ColEmeraldSoft   = Color.FromArgb(220, 252, 231);

        private Panel panelSidebar = null!;
        private FlowLayoutPanel flpNavContainer = null!;
        private Panel pnlBrand = null!;
        private Label lblBrandTitle = null!;
        private Panel panelHeader = null!;
        private Panel panelContent = null!;
        private Label lblHeaderTitle = null!;
        private NeonThemeButton btnThemeToggle = null!;
        private Panel? pnlThemeToggleHost;

        private readonly List<Panel> _navPanels = new();
        private readonly List<Label> _navGroupLabels = new();
        private Panel? _activeNavPanel;
        private UserControl? _activeView;
        private bool _isDarkTheme = DashboardUC.IsDarkTheme;

        // Các mục điều hướng để tự động highlight theo bước hiện tại
        private Panel? _navCustomerHome;
        private Panel? _navCustomerShowtime;
        private Panel? _navCustomerSeat;
        private Panel? _navCustomerFood;
        private Panel? _navCustomerCheckout;
        private Panel? _navCustomerTicket;
        private Panel? _navStaffPos;
        private Panel? _navStaffScanner;

        private readonly UserRole currentRole;

        // Tránh xử lý Resize khi giao diện chưa khởi tạo xong.
        private bool _responsiveReady;

        // Dữ liệu dùng chung trong toàn bộ quy trình đặt vé.
        private readonly BookingContext _bookingContext = new();

        // =========================================================
        // 2. CONSTRUCTORS
        // =========================================================

        public MainForm() : this(UserRole.Admin)
        {
        }

        public MainForm(UserRole role)
        {
            InitializeComponent();
            currentRole = role;

            // WinForms tự co giãn theo DPI của Windows.
            AutoScaleMode = AutoScaleMode.Dpi;

            SetupFullUILayout();
            ApplyShellTheme(_isDarkTheme);

            // Chỉ bật responsive sau khi giao diện đã được tạo.
            SizeChanged += MainForm_SizeChanged;
            DpiChanged += (s, e) =>
            {
                UpdateResponsiveLayout();
                UpdateBrandHeaderLayout();
                btnThemeToggle.Width = ScaleForDpi(78);
                btnThemeToggle.Height = ScaleForDpi(38);
                PositionThemeToggle();
                PositionCustomerThemeToggle();
            };
            _responsiveReady = true;

            UpdateResponsiveLayout();
        }

        public MainForm(string roleName)
            : this(
                Enum.TryParse<UserRole>(
                    roleName,
                    true,
                    out var parsedRole)
                    ? parsedRole
                    : UserRole.Admin)
        {
        }

        // =========================================================
        // 3. MAIN INTERFACE
        // =========================================================

        private void SetupFullUILayout()
        {
            string userDisplay =
                UserSession.CurrentUser?.FullName
                ?? currentRole.ToString();

            Text =
                $"HỆ THỐNG QUẢN LÝ RẠP CHIẾU PHIM - " +
                $"[{currentRole.ToString().ToUpper()} : {userDisplay}]";

            Size = new Size(1320, 780);
            MinimumSize = new Size(1040, 660);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(245, 246, 250);

            // -----------------------------------------------------
            // SIDEBAR
            // -----------------------------------------------------
            panelSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 256,
                MinimumSize = new Size(190, 0),
                BackColor = ColSidebarBg,
                Padding = new Padding(0, 0, 1, 0)
            };
            panelSidebar.Paint += (s, e) =>
            {
                using var borderPen = new Pen(ColSidebarBorder, 1F);
                e.Graphics.DrawLine(
                    borderPen,
                    panelSidebar.Width - 1,
                    0,
                    panelSidebar.Width - 1,
                    panelSidebar.Height);
            };

            pnlBrand = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = ColSidebarBg
            };
            lblBrandTitle = new Label
            {
                Text = "CINEMA APP",
                AutoSize = false,
                AutoEllipsis = true,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                ForeColor = ColItemText,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlBrand.Controls.Add(lblBrandTitle);
            pnlBrand.SizeChanged += (s, e) => UpdateBrandHeaderLayout();
            pnlBrand.Layout += (s, e) => UpdateBrandHeaderLayout();
            UpdateBrandHeaderLayout();

            pnlBrand.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var logoBox = new Rectangle(
                    ScaleForDpi(18), ScaleForDpi(16), ScaleForDpi(36), ScaleForDpi(36));
                using var path = CreateRoundedPath(logoBox, ScaleForDpi(10));
                using var bg = new SolidBrush(ColEmeraldSoft);
                e.Graphics.FillPath(bg, path);

                DrawVectorIcon(e.Graphics, NavIconType.Movie,
                    new Rectangle(ScaleForDpi(24), ScaleForDpi(22), ScaleForDpi(24), ScaleForDpi(24)),
                    ColEmeraldIcon);

                using var fontRole = new Font("Segoe UI", 8F, FontStyle.Bold);
                using var brushRole = new SolidBrush(ColEmeraldIcon);
                string userLine = $"● {userDisplay}";
                e.Graphics.DrawString(
                    userLine, fontRole, brushRole, ScaleForDpi(63), ScaleForDpi(41));

                using var divPen = new Pen(ColSidebarBorder, 1F);
                e.Graphics.DrawLine(
                    divPen,
                    ScaleForDpi(16),
                    pnlBrand.Height - 1,
                    pnlBrand.Width - ScaleForDpi(16),
                    pnlBrand.Height - 1);
            };

            flpNavContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = ColSidebarBg,
                Padding = new Padding(12, 8, 12, 16)
            };

            panelSidebar.Controls.Add(flpNavContainer);
            panelSidebar.Controls.Add(pnlBrand);

            // -----------------------------------------------------
            // HEADER
            // -----------------------------------------------------
            panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 62,
                BackColor = ColSidebarBg
            };
            panelHeader.Paint += (s, e) =>
            {
                using var borderPen = new Pen(ColSidebarBorder, 1F);
                e.Graphics.DrawLine(
                    borderPen,
                    0,
                    panelHeader.Height - 1,
                    panelHeader.Width,
                    panelHeader.Height - 1);
            };

            lblHeaderTitle = new Label
            {
                Text = "Trang làm việc",
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                ForeColor = ColItemText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(22, 0, 110, 0),
                AutoEllipsis = true
            };

            btnThemeToggle = new NeonThemeButton
            {
                Width = ScaleForDpi(78),
                Height = ScaleForDpi(38),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(panelHeader.ClientSize.Width - 164, 12),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnThemeToggle.IsDarkTheme = _isDarkTheme;
            btnThemeToggle.Click += (s, e) => ToggleTheme();

            panelHeader.Controls.Add(lblHeaderTitle);
            if (currentRole != UserRole.Customer)
            {
                panelHeader.Controls.Add(btnThemeToggle);
                btnThemeToggle.BringToFront();
                panelHeader.Resize += (s, e) => PositionThemeToggle();
                panelHeader.Layout += (s, e) => PositionThemeToggle();
                PositionThemeToggle();
            }
            else
            {
                lblHeaderTitle.Padding = new Padding(22, 0, 10, 0);
            }

            // -----------------------------------------------------
            // CONTENT
            // -----------------------------------------------------
            panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(241, 242, 246)
            };
// =====================================================
            // MENU ITEMS (PHÂN QUYỀN ĐẦY ĐỦ)
            // =====================================================

            // STAFF & ADMIN MENU
            if (currentRole == UserRole.Admin || currentRole == UserRole.Staff)
            {
                AddNavGroup("BÁN HÀNG & SOÁT VÉ");

                _navStaffPos = AddNavButton(
                    "Bán vé tại quầy (POS)",
                    NavIconType.Cart,
                    OpenPOSView);

                _navStaffScanner = AddNavButton(
                    "Quét mã QR soát vé",
                    NavIconType.QrScan,
                    () => OpenTicketScannerView());

                if (currentRole == UserRole.Staff)
                    SetActiveNav(_navStaffPos);
            }

            // ADMIN MENU
            if (currentRole == UserRole.Admin)
            {
                AddNavGroup("QUẢN TRỊ VIÊN (ADMIN)");

                var itemDash = AddNavButton(
                    "Dashboard Thống kê",
                    NavIconType.Chart,
                    OpenDashboardView);

                AddNavButton(
                    "Quản lý Phim",
                    NavIconType.Movie,
                    () => SwitchView(
                        "Danh sách phim",
                        new MovieManagementUC()));

                AddNavButton(
                    "Quản lý Phòng chiếu",
                    NavIconType.Room,
                    () => SwitchView(
                        "Quản lý phòng chiếu",
                        new RoomManagementUC()));

                AddNavButton(
                    "Quản lý Suất chiếu",
                    NavIconType.Schedule,
                    () => SwitchView(
                        "Lịch chiếu rạp",
                        new ShowtimeManagementUC()));

                AddNavButton(
                    "Quản lý Giá vé",
                    NavIconType.PriceTag,
                    () => SwitchView(
                        "Quản lý giá vé",
                        new PricingUC()));

                AddNavButton(
                    "Quản lý Bắp nước",
                    NavIconType.Food,
                    () => SwitchView(
                        "Menu sản phẩm",
                        new FoodManagementUC()));

                AddNavButton(
                    "Quản lý Khuyến mãi",
                    NavIconType.Gift,
                    () => SwitchView(
                        "Quản lý chương trình khuyến mãi",
                        new PromotionUC()));

                AddNavButton(
                    "Quản lý Người dùng",
                    NavIconType.Users,
                    () => SwitchView(
                        "Tài khoản người dùng",
                        new UserManagementUC()));

                SetActiveNav(itemDash);
            }

            // CUSTOMER MENU
            if (currentRole == UserRole.Customer)
            {
                AddNavGroup("DỊCH VỤ KHÁCH HÀNG");

                pnlThemeToggleHost = new Panel
                {
                    Height = ScaleForDpi(48),
                    Margin = new Padding(0, ScaleForDpi(2), 0, ScaleForDpi(6)),
                    BackColor = ColSidebarBg
                };
                pnlThemeToggleHost.Controls.Add(btnThemeToggle);
                flpNavContainer.Controls.Add(pnlThemeToggleHost);
                flpNavContainer.ClientSizeChanged += (s, e) => PositionCustomerThemeToggle();
                flpNavContainer.Layout += (s, e) => PositionCustomerThemeToggle();
                PositionCustomerThemeToggle();

                _navCustomerHome = AddNavButton(
                    "Trang chủ Phim",
                    NavIconType.Home,
                    OpenHomeView);

                AddNavButton(
                    "Khuyến mãi",
                    NavIconType.Gift,
                    () => SwitchView(
                        "Ưu đãi & Khuyến mãi",
                        new PromotionListUC()));

                _navCustomerShowtime = AddNavButton(
                    "Lịch chiếu",
                    NavIconType.Schedule,
                    () => OpenShowtimeView(_bookingContext.Movie));

                _navCustomerSeat = AddNavButton(
                    "Chọn ghế đặt vé",
                    NavIconType.Seat,
                    () => OpenSeatSelectionView());

                _navCustomerFood = AddNavButton(
                    "Chọn bắp nước",
                    NavIconType.Food,
                    OpenFoodSelectionView);

                _navCustomerCheckout = AddNavButton(
                    "Thanh toán",
                    NavIconType.Payment,
                    OpenCheckoutView);

                _navCustomerTicket = AddNavButton(
                    "Vé của tôi",
                    NavIconType.Ticket,
                    () => OpenTicketView());

                SetActiveNav(_navCustomerHome);
            }

            // =====================================================
            // LOGOUT
            // =====================================================
            AddNavGroup("HỆ THỐNG");

            AddNavButton("Đăng xuất", NavIconType.Logout, () =>
            {
                // Xóa lượt đặt vé chưa hoàn tất.
                _bookingContext.Clear();

                // Chỉ đăng xuất, không xóa lịch sử đặt vé.
                UserSession.Logout();

                Hide();

                var loginForm = new LoginForm();
                loginForm.FormClosed += (s, args) => Close();
                loginForm.Show();
            }, isDanger: true);

            // ADD CONTROLS
            Controls.Add(panelContent);
            Controls.Add(panelHeader);
            Controls.Add(panelSidebar);

            // DEFAULT VIEW
            if (currentRole == UserRole.Customer)
            {
                OpenHomeView();
            }
            else if (currentRole == UserRole.Staff)
            {
                OpenPOSView();
            }
            else
            {
                OpenDashboardView();
            }
        }

        private void PositionThemeToggle()
        {
            if (currentRole == UserRole.Customer ||
                panelHeader == null ||
                btnThemeToggle == null ||
                panelHeader.IsDisposed)
            {
                return;
            }

            int rightPadding = ScaleForDpi(16);
            int top = Math.Max(0, (panelHeader.ClientSize.Height - btnThemeToggle.Height) / 2);
            int left = Math.Max(
                ScaleForDpi(8),
                panelHeader.ClientSize.Width - btnThemeToggle.Width - rightPadding);

            btnThemeToggle.Location = new Point(left, top);
        }

        private void PositionCustomerThemeToggle()
        {
            if (currentRole != UserRole.Customer ||
                flpNavContainer == null ||
                flpNavContainer.IsDisposed ||
                pnlThemeToggleHost == null ||
                pnlThemeToggleHost.IsDisposed)
            {
                return;
            }

            int scrollbarWidth = flpNavContainer.VerticalScroll.Visible
                ? SystemInformation.VerticalScrollBarWidth
                : 0;
            int hostWidth = Math.Max(
                btnThemeToggle.Width,
                flpNavContainer.ClientSize.Width -
                flpNavContainer.Padding.Horizontal - scrollbarWidth);

            if (pnlThemeToggleHost.Width != hostWidth)
                pnlThemeToggleHost.Width = hostWidth;

            int left = Math.Max(0, (pnlThemeToggleHost.ClientSize.Width - btnThemeToggle.Width) / 2);
            int top = Math.Max(0, (pnlThemeToggleHost.ClientSize.Height - btnThemeToggle.Height) / 2);
            var location = new Point(left, top);
            if (btnThemeToggle.Location != location)
                btnThemeToggle.Location = location;
        }

        // =========================================================
        // 4. MODERN SIDEBAR BUILDER & VECTOR ICON RENDERER
        // =========================================================

        private void AddNavGroup(string text)
        {
            var lblGroup = new Label
            {
                Text = text,
                ForeColor = ColGroupLabel,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Width = 224,
                Height = 30,
                Margin = new Padding(0, 8, 0, 2),
                TextAlign = ContentAlignment.BottomLeft,
                Padding = new Padding(12, 0, 0, 4),
                BackColor = Color.Transparent
            };

            flpNavContainer.Controls.Add(lblGroup);
            _navGroupLabels.Add(lblGroup);
        }

        private void OpenDashboardView()
        {
            var dashboard = new DashboardUC();
            dashboard.ThemeChanged += ApplyShellTheme;
            SwitchView("Dashboard Doanh thu & Suất chiếu", dashboard);
            ApplyShellTheme(DashboardUC.IsDarkTheme);
        }

        private void ToggleTheme()
        {
            bool isDark = !_isDarkTheme;
            if (_activeView is DashboardUC dashboard)
            {
                dashboard.SetTheme(isDark);
                return;
            }

            DashboardUC.SetThemePreference(isDark);
            ApplyShellTheme(isDark);
        }

        private void ApplyShellTheme(bool isDark)
        {
            _isDarkTheme = isDark;
            DashboardUC.SetThemePreference(isDark);

            if (isDark)
            {
                ColSidebarBg = Color.FromArgb(15, 23, 42);
                ColSidebarBorder = Color.FromArgb(42, 56, 86);
                ColGroupLabel = Color.FromArgb(148, 163, 184);
                ColItemText = Color.FromArgb(226, 232, 240);
                ColItemActiveBg = Color.FromArgb(30, 41, 59);
                ColItemHoverBg = Color.FromArgb(27, 38, 59);
                ColEmeraldIcon = Color.FromArgb(52, 211, 153);
                ColEmeraldSoft = Color.FromArgb(6, 78, 59);
            }
            else
            {
                ColSidebarBg = Color.White;
                ColSidebarBorder = Color.FromArgb(229, 234, 242);
                ColGroupLabel = Color.FromArgb(148, 163, 184);
                ColItemText = Color.FromArgb(30, 41, 59);
                ColItemActiveBg = Color.FromArgb(241, 245, 249);
                ColItemHoverBg = Color.FromArgb(248, 250, 252);
                ColEmeraldIcon = Color.FromArgb(22, 163, 74);
                ColEmeraldSoft = Color.FromArgb(220, 252, 231);
            }

            panelSidebar.BackColor = ColSidebarBg;
            pnlBrand.BackColor = ColSidebarBg;
            lblBrandTitle.ForeColor = ColItemText;
            flpNavContainer.BackColor = ColSidebarBg;
            if (pnlThemeToggleHost != null)
                pnlThemeToggleHost.BackColor = ColSidebarBg;
            panelHeader.BackColor = ColSidebarBg;
            panelContent.BackColor = isDark
                ? Color.FromArgb(11, 16, 32)
                : Color.FromArgb(241, 242, 246);
            lblHeaderTitle.ForeColor = ColItemText;
            btnThemeToggle.IsDarkTheme = isDark;
            this.BackColor = panelContent.BackColor;

            foreach (Label label in _navGroupLabels)
                label.ForeColor = ColGroupLabel;

            foreach (Panel item in _navPanels)
            {
                item.BackColor = ColSidebarBg;
                item.Invalidate();
            }

            panelSidebar.Invalidate(true);
            panelHeader.Invalidate();
            pnlBrand.Invalidate();

            if (_activeView != null && _activeView is not DashboardUC)
                ThemeColorApplicator.Apply(_activeView, isDark);

            ApplyActiveViewTheme(isDark);
        }

        private Panel AddNavButton(
            string text,
            NavIconType iconType,
            Action onClick,
            bool isDanger = false)
        {
            var state = new NavItemState
            {
                IconType = iconType,
                Label = text,
                IsDanger = isDanger
            };

            var item = new Panel
            {
                Tag = state,
                Width = Math.Max(180, flpNavContainer.ClientSize.Width - 24),
                Height = 44,
                Margin = new Padding(0, 2, 0, 2),
                BackColor = ColSidebarBg,
                Cursor = Cursors.Hand
            };

            // Bật DoubleBuffered để hạn chế nhấp nháy khi vẽ Sidebar.
            typeof(Control).InvokeMember(
                "DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic,
                null,
                item,
                new object[] { true });

            item.Paint += (sender, e) =>
            {
                if (item.Tag is not NavItemState itemState)
                    return;

                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                Rectangle pillRect = new Rectangle(
                    1, 1,
                    Math.Max(0, item.Width - 3),
                    Math.Max(0, item.Height - 3));

                if (itemState.IsActive || itemState.IsHovered)
                {
                    using var path = CreateRoundedPath(pillRect, 12);
                    Color background = itemState.IsActive
                        ? ColItemActiveBg
                        : ColItemHoverBg;

                    using var backgroundBrush = new SolidBrush(background);
                    g.FillPath(backgroundBrush, path);
                }

                Color iconColor = itemState.IsDanger
                    ? Color.FromArgb(220, 38, 38)
                    : ColEmeraldIcon;

                Rectangle iconRect = new Rectangle(
                    14,
                    (item.Height - 18) / 2,
                    18,
                    18);

                DrawVectorIcon(g, itemState.IconType, iconRect, iconColor);

                Color textColor = itemState.IsDanger
                    ? Color.FromArgb(220, 38, 38)
                    : itemState.IsActive
                        ? ColItemText
                        : ColItemText;

                using var textFont = new Font(
                    "Segoe UI",
                    9.5F,
                    itemState.IsActive ? FontStyle.Bold : FontStyle.Regular);

                using var textBrush = new SolidBrush(textColor);
                using var stringFormat = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter
                };

                Rectangle textRect = new Rectangle(
                    44,
                    0,
                    Math.Max(0, item.Width - 52),
                    item.Height);

                g.DrawString(
                    itemState.Label,
                    textFont,
                    textBrush,
                    textRect,
                    stringFormat);
            };

            item.MouseEnter += (sender, e) =>
            {
                if (item.Tag is NavItemState itemState)
                    itemState.IsHovered = true;

                item.Invalidate();
            };

            item.MouseLeave += (sender, e) =>
            {
                if (item.Tag is NavItemState itemState)
                    itemState.IsHovered = false;

                item.Invalidate();
            };

            item.Click += (sender, e) =>
            {
                if (item.Tag is NavItemState itemState && !itemState.IsDanger)
                    SetActiveNav(item);

                onClick();
            };

            _navPanels.Add(item);
            flpNavContainer.Controls.Add(item);

            return item;
        }

        private void SetActiveNav(Panel? target)
        {
            if (target == null)
                return;

            if (_activeNavPanel != null)
            {
                if (_activeNavPanel.Tag is NavItemState oldSt)
                    oldSt.IsActive = false;
                _activeNavPanel.Invalidate();
            }

            _activeNavPanel = target;
            if (_activeNavPanel.Tag is NavItemState newSt)
                newSt.IsActive = true;
            _activeNavPanel.Invalidate();
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

        private static void DrawVectorIcon(Graphics g, NavIconType type, Rectangle r, Color color)
        {
            using var pen = new Pen(color, 1.65F)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            using var brush = new SolidBrush(color);

            int x = r.X, y = r.Y, w = r.Width, h = r.Height;

            switch (type)
            {
                case NavIconType.GridMenu:
                    g.DrawRectangle(pen, x + 1, y + 1, 6, 6);
                    g.DrawRectangle(pen, x + 10, y + 1, 6, 6);
                    g.DrawRectangle(pen, x + 1, y + 10, 6, 6);
                    g.DrawRectangle(pen, x + 10, y + 10, 6, 6);
                    break;

                case NavIconType.Cart:
                    g.DrawLines(pen, new[]
                    {
                        new Point(x + 1, y + 3),
                        new Point(x + 4, y + 3),
                        new Point(x + 6, y + 12),
                        new Point(x + 15, y + 12),
                        new Point(x + 17, y + 6),
                        new Point(x + 5, y + 6)
                    });
                    g.FillEllipse(brush, x + 6, y + 14, 3, 3);
                    g.FillEllipse(brush, x + 13, y + 14, 3, 3);
                    break;

                case NavIconType.QrScan:
                    g.DrawArc(pen, x + 1, y + 1, 6, 6, 180, 90);
                    g.DrawArc(pen, x + w - 7, y + 1, 6, 6, 270, 90);
                    g.DrawArc(pen, x + w - 7, y + h - 7, 6, 6, 0, 90);
                    g.DrawArc(pen, x + 1, y + h - 7, 6, 6, 90, 90);
                    g.DrawLine(pen, x + 3, y + h / 2, x + w - 3, y + h / 2);
                    break;

                case NavIconType.Chart:
                    g.DrawLine(pen, x + 2, y + h - 2, x + w - 2, y + h - 2);
                    g.DrawLine(pen, x + 4, y + h - 2, x + 4, y + 9);
                    g.DrawLine(pen, x + 9, y + h - 2, x + 9, y + 4);
                    g.DrawLine(pen, x + 14, y + h - 2, x + 14, y + 7);
                    break;

                case NavIconType.Movie:
                    g.DrawRectangle(pen, x + 2, y + 3, w - 4, h - 6);
                    g.DrawLine(pen, x + 2, y + 7, x + w - 2, y + 7);
                    g.DrawLine(pen, x + 6, y + 3, x + 8, y + 7);
                    g.DrawLine(pen, x + 11, y + 3, x + 13, y + 7);
                    break;

                case NavIconType.Room:
                    g.DrawRectangle(pen, x + 3, y + 2, w - 6, h - 4);
                    g.FillEllipse(brush, x + w - 6, y + h / 2 - 1, 2, 2);
                    break;

                case NavIconType.Schedule:
                    g.DrawRectangle(pen, x + 2, y + 3, w - 4, h - 5);
                    g.DrawLine(pen, x + 2, y + 7, x + w - 2, y + 7);
                    g.DrawLine(pen, x + 6, y + 1, x + 6, y + 4);
                    g.DrawLine(pen, x + 12, y + 1, x + 12, y + 4);
                    break;

                case NavIconType.PriceTag:
                    g.DrawPolygon(pen, new[]
                    {
                        new Point(x + 2, y + 9),
                        new Point(x + 9, y + 2),
                        new Point(x + 16, y + 2),
                        new Point(x + 16, y + 9),
                        new Point(x + 9, y + 16)
                    });
                    g.FillEllipse(brush, x + 12, y + 5, 2, 2);
                    break;

                case NavIconType.Food:
                    g.DrawPolygon(pen, new[]
                    {
                        new Point(x + 4, y + 7),
                        new Point(x + 14, y + 7),
                        new Point(x + 12, y + 16),
                        new Point(x + 6, y + 16)
                    });
                    g.DrawArc(pen, x + 4, y + 3, 5, 5, 180, 180);
                    g.DrawArc(pen, x + 9, y + 3, 5, 5, 180, 180);
                    break;

                case NavIconType.Gift:
                    g.DrawRectangle(pen, x + 3, y + 8, 12, 8);
                    g.DrawRectangle(pen, x + 2, y + 5, 14, 3);
                    g.DrawLine(pen, x + 9, y + 5, x + 9, y + 16);
                    g.DrawArc(pen, x + 5, y + 2, 4, 3, 0, 360);
                    g.DrawArc(pen, x + 9, y + 2, 4, 3, 0, 360);
                    break;

                case NavIconType.Users:
                    g.DrawEllipse(pen, x + 4, y + 3, 5, 5);
                    g.DrawArc(pen, x + 2, y + 10, 9, 7, 180, 180);
                    g.DrawEllipse(pen, x + 11, y + 4, 4, 4);
                    g.DrawArc(pen, x + 10, y + 11, 7, 6, 180, 180);
                    break;

                case NavIconType.Home:
                    g.DrawLines(pen, new[]
                    {
                        new Point(x + 2, y + 8),
                        new Point(x + 9, y + 2),
                        new Point(x + 16, y + 8)
                    });
                    g.DrawRectangle(pen, x + 4, y + 8, 10, 8);
                    break;

                case NavIconType.Ticket:
                    g.DrawRectangle(pen, x + 2, y + 4, w - 4, h - 8);
                    g.DrawLine(pen, x + 7, y + 4, x + 7, y + h - 4);
                    break;

                case NavIconType.Seat:
                    g.DrawRectangle(pen, x + 4, y + 3, 10, 7);
                    g.DrawLine(pen, x + 3, y + 11, x + 15, y + 11);
                    g.DrawLine(pen, x + 5, y + 11, x + 5, y + 16);
                    g.DrawLine(pen, x + 13, y + 11, x + 13, y + 16);
                    break;

                case NavIconType.Payment:
                    g.DrawRectangle(pen, x + 2, y + 4, w - 4, h - 8);
                    g.DrawLine(pen, x + 2, y + 8, x + w - 2, y + 8);
                    break;

                case NavIconType.Logout:
                    g.DrawLines(pen, new[]
                    {
                        new Point(x + 8, y + 2),
                        new Point(x + 3, y + 2),
                        new Point(x + 3, y + 16),
                        new Point(x + 8, y + 16)
                    });
                    g.DrawLine(pen, x + 7, y + 9, x + 16, y + 9);
                    g.DrawLines(pen, new[]
                    {
                        new Point(x + 13, y + 6),
                        new Point(x + 16, y + 9),
                        new Point(x + 13, y + 12)
                    });
                    break;
            }
        }

        // =========================================================
        // 5. SWITCH VIEW
        // =========================================================

        public void SwitchView(string title, UserControl control)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            lblHeaderTitle.Text = title;

            // Giải phóng màn hình cũ.
            foreach (Control c in panelContent.Controls)
            {
                c.Dispose();
            }

            panelContent.Controls.Clear();

            // Hiển thị màn hình mới.
            control.Dock = DockStyle.Fill;
            panelContent.Controls.Add(control);
            control.BringToFront();
            _activeView = control;

            if (control is not DashboardUC)
                ThemeColorApplicator.Apply(control, _isDarkTheme);

            ApplyActiveViewTheme(_isDarkTheme);
        }

        private void ApplyActiveViewTheme(bool isDark)
        {
            if (_activeView is HomeUC home)
                home.SetTheme(isDark);
            else if (_activeView is TicketScannerUC scanner)
                scanner.SetTheme(isDark);
        }

        // =========================================================
        // 6. RESPONSIVE LAYOUT
        // =========================================================

        /// <summary>
        /// Tự điều chỉnh chiều rộng Sidebar theo cửa sổ.
        /// Không thay đổi Font của các control.
        /// </summary>
        
        private void UpdateResponsiveLayout()
        {
            if (panelSidebar == null || panelSidebar.IsDisposed)
                return;

            int availableWidth = ClientSize.Width;

            // Sidebar chiếm khoảng 18% chiều rộng cửa sổ.
            int sidebarWidth = (int)(availableWidth * 0.18f);

            // Giới hạn để Sidebar không chiếm quá nhiều diện tích.
            if (availableWidth < 1100)
            {
                sidebarWidth = Math.Clamp(sidebarWidth, 180, 210);
            }
            else
            {
                sidebarWidth = Math.Clamp(sidebarWidth, 210, 250);
            }

            panelSidebar.Width = sidebarWidth;
        }

        private int ScaleForDpi(int value)
        {
            return (int)Math.Round(value * DeviceDpi / 96F);
        }

        private void UpdateBrandHeaderLayout()
        {
            if (pnlBrand == null || lblBrandTitle == null || pnlBrand.IsDisposed)
                return;

            int titleLeft = ScaleForDpi(62);
            int rightPadding = ScaleForDpi(16);
            int availableWidth = Math.Max(
                0,
                pnlBrand.ClientSize.Width - titleLeft - rightPadding);
            int measuredWidth = TextRenderer.MeasureText(
                lblBrandTitle.Text,
                lblBrandTitle.Font,
                Size.Empty,
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
            int titleWidth = Math.Min(measuredWidth, availableWidth);
            var titleBounds = new Rectangle(
                titleLeft,
                ScaleForDpi(13),
                titleWidth,
                ScaleForDpi(25));

            if (lblBrandTitle.Bounds != titleBounds)
                lblBrandTitle.Bounds = titleBounds;
        }


        private void MainForm_SizeChanged(object? sender, EventArgs e)
        {
            if (!_responsiveReady || IsDisposed)
                return;

            UpdateResponsiveLayout();
        }

        // =========================================================
        // 7. CUSTOMER BOOKING FLOW
        // =========================================================

        public void OpenHomeView()
        {
            var homeUC = new HomeUC();

            homeUC.OnMovieClicked += (s, selectedMovie) =>
            {
                _bookingContext.SetMovie(selectedMovie);
                OpenMovieDetailView(selectedMovie);
            };

            homeUC.OnBookNowClicked += (s, selectedMovie) =>
            {
                _bookingContext.SetMovie(selectedMovie);
                OpenShowtimeView(selectedMovie);
            };

            SetActiveNav(_navCustomerHome);
            SwitchView("Trang chủ - Chọn phim", homeUC);
        }

        public void OpenMovieDetailView(Movie movie)
        {
            if (movie == null)
                return;

            _bookingContext.SetMovie(movie);

            var detailUC = new MovieDetailUC(movie);

            detailUC.OnBookTicketClicked += (s, selectedMovie) =>
            {
                _bookingContext.SetMovie(selectedMovie);
                OpenShowtimeView(selectedMovie);
            };

            detailUC.OnBackClicked += (s, e) =>
            {
                OpenHomeView();
            };

            SetActiveNav(_navCustomerHome);
            SwitchView($"Chi tiết phim: {movie.Title}", detailUC);
        }

        public void OpenShowtimeView(Movie? movie = null)
        {
            if (movie != null)
            {
                _bookingContext.SetMovie(movie);
            }

            var showtimeUC = _bookingContext.Movie != null
                ? new ShowtimeUC(_bookingContext.Movie)
                : new ShowtimeUC();

            showtimeUC.OnShowtimeSelected += (s, selectedShowtime) =>
            {
                Movie? selectedMovie =
                    showtimeUC.MovieData ?? _bookingContext.Movie;

                if (selectedMovie == null)
                {
                    MessageBox.Show(
                        "Không tìm thấy thông tin phim.",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                _bookingContext.SetMovie(selectedMovie);
                _bookingContext.SetShowtime(selectedShowtime);

                OpenSeatSelectionView();
            };

            showtimeUC.OnBackClicked += (s, e) =>
            {
                if (_bookingContext.Movie != null)
                {
                    OpenMovieDetailView(_bookingContext.Movie);
                }
                else
                {
                    OpenHomeView();
                }
            };

            string header = _bookingContext.Movie != null
                ? $"Chọn suất chiếu: {_bookingContext.Movie.Title}"
                : "Lịch chiếu phim";

            SetActiveNav(_navCustomerShowtime);
            SwitchView(header, showtimeUC);
        }

        public void OpenSeatSelectionView(
            Movie? movie = null,
            Showtime? showtime = null)
        {
            if (movie != null)
            {
                _bookingContext.SetMovie(movie);
            }

            if (showtime != null)
            {
                _bookingContext.SetShowtime(showtime);
            }

            if (!_bookingContext.HasShowtime)
            {
                MessageBox.Show(
                    "Vui lòng chọn phim và suất chiếu trước.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                OpenShowtimeView(_bookingContext.Movie);
                return;
            }

            var seatUC = new SeatSelectionUC(_bookingContext);

            seatUC.OnContinueToFood += (s, seatData) =>
            {
                _bookingContext.SetSeats(seatData.seats);
                _bookingContext.SetTicketAmount(seatData.price);

                if (!_bookingContext.HasSeats ||
                    _bookingContext.TicketAmount <= 0)
                {
                    MessageBox.Show(
                        "Thông tin ghế hoặc giá vé không hợp lệ.",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                OpenFoodSelectionView();
            };

            seatUC.OnBackClicked += (s, e) =>
            {
                OpenShowtimeView(_bookingContext.Movie);
            };

            SetActiveNav(_navCustomerSeat);
            SwitchView($"Chọn ghế - {_bookingContext.Movie!.Title}", seatUC);
        }

        private void OpenFoodSelectionView()
        {
            if (!_bookingContext.HasSeats)
            {
                MessageBox.Show(
                    "Bạn cần chọn ghế trước khi chọn bắp nước.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                OpenSeatSelectionView();
                return;
            }

            var foodUC = new FoodSelectionUC(_bookingContext);

            foodUC.OnContinueToCheckout += (s, cartData) =>
            {
                _bookingContext.SetFoodCart(cartData.foodCart);
                OpenCheckoutView();
            };

            foodUC.OnBackClicked += (s, e) =>
            {
                OpenSeatSelectionView();
            };

            SetActiveNav(_navCustomerFood);
            SwitchView("Chọn bắp nước & Combo", foodUC);
        }

        // ---------------------------------------------------------
        // STEP 5: CHECKOUT
        // ---------------------------------------------------------

        private void OpenCheckoutView()
        {
            if (!_bookingContext.CanCheckout)
            {
                MessageBox.Show(
                    "Vui lòng hoàn thành các bước chọn phim, suất chiếu và ghế.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                if (!_bookingContext.HasShowtime)
                {
                    OpenShowtimeView(_bookingContext.Movie);
                }
                else
                {
                    OpenSeatSelectionView();
                }

                return;
            }

            var checkoutUC = new CheckoutUC();
            checkoutUC.SetBookingContext(_bookingContext);

            checkoutUC.OnBackClicked += (s, e) =>
            {
                OpenFoodSelectionView();
            };

            checkoutUC.OnPaymentCompleted += (s, result) =>
            {
                if (result == null)
                    return;

                try
                {
                    // 1. Lưu đơn đặt vé, từng ghế và sản phẩm bắp nước vào SQLite.
                    // BookingService chỉ trả về khi giao dịch lưu thành công.
                    var bookingService = new BookingService();
                    long bookingId = bookingService.CreateBooking(result);

                    if (bookingId <= 0)
                    {
                        throw new InvalidOperationException(
                            "Không thể lưu đơn đặt vé vào cơ sở dữ liệu.");
                    }

                    // 2. Xóa ngay dữ liệu tạm của lượt đặt vé vừa hoàn tất
                    // để tránh còn lưu ghế/giỏ hàng cũ nếu khách bấm sang menu khác.
                    _bookingContext.Clear();

                    // 3. Chuyển sang màn hình vé điện tử.
                    OpenTicketView(result);
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(
                        ex.Message,
                        "Không thể hoàn tất đơn hàng",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    // Nếu suất chiếu đã qua giờ thì đưa khách về chọn lại suất chiếu
                    if (_bookingContext.Showtime != null &&
                        _bookingContext.Showtime.StartTime <= DateTime.Now)
                    {
                        Movie? currentMovie = _bookingContext.Movie;
                        _bookingContext.ClearAfterMovieChange();
                        OpenShowtimeView(currentMovie);
                        return;
                    }

                    // Nếu có ghế vừa bị người khác đặt thì đưa khách về sơ đồ ghế để chọn lại
                    if (_bookingContext.Showtime != null &&
                        _bookingContext.Seats.Count > 0)
                    {
                        try
                        {
                            var bookedSeats = new TicketService()
                                .GetBookedSeatNumbers(_bookingContext.Showtime.Id);

                            if (_bookingContext.Seats.Any(s => bookedSeats.Contains(s)))
                            {
                                OpenSeatSelectionView();
                            }
                        }
                        catch
                        {
                            // Giữ người dùng ở lại màn thanh toán với nút đã mở khóa
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Lỗi xử lý đơn hàng:\n" + ex.Message,
                        "Lỗi",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            };

            SetActiveNav(_navCustomerCheckout);
            SwitchView("Thanh toán", checkoutUC);
        }

        public void OpenTicketView(BookingResultEventArgs? bookingData = null)
        {
            if (bookingData == null)
            {
                if (currentRole == UserRole.Customer)
                {
                    try
                    {
                        bookingData = new BookingService().GetCurrentUserLatestBooking();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "Không thể tải thông tin vé từ cơ sở dữ liệu:\n" + ex.Message,
                            "Lỗi tải vé",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }
                }
                else
                {
                    bookingData = UserSession.LatestBooking;
                }
            }

            if (bookingData == null)
            {
                MessageBox.Show(
                    currentRole == UserRole.Customer
                        ? "Tài khoản của bạn chưa có vé nào đã đặt."
                        : "Không tìm thấy thông tin vé.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                if (currentRole == UserRole.Staff ||
                    currentRole == UserRole.Admin)
                {
                    OpenPOSView();
                }
                else
                {
                    OpenHomeView();
                }

                return;
            }

            var ticketUC = new TicketUC(bookingData);

            ticketUC.OnGoHomeClicked += (s, e) =>
            {
                _bookingContext.Clear();

                if (currentRole == UserRole.Staff ||
                    currentRole == UserRole.Admin)
                {
                    OpenPOSView();
                }
                else
                {
                    OpenHomeView();
                }
            };

            if (currentRole == UserRole.Customer)
            {
                SetActiveNav(_navCustomerTicket);
            }

            SwitchView("Hoàn tất & Xuất vé điện tử", ticketUC);
        }

        // =========================================================
        // 8. STAFF / ADMIN POS
        // =========================================================

        public void OpenPOSView()
        {
            var posUC = new POSUC();

            posUC.OnShowtimeSelected += (s, data) =>
            {
                lblHeaderTitle.Text =
                    $"Bán vé tại quầy - {data.Movie.Title} ({data.Showtime.StartTime:HH:mm})";
            };

            posUC.OnSeatsSelected += (s, seats) =>
            {
                lblHeaderTitle.Text =
                    $"Bán vé tại quầy - Đã chọn {seats.Count} ghế: {string.Join(", ", seats)}";
            };

            posUC.OnOrderCleared += (s, e) =>
            {
                lblHeaderTitle.Text = "Bán vé tại quầy (POS)";
            };

            posUC.OnPaymentCompleted += (s, result) =>
            {
                lblHeaderTitle.Text =
                    $"Bán vé tại quầy - Đã thu {result.TotalAmount:N0} đ (Mã {result.BookingCode})";
            };

            posUC.OnPrintTicketClicked += (s, result) =>
            {
                OpenTicketView(result);
            };

            posUC.OnOpenScannerClicked += (s, code) =>
            {
                OpenTicketScannerView(code);
            };

            SetActiveNav(_navStaffPos);
            SwitchView("Bán vé tại quầy (POS)", posUC);
        }

        // =========================================================
        // 9. TICKET SCANNER
        // =========================================================

        public void OpenTicketScannerView(string? prefillCode = null)
        {
            var scannerUC = new TicketScannerUC(prefillCode);

            scannerUC.OnTicketScanned += (s, result) =>
            {
                string state = result.Status switch
                {
                    TicketScanStatus.Valid => "✔ Hợp lệ",
                    TicketScanStatus.AlreadyUsed => "⚠ Vé trùng",
                    TicketScanStatus.Expired => "✖ Hết hạn",
                    TicketScanStatus.WrongShowtime => "✖ Sai suất chiếu",
                    _ => "✖ Không tồn tại"
                };

                lblHeaderTitle.Text =
                    $"Soát vé QR Code - {result.Code}: {state}";
            };

            scannerUC.OnTicketAccepted += (s, result) =>
            {
                int room = result.Booking?.Showtime?.RoomId ?? 1;
                var seats = result.Booking?.Seats ?? new List<string>();

                lblHeaderTitle.Text =
                    $"Soát vé - Mời khách vào Phòng 0{room} (Ghế {string.Join(", ", seats)})";
            };

            scannerUC.OnTicketRejected += (s, result) =>
            {
                if (result.Status == TicketScanStatus.AlreadyUsed)
                {
                    MessageBox.Show(
                        $"CẢNH BÁO VÉ TRÙNG!\n\n" +
                        $"Mã {result.Code} đã được soát lúc " +
                        $"{result.FirstCheckInTime:HH:mm:ss}.\n" +
                        "Vui lòng kiểm tra lại với khách hàng.",
                        "Soát vé",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            };

            scannerUC.OnScanNextClicked += (s, e) =>
            {
                lblHeaderTitle.Text = "Soát vé QR Code";
            };

            scannerUC.OnOpenPOSClicked += (s, e) =>
            {
                OpenPOSView();
            };

            SetActiveNav(_navStaffScanner);
            SwitchView("Soát vé QR Code", scannerUC);
        }
    }
}