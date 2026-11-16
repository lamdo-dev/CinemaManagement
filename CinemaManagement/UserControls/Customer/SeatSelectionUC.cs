
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

using CinemaManagement.Models;
using CinemaManagement.Repositories;
using CinemaManagement.UserControls.Common;
using CinemaManagement.Utils;

namespace CinemaManagement.UserControls.Customer
{
    public partial class SeatSelectionUC : UserControl
    {
        // ==========================================
        // 1. MÀU SẮC GIAO DIỆN
        // ==========================================

        private readonly Color ColorBg =
            Color.FromArgb(14, 15, 22);

        private readonly Color ColorCard =
            Color.FromArgb(24, 25, 36);

        private readonly Color ColorGold =
            Color.FromArgb(195, 155, 80);

        private readonly Color ColorTextMuted =
            Color.FromArgb(150, 150, 170);

        // ==========================================
        // 2. REPOSITORY
        // ==========================================

        private readonly RoomRepository _roomRepository = new();

        // ==========================================
        // 3. CÁC CONTROL GIAO DIỆN
        // ==========================================

        private Panel pnlHeader = null!;
        private Button btnBack = null!;
        private Panel pnlLeftLegend = null!;
        private Panel pnlBottomBar = null!;
        private Label lblContextInfo = null!;

        private SeatMatrixUC seatMatrix = null!;

        private Label lblSelectedSeats = null!;
        private Label lblTotalPrice = null!;

        private Button btnContinue = null!;

        // ==========================================
        // 4. DỮ LIỆU ĐẶT VÉ
        // ==========================================

        private decimal currentTotal = 0m;

        public Movie? MovieData { get; private set; }

        public Showtime? ShowtimeData { get; private set; }

        private BookingContext? _bookingContext;

        // ==========================================
        // 5. EVENTS
        // ==========================================

        public event EventHandler<(List<string> seats, decimal price)>?
            OnContinueToFood;

        public event EventHandler? OnBackClicked;

        // ==========================================
        // 6. CONSTRUCTOR
        // ==========================================

        public SeatSelectionUC()
        {
            InitializeComponents();
        }

        // Constructor tương thích code cũ
        public SeatSelectionUC(
            Movie? movie,
            Showtime? showtime) : this()
        {
            SetBookingContext(movie, showtime);
        }

        // Constructor sử dụng BookingContext
        public SeatSelectionUC(
            BookingContext bookingContext) : this()
        {
            _bookingContext = bookingContext
                ?? throw new ArgumentNullException(
                    nameof(bookingContext));

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
                ?? throw new ArgumentNullException(
                    nameof(bookingContext));

            SetBookingContext(movie, showtime);
        }

        // ==========================================
        // 7. THIẾT LẬP PHIM VÀ SUẤT CHIẾU
        // ==========================================

        public void SetBookingContext(
            Movie? movie,
            Showtime? showtime)
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

            // Kiểm tra người dùng có đổi phim hoặc suất chiếu
            bool isDifferentShowtime;

            if (_bookingContext != null)
            {
                isDifferentShowtime =
                    _bookingContext.Movie == null ||
                    _bookingContext.Showtime == null ||
                    _bookingContext.Movie.Id != movie.Id ||
                    _bookingContext.Showtime.Id != showtime.Id;
            }
            else
            {
                isDifferentShowtime =
                    MovieData == null ||
                    ShowtimeData == null ||
                    MovieData.Id != movie.Id ||
                    ShowtimeData.Id != showtime.Id;
            }

            // Đồng bộ BookingContext
            if (_bookingContext != null)
            {
                _bookingContext.SetMovie(movie);
                _bookingContext.SetShowtime(showtime);

                // Đổi suất chiếu thì xóa ghế và đồ ăn cũ
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

            LoadSeatData(isDifferentShowtime);
        }

        // ==========================================
        // 8. TẢI DỮ LIỆU PHÒNG VÀ GHẾ
        // ==========================================

