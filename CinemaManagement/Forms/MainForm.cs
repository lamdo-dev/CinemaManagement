using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CinemaManagement.Forms.Dialogs;
using CinemaManagement.Models;
using CinemaManagement.Models.Enums;
using CinemaManagement.UserControls.Admin;
using CinemaManagement.UserControls.Customer;
using CinemaManagement.UserControls.Staff;
using CinemaManagement.Utils;

namespace CinemaManagement;

public partial class MainForm : Form
{
    private Panel panelSidebar = null!;
    private Panel panelContent = null!;
    private Label lblHeaderTitle = null!;
    private UserRole currentRole;

    // Trạng thái lưu giữ xuyên suốt luồng đặt vé của khách hàng (Customer Booking Flow State)
    private Movie _currentMovie = new Movie
    {
        Id = 1,
        Title = "Spider-Man: Across The Spider-Verse",
        Genre = "Action",
        DurationMinutes = 140
    };

    private Showtime _currentShowtime = new Showtime
    {
        Id = 1,
        MovieId = 1,
        RoomId = 1,
        StartTime = DateTime.Today.AddHours(19).AddMinutes(30),
        BasePrice = 90000m
    };

    private List<string> _currentSeats = new List<string>();
    private decimal _currentTicketAmount = 0m;
    private Dictionary<int, (string Name, decimal Price, int Qty, string ImgPath)> _currentFoodCart = new();
    private decimal _currentFoodAmount = 0m;

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
        string userDisplay = UserSession.CurrentUser?.FullName ?? currentRole.ToString();
        this.Text = $"HỆ THỐNG QUẢN LÝ RẠP CHIẾU PHIM - [{currentRole.ToString().ToUpper()} : {userDisplay}]";
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
            AddNavButton("🛒 Bán vé tại quầy (POS)", () => OpenPOSView());
            AddNavButton("📷 Quét mã QR soát vé", () => OpenTicketScannerView());
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

        // 3. KHÁCH HÀNG & ADMIN: LUỒNG ĐẶT VÉ TRỰC TUYẾN (CUSTOMER BOOKING FLOW)
        if (currentRole == UserRole.Admin || currentRole == UserRole.Customer)
        {
            AddNavGroup("--- KHÁCH HÀNG (CUSTOMER) ---");
            AddNavButton("🏠 Trang chủ Phim", () => OpenHomeView());
            AddNavButton("📅 Lịch chiếu", () => OpenShowtimeView(null));
            AddNavButton("🎟️ Đặt vé liên hoàn (Flow)", () => StartBookingFlow());
            AddNavButton("💺 Chọn ghế đặt vé", () => OpenSeatSelectionView(_currentMovie, _currentShowtime));
            AddNavButton("🍿 Chọn bắp nước", () => OpenFoodSelectionView(_currentticketAmountOrDefault()));
            AddNavButton("💳 Thanh toán", () => OpenCheckoutView());
            AddNavButton("🎟️ Vé của tôi", () => OpenTicketView(UserSession.LatestBooking));
        }

        // 4. NÚT ĐĂNG XUẤT HỆ THỐNG
        AddNavGroup("--- HỆ THỐNG ---");
        AddNavButton("🚪 Đăng xuất", () =>
        {
            UserSession.CurrentUser = null;
            this.Hide();
            var loginForm = new LoginForm();
            loginForm.FormClosed += (s, args) => this.Close();
            loginForm.Show();
        });

        this.Controls.Add(panelContent);
        this.Controls.Add(panelHeader);
        this.Controls.Add(panelSidebar);

