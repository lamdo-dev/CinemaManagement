using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Models;
using CinemaManagement.Models.Enums;
using CinemaManagement.Repositories;
using CinemaManagement.Services;

namespace CinemaManagement.UserControls.Customer;

public partial class ShowtimeUC : UserControl
{
    // =========================================
    // 1. REPOSITORY VÀ DỮ LIỆU
    // =========================================

    private readonly MovieRepository _movieRepository = new();
    private readonly RoomRepository _roomRepository = new();
    private readonly ShowtimeRepository _showtimeRepository = new();
    private readonly PricingService _pricingService = new();

    private List<Movie> _movies = new();
    private List<Room> _rooms = new();
    private List<Showtime> _showtimes = new();
    private List<PricingRule> _pricingRules = new();

    // Bảng màu Dark Cinema & Gold Accent
    private readonly Color ColorBg = Color.FromArgb(20, 20, 28);
    private readonly Color ColorCard = Color.FromArgb(28, 29, 38);
    private readonly Color ColorGold = Color.FromArgb(195, 155, 80);
    private readonly Color ColorTextMuted = Color.FromArgb(150, 150, 170);

    // Controls UI
    private TableLayoutPanel tlpHeader = null!;
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
        AutoScaleMode = AutoScaleMode.Dpi;

