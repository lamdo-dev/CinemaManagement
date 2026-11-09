namespace CinemaManagement.UserControls.Customer;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Models;

public partial class ShowtimeUC : UserControl
{
    // Bảng màu Dark Cinema & Gold Accent từ ảnh mẫu
    private readonly Color ColorBg = Color.FromArgb(20, 20, 28);
    private readonly Color ColorCard = Color.FromArgb(28, 29, 38);
    private readonly Color ColorGold = Color.FromArgb(195, 155, 80);
    private readonly Color ColorTextMuted = Color.FromArgb(150, 150, 170);

    // Controls UI
    private Panel pnlHeader;
    private Button btnBack;
    private FlowLayoutPanel flpDateSelector;
    private FlowLayoutPanel flpShowtimeList;
    private DateTime selectedDate = DateTime.Today;

    public Movie? MovieData { get; private set; }

    // Sự kiện khi bấm vào 1 Suất chiếu để chuyển sang Sơ đồ ghế (SeatSelectionUC)
    public event EventHandler<Showtime>? OnShowtimeSelected;
    // Sự kiện bấm Quay lại
    public event EventHandler? OnBackClicked;

    public ShowtimeUC()
    {
        InitializeComponents();
    }

    public ShowtimeUC(Movie movie) : this()
    {
        SetMovie(movie);
    }

    private void InitializeComponents()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ColorBg;
        this.AutoScroll = true;

        // -------------------------------------------------------------
        // 1. HEADER & STEPPER BAR (THANH TIẾN TRÌNH 1-2-3)
        // -------------------------------------------------------------
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
        btnBack.Click += (s, e) => OnBackClicked?.Invoke(this, EventArgs.Empty);

        // Thanh tiến trình Step 1 - 2 - 3
        Panel pnlStepper = new Panel
        {
            Size = new Size(600, 50),
            Location = new Point(280, 15),
            BackColor = Color.Transparent
        };

        Label lblStep1 = CreateStepLabel("①  CHỌN SUẤT CHIẾU", true, 0);
        Label lblDivider1 = CreateDividerLabel("─────────", 185);
        Label lblStep2 = CreateStepLabel("②  CHỌN GHẾ", false, 275);
        Label lblDivider2 = CreateDividerLabel("─────────", 400);
        Label lblStep3 = CreateStepLabel("③  THANH TOÁN", false, 480);

        pnlStepper.Controls.AddRange(new Control[] { lblStep1, lblDivider1, lblStep2, lblDivider2, lblStep3 });

        // Thanh chọn ngày chiếu (Date Selector)
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

        // -------------------------------------------------------------
        // 2. PHẦN DANH SÁCH SUẤT CHIẾU (MAIN CONTENT)
        // -------------------------------------------------------------
        flpShowtimeList = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            AutoScroll = true
        };

        this.Controls.Add(flpShowtimeList);
        this.Controls.Add(pnlHeader);
    }

    private Label CreateStepLabel(string text, bool isActive, int xLocation)
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

    private Label CreateDividerLabel(string text, int xLocation)
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

    // Tạo các nút ngày (Hôm nay, Ngày mai...)
    private void GenerateDateButtons()
    {
        flpDateSelector.Controls.Clear();
        for (int i = 0; i < 7; i++)
        {
            DateTime date = DateTime.Today.AddDays(i);
            bool isSelected = date.Date == selectedDate.Date;

            Button btnDate = new Button
            {
                Text = i == 0 ? "HÔM NAY" : date.ToString("ddd dd/MM").ToUpper(),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = isSelected ? Color.Black : Color.White,
                BackColor = isSelected ? ColorGold : Color.FromArgb(35, 36, 50),
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
        if (sender is Button btn && btn.Tag is DateTime date)
        {
            selectedDate = date;
            GenerateDateButtons(); // Reload giao diện nút ngày
            LoadShowtimes();        // Reload danh sách suất chiếu
        }
    }

    public void SetMovie(Movie movie)
    {
        MovieData = movie;
        LoadShowtimes();
    }

    // Tải và hiển thị danh sách Suất chiếu (Giả lập theo từng định dạng)
    private void LoadShowtimes()
    {
        flpShowtimeList.Controls.Clear();

        if (MovieData == null) return;

        // Tạo 3 khối nhóm định dạng phim mẫu (Digital 2D, IMAX 3D, 4DX 3D)
        string[] formats = { "DIGITAL 2D", "IMAX 3D", "4DX 3D" };
        string[][] sampleTimes = {
            new[] { "09:20", "11:45", "14:15", "16:50", "19:30", "22:00" },
            new[] { "10:30", "13:40", "17:15", "20:45" },
            new[] { "12:00", "15:30", "18:45" }
        };

        for (int i = 0; i < formats.Length; i++)
        {
            Panel pnlFormatGroup = CreateShowtimeCardGroup(formats[i], sampleTimes[i]);
            flpShowtimeList.Controls.Add(pnlFormatGroup);
        }
    }

    private Panel CreateShowtimeCardGroup(string formatName, string[] times)
    {
        Panel card = new Panel
        {
            Size = new Size(1100, 150),
            BackColor = ColorCard,
            Margin = new Padding(0, 0, 0, 15)
        };

        // Poster phim thu nhỏ bên trái
        PictureBox picPoster = new PictureBox
        {
            Location = new Point(15, 15),
            Size = new Size(80, 120),
            SizeMode = PictureBoxSizeMode.StretchImage,
            BackColor = Color.FromArgb(45, 45, 60)
        };

        if (MovieData != null && !string.IsNullOrEmpty(MovieData.PosterUrl) && System.IO.File.Exists(MovieData.PosterUrl))
        {
            try { picPoster.Image = Image.FromFile(MovieData.PosterUrl); } catch { }
        }

        // Tên Phim & Định dạng
        Label lblTitle = new Label
        {
            Text = MovieData?.Title ?? "Movie Title",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(115, 15),
            AutoSize = true
        };

        Label lblFormatTag = new Label
        {
            Text = formatName,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = ColorGold,
            Location = new Point(115, 45),
            AutoSize = true
        };

        // FlowPanel chứa các nút Giờ chiếu
        FlowLayoutPanel flpTimes = new FlowLayoutPanel
        {
            Location = new Point(115, 75),
            Size = new Size(960, 60),
            AutoScroll = false
        };

        foreach (var timeStr in times)
        {
            Button btnTime = new Button
            {
                Text = timeStr,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(42, 43, 58),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(95, 38),
                Margin = new Padding(0, 0, 12, 0),
                Cursor = Cursors.Hand
            };
            btnTime.FlatAppearance.BorderSize = 0;
            btnTime.MouseEnter += (s, e) => btnTime.BackColor = ColorGold;
            btnTime.MouseLeave += (s, e) => btnTime.BackColor = Color.FromArgb(42, 43, 58);
            
            // Xử lý sự kiện chọn suất chiếu
            btnTime.Click += (s, e) =>
            {
                Showtime selectedShowtime = new Showtime
                {
                    Id = 1,
                    MovieId = MovieData?.Id ?? 1,
                    StartTime = DateTime.Parse($"{selectedDate:yyyy-MM-dd} {timeStr}")
                };
                OnShowtimeSelected?.Invoke(this, selectedShowtime);
            };

            flpTimes.Controls.Add(btnTime);
        }

        card.Controls.Add(picPoster);
        card.Controls.Add(lblTitle);
        card.Controls.Add(lblFormatTag);
        card.Controls.Add(flpTimes);

        return card;
    }
}