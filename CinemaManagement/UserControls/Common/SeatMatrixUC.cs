
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CinemaManagement.UserControls.Common
{
    public partial class SeatMatrixUC : UserControl
    {
        // Màu sắc trạng thái ghế
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

        private Panel pnlScreen = null!;
        private Panel pnlMatrixContainer = null!;

        private readonly List<Button> seatButtons = new();

        // Danh sách ghế đang được khách hàng chọn
        public List<string> SelectedSeatCodes { get; private set; }
            = new();

        // Sự kiện thông báo khi danh sách ghế thay đổi
        public event EventHandler<List<string>>?
            OnSeatSelectionChanged;

        public SeatMatrixUC()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(18, 19, 26);
            AutoScroll = true;

            // Thanh màn hình
            pnlScreen = new Panel
            {
                Size = new Size(600, 32),
                Location = new Point(100, 15),
                BackColor = Color.FromArgb(41, 128, 185)
            };

            Label lblScreen = new Label
            {
                Text = "MÀN HÌNH (PANTALLA)",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlScreen.Controls.Add(lblScreen);

            // Container sơ đồ ghế
            pnlMatrixContainer = new Panel
            {
                Location = new Point(20, 65),
                Size = new Size(760, 480),
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            Controls.Add(pnlScreen);
            Controls.Add(pnlMatrixContainer);

            // Tạo sơ đồ mặc định, không tự khóa ghế mẫu
            GenerateSeatMatrix(10, 14);
        }

        // Tạo sơ đồ ghế
        public void GenerateSeatMatrix(
            int rows,
            int cols,
            IEnumerable<string>? extraOccupiedSeats = null)
        {
            if (rows < 1 || rows > 26)
                throw new ArgumentOutOfRangeException(nameof(rows));

            if (cols < 1)
                throw new ArgumentOutOfRangeException(nameof(cols));

            // Lưu ghế khách hàng đang chọn để khôi phục
            var previousSelection = new HashSet<string>(
                SelectedSeatCodes,
                StringComparer.OrdinalIgnoreCase);

            // Xóa giao diện cũ
            foreach (Control control in pnlMatrixContainer.Controls)
            {
                control.Dispose();
            }

            pnlMatrixContainer.Controls.Clear();
            seatButtons.Clear();

            SelectedSeatCodes.Clear();

            int seatSize = 32;
            int gap = 8;

            int startX = 50;
            int startY = 20;

            // Danh sách ghế đã đặt lấy từ bên ngoài
            HashSet<string> occupiedSeats =
                new(StringComparer.OrdinalIgnoreCase);

            if (extraOccupiedSeats != null)
            {
                foreach (string code in extraOccupiedSeats)
                {
                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        occupiedSeats.Add(
                            code.Trim().ToUpperInvariant());
                    }
                }
            }

            for (int r = 0; r < rows; r++)
            {
                char rowLetter = (char)('A' + r);

                // Nhãn hàng ghế
                Label lblRow = new Label
                {
                    Text = rowLetter.ToString(),
                    Font = new Font(
                        "Segoe UI", 9.5f, FontStyle.Bold),
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

                    bool isOccupied =
                        occupiedSeats.Contains(seatCode);

                    // Xác định loại ghế
                    Color seatColor = GetSeatColor(r);

                    Button btnSeat = new Button
                    {
                        Text = c.ToString(),
                        Name = seatCode,
                        Size = new Size(seatSize, seatSize),

                        Location = new Point(
                            startX + (c - 1) * (seatSize + gap),
                            startY + r * (seatSize + gap)),

                        Font = new Font(
                            "Segoe UI", 8, FontStyle.Bold),

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

                    btnSeat.FlatAppearance.MouseOverBackColor =
                        isOccupied ? ColorOccupied : seatColor;

                    // Chỉ cho chọn ghế chưa đặt
                    if (!isOccupied)
                    {
                        btnSeat.Click += BtnSeat_Click;
                    }

                    seatButtons.Add(btnSeat);

                    pnlMatrixContainer.Controls.Add(btnSeat);
                }
            }

            // Khôi phục ghế đã chọn trước đó
            RestoreSelectedSeats(previousSelection);
        }

        // Xác định màu ghế theo hàng
        private static Color GetSeatColor(int rowIndex)
        {
            if (rowIndex >= 3 && rowIndex <= 6)
                return ColorVIP;

            if (rowIndex > 6)
                return ColorPlus;

            return ColorStandard;
        }

        // Tính giá ghế theo loại
        public static decimal GetSeatPriceByCode(string seatCode)
        {
            if (string.IsNullOrWhiteSpace(seatCode))
                return 90000m;

            char row = char.ToUpperInvariant(seatCode[0]);

            int rowIndex = row - 'A';

            if (rowIndex >= 3 && rowIndex <= 6)
                return 120000m;

            if (rowIndex > 6)
                return 110000m;

            return 90000m;
        }

        // Xử lý chọn hoặc bỏ chọn ghế
        private void BtnSeat_Click(object? sender, EventArgs e)
        {
            if (sender is not Button btn)
                return;

            string seatCode = btn.Name;

            Color originalColor = (Color)btn.Tag!;

            if (SelectedSeatCodes.Contains(
                seatCode,
                StringComparer.OrdinalIgnoreCase))
            {
                // Bỏ chọn ghế
                SelectedSeatCodes.RemoveAll(
                    x => string.Equals(
                        x,
                        seatCode,
                        StringComparison.OrdinalIgnoreCase));

                btn.BackColor = originalColor;
            }
            else
            {
                // Chọn ghế
                SelectedSeatCodes.Add(seatCode);

                btn.BackColor = ColorSelected;
            }

            NotifySelectionChanged();
        }

        // Khôi phục danh sách ghế đã chọn
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
                    requestedSeats.Contains(btn.Name))
                {
                    SelectedSeatCodes.Add(btn.Name);

                    btn.BackColor = ColorSelected;
                }
            }

            NotifySelectionChanged();
        }

        // Lấy danh sách ghế hiện tại
        public List<string> GetSelectedSeats()
        {
            return new List<string>(SelectedSeatCodes);
        }

        // Xóa toàn bộ ghế đang chọn
        public void ClearSelectedSeats()
        {
            foreach (Button btn in seatButtons)
            {
                if (!btn.Enabled)
                    continue;

                btn.BackColor = (Color)btn.Tag!;
            }

            SelectedSeatCodes.Clear();

            NotifySelectionChanged();
        }

        // Gửi sự kiện ra ngoài
        private void NotifySelectionChanged()
        {
            OnSeatSelectionChanged?.Invoke(
                this,
                new List<string>(SelectedSeatCodes));
        }
    }
}
