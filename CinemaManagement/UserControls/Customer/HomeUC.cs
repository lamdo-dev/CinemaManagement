namespace CinemaManagement.UserControls.Customer;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Models;
<<<<<<< HEAD
using CinemaManagement.UserControls.Common;
using CinemaManagement.Models.Enums;
using CinemaManagement.Repositories;

public partial class HomeUC : UserControl
{
    private Panel pnlSidebar = null!;
    private Panel pnlMainContent = null!;
    private FlowLayoutPanel flpMovieGrid = null!;
    private TextBox txtSearch = null!;

    private List<Movie> _allMovies = new();
    private string? _selectedGenreFilter;
    
    private readonly MovieRepository _movieRepository = new();    
=======
using CinemaManagement.Models.Enums;
using CinemaManagement.Repositories;
using CinemaManagement.UserControls.Common;

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
>>>>>>> origin/dev

    // Sự kiện chuyển màn hình sang Chi tiết Phim (MovieDetailUC)
    public event EventHandler<Movie>? OnMovieClicked;

    // Sự kiện chuyển thẳng sang Chọn Suất Chiếu (ShowtimeUC) khi bấm "Đặt vé ngay"
    public event EventHandler<Movie>? OnBookNowClicked;

    public HomeUC()
    {
        InitializeComponents();
<<<<<<< HEAD
        LoadMovieData();
=======
        RefreshMovies();

        // Tự động làm mới dữ liệu khi quay lại màn hình Trang chủ
        this.VisibleChanged += (s, e) =>
        {
            if (this.Visible)
            {
                RefreshMovies();
            }
        };
>>>>>>> origin/dev
    }

    private void InitializeComponents()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.FromArgb(24, 24, 36);

        // -------------------------------------------------------------
<<<<<<< HEAD
        // 1. SIDEBAR BÊN TRÁI (DANH MỤC & THỂ LOẠI)
=======
        // 1. SIDEBAR BÊN TRÁI (DANH MỤC & THỂ LOẠI ĐỘNG TỪ DB)
>>>>>>> origin/dev
        // -------------------------------------------------------------
        pnlSidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 220,
            BackColor = Color.FromArgb(18, 18, 28),
            Padding = new Padding(15)
        };

        Label lblLibraryHeader = new Label
        {
<<<<<<< HEAD
            Text = "Library",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 40
        };

        Button btnWatchNow = CreateSidebarButton("▶  Watch Now", true);
        Button btnBrowse = CreateSidebarButton("☰  Browse", false);
        Button btnLiked = CreateSidebarButton("♡  Liked", false);

        btnWatchNow.Click += (s, e) =>
        {
            _selectedGenreFilter = null;
            txtSearch.Text = "";
            ApplyFilter();
        };
        btnBrowse.Click += (s, e) =>
        {
            _selectedGenreFilter = null;
            ApplyFilter();
        };
        btnLiked.Click += (s, e) =>
        {
            _selectedGenreFilter = null;
=======
            Text = "Danh mục phim",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 38
        };

        btnWatchNow = CreateSidebarButton("▶  Đang chiếu", true);
        btnComingSoon = CreateSidebarButton("🕒  Sắp chiếu", false);
        btnBrowseAll = CreateSidebarButton("☰  Tất cả phim", false);

        btnWatchNow.Click += (s, e) =>
        {
            _selectedStatusFilter = MovieStatus.Showing;
            _selectedGenreFilter = null;
            lblTitle.Text = "Phim Đang Chiếu";
            lblSubTitle.Text = "Chọn phim bạn yêu thích hoặc nhấn 'Đặt vé ngay' để chọn suất chiếu.";
            SetActiveSidebarButton(btnWatchNow);
            BuildDynamicGenresList();
            ApplyFilter();
        };

        btnComingSoon.Click += (s, e) =>
        {
            _selectedStatusFilter = MovieStatus.ComingSoon;
            _selectedGenreFilter = null;
            lblTitle.Text = "Phim Sắp Chiếu";
            lblSubTitle.Text = "Những bộ phim chuẩn bị ra mắt tại rạp trong thời gian tới.";
            SetActiveSidebarButton(btnComingSoon);
            BuildDynamicGenresList();
            ApplyFilter();
        };

        btnBrowseAll.Click += (s, e) =>
        {
            _selectedStatusFilter = null;
            _selectedGenreFilter = null;
            lblTitle.Text = "Tất Cả Phim Tại Rạp";
            lblSubTitle.Text = "Toàn bộ danh sách phim đang chiếu và sắp khởi chiếu.";
            SetActiveSidebarButton(btnBrowseAll);
            BuildDynamicGenresList();
>>>>>>> origin/dev
            ApplyFilter();
        };

        Label lblGenresHeader = new Label
        {
<<<<<<< HEAD
            Text = "Genres",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.FromArgb(150, 150, 170),
            Location = new Point(15, 180),
            AutoSize = true
        };

        // Danh sách Thể loại Phim
        Panel pnlGenresList = new Panel
        {
            Location = new Point(15, 210),
            Size = new Size(190, 300),
            AutoScroll = true
        };

        (string Display, string? FilterKey)[] genres =
        {
            ("All Movies", null),
            ("Action", "Action"),
            ("Sci-Fi", "Sci-Fi"),
            ("Musical", "Musical"),
            ("Romance", "Romance"),
            ("Drama", "Drama")
        };

        int yOffset = 0;
        foreach (var genre in genres)
        {
            Label lblGenreItem = new Label
            {
                Text = genre.Display,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(180, 180, 200),
                Location = new Point(5, yOffset),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            lblGenreItem.MouseEnter += (s, e) => lblGenreItem.ForeColor = Color.FromArgb(229, 9, 20); // Red Accent
            lblGenreItem.MouseLeave += (s, e) => lblGenreItem.ForeColor = Color.FromArgb(180, 180, 200);
            lblGenreItem.Click += (s, e) =>
            {
                _selectedGenreFilter = genre.FilterKey;
                ApplyFilter();
            };

            pnlGenresList.Controls.Add(lblGenreItem);
            yOffset += 30;
        }

        pnlSidebar.Controls.Add(pnlGenresList);
        pnlSidebar.Controls.Add(lblGenresHeader);
        pnlSidebar.Controls.Add(btnLiked);
        pnlSidebar.Controls.Add(btnBrowse);
=======
            Text = "Thể loại",
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(150, 150, 170),
            Location = new Point(15, 185),
            AutoSize = true
        };

        pnlGenresList = new Panel
        {
            Location = new Point(15, 215),
            Size = new Size(190, 350),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left,
            AutoScroll = true
        };

        pnlSidebar.Controls.Add(pnlGenresList);
        pnlSidebar.Controls.Add(lblGenresHeader);
        pnlSidebar.Controls.Add(btnBrowseAll);
        pnlSidebar.Controls.Add(btnComingSoon);
