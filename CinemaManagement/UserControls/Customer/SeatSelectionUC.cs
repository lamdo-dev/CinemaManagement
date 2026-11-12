
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Models;
using CinemaManagement.UserControls.Common;
using CinemaManagement.Utils;

namespace CinemaManagement.UserControls.Customer
{
    public partial class SeatSelectionUC : UserControl
    {
        private readonly Color ColorBg = Color.FromArgb(14, 15, 22);
        private readonly Color ColorCard = Color.FromArgb(24, 25, 36);
        private readonly Color ColorGold = Color.FromArgb(195, 155, 80);
        private readonly Color ColorTextMuted = Color.FromArgb(150, 150, 170);

        private Panel pnlHeader = null!;
        private Button btnBack = null!;
        private Panel pnlLeftLegend = null!;
        private Panel pnlBottomBar = null!;
        private Label lblContextInfo = null!;

        private SeatMatrixUC seatMatrix = null!;
        private Label lblSelectedSeats = null!;
        private Label lblTotalPrice = null!;
        private Button btnContinue = null!;

        private decimal currentTotal = 0m;

        public Movie? MovieData { get; private set; }
        public Showtime? ShowtimeData { get; private set; }

        private BookingContext? _bookingContext;

        public event EventHandler<(List<string> seats, decimal price)>?
            OnContinueToFood;

        public event EventHandler? OnBackClicked;

        // Constructor mặc định
        public SeatSelectionUC()
        {
            InitializeComponents();
        }

        // Constructor tương thích code cũ
        public SeatSelectionUC(Movie? movie, Showtime? showtime) : this()
        {
            SetBookingContext(movie, showtime);
        }

        // Constructor sử dụng BookingContext chung
        public SeatSelectionUC(BookingContext bookingContext) : this()
        {
            _bookingContext = bookingContext
                ?? throw new ArgumentNullException(nameof(bookingContext));

            SetBookingContext(
                bookingContext.Movie,
                bookingContext.Showtime);
        }

        // Constructor đầy đủ
        public SeatSelectionUC(
            Movie? movie,
            Showtime? showtime,
            BookingContext bookingContext) : this()
        {
            _bookingContext = bookingContext
                ?? throw new ArgumentNullException(nameof(bookingContext));

            SetBookingContext(movie, showtime);
        }

        // ==========================================
        // THIẾT LẬP PHIM VÀ SUẤT CHIẾU
        // ==========================================
        public void SetBookingContext(Movie? movie, Showtime? showtime)
        {
            if (movie == null || showtime == null)
            {
                MovieData = null;
                ShowtimeData = null;
                currentTotal = 0m;

                seatMatrix.GenerateSeatMatrix(10, 14);

                lblContextInfo.Text =
                    "Vui lòng chọn phim và suất chiếu.";

                UpdateSeatInformation();
                return;
            }

            // Kiểm tra suất chiếu hiện tại có khác dữ liệu
            // trong BookingContext hay không.
            bool isDifferentShowtime;

            if (_bookingContext != null)
            {
                isDifferentShowtime =
                    _bookingContext.Movie == null ||
                    _bookingContext.Showtime == null ||
                    _bookingContext.Movie.Id != movie.Id ||
                    _bookingContext.Showtime.RoomId != showtime.RoomId ||
                    _bookingContext.Showtime.StartTime != showtime.StartTime;
            }
            else
            {
                isDifferentShowtime =
                    MovieData == null ||
                    ShowtimeData == null ||
                    MovieData.Id != movie.Id ||
                    ShowtimeData.RoomId != showtime.RoomId ||
                    ShowtimeData.StartTime != showtime.StartTime;
            }

            // Đồng bộ phim và suất chiếu vào BookingContext
            if (_bookingContext != null)
            {
                _bookingContext.SetMovie(movie);
                _bookingContext.SetShowtime(showtime);

                // Chỉ xóa ghế và đồ ăn khi thực sự đổi suất chiếu
                if (isDifferentShowtime)
                {
                    _bookingContext.ClearAfterShowtimeChange();
                }
            }

            MovieData = movie;
            ShowtimeData = showtime;

            lblContextInfo.Text =
                $"🎬 {MovieData.Title}\n" +
                $"⏰ Suất: {ShowtimeData.StartTime:HH:mm - dd/MM/yyyy}";

            // Lấy danh sách ghế đã được đặt
            HashSet<string> occupiedSeats =
                UserSession.GetBookedSeats(MovieData, ShowtimeData);

            // Tạo sơ đồ ghế
            seatMatrix.GenerateSeatMatrix(
                10,
                14,
                occupiedSeats);

            // Khôi phục ghế người dùng đã chọn trước đó
            if (_bookingContext != null && !isDifferentShowtime)
            {
                seatMatrix.RestoreSelectedSeats(
                    _bookingContext.Seats);
            }

            // Cập nhật tổng tiền và thông tin ghế
            UpdateSeatInformation();
        }

