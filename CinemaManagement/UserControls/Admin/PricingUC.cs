using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Models;
using CinemaManagement.Services;

namespace CinemaManagement.UserControls.Admin;

public partial class PricingUC : UserControl
{
    // =========================================
    // 1. DỮ LIỆU VÀ REPOSITORY
    // =========================================

    private readonly PricingService _pricingService = new();
    private readonly List<PricingRule> _rules = new();

    private int? _editingId;
    private bool _isApplyingResponsiveLayout;

    // =========================================
    // 2. CÁC THÀNH PHẦN GIAO DIỆN
    // =========================================

    private DataGridView _dgvPricing = null!;
    private TextBox _txtSearch = null!;

    private ComboBox _cboFilterFormat = null!;
    private ComboBox _cboFilterDay = null!;

    private ComboBox _cboFormat = null!;
    private ComboBox _cboSeatType = null!;
    private ComboBox _cboDayType = null!;
    private ComboBox _cboTimeSlot = null!;

    private NumericUpDown _numBasePrice = null!;
    private NumericUpDown _numSurcharge = null!;

    private Label _lblTotalRules = null!;
    private Label _lblMinPrice = null!;
    private Label _lblMaxPrice = null!;
    private Label _lblPricePreview = null!;
    private Label _lblResult = null!;

    private Button _btnSave = null!;
    private Button _btnEdit = null!;
    private Button _btnDelete = null!;
    private Button _btnCancel = null!;

    private Panel _rootScrollPanel = null!;

    private TableLayoutPanel _mainLayout = null!;
    private TableLayoutPanel _statisticPanel = null!;
    private TableLayoutPanel _editorLayout = null!;
    private TableLayoutPanel _fieldsPanel = null!;

    private Control[] _editorFields = Array.Empty<Control>();

    private FlowLayoutPanel _filterPanel = null!;
    private FlowLayoutPanel _actionPanel = null!;

    private Panel _cardTotal = null!;
    private Panel _cardMin = null!;
    private Panel _cardMax = null!;

    private Label _lblHeaderTitle = null!;
    private Label _lblHeaderDescription = null!;

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

    public PricingUC()
    {
        InitializeComponent();

        Dock = DockStyle.Fill;
        BackColor = _backgroundColor;
        AutoScaleMode = AutoScaleMode.Dpi;
        TabStop = true;

        BuildInterface();

        SizeChanged += PricingUC_SizeChanged;

        HandleCreated += (s, e) =>
        {
            BeginInvoke(new Action(() =>
            {
                if (IsDisposed)
                    return;

                ApplyResponsiveLayout();
                ClearInitialComboHighlight();
            }));
        };

        VisibleChanged += (s, e) =>
        {
            if (Visible && IsHandleCreated && !IsDisposed)
            {
                BeginInvoke(new Action(ClearInitialComboHighlight));
            }
        };

        LoadPricingData();
        UpdatePricePreview();
    }

    // =========================================
    // 4. BỎ TÔ XANH CÁC COMBO KHI VỪA MỞ TRANG
    // =========================================

    private void ClearInitialComboHighlight()
    {
        if (IsDisposed || !IsHandleCreated)
            return;

        // Bỏ trạng thái chọn chữ trong hai ComboBox.
        if (_cboDayType != null && !_cboDayType.IsDisposed)
        {
            _cboDayType.SelectionLength = 0;
            _cboDayType.SelectionStart = 0;
        }

        if (_cboTimeSlot != null && !_cboTimeSlot.IsDisposed)
        {
            _cboTimeSlot.SelectionLength = 0;
            _cboTimeSlot.SelectionStart = 0;
        }

        // Chuyển focus về UserControl để ComboBox không còn highlight xanh.
        Focus();
        Select();
    }

    // =========================================
    // 5. XÂY DỰNG GIAO DIỆN
    // =========================================

