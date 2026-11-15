
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Models;
using CinemaManagement.Repositories;

namespace CinemaManagement.UserControls.Customer;

public partial class ShowtimeUC : UserControl
{
    // =========================================
    // 1. REPOSITORY VÀ DỮ LIỆU
    // =========================================

    private readonly MovieRepository _movieRepository = new();
    private readonly RoomRepository _roomRepository = new();
    private readonly ShowtimeRepository _showtimeRepository = new();

    private List<Movie> _movies = new();
    private List<Room> _rooms = new();
    private List<Showtime> _showtimes = new();

    // Bảng màu Dark Cinema & Gold Accent
    private readonly Color ColorBg = Color.FromArgb(20, 20, 28);
    private readonly Color ColorCard = Color.FromArgb(28, 29, 38);
    private readonly Color ColorGold = Color.FromArgb(195, 155, 80);
    private readonly Color ColorTextMuted = Color.FromArgb(150, 150, 170);

    // Controls UI
    private Panel pnlHeader = null!;
    private Button btnBack = null!;
    private FlowLayoutPanel flpDateSelector = null!;
    private FlowLayoutPanel flpShowtimeList = null!;

    private DateTime selectedDate = DateTime.Today;

    public Movie? MovieData { get; private set; }

    // Sự kiện chọn suất chiếu để chuyển sang SeatSelectionUC
    public event EventHandler<Showtime>? OnShowtimeSelected;

    // Sự kiện quay lại
    public event EventHandler? OnBackClicked;

    // =========================================
    // 2. CONSTRUCTOR
    // =========================================

    public ShowtimeUC()
    {
        InitializeComponents();

        LoadDatabaseData();
        LoadShowtimes();
    }

    public ShowtimeUC(Movie movie) : this()
    {
        SetMovie(movie);
    }

    // =========================================
    // 3. KHỞI TẠO GIAO DIỆN
    // =========================================

    private void InitializeComponents()
    {
        Dock = DockStyle.Fill;
        BackColor = ColorBg;
        AutoScroll = true;

        // HEADER
        pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 110,
            BackColor = Color.FromArgb(16, 16, 24)
        };

        btnBack = new Button
        {
            Text = "← Quay lại",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(35, 36, 50),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(100, 32),
            Location = new Point(20, 15),
            Cursor = Cursors.Hand
        };

        btnBack.FlatAppearance.BorderSize = 0;
        btnBack.Click += (s, e) =>
            OnBackClicked?.Invoke(this, EventArgs.Empty);

        // Thanh tiến trình
        Panel pnlStepper = new Panel
        {
            Size = new Size(600, 50),
            Location = new Point(280, 15),
            BackColor = Color.Transparent
        };

        Label lblStep1 = CreateStepLabel(
            "①  CHỌN SUẤT CHIẾU", true, 0);

        Label lblDivider1 = CreateDividerLabel(
            "─────────", 185);

        Label lblStep2 = CreateStepLabel(
            "②  CHỌN GHẾ", false, 275);

        Label lblDivider2 = CreateDividerLabel(
            "─────────", 400);

        Label lblStep3 = CreateStepLabel(
            "③  THANH TOÁN", false, 480);

        pnlStepper.Controls.AddRange(new Control[]
        {
            lblStep1,
            lblDivider1,
            lblStep2,
            lblDivider2,
            lblStep3
        });

        // Chọn ngày chiếu
        flpDateSelector = new FlowLayoutPanel
        {
            Location = new Point(20, 60),
            Size = new Size(1100, 45),
            AutoScroll = false,
            WrapContents = false
        };

        GenerateDateButtons();

        pnlHeader.Controls.Add(btnBack);
        pnlHeader.Controls.Add(pnlStepper);
        pnlHeader.Controls.Add(flpDateSelector);

