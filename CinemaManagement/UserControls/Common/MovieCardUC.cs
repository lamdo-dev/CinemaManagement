
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using CinemaManagement.Models;
using CinemaManagement.Models.Enums;

namespace CinemaManagement.UserControls.Common
{
    public partial class MovieCardUC : UserControl
    {
        // =========================================================
        // 1. FIELDS
        // =========================================================

        private PictureBox picPoster = null!;
        private Label lblTitle = null!;
        private Label lblSubInfo = null!;
        private Label lblStatusBadge = null!;
        private Button btnBookNow = null!;
        private Panel pnlCard = null!;
        private Panel pnlInfo = null!;
        private bool _isDarkTheme = true;
        private bool _isHovered;

        public Movie MovieData { get; private set; } = null!;

        // Sự kiện khi người dùng click vào thẻ phim để sang chi tiết.
        public event EventHandler<Movie>? MovieSelected;

        // Sự kiện khi người dùng click nút Đặt vé.
        public event EventHandler<Movie>? OnBookNowClicked;

        // =========================================================
        // 2. CONSTRUCTORS
        // =========================================================

        public MovieCardUC()
        {
            InitializeComponents();
        }

        public MovieCardUC(Movie movie) : this()
        {
            SetMovie(movie);
        }

        // =========================================================
        // 3. INITIALIZE COMPONENTS
        // =========================================================

        private void InitializeComponents()
        {
            Size = new Size(205, 365);
            MinimumSize = new Size(170, 320);
            Margin = new Padding(10);
            BackColor = Color.Transparent;

            pnlCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 46),
                Cursor = Cursors.Hand,
                Padding = new Padding(0)
            };

            // ---------------- POSTER ----------------

            picPoster = new PictureBox
            {
                Dock = DockStyle.Top,
                Height = 235,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(45, 45, 60),
                Cursor = Cursors.Hand
            };

            picPoster.Paint += PicPoster_PaintPlaceholder;

            // STATUS BADGE
            lblStatusBadge = new Label
            {
                Location = new Point(8, 8),
                AutoSize = true,
                Padding = new Padding(6, 3, 6, 3),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(229, 9, 20),
                Text = "ĐANG CHIẾU",
                Visible = false
            };

            picPoster.Controls.Add(lblStatusBadge);
            lblStatusBadge.BringToFront();

            // ---------------- INFORMATION AREA ----------------

