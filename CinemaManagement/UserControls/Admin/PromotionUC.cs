
namespace CinemaManagement.UserControls.Admin;

// =============================================
// MODEL HIỂN THỊ KHUYẾN MÃI
// =============================================

internal sealed record PromotionDisplayItem
{
    public int Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string DiscountType { get; init; } = "Phần trăm";

    public decimal DiscountValue { get; init; }

    public decimal MaxDiscount { get; init; }

    public decimal MinimumOrder { get; init; }

    public DateTime StartDate { get; init; }

    public DateTime EndDate { get; init; }

    public bool IsEnabled { get; init; } = true;
}

// =============================================
// PROMOTION USER CONTROL
// =============================================

public partial class PromotionUC : UserControl
{
    private readonly List<PromotionDisplayItem> _promotions = [];

    private DataGridView _dgvPromotions = null!;

    private TextBox _txtSearch = null!;

    private ComboBox _cboDiscountType = null!;

    private ComboBox _cboStatus = null!;

    private Label _lblTotal = null!;

    private Label _lblActive = null!;

    private Label _lblUpcoming = null!;

    private Label _lblResult = null!;

    private Button _btnAdd = null!;

    private Button _btnEdit = null!;

    private Button _btnDelete = null!;

    private int _nextId = 5;

    private readonly Color _backgroundColor =
        Color.FromArgb(15, 23, 42);

    private readonly Color _panelColor =
        Color.FromArgb(30, 41, 59);

    private readonly Color _accentColor =
        Color.FromArgb(37, 99, 235);

    private readonly Color _textColor =
        Color.FromArgb(226, 232, 240);

    public PromotionUC()
    {
        InitializeComponent();

        BuildInterface();

        LoadSampleData();

        RefreshPromotionTable();
    }

    // =========================================
    // 1. XÂY DỰNG GIAO DIỆN
    // =========================================

    private void BuildInterface()
    {
        TableLayoutPanel mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = _backgroundColor,
            Padding = new Padding(15)
        };

        mainLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 75));

        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 110));

        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 70));

        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 50));

        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 32));

        // =====================================
        // 1.1 HEADER
        // =====================================

        Panel headerPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = _backgroundColor
        };

        Label lblTitle = new Label
        {
            Text = "QUẢN LÝ KHUYẾN MÃI",
            Font = new Font("Segoe UI", 21, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(5, 5)
        };

        Label lblDescription = new Label
        {
            Text = "Quản lý chương trình giảm giá và mã voucher",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(7, 45)
        };

        headerPanel.Controls.Add(lblTitle);
        headerPanel.Controls.Add(lblDescription);

        // =====================================
        // 1.2 THỐNG KÊ
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

        _lblTotal = new Label
        {
            Text = "0",
            Font = new Font("Segoe UI", 23, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true
        };

        _lblActive = new Label
        {
            Text = "0",
            Font = new Font("Segoe UI", 23, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true
        };

        _lblUpcoming = new Label
        {
            Text = "0",
            Font = new Font("Segoe UI", 23, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true
        };

        statisticPanel.Controls.Add(
            CreateStatisticCard(
                "TỔNG KHUYẾN MÃI",
                Color.FromArgb(37, 99, 235),
                _lblTotal
            ), 0, 0);

        statisticPanel.Controls.Add(
            CreateStatisticCard(
                "ĐANG ÁP DỤNG",
                Color.FromArgb(16, 185, 129),
                _lblActive
            ), 1, 0);

        statisticPanel.Controls.Add(
            CreateStatisticCard(
                "SẮP DIỄN RA",
                Color.FromArgb(245, 158, 11),
                _lblUpcoming
            ), 2, 0);

        // =====================================
        // 1.3 TÌM KIẾM VÀ BỘ LỌC
        // =====================================

        TableLayoutPanel filterPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 1,
            BackColor = _panelColor,
            Padding = new Padding(12, 8, 12, 8),
            Margin = new Padding(0)
        };

        // Phân chia chiều rộng
        filterPanel.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 85));

        filterPanel.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        filterPanel.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 85));

        filterPanel.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 145));

        filterPanel.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 90));

        filterPanel.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 155));

        filterPanel.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        Color inputColor = Color.FromArgb(15, 23, 42);

        // -------------------------------------
        // TÌM KIẾM
        // -------------------------------------

        Label lblSearch = CreateLabel("Tìm kiếm:");

        lblSearch.AutoSize = false;
        lblSearch.Dock = DockStyle.Fill;
        lblSearch.TextAlign = ContentAlignment.MiddleLeft;
        lblSearch.Margin = new Padding(0, 0, 5, 0);

        _txtSearch = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10),
            BackColor = inputColor,
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Nhập mã voucher hoặc tên chương trình...",
            MinimumSize = new Size(250, 34),
            Margin = new Padding(3, 3, 15, 3)
        };

        // -------------------------------------
        // LOẠI GIẢM GIÁ
        // -------------------------------------

        Label lblType = CreateLabel("Loại giảm:");

        lblType.AutoSize = false;
        lblType.Dock = DockStyle.Fill;
        lblType.TextAlign = ContentAlignment.MiddleLeft;
        lblType.Margin = new Padding(0, 0, 5, 0);

        _cboDiscountType = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f),
            BackColor = inputColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(2, 3, 10, 3),
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 25
        };

        _cboDiscountType.Items.AddRange(new object[]
        {
            "Tất cả",
            "Phần trăm",
            "Số tiền"
        });

        _cboDiscountType.SelectedIndex = 0;

        // -------------------------------------
        // TRẠNG THÁI
        // -------------------------------------

        Label lblStatus = CreateLabel("Trạng thái:");

        lblStatus.AutoSize = false;
        lblStatus.Dock = DockStyle.Fill;
        lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        lblStatus.Margin = new Padding(0, 0, 5, 0);

        _cboStatus = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f),
            BackColor = inputColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 25,
            Margin = new Padding(2, 3, 0, 3)
        };

        _cboStatus.Items.AddRange(new object[]
        {
            "Tất cả",
            "Đang áp dụng",
            "Sắp diễn ra",
            "Đã kết thúc",
            "Tạm ngưng"
        });

        _cboStatus.SelectedIndex = 0;

        // -------------------------------------
        // GHÉP CÁC THÀNH PHẦN
        // -------------------------------------

        filterPanel.Controls.Add(lblSearch, 0, 0);
        filterPanel.Controls.Add(_txtSearch, 1, 0);

        filterPanel.Controls.Add(lblType, 2, 0);
        filterPanel.Controls.Add(_cboDiscountType, 3, 0);

        filterPanel.Controls.Add(lblStatus, 4, 0);
        filterPanel.Controls.Add(_cboStatus, 5, 0);

        // =====================================
        // 1.4 CÁC NÚT CHỨC NĂNG
        // =====================================

        FlowLayoutPanel actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = _backgroundColor,
            Padding = new Padding(0, 6, 0, 0)
        };

        _btnAdd = CreateButton(
            "+ Thêm khuyến mãi",
            _accentColor,
            165
        );

        _btnEdit = CreateButton(
            "Sửa khuyến mãi",
            Color.FromArgb(217, 119, 6),
            145
        );

        _btnDelete = CreateButton(
            "Xóa khuyến mãi",
            Color.FromArgb(220, 38, 38),
            145
        );

        actionPanel.Controls.Add(_btnAdd);
        actionPanel.Controls.Add(_btnEdit);
        actionPanel.Controls.Add(_btnDelete);

        // =====================================
        // 1.5 BẢNG DANH SÁCH KHUYẾN MÃI
        // =====================================

        _dgvPromotions = new DataGridView
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

            ColumnHeadersHeight = 45,
            RowTemplate = { Height = 45 },

            GridColor = Color.FromArgb(51, 65, 85),

            CellBorderStyle =
                DataGridViewCellBorderStyle.SingleHorizontal
        };

        // Header bảng

        _dgvPromotions.ColumnHeadersDefaultCellStyle.BackColor =
            _panelColor;

        _dgvPromotions.ColumnHeadersDefaultCellStyle.ForeColor =
            _textColor;

        _dgvPromotions.ColumnHeadersDefaultCellStyle.Font =
            new Font("Segoe UI", 9, FontStyle.Bold);

        _dgvPromotions.ColumnHeadersDefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleLeft;

        _dgvPromotions.ColumnHeadersDefaultCellStyle.Padding =
            new Padding(8, 0, 0, 0);

        // Nội dung bảng

        _dgvPromotions.DefaultCellStyle.BackColor =
            _panelColor;

        _dgvPromotions.DefaultCellStyle.ForeColor =
            _textColor;

        _dgvPromotions.DefaultCellStyle.Font =
            new Font("Segoe UI", 9);

        _dgvPromotions.DefaultCellStyle.SelectionBackColor =
            Color.FromArgb(51, 65, 85);

        _dgvPromotions.DefaultCellStyle.SelectionForeColor =
            Color.White;

        _dgvPromotions.DefaultCellStyle.Padding =
            new Padding(8, 0, 5, 0);

        _dgvPromotions.AlternatingRowsDefaultCellStyle.BackColor =
            Color.FromArgb(37, 49, 68);

        // Các cột

        _dgvPromotions.Columns.Add("Code", "MÃ VOUCHER");
        _dgvPromotions.Columns.Add("Name", "TÊN CHƯƠNG TRÌNH");
        _dgvPromotions.Columns.Add("Discount", "MỨC GIẢM");
        _dgvPromotions.Columns.Add("MaxDiscount", "GIẢM TỐI ĐA");
        _dgvPromotions.Columns.Add("MinOrder", "ĐƠN TỐI THIỂU");
        _dgvPromotions.Columns.Add("StartDate", "NGÀY BẮT ĐẦU");
        _dgvPromotions.Columns.Add("EndDate", "NGÀY KẾT THÚC");
        _dgvPromotions.Columns.Add("Status", "TRẠNG THÁI");

        // Độ rộng cột

        _dgvPromotions.Columns["Code"]!.FillWeight = 90;
        _dgvPromotions.Columns["Name"]!.FillWeight = 145;
        _dgvPromotions.Columns["Discount"]!.FillWeight = 90;
        _dgvPromotions.Columns["MaxDiscount"]!.FillWeight = 100;
        _dgvPromotions.Columns["MinOrder"]!.FillWeight = 105;
        _dgvPromotions.Columns["StartDate"]!.FillWeight = 105;
        _dgvPromotions.Columns["EndDate"]!.FillWeight = 105;
        _dgvPromotions.Columns["Status"]!.FillWeight = 110;

        // Căn chỉnh dữ liệu

        _dgvPromotions.Columns["Code"]!.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleCenter;

        _dgvPromotions.Columns["Discount"]!.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleRight;

        _dgvPromotions.Columns["MaxDiscount"]!.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleRight;

        _dgvPromotions.Columns["MinOrder"]!.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleRight;

        _dgvPromotions.Columns["StartDate"]!.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleCenter;

        _dgvPromotions.Columns["EndDate"]!.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleCenter;

        _dgvPromotions.Columns["Status"]!.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleCenter;

        // =====================================
        // 1.6 CHÂN TRANG
        // =====================================

        _lblResult = new Label
        {
            Text = "Tổng số khuyến mãi: 0",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // =====================================
        // 1.7 GHÉP GIAO DIỆN
        // =====================================

        mainLayout.Controls.Add(headerPanel, 0, 0);
        mainLayout.Controls.Add(statisticPanel, 0, 1);
        mainLayout.Controls.Add(filterPanel, 0, 2);
        mainLayout.Controls.Add(actionPanel, 0, 3);
        mainLayout.Controls.Add(_dgvPromotions, 0, 4);
        mainLayout.Controls.Add(_lblResult, 0, 5);

        Controls.Add(mainLayout);

        // =====================================
        // 1.8 SỰ KIỆN
        // =====================================

        _txtSearch.TextChanged += (_, _) =>
            RefreshPromotionTable();

        _cboDiscountType.SelectedIndexChanged += (_, _) =>
            RefreshPromotionTable();

        _cboStatus.SelectedIndexChanged += (_, _) =>
            RefreshPromotionTable();

        // Căn giữa chữ trong hai ComboBox

        _cboDiscountType.DrawItem += FilterComboBox_DrawItem;
        _cboStatus.DrawItem += FilterComboBox_DrawItem;

        _dgvPromotions.CellFormatting +=
            DgvPromotions_CellFormatting;

        _btnAdd.Click += BtnAdd_Click;
        _btnEdit.Click += BtnEdit_Click;
        _btnDelete.Click += BtnDelete_Click;
    }

    // =========================================
    // CĂN GIỮA CHỮ TRONG HAI COMBOBOX BỘ LỌC
    // =========================================

    private void FilterComboBox_DrawItem(
        object? sender,
        DrawItemEventArgs e)
    {
        if (sender is not ComboBox comboBox)
            return;

        int itemIndex = e.Index >= 0
            ? e.Index
            : comboBox.SelectedIndex;

        if (itemIndex < 0 || itemIndex >= comboBox.Items.Count)
        {
            e.DrawBackground();
            return;
        }

        bool isSelected =
            (e.State & DrawItemState.Selected) == DrawItemState.Selected;

        Color itemBackColor = isSelected
            ? Color.FromArgb(37, 99, 235)
            : Color.FromArgb(15, 23, 42);

        using (SolidBrush brush = new SolidBrush(itemBackColor))
        {
            e.Graphics.FillRectangle(brush, e.Bounds);
        }

        string itemText =
            comboBox.Items[itemIndex]?.ToString() ?? string.Empty;

        TextRenderer.DrawText(
            e.Graphics,
            itemText,
            e.Font ?? comboBox.Font,
            e.Bounds,
            Color.White,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix
        );

        if ((e.State & DrawItemState.Focus) == DrawItemState.Focus)
            e.DrawFocusRectangle();
    }

    // =========================================
    // 2. TẠO LABEL
    // =========================================

    private Label CreateLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            ForeColor = _textColor,
            Font = new Font("Segoe UI", 9),
            Margin = new Padding(3, 8, 3, 0)
        };
    }

    // =========================================
    // 3. TẠO BUTTON
    // =========================================

    private Button CreateButton(
        string text,
        Color color,
        int width)
    {
        Button button = new Button
        {
            Text = text,
            Size = new Size(width, 38),
            BackColor = color,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 10, 0)
        };

        button.FlatAppearance.BorderSize = 0;

        return button;
    }

    // =========================================
    // 4. TẠO THẺ THỐNG KÊ
    // =========================================

    private Panel CreateStatisticCard(
        string title,
        Color accentColor,
        Label valueLabel)
    {
        Panel card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = _panelColor,
            Margin = new Padding(5)
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
            Location = new Point(20, 10),
            AutoSize = true
        };

        valueLabel.Location = new Point(20, 35);

        card.Controls.Add(lblTitle);
        card.Controls.Add(valueLabel);
        card.Controls.Add(accent);

        return card;
    }

    // =========================================
    // 5. DỮ LIỆU KHUYẾN MÃI MẪU
    // =========================================

    private void LoadSampleData()
    {
        DateTime today = DateTime.Today;

        _promotions.Clear();

        _promotions.AddRange(
        [
            new PromotionDisplayItem
            {
                Id = 1,
                Code = "WELCOME20",
                Name = "Ưu đãi khách hàng mới",
                DiscountType = "Phần trăm",
                DiscountValue = 20,
                MaxDiscount = 50000,
                MinimumOrder = 100000,
                StartDate = today.AddDays(-5),
                EndDate = today.AddDays(20),
                IsEnabled = true
            },

            new PromotionDisplayItem
            {
                Id = 2,
                Code = "HAPPY50000",
                Name = "Ưu đãi thành viên",
                DiscountType = "Số tiền",
                DiscountValue = 50000,
                MaxDiscount = 0,
                MinimumOrder = 200000,
                StartDate = today.AddDays(3),
                EndDate = today.AddDays(20),
                IsEnabled = true
            },

            new PromotionDisplayItem
            {
                Id = 3,
                Code = "STUDENT10",
                Name = "Ưu đãi sinh viên",
                DiscountType = "Phần trăm",
                DiscountValue = 10,
                MaxDiscount = 30000,
                MinimumOrder = 50000,
                StartDate = today.AddDays(-30),
                EndDate = today.AddDays(-5),
                IsEnabled = true
            },

            new PromotionDisplayItem
            {
                Id = 4,
                Code = "CINEDAY",
                Name = "Ngày hội điện ảnh",
                DiscountType = "Phần trăm",
                DiscountValue = 30,
                MaxDiscount = 70000,
                MinimumOrder = 150000,
                StartDate = today,
                EndDate = today.AddDays(15),
                IsEnabled = false
            }
        ]);
    }

    // =========================================
    // 6. XÁC ĐỊNH TRẠNG THÁI HIỂN THỊ
    // =========================================

    private string GetPromotionStatus(PromotionDisplayItem item)
    {
        if (!item.IsEnabled)
            return "Tạm ngưng";

        if (DateTime.Today < item.StartDate.Date)
            return "Sắp diễn ra";

        if (DateTime.Today > item.EndDate.Date)
            return "Đã kết thúc";

        return "Đang áp dụng";
    }

    // =========================================
    // 7. HIỂN THỊ MỨC GIẢM GIÁ
    // =========================================

    private string GetDiscountText(PromotionDisplayItem item)
    {
        return item.DiscountType == "Phần trăm"
            ? $"{item.DiscountValue:0.##}%"
            : $"{item.DiscountValue:N0} đ";
    }

    // =========================================
    // 8. HIỂN THỊ DANH SÁCH
    // =========================================

    private void RefreshPromotionTable()
    {
        _dgvPromotions.Rows.Clear();

        string keyword = _txtSearch.Text.Trim();

        string discountType =
            _cboDiscountType.SelectedItem?.ToString() ?? "Tất cả";

        string status =
            _cboStatus.SelectedItem?.ToString() ?? "Tất cả";

        var result = _promotions
            .Where(item =>
                item.Code.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase
                ) ||
                item.Name.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase
                ))
            .Where(item =>
                discountType == "Tất cả" ||
                item.DiscountType == discountType)
            .Where(item =>
                status == "Tất cả" ||
                GetPromotionStatus(item) == status)
            .OrderByDescending(item => item.StartDate)
            .ToList();

        foreach (PromotionDisplayItem item in result)
        {
            string maxDiscount = item.MaxDiscount <= 0
                ? "Không giới hạn"
                : item.MaxDiscount.ToString("N0") + " đ";

            _dgvPromotions.Rows.Add(
                item.Code,
                item.Name,
                GetDiscountText(item),
                maxDiscount,
                item.MinimumOrder.ToString("N0") + " đ",
                item.StartDate.ToString("dd/MM/yyyy"),
                item.EndDate.ToString("dd/MM/yyyy"),
                GetPromotionStatus(item)
            );
        }

        UpdateStatistics();

        _lblResult.Text =
            $"Đang hiển thị {result.Count} / {_promotions.Count} chương trình";
    }

    // =========================================
    // 9. CẬP NHẬT THỐNG KÊ
    // =========================================

    private void UpdateStatistics()
    {
        _lblTotal.Text = _promotions.Count.ToString();

        _lblActive.Text = _promotions.Count(
            item => GetPromotionStatus(item) == "Đang áp dụng"
        ).ToString();

        _lblUpcoming.Text = _promotions.Count(
            item => GetPromotionStatus(item) == "Sắp diễn ra"
        ).ToString();
    }

    // =========================================
    // 10. ĐỔI MÀU TRẠNG THÁI
    // =========================================

    private void DgvPromotions_CellFormatting(
        object? sender,
        DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        string columnName =
            _dgvPromotions.Columns[e.ColumnIndex].Name;

        if (columnName != "Status")
            return;

        string status = e.Value?.ToString() ?? "";

        e.CellStyle.Font =
            new Font("Segoe UI", 9, FontStyle.Bold);

        e.CellStyle.ForeColor = status switch
        {
            "Đang áp dụng" => Color.FromArgb(52, 211, 153),
            "Sắp diễn ra" => Color.FromArgb(96, 165, 250),
            "Đã kết thúc" => Color.FromArgb(248, 113, 113),
            "Tạm ngưng" => Color.FromArgb(148, 163, 184),
            _ => _textColor
        };
    }

    // =========================================
    // 11. LẤY CHƯƠNG TRÌNH ĐƯỢC CHỌN
    // =========================================

    private PromotionDisplayItem? GetSelectedPromotion()
    {
        if (_dgvPromotions.SelectedRows.Count == 0)
            return null;

        string code =
            _dgvPromotions.SelectedRows[0]
                .Cells["Code"].Value?.ToString() ?? "";

        return _promotions.FirstOrDefault(
            item => item.Code == code
        );
    }

    // =========================================
    // 12. THÊM KHUYẾN MÃI
    // =========================================

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        using PromotionEditDialog dialog = new PromotionEditDialog();

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        bool duplicate = _promotions.Any(item =>
            item.Code.Equals(
                dialog.PromotionData.Code,
                StringComparison.OrdinalIgnoreCase
            ));

        if (duplicate)
        {
            MessageBox.Show(
                "Mã voucher đã tồn tại!",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );

            return;
        }

        PromotionDisplayItem newItem = dialog.PromotionData with
        {
            Id = _nextId++
        };

        _promotions.Add(newItem);

        RefreshPromotionTable();
    }

    // =========================================
    // 13. SỬA KHUYẾN MÃI
    // =========================================

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        PromotionDisplayItem? selected = GetSelectedPromotion();

        if (selected == null)
        {
            MessageBox.Show("Vui lòng chọn chương trình cần sửa!");
            return;
        }

        using PromotionEditDialog dialog =
            new PromotionEditDialog(selected);

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        bool duplicate = _promotions.Any(item =>
            item.Id != selected.Id &&
            item.Code.Equals(
                dialog.PromotionData.Code,
                StringComparison.OrdinalIgnoreCase
            ));

        if (duplicate)
        {
            MessageBox.Show(
                "Mã voucher đã được sử dụng!",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );

            return;
        }

        PromotionDisplayItem updated = dialog.PromotionData with
        {
            Id = selected.Id
        };

        int index = _promotions.FindIndex(
            item => item.Id == selected.Id
        );

        if (index >= 0)
            _promotions[index] = updated;

        RefreshPromotionTable();
    }

    // =========================================
    // 14. XÓA KHUYẾN MÃI
    // =========================================

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        PromotionDisplayItem? selected = GetSelectedPromotion();

        if (selected == null)
        {
            MessageBox.Show("Vui lòng chọn chương trình cần xóa!");
            return;
        }

        DialogResult confirm = MessageBox.Show(
            $"Bạn có chắc muốn xóa voucher '{selected.Code}' không?",
            "Xác nhận xóa",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        );

        if (confirm != DialogResult.Yes)
            return;

        _promotions.Remove(selected);

        RefreshPromotionTable();
    }
}

