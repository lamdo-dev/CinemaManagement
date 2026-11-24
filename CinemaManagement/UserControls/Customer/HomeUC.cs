

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using CinemaManagement.Models;
using CinemaManagement.Models.Enums;
using CinemaManagement.Services;
using CinemaManagement.UserControls.Common;

namespace CinemaManagement.UserControls.Customer
{
    public partial class HomeUC : UserControl
    {
        // =========================================================
        // 1. FIELDS
        // =========================================================

        private readonly TicketService _ticketService = new();

        private Panel pnlSidebar = null!;
        private Panel pnlMainContent = null!;
        private FlowLayoutPanel flpGenresList = null!;
        private Panel pnlHeader = null!;
        private Panel pnlSearchBox = null!;

        private FlowLayoutPanel flpMovieGrid = null!;

        private TextBox txtSearch = null!;
        private Label lblTitle = null!;
        private Label lblSubTitle = null!;

        private Button btnWatchNow = null!;
        private Button btnComingSoon = null!;
        private Button btnBrowseAll = null!;
        private Button btnRefresh = null!;

        private List<Movie> _allMovies = new();

        private string? _selectedGenreFilter;
        private MovieStatus? _selectedStatusFilter = MovieStatus.Showing;

        private bool _layoutReady;

        // =========================================================
        // 2. EVENTS
        // =========================================================

        // Chuyển sang màn hình chi tiết phim.
        public event EventHandler<Movie>? OnMovieClicked;

        // Chuyển sang màn hình chọn suất chiếu.
        public event EventHandler<Movie>? OnBookNowClicked;

        // =========================================================
        // 3. CONSTRUCTOR
        // =========================================================

        public HomeUC()
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            InitializeComponents();

            _layoutReady = true;

            ApplyResponsiveLayout();

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

        // =========================================================
        // 4. INITIALIZE COMPONENTS
        // =========================================================

        private void InitializeComponents()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(24, 24, 36);

            // ================= SIDEBAR =================
            pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 230,
                MinimumSize = new Size(180, 0),
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
            tlpSidebar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpSidebar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpSidebar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpSidebar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpSidebar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpSidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var lblLibraryHeader = new Label
            {
                Text = "DANH MỤC PHIM",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 42,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 0, 8)
            };

            btnWatchNow = CreateSidebarButton("▶  Đang chiếu", true);
            btnComingSoon = CreateSidebarButton("◷  Sắp chiếu", false);
            btnBrowseAll = CreateSidebarButton("☰  Tất cả phim", false);

            btnWatchNow.Click += (sender, e) =>
            {
                _selectedStatusFilter = MovieStatus.Showing;
                _selectedGenreFilter = null;
                lblTitle.Text = "Phim Đang Chiếu";
                lblSubTitle.Text = "Chọn phim bạn yêu thích hoặc nhấn Đặt vé ngay để chọn suất chiếu.";
                SetActiveSidebarButton(btnWatchNow);
                BuildDynamicGenresList();
                ApplyFilter();
            };

            btnComingSoon.Click += (sender, e) =>
            {
                _selectedStatusFilter = MovieStatus.ComingSoon;
                _selectedGenreFilter = null;
                lblTitle.Text = "Phim Sắp Chiếu";
                lblSubTitle.Text = "Những bộ phim chuẩn bị ra mắt tại rạp trong thời gian tới.";
                SetActiveSidebarButton(btnComingSoon);
                BuildDynamicGenresList();
                ApplyFilter();
            };

            btnBrowseAll.Click += (sender, e) =>
            {
                _selectedStatusFilter = null;
                _selectedGenreFilter = null;
                lblTitle.Text = "Tất Cả Phim Tại Rạp";
                lblSubTitle.Text = "Toàn bộ danh sách phim đang chiếu và sắp khởi chiếu.";
                SetActiveSidebarButton(btnBrowseAll);
                BuildDynamicGenresList();
                ApplyFilter();
            };

