
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using CinemaManagement.Models;

namespace CinemaManagement.UserControls.Common
{
    public partial class SeatMatrixUC : UserControl
    {
        // ==========================================
        // 1. MÀU SẮC GHẾ
        // ==========================================

        public static readonly Color ColorStandard =
            Color.FromArgb(46, 204, 113);

        public static readonly Color ColorVIP =
            Color.FromArgb(155, 89, 182);

        public static readonly Color ColorPlus =
            Color.FromArgb(230, 126, 34);

        public static readonly Color ColorSelected =
            Color.FromArgb(52, 152, 219);

        public static readonly Color ColorOccupied =
            Color.FromArgb(70, 70, 85);

        private readonly Color ColorBackground =
            Color.FromArgb(18, 19, 26);

        // ==========================================
        // 2. CONTROL
        // ==========================================

        private Panel pnlScreen = null!;
        private Panel pnlMatrixContainer = null!;

        private readonly List<Button> seatButtons = new();

        // Lưu hệ số giá của từng ghế
        private readonly Dictionary<string, decimal> seatMultipliers =
            new(StringComparer.OrdinalIgnoreCase);

        // Giá cơ bản của suất chiếu
        private decimal basePrice = 90000m;

        // ==========================================
        // 3. DỮ LIỆU GHẾ
        // ==========================================

        public List<string> SelectedSeatCodes { get; private set; } = new();

        public event EventHandler<List<string>>? OnSeatSelectionChanged;

        // ==========================================
        // 4. CONSTRUCTOR
        // ==========================================

        public SeatMatrixUC()
        {
            InitializeComponents();
        }

        // ==========================================
        // 5. KHỞI TẠO GIAO DIỆN
        // ==========================================