        private void LoadSeatData(bool isDifferentShowtime)
        {
            if (MovieData == null || ShowtimeData == null)
                return;

            try
            {
                // 1. Lấy phòng chiếu theo RoomId
                Room? room = _roomRepository.GetById(
                    Convert.ToInt64(ShowtimeData.RoomId));

                if (room == null)
                {
                    ShowSeatDataError(
                        "Không tìm thấy phòng chiếu của suất chiếu này.");

                    return;
                }

                if (room.TotalRows <= 0 ||
                    room.TotalColumns <= 0)
                {
                    ShowSeatDataError(
                        "Kích thước phòng chiếu không hợp lệ.");

                    return;
                }

                // 2. Lấy danh sách ghế thực tế trong phòng
                var roomSeats = _roomRepository
                    .GetSeatsByRoomId(Convert.ToInt32(room.Id))
                    .ToList();

                if (roomSeats.Count == 0)
                {
                    ShowSeatDataError(
                        "Phòng chiếu chưa có dữ liệu ghế.");

                    return;
                }

                // 3. Lấy danh sách ghế đã đặt
                HashSet<string> occupiedSeats =
                    UserSession.GetBookedSeats(
                        MovieData,
                        ShowtimeData);

                // 4. Lấy giá cơ bản của suất chiếu
                decimal ticketBasePrice =
                    ShowtimeData.BasePrice;

                if (ticketBasePrice <= 0)
                {
                    ticketBasePrice = 90000m;
                }

                // 5. Tạo sơ đồ ghế theo dữ liệu phòng
                seatMatrix.GenerateSeatMatrix(
                    room.TotalRows,
                    room.TotalColumns,
                    roomSeats,
                    occupiedSeats,
                    ticketBasePrice);

                // 6. Khôi phục ghế đã chọn nếu vẫn cùng suất chiếu
                if (_bookingContext != null &&
                    !isDifferentShowtime)
                {
                    seatMatrix.RestoreSelectedSeats(
                        _bookingContext.Seats);
                }

                UpdateSeatInformation();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể tải dữ liệu phòng chiếu và ghế.\n\n" +
                    ex.Message,
                    "Lỗi tải dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                seatMatrix.GenerateSeatMatrix(10, 14);

                UpdateSeatInformation();
            }
        }

        // ==========================================
        // 9. HIỂN THỊ LỖI DỮ LIỆU GHẾ
        // ==========================================

        private void ShowSeatDataError(string message)
        {
            MessageBox.Show(
                message,
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            seatMatrix.GenerateSeatMatrix(10, 14);

            UpdateSeatInformation();
        }

        // ==========================================
        // 10. KHỞI TẠO GIAO DIỆN
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
                Font = new Font(
                    "Segoe UI",
                    9.5f,
                    FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(35, 36, 50),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 32),
                Location = new Point(20, 15),
                Cursor = Cursors.Hand
            };

            btnBack.FlatAppearance.BorderSize = 0;

            btnBack.Click += (s, e) =>
                OnBackClicked?.Invoke(
                    this,
                    EventArgs.Empty);

            // ==========================================
            // STEPPER
            // ==========================================

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
                Font = new Font(
                    "Segoe UI",
                    9,
                    FontStyle.Bold),
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
            // BẢNG CHÚ THÍCH GHẾ
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
                Font = new Font(
                    "Segoe UI",
                    12,
                    FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 35
            };

            pnlLeftLegend.Controls.Add(lblLegendTitle);

            AddLegendItem(
                pnlLeftLegend,
                "Ghế Thường (Standard)",
                "Hệ số giá: x1.0",
                SeatMatrixUC.ColorStandard,
                50);

            AddLegendItem(
                pnlLeftLegend,
                "Ghế VIP",
                "Hệ số giá: x1.2",
                SeatMatrixUC.ColorVIP,
                120);

