
using System;
using System.Drawing;
using System.Windows.Forms;
using CinemaManagement.Models;
using CinemaManagement.Models.Enums;

namespace CinemaManagement.Dialogs;

public partial class MovieEditDialog : Form
{
    private readonly Movie? originalMovie;

    private TextBox txtTitle = null!;
    private TextBox txtGenre = null!;
    private TextBox txtDirector = null!;
    private TextBox txtPoster = null!;
    private TextBox txtDescription = null!;

    private NumericUpDown numDuration = null!;
    private DateTimePicker dtpReleaseDate = null!;
    private ComboBox cboStatus = null!;

    private Button btnSave = null!;
    private Button btnCancel = null!;

    private readonly Color backgroundColor =
        Color.FromArgb(15, 23, 42);

    private readonly Color panelColor =
        Color.FromArgb(30, 41, 59);

    private readonly Color accentColor =
        Color.FromArgb(37, 99, 235);

    private readonly Color textColor =
        Color.FromArgb(226, 232, 240);

    // Dữ liệu trả về sau khi nhấn Lưu
    public Movie MovieData { get; private set; } = new();

    // Constructor dùng chung cho cả thêm và sửa
    public MovieEditDialog(Movie? movie = null)
    {
        originalMovie = movie;

        InitializeComponent();

        BuildInterface();
        LoadMovieInfo();
    }

    // =====================================
    // 1. XÂY DỰNG GIAO DIỆN
    // =====================================

