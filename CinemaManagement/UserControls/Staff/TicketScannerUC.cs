using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Utils;

namespace CinemaManagement.UserControls.Staff
{
    public partial class TicketScannerUC : UserControl
    {
        private static readonly CultureInfo VN = new CultureInfo("vi-VN");
        private readonly Color ColorValid = Color.FromArgb(46, 204, 113);
        private readonly Color ColorWarning = Color.FromArgb(241, 196, 15);
        private readonly Color ColorError = Color.FromArgb(231, 76, 60);
        private readonly Color ColorIdle = Color.FromArgb(52, 152, 219);

        // Controls
        private Panel qrFrame = null!;
        private TextBox txtCode = null!;
        private Label lblStatus = null!;
        private Label lblTicketDetail = null!;
        private Panel pnlStatusBar = null!;
        private Label lblHeader = null!;
        private DataGridView dgvLogs = null!;
        private ListBox lstPending = null!;

        private Color _frameColor;

        // Sự kiện đẩy ra bên ngoài
        public event EventHandler<TicketScanResult>? OnTicketScanned;
        public event EventHandler<TicketScanResult>? OnTicketAccepted;
        public event EventHandler<TicketScanResult>? OnTicketRejected;
        public event EventHandler? OnScanNextClicked;
        public event EventHandler? OnOpenPOSClicked;

        public TicketScannerUC()
        {
            _frameColor = ColorIdle;
            InitializeComponentManual();
            RefreshLogs();
            RefreshPending();
            ResetResult();
        }

        // Mở màn hình soát vé và điền sẵn mã (VD: vé vừa bán tại quầy)
        public TicketScannerUC(string? prefillCode) : this()
        {
            if (!string.IsNullOrWhiteSpace(prefillCode))
            {
                txtCode.Text = prefillCode;
            }
        }

        private void InitializeComponentManual()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(15, 20, 30);

            // =========================================================
            // CỘT TRÁI: KHUNG QUÉT + KẾT QUẢ
            // =========================================================
            Panel pnlCamera = new Panel
            {
                Dock = DockStyle.Left,
                Width = 430,
                Padding = new Padding(25),
                BackColor = Color.FromArgb(20, 26, 38)
            };

            qrFrame = new Panel
            {
                Height = 210,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(10, 15, 25)
            };
            qrFrame.Paint += QrFrame_Paint;

