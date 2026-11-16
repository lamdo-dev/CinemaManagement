namespace CinemaManagement.UserControls.Customer;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Models;
using CinemaManagement.Models.Enums;
using CinemaManagement.Repositories;
using CinemaManagement.UserControls.Common;

public partial class HomeUC : UserControl
{
    private readonly MovieRepository _movieRepository = new();

    private Panel pnlSidebar = null!;
    private Panel pnlMainContent = null!;
    private FlowLayoutPanel flpGenresList = null!;
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

    // Sự kiện chuyển màn hình sang Chi tiết Phim (MovieDetailUC)
    public event EventHandler<Movie>? OnMovieClicked;

    // Sự kiện chuyển thẳng sang Chọn Suất Chiếu (ShowtimeUC) khi bấm "Đặt vé ngay"
    public event EventHandler<Movie>? OnBookNowClicked;

    public HomeUC()
    {
        AutoScaleMode = AutoScaleMode.Dpi;

        InitializeComponents();
        RefreshMovies();

        // Tự động làm mới dữ liệu khi quay lại màn hình Trang chủ
        VisibleChanged += (s, e) =>
        {
            if (Visible)
            {
                RefreshMovies();
            }
        };
    }

    private void InitializeComponents()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(24, 24, 36);

        // -------------------------------------------------------------
        // 1. SIDEBAR BÊN TRÁI (DANH MỤC & THỂ LOẠI ĐỘNG TỪ DB)
        // -------------------------------------------------------------
        pnlSidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 230,
            BackColor = Color.FromArgb(18, 18, 28),
            Padding = new Padding(14, 16, 14, 16)
        };

        var tlpSidebar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        tlpSidebar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpSidebar.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Tiêu đề Danh mục
        tlpSidebar.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Nút Đang chiếu
        tlpSidebar.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Nút Sắp chiếu
        tlpSidebar.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Nút Tất cả phim
        tlpSidebar.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Tiêu đề Thể loại
        tlpSidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Danh sách Thể loại

        Label lblLibraryHeader = new Label
        {
            Text = "Danh mục phim",
            Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Margin = new Padding(4, 0, 0, 10)
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
            ApplyFilter();
        };

        Label lblGenresHeader = new Label
        {
            Text = "Thể loại",
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.FromArgb(150, 150, 170),
            AutoSize = true,
            Margin = new Padding(4, 18, 0, 8)
        };

        flpGenresList = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };

        tlpSidebar.Controls.Add(lblLibraryHeader, 0, 0);
        tlpSidebar.Controls.Add(btnWatchNow, 0, 1);
        tlpSidebar.Controls.Add(btnComingSoon, 0, 2);
        tlpSidebar.Controls.Add(btnBrowseAll, 0, 3);
        tlpSidebar.Controls.Add(lblGenresHeader, 0, 4);
        tlpSidebar.Controls.Add(flpGenresList, 0, 5);

        pnlSidebar.Controls.Add(tlpSidebar);

        // -------------------------------------------------------------
        // 2. KHU VỰC NỘI DUNG CHÍNH BÊN PHẢI (RESPONSIVE HEADER + GRID)
        // -------------------------------------------------------------
        pnlMainContent = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 18, 24, 18)
        };

        var tlpHeader = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0, 0, 0, 16)
        };
        tlpHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tlpHeader.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Khối Tiêu đề + Mô tả phụ (xếp dọc tự giãn -> không bao giờ đè chữ)
        var tlpTitleBlock = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 16, 0)
        };
        tlpTitleBlock.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpTitleBlock.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tlpTitleBlock.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        lblTitle = new Label
        {
            Text = "Phim Đang Chiếu",
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4)
        };

        lblSubTitle = new Label
        {
            Text = "Chọn phim bạn yêu thích hoặc nhấn 'Đặt vé ngay' để chọn suất chiếu.",
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = Color.FromArgb(160, 165, 185),
            AutoSize = true,
            Margin = new Padding(2, 0, 0, 0)
        };

        tlpTitleBlock.Controls.Add(lblTitle, 0, 0);
        tlpTitleBlock.Controls.Add(lblSubTitle, 0, 1);

        // Khối Tìm kiếm + Nút Làm mới (tự giãn theo chiều rộng chữ)
        var flpSearchActions = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 4, 0, 0)
        };

        txtSearch = new TextBox
        {
            Font = new Font("Segoe UI", 10F),
            PlaceholderText = "🔍 Tìm tên phim, đạo diễn...",
            Width = 230,
            Margin = new Padding(0, 1, 8, 0)
        };
        txtSearch.TextChanged += (s, e) => ApplyFilter();

        Button btnRefresh = new Button
        {
            Text = "🔄 Làm mới",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(12, 5, 12, 5),
            Margin = new Padding(0),
            BackColor = Color.FromArgb(40, 42, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnRefresh.FlatAppearance.BorderSize = 0;
        btnRefresh.MouseEnter += (s, e) => btnRefresh.BackColor = Color.FromArgb(56, 59, 82);
        btnRefresh.MouseLeave += (s, e) => btnRefresh.BackColor = Color.FromArgb(40, 42, 60);
        btnRefresh.Click += (s, e) => RefreshMovies();

        flpSearchActions.Controls.Add(txtSearch);
        flpSearchActions.Controls.Add(btnRefresh);

        tlpHeader.Controls.Add(tlpTitleBlock, 0, 0);
        tlpHeader.Controls.Add(flpSearchActions, 1, 0);

        // Tự động điều chỉnh chiều rộng tối đa của dòng mô tả để xuống dòng gọn khi thu nhỏ cửa sổ
        tlpHeader.SizeChanged += (s, e) =>
        {
            int availableWidth = tlpHeader.ClientSize.Width - flpSearchActions.Width - 24;
            if (availableWidth > 180)
            {
                lblSubTitle.MaximumSize = new Size(availableWidth, 0);
            }
        };

        flpMovieGrid = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };

        pnlMainContent.Controls.Add(flpMovieGrid);
        pnlMainContent.Controls.Add(tlpHeader);

        Controls.Add(pnlMainContent);
        Controls.Add(pnlSidebar);
    }

    private Button CreateSidebarButton(string text, bool isActive)
    {
        Button btn = new Button
        {
            Text = text,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = isActive ? Color.White : Color.FromArgb(165, 170, 190),
            BackColor = isActive ? Color.FromArgb(229, 9, 20) : Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 8, 10, 8),
            Margin = new Padding(0, 3, 0, 3),
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
            btn.ForeColor = isSelected ? Color.White : Color.FromArgb(165, 170, 190);
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
                MessageBoxIcon.Error
            );
        }
    }

    private void BuildDynamicGenresList()
    {
        flpGenresList.SuspendLayout();
        flpGenresList.Controls.Clear();

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

        foreach (var genre in genreItems)
        {
            bool isSelected = string.Equals(_selectedGenreFilter, genre.FilterKey, StringComparison.OrdinalIgnoreCase);

            Label lblGenreItem = new Label
            {
                Text = genre.Display,
                Font = new Font("Segoe UI", 9.5F, isSelected ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = isSelected ? Color.FromArgb(229, 9, 20) : Color.FromArgb(180, 185, 205),
                AutoSize = true,
                Margin = new Padding(4, 5, 4, 5),
                Cursor = Cursors.Hand
            };

            lblGenreItem.MouseEnter += (s, e) => lblGenreItem.ForeColor = Color.FromArgb(229, 9, 20);
            lblGenreItem.MouseLeave += (s, e) =>
            {
                bool active = string.Equals(_selectedGenreFilter, genre.FilterKey, StringComparison.OrdinalIgnoreCase);
                lblGenreItem.ForeColor = active ? Color.FromArgb(229, 9, 20) : Color.FromArgb(180, 185, 205);
            };

            lblGenreItem.Click += (s, e) =>
            {
                _selectedGenreFilter = genre.FilterKey;
                BuildDynamicGenresList();
                ApplyFilter();
            };

            flpGenresList.Controls.Add(lblGenreItem);
        }

        flpGenresList.ResumeLayout();
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
                    ? "🎬 Hiện chưa có phim nào trong hệ thống.\nVui lòng đăng nhập tài khoản Admin -> Quản lý Phim để thêm phim mới."
                    : "🔍 Không tìm thấy phim phù hợp với điều kiện lọc.",
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.FromArgb(160, 165, 190),
                AutoSize = true,
                Padding = new Padding(4, 24, 0, 0)
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