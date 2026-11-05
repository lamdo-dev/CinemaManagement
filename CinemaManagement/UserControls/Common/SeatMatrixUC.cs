namespace CinemaManagement.UserControls.Common;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CinemaManagement.Models;
using CinemaManagement.Models.Enums;

public partial class SeatMatrixUC : UserControl
{
    // Màu sắc trạng thái ghế
    public static readonly Color ColorStandard = Color.FromArgb(46, 204, 113);   // Xanh lá (Thường)
    public static readonly Color ColorVIP = Color.FromArgb(155, 89, 182);        // Tím (VIP)
    public static readonly Color ColorPlus = Color.FromArgb(230, 126, 34);       // Cam (Plus/Premium)
    public static readonly Color ColorSelected = Color.FromArgb(52, 152, 219);   // Xanh dương (Đang chọn)
    public static readonly Color ColorOccupied = Color.FromArgb(70, 70, 85);     // Xám xẫm (Đã bán/Khóa)

    private Panel pnlScreen;
    private Panel pnlMatrixContainer;
    private List<Button> seatButtons = new List<Button>();

    public List<string> SelectedSeatCodes { get; private set; } = new List<string>();
    
    // Sự kiện gửi danh sách ghế được chọn ra ngoài cho SeatSelectionUC tính tiền
    public event EventHandler<List<string>>? OnSeatSelectionChanged;

    public SeatMatrixUC()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.FromArgb(18, 19, 26);
        this.AutoScroll = true;

        // 1. THANH MÀN HÌNH (SCREEN BANNER)
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

        // 2. CONTAINER CHỨA MA TRẬN GHẾ
        pnlMatrixContainer = new Panel
        {
            Location = new Point(20, 65),
            Size = new Size(760, 480),
            AutoScroll = true,
            BackColor = Color.Transparent
        };

        this.Controls.Add(pnlScreen);
        this.Controls.Add(pnlMatrixContainer);

        // Dựng thử ma trận ghế mặc định
        GenerateSeatMatrix(10, 14);
    }

    public void GenerateSeatMatrix(int rows, int cols)
    {
        pnlMatrixContainer.Controls.Clear();
        seatButtons.Clear();
        SelectedSeatCodes.Clear();

        int seatSize = 32;
        int gap = 8;
        int startX = 50;
        int startY = 20;

        // Danh sách ghế giả lập đã đặt trước
        List<string> occupiedSeats = new List<string> { "C5", "C6", "D7", "D8", "E8", "E9" };

        for (int r = 0; r < rows; r++)
        {
            char rowLetter = (char)('A' + r);

            // Nhãn tên hàng (A, B, C...)
            Label lblRow = new Label
            {
                Text = rowLetter.ToString(),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Size = new Size(30, seatSize),
                Location = new Point(10, startY + r * (seatSize + gap)),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlMatrixContainer.Controls.Add(lblRow);

            for (int c = 1; c <= cols; c++)
            {
                string seatCode = $"{rowLetter}{c}";
                bool isOccupied = occupiedSeats.Contains(seatCode);

                // Xác định loại ghế theo vị trí hàng
                Color seatColor = ColorStandard; // Hàng A-C: Thường
                if (r >= 3 && r <= 6) seatColor = ColorVIP; // Hàng D-G: VIP
                else if (r > 6) seatColor = ColorPlus;      // Hàng H-J: Premium

                Button btnSeat = new Button
                {
                    Text = c.ToString(),
                    Name = seatCode,
                    Size = new Size(seatSize, seatSize),
                    Location = new Point(startX + (c - 1) * (seatSize + gap), startY + r * (seatSize + gap)),
                    Font = new Font("Segoe UI", 8, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = isOccupied ? ColorOccupied : seatColor,
                    Enabled = !isOccupied,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = isOccupied ? Cursors.No : Cursors.Hand,
                    Tag = seatColor // Lưu màu gốc
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
    }

    private void BtnSeat_Click(object? sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            string seatCode = btn.Name;
            Color originalColor = (Color)btn.Tag;

            if (SelectedSeatCodes.Contains(seatCode))
            {
                // Bỏ chọn
                SelectedSeatCodes.Remove(seatCode);
                btn.BackColor = originalColor;
            }
            else
            {
                // Thêm vào danh sách chọn
                SelectedSeatCodes.Add(seatCode);
                btn.BackColor = ColorSelected;
            }

            // Thông báo danh sách ghế đã thay đổi
            OnSeatSelectionChanged?.Invoke(this, new List<string>(SelectedSeatCodes));
        }
    }
}