            AddLegendItem(
                pnlLeftLegend,
                "Ghế Đôi (Couple)",
                "Hệ số giá: x2.0",
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
                Font = new Font(
                    "Segoe UI",
                    9.5f,
                    FontStyle.Bold),
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
                Font = new Font(
                    "Segoe UI",
                    11,
                    FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };

            lblTotalPrice = new Label
            {
                Text = "Tổng tiền: 0 đ",
                Font = new Font(
                    "Segoe UI",
                    14,
                    FontStyle.Bold),
                ForeColor = ColorGold,
                Location = new Point(20, 40),
                AutoSize = true
            };

            // ==========================================
            // NÚT TIẾP TỤC
            // ==========================================

            btnContinue = new Button
            {
                Text = "TIẾP TỤC - CHỌN BẮP NƯỚC →",
                Font = new Font(
                    "Segoe UI",
                    10,
                    FontStyle.Bold),
                ForeColor = Color.Black,
                BackColor = ColorGold,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(270, 45),
                Anchor =
                    AnchorStyles.Top |
                    AnchorStyles.Right,
                Cursor = Cursors.Hand
            };

            btnContinue.FlatAppearance.BorderSize = 0;

            btnContinue.FlatAppearance.MouseOverBackColor =
                Color.FromArgb(220, 180, 105);

            btnContinue.Click += BtnContinue_Click;

            // Căn nút bên phải
            Action positionContinueButton = () =>
            {
                btnContinue.Location = new Point(
                    Math.Max(
                        20,
                        pnlBottomBar.ClientSize.Width -
                        btnContinue.Width - 20),
                    15);
            };

            positionContinueButton();

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
        // 11. TẠO CHÚ THÍCH BẢNG GIÁ
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
                Font = new Font(
                    "Segoe UI",
                    9.5f,
                    FontStyle.Bold),
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
        // 12. KHI NGƯỜI DÙNG CHỌN GHẾ
        // ==========================================

        private void SeatMatrix_OnSeatSelectionChanged(
            object? sender,
            List<string> selectedSeats)
        {
            currentTotal = selectedSeats.Sum(
                seat => seatMatrix.GetSeatPrice(seat));

            SyncBookingContext(selectedSeats);

            UpdateSeatInformation();
        }

        // ==========================================
        // 13. ĐỒNG BỘ BOOKING CONTEXT
        // ==========================================

        private void SyncBookingContext(
            IEnumerable<string> seats)
        {
            if (_bookingContext == null)
                return;

            _bookingContext.SetSeats(seats);

            _bookingContext.SetTicketAmount(currentTotal);
        }

        // ==========================================
        // 14. CẬP NHẬT THÔNG TIN GHẾ VÀ GIÁ VÉ
        // ==========================================

        private void UpdateSeatInformation()
        {
            List<string> seats =
                seatMatrix?.GetSelectedSeats()
                ?? new List<string>();

            if (seats.Count == 0)
            {
                lblSelectedSeats.Text =
                    "Ghế đã chọn: Chưa chọn";

                currentTotal = 0m;
            }
            else
            {
                lblSelectedSeats.Text =
                    $"Ghế đã chọn: {string.Join(", ", seats)}";

                currentTotal = seats.Sum(
                    seat => seatMatrix.GetSeatPrice(seat));
            }

            lblTotalPrice.Text =
                $"Tổng tiền: {currentTotal.ToString(
                    "N0",
                    new CultureInfo("vi-VN"))} đ";

            // Chỉ cho tiếp tục khi đã chọn phim,
            // suất chiếu và ít nhất một ghế.
            btnContinue.Enabled =
                MovieData != null &&
                ShowtimeData != null &&
                seats.Count > 0;

            SyncBookingContext(seats);
        }

        // ==========================================
        // 15. TIẾP TỤC CHỌN BẮP NƯỚC
        // ==========================================

        private void BtnContinue_Click(
            object? sender,
            EventArgs e)
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

            // Tính tiền theo cấu hình ghế
            currentTotal = selectedSeats.Sum(
                seat => seatMatrix.GetSeatPrice(seat));

            // Đồng bộ BookingContext
            SyncBookingContext(selectedSeats);

            // Chuyển dữ liệu sang MainForm
            OnContinueToFood?.Invoke(
                this,
                (
                    new List<string>(selectedSeats),
                    currentTotal
                ));
        }
    }
}
