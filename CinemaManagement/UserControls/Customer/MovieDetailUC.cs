namespace CinemaManagement.UserControls.Customer;

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CinemaManagement.Models;
using CinemaManagement.Models.Enums;

public partial class MovieDetailUC : UserControl
{
    private readonly Color ColorBg = Color.FromArgb(18, 19, 28);
    private readonly Color ColorCardBg = Color.FromArgb(26, 27, 38);
    private readonly Color ColorAccentRed = Color.FromArgb(229, 9, 20);
    private readonly Color ColorDisabledBtn = Color.FromArgb(65, 70, 90);
    private readonly Color ColorTextMuted = Color.FromArgb(160, 160, 180);

    private Panel pnlHero = null!;
    private PictureBox picPoster = null!;
    private Label lblTitle = null!;
    private Label lblSynopsisHeader = null!;
    private RichTextBox lblDescription = null!;
    private FlowLayoutPanel flpMetadata = null!;
    private Label lblMetaDirector = null!;
    private Label lblMetaGenre = null!;
    private Label lblMetaDuration = null!;
    private Label lblMetaReleaseDate = null!;
    private Label lblMetaStatus = null!;
    private Button btnBookTicket = null!;
    private Button btnBack = null!;

    public Movie? MovieData { get; private set; }
    public event EventHandler<Movie>? OnBookTicketClicked;
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
        Dock = DockStyle.Fill;
        BackColor = ColorBg;
        AutoScroll = true;
        MinimumSize = new Size(0, 0);

