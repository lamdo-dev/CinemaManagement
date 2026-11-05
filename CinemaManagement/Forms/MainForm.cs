
using System;
using System.Drawing;
using System.Windows.Forms;
using CinemaManagement.UserControls.Admin;

namespace CinemaManagement;

public partial class MainForm : Form
{
    // Các khu vực giao diện
    private Panel sidebar = null!;
    private Panel mainContent = null!;
    private Panel header = null!;
    private Panel pageHost = null!;

    private Label lblPageTitle = null!;
    private Label lblUser = null!;

    // Các nút menu
    private Button btnDashboard = null!;
    private Button btnMovies = null!;
    private Button btnRooms = null!;
    private Button btnShowtimes = null!;
    private Button btnPricing = null!;
    private Button btnFood = null!;
    private Button btnPromotion = null!;

    // Màu giao diện
    private readonly Color backgroundColor =
        Color.FromArgb(15, 23, 42);

    private readonly Color sidebarColor =
        Color.FromArgb(17, 24, 39);

    private readonly Color panelColor =
        Color.FromArgb(30, 41, 59);

    private readonly Color accentColor =
        Color.FromArgb(37, 99, 235);

    private readonly Color textColor =
        Color.FromArgb(226, 232, 240);

    public MainForm()
    {
        InitializeComponent();

        Text = "Cinema Management";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1100, 650);
        BackColor = backgroundColor;
        Font = new Font("Segoe UI", 10);

