
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Models;
using CinemaManagement.Models.Enums;
using CinemaManagement.Repositories;
using CinemaManagement.UserControls.Common;

namespace CinemaManagement.UserControls.Customer
{
    public partial class HomeUC : UserControl
    {
        private readonly MovieRepository _movieRepository = new();

        private Panel pnlSidebar = null!;
        private Panel pnlMainContent = null!;
        private Panel pnlGenresList = null!;
        private FlowLayoutPanel flpMovieGrid = null!;
        private TextBox txtSearch = null!;
        private Label lblTitle = null!;
        private Label lblSubTitle = null!;

        private Button btnWatchNow = null!;
        private Button btnComingSoon = null!;
        private Button btnBrowseAll = null!;

        private List<Movie> _allMovies = new();
        private string? _selectedGenreFilter;
        private MovieStatus? _selectedStatusFilter = MovieStatus.Showing;

        // Chuyển sang màn hình chi tiết phim.
        public event EventHandler<Movie>? OnMovieClicked;

        // Chuyển sang màn hình chọn suất chiếu.
        public event EventHandler<Movie>? OnBookNowClicked;

        public HomeUC()
        {
            InitializeComponents();
            RefreshMovies();

            // Tải lại dữ liệu khi quay lại trang chủ.
            VisibleChanged += HomeUC_VisibleChanged;
        }

        private void HomeUC_VisibleChanged(object? sender, EventArgs e)
        {
            if (Visible)
            {
                RefreshMovies();
            }
        }

        private void InitializeComponents()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(24, 24, 36);

            // ================= SIDEBAR =================

            pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = Color.FromArgb(18, 18, 28),
                Padding = new Padding(15)
            };

            Label lblLibraryHeader = new Label
            {
                Text = "DANH MỤC PHIM",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 42,
                TextAlign = ContentAlignment.MiddleLeft
            };

            btnWatchNow = CreateSidebarButton("▶  Đang chiếu", true);
            btnComingSoon = CreateSidebarButton("◷  Sắp chiếu", false);
            btnBrowseAll = CreateSidebarButton("☰  Tất cả phim", false);

            btnWatchNow.Click += (sender, e) =>
            {
                _selectedStatusFilter = MovieStatus.Showing;
                _selectedGenreFilter = null;

                lblTitle.Text = "Phim Đang Chiếu";
                lblSubTitle.Text =
                    "Chọn phim bạn yêu thích hoặc nhấn Đặt vé ngay để chọn suất chiếu.";

                SetActiveSidebarButton(btnWatchNow);
                BuildDynamicGenresList();
                ApplyFilter();
            };

            btnComingSoon.Click += (sender, e) =>
            {
                _selectedStatusFilter = MovieStatus.ComingSoon;
                _selectedGenreFilter = null;

                lblTitle.Text = "Phim Sắp Chiếu";
                lblSubTitle.Text =
                    "Những bộ phim chuẩn bị ra mắt tại rạp trong thời gian tới.";

                SetActiveSidebarButton(btnComingSoon);
                BuildDynamicGenresList();
                ApplyFilter();
            };

            btnBrowseAll.Click += (sender, e) =>
            {
                _selectedStatusFilter = null;
                _selectedGenreFilter = null;

                lblTitle.Text = "Tất Cả Phim Tại Rạp";
                lblSubTitle.Text =
                    "Toàn bộ danh sách phim đang chiếu và sắp khởi chiếu.";

                SetActiveSidebarButton(btnBrowseAll);
                BuildDynamicGenresList();
                ApplyFilter();
            };