        btnBack = new Button
        {
            Text = "← Quay lại",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(35, 36, 50),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(110, 35),
            Location = new Point(12, 12),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        btnBack.FlatAppearance.BorderSize = 0;
        btnBack.Click += (_, _) => OnBackClicked?.Invoke(this, EventArgs.Empty);

        pnlHero = new Panel
        {
            Location = new Point(12, 58),
            Size = new Size(900, 500),
            BackColor = ColorCardBg,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        picPoster = new PictureBox
        {
            Size = new Size(220, 320),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(40, 40, 55),
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        picPoster.Paint += (_, e) =>
        {
            if (picPoster.Image != null) return;
            using var font = new Font("Segoe UI Emoji", 32f);
            using var brush = new SolidBrush(Color.FromArgb(130, 135, 160));
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString("🎬", font, brush, picPoster.ClientRectangle, sf);
        };

        lblTitle = new Label
        {
            Text = "Thông tin bộ phim",
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = false,
            AutoEllipsis = true
        };

        lblSynopsisHeader = new Label
        {
            Text = "Tóm tắt & Chi tiết",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true
        };

        lblDescription = new RichTextBox
        {
            Text = "Đang tải mô tả phim...",
            Font = new Font("Segoe UI", 11.5f, FontStyle.Regular),
            ForeColor = Color.FromArgb(220, 228, 240),
            BackColor = ColorCardBg,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            Multiline = true,
            WordWrap = true,
            ScrollBars = RichTextBoxScrollBars.None,
            DetectUrls = false,
            TabStop = false,
            HideSelection = true,
            ShortcutsEnabled = true,
            AutoSize = false
        };

        flpMetadata = new FlowLayoutPanel
        {
            AutoScroll = false,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        lblMetaDirector = AddMetaItem("Đạo diễn", "Đang cập nhật");
        lblMetaGenre = AddMetaItem("Thể loại", "Đang cập nhật");
        lblMetaDuration = AddMetaItem("Thời lượng", "-- phút");
        lblMetaReleaseDate = AddMetaItem("Khởi chiếu", "--/--/----");
        lblMetaStatus = AddMetaItem("Trạng thái", "Đang chiếu");

        btnBookTicket = new Button
        {
            Text = "🎟  ĐẶT VÉ NGAY",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ColorAccentRed,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(280, 45),
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

        Controls.Add(btnBack);
        Controls.Add(pnlHero);

        Resize += (_, _) => UpdateResponsiveLayout();
        pnlHero.Resize += (_, _) => UpdateResponsiveLayout();
        UpdateResponsiveLayout();
    }

    private void UpdateResponsiveLayout()
    {
        if (pnlHero == null || IsDisposed) return;

        // Panel luôn theo chiều rộng hiện tại của UserControl, không giữ width cố định.
        int availableWidth = Math.Max(280, ClientSize.Width - 24);
        pnlHero.Location = new Point(12, 58);
        pnlHero.Width = availableWidth;

        int padding = 16;
        int posterWidth = 220;
        int posterHeight = 320;
        bool narrow = availableWidth < 760;

        if (!narrow)
        {
            // Màn hình rộng: poster bên trái, nội dung bên phải.
            picPoster.Location = new Point(padding, padding);
            picPoster.Size = new Size(posterWidth, posterHeight);

            int contentLeft = padding + posterWidth + 20;
            int contentWidth = Math.Max(220, availableWidth - contentLeft - padding);
            LayoutContent(contentLeft, padding, contentWidth, false);
        }
        else
        {
            // Màn hình nhỏ: poster ở trên, nội dung xếp dọc bên dưới.
            int posterW = Math.Min(220, Math.Max(140, availableWidth - padding * 2));
            int posterH = (int)(posterW * 1.45);
            picPoster.Size = new Size(posterW, posterH);
            picPoster.Location = new Point((availableWidth - posterW) / 2, padding);

            int contentTop = padding + posterH + 18;
            int contentWidth = Math.Max(240, availableWidth - padding * 2);
            LayoutContent(padding, contentTop, contentWidth, true);
        }
    }

    private void LayoutContent(int left, int top, int width, bool narrow)
    {
        lblTitle.Location = new Point(left, top);
        lblTitle.Width = width;
        lblTitle.Height = 56;

        lblSynopsisHeader.Location = new Point(left, top + 58);

        int descriptionTop = top + 92;
        lblDescription.Location = new Point(left, descriptionTop);
        lblDescription.Width = width;

        // Đo chiều cao theo đúng chiều rộng hiện tại để mọi dòng được hiển thị.
        string text = string.IsNullOrEmpty(lblDescription.Text)
            ? "Chưa có mô tả cho bộ phim này."
            : lblDescription.Text;
        int measuredWidth = Math.Max(120, width - 12);
        Size measured = TextRenderer.MeasureText(
            text,
            lblDescription.Font,
            new Size(measuredWidth, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPadding);
        int descriptionHeight = Math.Max(58, measured.Height + 18);
        lblDescription.Height = descriptionHeight;

        int metadataTop = descriptionTop + descriptionHeight + 18;
        flpMetadata.Location = new Point(left, metadataTop);
        flpMetadata.Width = width;

        int itemWidth = 155;
        int itemGap = 12;
        int columns = Math.Max(1, (width + itemGap) / (itemWidth + itemGap));
        int rows = (int)Math.Ceiling(5.0 / columns);
        flpMetadata.Height = rows * 62;

        int buttonTop = metadataTop + flpMetadata.Height + 14;
        btnBookTicket.Location = new Point(left, buttonTop);
        btnBookTicket.Width = Math.Min(280, width);

        int bottom = buttonTop + btnBookTicket.Height + 18;
        int minBottom = narrow ? picPoster.Bottom + 18 : picPoster.Bottom + 18;
        pnlHero.Height = Math.Max(bottom, minBottom);

        // RichTextBox does not need its own scroll bar because its full text is measured;
        // the parent UserControl provides vertical scrolling for long content.
        lblDescription.WordWrap = true;
        lblDescription.ScrollBars = RichTextBoxScrollBars.None;
    }

    private Label AddMetaItem(string title, string value)
    {
        Panel pnl = new Panel
        {
            Size = new Size(155, 58),
            Margin = new Padding(0, 0, 12, 4)
        };
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
            Height = 34,
            AutoEllipsis = true
        };
        pnl.Controls.Add(lblT);
        pnl.Controls.Add(lblV);
        flpMetadata.Controls.Add(pnl);
        return lblV;
    }

    public void SetMovie(Movie movie)
    {
        if (movie == null) return;

        MovieData = movie;
        lblTitle.Text = string.IsNullOrWhiteSpace(movie.Title) ? "Chưa có tên phim" : movie.Title;
        lblDescription.Text = string.IsNullOrWhiteSpace(movie.Description)
            ? "Chưa có mô tả cho bộ phim này."
            : movie.Description.Trim();
        lblDescription.SelectionStart = 0;
        lblDescription.SelectionLength = 0;

        lblMetaDirector.Text = string.IsNullOrWhiteSpace(movie.Director) ? "Đang cập nhật" : movie.Director;
        lblMetaGenre.Text = string.IsNullOrWhiteSpace(movie.Genre) ? "Chưa phân loại" : movie.Genre;
        lblMetaDuration.Text = movie.DurationMinutes > 0 ? $"{movie.DurationMinutes} phút" : "Đang cập nhật";
        lblMetaReleaseDate.Text = movie.ReleaseDate != default ? movie.ReleaseDate.ToString("dd/MM/yyyy") : "Đang cập nhật";

        switch (movie.Status)
        {
            case MovieStatus.Showing:
                lblMetaStatus.Text = "Đang chiếu";
                lblMetaStatus.ForeColor = Color.LightGreen;
                btnBookTicket.Enabled = true;
                btnBookTicket.Text = "🎟  ĐẶT VÉ NGAY";
                btnBookTicket.BackColor = ColorAccentRed;
                btnBookTicket.Cursor = Cursors.Hand;
                break;
            case MovieStatus.ComingSoon:
                lblMetaStatus.Text = "Sắp chiếu";
                lblMetaStatus.ForeColor = Color.Gold;
                btnBookTicket.Enabled = true;
                btnBookTicket.Text = "🎟  ĐẶT VÉ TRƯỚC";
                btnBookTicket.BackColor = ColorAccentRed;
                btnBookTicket.Cursor = Cursors.Hand;
                break;
            default:
                lblMetaStatus.Text = "Đã kết thúc";
                lblMetaStatus.ForeColor = Color.IndianRed;
                btnBookTicket.Enabled = false;
                btnBookTicket.Text = "⏹  PHIM ĐÃ KẾT THÚC";
                btnBookTicket.BackColor = ColorDisabledBtn;
                btnBookTicket.Cursor = Cursors.No;
                break;
        }

        if (picPoster.Image != null)
        {
            Image oldImg = picPoster.Image;
            picPoster.Image = null;
            oldImg.Dispose();
        }

        if (!string.IsNullOrWhiteSpace(movie.PosterUrl) && File.Exists(movie.PosterUrl))
        {
            try
            {
                using var stream = new FileStream(movie.PosterUrl, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var img = Image.FromStream(stream);
                picPoster.Image = new Bitmap(img);
            }
            catch
            {
                picPoster.Image = null;
            }
        }

        picPoster.Invalidate();
        UpdateResponsiveLayout();
    }

    private void BtnBookTicket_Click(object? sender, EventArgs e)
    {
        if (MovieData == null) return;

        if (MovieData.Status == MovieStatus.Ended)
        {
            MessageBox.Show(
                "Bộ phim này đã kết thúc đợt chiếu.",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        OnBookTicketClicked?.Invoke(this, MovieData);
    }
}