        private void InitializeComponents()
        {
            Dock = DockStyle.Fill;
            BackColor = ColorBackground;
            AutoScroll = true;

            pnlScreen = new Panel
            {
                Size = new Size(600, 32),
                Location = new Point(100, 15),
                BackColor = Color.FromArgb(41, 128, 185)
            };

            Label lblScreen = new Label
            {
                Text = "MÀN HÌNH CHIẾU",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlScreen.Controls.Add(lblScreen);

            pnlMatrixContainer = new Panel
            {
                Location = new Point(20, 65),
                Size = new Size(760, 480),
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            Controls.Add(pnlScreen);
            Controls.Add(pnlMatrixContainer);

            // Sơ đồ mặc định để tương thích với code cũ
            GenerateSeatMatrix(10, 14);
        }

        // ==========================================
        // 6. TẠO SƠ ĐỒ GHẾ - PHIÊN BẢN CŨ
        // ==========================================

        public void GenerateSeatMatrix(
            int rows,
            int cols,
            IEnumerable<string>? extraOccupiedSeats = null)
        {
            GenerateSeatMatrix(
                rows,
                cols,
                extraOccupiedSeats,
                90000m);
        }

        // ==========================================
        // 7. TẠO SƠ ĐỒ GHẾ VỚI GIÁ SUẤT CHIẾU
        // ==========================================

        public void GenerateSeatMatrix(
            int rows,
            int cols,
            IEnumerable<string>? extraOccupiedSeats,
            decimal ticketBasePrice)
        {
            ValidateMatrixSize(rows, cols);

            basePrice = ticketBasePrice > 0
                ? ticketBasePrice
                : 90000m;

            var previousSelection = new HashSet<string>(
                SelectedSeatCodes,
                StringComparer.OrdinalIgnoreCase);

            ClearMatrixControls();

            var occupiedSeats = new HashSet<string>(
                extraOccupiedSeats ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            int seatSize = 32;
            int gap = 8;
            int startX = 50;
            int startY = 20;

            for (int r = 0; r < rows; r++)
            {
                char rowLetter = (char)('A' + r);

                Label lblRow = new Label
                {
                    Text = rowLetter.ToString(),
                    Font = new Font(
                        "Segoe UI",
                        9.5f,
                        FontStyle.Bold),
                    ForeColor = Color.White,
                    Size = new Size(30, seatSize),
                    Location = new Point(
                        10,
                        startY + r * (seatSize + gap)),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                pnlMatrixContainer.Controls.Add(lblRow);

                for (int c = 1; c <= cols; c++)
                {
                    string seatCode = $"{rowLetter}{c}";

                    bool isOccupied = occupiedSeats.Contains(seatCode);

                    decimal multiplier = GetDefaultMultiplier(r);

                    seatMultipliers[seatCode] = multiplier;

                    Color seatColor = GetSeatColor(multiplier);

                    Button btnSeat = new Button
                    {
                        Text = c.ToString(),
                        Name = seatCode,
                        Size = new Size(seatSize, seatSize),
                        Location = new Point(
                            startX + (c - 1) * (seatSize + gap),
                            startY + r * (seatSize + gap)),
                        Font = new Font(
                            "Segoe UI",
                            8,
                            FontStyle.Bold),
                        ForeColor = Color.White,
                        BackColor = isOccupied
                            ? ColorOccupied
                            : seatColor,
                        Enabled = !isOccupied,
                        FlatStyle = FlatStyle.Flat,
                        Cursor = isOccupied
                            ? Cursors.No
                            : Cursors.Hand,
                        Tag = seatColor
                    };

                    btnSeat.FlatAppearance.BorderSize = 0;

                    if (!isOccupied)
                    {
                        btnSeat.Click += BtnSeat_Click;
                    }

                    seatButtons.Add(btnSeat);

                    pnlMatrixContainer.Controls.Add(btnSeat);
                }
            }

            // Khôi phục lựa chọn cũ nếu ghế vẫn còn khả dụng
            RestoreSelectedSeats(previousSelection);
        }

        // ==========================================
        // 8. TẠO SƠ ĐỒ THEO DỮ LIỆU GHẾ THỰC TẾ
        // ==========================================

        public void GenerateSeatMatrix(
            int rows,
            int cols,
            IEnumerable<Seat> roomSeats,
            IEnumerable<string>? occupiedSeats,
            decimal ticketBasePrice)
        {
            if (roomSeats == null)
            {
                throw new ArgumentNullException(nameof(roomSeats));
            }

            List<Seat> seatList = roomSeats.ToList();

            ValidateMatrixSize(rows, cols);

            // Tạo sơ đồ theo kích thước phòng
            GenerateSeatMatrix(
                rows,
                cols,
                occupiedSeats,
                ticketBasePrice);

            // Danh sách mã ghế tồn tại trong database
            var existingSeatCodes = new HashSet<string>(
                seatList
                    .Where(s => !string.IsNullOrWhiteSpace(s.SeatNumber))
                    .Select(s => s.SeatNumber.Trim()),
                StringComparer.OrdinalIgnoreCase);

            // Cập nhật hệ số giá và màu sắc theo dữ liệu thực tế
            foreach (Seat seat in seatList)
            {
                string code = seat.SeatNumber?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(code))
                {
                    continue;
                }

                // PriceMultiplier trong Seat là double.
                // Dictionary của SeatMatrixUC sử dụng decimal.
                decimal multiplier = seat.PriceMultiplier > 0
                    ? Convert.ToDecimal(seat.PriceMultiplier)
                    : 1.0m;

                seatMultipliers[code] = multiplier;

                Button? button = seatButtons.FirstOrDefault(
                    b => string.Equals(
                        b.Name,
                        code,
                        StringComparison.OrdinalIgnoreCase));

                if (button == null)
                {
                    continue;
                }

                Color color = GetSeatColor(multiplier);

                button.Tag = color;

                if (button.Enabled)
                {
                    button.BackColor = color;
                }
            }

            // Ẩn những vị trí không có ghế trong database.
            // Tránh cho khách hàng chọn ghế không tồn tại.
            foreach (Button button in seatButtons)
            {
                if (!existingSeatCodes.Contains(button.Name))
                {
                    button.Visible = false;
                    button.Enabled = false;

                    SelectedSeatCodes.RemoveAll(
                        s => string.Equals(
                            s,
                            button.Name,
                            StringComparison.OrdinalIgnoreCase));
                }
            }

            NotifySelectionChanged();
        }

        // ==========================================
        // 9. KIỂM TRA KÍCH THƯỚC SƠ ĐỒ
        // ==========================================

        private static void ValidateMatrixSize(
            int rows,
            int cols)
        {
            if (rows < 1 || rows > 26)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rows),
                    "Số hàng phải nằm trong khoảng từ 1 đến 26.");
            }

            if (cols < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cols),
                    "Số cột phải lớn hơn 0.");
            }
        }

        // ==========================================
        // 10. XÓA SƠ ĐỒ CŨ
        // ==========================================

        private void ClearMatrixControls()
        {
            foreach (Control control in pnlMatrixContainer.Controls)
            {
                control.Dispose();
            }

            pnlMatrixContainer.Controls.Clear();

            seatButtons.Clear();

            seatMultipliers.Clear();

            SelectedSeatCodes.Clear();
        }

        // ==========================================
        // 11. HỆ SỐ GIÁ MẶC ĐỊNH
        // ==========================================

        private static decimal GetDefaultMultiplier(int rowIndex)
        {
            if (rowIndex >= 3 && rowIndex <= 6)
            {
                return 1.2m;
            }

            if (rowIndex > 6)
            {
                return 2.0m;
            }

            return 1.0m;
        }

        // ==========================================
        // 12. MÀU SẮC THEO HỆ SỐ GIÁ
        // ==========================================

        private static Color GetSeatColor(decimal multiplier)
        {
            if (multiplier >= 1.8m)
            {
                return ColorPlus;
            }

            if (multiplier > 1.0m)
            {
                return ColorVIP;
            }

            return ColorStandard;
        }

        // ==========================================
        // 13. TÍNH GIÁ TIỀN GHẾ
        // ==========================================

        public decimal GetSeatPrice(string seatCode)
        {
            if (string.IsNullOrWhiteSpace(seatCode))
            {
                return 0m;
            }

            if (!seatMultipliers.TryGetValue(
                seatCode,
                out decimal multiplier))
            {
                return 0m;
            }

            return basePrice * multiplier;
        }

        // ==========================================
        // 14. XỬ LÝ CHỌN / BỎ CHỌN GHẾ
        // ==========================================

        private void BtnSeat_Click(object? sender, EventArgs e)
        {
            if (sender is not Button btn)
            {
                return;
            }

            if (!btn.Enabled || !btn.Visible)
            {
                return;
            }

            string seatCode = btn.Name;

            Color originalColor = (Color)btn.Tag!;

            bool isSelected = SelectedSeatCodes.Contains(
                seatCode,
                StringComparer.OrdinalIgnoreCase);

            if (isSelected)
            {
                SelectedSeatCodes.RemoveAll(
                    x => string.Equals(
                        x,
                        seatCode,
                        StringComparison.OrdinalIgnoreCase));

                btn.BackColor = originalColor;
            }
            else
            {
                SelectedSeatCodes.Add(seatCode);

                btn.BackColor = ColorSelected;
            }

            NotifySelectionChanged();
        }

        // ==========================================
        // 15. KHÔI PHỤC GHẾ ĐÃ CHỌN
        // ==========================================

        public void RestoreSelectedSeats(
            IEnumerable<string>? seatCodes)
        {
            SelectedSeatCodes.Clear();

            var requestedSeats = new HashSet<string>(
                seatCodes ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            foreach (Button btn in seatButtons)
            {
                if (btn.Enabled &&
                    btn.Visible &&
                    requestedSeats.Contains(btn.Name))
                {
                    SelectedSeatCodes.Add(btn.Name);

                    btn.BackColor = ColorSelected;
                }
            }

            NotifySelectionChanged();
        }

        // ==========================================
        // 16. LẤY DANH SÁCH GHẾ ĐÃ CHỌN
        // ==========================================

        public List<string> GetSelectedSeats()
        {
            return new List<string>(SelectedSeatCodes);
        }

        // ==========================================
        // 17. XÓA TẤT CẢ GHẾ ĐÃ CHỌN
        // ==========================================

        public void ClearSelectedSeats()
        {
            foreach (Button btn in seatButtons)
            {
                if (btn.Enabled && btn.Visible)
                {
                    btn.BackColor = (Color)btn.Tag!;
                }
            }

            SelectedSeatCodes.Clear();

            NotifySelectionChanged();
        }

        // ==========================================
        // 18. THÔNG BÁO THAY ĐỔI GHẾ
        // ==========================================

        private void NotifySelectionChanged()
        {
            OnSeatSelectionChanged?.Invoke(
                this,
                new List<string>(SelectedSeatCodes));
        }

        // ==========================================
        // 19. PHƯƠNG THỨC TƯƠNG THÍCH CODE CŨ
        // ==========================================

        // Giữ lại để các màn hình cũ không bị lỗi.
        // Màn hình mới nên sử dụng GetSeatPrice().
        public static decimal GetSeatPriceByCode(string seatCode)
        {
            if (string.IsNullOrWhiteSpace(seatCode))
            {
                return 90000m;
            }

            int rowIndex =
                char.ToUpperInvariant(seatCode[0]) - 'A';

            if (rowIndex >= 3 && rowIndex <= 6)
            {
                return 120000m;
            }

            if (rowIndex > 6)
            {
                return 180000m;
            }

            return 90000m;
        }
    }
}