    private void BuildInterface()
    {
        _mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = _backgroundColor,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        _mainLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        _mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 85));

        _mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 115));

        _mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 65));

        _mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 260));

        _mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        _mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 32));

        // =====================================
        // 4.1 HEADER
        // =====================================

        Panel headerPanel = new()
        {
            Dock = DockStyle.Fill,
            BackColor = _backgroundColor,
            Padding = new Padding(12, 5, 8, 5)
        };

        _lblHeaderTitle = new Label
        {
            Text = "QUẢN LÝ GIÁ VÉ",
            Font = new Font("Segoe UI", 21, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(5, 8)
        };

        _lblHeaderDescription = new Label
        {
            Text = "Thiết lập bảng giá vé theo định dạng, loại ghế và thời gian",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(7, 51)
        };

        headerPanel.Controls.Add(_lblHeaderTitle);
        headerPanel.Controls.Add(_lblHeaderDescription);

        // =====================================
        // 4.2 THỐNG KÊ
        // =====================================

        _statisticPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = _backgroundColor,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        for (int i = 0; i < 3; i++)
        {
            _statisticPanel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 33.33f));
        }

        _statisticPanel.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        _cardTotal = CreateStatisticCard(
            "TỔNG QUY TẮC GIÁ",
            "Các mức giá đang thiết lập",
            Color.FromArgb(37, 99, 235),
            out _lblTotalRules
        );

        _cardMin = CreateStatisticCard(
            "GIÁ THẤP NHẤT",
            "Mức giá vé thấp nhất",
            Color.FromArgb(16, 185, 129),
            out _lblMinPrice
        );

        _cardMax = CreateStatisticCard(
            "GIÁ CAO NHẤT",
            "Mức giá vé cao nhất",
            Color.FromArgb(139, 92, 246),
            out _lblMaxPrice
        );

        _cardTotal.Margin = new Padding(0, 5, 10, 5);
        _cardMin.Margin = new Padding(5);
        _cardMax.Margin = new Padding(10, 5, 0, 5);

        _statisticPanel.Controls.Add(_cardTotal, 0, 0);
        _statisticPanel.Controls.Add(_cardMin, 1, 0);
        _statisticPanel.Controls.Add(_cardMax, 2, 0);

        // =====================================
        // 4.3 TÌM KIẾM VÀ LỌC
        // =====================================

        _filterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = _panelColor,
            Padding = new Padding(12, 10, 8, 5),
            WrapContents = true,
            AutoScroll = true,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0)
        };

        Label lblSearch = CreateLabel("Tìm kiếm:", 8);

        _txtSearch = new TextBox
        {
            Width = 240,
            Font = new Font("Segoe UI", 10),
            Margin = new Padding(0, 2, 20, 0)
        };

        Label lblFormatFilter = CreateLabel("Định dạng:", 8);

        _cboFilterFormat = new ComboBox
        {
            Width = 120,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9),
            Margin = new Padding(0, 2, 15, 0)
        };
        ApplyConsistentComboStyle(_cboFilterFormat);

        _cboFilterFormat.Items.AddRange(new object[]
        {
            "Tất cả",
            "2D",
            "3D"
        });

        _cboFilterFormat.SelectedIndex = 0;

        Label lblDayFilter = CreateLabel("Ngày áp dụng:", 8);

        _cboFilterDay = new ComboBox
        {
            Width = 155,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9),
            Margin = new Padding(0, 2, 5, 0)
        };
        ApplyConsistentComboStyle(_cboFilterDay);

        _cboFilterDay.Items.AddRange(new object[]
        {
            "Tất cả",
            "Ngày thường",
            "Cuối tuần",
            "Ngày lễ",
            "Tết Nguyên Đán",
            "Giỗ Tổ Hùng Vương",
            "30/4 - 1/5",
            "Quốc khánh 2/9",
            "Ngày đặc biệt"
        });

        _cboFilterDay.SelectedIndex = 0;

        _filterPanel.Controls.Add(lblSearch);
        _filterPanel.Controls.Add(_txtSearch);
        _filterPanel.Controls.Add(lblFormatFilter);
        _filterPanel.Controls.Add(_cboFilterFormat);
        _filterPanel.Controls.Add(lblDayFilter);
        _filterPanel.Controls.Add(_cboFilterDay);

        // =====================================
        // 4.4 FORM THIẾT LẬP GIÁ
        // =====================================

        Panel editorPanel = new()
        {
            Dock = DockStyle.Fill,
            BackColor = _panelColor,
            Padding = new Padding(12),
            AutoScroll = true,
            Margin = new Padding(0)
        };

        _editorLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = _panelColor,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        _editorLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        _editorLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 35));

        _editorLayout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        _editorLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 45));

        Label lblEditorTitle = new()
        {
            Text = "THIẾT LẬP QUY TẮC GIÁ VÉ",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0)
        };

        // =====================================
        // CÁC TRƯỜNG NHẬP LIỆU
        // =====================================

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

        for (int i = 0; i < 2; i++)
        {
            _fieldsPanel.RowStyles.Add(
                new RowStyle(SizeType.Percent, 50));
        }

        // ĐỊNH DẠNG PHIM

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
            CreateEditorField("Định dạng phim:", _cboFormat),
            0, 0
        );

        // LOẠI GHẾ

        _cboSeatType = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9)
        };

        _cboSeatType.Items.AddRange(new object[]
        {
            "Ghế thường",
            "Ghế VIP",
            "Ghế đôi"
        });

        _cboSeatType.SelectedIndex = 0;

        _fieldsPanel.Controls.Add(
            CreateEditorField("Loại ghế:", _cboSeatType),
            1, 0
        );

        // NGÀY ÁP DỤNG

        _cboDayType = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDown,
            Font = new Font("Segoe UI", 9),
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems
        };

        _cboDayType.Items.AddRange(new object[]
        {
            "Ngày thường",
            "Cuối tuần",
            "Ngày lễ",
            "Tết Nguyên Đán",
            "Giỗ Tổ Hùng Vương",
            "30/4 - 1/5",
            "Quốc khánh 2/9",
            "Ngày đặc biệt"
        });

        _cboDayType.SelectedIndex = 0;

        _fieldsPanel.Controls.Add(
            CreateEditorField("Ngày áp dụng:", _cboDayType),
            2, 0
        );

        // KHUNG GIỜ

        _cboTimeSlot = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDown,
            Font = new Font("Segoe UI", 9),
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems
        };

        _cboTimeSlot.Items.AddRange(new object[]
        {
            "Cả ngày",
            "Trước 12:00",
            "12:00 - 17:00",
            "Sau 17:00",
            "08:00 - 10:00",
            "10:00 - 12:00",
            "12:00 - 14:00",
            "14:00 - 17:00",
            "17:00 - 20:00",
            "20:00 - 23:59"
        });

        _cboTimeSlot.SelectedItem = "12:00 - 17:00";

        _fieldsPanel.Controls.Add(
            CreateEditorField("Khung giờ:", _cboTimeSlot),
            3, 0
        );

        // GIÁ CƠ BẢN

        _numBasePrice = new NumericUpDown
        {
            Minimum = 10000,
            Maximum = 1000000,
            Increment = 5000,
            Value = 75000,
            ThousandsSeparator = true,
            Font = new Font("Segoe UI", 9)
        };

        _fieldsPanel.Controls.Add(
            CreateEditorField("Giá cơ bản (VNĐ):", _numBasePrice),
            0, 1
        );

        // PHỤ THU

        _numSurcharge = new NumericUpDown
        {
            Minimum = -100000,
            Maximum = 500000,
            Increment = 5000,
            Value = 0,
            ThousandsSeparator = true,
            Font = new Font("Segoe UI", 9)
        };

        _fieldsPanel.Controls.Add(
            CreateEditorField("Phụ thu (VNĐ):", _numSurcharge),
            1, 1
        );

        // GIÁ CUỐI CÙNG

        _lblPricePreview = new Label
        {
            Text = "75.000 đ",
            ForeColor = Color.FromArgb(52, 211, 153),
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill
        };

        _fieldsPanel.Controls.Add(
            CreateEditorField(
                "Giá vé sau điều chỉnh:",
                _lblPricePreview),
            2, 1
        );

        // CÁCH TÍNH

        Label lblNote = new()
        {
            Text = "Giá cuối = Giá cơ bản + Phụ thu",
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Segoe UI", 9),
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill
        };

        _fieldsPanel.Controls.Add(
            CreateEditorField("Cách tính:", lblNote),
            3, 1
        );

        _editorFields = _fieldsPanel.Controls
            .Cast<Control>()
            .ToArray();

        // =====================================
        // CÁC NÚT CHỨC NĂNG
        // =====================================

        _actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = true,
            AutoScroll = true,
            BackColor = _panelColor,
            Padding = new Padding(0, 4, 0, 0),
            Margin = new Padding(0)
        };

        _btnDelete = CreateButton(
            "Xóa quy tắc",
            Color.FromArgb(220, 38, 38)
        );

        _btnCancel = CreateButton(
            "Hủy sửa",
            Color.FromArgb(71, 85, 105)
        );

        _btnEdit = CreateButton(
            "Sửa quy tắc",
            Color.FromArgb(217, 119, 6)
        );

        _btnSave = CreateButton(
            "Thêm quy tắc",
            _accentColor
        );

        _btnCancel.Visible = false;

        _actionPanel.Controls.Add(_btnDelete);
        _actionPanel.Controls.Add(_btnCancel);
        _actionPanel.Controls.Add(_btnEdit);
        _actionPanel.Controls.Add(_btnSave);

        _editorLayout.Controls.Add(lblEditorTitle, 0, 0);
        _editorLayout.Controls.Add(_fieldsPanel, 0, 1);
        _editorLayout.Controls.Add(_actionPanel, 0, 2);

        editorPanel.Controls.Add(_editorLayout);

        // =====================================
        // 4.5 BẢNG GIÁ VÉ
        // =====================================

        _dgvPricing = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = _panelColor,
            BorderStyle = BorderStyle.None,

            AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill,

            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,

            ReadOnly = true,
            MultiSelect = false,

            SelectionMode =
                DataGridViewSelectionMode.FullRowSelect,

            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,
            ScrollBars = ScrollBars.Both,

            ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        };

        _dgvPricing.ColumnHeadersDefaultCellStyle.BackColor =
            _accentColor;

        _dgvPricing.ColumnHeadersDefaultCellStyle.ForeColor =
            Color.White;

        _dgvPricing.ColumnHeadersDefaultCellStyle.Font =
            new Font("Segoe UI", 9, FontStyle.Bold);

        _dgvPricing.ColumnHeadersHeight = 44;

        _dgvPricing.DefaultCellStyle.BackColor = _panelColor;
        _dgvPricing.DefaultCellStyle.ForeColor = Color.White;

        _dgvPricing.DefaultCellStyle.SelectionBackColor =
            Color.FromArgb(51, 65, 85);

        _dgvPricing.DefaultCellStyle.SelectionForeColor =
            Color.White;

        _dgvPricing.AlternatingRowsDefaultCellStyle.BackColor =
            Color.FromArgb(37, 49, 68);

        _dgvPricing.DefaultCellStyle.Font =
            new Font("Segoe UI", 9);

        _dgvPricing.RowTemplate.Height = 40;

        _dgvPricing.Columns.Add("Id", "Mã giá");
        _dgvPricing.Columns.Add("Format", "Định dạng");
        _dgvPricing.Columns.Add("SeatType", "Loại ghế");
        _dgvPricing.Columns.Add("DayType", "Ngày áp dụng");
        _dgvPricing.Columns.Add("TimeSlot", "Khung giờ");
        _dgvPricing.Columns.Add("BasePrice", "Giá cơ bản");
        _dgvPricing.Columns.Add("Surcharge", "Phụ thu");
        _dgvPricing.Columns.Add("FinalPrice", "Giá cuối cùng");
        _dgvPricing.Columns.Insert(0, new DataGridViewTextBoxColumn
        {
            Name = "STT",
            HeaderText = "STT",
            ReadOnly = true,
            Width = 55,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _dgvPricing.Columns["Id"]!.Visible = false;

        // =====================================
        // 4.6 CHÂN TRANG
        // =====================================

        _lblResult = new Label
        {
            Text = "Tổng số quy tắc giá: 0",
            Dock = DockStyle.Fill,
            ForeColor = _textColor,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(5, 0, 0, 0)
        };

        // =====================================
        // 4.7 GHÉP GIAO DIỆN
        // =====================================

        _mainLayout.Controls.Add(headerPanel, 0, 0);
        _mainLayout.Controls.Add(_statisticPanel, 0, 1);
        _mainLayout.Controls.Add(_filterPanel, 0, 2);
        _mainLayout.Controls.Add(editorPanel, 0, 3);
        _mainLayout.Controls.Add(_dgvPricing, 0, 4);
        _mainLayout.Controls.Add(_lblResult, 0, 5);

        // =====================================
        // PANEL CUỘN TOÀN BỘ GIAO DIỆN
        // =====================================

        _rootScrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = _backgroundColor,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        _mainLayout.Dock = DockStyle.Top;
        _mainLayout.Anchor =
            AnchorStyles.Top | AnchorStyles.Left;

        _mainLayout.Width = Math.Max(320, ClientSize.Width);
        _mainLayout.Height = Math.Max(1200, ClientSize.Height);

        _rootScrollPanel.Controls.Add(_mainLayout);
        Controls.Add(_rootScrollPanel);

        _rootScrollPanel.SizeChanged += (s, e) =>
        {
            UpdateScrollLayout();
        };

        // =====================================
        // 4.8 GẮN SỰ KIỆN
        // =====================================

        _txtSearch.TextChanged += (s, e) =>
            RefreshPricingTable();

        _cboFilterFormat.SelectedIndexChanged += (s, e) =>
            RefreshPricingTable();

        _cboFilterDay.SelectedIndexChanged += (s, e) =>
            RefreshPricingTable();

        _numBasePrice.ValueChanged += (s, e) =>
            UpdatePricePreview();

        _numSurcharge.ValueChanged += (s, e) =>
            UpdatePricePreview();

        _btnSave.Click += BtnSave_Click;
        _btnEdit.Click += BtnEdit_Click;
        _btnCancel.Click += BtnCancel_Click;
        _btnDelete.Click += BtnDelete_Click;
    }

    // =========================================
    // 5. CẬP NHẬT KÍCH THƯỚC VÙNG CUỘN
    // =========================================

    private void UpdateScrollLayout()
    {
        if (_rootScrollPanel == null || _mainLayout == null)
            return;

        int width = _rootScrollPanel.ClientSize.Width;
        int height = _rootScrollPanel.ClientSize.Height;

        if (width <= 0 || height <= 0)
            return;

        bool mobile = width < 650;
        bool tablet = width >= 650 && width < 1050;

        int headerHeight = mobile ? 78 : 85;
        int statisticHeight = mobile ? 225 : 115;
        int filterHeight = mobile ? 150 : tablet ? 95 : 65;
        int editorHeight = mobile ? 650 : tablet ? 410 : 260;
        int tableMinHeight = 350;
        int footerHeight = 32;

        int requiredHeight =
            headerHeight +
            statisticHeight +
            filterHeight +
            editorHeight +
            tableMinHeight +
            footerHeight;

        int contentHeight = Math.Max(height, requiredHeight);

        _mainLayout.SuspendLayout();

        try
        {
            _mainLayout.Dock = DockStyle.Top;
            _mainLayout.Width = width;
            _mainLayout.Height = contentHeight;
        }
        finally
        {
            _mainLayout.ResumeLayout(true);
        }
    }

    // =========================================
    // 6. RESPONSIVE LAYOUT
    // =========================================

    private void PricingUC_SizeChanged(object? sender, EventArgs e)
    {
        ApplyResponsiveLayout();
    }

    private void ApplyResponsiveLayout()
    {
        if (_mainLayout == null ||
            _statisticPanel == null ||
            _editorLayout == null ||
            _fieldsPanel == null ||
            _filterPanel == null ||
            _actionPanel == null ||
            _dgvPricing == null ||
            _isApplyingResponsiveLayout ||
            _editorFields.Length != 8)
        {
            return;
        }

        if (_rootScrollPanel == null ||
            _rootScrollPanel.ClientSize.Width <= 0)
        {
            return;
        }

        _isApplyingResponsiveLayout = true;

        try
        {
            int width = _rootScrollPanel.ClientSize.Width;

            bool mobile = width < 650;
            bool tablet = width >= 650 && width < 1050;

            _mainLayout.SuspendLayout();
            _statisticPanel.SuspendLayout();
            _fieldsPanel.SuspendLayout();
            _filterPanel.SuspendLayout();
            _actionPanel.SuspendLayout();
            _editorLayout.SuspendLayout();

            // =================================
            // 6.1 CHIỀU CAO CÁC KHU VỰC
            // =================================

            _mainLayout.RowStyles[0].Height =
                mobile ? 78 : 85;

            _mainLayout.RowStyles[1].Height =
                mobile ? 225 : 115;

            _mainLayout.RowStyles[2].Height =
                mobile ? 150 : (tablet ? 95 : 65);

            _mainLayout.RowStyles[3].Height =
                mobile ? 650 : (tablet ? 410 : 260);

            _mainLayout.RowStyles[4].SizeType =
                SizeType.Percent;

            _mainLayout.RowStyles[4].Height = 100;
            _mainLayout.RowStyles[5].Height = 32;

            _editorLayout.RowStyles[0].Height =
                mobile ? 38 : 35;

            _editorLayout.RowStyles[2].Height =
                mobile ? 82 : 45;

            // =================================
            // 6.2 HEADER VÀ FONT
            // =================================

            _lblHeaderTitle.Font = new Font(
                "Segoe UI",
                mobile ? 16 : tablet ? 18 : 21,
                FontStyle.Bold);

            _lblHeaderDescription.Font = new Font(
                "Segoe UI",
                mobile ? 8 : 9);

            // =================================
            // 6.3 THẺ THỐNG KÊ
            // =================================

            _statisticPanel.Controls.Clear();
            _statisticPanel.ColumnStyles.Clear();
            _statisticPanel.RowStyles.Clear();

            if (mobile)
            {
                _statisticPanel.ColumnCount = 1;
                _statisticPanel.RowCount = 3;

                _statisticPanel.ColumnStyles.Add(
                    new ColumnStyle(SizeType.Percent, 100));

                for (int i = 0; i < 3; i++)
                {
                    _statisticPanel.RowStyles.Add(
                        new RowStyle(SizeType.Percent, 33.33f));
                }

                _cardTotal.Margin = new Padding(3);
                _cardMin.Margin = new Padding(3);
                _cardMax.Margin = new Padding(3);

                _statisticPanel.Controls.Add(_cardTotal, 0, 0);
                _statisticPanel.Controls.Add(_cardMin, 0, 1);
                _statisticPanel.Controls.Add(_cardMax, 0, 2);
            }
            else
            {
                _statisticPanel.ColumnCount = 3;
                _statisticPanel.RowCount = 1;

                for (int i = 0; i < 3; i++)
                {
                    _statisticPanel.ColumnStyles.Add(
                        new ColumnStyle(SizeType.Percent, 33.33f));
                }

                _statisticPanel.RowStyles.Add(
                    new RowStyle(SizeType.Percent, 100));

                _cardTotal.Margin = new Padding(0, 5, 10, 5);
                _cardMin.Margin = new Padding(5);
                _cardMax.Margin = new Padding(10, 5, 0, 5);

                _statisticPanel.Controls.Add(_cardTotal, 0, 0);
                _statisticPanel.Controls.Add(_cardMin, 1, 0);
                _statisticPanel.Controls.Add(_cardMax, 2, 0);
            }

            // =================================
            // 6.4 FORM: 4 CỘT / 2 CỘT / 1 CỘT
            // =================================

            int columns = mobile ? 1 : tablet ? 2 : 4;
            int rows = 8 / columns;

            _fieldsPanel.Controls.Clear();
            _fieldsPanel.ColumnStyles.Clear();
            _fieldsPanel.RowStyles.Clear();

            _fieldsPanel.ColumnCount = columns;
            _fieldsPanel.RowCount = rows;

            for (int i = 0; i < columns; i++)
            {
                _fieldsPanel.ColumnStyles.Add(
                    new ColumnStyle(SizeType.Percent, 100f / columns));
            }

            for (int i = 0; i < rows; i++)
            {
                _fieldsPanel.RowStyles.Add(
                    new RowStyle(SizeType.Percent, 100f / rows));
            }

            for (int i = 0; i < _editorFields.Length; i++)
            {
                _fieldsPanel.Controls.Add(
                    _editorFields[i],
                    i % columns,
                    i / columns);
            }

            Font inputFont = new("Segoe UI", 9);

            Font fieldLabelFont = new(
                "Segoe UI",
                mobile ? 8 : 9,
                FontStyle.Bold);

            _cboFormat.Font = inputFont;
            _cboSeatType.Font = inputFont;
            _cboDayType.Font = inputFont;
            _cboTimeSlot.Font = inputFont;

            _numBasePrice.Font = inputFont;
            _numSurcharge.Font = inputFont;

            foreach (Control field in _editorFields)
            {
                if (field.Controls.Count > 0 &&
                    field.Controls[0] is TableLayoutPanel fieldLayout &&
                    fieldLayout.Controls.Count > 0 &&
                    fieldLayout.Controls[0] is Label fieldLabel)
                {
                    fieldLabel.Font = fieldLabelFont;
                }
            }

            _lblPricePreview.Font = new Font(
                "Segoe UI",
                mobile ? 11 : 13,
                FontStyle.Bold);

            // =================================
            // 6.5 TÌM KIẾM VÀ LỌC
            // =================================

            _filterPanel.WrapContents = true;
            _filterPanel.AutoScroll = true;
            _filterPanel.FlowDirection =
                FlowDirection.LeftToRight;

            _filterPanel.Padding = mobile
                ? new Padding(8, 8, 8, 8)
                : new Padding(12, 8, 8, 6);

            _txtSearch.Width = mobile
                ? Math.Max(180, width - 65)
                : Math.Max(180, Math.Min(350, width - 220));

            _cboFilterFormat.Width = mobile ? 150 : 120;
            _cboFilterDay.Width = mobile ? 180 : 155;

            _txtSearch.Font = new Font(
                "Segoe UI",
                mobile ? 9 : 10);

            _cboFilterFormat.Font = new Font("Segoe UI", 9);
            _cboFilterDay.Font = new Font("Segoe UI", 9);

            // =================================
            // 6.6 CÁC NÚT CHỨC NĂNG
            // =================================

            _actionPanel.WrapContents = true;
            _actionPanel.AutoScroll = true;
            _actionPanel.FlowDirection =
                FlowDirection.RightToLeft;

            foreach (Control control in _actionPanel.Controls)
            {
                control.Margin = new Padding(4, 3, 4, 3);
                control.Width = mobile ? 115 : 135;
                control.Height = 36;

                control.Font = new Font(
                    "Segoe UI",
                    mobile ? 8 : 9,
                    FontStyle.Bold);
            }

            // =================================
            // 6.7 BẢNG DỮ LIỆU
            // =================================

            _dgvPricing.ScrollBars = ScrollBars.Both;

            _dgvPricing.AutoSizeColumnsMode =
                mobile || tablet
                    ? DataGridViewAutoSizeColumnsMode.None
                    : DataGridViewAutoSizeColumnsMode.Fill;

            int[] columnWidths =
            {
                70, 85, 120, 125, 125, 115, 100, 125
            };

            for (int i = 0;
                 i < _dgvPricing.Columns.Count &&
                 i < columnWidths.Length;
                 i++)
            {
                _dgvPricing.Columns[i].Width = mobile
                    ? Math.Max(65, columnWidths[i] - 10)
                    : columnWidths[i];
            }

            _dgvPricing.DefaultCellStyle.Font = new Font(
                "Segoe UI",
                mobile ? 8 : 9);

            _dgvPricing.ColumnHeadersDefaultCellStyle.Font =
                new Font(
                    "Segoe UI",
                    mobile ? 8 : 9,
                    FontStyle.Bold);

            _dgvPricing.RowTemplate.Height =
                mobile ? 38 : 40;

            _dgvPricing.ColumnHeadersHeight =
                mobile ? 40 : 44;

            _lblResult.Font = new Font(
                "Segoe UI",
                mobile ? 8 : 9,
                FontStyle.Bold);

            // =================================
            // 6.8 HOÀN TẤT
            // =================================

            _fieldsPanel.ResumeLayout(true);
            _statisticPanel.ResumeLayout(true);
            _filterPanel.ResumeLayout(true);
            _actionPanel.ResumeLayout(true);
            _editorLayout.ResumeLayout(true);
            _mainLayout.ResumeLayout(true);

            UpdateScrollLayout();

            PerformLayout();
        }
        finally
        {
            _isApplyingResponsiveLayout = false;
        }
    }

    // =========================================
    // 7. TẠO LABEL
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
    // 8. ĐỒNG BỘ STYLE COMBOBOX
    // =========================================

    private static void ApplyConsistentComboStyle(ComboBox combo)
    {
        // Chỉ đồng bộ màu sắc; giữ nguyên FlatStyle, font, DrawMode,
        // kích thước, viền và cách hiển thị riêng của từng ComboBox.
        combo.BackColor = Color.White;
        combo.ForeColor = Color.FromArgb(15, 23, 42);
    }

    // =========================================
    // 9. TẠO Ô NHẬP LIỆU
    // =========================================

    private Panel CreateEditorField(
        string title,
        Control input)
    {
        Panel field = new()
        {
            Dock = DockStyle.Fill,
            BackColor = _panelColor,
            Margin = new Padding(6, 5, 6, 5),
            Padding = new Padding(2)
        };

        TableLayoutPanel fieldLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = _panelColor,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        fieldLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 28));

        fieldLayout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        fieldLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        Label label = new()
        {
            Text = title,
            ForeColor = _textColor,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Margin = new Padding(0)
        };

        // Đồng bộ màu sắc tất cả ô nhập trong phần thiết lập:
        // nền trắng, chữ tối, viền phẳng giống ô Định dạng phim.
        if (input is ComboBox combo)
        {
            ApplyConsistentComboStyle(combo);
        }
        else if (input is NumericUpDown numeric)
        {
            numeric.BackColor = Color.White;
            numeric.ForeColor = Color.FromArgb(15, 23, 42);
        }

        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(0);

        fieldLayout.Controls.Add(label, 0, 0);
        fieldLayout.Controls.Add(input, 0, 1);

        field.Controls.Add(fieldLayout);

        return field;
    }

    // =========================================
    // 9. TẠO BUTTON
    // =========================================

    private Button CreateButton(string text, Color color)
    {
        Button button = new()
        {
            Text = text,
            Size = new Size(135, 36),
            MinimumSize = new Size(100, 34),
            BackColor = color,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(5, 0, 0, 0),
            UseVisualStyleBackColor = false
        };

        button.FlatAppearance.BorderSize = 0;

        return button;
    }

    // =========================================
    // 10. TẠO THẺ THỐNG KÊ
    // =========================================

    private Panel CreateStatisticCard(
        string title,
        string description,
        Color accentColor,
        out Label valueLabel)
    {
        Panel card = new()
        {
            Dock = DockStyle.Fill,
            BackColor = _panelColor,
            Margin = new Padding(0)
        };

        Panel accent = new()
        {
            Dock = DockStyle.Left,
            Width = 5,
            BackColor = accentColor
        };

        Label lblTitle = new()
        {
            Text = title,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(20, 8)
        };

        Label currentValueLabel = new()
        {
            Text = "0",
            Font = new Font("Segoe UI", 21, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 30)
        };

        valueLabel = currentValueLabel;

        Label lblDescription = new()
        {
            Text = description,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(203, 213, 225),
            AutoSize = false,
            Location = new Point(22, 76),
            Size = new Size(250, 20),
            Anchor = AnchorStyles.Top |
                     AnchorStyles.Left |
                     AnchorStyles.Right
        };

        card.Resize += (s, e) =>
        {
            lblDescription.Width =
                Math.Max(80, card.ClientSize.Width - 35);

            bool compactCard = card.ClientSize.Height < 90;
            bool narrowCard = card.ClientSize.Width < 260;

            lblTitle.Location = compactCard
                ? new Point(20, 4)
                : new Point(20, 8);

            currentValueLabel.Location = compactCard
                ? new Point(20, 23)
                : new Point(20, 30);

            lblDescription.Location = compactCard
                ? new Point(22, 49)
                : new Point(22, 76);

            lblTitle.Font = new Font(
                "Segoe UI",
                narrowCard ? 8 : 9,
                FontStyle.Bold);

            currentValueLabel.Font = new Font(
                "Segoe UI",
                narrowCard ? 15 : 21,
                FontStyle.Bold);

            lblDescription.Font = new Font(
                "Segoe UI",
                narrowCard ? 7.5f : 8.5f);
        };

        card.Controls.Add(lblTitle);
        card.Controls.Add(currentValueLabel);
        card.Controls.Add(lblDescription);
        card.Controls.Add(accent);

        return card;
    }

    // =========================================
    // 11. TẢI DỮ LIỆU TỪ SQLITE
    // =========================================

    private void LoadPricingData()
    {
        try
        {
            _rules.Clear();

            List<PricingRule> data = _pricingService.GetAll();

            _rules.AddRange(data);

            RefreshPricingTable();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Không thể tải dữ liệu giá vé từ cơ sở dữ liệu.\n\n" +
                ex.Message,
                "Lỗi tải dữ liệu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    // =========================================
    // 12. ĐỊNH DẠNG TIỀN
    // =========================================

    private string FormatMoney(decimal amount)
    {
        return amount.ToString("N0") + " đ";
    }

    // =========================================
    // 13. CẬP NHẬT GIÁ DỰ KIẾN
    // =========================================

    private void UpdatePricePreview()
    {
        decimal finalPrice =
            _numBasePrice.Value + _numSurcharge.Value;

        _lblPricePreview.Text =
            FormatMoney(finalPrice);

        _lblPricePreview.ForeColor =
            finalPrice <= 0
                ? Color.FromArgb(248, 113, 113)
                : Color.FromArgb(52, 211, 153);
    }

    // =========================================
    // 14. HIỂN THỊ BẢNG GIÁ
    // =========================================

    private void RefreshPricingTable()
    {
        _dgvPricing.Rows.Clear();

        string keyword = _txtSearch.Text
            .Trim()
            .ToLowerInvariant();

        string selectedFormat =
            _cboFilterFormat.SelectedItem?.ToString() ?? "Tất cả";

        string selectedDay =
            _cboFilterDay.SelectedItem?.ToString() ?? "Tất cả";

        var result = _rules.Where(rule =>
        {
            bool matchSearch =
                rule.Format.ToLowerInvariant().Contains(keyword) ||
                rule.SeatType.ToLowerInvariant().Contains(keyword) ||
                rule.DayType.ToLowerInvariant().Contains(keyword) ||
                rule.TimeSlot.ToLowerInvariant().Contains(keyword);

            bool matchFormat =
                selectedFormat == "Tất cả" ||
                rule.Format == selectedFormat;

            bool matchDay =
                selectedDay == "Tất cả" ||
                rule.DayType == selectedDay;

            return matchSearch && matchFormat && matchDay;
        })
        .OrderBy(r => r.Format)
        .ThenBy(r => r.SeatType)
        .ThenBy(r => r.DayType)
        .ThenBy(r => r.TimeSlot)
        .ToList();

        for (int index = 0; index < result.Count; index++)
        {
            PricingRule rule = result[index];
            _dgvPricing.Rows.Add(
                index + 1,
                rule.Id,
                rule.Format,
                rule.SeatType,
                rule.DayType,
                rule.TimeSlot,
                FormatMoney(rule.BasePrice),
                FormatMoney(rule.Surcharge),
                FormatMoney(rule.FinalPrice)
            );
        }

        UpdateStatistics();

        _lblResult.Text =
            $"Đang hiển thị {result.Count} / {_rules.Count} quy tắc giá";
    }

    // =========================================
    // 15. CẬP NHẬT THỐNG KÊ
    // =========================================

    private void UpdateStatistics()
    {
        _lblTotalRules.Text = _rules.Count.ToString();

        if (_rules.Count == 0)
        {
            _lblMinPrice.Text = "0 đ";
            _lblMaxPrice.Text = "0 đ";
            return;
        }

        _lblMinPrice.Text = FormatMoney(
            _rules.Min(r => r.FinalPrice)
        );

        _lblMaxPrice.Text = FormatMoney(
            _rules.Max(r => r.FinalPrice)
        );
    }

    // =========================================
    // 16. THÊM HOẶC CẬP NHẬT QUY TẮC GIÁ
    // =========================================

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        try
        {
            decimal finalPrice =
                _numBasePrice.Value + _numSurcharge.Value;

            if (finalPrice <= 0)
            {
                MessageBox.Show(
                    "Giá vé sau điều chỉnh phải lớn hơn 0!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            PricingRule newRule = new()
            {
                Id = _editingId ?? 0,

                Format = _cboFormat.SelectedItem?.ToString()
                         ?? "2D",

                SeatType = _cboSeatType.SelectedItem?.ToString()
                           ?? "Ghế thường",

                DayType = _cboDayType.Text.Trim(),

                TimeSlot = _cboTimeSlot.Text.Trim(),

                BasePrice = _numBasePrice.Value,

                Surcharge = _numSurcharge.Value
            };

            if (_editingId.HasValue)
            {
                bool updated = _pricingService.Update(newRule);

                if (!updated)
                {
                    MessageBox.Show(
                        "Không thể cập nhật quy tắc giá.",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );

                    return;
                }

                MessageBox.Show(
                    "Cập nhật giá vé thành công!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            else
            {
                _pricingService.Add(newRule);

                MessageBox.Show(
                    "Thêm quy tắc giá thành công!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }

            LoadPricingData();

            ResetEditor();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Không thể lưu quy tắc giá.\n\n" + ex.Message,
                "Lỗi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    // =========================================
    // 17. CHỌN QUY TẮC GIÁ ĐỂ SỬA
    // =========================================

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        if (_dgvPricing.SelectedRows.Count == 0)
        {
            MessageBox.Show(
                "Vui lòng chọn quy tắc giá cần sửa!",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );

            return;
        }

        try
        {
            int id = Convert.ToInt32(
                _dgvPricing.SelectedRows[0].Cells["Id"].Value
            );

            PricingRule? rule = _pricingService.GetById(id);

            if (rule == null)
            {
                MessageBox.Show("Không tìm thấy quy tắc giá!");
                return;
            }

            _editingId = id;

            _cboFormat.SelectedItem = rule.Format;
            _cboSeatType.SelectedItem = rule.SeatType;
            _cboDayType.Text = rule.DayType;
            _cboTimeSlot.Text = rule.TimeSlot;

            _numBasePrice.Value = Math.Clamp(
                rule.BasePrice,
                _numBasePrice.Minimum,
                _numBasePrice.Maximum
            );

            _numSurcharge.Value = Math.Clamp(
                rule.Surcharge,
                _numSurcharge.Minimum,
                _numSurcharge.Maximum
            );

            _btnSave.Text = "Cập nhật giá";
            _btnCancel.Visible = true;

            UpdatePricePreview();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Không thể tải quy tắc giá cần sửa.\n\n" + ex.Message,
                "Lỗi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    // =========================================
    // 18. HỦY CHỈNH SỬA
    // =========================================

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        ResetEditor();
    }

    private void ResetEditor()
    {
        _editingId = null;

        _cboFormat.SelectedIndex = 0;
        _cboSeatType.SelectedIndex = 0;
        _cboDayType.Text = "Ngày thường";
        _cboTimeSlot.Text = "12:00 - 17:00";

        _numBasePrice.Value = 75000;
        _numSurcharge.Value = 0;

        _btnSave.Text = "Thêm quy tắc";
        _btnCancel.Visible = false;

        UpdatePricePreview();
    }

    // =========================================
    // 19. XÓA QUY TẮC GIÁ
    // =========================================

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (_dgvPricing.SelectedRows.Count == 0)
        {
            MessageBox.Show(
                "Vui lòng chọn quy tắc giá cần xóa!",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );

            return;
        }

        int id = Convert.ToInt32(
            _dgvPricing.SelectedRows[0].Cells["Id"].Value
        );

        DialogResult confirm = MessageBox.Show(
            "Bạn có chắc chắn muốn xóa quy tắc giá này?",
            "Xác nhận xóa",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        );

        if (confirm != DialogResult.Yes)
            return;

        try
        {
            bool deleted = _pricingService.Delete(id);

            if (!deleted)
            {
                MessageBox.Show(
                    "Không thể xóa quy tắc giá.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            if (_editingId == id)
                ResetEditor();

            LoadPricingData();

            MessageBox.Show(
                "Xóa quy tắc giá thành công!",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Không thể xóa quy tắc giá.\n\n" + ex.Message,
                "Lỗi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }
}