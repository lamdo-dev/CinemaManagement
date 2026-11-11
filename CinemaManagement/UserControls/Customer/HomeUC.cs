namespace CinemaManagement.UserControls.Customer;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Models;
using CinemaManagement.UserControls.Common;

public partial class HomeUC : UserControl
{
    private Panel pnlSidebar = null!;
    private Panel pnlMainContent = null!;
    private FlowLayoutPanel flpMovieGrid = null!;
    private TextBox txtSearch = null!;

    private List<Movie> _allMovies = new();
    private string? _selectedGenreFilter;

    // Sự kiện chuyển màn hình sang Chi tiết Phim (MovieDetailUC)
    public event EventHandler<Movie>? OnMovieClicked;

    // Sự kiện chuyển thẳng sang Chọn Suất Chiếu (ShowtimeUC) khi bấm "Đặt vé ngay"
    public event EventHandler<Movie>? OnBookNowClicked;

    public HomeUC()
    {
        InitializeComponents();
        LoadDummyData(); // Tải dữ liệu phim mẫu hiển thị demo
    }

    private void InitializeComponents()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.FromArgb(24, 24, 36);

        // -------------------------------------------------------------
        // 1. SIDEBAR BÊN TRÁI (DANH MỤC & THỂ LOẠI)
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
            ApplyFilter();
        };

        Label lblGenresHeader = new Label
        {
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
        pnlSidebar.Controls.Add(btnWatchNow);
        pnlSidebar.Controls.Add(lblLibraryHeader);

        // -------------------------------------------------------------
        // 2. KHU VỰC NỘI DUNG CHÍNH BÊN PHẢI (MAIN CONTENT)
        // -------------------------------------------------------------
        pnlMainContent = new Panel
        {
            Dock = DockStyle.Fill,
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
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(0, 0),
            AutoSize = true
        };

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
        flpMovieGrid = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Transparent
        };

        pnlMainContent.Controls.Add(flpMovieGrid);
        pnlMainContent.Controls.Add(pnlHeader);

        // Add cả 2 Panel vào UserControl
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
            BackColor = isActive ? Color.FromArgb(229, 9, 20) : Color.Transparent, // Đỏ CinemaAccent
            FlatStyle = FlatStyle.Flat,
            Dock = DockStyle.Top,
            Height = 40,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 5, 0, 5),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    // Tải dữ liệu giả lập để hiển thị thử giao diện
    private void LoadDummyData()
    {
        _allMovies = new List<Movie>
        {
            new Movie
            {
                Id = 1,
                Title = "Die Hard",
                Genre = "Action",
                Director = "John McTiernan",
                DurationMinutes = 132,
                ReleaseDate = new DateTime(1988, 7, 15),
                Description = "Sĩ quan cảnh sát New York John McClane một mình đối đầu với nhóm khủng bố chiếm giữ tòa nhà Nakatomi Plaza vào đêm Giáng Sinh."
            },
            new Movie
            {
                Id = 2,
                Title = "Grease",
                Genre = "Musical",
                Director = "Randal Kleiser",
                DurationMinutes = 110,
                ReleaseDate = new DateTime(1978, 6, 16),
                Description = "Chuyện tình mùa hè đầy âm nhạc và vũ đạo sôi động giữa chàng trai Danny Zuko và cô nàng Sandy Olsson."
            },
            new Movie
            {
                Id = 3,
                Title = "La La Land",
                Genre = "Romance",
                Director = "Damien Chazelle",
                DurationMinutes = 128,
                ReleaseDate = new DateTime(2016, 12, 9),
                Description = "Một nghệ sĩ dương cầm nhạc Jazz và một nữ diễn viên đầy tham vọng đem lòng yêu nhau khi cùng theo đuổi ước mơ tại Los Angeles."
            },
            new Movie
            {
                Id = 4,
                Title = "Avatar: The Way of Water",
                Genre = "Sci-Fi",
                Director = "James Cameron",
                DurationMinutes = 192,
                ReleaseDate = new DateTime(2022, 12, 16),
                Description = "Jake Sully và Ney'tiri phải rời bỏ quê hương và khám phá các vùng biển của Pandora để bảo vệ gia đình trước mối đe dọa mới."
            },
            new Movie
            {
                Id = 5,
                Title = "Oppenheimer",
                Genre = "Drama",
                Director = "Christopher Nolan",
                DurationMinutes = 180,
                ReleaseDate = new DateTime(2023, 7, 21),
                Description = "Câu chuyện kịch tính về nhà vật lý J. Robert Oppenheimer và vai trò dẫn dắt Dự án Manhattan chế tạo bom nguyên tử."
            },
            new Movie
            {
                Id = 6,
                Title = "Spider-Man: Across The Spider-Verse",
                Genre = "Action",
                Director = "Joaquim Dos Santos",
                DurationMinutes = 140,
                ReleaseDate = new DateTime(2023, 6, 2),
                Description = "Miles Morales du hành qua Đa vũ trụ và gặp gỡ Đội quân Người Nhện bảo vệ sự tồn tại của mọi thế giới."
            }
        };

        DisplayMovies(_allMovies);
    }

    private void DisplayMovies(List<Movie> movies)
    {
        flpMovieGrid.SuspendLayout();
        flpMovieGrid.Controls.Clear();

        foreach (var movie in movies)
        {
            MovieCardUC card = new MovieCardUC(movie);

            // Sự kiện click vào thẻ phim -> xem chi tiết phim (MovieDetailUC)
            card.MovieSelected += (s, selectedMovie) =>
            {
                OnMovieClicked?.Invoke(this, selectedMovie);
            };

            // Sự kiện click nút "Đặt vé ngay" -> sang thẳng chọn suất chiếu (ShowtimeUC)
            card.OnBookNowClicked += (s, selectedMovie) =>
            {
                OnBookNowClicked?.Invoke(this, selectedMovie);
            };

            flpMovieGrid.Controls.Add(card);
        }

        flpMovieGrid.ResumeLayout();
    }

    private void ApplyFilter()
    {
        string keyword = txtSearch.Text.Trim();
        IEnumerable<Movie> filtered = _allMovies;

        if (!string.IsNullOrEmpty(_selectedGenreFilter))
        {
            filtered = filtered.Where(m => m.Genre.Contains(_selectedGenreFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(keyword))
        {
            filtered = filtered.Where(m =>
                m.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                m.Genre.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        DisplayMovies(filtered.ToList());
    }

    private void TxtSearch_TextChanged(object? sender, EventArgs e)
    {
        ApplyFilter();
    }
}
