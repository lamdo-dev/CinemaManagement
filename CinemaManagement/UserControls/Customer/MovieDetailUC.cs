namespace CinemaManagement.UserControls.Customer;

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CinemaManagement.Models;
using CinemaManagement.Models.Enums;

public partial class MovieDetailUC : UserControl
{
    // Color Palette
    private readonly Color ColorBg = Color.FromArgb(18, 19, 28);
    private readonly Color ColorCardBg = Color.FromArgb(26, 27, 38);
    private readonly Color ColorAccentRed = Color.FromArgb(229, 9, 20);
    private readonly Color ColorDisabledBtn = Color.FromArgb(65, 70, 90);
    private readonly Color ColorTextMuted = Color.FromArgb(160, 160, 180);

    // UI Controls
    private Panel pnlHero = null!;
    private PictureBox picPoster = null!;
    private Label lblTitle = null!;
    private Label lblSynopsisHeader = null!;
    private Label lblDescription = null!;
    private FlowLayoutPanel flpMetadata = null!;
    private Label lblMetaDirector = null!;
    private Label lblMetaGenre = null!;
    private Label lblMetaDuration = null!;
    private Label lblMetaReleaseDate = null!;
    private Label lblMetaStatus = null!;
    private Button btnBookTicket = null!;
    private Button btnBack = null!;

    public Movie? MovieData { get; private set; }

    // Sự kiện khi bấm "Đặt Vé Ngay" để chuyển sang màn hình Chọn Suất Chiếu (ShowtimeUC)
    public event EventHandler<Movie>? OnBookTicketClicked;
    // Sự kiện khi bấm "Quay lại" về HomeUC
    public event EventHandler? OnBackClicked;

    public MovieDetailUC()
    {
        InitializeComponents();
    }

    public MovieDetailUC(Movie movie) : this()
    {
        SetMovie(movie);
    }

    private void InitializeComponents()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ColorBg;
        this.AutoScroll = true;

        // -------------------------------------------------------------
        // 1. NÚT BACK (QUAY LẠI TRANG CHỦ)
        // -------------------------------------------------------------
        btnBack = new Button
        {
            Text = "← Quay lại",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(35, 36, 50),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(110, 35),
            Location = new Point(20, 15),
            Cursor = Cursors.Hand
        };
        btnBack.FlatAppearance.BorderSize = 0;
        btnBack.Click += (s, e) => OnBackClicked?.Invoke(this, EventArgs.Empty);

        // -------------------------------------------------------------
        // 2. KHỐI HERO (BANNER BANNER & THÔNG TIN CHÍNH)
        // -------------------------------------------------------------
        pnlHero = new Panel
        {
            Location = new Point(20, 60),
            Size = new Size(1140, 360),
            BackColor = ColorCardBg,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        // Poster Phim bên trái
        picPoster = new PictureBox
        {
            Location = new Point(20, 20),
            Size = new Size(220, 320),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(40, 40, 55)
        };
        picPoster.Paint += (s, e) =>
        {
            if (picPoster.Image != null)
                return;

            using var font = new Font("Segoe UI Emoji", 32f);
            using var brush = new SolidBrush(Color.FromArgb(130, 135, 160));
            using var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            e.Graphics.DrawString("🎬", font, brush, picPoster.ClientRectangle, sf);
        };

        // Tiêu đề Phim
        lblTitle = new Label
        {
            Text = "Thông tin bộ phim",
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(260, 20),
            Size = new Size(850, 45),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            AutoEllipsis = true
        };

        // Tiêu đề tóm tắt
        lblSynopsisHeader = new Label
        {
            Text = "Tóm tắt & Chi tiết",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(260, 70),
            AutoSize = true
        };

        // Mô tả nội dung phim
        lblDescription = new Label
        {
            Text = "Đang tải mô tả phim...",
            Font = new Font("Segoe UI", 10),
            ForeColor = ColorTextMuted,
            Location = new Point(260, 105),
            Size = new Size(840, 70),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            AutoEllipsis = true
        };

        // Grid Thông tin phụ (Đạo diễn, Thể loại, Thời lượng, Khởi chiếu, Trạng thái)
        flpMetadata = new FlowLayoutPanel
        {
            Location = new Point(260, 185),
            Size = new Size(840, 75),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            AutoScroll = false,
            WrapContents = true
        };

        lblMetaDirector = AddMetaItem("Đạo diễn", "Đang cập nhật");
        lblMetaGenre = AddMetaItem("Thể loại", "Đang cập nhật");
        lblMetaDuration = AddMetaItem("Thời lượng", "-- phút");
        lblMetaReleaseDate = AddMetaItem("Khởi chiếu", "--/--/----");
        lblMetaStatus = AddMetaItem("Trạng thái", "Đang chiếu");

        // Nút bấm ĐẶT VÉ NGAY
        btnBookTicket = new Button
        {
            Text = "🎟  ĐẶT VÉ NGAY",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ColorAccentRed,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(280, 45),
            Location = new Point(260, 280),
            Cursor = Cursors.Hand
        };
        btnBookTicket.FlatAppearance.BorderSize = 0;
        btnBookTicket.Click += BtnBookTicket_Click;

        pnlHero.Controls.Add(picPoster);
        pnlHero.Controls.Add(lblTitle);
        pnlHero.Controls.Add(lblSynopsisHeader);
        pnlHero.Controls.Add(lblDescription);
        pnlHero.Controls.Add(flpMetadata);
        pnlHero.Controls.Add(btnBookTicket);

        // Thêm tất cả vào UserControl
        this.Controls.Add(btnBack);
        this.Controls.Add(pnlHero);
    }