// =============================================
// 15. DIALOG THÊM / SỬA KHUYẾN MÃI
// =============================================

internal sealed class PromotionEditDialog : Form
{
    public PromotionDisplayItem PromotionData { get; private set; } = new();

    private TextBox _txtCode = null!;

    private TextBox _txtName = null!;

    private ComboBox _cboDiscountType = null!;

    private NumericUpDown _numDiscountValue = null!;

    private NumericUpDown _numMaxDiscount = null!;

    private NumericUpDown _numMinimumOrder = null!;

    private DateTimePicker _dtpStartDate = null!;

    private DateTimePicker _dtpEndDate = null!;

    private CheckBox _chkEnabled = null!;

    private readonly Color _background =
        Color.FromArgb(15, 23, 42);

    private readonly Color _panel =
        Color.FromArgb(30, 41, 59);

    public PromotionEditDialog(PromotionDisplayItem? item = null)
    {
        Text = item == null
            ? "Thêm khuyến mãi"
            : "Chỉnh sửa khuyến mãi";

        Size = new Size(800, 630);
        MinimumSize = new Size(720, 570);

        StartPosition = FormStartPosition.CenterParent;

        FormBorderStyle = FormBorderStyle.FixedDialog;

        MaximizeBox = false;
        MinimizeBox = false;

        BackColor = _background;
        ForeColor = Color.White;

        Font = new Font("Segoe UI", 9);

        BuildInterface(item);
    }

