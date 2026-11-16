
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
    // 1. DỮ LIỆU GIÁ VÉ
    // =========================================

    private readonly PricingService _pricingService = new();
    private readonly List<PricingRule> _rules = new();

    private int? _editingId;

    // =========================================
    // 2. KHAI BÁO GIAO DIỆN
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

        BuildInterface();

        LoadPricingData();

        UpdatePricePreview();
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

        mainLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 85));

        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 115));

        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 65));

        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 230));

        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 32));

        // =====================================
        // 4.1 HEADER
        // =====================================

        Panel headerPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = _backgroundColor
        };

        Label lblTitle = new Label
        {
            Text = "QUẢN LÝ GIÁ VÉ",
            Font = new Font("Segoe UI", 21, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(5, 10)
        };

        Label lblDescription = new Label
        {
            Text = "Thiết lập bảng giá vé theo định dạng, loại ghế và thời gian",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(7, 53)
        };

        headerPanel.Controls.Add(lblTitle);
        headerPanel.Controls.Add(lblDescription);

        // =====================================
        // 4.2 THỐNG KÊ
        // =====================================

        TableLayoutPanel statisticPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = _backgroundColor
        };

        for (int i = 0; i < 3; i++)
        {
            statisticPanel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 33.33f));
        }

        statisticPanel.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        Panel cardTotal = CreateStatisticCard(
            "TỔNG QUY TẮC GIÁ",
            "Các mức giá đang thiết lập",
            Color.FromArgb(37, 99, 235),
            out _lblTotalRules
        );

        Panel cardMin = CreateStatisticCard(
            "GIÁ THẤP NHẤT",
            "Mức giá vé thấp nhất",
            Color.FromArgb(16, 185, 129),
            out _lblMinPrice
        );

        Panel cardMax = CreateStatisticCard(
            "GIÁ CAO NHẤT",
            "Mức giá vé cao nhất",
            Color.FromArgb(139, 92, 246),
            out _lblMaxPrice
        );

        cardTotal.Margin = new Padding(0, 5, 10, 5);
        cardMin.Margin = new Padding(5, 5, 5, 5);
        cardMax.Margin = new Padding(10, 5, 0, 5);

        statisticPanel.Controls.Add(cardTotal, 0, 0);
        statisticPanel.Controls.Add(cardMin, 1, 0);
        statisticPanel.Controls.Add(cardMax, 2, 0);

        // =====================================
        // 4.3 TÌM KIẾM VÀ LỌC
        // =====================================

        FlowLayoutPanel filterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = _panelColor,
            Padding = new Padding(12, 12, 5, 0),
            WrapContents = false
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

        _cboFilterDay.Items.AddRange(new object[]
        {
            "Tất cả",
            "Ngày thường",
            "Cuối tuần"
        });

        _cboFilterDay.SelectedIndex = 0;

        filterPanel.Controls.Add(lblSearch);
        filterPanel.Controls.Add(_txtSearch);
        filterPanel.Controls.Add(lblFormatFilter);
        filterPanel.Controls.Add(_cboFilterFormat);
        filterPanel.Controls.Add(lblDayFilter);
        filterPanel.Controls.Add(_cboFilterDay);

        // =====================================
        // 4.4 FORM THIẾT LẬP GIÁ
        // =====================================

        Panel editorPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = _panelColor,
            Padding = new Padding(12)
        };

        TableLayoutPanel editorLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = _panelColor
        };

        editorLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        editorLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 28));

        editorLayout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        editorLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 40));

        Label lblEditorTitle = new Label
        {
            Text = "THIẾT LẬP QUY TẮC GIÁ VÉ",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        // =====================================
        // CÁC TRƯỜNG NHẬP LIỆU
        // =====================================

        TableLayoutPanel fieldsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2,
            BackColor = _panelColor,
            Padding = new Padding(2)
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

        // Định dạng phim
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
            CreateEditorField("Định dạng phim:", _cboFormat),
            0, 0
        );

        // Loại ghế
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

        fieldsPanel.Controls.Add(
            CreateEditorField("Loại ghế:", _cboSeatType),
            1, 0
        );

        // Ngày áp dụng
        _cboDayType = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9)
        };

        _cboDayType.Items.AddRange(new object[]
        {
            "Ngày thường",
            "Cuối tuần"
        });

        _cboDayType.SelectedIndex = 0;

        fieldsPanel.Controls.Add(
            CreateEditorField("Ngày áp dụng:", _cboDayType),
            2, 0
        );

        // Khung giờ
        _cboTimeSlot = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9)
        };

        _cboTimeSlot.Items.AddRange(new object[]
        {
            "Trước 12:00",
            "12:00 - 17:00",
            "Sau 17:00"
        });

        _cboTimeSlot.SelectedIndex = 1;

        fieldsPanel.Controls.Add(
            CreateEditorField("Khung giờ:", _cboTimeSlot),
            3, 0
        );

        // Giá cơ bản
        _numBasePrice = new NumericUpDown
        {
            Minimum = 10000,
            Maximum = 1000000,
            Increment = 5000,
            Value = 75000,
            ThousandsSeparator = true,
            Font = new Font("Segoe UI", 9)
        };

        fieldsPanel.Controls.Add(
            CreateEditorField("Giá cơ bản (VNĐ):", _numBasePrice),
            0, 1
        );

        // Phụ thu
        _numSurcharge = new NumericUpDown
        {
            Minimum = -100000,
            Maximum = 500000,
            Increment = 5000,
            ThousandsSeparator = true,
            Value = 0,
            Font = new Font("Segoe UI", 9)
        };

        fieldsPanel.Controls.Add(
            CreateEditorField("Phụ thu (VNĐ):", _numSurcharge),
            1, 1
        );

        // Giá cuối cùng
        _lblPricePreview = new Label
        {
            Text = "75.000 VNĐ",
            ForeColor = Color.FromArgb(52, 211, 153),
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        fieldsPanel.Controls.Add(
            CreateEditorField("Giá vé sau điều chỉnh:", _lblPricePreview),
            2, 1
        );

        // Cách tính
        Label lblNote = new Label
        {
            Text = "Giá cuối = Giá cơ bản + Phụ thu",
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Segoe UI", 9),
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill
        };

        fieldsPanel.Controls.Add(
            CreateEditorField("Cách tính:", lblNote),
            3, 1
        );

        // =====================================
        // CÁC NÚT CHỨC NĂNG
        // =====================================

        FlowLayoutPanel actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = _panelColor,
            Padding = new Padding(0, 3, 0, 0)
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

        actionPanel.Controls.Add(_btnDelete);
        actionPanel.Controls.Add(_btnCancel);
        actionPanel.Controls.Add(_btnEdit);
        actionPanel.Controls.Add(_btnSave);

        editorLayout.Controls.Add(lblEditorTitle, 0, 0);
        editorLayout.Controls.Add(fieldsPanel, 0, 1);
        editorLayout.Controls.Add(actionPanel, 0, 2);

        editorPanel.Controls.Add(editorLayout);

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

            ReadOnly = true,
            MultiSelect = false,

            SelectionMode =
                DataGridViewSelectionMode.FullRowSelect,

            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false
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

        _dgvPricing.RowTemplate.Height = 38;

        _dgvPricing.Columns.Add("Id", "Mã giá");
        _dgvPricing.Columns.Add("Format", "Định dạng");
        _dgvPricing.Columns.Add("SeatType", "Loại ghế");
        _dgvPricing.Columns.Add("DayType", "Ngày áp dụng");
        _dgvPricing.Columns.Add("TimeSlot", "Khung giờ");
        _dgvPricing.Columns.Add("BasePrice", "Giá cơ bản");
        _dgvPricing.Columns.Add("Surcharge", "Phụ thu");
        _dgvPricing.Columns.Add("FinalPrice", "Giá cuối cùng");

        // =====================================
        // 4.6 CHÂN TRANG
        // =====================================

        _lblResult = new Label
        {
            Text = "Tổng số quy tắc giá: 0",
            Dock = DockStyle.Fill,
            ForeColor = _textColor,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // =====================================
        // 4.7 GHÉP GIAO DIỆN
        // =====================================

        mainLayout.Controls.Add(headerPanel, 0, 0);
        mainLayout.Controls.Add(statisticPanel, 0, 1);
        mainLayout.Controls.Add(filterPanel, 0, 2);
        mainLayout.Controls.Add(editorPanel, 0, 3);
        mainLayout.Controls.Add(_dgvPricing, 0, 4);
        mainLayout.Controls.Add(_lblResult, 0, 5);

        Controls.Add(mainLayout);

        // =====================================
        // 4.8 SỰ KIỆN
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
    // 5. TẠO LABEL
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
    // 6. TẠO Ô NHẬP LIỆU
    // =========================================

    private Panel CreateEditorField(
        string title,
        Control input)
    {
        Panel field = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = _panelColor,
            Margin = new Padding(6, 3, 6, 3)
        };

        TableLayoutPanel fieldLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = _panelColor,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        fieldLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 25));

        fieldLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 32));

        fieldLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        Label label = new Label
        {
            Text = title,
            ForeColor = _textColor,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0)
        };

        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(0);

        fieldLayout.Controls.Add(label, 0, 0);
        fieldLayout.Controls.Add(input, 0, 1);

        field.Controls.Add(fieldLayout);

        return field;
    }

    // =========================================
    // 7. TẠO BUTTON
    // =========================================

    private Button CreateButton(string text, Color color)
    {
        Button button = new Button
        {
            Text = text,
            Size = new Size(135, 35),
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
    // 8. TẠO THẺ THỐNG KÊ
    // =========================================

    private Panel CreateStatisticCard(
        string title,
        string description,
        Color accentColor,
        out Label valueLabel)
    {
        Panel card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = _panelColor,
            Margin = new Padding(0)
        };

        Panel accent = new Panel
        {
            Dock = DockStyle.Left,
            Width = 5,
            BackColor = accentColor
        };

        Label lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(20, 10)
        };

        valueLabel = new Label
        {
            Text = "0",
            Font = new Font("Segoe UI", 21, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 34)
        };

        Label lblDescription = new Label
        {
            Text = description,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(203, 213, 225),
            AutoSize = true,
            Location = new Point(22, 76)
        };

        card.Controls.Add(lblTitle);
        card.Controls.Add(valueLabel);
        card.Controls.Add(lblDescription);
        card.Controls.Add(accent);

        return card;
    }

    // =========================================
    // 9. TẢI DỮ LIỆU TỪ SQLITE
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
    // 10. ĐỊNH DẠNG TIỀN
    // =========================================

    private string FormatMoney(decimal amount)
    {
        return amount.ToString("N0") + " đ";
    }

    // =========================================
    // 11. CẬP NHẬT GIÁ DỰ KIẾN
    // =========================================

    private void UpdatePricePreview()
    {
        decimal finalPrice =
            _numBasePrice.Value + _numSurcharge.Value;

        _lblPricePreview.Text =
            FormatMoney(finalPrice);

        _lblPricePreview.ForeColor =
            finalPrice < 0
                ? Color.FromArgb(248, 113, 113)
                : Color.FromArgb(52, 211, 153);
    }

    // =========================================
    // 12. HIỂN THỊ BẢNG GIÁ
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

        }).OrderBy(r => r.Format)
          .ThenBy(r => r.SeatType)
          .ThenBy(r => r.DayType)
          .ThenBy(r => r.TimeSlot)
          .ToList();

        foreach (PricingRule rule in result)
        {
            _dgvPricing.Rows.Add(
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
    // 13. CẬP NHẬT THỐNG KÊ
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
    // 14. THÊM HOẶC CẬP NHẬT QUY TẮC GIÁ
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

            PricingRule newRule = new PricingRule
            {
                Id = _editingId ?? 0,

                Format = _cboFormat.SelectedItem?.ToString() ?? "2D",

                SeatType = _cboSeatType.SelectedItem?.ToString()
                           ?? "Ghế thường",

                DayType = _cboDayType.SelectedItem?.ToString()
                          ?? "Ngày thường",

                TimeSlot = _cboTimeSlot.SelectedItem?.ToString()
                           ?? "12:00 - 17:00",

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
    // 15. CHỌN QUY TẮC GIÁ ĐỂ SỬA
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
            _cboDayType.SelectedItem = rule.DayType;
            _cboTimeSlot.SelectedItem = rule.TimeSlot;

            _numBasePrice.Value = rule.BasePrice;
            _numSurcharge.Value = rule.Surcharge;

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
    // 16. HỦY CHỈNH SỬA
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
        _cboDayType.SelectedIndex = 0;
        _cboTimeSlot.SelectedIndex = 1;

        _numBasePrice.Value = 75000;
        _numSurcharge.Value = 0;

        _btnSave.Text = "Thêm quy tắc";
        _btnCancel.Visible = false;

        UpdatePricePreview();
    }

    // =========================================
    // 17. XÓA QUY TẮC GIÁ
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