    private Label AddMetaItem(string title, string value)
    {
        Panel pnl = new Panel { Size = new Size(155, 58), Margin = new Padding(0, 0, 12, 0) };
        Label lblT = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 20
        };
        Label lblV = new Label
        {
            Text = value,
            Font = new Font("Segoe UI", 9),
            ForeColor = ColorTextMuted,
            Dock = DockStyle.Bottom,
            Height = 34,
            AutoEllipsis = true
        };
        pnl.Controls.Add(lblT);
        pnl.Controls.Add(lblV);
        flpMetadata.Controls.Add(pnl);
        return lblV;
    }

    public void SetMovie(Movie movie)
    {
        if (movie == null)
            return;

        MovieData = movie;
        lblTitle.Text = string.IsNullOrWhiteSpace(movie.Title)
            ? "Chưa có tên phim"
            : movie.Title;

        lblDescription.Text = string.IsNullOrWhiteSpace(movie.Description)
            ? "Chưa có mô tả cho bộ phim này."
            : movie.Description;

        lblMetaDirector.Text = string.IsNullOrWhiteSpace(movie.Director)
            ? "Đang cập nhật"
            : movie.Director;

        lblMetaGenre.Text = string.IsNullOrWhiteSpace(movie.Genre)
            ? "Chưa phân loại"
            : movie.Genre;

        lblMetaDuration.Text = movie.DurationMinutes > 0
            ? $"{movie.DurationMinutes} phút"
            : "Đang cập nhật";

        lblMetaReleaseDate.Text = movie.ReleaseDate != default
            ? movie.ReleaseDate.ToString("dd/MM/yyyy")
            : "Đang cập nhật";

        switch (movie.Status)
        {
            case MovieStatus.Showing:
                lblMetaStatus.Text = "Đang chiếu";
                lblMetaStatus.ForeColor = Color.LightGreen;
                btnBookTicket.Enabled = true;
                btnBookTicket.Text = "🎟  ĐẶT VÉ NGAY";
                btnBookTicket.BackColor = ColorAccentRed;
                btnBookTicket.Cursor = Cursors.Hand;
                break;

            case MovieStatus.ComingSoon:
                lblMetaStatus.Text = "Sắp chiếu";
                lblMetaStatus.ForeColor = Color.Gold;
                btnBookTicket.Enabled = true;
                btnBookTicket.Text = "🎟  ĐẶT VÉ TRƯỚC";
                btnBookTicket.BackColor = ColorAccentRed;
                btnBookTicket.Cursor = Cursors.Hand;
                break;

            default:
                lblMetaStatus.Text = "Đã kết thúc";
                lblMetaStatus.ForeColor = Color.IndianRed;
                btnBookTicket.Enabled = false;
                btnBookTicket.Text = "⏹  PHIM ĐÃ KẾT THÚC";
                btnBookTicket.BackColor = ColorDisabledBtn;
                btnBookTicket.Cursor = Cursors.No;
                break;
        }

        if (picPoster.Image != null)
        {
            Image oldImg = picPoster.Image;
            picPoster.Image = null;
            oldImg.Dispose();
        }

        if (!string.IsNullOrWhiteSpace(movie.PosterUrl) && File.Exists(movie.PosterUrl))
        {
            try
            {
                using var stream = new FileStream(
                    movie.PosterUrl,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite);
                using var img = Image.FromStream(stream);
                picPoster.Image = new Bitmap(img);
            }
            catch
            {
                picPoster.Image = null;
            }
        }

        picPoster.Invalidate();
    }

    private void BtnBookTicket_Click(object? sender, EventArgs e)
    {
        if (MovieData == null)
            return;

        if (MovieData.Status == MovieStatus.Ended)
        {
            MessageBox.Show(
                "Bộ phim này đã kết thúc đợt chiếu.",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        OnBookTicketClicked?.Invoke(this, MovieData);
    }
}