>>>>>>> origin/dev
        pnlSidebar.Controls.Add(btnWatchNow);
        pnlSidebar.Controls.Add(lblLibraryHeader);

        // -------------------------------------------------------------
<<<<<<< HEAD
        // 2. KHU VỰC NỘI DUNG CHÍNH BÊN PHẢI (MAIN CONTENT)
=======
        // 2. KHU VỰC NỘI DUNG CHÍNH BÊN PHẢI
>>>>>>> origin/dev
        // -------------------------------------------------------------
        pnlMainContent = new Panel
        {
            Dock = DockStyle.Fill,
<<<<<<< HEAD
            Padding = new Padding(25),
            AutoScroll = true
        };

        // Top Header: Tiêu đề + Thanh tìm kiếm
        Panel pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 90
        };

        Label lblTitle = new Label
        {
            Text = "Watch Now",
=======
            Padding = new Padding(25, 20, 25, 20)
        };

        Panel pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 82
        };

        lblTitle = new Label
        {
            Text = "Phim Đang Chiếu",
>>>>>>> origin/dev
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(0, 0),
            AutoSize = true
        };

<<<<<<< HEAD
        Label lblSubTitle = new Label
        {
            Text = "Jump right into something new, chosen for you. Click a movie or 'Đặt vé ngay' to book.",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(150, 150, 170),
            Location = new Point(0, 38),
            AutoSize = true
        };

        // Ô tìm kiếm Phim
        txtSearch = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            PlaceholderText = "🔍 Search movies...",
            Size = new Size(250, 35),
            Location = new Point(550, 10),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        txtSearch.TextChanged += TxtSearch_TextChanged;

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSubTitle);
        pnlHeader.Controls.Add(txtSearch);

        // Grid chứa các thẻ phim MovieCardUC