        // ==========================================
        // KHỞI TẠO GIAO DIỆN
        // ==========================================
        private void InitializeComponents()
        {
            Dock = DockStyle.Fill;
            BackColor = ColorBg;

            // ==========================================
            // HEADER
            // ==========================================
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(18, 19, 28)
            };

            btnBack = new Button
            {
                Text = "← Quay lại",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(35, 36, 50),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 32),
                Location = new Point(20, 15),
                Cursor = Cursors.Hand
            };

            btnBack.FlatAppearance.BorderSize = 0;

            btnBack.Click += (s, e) =>
                OnBackClicked?.Invoke(this, EventArgs.Empty);

            // Stepper
            Panel pnlStepper = new Panel
            {
                Size = new Size(500, 40),
                Location = new Point(320, 12)
            };

            Label lblStep1 = new Label
            {
                Text = "① CHỌN SUẤT CHIẾU",
                Font = new Font("Segoe UI", 9),
                ForeColor = ColorTextMuted,
                Location = new Point(0, 10),
                AutoSize = true
            };

            Label lblDiv1 = new Label
            {
                Text = "───────",
                ForeColor = ColorTextMuted,
                Location = new Point(155, 10),
                AutoSize = true
            };

            Label lblStep2 = new Label
            {
                Text = "② CHỌN GHẾ",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = ColorGold,
                Location = new Point(230, 10),
                AutoSize = true
            };

            Label lblDiv2 = new Label
            {
                Text = "───────",
                ForeColor = ColorTextMuted,
                Location = new Point(335, 10),
                AutoSize = true
            };

            Label lblStep3 = new Label
            {
                Text = "③ THANH TOÁN",
                Font = new Font("Segoe UI", 9),
                ForeColor = ColorTextMuted,
                Location = new Point(410, 10),
                AutoSize = true
            };

            pnlStepper.Controls.AddRange(new Control[]
            {
                lblStep1,
                lblDiv1,
                lblStep2,
                lblDiv2,
                lblStep3
            });

            pnlHeader.Controls.Add(btnBack);
            pnlHeader.Controls.Add(pnlStepper);

            // ==========================================
            // BẢNG GIÁ GHẾ
            // ==========================================
            pnlLeftLegend = new Panel
            {
                Dock = DockStyle.Left,
                Width = 260,
                BackColor = ColorCard,
                Padding = new Padding(15)
            };

            Label lblLegendTitle = new Label
            {
                Text = "BẢNG GIÁ VÉ",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 35
            };

            pnlLeftLegend.Controls.Add(lblLegendTitle);

            AddLegendItem(
                pnlLeftLegend,
                "Ghế Thường (Classic)",
                "90.000 đ",
                SeatMatrixUC.ColorStandard,
                50);

            AddLegendItem(
                pnlLeftLegend,
                "Ghế VIP",
                "120.000 đ",
                SeatMatrixUC.ColorVIP,
                120);

            AddLegendItem(
                pnlLeftLegend,
                "Ghế Premium/Plus",
                "110.000 đ",
                SeatMatrixUC.ColorPlus,
                190);

            AddLegendItem(
                pnlLeftLegend,
                "Ghế Đang chọn",
                "Đã chọn",
                SeatMatrixUC.ColorSelected,
                260);

            AddLegendItem(
                pnlLeftLegend,
                "Ghế Đã bán",
                "Không trống",
                SeatMatrixUC.ColorOccupied,
                330);

            lblContextInfo = new Label
            {
                Text = "🎬 Vui lòng chọn suất chiếu",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = ColorGold,
                Location = new Point(15, 405),
                Size = new Size(230, 65)
            };

            pnlLeftLegend.Controls.Add(lblContextInfo);