            Label lblGenresHeader = new Label
            {
                Text = "THỂ LOẠI",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(150, 150, 170),
                Dock = DockStyle.Top,
                Height = 38,
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlGenresList = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            pnlSidebar.Controls.Add(pnlGenresList);
            pnlSidebar.Controls.Add(lblGenresHeader);
            pnlSidebar.Controls.Add(btnBrowseAll);
            pnlSidebar.Controls.Add(btnComingSoon);
            pnlSidebar.Controls.Add(btnWatchNow);
            pnlSidebar.Controls.Add(lblLibraryHeader);

            // ================= MAIN CONTENT =================

            pnlMainContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(25),
                BackColor = Color.FromArgb(24, 24, 36)
            };

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 92
            };

            lblTitle = new Label
            {
                Text = "Phim Đang Chiếu",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(0, 0),
                AutoSize = true
            };

            lblSubTitle = new Label
            {
                Text = "Chọn phim bạn yêu thích hoặc nhấn Đặt vé ngay để chọn suất chiếu.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(150, 150, 170),
                Location = new Point(2, 42),
                AutoSize = true
            };

            Panel pnlSearchBox = new Panel
            {
                Dock = DockStyle.Right,
                Width = 350
            };

            txtSearch = new TextBox
            {
                Font = new Font("Segoe UI", 10.5f),
                PlaceholderText = "Tìm tên phim, thể loại, đạo diễn...",
                Size = new Size(240, 34),
                Location = new Point(0, 12)
            };

            txtSearch.TextChanged += (sender, e) => ApplyFilter();

            Button btnRefresh = new Button
            {
                Text = "↻ Làm mới",
                Location = new Point(248, 11),
                Size = new Size(98, 35),
                BackColor = Color.FromArgb(40, 42, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (sender, e) => RefreshMovies();

            pnlSearchBox.Controls.Add(txtSearch);
            pnlSearchBox.Controls.Add(btnRefresh);

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubTitle);
            pnlHeader.Controls.Add(pnlSearchBox);

            flpMovieGrid = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
                WrapContents = true,
                Padding = new Padding(0, 12, 0, 0)
            };

            pnlMainContent.Controls.Add(flpMovieGrid);
            pnlMainContent.Controls.Add(pnlHeader);

            Controls.Add(pnlMainContent);
            Controls.Add(pnlSidebar);
        }

        // ================= SIDEBAR BUTTON =================

        private Button CreateSidebarButton(string text, bool isActive)
        {
            Button btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = isActive
                    ? Color.White
                    : Color.FromArgb(160, 160, 180),

                BackColor = isActive
                    ? Color.FromArgb(229, 9, 20)
                    : Color.Transparent,

                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Top,
                Height = 42,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                Margin = new Padding(0, 4, 0, 4),
                Cursor = Cursors.Hand
            };

            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void SetActiveSidebarButton(Button activeBtn)
        {
            foreach (Button btn in new[]
                     {
                         btnWatchNow,
                         btnComingSoon,
                         btnBrowseAll
                     })
            {
                bool isSelected = btn == activeBtn;

                btn.BackColor = isSelected
                    ? Color.FromArgb(229, 9, 20)
                    : Color.Transparent;

                btn.ForeColor = isSelected
                    ? Color.White
                    : Color.FromArgb(160, 160, 180);
            }
        }

        // ================= LOAD MOVIES =================

        public void RefreshMovies()
        {
            try
            {
                _allMovies = _movieRepository.GetAll()
                    .Where(movie =>
                        movie.Status == MovieStatus.Showing ||
                        movie.Status == MovieStatus.ComingSoon)
                    .ToList();

                BuildDynamicGenresList();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                _allMovies.Clear();
                flpMovieGrid.Controls.Clear();

                MessageBox.Show(
                    "Không thể tải danh sách phim từ hệ thống.\n\n" + ex.Message,
                    "Lỗi tải phim",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // ================= DYNAMIC GENRES =================

        private void BuildDynamicGenresList()
        {
            pnlGenresList.Controls.Clear();

            var genres = _allMovies
                .SelectMany(movie =>
                    (movie.Genre ?? string.Empty).Split(
                        new[] { ',', '/', '|' },
                        StringSplitOptions.RemoveEmptyEntries))
                .Select(genre => genre.Trim())
                .Where(genre => !string.IsNullOrWhiteSpace(genre))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(genre => genre)
                .ToList();

            var genreItems = new List<(string Display, string? FilterKey)>
            {
                ("Tất cả thể loại", null)
            };

            genreItems.AddRange(
                genres.Select(genre => (genre, (string?)genre)));

            int yOffset = 8;

            foreach (var genre in genreItems)
            {
                bool isSelected = string.Equals(
                    _selectedGenreFilter,
                    genre.FilterKey,
                    StringComparison.OrdinalIgnoreCase);

                Label lblGenreItem = new Label
                {
                    Text = genre.Display,
                    Font = new Font(
                        "Segoe UI",
                        9.5f,
                        isSelected
                            ? FontStyle.Bold
                            : FontStyle.Regular),

                    ForeColor = isSelected
                        ? Color.FromArgb(229, 9, 20)
                        : Color.FromArgb(180, 180, 200),

                    Location = new Point(8, yOffset),
                    AutoSize = true,
                    Cursor = Cursors.Hand
                };

                lblGenreItem.MouseEnter += (sender, e) =>
                    lblGenreItem.ForeColor = Color.FromArgb(229, 9, 20);

                lblGenreItem.MouseLeave += (sender, e) =>
                {
                    bool active = string.Equals(
                        _selectedGenreFilter,
                        genre.FilterKey,
                        StringComparison.OrdinalIgnoreCase);

                    lblGenreItem.ForeColor = active
                        ? Color.FromArgb(229, 9, 20)
                        : Color.FromArgb(180, 180, 200);
                };

                lblGenreItem.Click += (sender, e) =>
                {
                    _selectedGenreFilter = genre.FilterKey;
                    BuildDynamicGenresList();
                    ApplyFilter();
                };

                pnlGenresList.Controls.Add(lblGenreItem);
                yOffset += 30;
            }
        }

        // ================= DISPLAY MOVIES =================

        private void DisplayMovies(List<Movie> movies)
        {
            flpMovieGrid.SuspendLayout();
            flpMovieGrid.Controls.Clear();

            if (movies.Count == 0)
            {
                Label lblEmpty = new Label
                {
                    Text = _allMovies.Count == 0
                        ? "🎬 Hiện chưa có phim nào trong hệ thống.\nVui lòng đăng nhập tài khoản Admin và thêm phim mới."
                        : "🔍 Không tìm thấy phim phù hợp với điều kiện lọc.",

                    Font = new Font("Segoe UI", 11.5f),
                    ForeColor = Color.FromArgb(160, 165, 190),
                    AutoSize = true,
                    Padding = new Padding(12, 30, 0, 0)
                };

                flpMovieGrid.Controls.Add(lblEmpty);
                flpMovieGrid.ResumeLayout();
                return;
            }

            foreach (Movie movie in movies)
            {
                MovieCardUC card = new MovieCardUC(movie);

                // Click thẻ phim: mở chi tiết phim.
                card.MovieSelected += (sender, selectedMovie) =>
                {
                    OnMovieClicked?.Invoke(this, selectedMovie);
                };

                // Phim sắp chiếu: mở chi tiết.
                // Phim đang chiếu: chuyển sang chọn suất chiếu.
                card.OnBookNowClicked += (sender, selectedMovie) =>
                {
                    if (selectedMovie.Status == MovieStatus.ComingSoon)
                    {
                        OnMovieClicked?.Invoke(this, selectedMovie);
                    }
                    else
                    {
                        OnBookNowClicked?.Invoke(this, selectedMovie);
                    }
                };

                flpMovieGrid.Controls.Add(card);
            }

            flpMovieGrid.ResumeLayout();
        }

        // ================= FILTER MOVIES =================

        private void ApplyFilter()
        {
            string keyword = txtSearch.Text.Trim();

            IEnumerable<Movie> filtered = _allMovies;

            // Lọc theo trạng thái.
            if (_selectedStatusFilter.HasValue)
            {
                filtered = filtered.Where(movie =>
                    movie.Status == _selectedStatusFilter.Value);
            }

            // Lọc theo thể loại.
            if (!string.IsNullOrWhiteSpace(_selectedGenreFilter))
            {
                filtered = filtered.Where(movie =>
                    (movie.Genre ?? string.Empty).Contains(
                        _selectedGenreFilter,
                        StringComparison.OrdinalIgnoreCase));
            }

            // Lọc theo tên phim, thể loại hoặc đạo diễn.
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                filtered = filtered.Where(movie =>
                    (movie.Title ?? string.Empty).Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase) ||

                    (movie.Genre ?? string.Empty).Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase) ||

                    (movie.Director ?? string.Empty).Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase));
            }

            DisplayMovies(filtered.ToList());
        }
    }
}
