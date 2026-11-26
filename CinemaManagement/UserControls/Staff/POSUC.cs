using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CinemaManagement.Models;
using CinemaManagement.Models.Enums;
using CinemaManagement.Repositories;
using CinemaManagement.Services;
using CinemaManagement.UserControls.Common;
using CinemaManagement.Utils;

namespace CinemaManagement.UserControls.Staff
{
    public partial class POSUC : UserControl
    {
        // =====================================================
        // 1. THEME
        // =====================================================

        private static readonly CultureInfo VN =
            new CultureInfo("vi-VN");

        private readonly Color ColorPrimary =
            Color.FromArgb(124, 58, 237);

        private readonly Color ColorBg =
            Color.FromArgb(245, 246, 250);

        private readonly Color ColorMuted =
            Color.FromArgb(120, 125, 140);

        // =====================================================
        // 2. POS DATA
        // =====================================================

        private readonly TicketService _ticketService = new();
        private readonly FoodService _foodService = new();
        private readonly BookingService _bookingService = new();
        private readonly MovieRepository _movieRepository = new();
        private readonly List<Movie> _movies = new();
        private readonly List<Product> _products = new();
        private readonly List<(Showtime Showtime, Movie Movie, Room? Room, decimal StandardPrice)> _availableShowtimes = new();
        private readonly List<(string Format, long RoomId, string Label)> _showtimeFilters = new();
        private string _lastBookingCode = string.Empty;
        private string _activeBookingCode = GenerateBookingCode();
        private bool _updatingShowtimes;

        // =====================================================
        // 3. ORDER STATE
        // =====================================================

        private Movie? _selectedMovie;

        private Showtime? _selectedShowtime;

        private List<string> _selectedSeats = new();

        private decimal _ticketAmount;

        private readonly Dictionary<int, CartItem> _foodCart = new();

        // =====================================================
        // 4. CONTROLS
        // =====================================================

        private ComboBox cboMovie = null!;

        private ComboBox cboFormat = null!;

        private DateTimePicker dtpShowDate = null!;

        private FlowLayoutPanel flpTimes = null!;

        private Label lblShowtimeInfo = null!;

        private Button btnPickSeats = null!;

        private FlowLayoutPanel flpItems = null!;

        private FlowLayoutPanel flpOrderItems = null!;

        private Label lblOrderMeta = null!;

        private TextBox txtCustomer = null!;

        private ComboBox cboPayment = null!;

        private TextBox txtCashReceived = null!;

        private Label lblChange = null!;

        private Label lblTicketSub = null!;

        private Label lblFoodSub = null!;

        private Label lblTotal = null!;

        private Button btnPay = null!;

        // =====================================================
        // 5. EVENTS
        // =====================================================

        public event EventHandler<(Movie Movie, Showtime Showtime)>?
            OnShowtimeSelected;

        public event EventHandler<List<string>>?
            OnSeatsSelected;

        public event EventHandler<decimal>?
            OnCartChanged;

        public event EventHandler?
            OnOrderCleared;

        public event EventHandler<BookingResultEventArgs>?
            OnPaymentCompleted;

        public event EventHandler<BookingResultEventArgs>?
            OnPrintTicketClicked;

        public event EventHandler<string>?
            OnOpenScannerClicked;

        // =====================================================
        // 6. PUBLIC PROPERTIES
        // =====================================================

        public decimal FoodAmount =>
            _foodCart.Sum(x => x.Value.Price * x.Value.Qty);

        public decimal TotalAmount =>
            _ticketAmount + FoodAmount;

        // =====================================================
        // 7. CONSTRUCTOR
        // =====================================================

        public POSUC()
        {
            try
            {
                _movies.AddRange(_movieRepository.GetAll()
                    .Where(movie => movie.Status == MovieStatus.Showing));
                _products.AddRange(_foodService.GetAll()
                    .Where(product => product.IsActive && product.StockQuantity > 0));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Không thể tải dữ liệu POS: {ex.Message}");
            }

            InitializeComponentManual();

            foreach (Movie movie in _movies)
                cboMovie.Items.Add(movie);

            foreach (Product product in _products)
                flpItems.Controls.Add(CreateItemCard(product));

            if (cboMovie.Items.Count > 0)
                cboMovie.SelectedIndex = 0;

            if (cboPayment.Items.Count > 0)
                cboPayment.SelectedIndex = 0;

            if (cboMovie.Items.Count == 0)
            {
                lblShowtimeInfo.Text = "Chưa có phim đang chiếu trong cơ sở dữ liệu.";
                lblShowtimeInfo.ForeColor = ColorMuted;
            }

            if (_products.Count == 0)
            {
                flpItems.Controls.Add(new Label
                {
                    Text = "Chưa có sản phẩm đang bán hoặc còn hàng.",
                    AutoSize = true,
                    ForeColor = ColorMuted,
                    Font = new Font("Segoe UI", 10F),
                    Margin = new Padding(12)
                });
            }
        }