        // Danh sách suất chiếu
        flpShowtimeList = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            AutoScroll = true,
            WrapContents = true,
            FlowDirection = FlowDirection.TopDown,
            BackColor = ColorBg
        };

        Controls.Add(flpShowtimeList);
        Controls.Add(pnlHeader);
    }

    // =========================================
    // 4. TẠO LABEL
    // =========================================

    private Label CreateStepLabel(
        string text,
        bool isActive,
        int xLocation)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = isActive ? ColorGold : ColorTextMuted,
            Location = new Point(xLocation, 15),
            AutoSize = true
        };
    }

    private Label CreateDividerLabel(
        string text,
        int xLocation)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(60, 60, 80),
            Location = new Point(xLocation, 15),
            AutoSize = true
        };
    }

    // =========================================
    // 5. TẠO CÁC NÚT CHỌN NGÀY
    // =========================================

    private void GenerateDateButtons()
    {
        flpDateSelector.Controls.Clear();

        for (int i = 0; i < 7; i++)
        {
            DateTime date = DateTime.Today.AddDays(i);

            bool isSelected =
                date.Date == selectedDate.Date;

            Button btnDate = new Button
            {
                Text = i == 0
                    ? "HÔM NAY"
                    : date.ToString("ddd dd/MM").ToUpper(),

                Font = new Font("Segoe UI", 9, FontStyle.Bold),

                ForeColor = isSelected
                    ? Color.Black
                    : Color.White,

                BackColor = isSelected
                    ? ColorGold
                    : Color.FromArgb(35, 36, 50),

                FlatStyle = FlatStyle.Flat,
                Size = new Size(110, 36),
                Margin = new Padding(0, 0, 10, 0),
                Cursor = Cursors.Hand,
                Tag = date
            };

            btnDate.FlatAppearance.BorderSize = 0;
            btnDate.Click += BtnDate_Click;

            flpDateSelector.Controls.Add(btnDate);
        }
    }

    private void BtnDate_Click(object? sender, EventArgs e)
    {
        if (sender is not Button btn ||
            btn.Tag is not DateTime date)
            return;

        selectedDate = date;

        GenerateDateButtons();

        // Tải lại lịch chiếu theo ngày được chọn
        LoadShowtimes();
    }

    // =========================================
    // 6. TẢI DỮ LIỆU TỪ SQLITE
    // =========================================

    private void LoadDatabaseData()
    {
        try
        {
            _movies = _movieRepository.GetAll();
            _rooms = _roomRepository.GetAll();
            _showtimes = _showtimeRepository.GetAll();
        }
        catch (Exception ex)
        {
            _movies.Clear();
            _rooms.Clear();
            _showtimes.Clear();

            MessageBox.Show(
                "Không thể tải lịch chiếu từ SQLite.\n\n" +
                ex.Message,
                "Lỗi tải dữ liệu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    // =========================================
    // 7. CHỌN PHIM
    // =========================================

    public void SetMovie(Movie movie)
    {
        if (movie == null)
            return;

        MovieData = movie;

        // Đọc lại dữ liệu mới nhất từ database
        LoadDatabaseData();

        LoadShowtimes();
    }

    // =========================================
    // 8. LÀM MỚI LỊCH CHIẾU
    // =========================================

    public void RefreshShowtimes()
    {
        LoadDatabaseData();
        GenerateDateButtons();
        LoadShowtimes();
    }

    // =========================================
    // 9. TẢI VÀ HIỂN THỊ SUẤT CHIẾU
    // =========================================

    private void LoadShowtimes()
    {
        flpShowtimeList.SuspendLayout();

        try
        {
            flpShowtimeList.Controls.Clear();

            // Lọc lịch chiếu theo ngày
            IEnumerable<Showtime> query = _showtimes
                .Where(s =>
                    s.StartTime.Date == selectedDate.Date);

            // Nếu đã chọn phim thì chỉ lấy lịch của phim đó
            if (MovieData != null)
            {
                query = query.Where(s =>
                    s.MovieId == MovieData.Id);
            }

            List<Showtime> result = query
                .OrderBy(s => s.StartTime)
                .ToList();

            if (result.Count == 0)
            {
                Label lblEmpty = new Label
                {
                    Text = MovieData == null
                        ? "Không có suất chiếu trong ngày này."
                        : $"Phim {MovieData.Title} không có suất chiếu trong ngày này.",

                    ForeColor = ColorTextMuted,
                    Font = new Font("Segoe UI", 12),
                    AutoSize = true,
                    Margin = new Padding(20)
                };

                flpShowtimeList.Controls.Add(lblEmpty);
                return;
            }

            foreach (Showtime showtime in result)
            {
                Movie? movie = _movies.FirstOrDefault(
                    m => m.Id == showtime.MovieId);

                Room? room = _rooms.FirstOrDefault(
                    r => r.Id == showtime.RoomId);

                if (movie == null)
                    continue;

                Panel card = CreateShowtimeCard(
                    showtime,
                    movie,
                    room);

                flpShowtimeList.Controls.Add(card);
            }

            if (flpShowtimeList.Controls.Count == 0)
            {
                Label lblEmpty = new Label
                {
                    Text = "Không tìm thấy thông tin phim cho các suất chiếu.",
                    ForeColor = ColorTextMuted,
                    Font = new Font("Segoe UI", 11),
                    AutoSize = true,
                    Margin = new Padding(20)
                };

                flpShowtimeList.Controls.Add(lblEmpty);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Không thể hiển thị lịch chiếu.\n\n" +
                ex.Message,
                "Lỗi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            flpShowtimeList.ResumeLayout();
        }
    }

    // =========================================
    // 10. TẠO THẺ SUẤT CHIẾU
    // =========================================

    private Panel CreateShowtimeCard(
        Showtime showtime,
        Movie movie,
        Room? room)
    {
        Panel card = new Panel
        {
            Size = new Size(1000, 150),
            BackColor = ColorCard,
            Margin = new Padding(0, 0, 0, 15)
        };

        // Poster phim
        PictureBox picPoster = new PictureBox
        {
            Location = new Point(15, 15),
            Size = new Size(80, 120),
            SizeMode = PictureBoxSizeMode.StretchImage,
            BackColor = Color.FromArgb(45, 45, 60)
        };

        picPoster.Paint += (s, e) =>
        {
            if (picPoster.Image != null)
                return;

            using var font =
                new Font("Segoe UI Emoji", 20f);

            using var brush =
                new SolidBrush(Color.FromArgb(140, 140, 165));

            var sf = new StringFormat
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
        };

        // Hiển thị poster nếu đường dẫn tồn tại
        if (!string.IsNullOrWhiteSpace(movie.PosterUrl) &&
            System.IO.File.Exists(movie.PosterUrl))
        {
            try
            {
                using var image = Image.FromFile(movie.PosterUrl);
                picPoster.Image = new Bitmap(image);
            }
            catch
            {
                // Không có ảnh thì giữ biểu tượng mặc định
            }
        }

        // Tên phim
        Label lblTitle = new Label
        {
            Text = movie.Title,
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(115, 15),
            AutoSize = true
        };

        // Phòng chiếu và giá vé
        Label lblInfo = new Label
        {
            Text =
                $"{room?.Name ?? $"Phòng {showtime.RoomId}"}" +
                $"   •   {showtime.Format}" +
                $"   •   Giá vé: {showtime.BasePrice:N0} đ",

            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = ColorGold,
            Location = new Point(115, 45),
            AutoSize = true
        };

        // Thời gian bắt đầu và kết thúc
        Label lblTime = new Label
        {
            Text =
                $"{showtime.StartTime:HH:mm} - " +
                $"{showtime.EndTime:HH:mm}",

            Font = new Font("Segoe UI", 9),
            ForeColor = ColorTextMuted,
            Location = new Point(115, 72),
            AutoSize = true
        };

        // Nút chọn suất chiếu
        Button btnTime = new Button
        {
            Text = showtime.StartTime.ToString("HH:mm"),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(42, 43, 58),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(95, 38),
            Location = new Point(115, 100),
            Cursor = Cursors.Hand,
            Tag = showtime
        };

        btnTime.FlatAppearance.BorderSize = 0;

        btnTime.MouseEnter += (s, e) =>
        {
            btnTime.BackColor = ColorGold;
            btnTime.ForeColor = Color.Black;
        };

        btnTime.MouseLeave += (s, e) =>
        {
            btnTime.BackColor = Color.FromArgb(42, 43, 58);
            btnTime.ForeColor = Color.White;
        };

        // Chọn suất chiếu thật từ SQLite
        btnTime.Click += (s, e) =>
        {
            MovieData = movie;

            OnShowtimeSelected?.Invoke(
                this,
                showtime);
        };

        card.Controls.Add(picPoster);
        card.Controls.Add(lblTitle);
        card.Controls.Add(lblInfo);
        card.Controls.Add(lblTime);
        card.Controls.Add(btnTime);

        return card;
    }
}