    // =========================================
    // XÂY DỰNG GIAO DIỆN DIALOG
    // =========================================

    private void BuildInterface(PromotionDisplayItem? item)
    {
        TableLayoutPanel mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = _background,
            Padding = new Padding(18)
        };

        mainLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 70));

        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 55));

        // HEADER

        Panel header = new Panel
        {
            Dock = DockStyle.Fill
        };

        Label lblTitle = new Label
        {
            Text = item == null
                ? "THÊM KHUYẾN MÃI MỚI"
                : "CHỈNH SỬA KHUYẾN MÃI",

            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(3, 3)
        };

        Label lblDescription = new Label
        {
            Text = "Thiết lập mã voucher và thông tin chương trình",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(5, 42)
        };

        header.Controls.Add(lblTitle);
        header.Controls.Add(lblDescription);

        // BẢNG TRƯỜNG NHẬP

        TableLayoutPanel fieldsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            BackColor = _panel,
            Padding = new Padding(12)
        };

        fieldsPanel.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 50));

        fieldsPanel.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 50));

        for (int i = 0; i < 5; i++)
        {
            fieldsPanel.RowStyles.Add(
                new RowStyle(SizeType.Percent, 20));
        }

        // MÃ VOUCHER

        _txtCode = new TextBox
        {
            Font = new Font("Segoe UI", 10),
            BackColor = _background,
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            MaxLength = 30,
            PlaceholderText = "Ví dụ: WELCOME20"
        };

        fieldsPanel.Controls.Add(
            CreateField("Mã voucher *", _txtCode),
            0, 0
        );

        // TÊN CHƯƠNG TRÌNH

        _txtName = new TextBox
        {
            Font = new Font("Segoe UI", 10),
            BackColor = _background,
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Tên chương trình khuyến mãi..."
        };

        fieldsPanel.Controls.Add(
            CreateField("Tên chương trình *", _txtName),
            1, 0
        );

        // LOẠI GIẢM GIÁ

        _cboDiscountType = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10),
            BackColor = _background,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        _cboDiscountType.Items.AddRange(new object[]
        {
            "Phần trăm",
            "Số tiền"
        });

        _cboDiscountType.SelectedIndex = 0;

        fieldsPanel.Controls.Add(
            CreateField("Loại giảm giá", _cboDiscountType),
            0, 1
        );

        // GIÁ TRỊ GIẢM

        _numDiscountValue = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 100,
            Value = 20,
            DecimalPlaces = 0,
            ThousandsSeparator = true,
            Font = new Font("Segoe UI", 10),
            BackColor = _background,
            ForeColor = Color.White
        };

        fieldsPanel.Controls.Add(
            CreateField("Giá trị giảm", _numDiscountValue),
            1, 1
        );

        // GIẢM TỐI ĐA

        _numMaxDiscount = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 10000000,
            Value = 50000,
            Increment = 1000,
            ThousandsSeparator = true,
            Font = new Font("Segoe UI", 10),
            BackColor = _background,
            ForeColor = Color.White
        };

        fieldsPanel.Controls.Add(
            CreateField("Giảm tối đa (VNĐ)", _numMaxDiscount),
            0, 2
        );

        // ĐƠN HÀNG TỐI THIỂU

        _numMinimumOrder = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 100000000,
            Value = 100000,
            Increment = 10000,
            ThousandsSeparator = true,
            Font = new Font("Segoe UI", 10),
            BackColor = _background,
            ForeColor = Color.White
        };

        fieldsPanel.Controls.Add(
            CreateField("Đơn hàng tối thiểu (VNĐ)", _numMinimumOrder),
            1, 2
        );

        // NGÀY BẮT ĐẦU

        _dtpStartDate = new DateTimePicker
        {
            Dock = DockStyle.Bottom,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Font = new Font("Segoe UI", 10),
            Value = DateTime.Today,
            Margin = new Padding(0)
        };

        fieldsPanel.Controls.Add(
            CreateField("Ngày bắt đầu", _dtpStartDate),
            0, 3
        );

        // NGÀY KẾT THÚC

        _dtpEndDate = new DateTimePicker
        {
            Dock = DockStyle.Bottom,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Font = new Font("Segoe UI", 10),
            Value = DateTime.Today.AddDays(30),
            Margin = new Padding(0)
        };

        fieldsPanel.Controls.Add(
            CreateField("Ngày kết thúc", _dtpEndDate),
            1, 3
        );

        // TRẠNG THÁI

        _chkEnabled = new CheckBox
        {
            Text = "Cho phép áp dụng",
            Checked = true,
            AutoSize = false,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(52, 211, 153),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Padding = new Padding(5, 0, 0, 0)
        };

        fieldsPanel.Controls.Add(
            CreateField("Trạng thái chương trình", _chkEnabled),
            0, 4
        );

        Label lblHint = new Label
        {
            Text = "0 đồng ở mục giảm tối đa = không giới hạn.",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Segoe UI", 8.5f),
            TextAlign = ContentAlignment.MiddleLeft
        };

        fieldsPanel.Controls.Add(
            CreateField("Ghi chú", lblHint),
            1, 4
        );

        // FOOTER

        FlowLayoutPanel footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 9, 0, 0)
        };

        Button btnSave = CreateDialogButton(
            "Lưu khuyến mãi",
            Color.FromArgb(37, 99, 235),
            140
        );

        Button btnCancel = CreateDialogButton(
            "Hủy",
            Color.FromArgb(71, 85, 105),
            100
        );

        footer.Controls.Add(btnSave);
        footer.Controls.Add(btnCancel);

        // SỰ KIỆN

        _cboDiscountType.SelectedIndexChanged += (_, _) =>
        {
            if (_cboDiscountType.SelectedIndex == 0)
            {
                _numDiscountValue.Maximum = 100;

                if (_numDiscountValue.Value > 100)
                    _numDiscountValue.Value = 100;
            }
            else
            {
                _numDiscountValue.Maximum = 10000000;
            }
        };

        btnSave.Click += BtnSave_Click;

        btnCancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        // GHÉP GIAO DIỆN

        mainLayout.Controls.Add(header, 0, 0);
        mainLayout.Controls.Add(fieldsPanel, 0, 1);
        mainLayout.Controls.Add(footer, 0, 2);

        Controls.Add(mainLayout);

        AcceptButton = btnSave;
        CancelButton = btnCancel;

        if (item != null)
            LoadPromotion(item);
    }

    // =========================================
    // TẠO FIELD
    // =========================================

    private Panel CreateField(string title, Control input)
    {
        Panel field = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = _panel,
            Padding = new Padding(5),
            Margin = new Padding(6)
        };

        Label label = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 27,
            ForeColor = Color.FromArgb(226, 232, 240),
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };

        input.Dock = DockStyle.Bottom;
        input.Height = 34;

        field.Controls.Add(input);
        field.Controls.Add(label);

        return field;
    }

    // =========================================
    // TẠO BUTTON DIALOG
    // =========================================

    private Button CreateDialogButton(
        string text,
        Color color,
        int width)
    {
        Button button = new Button
        {
            Text = text,
            Size = new Size(width, 37),
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(7, 0, 0, 0)
        };

        button.FlatAppearance.BorderSize = 0;

        return button;
    }

    // =========================================
    // NẠP DỮ LIỆU KHI CHỈNH SỬA
    // =========================================

    private void LoadPromotion(PromotionDisplayItem item)
    {
        _txtCode.Text = item.Code;

        _txtName.Text = item.Name;

        _cboDiscountType.SelectedItem =
            item.DiscountType;

        _numDiscountValue.Value = Math.Clamp(
            item.DiscountValue,
            _numDiscountValue.Minimum,
            _numDiscountValue.Maximum
        );

        _numMaxDiscount.Value = Math.Clamp(
            item.MaxDiscount,
            _numMaxDiscount.Minimum,
            _numMaxDiscount.Maximum
        );

        _numMinimumOrder.Value = Math.Clamp(
            item.MinimumOrder,
            _numMinimumOrder.Minimum,
            _numMinimumOrder.Maximum
        );

        _dtpStartDate.Value = item.StartDate;

        _dtpEndDate.Value = item.EndDate;

        _chkEnabled.Checked = item.IsEnabled;
    }

    // =========================================
    // LƯU THÔNG TIN HIỂN THỊ
    // =========================================

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtCode.Text))
        {
            MessageBox.Show("Vui lòng nhập mã voucher!");
            _txtCode.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtName.Text))
        {
            MessageBox.Show("Vui lòng nhập tên chương trình!");
            _txtName.Focus();
            return;
        }

        if (_dtpEndDate.Value.Date < _dtpStartDate.Value.Date)
        {
            MessageBox.Show(
                "Ngày kết thúc không được trước ngày bắt đầu!",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );

            return;
        }

        PromotionData = new PromotionDisplayItem
        {
            Code = _txtCode.Text.Trim().ToUpperInvariant(),

            Name = _txtName.Text.Trim(),

            DiscountType =
                _cboDiscountType.SelectedItem?.ToString()
                ?? "Phần trăm",

            DiscountValue = _numDiscountValue.Value,

            MaxDiscount = _numMaxDiscount.Value,

            MinimumOrder = _numMinimumOrder.Value,

            StartDate = _dtpStartDate.Value.Date,

            EndDate = _dtpEndDate.Value.Date,

            IsEnabled = _chkEnabled.Checked
        };

        DialogResult = DialogResult.OK;

        Close();
    }
}
