namespace CinemaManagement.UserControls.Common;

using System;
using System.Drawing;
using System.Windows.Forms;
using CinemaManagement.Models;

public partial class MovieCardUC : UserControl
{
    private PictureBox picPoster = null!;
    private Label lblTitle = null!;
    private Label lblSubInfo = null!;
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

        // Container Card
        pnlCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(30, 30, 46), // Dark Cinema Card Background
            Cursor = Cursors.Hand
        };

        // Poster phim
        picPoster = new PictureBox
        {
            Size = new Size(185, 225),
            Location = new Point(0, 0),
            SizeMode = PictureBoxSizeMode.StretchImage,
            BackColor = Color.FromArgb(45, 45, 60),
            Cursor = Cursors.Hand
        };
        picPoster.Paint += PicPoster_PaintPlaceholder;

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

        // Phụ đề (Năm chiếu / Thể loại / Thời lượng)
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

        // Gắn các sự kiện click xem chi tiết phim
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
        lblSubInfo.Text = $"{movie.ReleaseDate.Year} • {movie.Genre} ({movie.DurationMinutes}p)";

        // Tải ảnh poster nếu đường dẫn tồn tại
        if (!string.IsNullOrEmpty(movie.PosterUrl) && System.IO.File.Exists(movie.PosterUrl))
        {
            try { picPoster.Image = Image.FromFile(movie.PosterUrl); } catch { }
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
