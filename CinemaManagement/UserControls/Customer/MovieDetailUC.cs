namespace CinemaManagement.UserControls.Customer;

using System;
using System.Drawing;
using System.Windows.Forms;
using CinemaManagement.Models;

public partial class MovieDetailUC : UserControl
{
    // Color Palette
    private readonly Color ColorBg = Color.FromArgb(18, 19, 28);
    private readonly Color ColorCardBg = Color.FromArgb(26, 27, 38);
    private readonly Color ColorAccentRed = Color.FromArgb(229, 9, 20);
    private readonly Color ColorTextMuted = Color.FromArgb(160, 160, 180);

    // UI Controls
    private Panel pnlHero;
    private PictureBox picPoster;
    private Label lblTitle;
    private Label lblSynopsisHeader;
    private Label lblDescription;
    private FlowLayoutPanel flpMetadata;
    private Button btnBookTicket;
    private Button btnBack;

    private Panel pnlCastSection;
    private Label lblCastHeader;
    private FlowLayoutPanel flpCastList;

    public Movie MovieData { get; private set; }

    // Sự kiện khi bấm "Đặt Vé Ngay" để chuyển sang màn hình Chọn Suất Chiếu (ShowtimeUC)
    public event EventHandler<Movie>? OnBookTicketClicked;
    // Sự kiện khi bấm "Quay lại" về HomeUC
    public event EventHandler? OnBackClicked;

    public MovieDetailUC()
    {
        InitializeComponents();
    }

    public MovieDetailUC(Movie movie) : this()
    {
        SetMovie(movie);
    }

    private void InitializeComponents()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = ColorBg;
        this.AutoScroll = true;

        // -------------------------------------------------------------
        // 1. NÚT BACK (QUAY LẠI TRANG CHỦ)
        // -------------------------------------------------------------
        btnBack = new Button
        {
            Text = "← Quay lại",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(35, 36, 50),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(110, 35),
            Location = new Point(20, 15),
            Cursor = Cursors.Hand
        };
        btnBack.FlatAppearance.BorderSize = 0;
        btnBack.Click += (s, e) => OnBackClicked?.Invoke(this, EventArgs.Empty);

