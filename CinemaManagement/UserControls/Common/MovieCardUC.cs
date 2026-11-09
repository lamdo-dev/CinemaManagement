namespace CinemaManagement.UserControls.Common;

using System;
using System.Drawing;
using System.Windows.Forms;
using CinemaManagement.Models;

public partial class MovieCardUC : UserControl
{
    private PictureBox picPoster;
    private Label lblTitle;
    private Label lblSubInfo;
    private Panel pnlCard;

    public Movie MovieData { get; private set; }

    // Sự kiện khi người dùng click vào thẻ phim để sang trang chi tiết
    public event EventHandler<Movie>? MovieSelected;

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
        this.Size = new Size(180, 310);
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
            Size = new Size(180, 240),
            Location = new Point(0, 0),
            SizeMode = PictureBoxSizeMode.StretchImage,
            BackColor = Color.FromArgb(45, 45, 60),
            Cursor = Cursors.Hand
        };

        // Tên Phim
        lblTitle = new Label
        {
            Location = new Point(5, 246),
            Size = new Size(170, 22),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.White,
            Text = "Movie Title",
            AutoEllipsis = true
        };

        // Phụ đề (Năm chiếu / Thể loại / Thời lượng)
        lblSubInfo = new Label
        {
            Location = new Point(5, 270),
            Size = new Size(170, 20),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            ForeColor = Color.FromArgb(160, 160, 175),
            Text = "2026 • Action",
            AutoEllipsis = true
        };

        // Gắn các sự kiện click
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

        this.Controls.Add(pnlCard);
    }

    public void SetMovie(Movie movie)
    {
        MovieData = movie;
        lblTitle.Text = movie.Title;
        lblSubInfo.Text = $"{movie.ReleaseDate.Year} • {movie.Genre}";

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