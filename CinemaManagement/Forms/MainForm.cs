
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Windows.Forms;

    using CinemaManagement.Models;
    using CinemaManagement.Models.Enums;
    using CinemaManagement.UserControls.Admin;
    using CinemaManagement.UserControls.Customer;
    using CinemaManagement.UserControls.Staff;
    using CinemaManagement.Utils;

    namespace CinemaManagement
    {
        public partial class MainForm : Form
        {
            // =========================================================
            // 1. FIELDS
            // =========================================================

            private Panel panelSidebar = null!;
            private Panel panelContent = null!;
            private Label lblHeaderTitle = null!;

            private UserRole currentRole;

            // Dữ liệu dùng chung trong toàn bộ quy trình đặt vé
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

                SetupFullUILayout();
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
            
            // 3. MAIN INTERFACE

            private void SetupFullUILayout()
            {
                string userDisplay =
                    UserSession.CurrentUser?.FullName
                    ?? currentRole.ToString();

                Text =
                    $"HỆ THỐNG QUẢN LÝ RẠP CHIẾU PHIM - " +
                    $"[{currentRole.ToString().ToUpper()} : {userDisplay}]";

                Size = new Size(1280, 760);
                MinimumSize = new Size(1000, 650);
                StartPosition = FormStartPosition.CenterScreen;
                BackColor = Color.FromArgb(245, 246, 250);

                // SIDEBAR
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

                // HEADER
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

                // CONTENT
                panelContent = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.FromArgb(241, 242, 246)
                };
                
                // STAFF AND ADMIN MENU

                if (currentRole == UserRole.Admin ||
                    currentRole == UserRole.Staff)
                {
                    AddNavGroup("--- BÁN HÀNG & SOÁT VÉ ---");

                    AddNavButton(
                        "🛒 Bán vé tại quầy (POS)",
                        OpenPOSView);

                    AddNavButton(
                        "📷 Quét mã QR soát vé",
                        () => OpenTicketScannerView());
                }
                
                // ADMIN MENU

                if (currentRole == UserRole.Admin)
                {
                    AddNavGroup("--- QUẢN TRỊ VIÊN (ADMIN) ---");

                    AddNavButton(
                        "📊 Dashboard Thống kê",
                        () => SwitchView(
                            "📊 Dashboard Doanh thu & Suất chiếu",
                            new DashboardUC()));

                    AddNavButton(
                        "🎬 Quản lý Phim",
                        () => SwitchView(
                            "🎬 Danh sách phim",
                            new MovieManagementUC()));

                    AddNavButton(
                        "🚪 Quản lý Phòng chiếu",
                        () => SwitchView(
                            "🚪 Quản lý phòng chiếu",
                            new RoomManagementUC()));

                    AddNavButton(
                        "⏰ Quản lý Suất chiếu",
                        () => SwitchView(
                            "⏰ Lịch chiếu rạp",
                            new ShowtimeManagementUC()));

                    AddNavButton(
                        "🍿 Quản lý Bắp nước",
                        () => SwitchView(
                            "🍿 Menu sản phẩm",
                            new FoodManagementUC()));

                    AddNavButton(
                        "👥 Quản lý Người dùng",
                        () => SwitchView(
                            "👥 Tài khoản người dùng",
                            new UserManagementUC()));
                }
                
                // CUSTOMER MENU

                if (currentRole == UserRole.Admin ||
                    currentRole == UserRole.Customer)
                {
                    AddNavGroup("--- KHÁCH HÀNG (CUSTOMER) ---");

                    AddNavButton(
                        "🏠 Trang chủ Phim",
                        OpenHomeView);

                    AddNavButton(
                        "📅 Lịch chiếu",
                        () => OpenShowtimeView(_bookingContext.Movie));

                    AddNavButton(
                        "🎟️ Đặt vé liên hoàn (Flow)",
                        StartBookingFlow);

                    AddNavButton(
                        "💺 Chọn ghế đặt vé",
                        () => OpenSeatSelectionView());

                    AddNavButton(
                        "🍿 Chọn bắp nước",
                        OpenFoodSelectionView);

                    AddNavButton(
                        "💳 Thanh toán",
                        OpenCheckoutView);

                    AddNavButton(
                        "🎟️ Vé của tôi",
                        () => OpenTicketView(UserSession.LatestBooking));
                }

                // =====================================================
                // LOGOUT
                // =====================================================

                AddNavGroup("--- HỆ THỐNG ---");

                AddNavButton("🚪 Đăng xuất", () =>
                {
                    // Xóa lượt đặt vé chưa hoàn tất
                    _bookingContext.Clear();

                    // Chỉ đăng xuất, không xóa lịch sử đặt vé
                    UserSession.Logout();

                    Hide();

                    var loginForm = new LoginForm();

                    loginForm.FormClosed += (s, args) =>
                    {
                        Close();
                    };

                    loginForm.Show();
                });

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
                    SwitchView(
                        "📊 Dashboard Doanh thu & Suất chiếu",
                        new DashboardUC());
                }
            }

            // =========================================================
            // 4. SIDEBAR
            // =========================================================

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

                btn.FlatAppearance.MouseOverBackColor =
                    Color.FromArgb(87, 101, 116);

                btn.Click += (s, e) => onClick();

                panelSidebar.Controls.Add(btn);
                btn.BringToFront();
            }

            // =========================================================
            // 5. SWITCH VIEW
            // =========================================================

            public void SwitchView(string title, UserControl control)
            {
                if (control == null)
                    throw new ArgumentNullException(nameof(control));

                lblHeaderTitle.Text = title;

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
            // 6. CUSTOMER BOOKING FLOW
            // Home -> Movie Detail -> Showtime
            // -> Seat -> Food -> Checkout -> Ticket
            // =========================================================

            // ---------------------------------------------------------
            // STEP 1: HOME
            // ---------------------------------------------------------

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

                SwitchView(
                    "🏠 Trang chủ - Chọn phim",
                    homeUC);
            }

            // ---------------------------------------------------------
            // STEP 1B: MOVIE DETAIL
            // ---------------------------------------------------------

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

                SwitchView(
                    $"🎬 Chi tiết phim: {movie.Title}",
                    detailUC);
            }

            // ---------------------------------------------------------
            // STEP 2: SHOWTIME
            // ---------------------------------------------------------

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
                    ? $"📅 Chọn suất chiếu: {_bookingContext.Movie.Title}"
                    : "📅 Lịch chiếu phim";

                SwitchView(header, showtimeUC);
            }

            // ---------------------------------------------------------
            // STEP 3: SEAT SELECTION
            // ---------------------------------------------------------

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

                SwitchView(
                    $"💺 Chọn ghế - {_bookingContext.Movie!.Title}",
                    seatUC);
            }

            // ---------------------------------------------------------
            // STEP 4: FOOD SELECTION
            // ---------------------------------------------------------

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

                SwitchView(
                    "🍿 Chọn bắp nước & Combo",
                    foodUC);
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
                        // Chỉ ghi nhận đơn sau khi thanh toán thành công.
                        UserSession.AddBooking(result);

                        OpenTicketView(result);
                    }
                    catch (InvalidOperationException ex)
                    {
                        MessageBox.Show(
                            ex.Message,
                            "Không thể đặt vé",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                };

                SwitchView("💳 Thanh toán", checkoutUC);
            }

            // ---------------------------------------------------------
            // STEP 6: TICKET
            // ---------------------------------------------------------

            public void OpenTicketView(
                BookingResultEventArgs? bookingData = null)
            {
                bookingData ??= UserSession.LatestBooking;

                if (bookingData == null)
                {
                    MessageBox.Show(
                        "Không tìm thấy thông tin vé.",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    OpenHomeView();
                    return;
                }

                var ticketUC = new TicketUC(bookingData);

                ticketUC.OnGoHomeClicked += (s, e) =>
                {
                    _bookingContext.Clear();

                    OpenHomeView();
                };

                SwitchView(
                    "🎟️ Hoàn tất & Xuất vé điện tử",
                    ticketUC);
            }

            // ---------------------------------------------------------
            // START BOOKING FLOW
            // ---------------------------------------------------------

            private void StartBookingFlow()
            {
                _bookingContext.Clear();

                OpenHomeView();
            }

            // =========================================================
            // 7. STAFF / ADMIN POS
            // =========================================================

            public void OpenPOSView()
            {
                var posUC = new POSUC();

                posUC.OnShowtimeSelected += (s, data) =>
                {
                    lblHeaderTitle.Text =
                        $"🛒 Bán vé tại quầy - " +
                        $"{data.Movie.Title} " +
                        $"({data.Showtime.StartTime:HH:mm})";
                };

                posUC.OnSeatsSelected += (s, seats) =>
                {
                    lblHeaderTitle.Text =
                        $"🛒 Bán vé tại quầy - " +
                        $"Đã chọn {seats.Count} ghế: " +
                        $"{string.Join(", ", seats)}";
                };

                posUC.OnOrderCleared += (s, e) =>
                {
                    lblHeaderTitle.Text =
                        "🛒 Bán vé tại quầy (POS)";
                };

                // POSUC tự ghi nhận booking cho đơn có vé.
                // Không gọi UserSession.AddBooking() tại đây.
                posUC.OnPaymentCompleted += (s, result) =>
                {
                    lblHeaderTitle.Text =
                        $"🛒 Bán vé tại quầy - " +
                        $"Đã thu {result.TotalAmount:N0} đ " +
                        $"(Mã {result.BookingCode})";
                };

                // Chỉ đăng ký một lần để tránh mở TicketUC hai lần.
                posUC.OnPrintTicketClicked += (s, result) =>
                {
                    OpenTicketView(result);
                };

                posUC.OnOpenScannerClicked += (s, code) =>
                {
                    OpenTicketScannerView(code);
                };

                SwitchView(
                    "🛒 Bán vé tại quầy (POS)",
                    posUC);
            }

            // =========================================================
            // 8. TICKET SCANNER
            // =========================================================

            public void OpenTicketScannerView(
                string? prefillCode = null)
            {
                var scannerUC = new TicketScannerUC(prefillCode);

                scannerUC.OnTicketScanned += (s, result) =>
                {
                    string state = result.Status switch
                    {
                        TicketScanStatus.Valid => "✔ Hợp lệ",
                        TicketScanStatus.AlreadyUsed => "⚠ Vé trùng",
                        TicketScanStatus.Expired => "✖ Hết hạn",
                        _ => "✖ Không tồn tại"
                    };

                    lblHeaderTitle.Text =
                        $"📷 Soát vé QR Code - {result.Code}: {state}";
                };

                scannerUC.OnTicketAccepted += (s, result) =>
                {
                    int room =
                        result.Booking?.Showtime?.RoomId ?? 1;

                    var seats =
                        result.Booking?.Seats ?? new List<string>();

                    lblHeaderTitle.Text =
                        $"📷 Soát vé - Mời khách vào Phòng 0{room} " +
                        $"(Ghế {string.Join(", ", seats)})";
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
                    lblHeaderTitle.Text = "📷 Soát vé QR Code";
                };

                scannerUC.OnOpenPOSClicked += (s, e) =>
                {
                    OpenPOSView();
                };

                SwitchView(
                    "📷 Soát vé QR Code",
                    scannerUC);
            }
        }
    }