=======
        lblSubTitle = new Label
        {
            Text = "Chọn phim bạn yêu thích hoặc nhấn 'Đặt vé ngay' để chọn suất chiếu.",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(150, 150, 170),
            Location = new Point(2, 42),
            AutoSize = true
        };

        Panel pnlSearchBox = new Panel
        {
            Dock = DockStyle.Right,
            Width = 345
        };

        txtSearch = new TextBox
        {
            Font = new Font("Segoe UI", 10.5f),
            PlaceholderText = "🔍 Tìm tên phim, đạo diễn...",
            Size = new Size(240, 32),
            Location = new Point(0, 12)
        };
        txtSearch.TextChanged += (s, e) => ApplyFilter();

        Button btnRefresh = new Button
        {
            Text = "🔄 Làm mới",
            Location = new Point(248, 11),
            Size = new Size(95, 32),
            BackColor = Color.FromArgb(40, 42, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnRefresh.FlatAppearance.BorderSize = 0;
        btnRefresh.Click += (s, e) => RefreshMovies();

        pnlSearchBox.Controls.Add(txtSearch);
        pnlSearchBox.Controls.Add(btnRefresh);

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSubTitle);
        pnlHeader.Controls.Add(pnlSearchBox);

>>>>>>> origin/dev
        flpMovieGrid = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Transparent
        };

        pnlMainContent.Controls.Add(flpMovieGrid);
        pnlMainContent.Controls.Add(pnlHeader);

<<<<<<< HEAD
        // Add cả 2 Panel vào UserControl