    private void BuildInterface()
    {
        Text = originalMovie == null
            ? "Thêm phim mới"
            : "Chỉnh sửa phim";

        Size = new Size(760, 750);
        MinimumSize = new Size(700, 680);

        StartPosition = FormStartPosition.CenterParent;

        FormBorderStyle = FormBorderStyle.FixedDialog;

        MaximizeBox = false;
        MinimizeBox = false;

        BackColor = backgroundColor;
        ForeColor = textColor;

        Font = new Font("Segoe UI", 10);

        // Layout chính
        TableLayoutPanel mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(20),
            BackColor = backgroundColor
        };

        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 75));

        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 65));

        // =====================================
        // 2. TIÊU ĐỀ
        // =====================================

        Panel headerPanel = new Panel
        {
            Dock = DockStyle.Fill
        };

        Label lblTitle = new Label
        {
            Text = originalMovie == null
                ? "THÊM PHIM MỚI"
                : "CHỈNH SỬA PHIM",

            Font = new Font("Segoe UI", 18, FontStyle.Bold),

            ForeColor = Color.White,

            AutoSize = true,

            Location = new Point(5, 5)
        };

        Label lblDescription = new Label
        {
            Text = "Nhập đầy đủ thông tin phim bên dưới",

            Font = new Font("Segoe UI", 9),

            ForeColor = Color.FromArgb(148, 163, 184),

            AutoSize = true,

            Location = new Point(7, 42)
        };

        headerPanel.Controls.Add(lblTitle);
        headerPanel.Controls.Add(lblDescription);

        // =====================================
        // 3. KHU VỰC NHẬP THÔNG TIN
        // =====================================

        TableLayoutPanel fieldsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 8,
            BackColor = panelColor,
            Padding = new Padding(15),
            AutoScroll = true
        };

        fieldsLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 145));

        fieldsLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        for (int i = 0; i < 7; i++)
        {
            fieldsLayout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 50));
        }

        fieldsLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 135));

        // Tên phim
        txtTitle = CreateTextBox();

        AddField(
            fieldsLayout,
            0,
            "Tên phim *",
            txtTitle
        );

        // Thể loại
        txtGenre = CreateTextBox();

        AddField(
            fieldsLayout,
            1,
            "Thể loại *",
            txtGenre
        );

        // Đạo diễn
        txtDirector = CreateTextBox();

        AddField(
            fieldsLayout,
            2,
            "Đạo diễn *",
            txtDirector
        );

        // Thời lượng
        numDuration = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 600,
            Value = 120,
            Dock = DockStyle.Fill,
            TextAlign = HorizontalAlignment.Left,
            Font = new Font("Segoe UI", 10),
            Margin = new Padding(3, 8, 3, 8)
        };

        AddField(
            fieldsLayout,
            3,
            "Thời lượng (phút) *",
            numDuration
        );

        // Ngày khởi chiếu
        dtpReleaseDate = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10),
            Margin = new Padding(3, 8, 3, 8)
        };

        AddField(
            fieldsLayout,
            4,
            "Ngày khởi chiếu",
            dtpReleaseDate
        );

        // Trạng thái
        cboStatus = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10),
            Margin = new Padding(3, 8, 3, 8)
        };

        cboStatus.Items.AddRange(new object[]
        {
            "Đang chiếu",
            "Sắp chiếu",
            "Đã kết thúc"
        });

        cboStatus.SelectedIndex = 0;

        AddField(
            fieldsLayout,
            5,
            "Trạng thái",
            cboStatus
        );

        // Poster
        txtPoster = CreateTextBox();

        Button btnBrowse = new Button
        {
            Text = "Chọn ảnh",
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(71, 85, 105),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Margin = new Padding(5, 3, 3, 3),
            Cursor = Cursors.Hand
        };

        btnBrowse.FlatAppearance.BorderSize = 0;

        TableLayoutPanel posterLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };

        posterLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        posterLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 105));

        posterLayout.Controls.Add(txtPoster, 0, 0);
        posterLayout.Controls.Add(btnBrowse, 1, 0);

        AddField(
            fieldsLayout,
            6,
            "Poster",
            posterLayout
        );

        btnBrowse.Click += BtnBrowse_Click;

        // Mô tả phim
        txtDescription = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Segoe UI", 10),
            MaxLength = 2000,
            Margin = new Padding(3, 5, 3, 5)
        };

        AddField(
            fieldsLayout,
            7,
            "Mô tả phim",
            txtDescription
        );

        // =====================================
        // 4. NÚT LƯU VÀ HỦY
        // =====================================

        FlowLayoutPanel footerPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 12, 0, 0)
        };

        btnSave = CreateButton(
            "Lưu thông tin",
            accentColor
        );

        btnCancel = CreateButton(
            "Hủy",
            Color.FromArgb(71, 85, 105)
        );

        footerPanel.Controls.Add(btnCancel);
        footerPanel.Controls.Add(btnSave);

        btnSave.Click += BtnSave_Click;

        btnCancel.Click += (s, e) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        CancelButton = btnCancel;
        AcceptButton = btnSave;

        // =====================================
        // 5. GHÉP GIAO DIỆN
        // =====================================

        mainLayout.Controls.Add(headerPanel, 0, 0);
        mainLayout.Controls.Add(fieldsLayout, 0, 1);
        mainLayout.Controls.Add(footerPanel, 0, 2);

        Controls.Add(mainLayout);
    }

    // =====================================
    // 6. TẠO TEXTBOX
    // =====================================

    private TextBox CreateTextBox()
    {
        return new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10),
            Margin = new Padding(3, 8, 3, 8)
        };
    }

    // =====================================
    // 7. THÊM MỘT DÒNG TRƯỜNG DỮ LIỆU
    // =====================================

    private void AddField(
        TableLayoutPanel layout,
        int row,
        string labelText,
        Control input)
    {
        Label label = new Label
        {
            Text = labelText,
            ForeColor = textColor,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(3)
        };

        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(input, 1, row);
    }

    // =====================================
    // 8. TẠO BUTTON
    // =====================================

    private Button CreateButton(string text, Color color)
    {
        Button button = new Button
        {
            Text = text,
            Size = new Size(135, 38),
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(8, 0, 0, 0)
        };

        button.FlatAppearance.BorderSize = 0;

        return button;
    }

    // =====================================
    // 9. TẢI THÔNG TIN PHIM KHI CHỈNH SỬA
    // =====================================

    private void LoadMovieInfo()
    {
        if (originalMovie == null)
            return;

        txtTitle.Text = originalMovie.Title;
        txtGenre.Text = originalMovie.Genre;
        txtDirector.Text = originalMovie.Director;

        numDuration.Value = Math.Clamp(
            originalMovie.DurationMinutes,
            (int)numDuration.Minimum,
            (int)numDuration.Maximum
        );

        dtpReleaseDate.Value = originalMovie.ReleaseDate;

        txtPoster.Text = originalMovie.PosterUrl;

        txtDescription.Text = originalMovie.Description;

        cboStatus.SelectedIndex = originalMovie.Status switch
        {
            MovieStatus.Showing => 0,
            MovieStatus.ComingSoon => 1,
            MovieStatus.Ended => 2,
            _ => 0
        };
    }

    // =====================================
    // 10. CHỌN POSTER
    // =====================================

    private void BtnBrowse_Click(object? sender, EventArgs e)
    {
        using OpenFileDialog dialog = new OpenFileDialog
        {
            Title = "Chọn poster phim",
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            txtPoster.Text = dialog.FileName;
        }
    }

    // =====================================
    // 11. LƯU THÔNG TIN PHIM
    // =====================================

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        // Kiểm tra tên phim
        if (string.IsNullOrWhiteSpace(txtTitle.Text))
        {
            MessageBox.Show(
                "Vui lòng nhập tên phim!",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );

            txtTitle.Focus();
            return;
        }

        // Kiểm tra thể loại
        if (string.IsNullOrWhiteSpace(txtGenre.Text))
        {
            MessageBox.Show("Vui lòng nhập thể loại phim!");
            txtGenre.Focus();
            return;
        }

        // Kiểm tra đạo diễn
        if (string.IsNullOrWhiteSpace(txtDirector.Text))
        {
            MessageBox.Show("Vui lòng nhập tên đạo diễn!");
            txtDirector.Focus();
            return;
        }

        // Chuyển trạng thái sang Enum
        MovieStatus status = cboStatus.SelectedIndex switch
        {
            0 => MovieStatus.Showing,
            1 => MovieStatus.ComingSoon,
            2 => MovieStatus.Ended,
            _ => MovieStatus.Showing
        };

        // Tạo đối tượng phim
        MovieData = new Movie
        {
            Id = originalMovie?.Id ?? 0,

            Title = txtTitle.Text.Trim(),

            Genre = txtGenre.Text.Trim(),

            Director = txtDirector.Text.Trim(),

            DurationMinutes = (int)numDuration.Value,

            ReleaseDate = dtpReleaseDate.Value.Date,

            Status = status,

            PosterUrl = txtPoster.Text.Trim(),

            Description = txtDescription.Text.Trim()
        };

        DialogResult = DialogResult.OK;
        Close();
    }
}