        // -------------------------------------------------------------
        // 2. KHỐI HERO (BANNER BANNER & THÔNG TIN CHÍNH)
        // -------------------------------------------------------------
        pnlHero = new Panel
        {
            Location = new Point(20, 60),
            Size = new Size(1140, 360),
            BackColor = ColorCardBg,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        // Poster Phim bên trái
        picPoster = new PictureBox
        {
            Location = new Point(20, 20),
            Size = new Size(220, 320),
            SizeMode = PictureBoxSizeMode.StretchImage,
            BackColor = Color.FromArgb(40, 40, 55)
        };

        // Tiêu đề Phim
        lblTitle = new Label
        {
            Text = "John Wick: Chapter 3 - Parabellum",
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(260, 20),
            Size = new Size(850, 45),
            AutoEllipsis = true
        };

        // Tiêu đề tóm tắt
        lblSynopsisHeader = new Label
        {
            Text = "Tóm tắt & Chi tiết",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(260, 70),
            AutoSize = true
        };

        // Mô tả nội dung phim
        lblDescription = new Label
        {
            Text = "Sau khi ám sát một thành viên của Hội Đồng Tối Cao, sát thủ John Wick bị treo thưởng 14 triệu USD. Anh phải chiến đấu chống lại hàng loạt sát thủ máu lạnh để tìm đường sống sót khỏi New York...",
            Font = new Font("Segoe UI", 10),
            ForeColor = ColorTextMuted,
            Location = new Point(260, 105),
            Size = new Size(840, 70)
        };

        // Grid Thông tin phụ (Đạo diễn, Thể loại, Thời lượng, Độ tuổi)
        flpMetadata = new FlowLayoutPanel
        {
            Location = new Point(260, 185),
            Size = new Size(840, 70),
            AutoScroll = false
        };

        AddMetaItem("Đạo diễn", "Chad Stahelski");
        AddMetaItem("Thể loại", "Hành động, Sát thủ");
        AddMetaItem("Thời lượng", "131 phút");
        AddMetaItem("Độ tuổi", "C18 (Trên 18 tuổi)");

        // Nút bấm ĐẶT VÉ NGAY
        btnBookTicket = new Button
        {
            Text = "🎟  ĐẶT VÉ NGAY",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ColorAccentRed,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(200, 45),
            Location = new Point(260, 280),
            Cursor = Cursors.Hand
        };
        btnBookTicket.FlatAppearance.BorderSize = 0;
        btnBookTicket.Click += BtnBookTicket_Click;

        pnlHero.Controls.Add(picPoster);
        pnlHero.Controls.Add(lblTitle);
        pnlHero.Controls.Add(lblSynopsisHeader);
        pnlHero.Controls.Add(lblDescription);
        pnlHero.Controls.Add(flpMetadata);
        pnlHero.Controls.Add(btnBookTicket);

        // -------------------------------------------------------------
        // 3. KHỐI DANH SÁCH DIỄN VIÊN (CAST / ELENCO)
        // -------------------------------------------------------------
        pnlCastSection = new Panel
        {
            Location = new Point(20, 440),
            Size = new Size(1140, 240),
            BackColor = Color.Transparent,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        lblCastHeader = new Label
        {
            Text = "Diễn viên chính",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(0, 0),
            AutoSize = true
        };

        flpCastList = new FlowLayoutPanel
        {
            Location = new Point(0, 35),
            Size = new Size(1140, 190),
            AutoScroll = true,
            WrapContents = false
        };

        // Tải danh sách diễn viên mẫu
        AddCastCard("Keanu Reeves", "John Wick");
        AddCastCard("Halle Berry", "Sofia");
        AddCastCard("Laurence Fishburne", "Bowery King");
        AddCastCard("Mark Dacascos", "Zero");
        AddCastCard("Asia Kate Dillon", "The Adjudicator");
        AddCastCard("Lance Reddick", "Charon");

        pnlCastSection.Controls.Add(lblCastHeader);
        pnlCastSection.Controls.Add(flpCastList);

        // Thêm tất cả vào UserControl
        this.Controls.Add(btnBack);
        this.Controls.Add(pnlHero);
        this.Controls.Add(pnlCastSection);
    }

    private void AddMetaItem(string title, string value)
    {
        Panel pnl = new Panel { Size = new Size(190, 55), Margin = new Padding(0, 0, 15, 0) };
        Label lblT = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 20
        };
        Label lblV = new Label
        {
            Text = value,
            Font = new Font("Segoe UI", 9),
            ForeColor = ColorTextMuted,
            Dock = DockStyle.Bottom,
            Height = 30
        };
        pnl.Controls.Add(lblT);
        pnl.Controls.Add(lblV);
        flpMetadata.Controls.Add(pnl);
    }

    private void AddCastCard(string actorName, string characterName)
    {
        Panel card = new Panel
        {
            Size = new Size(140, 170),
            BackColor = ColorCardBg,
            Margin = new Padding(0, 0, 15, 0)
        };

        PictureBox picActor = new PictureBox
        {
            Dock = DockStyle.Top,
            Height = 110,
            SizeMode = PictureBoxSizeMode.StretchImage,
            BackColor = Color.FromArgb(45, 45, 60)
        };

        Label lblActor = new Label
        {
            Text = actorName,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 24,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = true
        };

        Label lblChar = new Label
        {
            Text = characterName,
            Font = new Font("Segoe UI", 8),
            ForeColor = ColorTextMuted,
            Dock = DockStyle.Bottom,
            Height = 26,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = true
        };

        card.Controls.Add(lblChar);
        card.Controls.Add(lblActor);
        card.Controls.Add(picActor);
        flpCastList.Controls.Add(card);
    }

    public void SetMovie(Movie movie)
    {
        MovieData = movie;
        lblTitle.Text = movie.Title;
        lblDescription.Text = string.IsNullOrEmpty(movie.Description) 
            ? "Chưa có mô tả cho bộ phim này." 
            : movie.Description;

        if (!string.IsNullOrEmpty(movie.PosterUrl) && System.IO.File.Exists(movie.PosterUrl))
        {
            try { picPoster.Image = Image.FromFile(movie.PosterUrl); } catch { }
        }
    }

    private void BtnBookTicket_Click(object? sender, EventArgs e)
    {
        if (MovieData != null)
        {
            OnBookTicketClicked?.Invoke(this, MovieData);
        }
    }
}