        // Mở màn hình mặc định theo vai trò khi vừa đăng nhập thành công
        if (currentRole == UserRole.Customer)
            OpenHomeView();
        else if (currentRole == UserRole.Staff)
            OpenPOSView();
        else
            SwitchView("📊 Dashboard Doanh thu & Suất chiếu", new DashboardUC());
    }

    private decimal _currentticketAmountOrDefault() =>
        _currentTicketAmount > 0 ? _currentTicketAmount : 90000m;

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

    // =========================================================
    // LUỒNG ĐẶT VÉ KHÁCH HÀNG (CUSTOMER BOOKING FLOW)
    // Đăng nhập/Đăng ký -> Chọn phim (HomeUC / MovieDetailUC / ShowtimeUC)
    // -> Chọn ghế (SeatSelectionUC) -> Chọn bắp nước (FoodSelectionUC)
    // -> Xác nhận & Thanh toán (ConfirmBookingDialog) -> Xuất vé (TicketUC)
    // =========================================================

    // BƯỚC 1A: Màn hình Trang chủ Chọn phim (HomeUC)
    public void OpenHomeView()
    {
        var homeUC = new HomeUC();

        // Click vào thẻ phim -> Xem chi tiết phim (MovieDetailUC)
        homeUC.OnMovieClicked += (s, selectedMovie) =>
        {
            _currentMovie = selectedMovie;
            OpenMovieDetailView(selectedMovie);
        };

        // Click nút "🎟 Đặt vé ngay" trên thẻ phim -> Sang thẳng chọn suất chiếu (ShowtimeUC)
        homeUC.OnBookNowClicked += (s, selectedMovie) =>
        {
            _currentMovie = selectedMovie;
            OpenShowtimeView(selectedMovie);
        };

        SwitchView("🏠 Trang chủ - Chọn phim", homeUC);
    }

    // BƯỚC 1B: Màn hình Chi tiết phim (MovieDetailUC)
    public void OpenMovieDetailView(Movie movie)
    {
        _currentMovie = movie;
        var detailUC = new MovieDetailUC(movie);

        // Bấm "🎟 ĐẶT VÉ NGAY" -> Chuyển sang Chọn suất chiếu (ShowtimeUC)
        detailUC.OnBookTicketClicked += (s, selectedMovie) =>
        {
            _currentMovie = selectedMovie;
            OpenShowtimeView(selectedMovie);
        };

        // Bấm "← Quay lại" -> Về Trang chủ (HomeUC)
        detailUC.OnBackClicked += (s, e) =>
        {
            OpenHomeView();
        };

        SwitchView($"🎬 Chi tiết phim: {movie.Title}", detailUC);
    }

    // BƯỚC 1C: Màn hình Chọn suất chiếu (ShowtimeUC)
    public void OpenShowtimeView(Movie? movie = null)
    {
        if (movie != null)
        {
            _currentMovie = movie;
        }

        var showtimeUC = movie != null ? new ShowtimeUC(movie) : new ShowtimeUC();

        // Click chọn giờ chiếu -> Chuyển sang Chọn ghế (SeatSelectionUC)
        showtimeUC.OnShowtimeSelected += (s, selectedShowtime) =>
        {
            if (showtimeUC.MovieData != null)
            {
                _currentMovie = showtimeUC.MovieData;
            }
            _currentShowtime = selectedShowtime;
            OpenSeatSelectionView(_currentMovie, _currentShowtime);
        };

        // Click "← Quay lại" -> Về trang trước
        showtimeUC.OnBackClicked += (s, e) =>
        {
            if (movie != null)
                OpenMovieDetailView(movie);
            else
                OpenHomeView();
        };

        string header = movie != null
            ? $"📅 Chọn suất chiếu: {movie.Title}"
            : "📅 Lịch chiếu phim";
        SwitchView(header, showtimeUC);
    }

    // BƯỚC 2: Màn hình Chọn ghế ngồi (SeatSelectionUC)
    public void OpenSeatSelectionView(Movie? movie = null, Showtime? showtime = null)
    {
        if (movie != null) _currentMovie = movie;
        if (showtime != null) _currentShowtime = showtime;

        var seatUC = new SeatSelectionUC(_currentMovie, _currentShowtime);

        // Click "TIẾP TỤC (BẮP NƯỚC) →" -> Chuyển sang FoodSelectionUC
        seatUC.OnContinueToFood += (s, seatData) =>
        {
            _currentSeats = seatData.seats;
            _currentTicketAmount = seatData.price;
            OpenFoodSelectionView(_currentTicketAmount);
        };

        // Click "← Quay lại" -> Về màn hình Chọn suất chiếu (ShowtimeUC)
        seatUC.OnBackClicked += (s, e) =>
        {
            OpenShowtimeView(_currentMovie);
        };

        SwitchView($"💺 Chọn ghế ngồi - {_currentMovie.Title} ({_currentShowtime.StartTime:HH:mm dd/MM})", seatUC);
    }

    // BƯỚC 3: Màn hình Chọn bắp nước & Combo (FoodSelectionUC)
    private void OpenFoodSelectionView(decimal ticketTotalAmount = 0m)
    {
        if (ticketTotalAmount > 0)
        {
            _currentTicketAmount = ticketTotalAmount;
        }

        var foodUC = new FoodSelectionUC(_currentTicketAmount);

        // Click "TIẾP TỤC THANH TOÁN →" -> Mở hộp thoại Xác nhận & Thanh toán (ConfirmBookingDialog)
        foodUC.OnContinueToCheckout += (s, cartData) =>
        {
            _currentFoodCart = cartData.foodCart;
            _currentFoodAmount = cartData.totalFoodAmount;

            ShowConfirmBookingDialogAndComplete();
        };

        // Click "← Quay lại" -> Về màn hình Chọn ghế (SeatSelectionUC)
        foodUC.OnBackClicked += (s, e) =>
        {
            OpenSeatSelectionView(_currentMovie, _currentShowtime);
        };

        SwitchView("🍿 Chọn bắp nước & Combo", foodUC);
    }

    // BƯỚC 4: Hộp thoại Xác nhận & Thanh toán (ConfirmBookingDialog) -> Hoàn tất & Xuất vé (TicketUC)
    private void ShowConfirmBookingDialogAndComplete()
    {
        if (_currentSeats.Count == 0)
        {
            _currentSeats = new List<string> { "E5", "E6" };
            if (_currentTicketAmount <= 0) _currentTicketAmount = 180000m;
        }

        using var confirmDialog = new ConfirmBookingDialog(
            _currentMovie,
            _currentShowtime,
            _currentSeats,
            _currentTicketAmount,
            _currentFoodCart,
            _currentFoodAmount);

        if (confirmDialog.ShowDialog(this) == DialogResult.OK)
        {
            var bookingResult = confirmDialog.ConfirmedBookingResult;

            MessageBox.Show(
                $"🎉 Thanh toán thành công! Đã tạo hóa đơn và xuất vé điện tử.\nMã đơn hàng: {bookingResult?.BookingCode}",
                "Hoàn tất đặt vé",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            OpenTicketView(bookingResult);
        }
    }

    // Màn hình Thanh toán chi tiết (CheckoutUC) khi truy cập từ Sidebar
    public void OpenCheckoutView()
    {
        if (_currentSeats.Count == 0)
        {
            _currentSeats = new List<string> { "E5", "E6" };
            if (_currentTicketAmount <= 0) _currentTicketAmount = 180000m;
        }

        var checkoutUC = new CheckoutUC();
        checkoutUC.SetBookingData(
            _currentMovie,
            _currentShowtime,
            _currentSeats,
            _currentTicketAmount,
            _currentFoodCart,
            _currentFoodAmount);

        checkoutUC.OnBackClicked += (s, e) =>
        {
            OpenFoodSelectionView(_currentTicketAmount);
        };

        checkoutUC.OnPaymentCompleted += (s, result) =>
        {
            ShowConfirmBookingDialogAndComplete();
        };

        SwitchView("💳 Xác nhận & Thanh toán hóa đơn", checkoutUC);
    }

    // BƯỚC 5: Màn hình Hoàn tất & Xuất vé (TicketUC)
    public void OpenTicketView(BookingResultEventArgs? bookingData = null)
    {
        var ticketUC = bookingData != null
            ? new TicketUC(bookingData)
            : new TicketUC();

        // Click "🏠 Về Trang Chủ" -> Quay về HomeUC để đặt vé mới
        ticketUC.OnGoHomeClicked += (s, e) =>
        {
            OpenHomeView();
        };

        SwitchView("🎟️ Hoàn tất & Xuất vé điện tử", ticketUC);
    }

    // Khởi chạy luồng đặt vé liên hoàn
    private void StartBookingFlow()
    {
        OpenHomeView();
    }

    // =========================================================
    // LUỒNG QUẢN TRỊ / NHÂN VIÊN (ADMIN & STAFF FLOW)
    // Bán vé tại quầy (POSUC) <-> Soát vé vào phòng chiếu (TicketScannerUC)
    // =========================================================

    // Màn hình Bán vé tại quầy (POSUC)
    public void OpenPOSView()
    {
        var posUC = new POSUC();

        // Chọn suất chiếu -> cập nhật tiêu đề
        posUC.OnShowtimeSelected += (s, data) =>
        {
            lblHeaderTitle.Text = $"🛒 Bán vé tại quầy - {data.Movie.Title} ({data.Showtime.StartTime:HH:mm})";
        };

        // Chọn ghế xong -> hiển thị số ghế trên tiêu đề
        posUC.OnSeatsSelected += (s, seats) =>
        {
            lblHeaderTitle.Text = $"🛒 Bán vé tại quầy - Đã chọn {seats.Count} ghế: {string.Join(", ", seats)}";
        };

        // Hủy đơn -> trả lại tiêu đề mặc định
        posUC.OnOrderCleared += (s, e) =>
        {
            lblHeaderTitle.Text = "🛒 Bán vé tại quầy (POS)";
        };

        // Thanh toán thành công -> ghi nhận đơn
        posUC.OnPaymentCompleted += (s, result) =>
        {
            lblHeaderTitle.Text = $"🛒 Bán vé tại quầy - Đã thu {result.TotalAmount:N0} đ (Mã {result.BookingCode})";
        };

        // In vé -> mở màn hình vé điện tử cho khách xem / in
        posUC.OnPrintTicketClicked += (s, result) =>
        {
            OpenTicketView(result);
        };

        // Bấm "📷 Soát vé" -> chuyển sang màn hình soát vé, điền sẵn mã vé gần nhất
        posUC.OnOpenScannerClicked += (s, code) =>
        {
            OpenTicketScannerView(code);
        };

        SwitchView("🛒 Bán vé tại quầy (POS)", posUC);
    }

    // Màn hình Soát vé vào phòng chiếu (TicketScannerUC)
    public void OpenTicketScannerView(string? prefillCode = null)
    {
        var scannerUC = new TicketScannerUC(prefillCode);

        // Mỗi lần quét -> cập nhật tiêu đề theo kết quả
        scannerUC.OnTicketScanned += (s, result) =>
        {
            string state = result.Status switch
            {
                TicketScanStatus.Valid => "✔ Hợp lệ",
                TicketScanStatus.AlreadyUsed => "⚠ Vé trùng",
                TicketScanStatus.Expired => "✖ Hết hạn",
                _ => "✖ Không tồn tại"
            };
            lblHeaderTitle.Text = $"📷 Soát vé QR Code - {result.Code}: {state}";
        };

        // Vé hợp lệ -> mời khách vào phòng
        scannerUC.OnTicketAccepted += (s, result) =>
        {
            int room = result.Booking?.Showtime?.RoomId ?? 1;
            lblHeaderTitle.Text = $"📷 Soát vé - Mời khách vào Phòng 0{room} (Ghế {string.Join(", ", result.Booking?.Seats ?? new List<string>())})";
        };

        // Vé bị từ chối -> cảnh báo nhân viên
        scannerUC.OnTicketRejected += (s, result) =>
        {
            if (result.Status == TicketScanStatus.AlreadyUsed)
            {
                MessageBox.Show(
                    $"CẢNH BÁO VÉ TRÙNG!\n\nMã {result.Code} đã được soát lúc {result.FirstCheckInTime:HH:mm:ss}.\nVui lòng kiểm tra lại với khách hàng.",
                    "Soát vé", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        scannerUC.OnScanNextClicked += (s, e) =>
        {
            lblHeaderTitle.Text = "📷 Soát vé QR Code";
        };

        // Bấm "🛒 Về quầy bán vé" -> quay lại POS
        scannerUC.OnOpenPOSClicked += (s, e) =>
        {
            OpenPOSView();
        };

        SwitchView("📷 Soát vé QR Code", scannerUC);
    }
}