        // =====================================================
        // 8. INITIALIZE UI
        // =====================================================

        private void InitializeComponentManual()
        {
            Dock = DockStyle.Fill;
            BackColor = ColorBg;

            // -------------------------------------------------
            // RIGHT PANEL: ORDER
            // -------------------------------------------------

            Panel pnlOrder = new Panel
            {
                Dock = DockStyle.Right,
                Width = 380,
                BackColor = Color.White,
                Padding = new Padding(18)
            };

            Label lblOrderTab = new Label
            {
                Text = "HÓA ĐƠN BÁN VÉ",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = ColorPrimary,
                Dock = DockStyle.Top,
                Height = 32
            };

            lblOrderMeta = new Label
            {
                Text = $"Mã đơn: {_activeBookingCode}  ·  {DateTime.Now:dd/MM/yyyy HH:mm}",
                Font = new Font("Segoe UI", 8F),
                ForeColor = ColorMuted,
                Dock = DockStyle.Top,
                Height = 22,
                AutoEllipsis = true
            };

            flpOrderItems = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 4)
            };
            flpOrderItems.SizeChanged += (s, e) => ResizeOrderRows();

            Label lblHint = new Label
            {
                Text = "Dùng nút − / + cạnh sản phẩm để chỉnh số lượng",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = ColorMuted,
                Dock = DockStyle.Top,
                Height = 22
            };

