
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Models;
using CinemaManagement.Models.Enums;
using CinemaManagement.Repositories;

namespace CinemaManagement.UserControls.Admin;

public partial class ShowtimeManagementUC : UserControl
{
    // 1. REPOSITORY VÀ DỮ LIỆU

    private readonly MovieRepository _movieRepository = new();
    private readonly RoomRepository _roomRepository = new();
    private readonly ShowtimeRepository _showtimeRepository = new();

    private readonly List<Movie> _movies = new();
    private readonly List<Room> _rooms = new();
    private readonly List<Showtime> _showtimes = new();

    // Bảng Showtimes hiện chưa có cột Format.
    // Tạm thời lưu định dạng 2D/3D trong bộ nhớ.
    private readonly Dictionary<int, string> _formats = new();

    private int? _editingShowtimeId;

    // 2. CÁC THÀNH PHẦN GIAO DIỆN

    private DataGridView _dgvShowtimes = null!;

    private TextBox _txtSearch = null!;

    private ComboBox _cboMovie = null!;
    private ComboBox _cboRoom = null!;
    private ComboBox _cboFormat = null!;

    private NumericUpDown _numPrice = null!;

    private DateTimePicker _dtpDate = null!;
    private DateTimePicker _dtpStartTime = null!;
    private DateTimePicker _dtpFilterDate = null!;

    private Label _lblTotal = null!;
    private Label _lblToday = null!;
    private Label _lblRooms = null!;
    private Label _lblEndPreview = null!;
    private Label _lblResult = null!;

    private Button _btnSave = null!;
    private Button _btnEdit = null!;
    private Button _btnCancel = null!;
    private Button _btnDelete = null!;

    private readonly Color _backgroundColor =
        Color.FromArgb(15, 23, 42);

    private readonly Color _panelColor =
        Color.FromArgb(30, 41, 59);

    private readonly Color _accentColor =
        Color.FromArgb(37, 99, 235);

    private readonly Color _textColor =
        Color.FromArgb(226, 232, 240);

    // 3. CONSTRUCTOR

    public ShowtimeManagementUC()
    {
        InitializeComponent();

        Dock = DockStyle.Fill;
        BackColor = _backgroundColor;

        BuildInterface();

        // Lấy dữ liệu thật từ SQLite.
        LoadDatabaseData();

        LoadComboBoxes();

        RefreshShowtimeTable();

        UpdateEndPreview();
    }

    // =========================================
    // 4. XÂY DỰNG GIAO DIỆN
    // =========================================

    private void BuildInterface()
    {
        TableLayoutPanel mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = _backgroundColor,
            Padding = new Padding(0)
        };