            var lblGenresHeader = new Label
            {
                Text = "THỂ LOẠI",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(150, 150, 170),
                Dock = DockStyle.Top,
                Height = 38,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 12, 0, 4)
            };

            flpGenresList = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0, 4, 0, 4)
            };

            tlpSidebar.Controls.Add(lblLibraryHeader, 0, 0);
            tlpSidebar.Controls.Add(btnWatchNow, 0, 1);
            tlpSidebar.Controls.Add(btnComingSoon, 0, 2);
            tlpSidebar.Controls.Add(btnBrowseAll, 0, 3);
            tlpSidebar.Controls.Add(lblGenresHeader, 0, 4);
            tlpSidebar.Controls.Add(flpGenresList, 0, 5);
            pnlSidebar.Controls.Add(tlpSidebar);

            // ================= MAIN CONTENT =================
            pnlMainContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(25),
                BackColor = Color.FromArgb(24, 24, 36)
            };

            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 92,
                BackColor = Color.Transparent
            };

            lblTitle = new Label
            {
                Text = "Phim Đang Chiếu",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(0, 0),
                MaximumSize = new Size(600, 0)
            };

            lblSubTitle = new Label
            {
                Text = "Chọn phim bạn yêu thích hoặc nhấn Đặt vé ngay để chọn suất chiếu.",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(160, 165, 185),
                AutoSize = true,
                Location = new Point(2, 43),
                MaximumSize = new Size(600, 0)
            };

            pnlSearchBox = new Panel
            {
                Dock = DockStyle.Right,
                Width = 390,
                Height = 60,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 0, 0)
            };

            var tlpSearchRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            tlpSearchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpSearchRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpSearchRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                PlaceholderText = "🔍 Tìm tên phim, đạo diễn...",
                Margin = new Padding(0, 4, 8, 0)
            };
            txtSearch.TextChanged += (sender, e) => ApplyFilter();

            btnRefresh = new Button
            {
                Text = "↻ Làm mới",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(40, 42, 60),
                FlatStyle = FlatStyle.Flat,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(12, 5, 12, 5),
                Margin = new Padding(0),
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.MouseEnter += (sender, e) => btnRefresh.BackColor = Color.FromArgb(56, 59, 82);
            btnRefresh.MouseLeave += (sender, e) => btnRefresh.BackColor = Color.FromArgb(40, 42, 60);
            btnRefresh.Click += (sender, e) => RefreshMovies();

            tlpSearchRow.Controls.Add(txtSearch, 0, 0);
            tlpSearchRow.Controls.Add(btnRefresh, 1, 0);
            pnlSearchBox.Controls.Add(tlpSearchRow);

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubTitle);
            pnlHeader.Controls.Add(pnlSearchBox);

            flpMovieGrid = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
                WrapContents = true,
                Padding = new Padding(0, 12, 0, 0),
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0)
            };

            pnlMainContent.Controls.Add(flpMovieGrid);
            pnlMainContent.Controls.Add(pnlHeader);

            Controls.Add(pnlMainContent);
            Controls.Add(pnlSidebar);

            SizeChanged += HomeUC_SizeChanged;
            pnlMainContent.SizeChanged += HomeUC_SizeChanged;
        }

        // =========================================================
        // 5. RESPONSIVE LAYOUT
        // =========================================================

        private void HomeUC_SizeChanged(object? sender, EventArgs e)
        {
            if (!_layoutReady || IsDisposed)
                return;

            ApplyResponsiveLayout();
        }

        private void ApplyResponsiveLayout()
        {
            if (pnlMainContent == null ||
                pnlSidebar == null ||
                pnlHeader == null ||
                pnlSearchBox == null)
            {
                return;
            }

            int availableWidth = ClientSize.Width;

            // ---------------- SIDEBAR ----------------

            int sidebarWidth = (int)(availableWidth * 0.19f);

            sidebarWidth = Math.Max(
                180,
                Math.Min(220, sidebarWidth));

            pnlSidebar.Width = sidebarWidth;

            // ---------------- MAIN CONTENT ----------------

            bool isNarrow =
                pnlMainContent.ClientSize.Width < 900;

            if (isNarrow)
            {
                pnlMainContent.Padding = new Padding(15);

                // Header chuyển thành 2 hàng.
                pnlHeader.Height = 136;

                // Search chuyển xuống dưới tiêu đề.
                pnlSearchBox.Dock = DockStyle.None;
                pnlSearchBox.Padding = Padding.Empty;
                pnlSearchBox.Location = new Point(0, 82);
                pnlSearchBox.Width = Math.Max(260, pnlHeader.ClientSize.Width);
                pnlSearchBox.Height = 48;

                // Giới hạn chiều rộng tiêu đề.
                lblTitle.MaximumSize = new Size(
                    Math.Max(200, pnlHeader.ClientSize.Width - 10),
                    0);

                lblSubTitle.MaximumSize = new Size(
                    Math.Max(200, pnlHeader.ClientSize.Width - 10),
                    0);
            }
            else
            {
                pnlMainContent.Padding = new Padding(25);

                pnlHeader.Height = 92;

                // Search trở lại bên phải, tự co giãn theo nút AutoSize.
                int searchBoxWidth = Math.Clamp(
                    (int)(pnlHeader.ClientSize.Width * 0.42f),
                    390,
                    460);

                pnlSearchBox.Dock = DockStyle.Right;
                pnlSearchBox.Padding = new Padding(0, 8, 0, 0);
                pnlSearchBox.Location = Point.Empty;
                pnlSearchBox.Width = searchBoxWidth;
                pnlSearchBox.Height = 60;

                lblTitle.MaximumSize = new Size(
                    Math.Max(200, pnlHeader.ClientSize.Width - searchBoxWidth - 20),
                    0);

                lblSubTitle.MaximumSize = new Size(
                    Math.Max(200, pnlHeader.ClientSize.Width - searchBoxWidth - 20),
                    0);
            }

            pnlHeader.PerformLayout();
            pnlMainContent.PerformLayout();
        }

        // =========================================================
        // 6. SIDEBAR BUTTON
        // =========================================================

        private Button CreateSidebarButton(string text, bool isActive)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = isActive ? Color.White : Color.FromArgb(165, 170, 190),
                BackColor = isActive ? Color.FromArgb(229, 9, 20) : Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Top,
                Height = 42,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Margin = new Padding(0, 3, 0, 3),
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
                    : Color.FromArgb(165, 170, 190);
            }
        }

        // =========================================================
        // 7. LOAD MOVIES
        // =========================================================

        public void RefreshMovies()
        {
            try
            {
                _allMovies = _ticketService.GetCustomerMovies();

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

        // =========================================================
        // 8. DYNAMIC GENRES
        // =========================================================

        private void BuildDynamicGenresList()
        {
            flpGenresList.SuspendLayout();
            flpGenresList.Controls.Clear();

            var genres = _allMovies
                .SelectMany(movie =>
                    (movie.Genre ?? string.Empty).Split(
                        new[] { ',', '/', '|' },
                        StringSplitOptions.RemoveEmptyEntries))
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item)
                .ToList();

            var genreItems = new List<(string Display, string? FilterKey)>
            {
                ("Tất cả thể loại", null)
            };

            genreItems.AddRange(genres.Select(item => (item, (string?)item)));

            foreach (var genreItem in genreItems)
            {
                bool isSelected = string.Equals(
                    _selectedGenreFilter,
                    genreItem.FilterKey,
                    StringComparison.OrdinalIgnoreCase);

                var lblGenreItem = new Label
                {
                    Text = genreItem.Display,
                    Font = new Font(
                        "Segoe UI",
                        9.5F,
                        isSelected ? FontStyle.Bold : FontStyle.Regular),
                    ForeColor = isSelected
                        ? Color.FromArgb(229, 9, 20)
                        : Color.FromArgb(180, 185, 205),
                    AutoSize = true,
                    Margin = new Padding(8, 6, 4, 6),
                    Cursor = Cursors.Hand
                };

                lblGenreItem.MouseEnter += (sender, e) =>
                {
                    lblGenreItem.ForeColor = Color.FromArgb(229, 9, 20);
                };

                lblGenreItem.MouseLeave += (sender, e) =>
                {
                    bool active = string.Equals(
                        _selectedGenreFilter,
                        genreItem.FilterKey,
                        StringComparison.OrdinalIgnoreCase);

                    lblGenreItem.ForeColor = active
                        ? Color.FromArgb(229, 9, 20)
                        : Color.FromArgb(180, 185, 205);
                };

                lblGenreItem.Click += (sender, e) =>
                {
                    _selectedGenreFilter = genreItem.FilterKey;
                    BuildDynamicGenresList();
                    ApplyFilter();
                };

                flpGenresList.Controls.Add(lblGenreItem);
            }

            flpGenresList.ResumeLayout();
        }

        // =========================================================
        // 9. DISPLAY MOVIES
        // =========================================================

        private void DisplayMovies(List<Movie> movies)
        {
            flpMovieGrid.SuspendLayout();

            // Giải phóng các thẻ phim cũ.
            foreach (Control control in flpMovieGrid.Controls)
            {
                control.Dispose();
            }

            flpMovieGrid.Controls.Clear();

            if (movies.Count == 0)
            {
                Label lblEmpty = new Label
                {
                    Text = _allMovies.Count == 0
                        ? "🎬 Hiện chưa có phim nào trong hệ thống.\nVui lòng đăng nhập tài khoản Admin và thêm phim mới."
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

            foreach (Movie movie in movies)
            {
                MovieCardUC card = new MovieCardUC(movie);

                // Click thẻ phim: mở chi tiết phim.
                card.MovieSelected += (sender, selectedMovie) =>
                {
                    OnMovieClicked?.Invoke(this, selectedMovie);
                };

                // Cả phim đang chiếu và sắp chiếu đều cho phép chọn suất đặt vé.
                card.OnBookNowClicked += (sender, selectedMovie) =>
                {
                    if (selectedMovie.Status == MovieStatus.Ended)
                        return;

                    OnBookNowClicked?.Invoke(this, selectedMovie);
                };

                flpMovieGrid.Controls.Add(card);
            }

            flpMovieGrid.ResumeLayout();
        }

        // =========================================================
        // 10. FILTER MOVIES
        // =========================================================

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