            // ==========================================
            // THANH TỔNG TIỀN
            // ==========================================
            pnlBottomBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 75,
                BackColor = Color.FromArgb(20, 21, 32),
                Padding = new Padding(20, 10, 20, 10)
            };

            lblSelectedSeats = new Label
            {
                Text = "Ghế đã chọn: Chưa chọn",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };

            lblTotalPrice = new Label
            {
                Text = "Tổng tiền: 0 đ",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = ColorGold,
                Location = new Point(20, 40),
                AutoSize = true
            };

            // ==========================================
            // NÚT TIẾP TỤC CHỌN BẮP NƯỚC
            // ==========================================
            btnContinue = new Button
            {
                Text = "TIẾP TỤC - CHỌN BẮP NƯỚC →",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.Black,
                BackColor = ColorGold,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(270, 45),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };

            btnContinue.FlatAppearance.BorderSize = 0;

            btnContinue.FlatAppearance.MouseOverBackColor =
                Color.FromArgb(220, 180, 105);

            btnContinue.Click += BtnContinue_Click;

            // Hàm tự động căn nút bên phải
            Action positionContinueButton = () =>
            {
                btnContinue.Location = new Point(
                    Math.Max(
                        20,
                        pnlBottomBar.ClientSize.Width -
                        btnContinue.Width - 20),
                    15);
            };

            // Căn vị trí ban đầu
            positionContinueButton();

            // Tự động căn lại khi thay đổi kích thước cửa sổ
            pnlBottomBar.Resize += (s, e) =>
            {
                positionContinueButton();
            };

            pnlBottomBar.Controls.Add(lblSelectedSeats);
            pnlBottomBar.Controls.Add(lblTotalPrice);
            pnlBottomBar.Controls.Add(btnContinue);

            // ==========================================
            // SƠ ĐỒ GHẾ
            // ==========================================
            seatMatrix = new SeatMatrixUC
            {
                Dock = DockStyle.Fill
            };

            seatMatrix.OnSeatSelectionChanged +=
                SeatMatrix_OnSeatSelectionChanged;

            // Thứ tự thêm control quan trọng đối với Dock
            Controls.Add(seatMatrix);
            Controls.Add(pnlLeftLegend);
            Controls.Add(pnlHeader);
            Controls.Add(pnlBottomBar);

            UpdateSeatInformation();
        }

        // ==========================================
        // TẠO CHÚ THÍCH BẢNG GIÁ
        // ==========================================
        private void AddLegendItem(
            Panel container,
            string title,
            string price,
            Color color,
            int yPos)
        {
            Panel box = new Panel
            {
                Size = new Size(20, 20),
                Location = new Point(15, yPos),
                BackColor = color
            };

            Label lblT = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(45, yPos),
                AutoSize = true
            };

            Label lblP = new Label
            {
                Text = price,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = ColorTextMuted,
                Location = new Point(45, yPos + 20),
                AutoSize = true
            };

            container.Controls.Add(box);
            container.Controls.Add(lblT);
            container.Controls.Add(lblP);
        }

        // ==========================================
        // KHI NGƯỜI DÙNG CHỌN GHẾ
        // ==========================================
        private void SeatMatrix_OnSeatSelectionChanged(
            object? sender,
            List<string> selectedSeats)
        {
            currentTotal = selectedSeats.Sum(
                SeatMatrixUC.GetSeatPriceByCode);

            SyncBookingContext(selectedSeats);

            UpdateSeatInformation();
        }

        // ==========================================
        // ĐỒNG BỘ BOOKING CONTEXT
        // ==========================================
        private void SyncBookingContext(IEnumerable<string> seats)
        {
            if (_bookingContext == null)
                return;

            _bookingContext.SetSeats(seats);
            _bookingContext.SetTicketAmount(currentTotal);
        }

        // ==========================================
        // CẬP NHẬT THÔNG TIN GHẾ VÀ GIÁ VÉ
        // ==========================================
        private void UpdateSeatInformation()
        {
            List<string> seats =
                seatMatrix?.GetSelectedSeats() ?? new List<string>();

            if (seats.Count == 0)
            {
                lblSelectedSeats.Text = "Ghế đã chọn: Chưa chọn";
                currentTotal = 0m;
            }
            else
            {
                lblSelectedSeats.Text =
                    $"Ghế đã chọn: {string.Join(", ", seats)}";

                currentTotal = seats.Sum(
                    SeatMatrixUC.GetSeatPriceByCode);
            }

            lblTotalPrice.Text =
                $"Tổng tiền: {currentTotal.ToString(
                    "N0",
                    new CultureInfo("vi-VN"))} đ";

            // Chỉ cho phép tiếp tục khi đã chọn phim,
            // suất chiếu và ít nhất một ghế.
            btnContinue.Enabled =
                MovieData != null &&
                ShowtimeData != null &&
                seats.Count > 0;

            SyncBookingContext(seats);
        }

        // ==========================================
        // TIẾP TỤC CHỌN BẮP NƯỚC
        // ==========================================
        private void BtnContinue_Click(object? sender, EventArgs e)
        {
            if (MovieData == null || ShowtimeData == null)
            {
                MessageBox.Show(
                    "Vui lòng chọn phim và suất chiếu trước!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            List<string> selectedSeats =
                seatMatrix.GetSelectedSeats();

            if (selectedSeats.Count == 0)
            {
                MessageBox.Show(
                    "Vui lòng chọn ít nhất một ghế!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            // Tính tổng tiền vé
            currentTotal = selectedSeats.Sum(
                SeatMatrixUC.GetSeatPriceByCode);

            // Đồng bộ dữ liệu vào BookingContext
            SyncBookingContext(selectedSeats);

            // Gửi thông tin ghế và giá vé sang MainForm
            OnContinueToFood?.Invoke(
                this,
                (new List<string>(selectedSeats), currentTotal));
        }
    }
}
