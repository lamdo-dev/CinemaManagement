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
        private readonly TicketRepository _ticketRepository = new();

        // ==========================================
        // 3. CÁC CONTROL GIAO DIỆN
        // ==========================================

        private TableLayoutPanel tlpHeader = null!;
        private Button btnBack = null!;
        private Panel pnlLeftLegend = null!;
        private TableLayoutPanel tlpBottomBar = null!;
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
            AutoScaleMode = AutoScaleMode.Dpi;
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
                    "🎬 Vui lòng chọn phim và suất chiếu.";

                UpdateSeatInformation();

                return;
            }

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

            if (_bookingContext != null)
            {
                _bookingContext.SetMovie(movie);
                _bookingContext.SetShowtime(showtime);

                if (isDifferentShowtime)
                {
                    _bookingContext.ClearAfterShowtimeChange();
                }
            }

            MovieData = movie;
            ShowtimeData = showtime;

            lblContextInfo.Text =
                $"🎬 {MovieData.Title}\n" +
                $"⏰ Suất: {ShowtimeData.StartTime:HH:mm} - {ShowtimeData.StartTime:dd/MM/yyyy}";

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

                var roomSeats = _roomRepository
                    .GetSeatsByRoomId(Convert.ToInt32(room.Id))
                    .ToList();

                if (roomSeats.Count == 0)
                {
                    ShowSeatDataError(
                        "Phòng chiếu chưa có dữ liệu ghế.");

                    return;
                }

                // Đọc ghế đã đặt trực tiếp từ SQLite theo suất chiếu.
                // Không phụ thuộc UserSession nên vẫn đúng sau khi chạy lại ứng dụng.
                HashSet<string> occupiedSeats =
                    _ticketRepository.GetBookedSeatNumbers(
                        Convert.ToInt64(ShowtimeData.Id));

                decimal ticketBasePrice =
                    ShowtimeData.BasePrice;

                if (ticketBasePrice <= 0)
                {
                    ticketBasePrice = 90000m;
                }

                seatMatrix.GenerateSeatMatrix(
                    room.TotalRows,
                    room.TotalColumns,
                    roomSeats,
                    occupiedSeats,
                    ticketBasePrice);

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
        // 10. KHỞI TẠO GIAO DIỆN RESPONSIVE
        // ==========================================

        private void InitializeComponents()
        {
            Dock = DockStyle.Fill;
            BackColor = ColorBg;

            // HEADER RESPONSIVE
            tlpHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.FromArgb(18, 19, 28),
                Padding = new Padding(20, 12, 20, 12),
                Margin = new Padding(0)
            };
            tlpHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpHeader.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            btnBack = new Button
            {
                Text = "← Quay lại",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(35, 36, 50),
                FlatStyle = FlatStyle.Flat,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(12, 6, 12, 6),
                Margin = new Padding(0, 0, 16, 0),
                Cursor = Cursors.Hand
            };
            btnBack.FlatAppearance.BorderSize = 0;
            btnBack.MouseEnter += (s, e) =>
                btnBack.BackColor = Color.FromArgb(52, 54, 74);
            btnBack.MouseLeave += (s, e) =>
                btnBack.BackColor = Color.FromArgb(35, 36, 50);
            btnBack.Click += (s, e) =>
                OnBackClicked?.Invoke(this, EventArgs.Empty);

            var flpStepper = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Anchor = AnchorStyles.None,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };

            flpStepper.Controls.AddRange(new Control[]
            {
                CreateStepLabel("①  CHỌN SUẤT CHIẾU", false),
                CreateDividerLabel("──────"),
                CreateStepLabel("②  CHỌN GHẾ", true),
                CreateDividerLabel("──────"),
                CreateStepLabel("③  THANH TOÁN", false)
            });

            tlpHeader.Controls.Add(btnBack, 0, 0);
            tlpHeader.Controls.Add(flpStepper, 1, 0);

            // BẢNG CHÚ THÍCH GHẾ BÊN TRÁI
            pnlLeftLegend = new Panel
            {
                Dock = DockStyle.Left,
                Width = 285,
                BackColor = ColorCard,
                Padding = new Padding(16, 16, 16, 16),
                AutoScroll = true
            };

            var tlpLegend = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 7,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            tlpLegend.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            for (int i = 0; i < 7; i++)
            {
                tlpLegend.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            Label lblLegendTitle = new Label
            {
                Text = "BẢNG GIÁ VÉ",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 14)
            };

            tlpLegend.Controls.Add(lblLegendTitle, 0, 0);
            tlpLegend.Controls.Add(
                CreateLegendItem("Ghế Thường (Standard)", "Hệ số giá: x1.0",
                    SeatMatrixUC.ColorStandard), 0, 1);
            tlpLegend.Controls.Add(
                CreateLegendItem("Ghế VIP", "Hệ số giá: x1.2",
                    SeatMatrixUC.ColorVIP), 0, 2);
            tlpLegend.Controls.Add(
                CreateLegendItem("Ghế Đôi (Couple)", "Hệ số giá: x2.0",
                    SeatMatrixUC.ColorPlus), 0, 3);
            tlpLegend.Controls.Add(
                CreateLegendItem("Ghế Đang chọn", "Đã chọn",
                    SeatMatrixUC.ColorSelected), 0, 4);
            tlpLegend.Controls.Add(
                CreateLegendItem("Ghế Đã bán", "Không trống",
                    SeatMatrixUC.ColorOccupied), 0, 5);

            lblContextInfo = new Label
            {
                Text = "🎬 Vui lòng chọn suất chiếu",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = ColorGold,
                AutoSize = true,
                MaximumSize = new Size(245, 0),
                Margin = new Padding(0, 14, 0, 0)
            };

            tlpLegend.Controls.Add(lblContextInfo, 0, 6);
            pnlLeftLegend.Controls.Add(tlpLegend);

            // THANH TỔNG TIỀN VÀ NÚT TIẾP TỤC
            tlpBottomBar = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.FromArgb(20, 21, 32),
                Padding = new Padding(20, 12, 20, 14),
                Margin = new Padding(0)
            };
            tlpBottomBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpBottomBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpBottomBar.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var tlpSummaryLeft = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 16, 0)
            };
            tlpSummaryLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpSummaryLeft.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpSummaryLeft.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            lblSelectedSeats = new Label
            {
                Text = "Ghế đã chọn: Chưa chọn",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            lblTotalPrice = new Label
            {
                Text = "Tổng tiền: 0 đ",
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = ColorGold,
                AutoSize = true,
                Margin = new Padding(0)
            };

            tlpSummaryLeft.Controls.Add(lblSelectedSeats, 0, 0);
            tlpSummaryLeft.Controls.Add(lblTotalPrice, 0, 1);

            btnContinue = new Button
            {
                Text = "TIẾP TỤC - CHỌN BẮP NƯỚC →",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.Black,
                BackColor = ColorGold,
                FlatStyle = FlatStyle.Flat,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(18, 10, 18, 10),
                Margin = new Padding(0),
                Anchor = AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnContinue.FlatAppearance.BorderSize = 0;
            btnContinue.FlatAppearance.MouseOverBackColor =
                Color.FromArgb(220, 180, 105);
            btnContinue.Click += BtnContinue_Click;

            tlpBottomBar.Controls.Add(tlpSummaryLeft, 0, 0);
            tlpBottomBar.Controls.Add(btnContinue, 1, 0);

            tlpBottomBar.SizeChanged += (s, e) =>
            {
                int availableWidth =
                    tlpBottomBar.ClientSize.Width - btnContinue.Width - 60;

                if (availableWidth > 180)
                {
                    lblSelectedSeats.MaximumSize =
                        new Size(availableWidth, 0);
                }
            };

            // SƠ ĐỒ GHẾ
            seatMatrix = new SeatMatrixUC
            {
                Dock = DockStyle.Fill
            };

            seatMatrix.OnSeatSelectionChanged +=
                SeatMatrix_OnSeatSelectionChanged;

            // Thứ tự thêm control quan trọng đối với Dock
            Controls.Add(seatMatrix);
            Controls.Add(pnlLeftLegend);
            Controls.Add(tlpHeader);
            Controls.Add(tlpBottomBar);

            UpdateSeatInformation();
        }

        // ==========================================
        // 11. TẠO LABEL STEPPER VÀ CHÚ THÍCH
        // ==========================================

        private Label CreateStepLabel(string text, bool isActive)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = isActive ? ColorGold : ColorTextMuted,
                AutoSize = true,
                Margin = new Padding(4, 6, 4, 6)
            };
        }

        private Label CreateDividerLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(70, 72, 95),
                AutoSize = true,
                Margin = new Padding(4, 6, 4, 6)
            };
        }

        private Control CreateLegendItem(
            string title,
            string price,
            Color color)
        {
            var tlpItem = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 12)
            };
            tlpItem.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpItem.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpItem.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpItem.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Panel box = new Panel
            {
                Size = new Size(20, 20),
                BackColor = color,
                Margin = new Padding(0, 2, 10, 0)
            };

            Label lblT = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                MaximumSize = new Size(210, 0),
                Margin = new Padding(0, 0, 0, 2)
            };

            Label lblP = new Label
            {
                Text = price,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = ColorTextMuted,
                AutoSize = true,
                Margin = new Padding(0)
            };

            tlpItem.Controls.Add(box, 0, 0);
            tlpItem.SetRowSpan(box, 2);
            tlpItem.Controls.Add(lblT, 1, 0);
            tlpItem.Controls.Add(lblP, 1, 1);

            return tlpItem;
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

            currentTotal = selectedSeats.Sum(
                seat => seatMatrix.GetSeatPrice(seat));

            SyncBookingContext(selectedSeats);

            OnContinueToFood?.Invoke(
                this,
                (
                    new List<string>(selectedSeats),
                    currentTotal
                ));
        }
    }
}