        // Hàng 1: Tiêu đề
        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 85));

        // Hàng 2: Ba thẻ thống kê
        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 130));

        // Hàng 3: Tìm kiếm và lọc
        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 64));

        // Hàng 4: Biểu mẫu thông tin suất chiếu
        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 205));

        // Hàng 5: Bảng danh sách suất chiếu
        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        // Hàng 6: Chân trang
        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 32));

        // HEADER

        Panel headerPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = _backgroundColor
        };

        Label lblTitle = new Label
        {
            Text = "QUẢN LÝ SUẤT CHIẾU",
            Font = new Font("Segoe UI", 21, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(5, 10)
        };

        Label lblDescription = new Label
        {
            Text = "Lập lịch chiếu phim và kiểm tra lịch phòng",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(7, 52)
        };

        headerPanel.Controls.Add(lblTitle);
        headerPanel.Controls.Add(lblDescription);

        // THỐNG KÊ

        TableLayoutPanel statisticsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = _backgroundColor
        };

        for (int i = 0; i < 3; i++)
        {
            statisticsPanel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 33.33f));
        }

        statisticsPanel.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        Panel cardTotal = CreateStatisticCard(
            "TỔNG SUẤT CHIẾU",
            Color.FromArgb(37, 99, 235),
            out _lblTotal
        );

        Panel cardToday = CreateStatisticCard(
            "SUẤT CHIẾU HÔM NAY",
            Color.FromArgb(16, 185, 129),
            out _lblToday
        );

        Panel cardRooms = CreateStatisticCard(
            "PHÒNG ĐANG QUẢN LÝ",
            Color.FromArgb(139, 92, 246),
            out _lblRooms
        );

        cardTotal.Margin = new Padding(0, 5, 10, 5);
        cardToday.Margin = new Padding(5, 5, 5, 5);
        cardRooms.Margin = new Padding(10, 5, 0, 5);

        statisticsPanel.Controls.Add(cardTotal, 0, 0);
        statisticsPanel.Controls.Add(cardToday, 1, 0);
        statisticsPanel.Controls.Add(cardRooms, 2, 0);

        // TÌM KIẾM VÀ LỌC

        FlowLayoutPanel filterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = _panelColor,
            Padding = new Padding(12, 12, 5, 0),
            WrapContents = false
        };

        Label lblSearch = CreateLabel("Tìm kiếm:", 10);

        _txtSearch = new TextBox
        {
            Width = 250,
            Font = new Font("Segoe UI", 10),
            Margin = new Padding(0, 2, 25, 0)
        };

        Label lblFilterDate = CreateLabel("Lọc ngày:", 10);

        _dtpFilterDate = new DateTimePicker
        {
            Width = 160,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            ShowCheckBox = true,
            Checked = false,
            Font = new Font("Segoe UI", 9),
            Margin = new Padding(0, 2, 10, 0)
        };

        filterPanel.Controls.Add(lblSearch);
        filterPanel.Controls.Add(_txtSearch);
        filterPanel.Controls.Add(lblFilterDate);
        filterPanel.Controls.Add(_dtpFilterDate);

        // FORM THÔNG TIN SUẤT CHIẾU

        Panel editorPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = _panelColor,
            Padding = new Padding(12)
        };

        Label lblEditorTitle = new Label
        {
            Text = "THÔNG TIN SUẤT CHIẾU",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 28
        };

        // Bố cục nhập liệu: 4 cột x 2 hàng

        TableLayoutPanel fieldsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2,
            BackColor = _panelColor,
            Padding = new Padding(2),
            Margin = new Padding(0)
        };

        for (int i = 0; i < 4; i++)
        {
            fieldsPanel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 25));
        }

        fieldsPanel.RowStyles.Add(
            new RowStyle(SizeType.Percent, 50));

        fieldsPanel.RowStyles.Add(
            new RowStyle(SizeType.Percent, 50));

        // HÀNG 1

        // Phim

        _cboMovie = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9)
        };

        fieldsPanel.Controls.Add(
            CreateEditorField("Phim:", _cboMovie, 230), 0, 0);

        // Phòng chiếu

        _cboRoom = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9)
        };

        fieldsPanel.Controls.Add(
            CreateEditorField("Phòng chiếu:", _cboRoom, 180), 1, 0);

        // Ngày chiếu

        _dtpDate = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Font = new Font("Segoe UI", 9),
            Value = DateTime.Today.AddDays(1)
        };

        fieldsPanel.Controls.Add(
            CreateEditorField("Ngày chiếu:", _dtpDate, 150), 2, 0);

        // Giờ bắt đầu

        _dtpStartTime = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "HH:mm",
            ShowUpDown = true,
            Font = new Font("Segoe UI", 9),
            Value = DateTime.Today.AddHours(9)
        };

        fieldsPanel.Controls.Add(
            CreateEditorField("Giờ bắt đầu:", _dtpStartTime, 130), 3, 0);

        // HÀNG 2

        // Định dạng

        _cboFormat = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9)
        };

        _cboFormat.Items.AddRange(new object[]
        {
            "2D",
            "3D"
        });

        _cboFormat.SelectedIndex = 0;

        fieldsPanel.Controls.Add(
            CreateEditorField("Định dạng:", _cboFormat, 130), 0, 1);

        // Giá vé

        _numPrice = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 1000000,
            Increment = 5000,
            Value = 75000,
            ThousandsSeparator = true,
            Font = new Font("Segoe UI", 9)
        };

        fieldsPanel.Controls.Add(
            CreateEditorField("Giá vé (VNĐ):", _numPrice, 150), 1, 1);

        // Giờ kết thúc dự kiến

        _lblEndPreview = new Label
        {
            Text = "--:--",
            ForeColor = Color.FromArgb(52, 211, 153),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        fieldsPanel.Controls.Add(
            CreateEditorField(
                "Giờ kết thúc dự kiến:",
                _lblEndPreview,
                180), 2, 1);

        // Trạng thái kiểm tra lịch

        Label lblScheduleInfo = new Label
        {
            Text = "Tự động kiểm tra trùng lịch khi lưu",
            ForeColor = Color.FromArgb(52, 211, 153),
            Font = new Font("Segoe UI", 9),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        fieldsPanel.Controls.Add(
            CreateEditorField(
                "Kiểm tra lịch:",
                lblScheduleInfo,
                220), 3, 1);

        // CÁC NÚT CHỨC NĂNG

        FlowLayoutPanel actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 43,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = _panelColor,
            Padding = new Padding(0, 4, 0, 0)
        };

        _btnDelete = CreateButton(
            "Xóa suất chiếu",
            Color.FromArgb(220, 38, 38));

        _btnCancel = CreateButton(
            "Hủy sửa",
            Color.FromArgb(71, 85, 105));

        _btnEdit = CreateButton(
            "Sửa suất chiếu",
            Color.FromArgb(217, 119, 6));

        _btnSave = CreateButton(
            "Tạo suất chiếu",
            _accentColor);

        _btnCancel.Visible = false;

        actionPanel.Controls.Add(_btnDelete);
        actionPanel.Controls.Add(_btnCancel);
        actionPanel.Controls.Add(_btnEdit);
        actionPanel.Controls.Add(_btnSave);

        // Ghép các khu vực

        editorPanel.Controls.Add(fieldsPanel);
        editorPanel.Controls.Add(actionPanel);
        editorPanel.Controls.Add(lblEditorTitle);

        // BẢNG DANH SÁCH SUẤT CHIẾU

        _dgvShowtimes = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = _panelColor,
            BorderStyle = BorderStyle.None,
            AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            MultiSelect = false,
            SelectionMode =
                DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false
        };

        _dgvShowtimes.ColumnHeadersDefaultCellStyle.BackColor =
            _accentColor;

        _dgvShowtimes.ColumnHeadersDefaultCellStyle.ForeColor =
            Color.White;

        _dgvShowtimes.ColumnHeadersDefaultCellStyle.Font =
            new Font("Segoe UI", 9, FontStyle.Bold);

        _dgvShowtimes.ColumnHeadersHeight = 42;

        _dgvShowtimes.DefaultCellStyle.BackColor = _panelColor;
        _dgvShowtimes.DefaultCellStyle.ForeColor = Color.White;

        _dgvShowtimes.DefaultCellStyle.SelectionBackColor =
            Color.FromArgb(51, 65, 85);

        _dgvShowtimes.DefaultCellStyle.SelectionForeColor =
            Color.White;

        _dgvShowtimes.DefaultCellStyle.Font =
            new Font("Segoe UI", 9);

        _dgvShowtimes.RowTemplate.Height = 38;

        _dgvShowtimes.AlternatingRowsDefaultCellStyle.BackColor =
            Color.FromArgb(37, 49, 68);

        _dgvShowtimes.AllowUserToAddRows = false;
        _dgvShowtimes.ColumnHeadersHeight = 44;

        _dgvShowtimes.AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill;

        _dgvShowtimes.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleLeft;

        _dgvShowtimes.Columns.Add("Id", "Mã suất");
        _dgvShowtimes.Columns.Add("Movie", "Tên phim");
        _dgvShowtimes.Columns.Add("Room", "Phòng chiếu");
        _dgvShowtimes.Columns.Add("Date", "Ngày chiếu");
        _dgvShowtimes.Columns.Add("Start", "Giờ bắt đầu");
        _dgvShowtimes.Columns.Add("End", "Giờ kết thúc");
        _dgvShowtimes.Columns.Add("Format", "Định dạng");
        _dgvShowtimes.Columns.Add("Price", "Giá vé");

        // CHÂN TRANG

        _lblResult = new Label
        {
            Text = "Tổng số suất chiếu: 0",
            Dock = DockStyle.Fill,
            ForeColor = _textColor,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // GHÉP GIAO DIỆN

        mainLayout.Controls.Add(headerPanel, 0, 0);
        mainLayout.Controls.Add(statisticsPanel, 0, 1);
        mainLayout.Controls.Add(filterPanel, 0, 2);
        mainLayout.Controls.Add(editorPanel, 0, 3);
        mainLayout.Controls.Add(_dgvShowtimes, 0, 4);
        mainLayout.Controls.Add(_lblResult, 0, 5);

        Controls.Add(mainLayout);

        // GẮN SỰ KIỆN

        _txtSearch.TextChanged += (s, e) =>
            RefreshShowtimeTable();

        _dtpFilterDate.ValueChanged += (s, e) =>
            RefreshShowtimeTable();

        _cboMovie.SelectedIndexChanged += (s, e) =>
            UpdateEndPreview();

        _dtpStartTime.ValueChanged += (s, e) =>
            UpdateEndPreview();

        _dtpDate.ValueChanged += (s, e) =>
            UpdateEndPreview();

        _btnSave.Click += BtnSave_Click;
        _btnEdit.Click += BtnEdit_Click;
        _btnCancel.Click += BtnCancel_Click;
        _btnDelete.Click += BtnDelete_Click;
    }

    // 5. TẠO LABEL

    private Label CreateLabel(string text, int marginRight)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            ForeColor = _textColor,
            Font = new Font("Segoe UI", 9),
            Margin = new Padding(3, 8, marginRight, 0)
        };
    }

    // 6. TẠO Ô NHẬP LIỆU CÓ TIÊU ĐỀ

    private Panel CreateEditorField(
        string title,
        Control input,
        int width)
    {
        Panel field = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 65,
            BackColor = _panelColor,
            Margin = new Padding(6, 2, 6, 2)
        };

        Label label = new Label
        {
            Text = title,
            ForeColor = _textColor,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 23,
            TextAlign = ContentAlignment.MiddleLeft
        };

        input.Dock = DockStyle.Bottom;
        input.Height = 30;
        input.Margin = new Padding(0);

        field.Controls.Add(input);
        field.Controls.Add(label);

        return field;
    }

    // 7. TẠO NÚT

    private Button CreateButton(string text, Color color)
    {
        Button button = new Button
        {
            Text = text,
            Size = new Size(140, 35),
            BackColor = color,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(5, 0, 0, 0)
        };

        button.FlatAppearance.BorderSize = 0;

        return button;
    }

    // 8. TẠO THẺ THỐNG KÊ

    private Panel CreateStatisticCard(
        string title,
        Color cardAccentColor,
        out Label valueLabel)
    {
        Panel card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = _panelColor,
            Margin = new Padding(0)
        };

        // Thanh màu bên trái

        Panel accentBar = new Panel
        {
            Width = 5,
            Dock = DockStyle.Left,
            BackColor = cardAccentColor
        };

        // Tiêu đề

        Label lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(20, 10)
        };

        // Số liệu

        valueLabel = new Label
        {
            Text = "0",
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 32)
        };

        // Mô tả

        string description = title switch
        {
            "TỔNG SUẤT CHIẾU" =>
                "Tất cả lịch chiếu đã tạo",

            "SUẤT CHIẾU HÔM NAY" =>
                "Số suất chiếu trong ngày",

            "PHÒNG ĐANG QUẢN LÝ" =>
                "Tổng số phòng chiếu hiện có",

            _ => "Thông tin thống kê"
        };

        Label lblDescription = new Label
        {
            Text = description,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(203, 213, 225),
            AutoSize = false,
            Size = new Size(350, 20),
            Location = new Point(22, 81),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Biểu tượng

        Color iconBackground = Color.FromArgb(
            (30 + cardAccentColor.R) / 2,
            (41 + cardAccentColor.G) / 2,
            (59 + cardAccentColor.B) / 2
        );

        Panel iconPanel = new Panel
        {
            Size = new Size(48, 48),
            BackColor = iconBackground,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        string iconText = title switch
        {
            "TỔNG SUẤT CHIẾU" => "▦",
            "SUẤT CHIẾU HÔM NAY" => "◷",
            "PHÒNG ĐANG QUẢN LÝ" => "▣",
            _ => "●"
        };

        Label lblIcon = new Label
        {
            Text = iconText,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Symbol", 22, FontStyle.Bold),
            ForeColor = cardAccentColor,
            TextAlign = ContentAlignment.MiddleCenter
        };

        iconPanel.Controls.Add(lblIcon);

        // Căn giữa biểu tượng theo chiều dọc

        void UpdateIconPosition()
        {
            accentBar.Height = card.ClientSize.Height;

            iconPanel.Location = new Point(
                card.ClientSize.Width - iconPanel.Width - 18,
                (card.ClientSize.Height - iconPanel.Height) / 2
            );

            lblDescription.Width = Math.Max(
                150,
                card.ClientSize.Width - 110
            );
        }

        card.Resize += (s, e) => UpdateIconPosition();

        // Thêm thành phần

        card.Controls.Add(lblTitle);
        card.Controls.Add(valueLabel);
        card.Controls.Add(lblDescription);
        card.Controls.Add(iconPanel);
        card.Controls.Add(accentBar);

        UpdateIconPosition();

        // Viền thẻ

        card.Paint += (s, e) =>
        {
            using Pen pen = new Pen(
                Color.FromArgb(51, 65, 85));

            e.Graphics.DrawRectangle(
                pen,
                0,
                0,
                card.Width - 1,
                card.Height - 1
            );
        };

        return card;
    }

    // =========================================
    // 9. TẢI DỮ LIỆU TỪ SQLITE
    // =========================================

    private void LoadDatabaseData()
    {
        try
        {
            _movies.Clear();
            _rooms.Clear();
            _showtimes.Clear();

            // Lấy phim từ bảng Movies.
            _movies.AddRange(_movieRepository.GetAll());

            // Lấy phòng từ bảng Rooms.
            _rooms.AddRange(_roomRepository.GetAll());

            // Lấy suất chiếu từ bảng Showtimes.
            _showtimes.AddRange(_showtimeRepository.GetAll());

            // Database chưa có cột Format.
            // Tạm thời hiển thị 2D cho các suất chiếu đã lưu.
            _formats.Clear();

            foreach (Showtime showtime in _showtimes)
            {
                _formats[showtime.Id] = "2D";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Không thể tải dữ liệu từ database.\n\n" + ex.Message,
                "Lỗi tải dữ liệu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    // =========================================
    // 10. ĐỔ DỮ LIỆU VÀO COMBOBOX
    // =========================================

    private void LoadComboBoxes()
    {
        _cboMovie.Items.Clear();
        _cboRoom.Items.Clear();

        _cboMovie.DisplayMember = nameof(Movie.Title);
        _cboRoom.DisplayMember = nameof(Room.Name);

        foreach (Movie movie in _movies.Where(
                     m => m.Status != MovieStatus.Ended))
        {
            _cboMovie.Items.Add(movie);
        }

        foreach (Room room in _rooms)
        {
            _cboRoom.Items.Add(room);
        }

        if (_cboMovie.Items.Count > 0)
            _cboMovie.SelectedIndex = 0;

        if (_cboRoom.Items.Count > 0)
            _cboRoom.SelectedIndex = 0;
    }

    // =========================================
    // 11. CẬP NHẬT GIỜ KẾT THÚC DỰ KIẾN
    // =========================================

    private void UpdateEndPreview()
    {
        Movie? movie = _cboMovie.SelectedItem as Movie;

        if (movie == null || movie.DurationMinutes <= 0)
        {
            _lblEndPreview.Text = "--:--";
            return;
        }

        DateTime start = _dtpDate.Value.Date.Add(
            _dtpStartTime.Value.TimeOfDay
        );

        DateTime end = start.AddMinutes(movie.DurationMinutes);

        _lblEndPreview.Text =
            end.ToString("dd/MM/yyyy HH:mm");
    }

    // =========================================
    // 12. KIỂM TRA TRÙNG LỊCH PHÒNG
    // =========================================

    private Showtime? FindScheduleConflict(
        int roomId,
        DateTime start,
        DateTime end)
    {
        return _showtimes.FirstOrDefault(existing =>
            existing.RoomId == roomId &&
            existing.Id != (_editingShowtimeId ?? -1) &&
            start < existing.EndTime &&
            end > existing.StartTime
        );
    }

    // =========================================
    // 13. LÀM MỚI BẢNG SUẤT CHIẾU
    // =========================================

    private void RefreshShowtimeTable()
    {
        _dgvShowtimes.Rows.Clear();

        string keyword = _txtSearch.Text
            .Trim()
            .ToLowerInvariant();

        var result = _showtimes.Where(showtime =>
        {
            Movie? movie = _movies.FirstOrDefault(
                m => m.Id == showtime.MovieId
            );

            Room? room = _rooms.FirstOrDefault(
                r => r.Id == showtime.RoomId
            );

            bool matchKeyword =
                (movie?.Title.ToLowerInvariant().Contains(keyword) ?? false)
                ||
                (room?.Name.ToLowerInvariant().Contains(keyword) ?? false);

            bool matchDate =
                !_dtpFilterDate.Checked ||
                showtime.StartTime.Date == _dtpFilterDate.Value.Date;

            return matchKeyword && matchDate;

        })
        .OrderBy(s => s.StartTime)
        .ToList();

        foreach (Showtime showtime in result)
        {
            Movie? movie = _movies.FirstOrDefault(
                m => m.Id == showtime.MovieId
            );

            Room? room = _rooms.FirstOrDefault(
                r => r.Id == showtime.RoomId
            );

            string format = _formats.TryGetValue(
                showtime.Id,
                out string? value)
                ? value
                : "2D";

            _dgvShowtimes.Rows.Add(
                showtime.Id,
                movie?.Title ?? "Không xác định",
                room?.Name ?? "Không xác định",
                showtime.StartTime.ToString("dd/MM/yyyy"),
                showtime.StartTime.ToString("HH:mm"),
                showtime.EndTime.ToString("HH:mm"),
                format,
                showtime.BasePrice.ToString("N0") + " đ"
            );
        }

        UpdateStatistics();

        _lblResult.Text =
            $"Đang hiển thị {result.Count} / {_showtimes.Count} suất chiếu";
    }

    // =========================================
    // 14. CẬP NHẬT THỐNG KÊ
    // =========================================

    private void UpdateStatistics()
    {
        _lblTotal.Text = _showtimes.Count.ToString();

        _lblToday.Text = _showtimes.Count(
            s => s.StartTime.Date == DateTime.Today
        ).ToString();

        _lblRooms.Text = _rooms.Count.ToString();
    }

    // =========================================
    // 15. TẠO HOẶC CẬP NHẬT SUẤT CHIẾU
    // =========================================

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        Movie? movie = _cboMovie.SelectedItem as Movie;
        Room? room = _cboRoom.SelectedItem as Room;

        if (movie == null)
        {
            MessageBox.Show("Vui lòng chọn phim!");
            return;
        }

        if (room == null)
        {
            MessageBox.Show("Vui lòng chọn phòng chiếu!");
            return;
        }

        if (movie.DurationMinutes <= 0)
        {
            MessageBox.Show("Thời lượng phim không hợp lệ!");
            return;
        }

        DateTime start = _dtpDate.Value.Date.Add(
            _dtpStartTime.Value.TimeOfDay
        );

        DateTime end = start.AddMinutes(
            movie.DurationMinutes
        );

        int roomId;

        try
        {
            // Room.Id là long, còn Showtime.RoomId là int.
            roomId = checked((int)room.Id);
        }
        catch (OverflowException)
        {
            MessageBox.Show(
                "Mã phòng chiếu vượt quá giới hạn cho phép."
            );
            return;
        }

        try
        {
            // Lấy dữ liệu mới nhất từ SQLite trước khi kiểm tra.
            _showtimes.Clear();
            _showtimes.AddRange(_showtimeRepository.GetAll());

            // Kiểm tra trùng lịch trong cùng phòng.
            Showtime? conflict = FindScheduleConflict(
                roomId,
                start,
                end
            );

            if (conflict != null)
            {
                MessageBox.Show(
                    $"Phòng {room.Name} đã có suất chiếu trùng lịch!\n\n" +
                    $"Thời gian bị trùng: " +
                    $"{conflict.StartTime:dd/MM/yyyy HH:mm} - " +
                    $"{conflict.EndTime:dd/MM/yyyy HH:mm}",
                    "Trùng lịch chiếu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                RefreshShowtimeTable();
                return;
            }

            string format =
                _cboFormat.SelectedItem?.ToString() ?? "2D";

            if (_editingShowtimeId.HasValue)
            {
                // CẬP NHẬT SUẤT CHIẾU

                Showtime? current = _showtimes.FirstOrDefault(
                    s => s.Id == _editingShowtimeId.Value
                );

                if (current == null)
                {
                    MessageBox.Show("Không tìm thấy suất chiếu!");

                    ResetEditor();
                    RefreshShowtimeTable();

                    return;
                }

                current.MovieId = movie.Id;
                current.RoomId = roomId;
                current.StartTime = start;
                current.EndTime = end;
                current.BasePrice = _numPrice.Value;

                bool updated = _showtimeRepository.Update(current);

                if (!updated)
                {
                    MessageBox.Show(
                        "Không thể cập nhật suất chiếu!"
                    );
                    return;
                }

                _formats[current.Id] = format;

                MessageBox.Show(
                    "Cập nhật suất chiếu thành công!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            else
            {
                // TẠO SUẤT CHIẾU MỚI

                Showtime newShowtime = new Showtime
                {
                    MovieId = movie.Id,
                    RoomId = roomId,
                    StartTime = start,
                    EndTime = end,
                    BasePrice = _numPrice.Value
                };

                int newId = _showtimeRepository.Add(newShowtime);

                _formats[newId] = format;

                MessageBox.Show(
                    "Tạo suất chiếu thành công!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }

            // Tải lại dữ liệu từ SQLite.
            LoadDatabaseData();

            LoadComboBoxes();

            RefreshShowtimeTable();

            ResetEditor();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Không thể lưu suất chiếu vào database.\n\n" + ex.Message,
                "Lỗi lưu dữ liệu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    // =========================================
    // 16. CHỌN SUẤT CHIẾU ĐỂ SỬA
    // =========================================

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        if (_dgvShowtimes.SelectedRows.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn suất chiếu cần sửa!");
            return;
        }

        int id = Convert.ToInt32(
            _dgvShowtimes.SelectedRows[0].Cells["Id"].Value
        );

        Showtime? showtime = _showtimes.FirstOrDefault(
            s => s.Id == id
        );

        if (showtime == null)
            return;

        Movie? movie = _movies.FirstOrDefault(
            m => m.Id == showtime.MovieId
        );

        Room? room = _rooms.FirstOrDefault(
            r => r.Id == showtime.RoomId
        );

        if (movie == null || room == null)
        {
            MessageBox.Show(
                "Không tìm thấy phim hoặc phòng chiếu!"
            );
            return;
        }

        _editingShowtimeId = id;

        _cboMovie.SelectedItem = movie;
        _cboRoom.SelectedItem = room;

        _dtpDate.Value = showtime.StartTime.Date;

        _dtpStartTime.Value = DateTime.Today.Add(
            showtime.StartTime.TimeOfDay
        );

        _numPrice.Value = Math.Clamp(
            showtime.BasePrice,
            _numPrice.Minimum,
            _numPrice.Maximum
        );

        string format = _formats.TryGetValue(
            id,
            out string? value)
            ? value
            : "2D";

        _cboFormat.SelectedItem = format;

        _btnSave.Text = "Cập nhật suất chiếu";
        _btnCancel.Visible = true;

        UpdateEndPreview();
    }

    // =========================================
    // 17. HỦY CHẾ ĐỘ CHỈNH SỬA
    // =========================================

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        ResetEditor();
    }

    private void ResetEditor()
    {
        _editingShowtimeId = null;

        if (_cboMovie.Items.Count > 0)
            _cboMovie.SelectedIndex = 0;

        if (_cboRoom.Items.Count > 0)
            _cboRoom.SelectedIndex = 0;

        _cboFormat.SelectedIndex = 0;

        _dtpDate.Value = DateTime.Today.AddDays(1);

        _dtpStartTime.Value = DateTime.Today.AddHours(9);

        _numPrice.Value = 75000;

        _btnSave.Text = "Tạo suất chiếu";
        _btnCancel.Visible = false;

        UpdateEndPreview();
    }

    // =========================================
    // 18. XÓA SUẤT CHIẾU
    // =========================================

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (_dgvShowtimes.SelectedRows.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn suất chiếu cần xóa!");
            return;
        }

        int id = Convert.ToInt32(
            _dgvShowtimes.SelectedRows[0].Cells["Id"].Value
        );

        DialogResult confirm = MessageBox.Show(
            "Bạn có chắc chắn muốn xóa suất chiếu này?",
            "Xác nhận xóa",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        );

        if (confirm != DialogResult.Yes)
            return;

        try
        {
            bool deleted = _showtimeRepository.Delete(id);

            if (!deleted)
            {
                MessageBox.Show(
                    "Không tìm thấy suất chiếu để xóa!"
                );
                return;
            }

            _formats.Remove(id);

            if (_editingShowtimeId == id)
                ResetEditor();

            LoadDatabaseData();

            RefreshShowtimeTable();

            MessageBox.Show(
                "Xóa suất chiếu thành công!",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Không thể xóa suất chiếu.\n\n" + ex.Message,
                "Lỗi xóa dữ liệu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }
}
