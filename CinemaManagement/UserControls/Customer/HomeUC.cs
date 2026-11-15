namespace CinemaManagement.UserControls.Customer;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Models;
using CinemaManagement.Models.Enums;
using CinemaManagement.UserControls.Common;
using Dapper;
using Microsoft.Data.Sqlite;

public partial class HomeUC : UserControl
{
    private const string ConnectionString = "Data Source=cinema.db";

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
    private MovieStatus? _selectedStatusFilter = MovieStatus.Showing; // Mặc định hiện Phim đang chiếu

    // Sự kiện chuyển màn hình sang Chi tiết Phim (MovieDetailUC)
    public event EventHandler<Movie>? OnMovieClicked;

    // Sự kiện chuyển thẳng sang Chọn Suất Chiếu (ShowtimeUC) khi bấm "Đặt vé ngay"
    public event EventHandler<Movie>? OnBookNowClicked;

    public HomeUC()
    {
        InitializeComponents();
        LoadMoviesFromDatabase();

        // Tự động làm mới dữ liệu mỗi khi người dùng quay lại màn hình Trang chủ
        this.VisibleChanged += (s, e) =>
        {
            if (this.Visible)
            {
                LoadMoviesFromDatabase();
            }
        };
    }

    private void InitializeComponents()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.FromArgb(24, 24, 36);

