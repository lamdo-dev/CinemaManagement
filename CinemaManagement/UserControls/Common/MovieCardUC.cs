namespace CinemaManagement.UserControls.Common;

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CinemaManagement.Models;
using CinemaManagement.Models.Enums;

public partial class MovieCardUC : UserControl
{
    private PictureBox picPoster = null!;
    private Label lblTitle = null!;
    private Label lblSubInfo = null!;
    private Label lblStatusBadge = null!;
    private Button btnBookNow = null!;
    private Panel pnlCard = null!;

    public Movie MovieData { get; private set; } = null!;

    // Sự kiện khi người dùng click vào thẻ phim để sang trang chi tiết (MovieDetailUC)
    public event EventHandler<Movie>? MovieSelected;

    // Sự kiện khi người dùng click nút "Đặt vé" để sang thẳng trang Chọn suất chiếu (ShowtimeUC)
    public event EventHandler<Movie>? OnBookNowClicked;

    public MovieCardUC()
    {
        InitializeComponents();
    }

    public MovieCardUC(Movie movie) : this()
    {
        SetMovie(movie);
    }

    private void InitializeComponents()
    {
        this.Size = new Size(185, 335);
        this.Margin = new Padding(12);
        this.BackColor = Color.Transparent;

        pnlCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(30, 30, 46),
            Cursor = Cursors.Hand
        };

        // Poster phim
        picPoster = new PictureBox
        {
            Size = new Size(185, 225),
            Location = new Point(0, 0),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(45, 45, 60),
            Cursor = Cursors.Hand
        };
        picPoster.Paint += PicPoster_PaintPlaceholder;

        // Nhãn trạng thái góc trên poster
        lblStatusBadge = new Label
        {
            Location = new Point(8, 8),
            AutoSize = true,
            Padding = new Padding(6, 2, 6, 2),
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(229, 9, 20),
            Text = "ĐANG CHIẾU",
            Visible = false
        };
        picPoster.Controls.Add(lblStatusBadge);

        // Tên Phim
        lblTitle = new Label
        {
            Location = new Point(6, 232),
            Size = new Size(173, 22),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.White,
            Text = "Movie Title",
            AutoEllipsis = true,
            Cursor = Cursors.Hand
        };

        // Phụ đề (Năm chiếu • Thể loại • Thời lượng)
        lblSubInfo = new Label
        {
            Location = new Point(6, 255),
            Size = new Size(173, 20),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            ForeColor = Color.FromArgb(160, 160, 175),
            Text = "2026 • Action",
            AutoEllipsis = true,
            Cursor = Cursors.Hand
        };

        // Nút Đặt vé nhanh trên Card
        btnBookNow = new Button
        {
            Text = "🎟 Đặt vé ngay",
            Location = new Point(10, 283),
            Size = new Size(165, 36),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(229, 9, 20),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnBookNow.FlatAppearance.BorderSize = 0;
        btnBookNow.Click += (s, e) =>
        {
            if (MovieData != null)
            {
                OnBookNowClicked?.Invoke(this, MovieData);
            }
        };

        // Sự kiện click xem chi tiết phim
        pnlCard.Click += OnCardClicked;
        picPoster.Click += OnCardClicked;
        lblTitle.Click += OnCardClicked;
        lblSubInfo.Click += OnCardClicked;

        // Hover Effect
        pnlCard.MouseEnter += (s, e) => pnlCard.BackColor = Color.FromArgb(45, 45, 65);
        pnlCard.MouseLeave += (s, e) => pnlCard.BackColor = Color.FromArgb(30, 30, 46);
        picPoster.MouseEnter += (s, e) => pnlCard.BackColor = Color.FromArgb(45, 45, 65);
        picPoster.MouseLeave += (s, e) => pnlCard.BackColor = Color.FromArgb(30, 30, 46);

        pnlCard.Controls.Add(picPoster);
        pnlCard.Controls.Add(lblTitle);
        pnlCard.Controls.Add(lblSubInfo);
        pnlCard.Controls.Add(btnBookNow);

        this.Controls.Add(pnlCard);
    }

    private void PicPoster_PaintPlaceholder(object? sender, PaintEventArgs e)
    {
        if (picPoster.Image != null) return;

        using var font = new Font("Segoe UI Emoji", 32f);
        using var brush = new SolidBrush(Color.FromArgb(120, 130, 160));
        var sf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        e.Graphics.DrawString("🎬", font, brush, picPoster.ClientRectangle, sf);
    }

    public void SetMovie(Movie movie)
    {
        MovieData = movie;
        lblTitle.Text = movie.Title;

        string genreDisplay = string.IsNullOrWhiteSpace(movie.Genre) ? "Chưa phân loại" : movie.Genre;
        lblSubInfo.Text = $"{movie.ReleaseDate.Year} • {genreDisplay} ({movie.DurationMinutes}p)";

        if (movie.Status == MovieStatus.ComingSoon)
        {
            lblStatusBadge.Text = "SẮP CHIẾU";
            lblStatusBadge.BackColor = Color.FromArgb(217, 119, 6);
            lblStatusBadge.Visible = true;

            btnBookNow.Text = "🕒 Xem chi tiết";
            btnBookNow.BackColor = Color.FromArgb(71, 85, 105);
        }
        else
        {
            lblStatusBadge.Visible = false;
            btnBookNow.Text = "🎟 Đặt vé ngay";
            btnBookNow.BackColor = Color.FromArgb(229, 9, 20);
        }

        // Giải phóng ảnh cũ nếu có
        if (picPoster.Image != null)
        {
            var oldImg = picPoster.Image;
            picPoster.Image = null;
            oldImg.Dispose();
        }

        // Tải ảnh từ file nội bộ bằng MemoryStream để không khóa file trên ổ đĩa
        if (!string.IsNullOrWhiteSpace(movie.PosterUrl) && File.Exists(movie.PosterUrl))
        {
            try
            {
                byte[] imageBytes = File.ReadAllBytes(movie.PosterUrl);
                using var ms = new MemoryStream(imageBytes);
                using var tempImage = Image.FromStream(ms);
                picPoster.Image = new Bitmap(tempImage);
                picPoster.SizeMode = PictureBoxSizeMode.StretchImage;
            }
            catch
            {
                picPoster.Image = null;
            }
        }
    }

    private void OnCardClicked(object? sender, EventArgs e)
    {
        if (MovieData != null)
        {
            MovieSelected?.Invoke(this, MovieData);
        }
    }
}