        InitializeComponents();
        LoadDatabaseData();
        LoadShowtimes();
    }

    public ShowtimeUC(Movie movie) : this()
    {
        SetMovie(movie);
    }

    // =========================================
    // 3. KHỞI TẠO GIAO DIỆN RESPONSIVE
    // =========================================

    private void InitializeComponents()
    {
        Dock = DockStyle.Fill;
        BackColor = ColorBg;

        // HEADER TỰ GIÃN CHIỀU CAO THEO DPI
        tlpHeader = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.FromArgb(16, 16, 24),
            Padding = new Padding(20, 12, 20, 12),
            Margin = new Padding(0)
        };
        tlpHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpHeader.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tlpHeader.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Hàng 1: Nút Quay lại + Thanh tiến trình Stepper
        var tlpTopRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 10)
        };
        tlpTopRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tlpTopRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpTopRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        btnBack = new Button
        {
            Text = "← Quay lại",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(35, 36, 50),
            FlatStyle = FlatStyle.Flat,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(12, 6, 12, 6),
            Margin = new Padding(0, 0, 16, 0),
            Cursor = Cursors.Hand
        };
        btnBack.FlatAppearance.BorderSize = 0;
        btnBack.MouseEnter += (s, e) => btnBack.BackColor = Color.FromArgb(52, 54, 74);
        btnBack.MouseLeave += (s, e) => btnBack.BackColor = Color.FromArgb(35, 36, 50);
        btnBack.Click += (s, e) => OnBackClicked?.Invoke(this, EventArgs.Empty);

        // Thanh tiến trình dùng FlowLayoutPanel để các bước nối tiếp nhau, không bao giờ đè/cắt chữ
        var flpStepper = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Anchor = AnchorStyles.None,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };

        flpStepper.Controls.AddRange(new Control[]
        {
            CreateStepLabel("①  CHỌN SUẤT CHIẾU", true),
            CreateDividerLabel("──────"),
            CreateStepLabel("②  CHỌN GHẾ", false),
            CreateDividerLabel("──────"),
            CreateStepLabel("③  THANH TOÁN", false)
        });

        tlpTopRow.Controls.Add(btnBack, 0, 0);
        tlpTopRow.Controls.Add(flpStepper, 1, 0);

        // Hàng 2: Thanh chọn ngày chiếu tự giãn
        flpDateSelector = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };

        GenerateDateButtons();

        tlpHeader.Controls.Add(tlpTopRow, 0, 0);
        tlpHeader.Controls.Add(flpDateSelector, 0, 1);

        // Danh sách suất chiếu
        flpShowtimeList = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            AutoScroll = true,
            WrapContents = false,
            FlowDirection = FlowDirection.TopDown,
            BackColor = ColorBg
        };

        flpShowtimeList.SizeChanged += (s, e) =>
        {
            int targetWidth = Math.Max(420, flpShowtimeList.ClientSize.Width - 44);
            foreach (Control ctrl in flpShowtimeList.Controls)
            {
                if (ctrl is Panel card)
                {
                    card.MinimumSize = new Size(targetWidth, 0);
                    card.MaximumSize = new Size(targetWidth, 0);
                    card.Width = targetWidth;
                }
            }
        };

        Controls.Add(flpShowtimeList);
        Controls.Add(tlpHeader);
    }

    // =========================================
    // 4. TẠO LABEL STEPPER
    // =========================================

    private Label CreateStepLabel(string text, bool isActive)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = isActive ? ColorGold : ColorTextMuted,
            AutoSize = true,
            Margin = new Padding(4, 6, 4, 6)
        };
    }

    private Label CreateDividerLabel(string text)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(70, 72, 95),
            AutoSize = true,
            Margin = new Padding(4, 6, 4, 6)
        };
    }

    // =========================================
    // 5. TẠO CÁC NÚT CHỌN NGÀY
    // =========================================

    private void GenerateDateButtons()
    {
        flpDateSelector.SuspendLayout();
        flpDateSelector.Controls.Clear();

        for (int i = 0; i < 7; i++)
        {
            DateTime date = DateTime.Today.AddDays(i);
            bool isSelected = date.Date == selectedDate.Date;

            Button btnDate = new Button
            {
                Text = i == 0
                    ? "HÔM NAY"
                    : date.ToString("ddd dd/MM").ToUpper(),

                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = isSelected ? Color.Black : Color.White,
                BackColor = isSelected ? ColorGold : Color.FromArgb(35, 36, 50),
                FlatStyle = FlatStyle.Flat,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(14, 7, 14, 7),
                Margin = new Padding(0, 0, 8, 6),
                Cursor = Cursors.Hand,
                Tag = date
            };

            btnDate.FlatAppearance.BorderSize = 0;
            btnDate.Click += BtnDate_Click;

            flpDateSelector.Controls.Add(btnDate);
        }

        flpDateSelector.ResumeLayout();
    }

    private void BtnDate_Click(object? sender, EventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not DateTime date)
            return;

        selectedDate = date;
        GenerateDateButtons();
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
            _pricingRules = _pricingService.GetAll();
        }
        catch (Exception ex)
        {
            _movies.Clear();
            _rooms.Clear();
            _showtimes.Clear();
            _pricingRules.Clear();

            MessageBox.Show(
                "Không thể tải lịch chiếu từ SQLite.\n\n" + ex.Message,
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

            if (MovieData != null && MovieData.Status == MovieStatus.Ended)
            {
                Label lblEnded = new Label
                {
                    Text = $"Phim {MovieData.Title} đã kết thúc đợt chiếu.",
                    ForeColor = ColorTextMuted,
                    Font = new Font("Segoe UI", 11.5F),
                    AutoSize = true,
                    Margin = new Padding(8, 16, 8, 16)
                };

                flpShowtimeList.Controls.Add(lblEnded);
                return;
            }

            DateTime now = DateTime.Now;

            IEnumerable<Showtime> query = _showtimes
                .Where(s => s.StartTime.Date == selectedDate.Date &&
                            s.StartTime > now);

            if (MovieData != null)
            {
                query = query.Where(s => s.MovieId == MovieData.Id);
            }

            List<Showtime> result = query
                .OrderBy(s => s.StartTime)
                .ToList();

            if (result.Count == 0)
            {
                string emptyMessage = selectedDate.Date == DateTime.Today
                    ? (MovieData == null
                        ? "Không còn suất chiếu sắp diễn ra trong ngày hôm nay."
                        : $"Phim {MovieData.Title} không còn suất chiếu sắp diễn ra trong ngày hôm nay.")
                    : (MovieData == null
                        ? "Không có suất chiếu trong ngày này."
                        : $"Phim {MovieData.Title} không có suất chiếu trong ngày này.");

                Label lblEmpty = new Label
                {
                    Text = emptyMessage,
                    ForeColor = ColorTextMuted,
                    Font = new Font("Segoe UI", 11.5F),
                    AutoSize = true,
                    Margin = new Padding(8, 16, 8, 16)
                };

                flpShowtimeList.Controls.Add(lblEmpty);
                return;
            }

            int cardWidth = Math.Max(420, flpShowtimeList.ClientSize.Width - 44);

            foreach (Showtime showtime in result)
            {
                Movie? movie = _movies.FirstOrDefault(m => m.Id == showtime.MovieId);
                Room? room = _rooms.FirstOrDefault(r => r.Id == showtime.RoomId);

                if (movie == null || movie.Status == MovieStatus.Ended)
                    continue;

                Panel card = CreateShowtimeCard(showtime, movie, room, cardWidth);
                flpShowtimeList.Controls.Add(card);
            }

            if (flpShowtimeList.Controls.Count == 0)
            {
                Label lblEmpty = new Label
                {
                    Text = "Không tìm thấy thông tin phim cho các suất chiếu.",
                    ForeColor = ColorTextMuted,
                    Font = new Font("Segoe UI", 11F),
                    AutoSize = true,
                    Margin = new Padding(8, 16, 8, 16)
                };

                flpShowtimeList.Controls.Add(lblEmpty);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Không thể hiển thị lịch chiếu.\n\n" + ex.Message,
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
    // 10. TẠO THẺ SUẤT CHIẾU RESPONSIVE
    // =========================================

    private Panel CreateShowtimeCard(
        Showtime showtime,
        Movie movie,
        Room? room,
        int cardWidth)
    {
        Panel card = new Panel
        {
            Width = cardWidth,
            MinimumSize = new Size(cardWidth, 0),
            MaximumSize = new Size(cardWidth, 0),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = ColorCard,
            Padding = new Padding(16),
            Margin = new Padding(0, 0, 0, 14)
        };

        var tlpCard = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        tlpCard.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tlpCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpCard.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Cột 1: Poster phim
        PictureBox picPoster = new PictureBox
        {
            Size = new Size(84, 124),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(45, 45, 60),
            Margin = new Padding(0, 0, 18, 0)
        };

        picPoster.Paint += (s, e) =>
        {
            if (picPoster.Image != null)
                return;

            using var font = new Font("Segoe UI Emoji", 20F);
            using var brush = new SolidBrush(Color.FromArgb(140, 140, 165));
            using var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            e.Graphics.DrawString("🎬", font, brush, picPoster.ClientRectangle, sf);
        };

        if (!string.IsNullOrWhiteSpace(movie.PosterUrl) &&
            System.IO.File.Exists(movie.PosterUrl))
        {
            try
            {
                using var stream = new System.IO.FileStream(
                    movie.PosterUrl,
                    System.IO.FileMode.Open,
                    System.IO.FileAccess.Read,
                    System.IO.FileShare.ReadWrite);
                using var image = Image.FromStream(stream);
                picPoster.Image = new Bitmap(image);
            }
            catch
            {
                // Giữ biểu tượng mặc định nếu ảnh lỗi
            }
        }

        // Cột 2: Thông tin phim & nút chọn suất xếp dọc tự giãn
        var tlpDetails = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        tlpDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        for (int i = 0; i < 4; i++)
        {
            tlpDetails.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        Label lblTitle = new Label
        {
            Text = movie.Title,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4)
        };

        decimal displayPrice = _pricingService.CalculatePriceBySeatType(
            showtime,
            SeatType.Standard,
            1.0,
            _pricingRules);

        Label lblInfo = new Label
        {
            Text =
                $"{room?.Name ?? $"Phòng {showtime.RoomId}"}" +
                $"   •   {showtime.Format}" +
                $"   •   Giá vé từ: {displayPrice:N0} đ",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = ColorGold,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4)
        };

        Label lblTime = new Label
        {
            Text = $"{showtime.StartTime:HH:mm} - {showtime.EndTime:HH:mm}",
            Font = new Font("Segoe UI", 9F),
            ForeColor = ColorTextMuted,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10)
        };

        Button btnTime = new Button
        {
            Text = showtime.StartTime.ToString("HH:mm"),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(42, 43, 58),
            FlatStyle = FlatStyle.Flat,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(18, 7, 18, 7),
            Margin = new Padding(0),
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

        btnTime.Click += (s, e) =>
        {
            if (showtime.StartTime <= DateTime.Now)
            {
                MessageBox.Show(
                    "Suất chiếu này vừa bắt đầu hoặc đã qua giờ chiếu. Vui lòng chọn suất chiếu khác.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                RefreshShowtimes();
                return;
            }

            MovieData = movie;
            OnShowtimeSelected?.Invoke(this, showtime);
        };

        tlpDetails.Controls.Add(lblTitle, 0, 0);
        tlpDetails.Controls.Add(lblInfo, 0, 1);
        tlpDetails.Controls.Add(lblTime, 0, 2);
        tlpDetails.Controls.Add(btnTime, 0, 3);

        tlpCard.Controls.Add(picPoster, 0, 0);
        tlpCard.Controls.Add(tlpDetails, 1, 0);

        card.Controls.Add(tlpCard);
        return card;
    }
}