            pnlInfo = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 46),
                Padding = new Padding(8, 5, 8, 7)
            };

            lblTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 27,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                Text = "Movie Title",
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };

            lblSubInfo = new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(160, 160, 175),
                Text = "2026 • Action",
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };

            btnBookNow = new Button
            {
                Dock = DockStyle.Bottom,
                Height = 35,
                Text = "🎟 Đặt vé ngay",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(229, 9, 20),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };

            btnBookNow.FlatAppearance.BorderSize = 0;

            btnBookNow.Click += (s, e) =>
            {
                if (MovieData == null || MovieData.Status == MovieStatus.Ended)
                    return;

                OnBookNowClicked?.Invoke(this, MovieData);
            };

            // Thêm control theo thứ tự Dock.
            pnlInfo.Controls.Add(btnBookNow);
            pnlInfo.Controls.Add(lblSubInfo);
            pnlInfo.Controls.Add(lblTitle);

            pnlCard.Controls.Add(pnlInfo);
            pnlCard.Controls.Add(picPoster);

            Controls.Add(pnlCard);

            // ---------------- CLICK EVENTS ----------------

            pnlCard.Click += OnCardClicked;
            picPoster.Click += OnCardClicked;
            lblTitle.Click += OnCardClicked;
            lblSubInfo.Click += OnCardClicked;

            // ---------------- HOVER EFFECT ----------------

            pnlCard.MouseEnter += (s, e) =>
            {
                _isHovered = true;
                UpdateThemeColors();
            };

            pnlCard.MouseLeave += (s, e) =>
            {
                _isHovered = false;
                UpdateThemeColors();
            };

            picPoster.MouseEnter += (s, e) =>
            {
                _isHovered = true;
                UpdateThemeColors();
            };

            picPoster.MouseLeave += (s, e) =>
            {
                _isHovered = false;
                UpdateThemeColors();
            };
        }

        public void SetTheme(bool isDark)
        {
            _isDarkTheme = isDark;
            UpdateThemeColors();
        }

        private void UpdateThemeColors()
        {
            Color surface = _isDarkTheme
                ? (_isHovered ? Color.FromArgb(45, 45, 65) : Color.FromArgb(30, 30, 46))
                : (_isHovered ? Color.FromArgb(241, 245, 249) : Color.White);

            pnlCard.BackColor = surface;
            pnlInfo.BackColor = surface;
            picPoster.BackColor = _isDarkTheme
                ? Color.FromArgb(45, 45, 60)
                : Color.FromArgb(226, 232, 240);
            lblTitle.ForeColor = _isDarkTheme
                ? Color.White
                : Color.FromArgb(23, 32, 51);
            lblSubInfo.ForeColor = _isDarkTheme
                ? Color.FromArgb(190, 195, 210)
                : Color.FromArgb(100, 116, 139);
        }

        // =========================================================
        // 4. PLACEHOLDER POSTER
        // =========================================================

        private void PicPoster_PaintPlaceholder(
            object? sender,
            PaintEventArgs e)
        {
            if (picPoster.Image != null)
                return;

            using var font = new Font("Segoe UI Emoji", 32f);

            using var brush = new SolidBrush(
                Color.FromArgb(120, 130, 160));

            using var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            e.Graphics.DrawString(
                "🎬",
                font,
                brush,
                picPoster.ClientRectangle,
                sf);
        }

        // =========================================================
        // 5. SET MOVIE
        // =========================================================

        public void SetMovie(Movie movie)
        {
            if (movie == null)
                return;

            MovieData = movie;

            lblTitle.Text = movie.Title ?? "Chưa có tên phim";

            string genreDisplay =
                string.IsNullOrWhiteSpace(movie.Genre)
                    ? "Chưa phân loại"
                    : movie.Genre;

            lblSubInfo.Text =
                $"{movie.ReleaseDate.Year} • {genreDisplay} ({movie.DurationMinutes}p)";

            // ---------------- STATUS ----------------

            if (movie.Status == MovieStatus.ComingSoon)
            {
                lblStatusBadge.Text = "SẮP CHIẾU";
                lblStatusBadge.BackColor =
                    Color.FromArgb(217, 119, 6);

                lblStatusBadge.Visible = true;

                btnBookNow.Text = "🎟 Đặt vé trước";
                btnBookNow.BackColor =
                    Color.FromArgb(229, 9, 20);
                btnBookNow.Enabled = true;
            }
            else if (movie.Status == MovieStatus.Showing)
            {
                lblStatusBadge.Visible = false;

                btnBookNow.Text = "🎟 Đặt vé ngay";
                btnBookNow.BackColor =
                    Color.FromArgb(229, 9, 20);
                btnBookNow.Enabled = true;
            }
            else
            {
                lblStatusBadge.Text = "ĐÃ KẾT THÚC";
                lblStatusBadge.BackColor =
                    Color.FromArgb(100, 116, 139);
                lblStatusBadge.Visible = true;

                btnBookNow.Text = "Ngừng chiếu";
                btnBookNow.BackColor =
                    Color.FromArgb(71, 85, 105);
                btnBookNow.Enabled = false;
            }

            // ---------------- RELEASE OLD IMAGE ----------------

            if (picPoster.Image != null)
            {
                Image oldImage = picPoster.Image;

                picPoster.Image = null;

                oldImage.Dispose();
            }

            picPoster.SizeMode = PictureBoxSizeMode.Zoom;

            // ---------------- LOAD POSTER ----------------

            if (!string.IsNullOrWhiteSpace(movie.PosterUrl) &&
                File.Exists(movie.PosterUrl))
            {
                try
                {
                    byte[] imageBytes =
                        File.ReadAllBytes(movie.PosterUrl);

                    using var ms = new MemoryStream(imageBytes);
                    using var tempImage = Image.FromStream(ms);

                    picPoster.Image = new Bitmap(tempImage);
                }
                catch
                {
                    picPoster.Image = null;
                }
            }

            picPoster.Invalidate();
        }

        // =========================================================
        // 6. CLICK CARD
        // =========================================================

        private void OnCardClicked(object? sender, EventArgs e)
        {
            if (MovieData != null)
            {
                MovieSelected?.Invoke(this, MovieData);
            }
        }
        

           
    }
}
