
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using CinemaManagement.Models;
using CinemaManagement.UserControls.Common;
using CinemaManagement.UserControls.Customer;
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
        // 2. SAMPLE DATA
        // =====================================================

        private readonly List<Movie> _movies = new()
        {
            new Movie
            {
                Id = 1,
                Title = "Die Hard",
                Genre = "Action",
                DurationMinutes = 132
            },

            new Movie
            {
                Id = 3,
                Title = "La La Land",
                Genre = "Romance",
                DurationMinutes = 128
            },

            new Movie
            {
                Id = 4,
                Title = "Avatar: The Way of Water",
                Genre = "Sci-Fi",
                DurationMinutes = 192
            },

            new Movie
            {
                Id = 5,
                Title = "Oppenheimer",
                Genre = "Drama",
                DurationMinutes = 180
            },

            new Movie
            {
                Id = 6,
                Title = "Spider-Man: Across The Spider-Verse",
                Genre = "Action",
                DurationMinutes = 140
            }
        };

        private readonly (string Label, string[] Times,
            decimal BasePrice, int RoomId)[] _formats =
        {
            (
                "DIGITAL 2D • Phòng 01",
                new[] { "09:20", "11:45", "14:15", "16:50", "19:30", "22:00" },
                90000m,
                1
            ),

            (
                "IMAX 3D • Phòng IMAX",
                new[] { "10:30", "13:40", "17:15", "20:45" },
                120000m,
                2
            ),

            (
                "4DX 3D • Phòng VIP",
                new[] { "12:00", "15:30", "18:45" },
                150000m,
                3
            )
        };

        private readonly List<(int Id, string Name,
            decimal Price, string Category)> _products = new()
        {
            (1, "Coca Cola Regular 375ml", 35000m, "Drink"),
            (2, "Sprite Regular 375ml", 35000m, "Drink"),
            (3, "Fanta Orange 375ml", 35000m, "Drink"),
            (4, "Bắp Rang Bơ Thơm Lừng L", 55000m, "Popcorn"),
            (5, "Bắp Rang Phô Mai L", 65000m, "Popcorn"),
            (6, "Combo Solo (1 Bắp L + 1 Nước L)", 85000m, "Combo"),
            (7, "Combo Couple (1 Bắp L + 2 Nước L)", 115000m, "Combo"),
            (8, "Combo Family (2 Bắp L + 3 Nước L)", 195000m, "Combo")
        };

        // =====================================================
        // 3. ORDER STATE
        // =====================================================

        private Movie? _selectedMovie;

        private Showtime? _selectedShowtime;

        private List<string> _selectedSeats = new();

        private decimal _ticketAmount;

        private readonly Dictionary<int,
            (string Name, decimal Price, int Qty, string ImgPath)>
            _foodCart = new();

        // =====================================================
        // 4. CONTROLS
        // =====================================================

        private ComboBox cboMovie = null!;

        private ComboBox cboFormat = null!;

        private FlowLayoutPanel flpTimes = null!;

        private Label lblShowtimeInfo = null!;

        private Button btnPickSeats = null!;

        private FlowLayoutPanel flpItems = null!;

        private ListView lvOrder = null!;

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
            InitializeComponentManual();

            if (cboMovie.Items.Count > 0)
                cboMovie.SelectedIndex = 0;
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
                Text = "🧾 HÓA ĐƠN BÁN TẠI QUẦY",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = ColorPrimary,
                Dock = DockStyle.Top,
                Height = 32
            };

            lvOrder = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5f),
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };

            lvOrder.Columns.Add("Mục", 190);
            lvOrder.Columns.Add("SL", 40, HorizontalAlignment.Center);
            lvOrder.Columns.Add("Thành tiền", 105, HorizontalAlignment.Right);

            lvOrder.DoubleClick += LvOrder_DoubleClick;

            Label lblHint = new Label
            {
                Text = "💡 Nhấp đúp vào món bắp nước để bớt 1",
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
                Text = "🗑 Hủy đơn",
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
                Text = "💳 THANH TOÁN",
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

            pnlOrder.Controls.Add(lvOrder);
            pnlOrder.Controls.Add(lblHint);
            pnlOrder.Controls.Add(lblOrderTab);
            pnlOrder.Controls.Add(pnlTotal);

            // -------------------------------------------------
            // LEFT PANEL: SHOWTIME
            // -------------------------------------------------

            Panel pnlShowtime = new Panel
            {
                Dock = DockStyle.Top,
                Height = 175,
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
                Text = "📷 Soát vé",
                Location = new Point(560, 6),
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
                    UserSession.LatestBooking?.BookingCode
                    ?? string.Empty);
            };

            cboMovie = new ComboBox
            {
                Location = new Point(20, 40),
                Size = new Size(300, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };

            foreach (var movie in _movies)
                cboMovie.Items.Add(movie.Title);

            cboMovie.SelectedIndexChanged += (s, e) =>
                OnMovieOrFormatChanged();

            cboFormat = new ComboBox
            {
                Location = new Point(330, 40),
                Size = new Size(220, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };

            foreach (var format in _formats)
            {
                cboFormat.Items.Add(
                    $"{format.Label} ({format.BasePrice.ToString("N0", VN)}đ)");
            }

            cboFormat.SelectedIndexChanged += (s, e) =>
                OnMovieOrFormatChanged();

            flpTimes = new FlowLayoutPanel
            {
                Location = new Point(20, 76),
                Size = new Size(650, 42),
                WrapContents = false,
                AutoScroll = true
            };

            lblShowtimeInfo = new Label
            {
                Text = "Chưa chọn suất chiếu",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                ForeColor = ColorMuted,
                Location = new Point(20, 130),
                Size = new Size(420, 30)
            };

            btnPickSeats = new Button
            {
                Text = "💺 ② CHỌN GHẾ",
                Location = new Point(450, 124),
                Size = new Size(220, 38),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };

            btnPickSeats.FlatAppearance.BorderSize = 0;
            btnPickSeats.Click += BtnPickSeats_Click;

            pnlShowtime.Controls.AddRange(new Control[]
            {
                lblStep1,
                btnScanner,
                cboMovie,
                cboFormat,
                flpTimes,
                lblShowtimeInfo,
                btnPickSeats
            });

            // -------------------------------------------------
            // FOOD AREA
            // -------------------------------------------------

            Label lblStep3 = new Label
            {
                Text = "   ③ BẮP NƯỚC & COMBO (nhấp vào món để thêm vào hóa đơn)",
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

            foreach (var product in _products)
            {
                flpItems.Controls.Add(
                    CreateItemCard(
                        product.Id,
                        product.Name,
                        product.Price,
                        product.Category));
            }

            // Add controls
            Controls.Add(flpItems);
            Controls.Add(lblStep3);
            Controls.Add(pnlShowtime);
            Controls.Add(pnlOrder);
        }

        // =====================================================
        // 9. CREATE FOOD CARD
        // =====================================================

        private Panel CreateItemCard(
            int id,
            string name,
            decimal price,
            string category)
        {
            Panel card = new Panel
            {
                Width = 170,
                Height = 175,
                BackColor = Color.White,
                Margin = new Padding(10),
                Cursor = Cursors.Hand
            };

            string icon = category switch
            {
                "Drink" => "🥤",
                "Popcorn" => "🍿",
                _ => "🍱"
            };

            Label imgBox = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 30f),
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = Color.FromArgb(237, 233, 254),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };

            Label lblName = new Label
            {
                Text = name,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 44,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };

            Label lblPrice = new Label
            {
                Text = $"{price.ToString("N0", VN)} đ",
                ForeColor = Color.DarkSlateBlue,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };

            EventHandler addHandler = (s, e) =>
                AddFood(id, name, price);

            card.Click += addHandler;
            imgBox.Click += addHandler;
            lblName.Click += addHandler;
            lblPrice.Click += addHandler;

            card.MouseEnter += (s, e) =>
                card.BackColor = Color.FromArgb(245, 243, 255);

            card.MouseLeave += (s, e) =>
                card.BackColor = Color.White;

            card.Controls.Add(lblPrice);
            card.Controls.Add(lblName);
            card.Controls.Add(imgBox);

            return card;
        }

        // =====================================================
        // 10. MOVIE / FORMAT / SHOWTIME
        // =====================================================

        private void OnMovieOrFormatChanged()
        {
            if (cboMovie.SelectedIndex < 0 ||
                cboFormat.SelectedIndex < 0)
                return;

            _selectedMovie = null;
            _selectedShowtime = null;

            _selectedSeats.Clear();
            _ticketAmount = 0m;

            btnPickSeats.Enabled = false;

            lblShowtimeInfo.Text = "Chưa chọn suất chiếu";

            var format = _formats[cboFormat.SelectedIndex];

            flpTimes.Controls.Clear();

            foreach (var time in format.Times)
            {
                var start = DateTime.Parse(
                    $"{DateTime.Today:yyyy-MM-dd} {time}");

                bool passed = start < DateTime.Now;

                Button btnTime = new Button
                {
                    Text = time,
                    Size = new Size(78, 34),
                    Margin = new Padding(0, 0, 8, 0),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    BackColor = passed
                        ? Color.FromArgb(230, 230, 235)
                        : Color.FromArgb(237, 233, 254),
                    ForeColor = passed ? Color.Gray : ColorPrimary,
                    Enabled = !passed,
                    Cursor = Cursors.Hand,
                    Tag = start
                };

                btnTime.FlatAppearance.BorderSize = 0;
                btnTime.Click += BtnTime_Click;

                flpTimes.Controls.Add(btnTime);
            }

            RefreshOrderView();
        }

        private void BtnTime_Click(object? sender, EventArgs e)
        {
            if (sender is not Button btn ||
                btn.Tag is not DateTime start)
                return;

            foreach (Control control in flpTimes.Controls)
            {
                if (control is Button timeButton &&
                    timeButton.Enabled)
                {
                    timeButton.BackColor =
                        Color.FromArgb(237, 233, 254);

                    timeButton.ForeColor = ColorPrimary;
                }
            }

            btn.BackColor = ColorPrimary;
            btn.ForeColor = Color.White;

            var movie = _movies[cboMovie.SelectedIndex];
            var format = _formats[cboFormat.SelectedIndex];

            _selectedMovie = movie;

            _selectedShowtime = new Showtime
            {
                Id = format.RoomId,
                MovieId = movie.Id,
                RoomId = format.RoomId,
                StartTime = start,
                EndTime = start.AddMinutes(movie.DurationMinutes),
                BasePrice = format.BasePrice
            };

            _selectedSeats.Clear();
            _ticketAmount = 0m;

            btnPickSeats.Enabled = true;

            lblShowtimeInfo.Text =
                $"🎬 {movie.Title} | {format.Label} | ⏰ {start:HH:mm dd/MM}";

            lblShowtimeInfo.ForeColor =
                Color.FromArgb(47, 53, 66);

            OnShowtimeSelected?.Invoke(
                this,
                (movie, _selectedShowtime));

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

                Size = new Size(820, 640),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var matrix = new SeatMatrixUC
            {
                Dock = DockStyle.Fill
            };

            matrix.GenerateSeatMatrix(
                10,
                14,
                UserSession.GetBookedSeats(
                    _selectedMovie,
                    _selectedShowtime));

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
                Size = new Size(520, 25)
            };

            Button btnOk = new Button
            {
                Text = "✓ XÁC NHẬN GHẾ",
                Location = new Point(600, 10),
                Size = new Size(185, 40),
                BackColor = Color.FromArgb(195, 155, 80),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnOk.FlatAppearance.BorderSize = 0;

            matrix.OnSeatSelectionChanged += (s, seats) =>
            {
                decimal sum =
                    seats.Sum(SeatMatrixUC.GetSeatPriceByCode)
                    * GetFormatMultiplier();

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

                _ticketAmount =
                    _selectedSeats.Sum(
                        SeatMatrixUC.GetSeatPriceByCode)
                    * GetFormatMultiplier();

                OnSeatsSelected?.Invoke(
                    this,
                    new List<string>(_selectedSeats));

                RefreshOrderView();
            }
        }

        // =====================================================
        // 12. FORMAT PRICE
        // =====================================================

        private decimal GetFormatMultiplier()
        {
            if (cboFormat.SelectedIndex < 0)
                return 1m;

            return Math.Round(
                _formats[cboFormat.SelectedIndex].BasePrice
                / 90000m,
                2);
        }

        // =====================================================
        // 13. FOOD CART
        // =====================================================

        private void AddFood(
            int id,
            string name,
            decimal price)
        {
            int qty = _foodCart.TryGetValue(id, out var item)
                ? item.Qty + 1
                : 1;

            _foodCart[id] = (
                name,
                price,
                qty,
                string.Empty);

            RefreshOrderView();
        }

        private void LvOrder_DoubleClick(
            object? sender,
            EventArgs e)
        {
            if (lvOrder.SelectedItems.Count == 0)
                return;

            if (lvOrder.SelectedItems[0].Tag is not int productId)
                return;

            if (_foodCart.TryGetValue(productId, out var item))
            {
                if (item.Qty <= 1)
                {
                    _foodCart.Remove(productId);
                }
                else
                {
                    _foodCart[productId] = (
                        item.Name,
                        item.Price,
                        item.Qty - 1,
                        item.ImgPath);
                }

                RefreshOrderView();
            }
        }

        // =====================================================
        // 14. ORDER VIEW
        // =====================================================

        private void RefreshOrderView()
        {
            lvOrder.BeginUpdate();

            try
            {
                lvOrder.Items.Clear();

                if (_selectedSeats.Count > 0 &&
                    _selectedMovie != null)
                {
                    var head = new ListViewItem(
                        $"🎟 {_selectedMovie.Title}")
                    {
                        Font = new Font(
                            "Segoe UI",
                            9.5f,
                            FontStyle.Bold)
                    };

                    head.SubItems.Add(
                        _selectedSeats.Count.ToString());

                    head.SubItems.Add(
                        $"{_ticketAmount.ToString("N0", VN)} đ");

                    lvOrder.Items.Add(head);

                    var seatsLine = new ListViewItem(
                        $"   Ghế: {string.Join(", ", _selectedSeats)}")
                    {
                        ForeColor = ColorMuted
                    };

                    seatsLine.SubItems.Add("");
                    seatsLine.SubItems.Add("");

                    lvOrder.Items.Add(seatsLine);
                }

                foreach (var food in _foodCart)
                {
                    var row = new ListViewItem(
                        $"🍿 {food.Value.Name}")
                    {
                        Tag = food.Key
                    };

                    row.SubItems.Add(
                        food.Value.Qty.ToString());

                    row.SubItems.Add(
                        $"{(food.Value.Price * food.Value.Qty).ToString("N0", VN)} đ");

                    lvOrder.Items.Add(row);
                }
            }
            finally
            {
                lvOrder.EndUpdate();
            }

            lblTicketSub.Text =
                $"Tiền vé ({_selectedSeats.Count} ghế): " +
                $"{_ticketAmount.ToString("N0", VN)} đ";

            lblFoodSub.Text =
                $"Bắp nước: {FoodAmount.ToString("N0", VN)} đ";

            lblTotal.Text =
                $"TỔNG: {TotalAmount.ToString("N0", VN)} đ";

            btnPay.Text = TotalAmount > 0
                ? $"💳 THU {TotalAmount.ToString("N0", VN)} đ"
                : "💳 THANH TOÁN";

            UpdateChange();

            OnCartChanged?.Invoke(this, TotalAmount);
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
            // Kiểm tra hóa đơn
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

            // Kiểm tra thông tin vé
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

            // Không cho phép có tiền vé nhưng không có ghế
            if (!hasTicket && _ticketAmount > 0)
            {
                MessageBox.Show(
                    "Thông tin tiền vé không hợp lệ.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            // Kiểm tra phương thức thanh toán
            if (cboPayment.SelectedIndex < 0)
            {
                MessageBox.Show(
                    "Vui lòng chọn phương thức thanh toán.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            // Kiểm tra tiền mặt
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

            // Chuẩn hóa ghế
            var normalizedSeats = _selectedSeats
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToUpperInvariant())
                .ToList();

            // Kiểm tra ghế trùng trong đơn
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

            // Tạo kết quả thanh toán
            var result = new BookingResultEventArgs
            {
                BookingCode = GenerateBookingCode(),

                Movie = hasTicket ? _selectedMovie : null,

                Showtime = hasTicket ? _selectedShowtime : null,

                Seats = normalizedSeats,

                FoodCart = new Dictionary<int,
                    (string Name, decimal Price, int Qty, string ImgPath)>
                    (_foodCart),

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

            // Tránh nhấn thanh toán nhiều lần
            btnPay.Enabled = false;

            try
            {
                // Chỉ ghi nhận vào lịch sử vé khi đơn có vé.
                // Đơn chỉ mua đồ ăn không phải đơn đặt vé.
                if (hasTicket)
                {
                    UserSession.AddBooking(result);
                }

                // Báo cho MainForm biết thanh toán hoàn tất
                OnPaymentCompleted?.Invoke(this, result);

                // Tạo hóa đơn
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

                // Mở giao diện vé nếu khách muốn in vé
                if (hasTicket &&
                    ask == DialogResult.Yes)
                {
                    OnPrintTicketClicked?.Invoke(
                        this,
                        result);
                }

                // Reset đơn sau khi thanh toán thành công
                ResetOrder();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Không thể hoàn tất đơn hàng",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
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
            _selectedMovie = null;

            _selectedShowtime = null;

            _selectedSeats.Clear();

            _ticketAmount = 0m;

            _foodCart.Clear();

            txtCustomer.Text = string.Empty;

            txtCashReceived.Text = string.Empty;

            if (cboPayment.Items.Count > 0)
                cboPayment.SelectedIndex = 0;

            if (cboMovie.Items.Count > 0)
                cboMovie.SelectedIndex = 0;

            RefreshOrderView();
        }
    }
}
