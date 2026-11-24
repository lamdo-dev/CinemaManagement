
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using CinemaManagement.Models;
using CinemaManagement.Models.Enums;
using CinemaManagement.Services;

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
        private readonly List<Label> rowLabels = new();
        private int matrixRows;
        private int matrixColumns;

        // Lưu hệ số giá của từng ghế
        private readonly Dictionary<string, decimal> seatMultipliers =
            new(StringComparer.OrdinalIgnoreCase);

        // Lưu giá thực tế đã tính cho từng ghế (ưu tiên PricingRules)
        private readonly Dictionary<string, decimal> seatPrices =
            new(StringComparer.OrdinalIgnoreCase);

        // Giá cơ bản của suất chiếu
        private decimal basePrice = PricingService.DefaultBasePrice;

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
            AutoScroll = false;

            pnlScreen = new Panel
            {
                Height = 32,
                BackColor = Color.FromArgb(41, 128, 185),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
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
                BackColor = Color.Transparent,
                AutoScroll = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                         AnchorStyles.Left | AnchorStyles.Right
            };

            Controls.Add(pnlScreen);
            Controls.Add(pnlMatrixContainer);
            Resize += (s, e) => LayoutMatrix();

            GenerateSeatMatrix(10, 14);
        }

        // Căn chỉnh sơ đồ theo kích thước vùng hiển thị hiện tại.
        // Nếu màn hình quá hẹp, chỉ vùng sơ đồ cuộn ngang; không làm méo ghế.
        private void LayoutMatrix()
        {
            if (pnlScreen == null || pnlMatrixContainer == null)
                return;

            int width = Math.Max(100, ClientSize.Width);
            int height = Math.Max(100, ClientSize.Height);

            int sidePadding = Math.Clamp((int)(width * 0.035), 12, 32);
            pnlScreen.SetBounds(sidePadding, 15,
                Math.Max(100, width - sidePadding * 2), 32);

            pnlMatrixContainer.SetBounds(8, 58,
                Math.Max(80, width - 16), Math.Max(80, height - 66));

            if (matrixRows <= 0 || matrixColumns <= 0)
                return;

            int viewportWidth = Math.Max(80, pnlMatrixContainer.ClientSize.Width);
            int viewportHeight = Math.Max(80, pnlMatrixContainer.ClientSize.Height);

            const int rowLabelWidth = 28;
            const int leftPadding = 8;
            const int rightPadding = 12;
            const int topPadding = 12;
            const int bottomPadding = 12;

            int gap = viewportWidth < 520 ? 4 : 7;

            // Ưu tiên đủ rộng để hiển thị mã ghế A10, B11...
            // Nếu không đủ chỗ, giữ nguyên kích thước tối thiểu và cho cuộn ngang.
            int availableSeatWidth = viewportWidth - rowLabelWidth -
                leftPadding - rightPadding - gap * (matrixColumns - 1);

            int seatWidth = Math.Clamp(
                availableSeatWidth / matrixColumns, 46, 60);

            int availableSeatHeight = viewportHeight - topPadding -
                bottomPadding - gap * (matrixRows - 1);

            int seatHeight = Math.Clamp(
                availableSeatHeight / matrixRows, 32, 46);

            int gridWidth = matrixColumns * seatWidth +
                (matrixColumns - 1) * gap;
            int gridHeight = matrixRows * seatHeight +
                (matrixRows - 1) * gap;

            int contentWidth = rowLabelWidth + leftPadding +
                gridWidth + rightPadding;

            int startX = contentWidth <= viewportWidth
                ? Math.Max(rowLabelWidth + leftPadding,
                    (viewportWidth - gridWidth) / 2)
                : rowLabelWidth + leftPadding;

            int startY = Math.Max(topPadding,
                (viewportHeight - gridHeight) / 2);

            for (int r = 0; r < rowLabels.Count; r++)
            {
                rowLabels[r].SetBounds(3,
                    startY + r * (seatHeight + gap),
                    rowLabelWidth, seatHeight);
            }

            foreach (Button button in seatButtons)
            {
                string code = button.Name;
                if (code.Length < 2 ||
                    !int.TryParse(code.Substring(1), out int column))
                    continue;

                int row = char.ToUpperInvariant(code[0]) - 'A';
                if (row < 0 || row >= matrixRows ||
                    column < 1 || column > matrixColumns)
                    continue;

                button.SetBounds(
                    startX + (column - 1) * (seatWidth + gap),
                    startY + row * (seatHeight + gap),
                    seatWidth, seatHeight);

                button.Font = new Font("Segoe UI",
                    seatWidth >= 54 ? 9f : 8f, FontStyle.Bold);
                button.TextAlign = ContentAlignment.MiddleCenter;
                button.Padding = Padding.Empty;
            }

            pnlMatrixContainer.AutoScrollMinSize = new Size(
                Math.Max(viewportWidth, contentWidth),
                Math.Max(viewportHeight, startY + gridHeight + bottomPadding));
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
                PricingService.DefaultBasePrice);
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
                : PricingService.DefaultBasePrice;

            var previousSelection = new HashSet<string>(
                SelectedSeatCodes,
                StringComparer.OrdinalIgnoreCase);

            ClearMatrixControls();

            var occupiedSeats = new HashSet<string>(
                extraOccupiedSeats ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            matrixRows = rows;
            matrixColumns = cols;
            rowLabels.Clear();

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
                    Size = new Size(28, 32),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                rowLabels.Add(lblRow);
                pnlMatrixContainer.Controls.Add(lblRow);

                for (int c = 1; c <= cols; c++)
                {
                    string seatCode = $"{rowLetter}{c}";

                    bool isOccupied = occupiedSeats.Contains(seatCode);

                    decimal multiplier = GetDefaultMultiplier(r);

                    seatMultipliers[seatCode] = multiplier;
                    seatPrices[seatCode] = Math.Round(basePrice * multiplier, 0);

                    Color seatColor = GetSeatColor(multiplier);

                    Button btnSeat = new Button
                    {
                        // Hiển thị mã ghế đầy đủ để tránh nhầm 10, 11, 12 thành 1.
                        // Name giữ nguyên mã ghế để tương thích database và đặt vé.
                        Text = seatCode,
                        Name = seatCode,
                        Size = new Size(40, 34),
                        MinimumSize = new Size(38, 30),
                        Padding = new Padding(0),
                        Margin = new Padding(0),
                        Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                        ForeColor = Color.White,
                        TextAlign = ContentAlignment.MiddleCenter,
                        BackColor = isOccupied
                            ? ColorOccupied
                            : seatColor,
                        UseVisualStyleBackColor = false,
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
            LayoutMatrix();
        }

        // ==========================================
        // 8. TẠO SƠ ĐỒ THEO DỮ LIỆU GHẾ THỰC TẾ
        // ==========================================

        public void GenerateSeatMatrix(
            int rows,
            int cols,
            IEnumerable<Seat> roomSeats,
            IEnumerable<string>? occupiedSeats,
            decimal ticketBasePrice,
            IReadOnlyDictionary<string, decimal>? explicitSeatPrices = null)
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

            // Cập nhật hệ số giá, giá thực tế và màu sắc theo dữ liệu thực tế
            foreach (Seat seat in seatList)
            {
                string code = seat.SeatNumber?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(code))
                {
                    continue;
                }

                decimal multiplier = seat.PriceMultiplier > 0
                    ? Convert.ToDecimal(seat.PriceMultiplier)
                    : 1.0m;

                seatMultipliers[code] = multiplier;

                if (explicitSeatPrices != null &&
                    explicitSeatPrices.TryGetValue(code, out decimal explicitPrice) &&
                    explicitPrice > 0)
                {
                    seatPrices[code] = explicitPrice;
                }
                else
                {
                    seatPrices[code] = Math.Round(basePrice * multiplier, 0);
                }

                Button? button = seatButtons.FirstOrDefault(
                    b => string.Equals(
                        b.Name,
                        code,
                        StringComparison.OrdinalIgnoreCase));

                if (button == null)
                {
                    continue;
                }

                Color color = GetSeatColor(seat.Type, multiplier);

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
            rowLabels.Clear();
            pnlMatrixContainer.AutoScrollMinSize = Size.Empty;
            pnlMatrixContainer.AutoScrollPosition = Point.Empty;

            seatMultipliers.Clear();
            seatPrices.Clear();

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

        private static Color GetSeatColor(SeatType seatType, decimal multiplier)
        {
            if (seatType == SeatType.Couple)
                return ColorPlus;

            if (seatType == SeatType.VIP)
                return ColorVIP;

            return GetSeatColor(multiplier);
        }

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

            if (seatPrices.TryGetValue(seatCode, out decimal explicitPrice))
            {
                return explicitPrice;
            }

            if (!seatMultipliers.TryGetValue(
                seatCode,
                out decimal multiplier))
            {
                return 0m;
            }

            return Math.Round(basePrice * multiplier, 0);
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