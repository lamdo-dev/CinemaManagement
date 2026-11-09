using System.ComponentModel;
namespace CinemaManagement.Forms.Dialogs;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Models;

public partial class ConfirmBookingDialog : Form
{
    private readonly Color ColorBg = Color.FromArgb(20, 21, 32);
    private readonly Color ColorCard = Color.FromArgb(28, 29, 44);
    private readonly Color ColorGold = Color.FromArgb(245, 197, 24);
    private readonly Color ColorTextMuted = Color.FromArgb(160, 160, 180);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Movie? MovieData { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Showtime? ShowtimeData { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<string> SelectedSeats { get; set; } = new List<string>();
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Dictionary<int, (string Name, decimal Price, int Qty, string ImgPath)> FoodCart { get; set; } 
        = new Dictionary<int, (string, decimal, int, string)>();

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal TicketSubTotal { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal FoodSubTotal { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal DiscountAmount { get; set; }
    public decimal FinalTotal => (TicketSubTotal + FoodSubTotal) - DiscountAmount;

    private PictureBox picPoster;
    private Label lblMovieTitle;
    private Label lblShowtime;
    private Label lblSeats;
    private Label lblFoodSummary;
    private Label lblTicketPrice;
    private Label lblFoodPrice;
    private Label lblDiscountPrice;
    private Label lblFinalTotal;

    private Button btnConfirm;
    private Button btnCancel;

    public ConfirmBookingDialog()
    {
        InitializeComponent();
    }

    public ConfirmBookingDialog(Movie movie, Showtime showtime, List<string> seats, decimal ticketTotal,
        Dictionary<int, (string Name, decimal Price, int Qty, string ImgPath)> foodCart, decimal foodTotal, decimal discount = 0m) : this()
    {
        MovieData = movie;
        ShowtimeData = showtime;
        SelectedSeats = seats;
        TicketSubTotal = ticketTotal;
        FoodCart = foodCart;
        FoodSubTotal = foodTotal;
        DiscountAmount = discount;

        PopulateData();
    }

    private void InitializeComponent()
    {
        this.Size = new Size(520, 620);
        this.FormBorderStyle = FormBorderStyle.None;
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = ColorBg;
        this.Padding = new Padding(2);

        Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.FromArgb(14, 15, 23) };
        Label lblHeaderTitle = new Label
        {
            Text = "🛒 XÁC NHẬN ĐẶT VÉ & THANH TOÁN",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = ColorGold,
            Location = new Point(20, 16),
            AutoSize = true
        };

        Button btnCloseIcon = new Button
        {
            Text = "✕",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = ColorTextMuted,
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(35, 35),
            Location = new Point(470, 10),
            Cursor = Cursors.Hand
        };
        btnCloseIcon.FlatAppearance.BorderSize = 0;
        btnCloseIcon.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

        pnlHeader.Controls.Add(lblHeaderTitle);
        pnlHeader.Controls.Add(btnCloseIcon);

        Panel pnlMovieBox = new Panel { Location = new Point(20, 75), Size = new Size(480, 130), BackColor = ColorCard };
        picPoster = new PictureBox { Location = new Point(12, 12), Size = new Size(75, 106), SizeMode = PictureBoxSizeMode.StretchImage, BackColor = Color.FromArgb(40, 42, 60) };
        lblMovieTitle = new Label { Text = "TÊN PHIM", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.White, Location = new Point(100, 15), Size = new Size(365, 25), AutoEllipsis = true };
        lblShowtime = new Label { Text = "⏰ Suất: --:-- | --/--/----", Font = new Font("Segoe UI", 9.5f), ForeColor = ColorTextMuted, Location = new Point(100, 45), Size = new Size(365, 22) };
        lblSeats = new Label { Text = "🪑 Ghế đã chọn: --", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = ColorGold, Location = new Point(100, 72), Size = new Size(365, 25), AutoEllipsis = true };
        pnlMovieBox.Controls.AddRange(new Control[] { picPoster, lblMovieTitle, lblShowtime, lblSeats });

        Panel pnlDetailBox = new Panel { Location = new Point(20, 220), Size = new Size(480, 290), BackColor = ColorCard, Padding = new Padding(15) };
        Label lblFoodHeader = new Label { Text = "🍿 Bắp nước đã chọn:", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.White, Location = new Point(15, 12), AutoSize = true };
        lblFoodSummary = new Label { Text = "Không chọn bắp nước", Font = new Font("Segoe UI", 9), ForeColor = ColorTextMuted, Location = new Point(15, 35), Size = new Size(450, 45) };
        Panel divider = new Panel { Location = new Point(15, 88), Size = new Size(450, 1), BackColor = Color.FromArgb(50, 52, 72) };

        int yPos = 102;
        lblTicketPrice = AddDetailRow(pnlDetailBox, "Tiền vé phim:", "0 đ", ref yPos);
        lblFoodPrice = AddDetailRow(pnlDetailBox, "Tiền bắp nước:", "0 đ", ref yPos);
        lblDiscountPrice = AddDetailRow(pnlDetailBox, "Mã giảm giá (Voucher):", "-0 đ", ref yPos, Color.OrangeRed);

        Panel divider2 = new Panel { Location = new Point(15, yPos + 5), Size = new Size(450, 1), BackColor = Color.FromArgb(60, 62, 82) };
        Label lblTotalHeader = new Label { Text = "TỔNG THÀNH TIỀN:", Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.White, Location = new Point(15, yPos + 18), AutoSize = true };
        lblFinalTotal = new Label { Text = "0 đ", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = ColorGold, Location = new Point(230, yPos + 12), Size = new Size(235, 35), TextAlign = ContentAlignment.MiddleRight };
        pnlDetailBox.Controls.AddRange(new Control[] { lblFoodHeader, lblFoodSummary, divider, divider2, lblTotalHeader, lblFinalTotal });

        Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 75, BackColor = Color.FromArgb(16, 17, 25) };
        btnCancel = new Button { Text = "HỦY / CHỌN LẠI", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(45, 47, 65), FlatStyle = FlatStyle.Flat, Size = new Size(160, 45), Location = new Point(20, 15), Cursor = Cursors.Hand };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

        btnConfirm = new Button { Text = "✓ XÁC NHẬN THANH TOÁN", Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Color.Black, BackColor = ColorGold, FlatStyle = FlatStyle.Flat, Size = new Size(290, 45), Location = new Point(210, 15), Cursor = Cursors.Hand };
        btnConfirm.FlatAppearance.BorderSize = 0;
        btnConfirm.Click += (s, e) => { this.DialogResult = DialogResult.OK; this.Close(); };

        pnlFooter.Controls.Add(btnCancel);
        pnlFooter.Controls.Add(btnConfirm);

        this.Controls.Add(pnlDetailBox);
        this.Controls.Add(pnlMovieBox);
        this.Controls.Add(pnlFooter);
        this.Controls.Add(pnlHeader);
    }

    private Label AddDetailRow(Panel parent, string title, string val, ref int yPos, Color? color = null)
    {
        Label lblT = new Label { Text = title, Font = new Font("Segoe UI", 9.5f), ForeColor = ColorTextMuted, Location = new Point(15, yPos), AutoSize = true };
        Label lblV = new Label { Text = val, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = color ?? Color.White, Location = new Point(250, yPos), Size = new Size(215, 20), TextAlign = ContentAlignment.MiddleRight };

        parent.Controls.Add(lblT);
        parent.Controls.Add(lblV);
        yPos += 28;
        return lblV;
    }

    private void PopulateData()
    {
        if (MovieData != null)
        {
            lblMovieTitle.Text = MovieData.Title;
            if (!string.IsNullOrEmpty(MovieData.PosterUrl) && File.Exists(MovieData.PosterUrl))
            {
                try { picPoster.Image = Image.FromFile(MovieData.PosterUrl); } catch { }
            }
        }

        if (ShowtimeData != null)
        {
            lblShowtime.Text = $"⏰ Suất: {ShowtimeData.StartTime:HH:mm} | {ShowtimeData.StartTime:dd/MM/yyyy}";
        }

        lblSeats.Text = $"🪑 Ghế đã chọn: {(SelectedSeats.Count > 0 ? string.Join(", ", SelectedSeats) : "Chưa chọn")}";

        if (FoodCart.Count > 0)
        {
            var foodItemsText = FoodCart.Select(x => $"{x.Value.Name} (x{x.Value.Qty})");
            lblFoodSummary.Text = string.Join(", ", foodItemsText);
        }
        else
        {
            lblFoodSummary.Text = "Không đăng ký bắp nước.";
        }

        lblTicketPrice.Text = $"{TicketSubTotal.ToString("N0", new CultureInfo("vi-VN"))} đ";
        lblFoodPrice.Text = $"{FoodSubTotal.ToString("N0", new CultureInfo("vi-VN"))} đ";
        lblDiscountPrice.Text = $"-{DiscountAmount.ToString("N0", new CultureInfo("vi-VN"))} đ";
        lblFinalTotal.Text = $"{FinalTotal.ToString("N0", new CultureInfo("vi-VN"))} đ";
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            this.Capture = false;
            Message msg = Message.Create(this.Handle, 0xA1, new IntPtr(2), IntPtr.Zero);
            this.DefWndProc(ref msg);
        }
    }
}