        // -------------------------------------------------------------
        // 1. SIDEBAR BÊN TRÁI (DANH MỤC & THỂ LOẠI ĐỘNG TỪ DB)
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
            ApplyFilter();
        };

        btnComingSoon.Click += (s, e) =>
        {
            _selectedStatusFilter = MovieStatus.ComingSoon;
            _selectedGenreFilter = null;
            lblTitle.Text = "Phim Sắp Chiếu";
            lblSubTitle.Text = "Những bom tấn chuẩn bị ra mắt tại rạp trong thời gian tới.";
            SetActiveSidebarButton(btnComingSoon);
            ApplyFilter();
        };

        btnBrowseAll.Click += (s, e) =>
        {
            _selectedStatusFilter = null;
            _selectedGenreFilter = null;
            lblTitle.Text = "Tất Cả Phim Tại Rạp";
            lblSubTitle.Text = "Toàn bộ danh sách phim đang chiếu và sắp khởi chiếu.";
            SetActiveSidebarButton(btnBrowseAll);
            ApplyFilter();
        };

        Label lblGenresHeader = new Label
        {
            Text = "Thể loại (Genres)",
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(150, 150, 170),
            Location = new Point(15, 185),
            AutoSize = true
        };

        pnlGenresList = new Panel
        {
            Location = new Point(15, 215),
            Size = new Size(190, 340),
            AutoScroll = true
        };

        pnlSidebar.Controls.Add(pnlGenresList);
        pnlSidebar.Controls.Add(lblGenresHeader);
        pnlSidebar.Controls.Add(btnBrowseAll);
        pnlSidebar.Controls.Add(btnComingSoon);
        pnlSidebar.Controls.Add(btnWatchNow);
        pnlSidebar.Controls.Add(lblLibraryHeader);

        // -------------------------------------------------------------
        // 2. KHU VỰC NỘI DUNG CHÍNH BÊN PHẢI (MAIN CONTENT)
        // -------------------------------------------------------------
        pnlMainContent = new Panel
        {
            Dock = DockStyle.Fill,
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
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(0, 0),
            AutoSize = true
        };

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
        btnRefresh.Click += (s, e) => LoadMoviesFromDatabase();

        pnlSearchBox.Controls.Add(txtSearch);
        pnlSearchBox.Controls.Add(btnRefresh);

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSubTitle);
        pnlHeader.Controls.Add(pnlSearchBox);

        // Grid chứa các thẻ phim MovieCardUC
        flpMovieGrid = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Transparent
        };

        pnlMainContent.Controls.Add(flpMovieGrid);
        pnlMainContent.Controls.Add(pnlHeader);

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
            BackColor = isActive ? Color.FromArgb(229, 9, 20) : Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            Dock = DockStyle.Top,
            Height = 40,
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
        foreach (var btn in new[] { btnWatchNow, btnComingSoon, btnBrowseAll })
        {
            bool isSelected = btn == activeBtn;
            btn.BackColor = isSelected ? Color.FromArgb(229, 9, 20) : Color.Transparent;
            btn.ForeColor = isSelected ? Color.White : Color.FromArgb(160, 160, 180);
        }
    }

    // =============================================================
    // 3. TẢI DỮ LIỆU PHIM THẬT TỪ SQLITE (cinema.db)
    // =============================================================

    public void LoadMoviesFromDatabase()
    {
        try
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            // Đảm bảo bảng Movies tồn tại nếu DB mới khởi tạo
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS Movies (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Description TEXT,
                    DurationMinutes INTEGER NOT NULL DEFAULT 120,
                    Director TEXT,
                    Genre TEXT,
                    PosterUrl TEXT,
                    ReleaseDate TEXT,
                    Status INTEGER NOT NULL DEFAULT 0
                );");

            var rows = connection.Query(@"
                SELECT 
                    rowid AS Id,
                    COALESCE(Title, '') AS Title,
                    COALESCE(Description, '') AS Description,
                    COALESCE(DurationMinutes, 120) AS DurationMinutes,
                    COALESCE(Director, '') AS Director,
                    COALESCE(Genre, '') AS Genre,
                    COALESCE(PosterUrl, '') AS PosterUrl,
                    COALESCE(ReleaseDate, '') AS ReleaseDate,
                    COALESCE(Status, 0) AS Status
                FROM Movies
                WHERE COALESCE(Status, 0) != @EndedStatus
                ORDER BY rowid DESC;",
                new { EndedStatus = (int)MovieStatus.Ended }).ToList();

            _allMovies = rows.Select(r => new Movie
            {
                Id = Convert.ToInt32(r.Id),
                Title = (string)r.Title,
                Description = (string)r.Description,
                DurationMinutes = Convert.ToInt32(r.DurationMinutes),
                Director = (string)r.Director,
                Genre = (string)r.Genre,
                PosterUrl = (string)r.PosterUrl,
                ReleaseDate = ParseReleaseDate((string)r.ReleaseDate),
                Status = (MovieStatus)Convert.ToInt32(r.Status)
            }).ToList();

            BuildDynamicGenresList();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Không thể tải danh sách phim từ cơ sở dữ liệu:\n{ex.Message}",
                "Lỗi kết nối dữ liệu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    private static DateTime ParseReleaseDate(string rawDate)
    {
        if (DateTime.TryParseExact(
            rawDate,
            new[] { "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "dd/MM/yyyy" },
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime parsedExact))
        {
            return parsedExact;
        }

        if (DateTime.TryParse(rawDate, out DateTime parsed))
            return parsed;

        return DateTime.Today;
    }

    // Tự động lấy danh sách Thể loại từ các phim thật trong Database
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

    private void DisplayMovies(List<Movie> movies)
    {
        flpMovieGrid.SuspendLayout();
        flpMovieGrid.Controls.Clear();

        if (movies.Count == 0)
        {
            Label lblEmpty = new Label
            {
                Text = _allMovies.Count == 0
                    ? "🎬 Hiện chưa có phim nào trong cơ sở dữ liệu.\nVui lòng đăng nhập tài khoản Admin -> Quản lý Phim để thêm phim mới!"
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

        foreach (var movie in movies)
        {
            MovieCardUC card = new MovieCardUC(movie);

            card.MovieSelected += (s, selectedMovie) =>
            {
                OnMovieClicked?.Invoke(this, selectedMovie);
            };

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
            };

            flpMovieGrid.Controls.Add(card);
        }

        flpMovieGrid.ResumeLayout();
    }

    private void ApplyFilter()
    {
        string keyword = txtSearch.Text.Trim();
        IEnumerable<Movie> filtered = _allMovies;

        if (_selectedStatusFilter.HasValue)
        {
            filtered = filtered.Where(m => m.Status == _selectedStatusFilter.Value);
        }

        if (!string.IsNullOrEmpty(_selectedGenreFilter))
        {
            filtered = filtered.Where(m =>
                (m.Genre ?? string.Empty).Contains(_selectedGenreFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(keyword))
        {
            filtered = filtered.Where(m =>
                (m.Title ?? string.Empty).Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                (m.Genre ?? string.Empty).Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                (m.Director ?? string.Empty).Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        DisplayMovies(filtered.ToList());
    }
}