            Panel pnlInput = new Panel { Dock = DockStyle.Top, Height = 92, Padding = new Padding(0, 12, 0, 0) };
            Label lblInput = new Label
            {
                Text = "Mã vé (quét bằng máy đọc hoặc nhập tay rồi nhấn Enter):",
                ForeColor = Color.FromArgb(150, 160, 180),
                Font = new Font("Segoe UI", 8.5f),
                Location = new Point(0, 12),
                AutoSize = true
            };
            txtCode = new TextBox
            {
                Location = new Point(0, 34),
                Size = new Size(260, 32),
                Font = new Font("Consolas", 12f, FontStyle.Bold),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                CharacterCasing = CharacterCasing.Upper,
                PlaceholderText = "BK-... / POS-..."
            };
            txtCode.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    PerformScan(txtCode.Text);
                }
            };

            Button btnCheck = new Button
            {
                Text = "🔍 KIỂM TRA",
                Location = new Point(268, 33),
                Size = new Size(112, 32),
                BackColor = ColorIdle,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCheck.FlatAppearance.BorderSize = 0;
            btnCheck.Click += (s, e) => PerformScan(txtCode.Text);

            pnlInput.Controls.AddRange(new Control[] { lblInput, txtCode, btnCheck });

            Panel pnlResult = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0) };

            pnlStatusBar = new Panel { Dock = DockStyle.Top, Height = 6, BackColor = ColorIdle };

            lblStatus = new Label
            {
                Text = "SẴN SÀNG QUÉT",
                ForeColor = ColorIdle,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblTicketDetail = new Label
            {
                Text = "",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Dock = DockStyle.Fill
            };

            Panel pnlButtons = new Panel { Dock = DockStyle.Bottom, Height = 45 };
            Button btnScanNext = new Button
            {
                Text = "⟳ Quét vé tiếp theo",
                Location = new Point(0, 0),
                Size = new Size(185, 45),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnScanNext.FlatAppearance.BorderSize = 0;
            btnScanNext.Click += (s, e) =>
            {
                ResetResult();
                OnScanNextClicked?.Invoke(this, EventArgs.Empty);
            };

            Button btnOpenPOS = new Button
            {
                Text = "🛒 Về quầy bán vé",
                Location = new Point(195, 0),
                Size = new Size(185, 45),
                BackColor = Color.FromArgb(124, 58, 237),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnOpenPOS.FlatAppearance.BorderSize = 0;
            btnOpenPOS.Click += (s, e) => OnOpenPOSClicked?.Invoke(this, EventArgs.Empty);

            pnlButtons.Controls.AddRange(new Control[] { btnScanNext, btnOpenPOS });

            pnlResult.Controls.Add(lblTicketDetail);
            pnlResult.Controls.Add(lblStatus);
            pnlResult.Controls.Add(pnlStatusBar);
            pnlResult.Controls.Add(pnlButtons);

            pnlCamera.Controls.Add(pnlResult);
            pnlCamera.Controls.Add(pnlInput);
            pnlCamera.Controls.Add(qrFrame);

            // =========================================================
            // CỘT PHẢI: DANH SÁCH VÉ CHỜ SOÁT + NHẬT KÝ CỬA
            // =========================================================
            Panel pnlDoorList = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(25),
                BackColor = Color.FromArgb(15, 20, 30)
            };

            lblHeader = new Label
            {
                Text = "LIVE DOOR LIST",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 60
            };

            Panel pnlPending = new Panel { Dock = DockStyle.Top, Height = 170, Padding = new Padding(0, 0, 0, 12) };
            Label lblPending = new Label
            {
                Text = "🎫 VÉ CHƯA SOÁT (nhấp đúp để soát nhanh)",
                ForeColor = Color.FromArgb(150, 160, 180),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 24
            };
            lstPending = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 26, 38),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 9.5f)
            };
            lstPending.DoubleClick += (s, e) =>
            {
                if (lstPending.SelectedItem is PendingItem item)
                {
                    txtCode.Text = item.Code;
                    PerformScan(item.Code);
                }
            };
            pnlPending.Controls.Add(lstPending);
            pnlPending.Controls.Add(lblPending);

            dgvLogs = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(20, 26, 38),
                ForeColor = Color.White,
                GridColor = Color.FromArgb(35, 45, 60),
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 34,
                DefaultCellStyle = { BackColor = Color.FromArgb(20, 26, 38), ForeColor = Color.White, SelectionBackColor = Color.FromArgb(40, 50, 75) },
                ColumnHeadersDefaultCellStyle = { BackColor = Color.FromArgb(30, 41, 59), ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold) }
            };

            dgvLogs.Columns.Add("colTime", "THỜI GIAN");
            dgvLogs.Columns.Add("colCode", "MÃ VÉ");
            dgvLogs.Columns.Add("colMovie", "PHIM / SUẤT");
            dgvLogs.Columns.Add("colSeat", "GHẾ");
            dgvLogs.Columns.Add("colStatus", "TRẠNG THÁI");

            // Nhấp vào dòng nhật ký -> xem lại chi tiết vé
            dgvLogs.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                string? code = dgvLogs.Rows[e.RowIndex].Cells["colCode"].Value?.ToString();
                var booking = UserSession.FindBooking(code ?? string.Empty);
                if (booking != null)
                {
                    lblTicketDetail.Text = BuildDetail(booking);
                }
            };

            pnlDoorList.Controls.Add(dgvLogs);
            pnlDoorList.Controls.Add(pnlPending);
            pnlDoorList.Controls.Add(lblHeader);

            this.Controls.Add(pnlDoorList);
            this.Controls.Add(pnlCamera);
        }

        // Vẽ khung quét QR với 4 góc nhấn màu theo trạng thái
        private void QrFrame_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int size = Math.Min(qrFrame.Width, qrFrame.Height) - 40;
            int x = (qrFrame.Width - size) / 2;
            int y = (qrFrame.Height - size) / 2;
            int len = 28;

            using var pen = new Pen(_frameColor, 5) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(pen, x, y, x + len, y); g.DrawLine(pen, x, y, x, y + len);
            g.DrawLine(pen, x + size, y, x + size - len, y); g.DrawLine(pen, x + size, y, x + size, y + len);
            g.DrawLine(pen, x, y + size, x + len, y + size); g.DrawLine(pen, x, y + size, x, y + size - len);
            g.DrawLine(pen, x + size, y + size, x + size - len, y + size); g.DrawLine(pen, x + size, y + size, x + size, y + size - len);

            using var scanPen = new Pen(Color.FromArgb(120, _frameColor), 2);
            g.DrawLine(scanPen, x + 10, y + size / 2, x + size - 10, y + size / 2);

            using var font = new Font("Segoe UI", 9f);
            using var brush = new SolidBrush(Color.FromArgb(150, 160, 180));
            var sf = new StringFormat { Alignment = StringAlignment.Center };
            g.DrawString("Đưa mã QR vé vào khung", font, brush, new RectangleF(0, y + size + 4, qrFrame.Width, 20), sf);
        }

        // =========================================================
        // XỬ LÝ SOÁT VÉ
        // =========================================================
        public void PerformScan(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show("Vui lòng quét hoặc nhập mã vé!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCode.Focus();
                return;
            }

            var result = UserSession.ScanTicket(code);
            ShowResult(result);
            RefreshLogs();
            RefreshPending();

            OnTicketScanned?.Invoke(this, result);
            if (result.IsValid)
                OnTicketAccepted?.Invoke(this, result);
            else
                OnTicketRejected?.Invoke(this, result);

            txtCode.SelectAll();
            txtCode.Focus();
        }

        private void ShowResult(TicketScanResult r)
        {
            Color color = r.Status switch
            {
                TicketScanStatus.Valid => ColorValid,
                TicketScanStatus.AlreadyUsed => ColorWarning,
                _ => ColorError
            };

            string title = r.Status switch
            {
                TicketScanStatus.Valid => "✔ VÉ HỢP LỆ - MỜI VÀO",
                TicketScanStatus.AlreadyUsed => "⚠ VÉ ĐÃ ĐƯỢC SỬ DỤNG",
                TicketScanStatus.Expired => "✖ VÉ HẾT HẠN",
                _ => "✖ KHÔNG TÌM THẤY VÉ"
            };

            _frameColor = color;
            pnlStatusBar.BackColor = color;
            lblStatus.ForeColor = color;
            lblStatus.Text = title;

            lblTicketDetail.Text = r.Booking != null
                ? $"{r.Message}\n\n{BuildDetail(r.Booking)}"
                : $"{r.Message}\nMã: {r.Code}";

            qrFrame.Invalidate();
            if (!r.IsValid) System.Media.SystemSounds.Exclamation.Play();
        }

        private static string BuildDetail(CinemaManagement.UserControls.Customer.BookingResultEventArgs b)
        {
            string movie = b.Movie?.Title ?? "(Chỉ mua bắp nước)";
            string time = b.Showtime != null ? $"{b.Showtime.StartTime:HH:mm dd/MM/yyyy} - Phòng 0{b.Showtime.RoomId}" : "--";
            string seats = b.Seats.Count > 0 ? string.Join(", ", b.Seats) : "--";
            string food = b.FoodCart.Count > 0 ? string.Join(", ", b.FoodCart.Select(x => $"{x.Value.Name} x{x.Value.Qty}")) : "Không";
            string customer = string.IsNullOrWhiteSpace(b.CustomerName) ? (UserSession.CurrentUser?.FullName ?? "Khách hàng") : b.CustomerName;

            return $"MÃ VÉ: {b.BookingCode}  [{b.Channel}]\n" +
                   $"PHIM: {movie}\n" +
                   $"SUẤT: {time}\n" +
                   $"GHẾ: {seats}\n" +
                   $"BẮP NƯỚC: {food}\n" +
                   $"KHÁCH: {customer}\n" +
                   $"TỔNG TIỀN: {b.TotalAmount.ToString("N0", VN)} đ";
        }

        private void ResetResult()
        {
            _frameColor = ColorIdle;
            pnlStatusBar.BackColor = ColorIdle;
            lblStatus.ForeColor = ColorIdle;
            lblStatus.Text = "SẴN SÀNG QUÉT";
            lblTicketDetail.Text = "Quét mã QR trên vé điện tử hoặc nhập mã đơn hàng\n(VD: BK-20261111-165012 hoặc POS-20261111-170105).";
            txtCode.Text = string.Empty;
            qrFrame.Invalidate();
            txtCode.Focus();
        }

        private void RefreshLogs()
        {
            dgvLogs.Rows.Clear();
            foreach (var log in UserSession.ScanLogs.AsEnumerable().Reverse())
            {
                string movie = log.Booking?.Movie != null
                    ? $"{log.Booking.Movie.Title} ({log.Booking.Showtime?.StartTime:HH:mm})"
                    : "--";
                string seats = log.Booking != null && log.Booking.Seats.Count > 0 ? string.Join(", ", log.Booking.Seats) : "--";
                string status = log.Status switch
                {
                    TicketScanStatus.Valid => "Hợp lệ",
                    TicketScanStatus.AlreadyUsed => "Vé trùng",
                    TicketScanStatus.Expired => "Hết hạn",
                    _ => "Không tồn tại"
                };

                int idx = dgvLogs.Rows.Add(log.ScanTime.ToString("HH:mm:ss"), log.Code, movie, seats, status);
                dgvLogs.Rows[idx].Cells["colStatus"].Style.ForeColor = log.Status switch
                {
                    TicketScanStatus.Valid => ColorValid,
                    TicketScanStatus.AlreadyUsed => ColorWarning,
                    _ => ColorError
                };
            }

            var ticketBookings = UserSession.BookingHistory.Where(b => b.Seats.Count > 0).ToList();
            int totalSeats = ticketBookings.Sum(b => b.Seats.Count);
            int checkedSeats = ticketBookings.Where(b => UserSession.IsCheckedIn(b.BookingCode)).Sum(b => b.Seats.Count);
            double percent = totalSeats > 0 ? checkedSeats * 100.0 / totalSeats : 0;
            int rejected = UserSession.ScanLogs.Count(l => !l.IsValid);

            lblHeader.Text = $"LIVE DOOR LIST\nĐÃ VÀO: {checkedSeats} / {totalSeats} khách ({percent:0.#}%)   •   Từ chối: {rejected}";
        }

        private void RefreshPending()
        {
            lstPending.Items.Clear();
            foreach (var b in UserSession.BookingHistory
                         .Where(b => b.Seats.Count > 0 && !UserSession.IsCheckedIn(b.BookingCode))
                         .Reverse())
            {
                lstPending.Items.Add(new PendingItem(b.BookingCode,
                    $"{b.BookingCode,-24} {b.Movie?.Title} | {b.Showtime?.StartTime:HH:mm} | {string.Join(",", b.Seats)}"));
            }

            if (lstPending.Items.Count == 0)
            {
                lstPending.Items.Add("(Không có vé nào đang chờ soát)");
            }
        }

        private sealed class PendingItem
        {
            public string Code { get; }
            private readonly string _display;
            public PendingItem(string code, string display) { Code = code; _display = display; }
            public override string ToString() => _display;
        }
    }
}