=======
>>>>>>> origin/dev
        this.Controls.Add(pnlMainContent);
        this.Controls.Add(pnlSidebar);
    }

    private Button CreateSidebarButton(string text, bool isActive)
    {
        Button btn = new Button
        {
            Text = text,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = isActive ? Color.White : Color.FromArgb(160, 160, 180),
<<<<<<< HEAD
            BackColor = isActive ? Color.FromArgb(229, 9, 20) : Color.Transparent, // Đỏ CinemaAccent
=======
            BackColor = isActive ? Color.FromArgb(229, 9, 20) : Color.Transparent,
>>>>>>> origin/dev
            FlatStyle = FlatStyle.Flat,
            Dock = DockStyle.Top,
            Height = 40,
            TextAlign = ContentAlignment.MiddleLeft,
<<<<<<< HEAD
            Margin = new Padding(0, 5, 0, 5),
=======
            Padding = new Padding(8, 0, 0, 0),
            Margin = new Padding(0, 4, 0, 4),
>>>>>>> origin/dev
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
<<<<<<< HEAD
        
    }

    // Tải dữ liệu giả lập để hiển thị thử giao diện
    
    private void LoadMovieData()
    {
        try
        {
            // Lấy danh sách phim mới nhất từ SQLite
            List<Movie> movies = _movieRepository.GetAll();

            // Customer chỉ xem phim đang chiếu và sắp chiếu
            _allMovies = movies
                .Where(movie =>
                    movie.Status == MovieStatus.Showing ||
                    movie.Status == MovieStatus.ComingSoon)
                .ToList();

=======
    }

    private void SetActiveSidebarButton(Button activeBtn)
    {
        foreach (var btn in new[] { btnWatchNow, btnComingSoon, btnBrowseAll })
        {
            bool isSelected = btn == activeBtn;
            btn.BackColor = isSelected ? Color.FromArgb(229, 9, 20) : Color.Transparent;
            btn.ForeColor = isSelected ? Color.White : Color.FromArgb(160, 160, 180);
        }
    }

    // =============================================================
    // 3. TẢI & LỌC DỮ LIỆU PHIM QUA MovieRepository
    // =============================================================

    public void RefreshMovies()
    {
        try
        {
            // Khách hàng chỉ xem phim Đang chiếu hoặc Sắp chiếu
            _allMovies = _movieRepository.GetAll()
                .Where(m => m.Status == MovieStatus.Showing || m.Status == MovieStatus.ComingSoon)
                .ToList();

            BuildDynamicGenresList();
>>>>>>> origin/dev
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _allMovies.Clear();
            flpMovieGrid.Controls.Clear();

            MessageBox.Show(
<<<<<<< HEAD
                "Không thể tải danh sách phim từ hệ thống.\n\n" +
                ex.Message,
                "Lỗi tải phim",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

=======
                "Không thể tải danh sách phim từ hệ thống.\n\n" + ex.Message,
                "Lỗi tải phim",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    private void BuildDynamicGenresList()
    {
        pnlGenresList.Controls.Clear();

        var distinctGenres = _allMovies
            .SelectMany(m => (m.Genre ?? string.Empty)
                .Split(new[] { ',', '/', '|' }, StringSplitOptions.RemoveEmptyEntries))
            .Select(g => g.Trim())
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g)
            .ToList();

        var genreItems = new List<(string Display, string? FilterKey)>
        {
            ("Tất cả thể loại", null)
        };

        foreach (var g in distinctGenres)
        {
            genreItems.Add((g, g));
        }

        int yOffset = 0;
        foreach (var genre in genreItems)
        {
            bool isSelected = string.Equals(_selectedGenreFilter, genre.FilterKey, StringComparison.OrdinalIgnoreCase);

            Label lblGenreItem = new Label
            {
                Text = genre.Display,
                Font = new Font("Segoe UI", 9.5f, isSelected ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = isSelected ? Color.FromArgb(229, 9, 20) : Color.FromArgb(180, 180, 200),
                Location = new Point(5, yOffset),
                AutoSize = true,
                Cursor = Cursors.Hand
            };

            lblGenreItem.MouseEnter += (s, e) => lblGenreItem.ForeColor = Color.FromArgb(229, 9, 20);
            lblGenreItem.MouseLeave += (s, e) =>
            {
                bool active = string.Equals(_selectedGenreFilter, genre.FilterKey, StringComparison.OrdinalIgnoreCase);
                lblGenreItem.ForeColor = active ? Color.FromArgb(229, 9, 20) : Color.FromArgb(180, 180, 200);
            };

            lblGenreItem.Click += (s, e) =>
            {
                _selectedGenreFilter = genre.FilterKey;
                BuildDynamicGenresList();
                ApplyFilter();
            };

            pnlGenresList.Controls.Add(lblGenreItem);
            yOffset += 30;
        }
    }
>>>>>>> origin/dev

    private void DisplayMovies(List<Movie> movies)
    {
        flpMovieGrid.SuspendLayout();
        flpMovieGrid.Controls.Clear();

<<<<<<< HEAD
=======
        if (movies.Count == 0)
        {
            Label lblEmpty = new Label
            {
                Text = _allMovies.Count == 0
                    ? "🎬 Hiện chưa có phim nào trong hệ thống.\nVui lòng đăng nhập tài khoản Admin -> Quản lý Phim để thêm phim mới."
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

>>>>>>> origin/dev
        foreach (var movie in movies)
        {
            MovieCardUC card = new MovieCardUC(movie);

<<<<<<< HEAD
            // Sự kiện click vào thẻ phim -> xem chi tiết phim (MovieDetailUC)
=======
>>>>>>> origin/dev
            card.MovieSelected += (s, selectedMovie) =>
            {
                OnMovieClicked?.Invoke(this, selectedMovie);
            };

<<<<<<< HEAD
            // Sự kiện click nút "Đặt vé ngay" -> sang thẳng chọn suất chiếu (ShowtimeUC)
            card.OnBookNowClicked += (s, selectedMovie) =>
            {
                OnBookNowClicked?.Invoke(this, selectedMovie);
=======
            card.OnBookNowClicked += (s, selectedMovie) =>
            {
                if (selectedMovie.Status == MovieStatus.ComingSoon)
                {
                    OnMovieClicked?.Invoke(this, selectedMovie);
                }
                else
                {
                    OnBookNowClicked?.Invoke(this, selectedMovie);
                }
>>>>>>> origin/dev
            };

            flpMovieGrid.Controls.Add(card);
        }

        flpMovieGrid.ResumeLayout();
    }

    private void ApplyFilter()
    {
        string keyword = txtSearch.Text.Trim();
        IEnumerable<Movie> filtered = _allMovies;

<<<<<<< HEAD
        if (!string.IsNullOrEmpty(_selectedGenreFilter))
        {
            filtered = filtered.Where(m => m.Genre.Contains(_selectedGenreFilter, StringComparison.OrdinalIgnoreCase));
=======
        if (_selectedStatusFilter.HasValue)
        {
            filtered = filtered.Where(m => m.Status == _selectedStatusFilter.Value);
        }

        if (!string.IsNullOrEmpty(_selectedGenreFilter))
        {
            filtered = filtered.Where(m =>
                (m.Genre ?? string.Empty).Contains(_selectedGenreFilter, StringComparison.OrdinalIgnoreCase));
>>>>>>> origin/dev
        }

        if (!string.IsNullOrEmpty(keyword))
        {
            filtered = filtered.Where(m =>
<<<<<<< HEAD
                m.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                m.Genre.Contains(keyword, StringComparison.OrdinalIgnoreCase));
=======
                (m.Title ?? string.Empty).Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                (m.Genre ?? string.Empty).Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                (m.Director ?? string.Empty).Contains(keyword, StringComparison.OrdinalIgnoreCase));
>>>>>>> origin/dev
        }

        DisplayMovies(filtered.ToList());
    }
<<<<<<< HEAD

    private void TxtSearch_TextChanged(object? sender, EventArgs e)
    {
        ApplyFilter();
    }


    public void RefreshMovies()
    {
        LoadMovieData();
    }

} 

=======
}
>>>>>>> origin/dev