            Panel pnlTotal = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 300
            };

            Label lblCus = new Label
            {
                Text = "Tên khách hàng (tùy chọn):",
                Font = new Font("Segoe UI", 9),
                ForeColor = ColorMuted,
                Location = new Point(0, 6),
                AutoSize = true
            };

            txtCustomer = new TextBox
            {
                Location = new Point(0, 26),
                Size = new Size(342, 28),
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "Khách lẻ"
            };

            Label lblPay = new Label
            {
                Text = "Phương thức:",
                Font = new Font("Segoe UI", 9),
                ForeColor = ColorMuted,
                Location = new Point(0, 62),
                AutoSize = true
            };

            cboPayment = new ComboBox
            {
                Location = new Point(0, 82),
                Size = new Size(165, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };

            cboPayment.Items.AddRange(new object[]
            {
                "Tiền mặt",
                "Thẻ ATM / Visa",
                "Chuyển khoản QR",
                "Ví MoMo"
            });

            cboPayment.SelectedIndexChanged += (s, e) =>
                UpdateChange();

            Label lblCash = new Label
            {
                Text = "Khách đưa:",
                Font = new Font("Segoe UI", 9),
                ForeColor = ColorMuted,
                Location = new Point(177, 62),
                AutoSize = true
            };

            txtCashReceived = new TextBox
            {
                Location = new Point(177, 82),
                Size = new Size(165, 28),
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "0"
            };

            txtCashReceived.TextChanged += (s, e) =>
                UpdateChange();

            lblChange = new Label
            {
                Text = "Tiền thối: 0 đ",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.SeaGreen,
                Location = new Point(0, 116),
                Size = new Size(342, 22)
            };

            lblTicketSub = new Label
            {
                Text = "Tiền vé: 0 đ",
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(0, 142),
                Size = new Size(342, 20)
            };

            lblFoodSub = new Label
            {
                Text = "Bắp nước: 0 đ",
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(0, 162),
                Size = new Size(342, 20)
            };

            lblTotal = new Label
            {
                Text = "TỔNG: 0 đ",
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = ColorPrimary,
                Location = new Point(0, 186),
                Size = new Size(342, 32)
            };

            Button btnClear = new Button
            {
                Text = "HỦY GIAO DỊCH",
                Location = new Point(0, 236),
                Size = new Size(110, 50),
                BackColor = Color.FromArgb(235, 236, 242),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnClear.FlatAppearance.BorderSize = 0;

            btnClear.Click += (s, e) =>
            {
                if (_selectedSeats.Count == 0 &&
                    _foodCart.Count == 0)
                    return;

                if (MessageBox.Show(
                    "Hủy toàn bộ đơn hàng hiện tại?",
                    "Xác nhận",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    ResetOrder();

                    OnOrderCleared?.Invoke(
                        this,
                        EventArgs.Empty);
                }
            };

            btnPay = new Button
            {
                Text = "THANH TOÁN",
                Location = new Point(118, 236),
                Size = new Size(224, 50),
                BackColor = ColorPrimary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnPay.FlatAppearance.BorderSize = 0;
            btnPay.Click += BtnPay_Click;

            pnlTotal.Controls.AddRange(new Control[]
            {
                lblCus,
                txtCustomer,
                lblPay,
                cboPayment,
                lblCash,
                txtCashReceived,
                lblChange,
                lblTicketSub,
                lblFoodSub,
                lblTotal,
                btnClear,
                btnPay
            });

            pnlOrder.Controls.Add(flpOrderItems);
            pnlOrder.Controls.Add(lblHint);
            pnlOrder.Controls.Add(lblOrderMeta);
            pnlOrder.Controls.Add(lblOrderTab);
            pnlOrder.Controls.Add(pnlTotal);

            // -------------------------------------------------
            // LEFT PANEL: SHOWTIME
            // -------------------------------------------------

            Panel pnlShowtime = new Panel
            {
                Dock = DockStyle.Top,
                Width = 900,
                Height = 180,
                BackColor = Color.White,
                Padding = new Padding(20, 12, 20, 12)
            };

            Label lblStep1 = new Label
            {
                Text = "① CHỌN PHIM & SUẤT CHIẾU",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = ColorPrimary,
                Location = new Point(20, 10),
                AutoSize = true
            };

            Button btnScanner = new Button
            {
                Text = "SOÁT VÉ",
                Location = new Point(770, 8),
                Size = new Size(110, 30),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            btnScanner.FlatAppearance.BorderSize = 0;

            btnScanner.Click += (s, e) =>
            {
                OnOpenScannerClicked?.Invoke(
                    this,
                    _lastBookingCode);
            };

            cboMovie = new ComboBox
            {
                Location = new Point(20, 42),
                Size = new Size(260, 30),
                DropDownWidth = 340,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                DisplayMember = nameof(Movie.Title)
            };

            cboMovie.SelectedIndexChanged += (s, e) =>
                OnMovieOrFormatChanged();

            cboFormat = new ComboBox
            {
                Location = new Point(292, 42),
                Size = new Size(250, 30),
                DropDownWidth = 400,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };

            cboFormat.SelectedIndexChanged += (s, e) =>
                OnMovieOrFormatChanged();

            dtpShowDate = new DateTimePicker
            {
                Location = new Point(554, 42),
                Size = new Size(185, 30),
                Font = new Font("Segoe UI", 9.5f),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd MMM yyyy",
                MinDate = DateTime.Today,
                Value = DateTime.Today
            };
            dtpShowDate.ValueChanged += (s, e) => OnMovieOrFormatChanged();

            flpTimes = new FlowLayoutPanel
            {
                Location = new Point(20, 80),
                Size = new Size(860, 42),
                WrapContents = false,
                AutoScroll = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            lblShowtimeInfo = new Label
            {
                Text = "Chưa chọn suất chiếu",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = ColorMuted,
                Location = new Point(20, 134),
                Size = new Size(640, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            btnPickSeats = new Button
            {
                Text = "② CHỌN GHẾ",
                Location = new Point(680, 128),
                Size = new Size(200, 38),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            btnPickSeats.FlatAppearance.BorderSize = 0;
            btnPickSeats.Click += BtnPickSeats_Click;

            pnlShowtime.Controls.AddRange(new Control[]
            {
                lblStep1,
                btnScanner,
                cboMovie,
                cboFormat,
                dtpShowDate,
                flpTimes,
                lblShowtimeInfo,
                btnPickSeats
            });

            // -------------------------------------------------
            // FOOD AREA
            // -------------------------------------------------

            Label lblStep3 = new Label
            {
                Text = "③ BẮP NƯỚC & COMBO  ·  Chọn sản phẩm để thêm vào hóa đơn",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = ColorPrimary,
                Dock = DockStyle.Top,
                Height = 36,
                TextAlign = ContentAlignment.MiddleLeft
            };

            flpItems = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(14)
            };


            // Add controls
            Controls.Add(flpItems);
            Controls.Add(lblStep3);
            Controls.Add(pnlShowtime);
            Controls.Add(pnlOrder);
        }

        // =====================================================
        // 9. CREATE FOOD CARD
        // =====================================================

        private Panel CreateItemCard(Product product)
        {
            Panel card = new Panel
            {
                Width = 220,
                Height = 178,
                BackColor = Color.White,
                Margin = new Padding(8, 6, 8, 10),
                Cursor = Cursors.Hand
            };

            Panel productVisual = new Panel
            {
                Dock = DockStyle.Top,
                Height = 66,
                BackColor = Color.FromArgb(243, 240, 255),
                Cursor = Cursors.Hand
            };

            Image? productImage = TryLoadProductImage(product.ImageUrl);
            if (productImage != null)
            {
                var picture = new PictureBox
                {
                    Image = productImage,
                    Dock = DockStyle.Fill,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = productVisual.BackColor,
                    Cursor = Cursors.Hand
                };
                productVisual.Controls.Add(picture);
                card.Disposed += (s, e) => picture.Image?.Dispose();
            }
            else
            {
                productVisual.Controls.Add(new Label
                {
                    Text = product.Category.ToUpperInvariant(),
                    Dock = DockStyle.Fill,
                    ForeColor = ColorPrimary,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor = Cursors.Hand
                });
            }

            Label lblName = new Label
            {
                Text = product.Name,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(10, 4, 8, 2),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Cursor = Cursors.Hand
            };

            Label lblPrice = new Label
            {
                Text = $"{product.Price.ToString("N0", VN)} đ  ·  Còn {product.StockQuantity}",
                ForeColor = Color.FromArgb(84, 37, 208),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Dock = DockStyle.Bottom,
                Height = 36,
                Padding = new Padding(10, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };

            EventHandler addHandler = (s, e) =>
                AddFood(product.Id, product.Name, product.Price);

            card.Click += addHandler;
            productVisual.Click += addHandler;
            foreach (Control child in productVisual.Controls)
                child.Click += addHandler;
            lblName.Click += addHandler;
            lblPrice.Click += addHandler;

            card.MouseEnter += (s, e) =>
                card.BackColor = Color.FromArgb(245, 243, 255);

            card.MouseLeave += (s, e) =>
                card.BackColor = Color.White;

            card.Controls.Add(lblPrice);
            card.Controls.Add(lblName);
            card.Controls.Add(productVisual);

            return card;
        }

        private static Image? TryLoadProductImage(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return null;

            try
            {
                string path = Path.IsPathRooted(imageUrl)
                    ? imageUrl
                    : Path.Combine(Application.StartupPath, imageUrl);

                if (!File.Exists(path))
                    return null;

                using Image source = Image.FromFile(path);
                return new Bitmap(source);
            }
            catch
            {
                return null;
            }
        }

        // =====================================================
        // 10. MOVIE / FORMAT / SHOWTIME
        // =====================================================

        private void OnMovieOrFormatChanged()
        {
            if (_updatingShowtimes || cboMovie.SelectedItem is not Movie movie)
                return;

            _selectedMovie = null;
            _selectedShowtime = null;

            _selectedSeats.Clear();
            _ticketAmount = 0m;

            btnPickSeats.Enabled = false;
            flpTimes.Controls.Clear();
            _availableShowtimes.Clear();
            _showtimeFilters.Clear();

            try
            {
                _availableShowtimes.AddRange(
                    _ticketService.GetAvailableShowtimes(dtpShowDate.Value.Date, movie.Id));

                foreach (var showtime in _availableShowtimes)
                {
                    string format = string.IsNullOrWhiteSpace(showtime.Showtime.Format)
                        ? "2D"
                        : showtime.Showtime.Format.Trim();
                    string roomName = showtime.Room?.Name ?? $"Phòng {showtime.Showtime.RoomId}";
                    if (!_showtimeFilters.Any(option =>
                            option.RoomId == showtime.Showtime.RoomId &&
                            string.Equals(option.Format, format, StringComparison.OrdinalIgnoreCase)))
                    {
                        _showtimeFilters.Add((format, showtime.Showtime.RoomId, $"{format} · {roomName}"));
                    }
                }
            }
            catch (Exception ex)
            {
                lblShowtimeInfo.Text = $"Không thể tải lịch chiếu: {ex.Message}";
                lblShowtimeInfo.ForeColor = Color.FromArgb(185, 28, 28);
                RefreshOrderView();
                return;
            }

            _updatingShowtimes = true;
            try
            {
                cboFormat.BeginUpdate();
                cboFormat.Items.Clear();
                foreach (var option in _showtimeFilters)
                    cboFormat.Items.Add(option.Label);
                if (cboFormat.Items.Count > 0)
                    cboFormat.SelectedIndex = 0;
            }
            finally
            {
                cboFormat.EndUpdate();
                _updatingShowtimes = false;
            }

            PopulateShowtimeButtons();
        }

        private void PopulateShowtimeButtons()
        {
            flpTimes.Controls.Clear();
            if (cboFormat.SelectedIndex < 0 || _showtimeFilters.Count == 0)
            {
                lblShowtimeInfo.Text = "Không có suất chiếu còn vé cho ngày đã chọn.";
                lblShowtimeInfo.ForeColor = ColorMuted;
                RefreshOrderView();
                return;
            }

            var filter = _showtimeFilters[cboFormat.SelectedIndex];
            var matchingShowtimes = _availableShowtimes
                .Where(item => item.Showtime.RoomId == filter.RoomId &&
                               string.Equals(
                                   string.IsNullOrWhiteSpace(item.Showtime.Format)
                                       ? "2D"
                                       : item.Showtime.Format.Trim(),
                                   filter.Format,
                                   StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.Showtime.StartTime)
                .ToList();

            Button? firstTimeBtn = null;
            foreach (var showtime in matchingShowtimes)
            {
                var button = new Button
                {
                    Text = showtime.Showtime.StartTime.ToString("HH:mm"),
                    Size = new Size(82, 36),
                    Margin = new Padding(0, 0, 8, 0),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    BackColor = Color.FromArgb(243, 240, 255),
                    ForeColor = ColorPrimary,
                    Cursor = Cursors.Hand,
                    Tag = showtime.Showtime
                };

                button.FlatAppearance.BorderSize = 0;
                button.Click += BtnTime_Click;
                flpTimes.Controls.Add(button);
                firstTimeBtn ??= button;
            }

            if (firstTimeBtn != null)
                BtnTime_Click(firstTimeBtn, EventArgs.Empty);
        }

        private void BtnTime_Click(object? sender, EventArgs e)
        {
            if (sender is not Button btn ||
                btn.Tag is not Showtime showtime)
                return;

            foreach (Control control in flpTimes.Controls)
            {
                if (control is Button timeButton &&
                    timeButton.Enabled)
                {
                    timeButton.BackColor = Color.FromArgb(243, 240, 255);

                    timeButton.ForeColor = ColorPrimary;
                }
            }

            btn.BackColor = ColorPrimary;
            btn.ForeColor = Color.White;

            var selectedShowtime = _availableShowtimes
                .FirstOrDefault(item => item.Showtime.Id == showtime.Id);
            if (selectedShowtime.Showtime == null)
                return;

            _selectedMovie = selectedShowtime.Movie;
            _selectedShowtime = selectedShowtime.Showtime;

            _selectedSeats.Clear();
            _ticketAmount = 0m;

            btnPickSeats.Enabled = true;

            string roomName = selectedShowtime.Room?.Name ?? $"Phòng {showtime.RoomId}";
            lblShowtimeInfo.Text =
                $"{selectedShowtime.Movie.Title}  ·  {showtime.Format}  ·  {roomName}  ·  " +
                $"{showtime.StartTime:HH:mm dd/MM}  ·  {selectedShowtime.Movie.DurationMinutes} phút";

            lblShowtimeInfo.ForeColor =
                Color.FromArgb(47, 53, 66);

            OnShowtimeSelected?.Invoke(
                this,
                (_selectedMovie, _selectedShowtime));

            RefreshOrderView();
        }

        // =====================================================
        // 11. SEAT SELECTION
        // =====================================================

        private void BtnPickSeats_Click(object? sender, EventArgs e)
        {
            if (_selectedMovie == null ||
                _selectedShowtime == null)
                return;

            using var dlg = new Form
            {
                Text =
                    $"Chọn ghế - {_selectedMovie.Title} " +
                    $"({_selectedShowtime.StartTime:HH:mm})",
                Size = new Size(840, 650),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var matrix = new SeatMatrixUC
            {
                Dock = DockStyle.Fill
            };

            var seatData = _ticketService.GetSeatSelectionData(_selectedShowtime);
            if (seatData.Room == null || seatData.RoomSeats.Count == 0)
            {
                MessageBox.Show(
                    "Phòng chiếu chưa có sơ đồ ghế trong cơ sở dữ liệu.",
                    "Không thể chọn ghế",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            matrix.GenerateSeatMatrix(
                seatData.Room.TotalRows,
                seatData.Room.TotalColumns,
                seatData.RoomSeats,
                seatData.OccupiedSeats,
                _selectedShowtime.BasePrice,
                seatData.SeatPriceMap);

            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(20, 21, 32)
            };

            Label lblSel = new Label
            {
                Text = "Chưa chọn ghế",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(15, 18),
                Size = new Size(540, 25)
            };

            Button btnOk = new Button
            {
                Text = "XÁC NHẬN GHẾ",
                Location = new Point(620, 10),
                Size = new Size(185, 40),
                BackColor = Color.FromArgb(195, 155, 80),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnOk.FlatAppearance.BorderSize = 0;

            matrix.OnSeatSelectionChanged += (s, seats) =>
            {
                decimal sum = seats.Sum(SeatMatrixUC.GetSeatPriceByCode);

                lblSel.Text = seats.Count == 0
                    ? "Chưa chọn ghế"
                    : $"Ghế: {string.Join(", ", seats)} | {sum.ToString("N0", VN)} đ";
            };

            btnOk.Click += (s, e2) =>
            {
                if (matrix.SelectedSeatCodes.Count == 0)
                {
                    MessageBox.Show(
                        "Vui lòng chọn ít nhất 1 ghế!",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                dlg.DialogResult = DialogResult.OK;
            };

            pnlBottom.Controls.Add(lblSel);
            pnlBottom.Controls.Add(btnOk);

            dlg.Controls.Add(matrix);
            dlg.Controls.Add(pnlBottom);

            if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
            {
                _selectedSeats =
                    new List<string>(matrix.SelectedSeatCodes);

                _ticketAmount = _selectedSeats.Sum(SeatMatrixUC.GetSeatPriceByCode);

                OnSeatsSelected?.Invoke(
                    this,
                    new List<string>(_selectedSeats));

                RefreshOrderView();
            }
        }

        // =====================================================
        // 13. FOOD CART
        // =====================================================

        private void AddFood(
            int id,
            string name,
            decimal price)
        {
            Product? product = _products.FirstOrDefault(item => item.Id == id);
            if (product == null || !product.IsActive)
                return;

            int qty = _foodCart.TryGetValue(id, out var item)
                ? item.Qty + 1
                : 1;

            if (qty > product.StockQuantity)
            {
                MessageBox.Show(
                    $"Sản phẩm {product.Name} chỉ còn {product.StockQuantity} trong kho.",
                    "Không đủ tồn kho",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            _foodCart[id] = new CartItem(
                name,
                price,
                qty,
                string.Empty);

            RefreshOrderView();
        }

        private void ChangeFoodQuantity(int productId, int change)
        {
            if (!_foodCart.TryGetValue(productId, out var item))
                return;

            if (change > 0)
            {
                Product? product = _products.FirstOrDefault(candidate => candidate.Id == productId);
                if (product == null || item.Qty >= product.StockQuantity)
                {
                    MessageBox.Show(
                        $"Sản phẩm {item.Name} đã đạt số lượng tồn kho.",
                        "Không đủ tồn kho",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                _foodCart[productId] = item with { Qty = item.Qty + 1 };
            }
            else if (item.Qty <= 1)
            {
                _foodCart.Remove(productId);
            }
            else
            {
                _foodCart[productId] = item with { Qty = item.Qty - 1 };
            }

            RefreshOrderView();
        }

        // =====================================================
        // 14. ORDER VIEW
        // =====================================================

        private void RefreshOrderView()
        {
            flpOrderItems.SuspendLayout();

            try
            {
                flpOrderItems.Controls.Clear();

                if (_selectedSeats.Count > 0 &&
                    _selectedMovie != null)
                {
                    flpOrderItems.Controls.Add(CreateOrderInfoRow(
                        $"VÉ · {_selectedMovie.Title}",
                        $"{_selectedSeats.Count} vé  ·  {_ticketAmount.ToString("N0", VN)} đ",
                        FontStyle.Bold));
                    flpOrderItems.Controls.Add(CreateOrderInfoRow(
                        $"Ghế: {string.Join(", ", _selectedSeats)}",
                        string.Empty,
                        FontStyle.Regular));
                }

                foreach (var food in _foodCart)
                    flpOrderItems.Controls.Add(CreateFoodOrderRow(food.Key, food.Value));

                if (flpOrderItems.Controls.Count == 0)
                    flpOrderItems.Controls.Add(CreateOrderInfoRow(
                        "Chưa có vé hoặc sản phẩm",
                        "Chọn sản phẩm hoặc ghế để bắt đầu đơn hàng.",
                        FontStyle.Regular));
            }
            finally
            {
                flpOrderItems.ResumeLayout(true);
            }

            ResizeOrderRows();

            lblTicketSub.Text =
                $"Tiền vé ({_selectedSeats.Count} ghế): " +
                $"{_ticketAmount.ToString("N0", VN)} đ";

            lblFoodSub.Text =
                $"Bắp nước: {FoodAmount.ToString("N0", VN)} đ";

            lblTotal.Text =
                $"TỔNG: {TotalAmount.ToString("N0", VN)} đ";

            btnPay.Text = TotalAmount > 0
                ? $"THU {TotalAmount.ToString("N0", VN)} đ"
                : "THANH TOÁN";

            UpdateChange();

            OnCartChanged?.Invoke(this, TotalAmount);
        }

        private Panel CreateFoodOrderRow(int productId, CartItem item)
        {
            var row = new Panel
            {
                Height = 62,
                Width = Math.Max(180, flpOrderItems.ClientSize.Width - 8),
                Margin = new Padding(0, 0, 0, 5),
                BackColor = Color.FromArgb(248, 249, 252),
                Padding = new Padding(8, 4, 6, 4)
            };

            var name = new Label
            {
                Text = $"SP · {item.Name}",
                Dock = DockStyle.Top,
                Height = 25,
                AutoEllipsis = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var amount = new Label
            {
                Text = $"{(item.Price * item.Qty).ToString("N0", VN)} đ",
                Dock = DockStyle.Bottom,
                Height = 24,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = ColorMuted,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var quantityPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 100,
                BackColor = Color.Transparent
            };

            Button decrease = CreateQuantityButton("−", "Giảm số lượng");
            decrease.Location = new Point(2, 15);
            decrease.Click += (s, e) => ChangeFoodQuantity(productId, -1);

            var quantity = new Label
            {
                Text = item.Qty.ToString(),
                Location = new Point(33, 15),
                Size = new Size(32, 30),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Button increase = CreateQuantityButton("+", "Tăng số lượng");
            increase.Location = new Point(66, 15);
            increase.Click += (s, e) => ChangeFoodQuantity(productId, 1);

            quantityPanel.Controls.Add(decrease);
            quantityPanel.Controls.Add(quantity);
            quantityPanel.Controls.Add(increase);
            row.Controls.Add(amount);
            row.Controls.Add(name);
            row.Controls.Add(quantityPanel);
            return row;
        }

        private Button CreateQuantityButton(string text, string accessibleName)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(30, 30),
                BackColor = Color.FromArgb(237, 233, 254),
                ForeColor = ColorPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                AccessibleName = accessibleName,
                TabStop = true
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(221, 214, 254);
            button.FlatAppearance.BorderSize = 1;
            return button;
        }

        private Panel CreateOrderInfoRow(string title, string detail, FontStyle fontStyle)
        {
            var row = new Panel
            {
                Height = string.IsNullOrEmpty(detail) ? 30 : 46,
                Width = Math.Max(180, flpOrderItems.ClientSize.Width - 8),
                Margin = new Padding(0, 0, 0, 3),
                BackColor = Color.White
            };

            var titleLabel = new Label
            {
                Text = title,
                Dock = string.IsNullOrEmpty(detail) ? DockStyle.Fill : DockStyle.Top,
                Height = 24,
                AutoEllipsis = true,
                Font = new Font("Segoe UI", 9F, fontStyle),
                ForeColor = fontStyle == FontStyle.Bold
                    ? Color.FromArgb(30, 41, 59)
                    : ColorMuted,
                TextAlign = ContentAlignment.MiddleLeft
            };
            row.Controls.Add(titleLabel);

            if (!string.IsNullOrEmpty(detail))
            {
                var detailLabel = new Label
                {
                    Text = detail,
                    Dock = DockStyle.Bottom,
                    Height = 20,
                    Font = new Font("Segoe UI", 8.5F),
                    ForeColor = ColorMuted,
                    AutoEllipsis = true,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                row.Controls.Add(detailLabel);
            }

            return row;
        }

        private void ResizeOrderRows()
        {
            if (flpOrderItems == null || flpOrderItems.IsDisposed)
                return;

            int rowWidth = Math.Max(180, flpOrderItems.ClientSize.Width - 8);
            foreach (Control row in flpOrderItems.Controls)
                row.Width = rowWidth;
        }

        // =====================================================
        // 15. PAYMENT INFORMATION
        // =====================================================

        private bool IsCashPayment =>
            cboPayment.SelectedIndex == 0;

        private decimal ParseCash()
        {
            string raw = new string(
                txtCashReceived.Text
                    .Where(char.IsDigit)
                    .ToArray());

            return decimal.TryParse(raw, out var value)
                ? value
                : 0m;
        }

        private void UpdateChange()
        {
            if (cboPayment == null ||
                txtCashReceived == null ||
                lblChange == null)
                return;

            txtCashReceived.Enabled = IsCashPayment;

            if (!IsCashPayment)
            {
                lblChange.Text =
                    "Thanh toán không dùng tiền mặt";

                lblChange.ForeColor = ColorMuted;

                return;
            }

            decimal change = ParseCash() - TotalAmount;

            lblChange.Text = change >= 0
                ? $"Tiền thối: {change.ToString("N0", VN)} đ"
                : $"Còn thiếu: {(-change).ToString("N0", VN)} đ";

            lblChange.ForeColor = change >= 0
                ? Color.SeaGreen
                : Color.OrangeRed;
        }

        // =====================================================
        // 16. CREATE BOOKING CODE
        // =====================================================

        private static string GenerateBookingCode()
        {
            return $"POS-{Guid.NewGuid():N}".ToUpperInvariant();
        }

        // =====================================================
        // 17. PAYMENT
        // =====================================================

        private void BtnPay_Click(
            object? sender,
            EventArgs e)
        {
            if (TotalAmount <= 0)
            {
                MessageBox.Show(
                    "Hóa đơn đang trống! Hãy chọn vé hoặc bắp nước.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            bool hasTicket = _selectedSeats.Count > 0;

            if (hasTicket &&
                (_selectedMovie == null ||
                 _selectedShowtime == null))
            {
                MessageBox.Show(
                    "Thông tin phim hoặc suất chiếu không hợp lệ.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (!hasTicket && _ticketAmount > 0)
            {
                MessageBox.Show(
                    "Thông tin tiền vé không hợp lệ.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (cboPayment.SelectedIndex < 0)
            {
                MessageBox.Show(
                    "Vui lòng chọn phương thức thanh toán.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (IsCashPayment &&
                ParseCash() < TotalAmount)
            {
                MessageBox.Show(
                    "Số tiền khách đưa chưa đủ!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtCashReceived.Focus();

                return;
            }

            var normalizedSeats = _selectedSeats
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToUpperInvariant())
                .ToList();

            if (normalizedSeats.Count != normalizedSeats
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count())
            {
                MessageBox.Show(
                    "Danh sách ghế có ghế bị trùng.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (hasTicket && normalizedSeats.Count == 0)
            {
                MessageBox.Show(
                    "Danh sách ghế không hợp lệ.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            var result = new BookingResultEventArgs
            {
                BookingCode = _activeBookingCode,
                Movie = hasTicket ? _selectedMovie : null,
                Showtime = hasTicket ? _selectedShowtime : null,
                Seats = normalizedSeats,
                FoodCart = new Dictionary<int, CartItem>(_foodCart),
                TicketAmount = _ticketAmount,
                FoodAmount = FoodAmount,
                TotalAmount = TotalAmount,
                BookingTime = DateTime.Now,
                Channel = "POS",
                PaymentMethod =
                    cboPayment.SelectedItem?.ToString()
                    ?? "Tiền mặt",
                CustomerName =
                    string.IsNullOrWhiteSpace(txtCustomer.Text)
                        ? "Khách lẻ"
                        : txtCustomer.Text.Trim()
            };

            lblOrderMeta.Text =
                $"Mã giao dịch: {result.BookingCode}  ·  {result.BookingTime:dd/MM/yyyy HH:mm:ss}";

            btnPay.Enabled = false;
            btnPay.Text = "ĐANG XỬ LÝ...";

            try
            {
                _bookingService.CreatePosBooking(result);
                _lastBookingCode = result.BookingCode;

                OnPaymentCompleted?.Invoke(this, result);

                string receipt = BuildReceipt(result);

                DialogResult ask = MessageBox.Show(
                    receipt +
                    (hasTicket
                        ? "\n\nIn vé cho khách ngay bây giờ?"
                        : ""),
                    "Thanh toán thành công",
                    hasTicket
                        ? MessageBoxButtons.YesNo
                        : MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                if (hasTicket &&
                    ask == DialogResult.Yes)
                {
                    OnPrintTicketClicked?.Invoke(
                        this,
                        result);
                }

                ResetOrder();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Không thể hoàn tất đơn hàng",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                btnPay.Text = "THANH TOÁN";
                btnPay.Enabled = true;
            }
        }

        // =====================================================
        // 18. BUILD RECEIPT
        // =====================================================

        private string BuildReceipt(BookingResultEventArgs r)
        {
            var sb = new StringBuilder();

            sb.AppendLine("========== CINEMA APP ==========");
            sb.AppendLine($"Mã đơn: {r.BookingCode}");
            sb.AppendLine($"Khách hàng: {r.CustomerName}");
            sb.AppendLine(
                $"Thời gian: {r.BookingTime:HH:mm:ss dd/MM/yyyy}");

            if (r.Movie != null &&
                r.Showtime != null)
            {
                sb.AppendLine("--------------------------------");
                sb.AppendLine($"Phim: {r.Movie.Title}");
                sb.AppendLine(
                    $"Suất: {r.Showtime.StartTime:HH:mm dd/MM/yyyy}");
                sb.AppendLine(
                    $"Phòng: {r.Showtime.RoomId}");
                sb.AppendLine(
                    $"Ghế: {string.Join(", ", r.Seats)}");
            }

            if (r.FoodCart.Count > 0)
            {
                sb.AppendLine("--------------------------------");
                sb.AppendLine("BẮP NƯỚC:");

                foreach (var item in r.FoodCart)
                {
                    sb.AppendLine(
                        $"{item.Value.Name} x{item.Value.Qty} - " +
                        $"{(item.Value.Price * item.Value.Qty).ToString("N0", VN)} đ");
                }
            }

            sb.AppendLine("--------------------------------");
            sb.AppendLine($"Thanh toán: {r.PaymentMethod}");
            sb.AppendLine(
                $"TỔNG: {r.TotalAmount.ToString("N0", VN)} đ");

            if (IsCashPayment)
            {
                decimal received = ParseCash();
                decimal change = received - r.TotalAmount;

                sb.AppendLine(
                    $"Khách đưa: {received.ToString("N0", VN)} đ");
                sb.AppendLine(
                    $"Tiền thối: {change.ToString("N0", VN)} đ");
            }

            sb.AppendLine("================================");
            sb.AppendLine("       CẢM ƠN QUÝ KHÁCH!");

            return sb.ToString();
        }

        // =====================================================
        // 19. RESET ORDER
        // =====================================================

        public void ResetOrder()
        {
            _selectedSeats.Clear();
            _ticketAmount = 0m;
            _foodCart.Clear();
            _activeBookingCode = GenerateBookingCode();
            lblOrderMeta.Text =
                $"Mã đơn: {_activeBookingCode}  ·  {DateTime.Now:dd/MM/yyyy HH:mm}";

            txtCustomer.Text = string.Empty;
            txtCashReceived.Text = string.Empty;

            if (cboPayment.Items.Count > 0)
                cboPayment.SelectedIndex = 0;

            if (cboMovie.Items.Count > 0)
                cboMovie.SelectedIndex = 0;

            if (cboFormat.Items.Count > 0)
                cboFormat.SelectedIndex = 0;

            OnMovieOrFormatChanged();
        }
    }
}