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
    // =========================================
    // 1. REPOSITORY VÀ DỮ LIỆU
    // =========================================

    private readonly MovieRepository _movieRepository = new();
    private readonly RoomRepository _roomRepository = new();
    private readonly ShowtimeRepository _showtimeRepository = new();

    private readonly List<Movie> _movies = new();
    private readonly List<Room> _rooms = new();
    private readonly List<Showtime> _showtimes = new();

    private int? _editingShowtimeId;

    // =========================================
    // 2. CÁC THÀNH PHẦN GIAO DIỆN
    // =========================================

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

    private Panel _scrollHost = null!;
    private TableLayoutPanel _mainLayout = null!;
    private TableLayoutPanel _statisticsPanel = null!;
    private Panel _cardTotal = null!;
    private Panel _cardToday = null!;
    private Panel _cardRooms = null!;
    private FlowLayoutPanel _filterPanel = null!;
    private Panel _editorPanel = null!;
    private TableLayoutPanel _fieldsPanel = null!;
    private FlowLayoutPanel _actionPanel = null!;

    private bool _isApplyingResponsiveLayout;

    private readonly Color _backgroundColor =
        Color.FromArgb(15, 23, 42);

    private readonly Color _panelColor =
        Color.FromArgb(30, 41, 59);

    private readonly Color _accentColor =
        Color.FromArgb(37, 99, 235);

    private readonly Color _textColor =
        Color.FromArgb(226, 232, 240);

    // =========================================
    // 3. CONSTRUCTOR
    // =========================================

    public ShowtimeManagementUC()
    {
        InitializeComponent();

        AutoScaleMode = AutoScaleMode.Dpi;
        Dock = DockStyle.Fill;
        BackColor = _backgroundColor;

        BuildInterface();

        SizeChanged += ShowtimeManagementUC_SizeChanged;

        ApplyResponsiveLayout();

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
        // Vùng cuộn dọc bao toàn bộ nội dung UserControl.
        // Khi màn hình thấp, người dùng có thể cuộn từ tiêu đề đến bảng dữ liệu.
        _scrollHost = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = _backgroundColor,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        _mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.None,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = _backgroundColor,
            Padding = new Padding(0)
        };

        _mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 85));

        _mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 130));

        _mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 64));

        _mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 205));

        _mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        _mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 32));

        // HEADER - dùng TableLayoutPanel để chữ không bị cắt khi thay đổi DPI
        TableLayoutPanel headerPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = _backgroundColor,
            Padding = new Padding(10, 5, 8, 4),
            Margin = new Padding(0)
        };
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        headerPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        headerPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40));

        Label lblTitle = new Label
        {
            Text = "QUẢN LÝ SUẤT CHIẾU",
            Dock = DockStyle.Fill,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 21F, FontStyle.Bold),
            ForeColor = Color.White,
            Margin = new Padding(0)
        };

        Label lblDescription = new Label
        {
            Text = "Lập lịch chiếu phim và kiểm tra lịch phòng",
            Dock = DockStyle.Fill,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 10F, FontStyle.Regular),
            ForeColor = Color.FromArgb(148, 163, 184),
            Margin = new Padding(0)
        };

        headerPanel.Controls.Add(lblTitle, 0, 0);
        headerPanel.Controls.Add(lblDescription, 0, 1);

        // THỐNG KÊ

        _statisticsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = _backgroundColor
        };

        for (int i = 0; i < 3; i++)
        {
            _statisticsPanel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 33.33f));
        }

        _statisticsPanel.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        _cardTotal = CreateStatisticCard(
            "TỔNG SUẤT CHIẾU",
            Color.FromArgb(37, 99, 235),
            out _lblTotal
        );

        _cardToday = CreateStatisticCard(
            "SUẤT CHIẾU HÔM NAY",
            Color.FromArgb(16, 185, 129),
            out _lblToday
        );

        _cardRooms = CreateStatisticCard(
            "PHÒNG ĐANG QUẢN LÝ",
            Color.FromArgb(139, 92, 246),
            out _lblRooms
        );

        _cardTotal.Margin = new Padding(0, 5, 10, 5);
        _cardToday.Margin = new Padding(5, 5, 5, 5);
        _cardRooms.Margin = new Padding(10, 5, 0, 5);

        _statisticsPanel.Controls.Add(_cardTotal, 0, 0);
        _statisticsPanel.Controls.Add(_cardToday, 1, 0);
        _statisticsPanel.Controls.Add(_cardRooms, 2, 0);

        // TÌM KIẾM VÀ LỌC

        _filterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = _panelColor,
            Padding = new Padding(12, 12, 5, 0),
            WrapContents = true,
            AutoScroll = true
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

        _filterPanel.Controls.Add(lblSearch);
        _filterPanel.Controls.Add(_txtSearch);
        _filterPanel.Controls.Add(lblFilterDate);
        _filterPanel.Controls.Add(_dtpFilterDate);

        // FORM THÔNG TIN SUẤT CHIẾU

        _editorPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = _panelColor,
            Padding = new Padding(12),
            AutoScroll = true
        };

        Label lblEditorTitle = new Label
        {
            Text = "THÔNG TIN SUẤT CHIẾU",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 28
        };

        // BỐ CỤC NHẬP LIỆU

        _fieldsPanel = new TableLayoutPanel
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
            _fieldsPanel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 25));
        }

        _fieldsPanel.RowStyles.Add(
            new RowStyle(SizeType.Percent, 50));

        _fieldsPanel.RowStyles.Add(
            new RowStyle(SizeType.Percent, 50));

        // PHIM

        _cboMovie = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9)
        };

        _fieldsPanel.Controls.Add(
            CreateEditorField("Phim:", _cboMovie, 230), 0, 0);

        // PHÒNG CHIẾU

        _cboRoom = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9)
        };

        _fieldsPanel.Controls.Add(
            CreateEditorField("Phòng chiếu:", _cboRoom, 180), 1, 0);

        // NGÀY CHIẾU

        _dtpDate = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Font = new Font("Segoe UI", 9),
            Value = DateTime.Today.AddDays(1)
        };

        _fieldsPanel.Controls.Add(
            CreateEditorField("Ngày chiếu:", _dtpDate, 150), 2, 0);

        // GIỜ BẮT ĐẦU

        _dtpStartTime = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "HH:mm",
            ShowUpDown = true,
            Font = new Font("Segoe UI", 9),
            Value = DateTime.Today.AddHours(9)
        };

        _fieldsPanel.Controls.Add(
            CreateEditorField("Giờ bắt đầu:", _dtpStartTime, 130), 3, 0);

        // ĐỊNH DẠNG

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

        _fieldsPanel.Controls.Add(
            CreateEditorField("Định dạng:", _cboFormat, 130), 0, 1);

        // GIÁ VÉ

        _numPrice = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 1000000,
            Increment = 5000,
            Value = 75000,
            ThousandsSeparator = true,
            Font = new Font("Segoe UI", 9)
        };

        _fieldsPanel.Controls.Add(
            CreateEditorField("Giá vé (VNĐ):", _numPrice, 150), 1, 1);

        // GIỜ KẾT THÚC DỰ KIẾN

        _lblEndPreview = new Label
        {
            Text = "--:--",
            ForeColor = Color.FromArgb(52, 211, 153),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _fieldsPanel.Controls.Add(
            CreateEditorField(
                "Giờ kết thúc dự kiến:",
                _lblEndPreview,
                180), 2, 1);

        // KIỂM TRA LỊCH

        Label lblScheduleInfo = new Label
        {
            Text = "Tự động kiểm tra trùng lịch khi lưu",
            ForeColor = Color.FromArgb(52, 211, 153),
            Font = new Font("Segoe UI", 9),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _fieldsPanel.Controls.Add(
            CreateEditorField(
                "Kiểm tra lịch:",
                lblScheduleInfo,
                220), 3, 1);

        // CÁC NÚT CHỨC NĂNG

        _actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 43,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = true,
            AutoScroll = true,
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

        _actionPanel.Controls.Add(_btnDelete);
        _actionPanel.Controls.Add(_btnCancel);
        _actionPanel.Controls.Add(_btnEdit);
        _actionPanel.Controls.Add(_btnSave);

        _editorPanel.Controls.Add(_fieldsPanel);
        _editorPanel.Controls.Add(_actionPanel);
        _editorPanel.Controls.Add(lblEditorTitle);

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
            EnableHeadersVisualStyles = false,
            ScrollBars = ScrollBars.Both,
            MinimumSize = new Size(0, 120)
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

        _dgvShowtimes.ColumnHeadersHeight = 44;

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
        _dgvShowtimes.Columns.Insert(0, new DataGridViewTextBoxColumn
        {
            Name = "STT",
            HeaderText = "STT",
            ReadOnly = true,
            Width = 55,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _dgvShowtimes.Columns["Id"]!.Visible = false;

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

        _mainLayout.Controls.Add(headerPanel, 0, 0);
        _mainLayout.Controls.Add(_statisticsPanel, 0, 1);
        _mainLayout.Controls.Add(_filterPanel, 0, 2);
        _mainLayout.Controls.Add(_editorPanel, 0, 3);
        _mainLayout.Controls.Add(_dgvShowtimes, 0, 4);
        _mainLayout.Controls.Add(_lblResult, 0, 5);

        _scrollHost.Controls.Add(_mainLayout);
        Controls.Add(_scrollHost);

        _scrollHost.SizeChanged += (_, _) => ApplyResponsiveLayout();

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

    // =========================================
    // 5. RESPONSIVE LAYOUT
    // =========================================

    private void ShowtimeManagementUC_SizeChanged(
        object? sender,
        EventArgs e)
    {
        ApplyResponsiveLayout();
    }

    private void ApplyResponsiveLayout()
    {
        if (_scrollHost == null || _mainLayout == null || _fieldsPanel == null ||
            _filterPanel == null || _editorPanel == null ||
            _actionPanel == null || _dgvShowtimes == null ||
            _statisticsPanel == null || _isApplyingResponsiveLayout)
            return;

        _isApplyingResponsiveLayout = true;
        try
        {
            int width = Math.Max(320, _scrollHost.ClientSize.Width);
            int viewportHeight = Math.Max(320, _scrollHost.ClientSize.Height);
            bool narrow = width < 560;
            bool compact = width < 1050;

            _mainLayout.SuspendLayout();
            _statisticsPanel.SuspendLayout();
            _fieldsPanel.SuspendLayout();
            _filterPanel.SuspendLayout();
            _actionPanel.SuspendLayout();

            _mainLayout.RowStyles[0].Height = narrow ? 82 : 88;
            _mainLayout.RowStyles[1].Height = narrow ? 300 : 132;
            _mainLayout.RowStyles[2].Height = narrow ? 112 : (compact ? 82 : 64);
            _mainLayout.RowStyles[3].Height = narrow ? 610 : (compact ? 370 : 250);
            _mainLayout.RowStyles[4].SizeType = SizeType.Percent;
            _mainLayout.RowStyles[4].Height = 100;
            _mainLayout.RowStyles[5].Height = 32;

            // Chiều cao nội dung tối thiểu để không ép các khu vực vào màn hình thấp.
            // Phần bảng vẫn co giãn và có thanh cuộn riêng khi cần.
            int fixedContentHeight = narrow
                ? 82 + 300 + 112 + 610 + 32
                : compact
                    ? 88 + 132 + 82 + 370 + 32
                    : 88 + 132 + 64 + 250 + 32;
            int minimumGridHeight = narrow ? 300 : 280;
            int desiredContentHeight = fixedContentHeight + minimumGridHeight;
            _mainLayout.Width = Math.Max(320,
                _scrollHost.ClientSize.Width -
                (desiredContentHeight > viewportHeight ? SystemInformation.VerticalScrollBarWidth : 0));
            _mainLayout.Height = Math.Max(viewportHeight, desiredContentHeight);
            _mainLayout.Location = new Point(0, 0);

            // Thẻ thống kê: 3 cột trên màn hình rộng, xếp dọc trên màn hình hẹp.
            _statisticsPanel.Controls.Clear();
            _statisticsPanel.ColumnStyles.Clear();
            _statisticsPanel.RowStyles.Clear();
            if (narrow)
            {
                _statisticsPanel.ColumnCount = 1;
                _statisticsPanel.RowCount = 3;
                _statisticsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                for (int i = 0; i < 3; i++)
                    _statisticsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333f));

                _cardTotal.Margin = new Padding(0, 3, 0, 3);
                _cardToday.Margin = new Padding(0, 3, 0, 3);
                _cardRooms.Margin = new Padding(0, 3, 0, 3);
                _statisticsPanel.Controls.Add(_cardTotal, 0, 0);
                _statisticsPanel.Controls.Add(_cardToday, 0, 1);
                _statisticsPanel.Controls.Add(_cardRooms, 0, 2);
            }
            else
            {
                _statisticsPanel.ColumnCount = 3;
                _statisticsPanel.RowCount = 1;
                for (int i = 0; i < 3; i++)
                    _statisticsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
                _statisticsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

                _cardTotal.Margin = new Padding(0, 4, 6, 4);
                _cardToday.Margin = new Padding(3, 4, 3, 4);
                _cardRooms.Margin = new Padding(6, 4, 0, 4);
                _statisticsPanel.Controls.Add(_cardTotal, 0, 0);
                _statisticsPanel.Controls.Add(_cardToday, 1, 0);
                _statisticsPanel.Controls.Add(_cardRooms, 2, 0);
            }

            // Biểu mẫu: 4 cột, 2 cột hoặc 1 cột tùy chiều rộng.
            _fieldsPanel.Controls.Cast<Control>().ToList().ForEach(c =>
                _fieldsPanel.SetCellPosition(c, new TableLayoutPanelCellPosition(0, 0)));
            _fieldsPanel.ColumnStyles.Clear();
            _fieldsPanel.RowStyles.Clear();

            int columns = narrow ? 1 : (compact ? 2 : 4);
            int rows = 8 / columns;
            _fieldsPanel.ColumnCount = columns;
            _fieldsPanel.RowCount = rows;

            for (int i = 0; i < columns; i++)
                _fieldsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columns));
            for (int i = 0; i < rows; i++)
                _fieldsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rows));

            for (int i = 0; i < _fieldsPanel.Controls.Count; i++)
            {
                Control field = _fieldsPanel.Controls[i];
                _fieldsPanel.SetCellPosition(field,
                    new TableLayoutPanelCellPosition(i % columns, i / columns));
                field.MinimumSize = new Size(0, 0);
                field.Margin = new Padding(5, 3, 5, 3);
            }

            // Tìm kiếm và lọc ngày: tự xuống dòng khi không đủ chỗ.
            _filterPanel.Controls.Clear();
            _filterPanel.WrapContents = true;
            _filterPanel.AutoScroll = true;
            _filterPanel.Padding = new Padding(12, 8, 8, 6);

            int searchWidth = narrow ? Math.Max(180, width - 150)
                : compact ? Math.Max(220, width - 360)
                : Math.Max(280, width - 520);

            _txtSearch.Width = searchWidth;
            _txtSearch.Height = 30;
            _txtSearch.Font = new Font("Segoe UI", 10F);
            _dtpFilterDate.Width = narrow ? Math.Max(170, width - 150) : 170;

            Label searchLabel = CreateLabel("Tìm kiếm:", 8);
            Label dateLabel = CreateLabel("Lọc ngày:", 8);
            searchLabel.Margin = new Padding(3, 8, 8, 3);
            _txtSearch.Margin = new Padding(0, 3, 18, 3);
            dateLabel.Margin = new Padding(3, 8, 8, 3);
            _dtpFilterDate.Margin = new Padding(0, 3, 8, 3);

            _filterPanel.Controls.Add(searchLabel);
            _filterPanel.Controls.Add(_txtSearch);
            _filterPanel.Controls.Add(dateLabel);
            _filterPanel.Controls.Add(_dtpFilterDate);

            // Các nút thao tác.
            _actionPanel.Height = narrow ? 82 : (compact ? 76 : 43);
            _actionPanel.WrapContents = true;
            _actionPanel.AutoScroll = true;
            _actionPanel.FlowDirection = FlowDirection.RightToLeft;
            foreach (Control control in _actionPanel.Controls)
            {
                control.Margin = new Padding(4, 3, 4, 3);
                control.Width = narrow ? Math.Max(115, (width - 55) / 2) : 140;
                control.Height = 35;
                control.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            }

            // Bảng dữ liệu: giữ đủ chiều rộng cột và cho cuộn ngang ở màn hình nhỏ.
            _dgvShowtimes.AutoSizeColumnsMode = compact
                ? DataGridViewAutoSizeColumnsMode.None
                : DataGridViewAutoSizeColumnsMode.Fill;
            _dgvShowtimes.DefaultCellStyle.Font =
                new Font("Segoe UI", narrow ? 8.5F : 9.5F);
            _dgvShowtimes.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", narrow ? 8.5F : 9.5F, FontStyle.Bold);
            _dgvShowtimes.ColumnHeadersHeight = 44;
            _dgvShowtimes.RowTemplate.Height = 40;

            if (compact)
            {
                int[] widths = { 75, 190, 125, 110, 100, 100, 85, 115 };
                for (int i = 0; i < _dgvShowtimes.Columns.Count && i < widths.Length; i++)
                    _dgvShowtimes.Columns[i].Width = widths[i];
            }

            _fieldsPanel.ResumeLayout(true);
            _filterPanel.ResumeLayout(true);
            _actionPanel.ResumeLayout(true);
            _statisticsPanel.ResumeLayout(true);
            _mainLayout.ResumeLayout(true);
        }
        finally
        {
            _isApplyingResponsiveLayout = false;
        }
    }

    // =========================================
    // 6. TẠO LABEL
    // =========================================

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

    // =========================================
    // 7. TẠO Ô NHẬP LIỆU
    // =========================================

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
            Margin = new Padding(6, 2, 6, 2),
            MinimumSize = new Size(0, 0),
            Padding = new Padding(2, 1, 2, 2)
        };

        Label label = new Label
        {
            Text = title,
            ForeColor = _textColor,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 25,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        input.Dock = DockStyle.Bottom;
        input.Height = 32;
        input.Margin = new Padding(0);

        field.Controls.Add(input);
        field.Controls.Add(label);

        return field;
    }

    // =========================================
    // 8. TẠO NÚT
    // =========================================

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

    // =========================================
    // 9. TẠO THẺ THỐNG KÊ
    // =========================================

    private Panel CreateStatisticCard(
        string title,
        Color cardAccentColor,
        out Label valueLabel)
    {
        Panel card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = _panelColor,
            Margin = new Padding(0),
            MinimumSize = new Size(0, 0)
        };

        Panel accentBar = new Panel
        {
            Width = 5,
            Dock = DockStyle.Left,
            BackColor = cardAccentColor
        };

        string description = title switch
        {
            "TỔNG SUẤT CHIẾU" => "Tất cả lịch chiếu đã tạo",
            "SUẤT CHIẾU HÔM NAY" => "Số suất chiếu trong ngày",
            "PHÒNG ĐANG QUẢN LÝ" => "Tổng số phòng chiếu hiện có",
            _ => "Thông tin thống kê"
        };

        TableLayoutPanel textLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = _panelColor,
            Padding = new Padding(16, 7, 62, 6),
            Margin = new Padding(0)
        };
        textLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        textLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
        textLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        textLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 28));

        Label lblTitle = new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            AutoSize = false,
            ForeColor = Color.FromArgb(226, 232, 240),
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Margin = new Padding(0)
        };

        valueLabel = new Label
        {
            Text = "0",
            Dock = DockStyle.Fill,
            AutoSize = false,
            Font = new Font("Segoe UI", 23F, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0)
        };

        Label lblDescription = new Label
        {
            Text = description,
            Dock = DockStyle.Fill,
            AutoSize = false,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = Color.FromArgb(203, 213, 225),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Margin = new Padding(0)
        };

        textLayout.Controls.Add(lblTitle, 0, 0);
        textLayout.Controls.Add(valueLabel, 0, 1);
        textLayout.Controls.Add(lblDescription, 0, 2);

        Color iconBackground = Color.FromArgb(
            (30 + cardAccentColor.R) / 2,
            (41 + cardAccentColor.G) / 2,
            (59 + cardAccentColor.B) / 2);

        Panel iconPanel = new Panel
        {
            Size = new Size(42, 42),
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
            Font = new Font("Segoe UI Symbol", 21F, FontStyle.Bold),
            ForeColor = cardAccentColor,
            TextAlign = ContentAlignment.MiddleCenter
        };
        iconPanel.Controls.Add(lblIcon);

        void PositionIcon()
        {
            iconPanel.Location = new Point(
                Math.Max(8, card.ClientSize.Width - iconPanel.Width - 12),
                Math.Max(4, (card.ClientSize.Height - iconPanel.Height) / 2));
        }

        card.Resize += (s, e) => PositionIcon();
        card.Controls.Add(textLayout);
        card.Controls.Add(iconPanel);
        card.Controls.Add(accentBar);
        PositionIcon();

        card.Paint += (s, e) =>
        {
            using Pen pen = new Pen(Color.FromArgb(51, 65, 85));
            e.Graphics.DrawRectangle(pen, 0, 0,
                Math.Max(0, card.Width - 1), Math.Max(0, card.Height - 1));
        };

        return card;
    }

    // =========================================
    // 10. TẢI DỮ LIỆU TỪ SQLITE
    // =========================================

    private void LoadDatabaseData()
    {
        try
        {
            _movies.Clear();
            _rooms.Clear();
            _showtimes.Clear();

            _movies.AddRange(_movieRepository.GetAll());
            _rooms.AddRange(_roomRepository.GetAll());
            _showtimes.AddRange(_showtimeRepository.GetAll());
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
    // 11. ĐỔ DỮ LIỆU VÀO COMBOBOX
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
    // 12. CẬP NHẬT GIỜ KẾT THÚC DỰ KIẾN
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

        for (int index = 0; index < result.Count; index++)
        {
            Showtime showtime = result[index];
            Movie? movie = _movies.FirstOrDefault(
                m => m.Id == showtime.MovieId
            );

            Room? room = _rooms.FirstOrDefault(
                r => r.Id == showtime.RoomId
            );

            string format = showtime.Format;

            _dgvShowtimes.Rows.Add(
                index + 1,
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
            roomId = checked((int)room.Id);
        }
        catch (OverflowException)
        {
            MessageBox.Show(
                "Mã phòng chiếu vượt quá giới hạn cho phép."
            );
            return;
        }

        string format =
            _cboFormat.SelectedItem?.ToString() ?? "2D";

        try
        {
            _showtimes.Clear();
            _showtimes.AddRange(_showtimeRepository.GetAll());

            bool hasConflict = _showtimeRepository.HasScheduleConflict(
                roomId,
                start,
                end,
                _editingShowtimeId
            );

            if (hasConflict)
            {
                MessageBox.Show(
                    $"Phòng {room.Name} đã có suất chiếu trùng lịch!\n\n" +
                    $"Thời gian dự kiến: {start:dd/MM/yyyy HH:mm} - " +
                    $"{end:dd/MM/yyyy HH:mm}",
                    "Trùng lịch chiếu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                LoadDatabaseData();
                RefreshShowtimeTable();

                return;
            }

            if (_editingShowtimeId.HasValue)
            {
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
                current.Format = format;

                bool updated = _showtimeRepository.Update(current);

                if (!updated)
                {
                    MessageBox.Show(
                        "Không thể cập nhật suất chiếu!"
                    );

                    return;
                }

                MessageBox.Show(
                    "Cập nhật suất chiếu thành công!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            else
            {
                Showtime newShowtime = new Showtime
                {
                    MovieId = movie.Id,
                    RoomId = roomId,
                    StartTime = start,
                    EndTime = end,
                    BasePrice = _numPrice.Value,
                    Format = format
                };

                _showtimeRepository.Add(newShowtime);

                MessageBox.Show(
                    "Tạo suất chiếu thành công!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }

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

        _cboFormat.SelectedItem = showtime.Format;

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