        BuildInterface();
        ShowDashboard();
        SelectMenu(btnDashboard);
    }

   
    // 1. XÂY DỰNG GIAO DIỆN CHÍNH
  

    private void BuildInterface()
    {
        // Sidebar
        sidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 235,
            BackColor = sidebarColor
        };

        // Khu vực chính
        mainContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = backgroundColor
        };

        // Header
        header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 75,
            BackColor = panelColor
        };

        lblPageTitle = new Label
        {
            Text = "Dashboard",
            Font = new Font("Segoe UI", 19, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(25, 20)
        };

        lblUser = new Label
        {
            Text = "Administrator",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = textColor,
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        header.Controls.Add(lblPageTitle);
        header.Controls.Add(lblUser);

        // Tự căn chỉnh thông tin tài khoản bên phải
        header.Resize += (s, e) =>
        {
            lblUser.Location = new Point(
                header.ClientSize.Width - lblUser.Width - 25,
                28
            );
        };

        // Vùng hiển thị các trang
        pageHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = backgroundColor,
            Padding = new Padding(25)
        };

        mainContent.Controls.Add(pageHost);
        mainContent.Controls.Add(header);

        // Xây dựng Sidebar
        BuildSidebar();

        // Ghép vào Form
        Controls.Add(mainContent);
        Controls.Add(sidebar);
    }

    
    // 2. XÂY DỰNG SIDEBAR
   

    private void BuildSidebar()
    {
        // Logo
        Panel logoPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 115,
            BackColor = sidebarColor
        };

        Label lblLogo = new Label
        {
            Text = "CINEMA",
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 55
        };

        Label lblLogoSub = new Label
        {
            Text = "MANAGEMENT SYSTEM",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(148, 163, 184),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 30
        };

        logoPanel.Controls.Add(lblLogoSub);
        logoPanel.Controls.Add(lblLogo);

        // Menu
        FlowLayoutPanel menuPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(10, 15, 10, 10),
            BackColor = sidebarColor
        };

        btnDashboard = CreateMenuButton("▦", "Dashboard");
        btnMovies = CreateMenuButton("▣", "Quản lý phim");
        btnRooms = CreateMenuButton("▤", "Quản lý phòng chiếu");
        btnShowtimes = CreateMenuButton("◷", "Quản lý suất chiếu");
        btnPricing = CreateMenuButton("＄", "Quản lý giá vé");
        btnFood = CreateMenuButton("♧", "Quản lý đồ ăn");
        btnPromotion = CreateMenuButton("☆", "Quản lý khuyến mãi");

        menuPanel.Controls.Add(btnDashboard);
        menuPanel.Controls.Add(btnMovies);
        menuPanel.Controls.Add(btnRooms);
        menuPanel.Controls.Add(btnShowtimes);
        menuPanel.Controls.Add(btnPricing);
        menuPanel.Controls.Add(btnFood);
        menuPanel.Controls.Add(btnPromotion);

        // Thông tin cuối Sidebar
        Panel bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 65,
            BackColor = sidebarColor
        };

        Label lblVersion = new Label
        {
            Text = "Cinema Management v1.0",
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.FromArgb(100, 116, 139),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };

        bottomPanel.Controls.Add(lblVersion);

        sidebar.Controls.Add(menuPanel);
        sidebar.Controls.Add(bottomPanel);
        sidebar.Controls.Add(logoPanel);

        // Sự kiện menu
        btnDashboard.Click += (s, e) =>
        {
            SelectMenu(btnDashboard);
            ShowDashboard();
        };

        btnMovies.Click += (s, e) =>
        {
            SelectMenu(btnMovies);
            ShowMovieManagement();
        };

        btnRooms.Click += (s, e) =>
        {
            SelectMenu(btnRooms);
            ShowRoomManagement();
        };

        btnShowtimes.Click += (s, e) =>
        {
            SelectMenu(btnShowtimes);
            ShowShowtimeManagement();
        };

        btnPricing.Click += (s, e) =>
        {
            SelectMenu(btnPricing);
            ShowPlaceholder("Quản lý giá vé");
        };

        btnFood.Click += (s, e) =>
        {
            SelectMenu(btnFood);
            ShowPlaceholder("Quản lý đồ ăn và thức uống");
        };

        btnPromotion.Click += (s, e) =>
        {
            SelectMenu(btnPromotion);
            ShowPlaceholder("Quản lý khuyến mãi");
        };
    }

   
    private void ShowShowtimeManagement()
    {
        lblPageTitle.Text = "Quản lý suất chiếu";

        pageHost.Controls.Clear();

        ShowtimeManagementUC showtimeUC = new ShowtimeManagementUC
        {
            Dock = DockStyle.Fill
        };

        pageHost.Controls.Add(showtimeUC);
    }


    private void ShowRoomManagement()
    {
        lblPageTitle.Text = "Quản lý phòng chiếu";

        pageHost.Controls.Clear();

        RoomManagementUC roomUC = new RoomManagementUC
        {
            Dock = DockStyle.Fill
        };

        pageHost.Controls.Add(roomUC);
    }

    
    // 3. TẠO NÚT MENU

    private Button CreateMenuButton(string icon, string text)
    {
        Button button = new Button
        {
            Text = "   " + icon + "     " + text,
            Width = 205,
            Height = 47,
            Margin = new Padding(0, 4, 0, 4),
            FlatStyle = FlatStyle.Flat,
            BackColor = sidebarColor,
            ForeColor = textColor,
            Font = new Font("Segoe UI", 9.5f),
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = Cursors.Hand
        };

        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor =
            Color.FromArgb(30, 41, 59);

        return button;
    }

 
    // 4. CHỌN MENU
 

    private void SelectMenu(Button selectedButton)
    {
        foreach (Control control in sidebar.Controls)
        {
            if (control is FlowLayoutPanel menu)
            {
                foreach (Control item in menu.Controls)
                {
                    if (item is Button button)
                    {
                        button.BackColor = sidebarColor;
                        button.ForeColor = textColor;
                    }
                }
            }
        }

        selectedButton.BackColor = accentColor;
        selectedButton.ForeColor = Color.White;
    }

  
    // 5. DASHBOARD


    private void ShowDashboard()
    {
        lblPageTitle.Text = "Dashboard";

        pageHost.Controls.Clear();

        // Bố cục Dashboard
        TableLayoutPanel layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = backgroundColor,
            Padding = new Padding(0, 10, 0, 0)
        };

        layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 75));

        layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 165));

        layout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        // Tiêu đề Dashboard
        Panel welcomePanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = backgroundColor
        };

        Label lblWelcome = new Label
        {
            Text = "Tổng quan hệ thống",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(5, 0)
        };

        Label lblDescription = new Label
        {
            Text = "Theo dõi hoạt động kinh doanh của rạp chiếu phim",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(7, 38)
        };

        welcomePanel.Controls.Add(lblWelcome);
        welcomePanel.Controls.Add(lblDescription);

        // Bốn thẻ thống kê
        TableLayoutPanel cardLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = backgroundColor,
            Padding = new Padding(0, 5, 0, 10)
        };

        for (int i = 0; i < 4; i++)
        {
            cardLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 25));
        }

        cardLayout.Controls.Add(
            CreateStatisticCard(
                "TỔNG SỐ PHIM",
                "0",
                "Phim trong hệ thống",
                "▣",
                Color.FromArgb(37, 99, 235)
            ), 0, 0);

        cardLayout.Controls.Add(
            CreateStatisticCard(
                "PHÒNG CHIẾU",
                "0",
                "Phòng đang quản lý",
                "▤",
                Color.FromArgb(16, 185, 129)
            ), 1, 0);

        cardLayout.Controls.Add(
            CreateStatisticCard(
                "SUẤT CHIẾU",
                "0",
                "Suất chiếu hôm nay",
                "◷",
                Color.FromArgb(245, 158, 11)
            ), 2, 0);

        cardLayout.Controls.Add(
            CreateStatisticCard(
                "DOANH THU",
                "0 VNĐ",
                "Doanh thu hôm nay",
                "＄",
                Color.FromArgb(139, 92, 246)
            ), 3, 0);

        // Khu vực hoạt động
        Panel activityPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = panelColor,
            Padding = new Padding(20),
            Margin = new Padding(0, 10, 0, 0)
        };

        Label lblActivityTitle = new Label
        {
            Text = "HOẠT ĐỘNG HỆ THỐNG",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 20)
        };

        Label lblActivitySub = new Label
        {
            Text = "Theo dõi các hoạt động mới nhất",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(20, 52)
        };

        Panel separator = new Panel
        {
            BackColor = Color.FromArgb(51, 65, 85),
            Height = 1,
            Dock = DockStyle.Top
        };

        Label lblEmpty = new Label
        {
            Text = "Chưa có dữ liệu hoạt động.",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(20, 100)
        };

        activityPanel.Controls.Add(lblActivityTitle);
        activityPanel.Controls.Add(lblActivitySub);
        activityPanel.Controls.Add(lblEmpty);
        activityPanel.Controls.Add(separator);

        // Thêm các khu vực vào Dashboard
        layout.Controls.Add(welcomePanel, 0, 0);
        layout.Controls.Add(cardLayout, 0, 1);
        layout.Controls.Add(activityPanel, 0, 2);

        pageHost.Controls.Add(layout);
    }

   
    // 6. TẠO THẺ THỐNG KÊ


    private Panel CreateStatisticCard(
        string title,
        string value,
        string description,
        string icon,
        Color iconColor)
    {
        Panel card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = panelColor,
            Margin = new Padding(5, 5, 8, 5)
        };

        Panel iconPanel = new Panel
        {
            Size = new Size(42, 42),
            Location = new Point(15, 15),
            BackColor = Color.FromArgb(
                Math.Min(iconColor.R + 65, 255),
                Math.Min(iconColor.G + 65, 255),
                Math.Min(iconColor.B + 65, 255))
        };

        Label lblIcon = new Label
        {
            Text = icon,
            Font = new Font("Segoe UI", 17, FontStyle.Bold),
            ForeColor = iconColor,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };

        iconPanel.Controls.Add(lblIcon);

        Label lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(15, 72)
        };

        Label lblValue = new Label
        {
            Text = value,
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(15, 93)
        };

        Label lblDescription = new Label
        {
            Text = description,
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(15, 130)
        };

        card.Controls.Add(iconPanel);
        card.Controls.Add(lblTitle);
        card.Controls.Add(lblValue);
        card.Controls.Add(lblDescription);

        return card;
    }

  
    // 7. MỞ QUẢN LÝ PHIM
  

    private void ShowMovieManagement()
    {
        lblPageTitle.Text = "Quản lý phim";

        pageHost.Controls.Clear();

        MovieManagementUC movieUC = new MovieManagementUC
        {
            Dock = DockStyle.Fill
        };

        pageHost.Controls.Add(movieUC);
    }

 
    // 8. CÁC TRANG CHƯA HOÀN THIỆN
 

    private void ShowPlaceholder(string pageName)
    {
        lblPageTitle.Text = pageName;

        pageHost.Controls.Clear();

        Panel page = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = backgroundColor
        };

        Label lblTitle = new Label
        {
            Text = pageName,
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(10, 30)
        };

        Label lblDescription = new Label
        {
            Text = "Chức năng này sẽ được hoàn thiện trong phần tiếp theo.",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(12, 80)
        };

        page.Controls.Add(lblTitle);
        page.Controls.Add(lblDescription);

        pageHost.Controls.Add(page);
    }
}
