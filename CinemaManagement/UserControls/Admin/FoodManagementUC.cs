
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Models;

namespace CinemaManagement.UserControls.Admin;

public partial class FoodManagementUC : UserControl
{
    // =========================================
    // 1. KHAI BÁO DỮ LIỆU
    // =========================================

    private readonly List<Product> _products = new();

    private int _nextId = 1;

    // Các thành phần giao diện
    private DataGridView _dgvProducts = null!;

    private TextBox _txtSearch = null!;

    private ComboBox _cboCategory = null!;
    private ComboBox _cboStatus = null!;

    private Label _lblTotal = null!;
    private Label _lblActive = null!;
    private Label _lblLowStock = null!;
    private Label _lblResult = null!;

    // Các nút chức năng
    private Button _btnAdd = null!;
    private Button _btnEdit = null!;
    private Button _btnDelete = null!;
    private Button _btnImport = null!;
    private Button _btnExport = null!;

    // Màu giao diện
    private readonly Color _backgroundColor =
        Color.FromArgb(15, 23, 42);

    private readonly Color _panelColor =
        Color.FromArgb(30, 41, 59);

    private readonly Color _accentColor =
        Color.FromArgb(37, 99, 235);

    private readonly Color _textColor =
        Color.FromArgb(226, 232, 240);

    // =========================================
    // 2. CONSTRUCTOR
    // =========================================

    public FoodManagementUC()
    {
        InitializeComponent();

        Dock = DockStyle.Fill;
        BackColor = _backgroundColor;

        BuildInterface();

        LoadSampleData();

        RefreshProductTable();
    }

    // =========================================
    // 3. XÂY DỰNG GIAO DIỆN
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

        // Header
        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 85));

        // Thống kê
        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 115));

        // Tìm kiếm và lọc
        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 62));

        // Nút chức năng
        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 53));

        // Bảng sản phẩm
        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        // Chân trang
        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 32));

        // =====================================
        // 3.1 HEADER
        // =====================================

        Panel headerPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = _backgroundColor
        };

        Label lblTitle = new Label
        {
            Text = "QUẢN LÝ BẮP NƯỚC",
            Font = new Font("Segoe UI", 21, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(5, 10)
        };

        Label lblDescription = new Label
        {
            Text = "Quản lý sản phẩm, combo và số lượng tồn kho",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(7, 53)
        };

        headerPanel.Controls.Add(lblTitle);
        headerPanel.Controls.Add(lblDescription);

        // =====================================
        // 3.2 THỐNG KÊ
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
            "TỔNG SẢN PHẨM",
            "Tất cả sản phẩm trong hệ thống",
            Color.FromArgb(37, 99, 235),
            out _lblTotal
        );

        Panel cardActive = CreateStatisticCard(
            "ĐANG KINH DOANH",
            "Sản phẩm đang được bán",
            Color.FromArgb(16, 185, 129),
            out _lblActive
        );

        Panel cardLowStock = CreateStatisticCard(
            "SẮP HẾT HÀNG",
            "Sản phẩm còn không quá 10 đơn vị",
            Color.FromArgb(245, 158, 11),
            out _lblLowStock
        );

        // Tăng chiều cao các thẻ
        cardTotal.Height = 120;
        cardActive.Height = 120;
        cardLowStock.Height = 120;

        // Căn khoảng cách giữa các thẻ
        cardTotal.Margin = new Padding(0, 5, 10, 5);
        cardActive.Margin = new Padding(5, 5, 5, 5);
        cardLowStock.Margin = new Padding(10, 5, 0, 5);

        // Điều chỉnh khu vực chứa 3 thẻ
        statisticPanel.Padding = new Padding(3, 5, 3, 5);
        statisticPanel.Height = 135;

        // Điều chỉnh chiều cao dòng chứa các thẻ
        if (statisticPanel.RowStyles.Count > 0)
        {
            statisticPanel.RowStyles[0].SizeType = SizeType.Absolute;
            statisticPanel.RowStyles[0].Height = 125;
        }

        // Thêm các thẻ vào giao diện
        statisticPanel.Controls.Add(cardTotal, 0, 0);
        statisticPanel.Controls.Add(cardActive, 1, 0);
        statisticPanel.Controls.Add(cardLowStock, 2, 0);

        
        
         // =====================================
         // 3.3 TÌM KIẾM VÀ LỌC
         // =====================================

        TableLayoutPanel filterPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 1,
            BackColor = _panelColor,
            Padding = new Padding(8, 6, 8, 6),
            Margin = new Padding(0)
        };

        // Phân chia chiều rộng các cột
        filterPanel.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 75));

        filterPanel.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        filterPanel.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 68));

        filterPanel.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 95));

        filterPanel.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 72));

        filterPanel.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 110));

        filterPanel.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        // Màu nền chung
        Color inputBackColor = Color.FromArgb(15, 23, 42);

        // -------------------------------------
        // 1. TÌM KIẾM
        // -------------------------------------

        Label lblSearch = CreateLabel("Tìm kiếm:", 9);

        lblSearch.AutoSize = false;
        lblSearch.Dock = DockStyle.Fill;
        lblSearch.TextAlign = ContentAlignment.MiddleLeft;
        lblSearch.Margin = new Padding(0);

        _txtSearch = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9),
            BackColor = inputBackColor,
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Tên sản phẩm...",
            Margin = new Padding(3, 3, 8, 3)
        };

        // -------------------------------------
        // 2. DANH MỤC
        // -------------------------------------

        Label lblCategory = CreateLabel("Danh mục:", 9);

        lblCategory.AutoSize = false;
        lblCategory.Dock = DockStyle.Fill;
        lblCategory.TextAlign = ContentAlignment.MiddleLeft;
        lblCategory.Margin = new Padding(0);

        _cboCategory = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9),
            BackColor = inputBackColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(2, 3, 8, 3)
        };

        _cboCategory.Items.AddRange(new object[]
        {
            "Tất cả",
            "Đồ ăn",
            "Đồ uống",
            "Combo"
        });

        _cboCategory.SelectedIndex = 0;

        // -------------------------------------
        // 3. TRẠNG THÁI
        // -------------------------------------

        Label lblStatus = CreateLabel("Trạng thái:", 9);

        lblStatus.AutoSize = false;
        lblStatus.Dock = DockStyle.Fill;
        lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        lblStatus.Margin = new Padding(0);

        _cboStatus = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9),
            BackColor = inputBackColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(2, 3, 0, 3)
        };

        _cboStatus.Items.AddRange(new object[]
        {
            "Tất cả",
            "Đang kinh doanh",
            "Ngừng kinh doanh"
        });

        _cboStatus.SelectedIndex = 0;

        // -------------------------------------
        // 4. ĐƯA VÀO CÙNG MỘT HÀNG
        // -------------------------------------

        filterPanel.Controls.Add(lblSearch, 0, 0);
        filterPanel.Controls.Add(_txtSearch, 1, 0);

        filterPanel.Controls.Add(lblCategory, 2, 0);
        filterPanel.Controls.Add(_cboCategory, 3, 0);

        filterPanel.Controls.Add(lblStatus, 4, 0);
        filterPanel.Controls.Add(_cboStatus, 5, 0);



        
        // =====================================
        // 3.4 CÁC NÚT CHỨC NĂNG
        // =====================================

        FlowLayoutPanel actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            BackColor = _backgroundColor,
            Padding = new Padding(0, 7, 8, 0),
            Margin = new Padding(0)
        };

        // Thêm sản phẩm
        _btnAdd = CreateButton(
            "+ Thêm sản phẩm",
            _accentColor
        );

        // Sửa sản phẩm
        _btnEdit = CreateButton(
            "Sửa sản phẩm",
            Color.FromArgb(217, 119, 6)
        );

        // Xóa sản phẩm
        _btnDelete = CreateButton(
            "Xóa sản phẩm",
            Color.FromArgb(220, 38, 38)
        );
    
        // Nhập kho
        _btnImport = CreateButton(
            "Nhập kho",
            Color.FromArgb(16, 185, 129)
        );

        // Xuất kho
        _btnExport = CreateButton(
            "Xuất kho",
            Color.FromArgb(234, 88, 12)
        );

        // Thiết lập kích thước và kiểu chữ đồng bộ
        Button[] actionButtons =
        {
            _btnAdd,
            _btnEdit,
            _btnDelete,
            _btnImport,
            _btnExport
        };

        foreach (Button button in actionButtons)
        {
            button.Height = 38;
            button.Font = new Font(
                "Segoe UI",
                9,
                FontStyle.Bold
            );

            button.ForeColor = Color.White;
            button.Cursor = Cursors.Hand;
            button.Margin = new Padding(0, 0, 10, 0);
        }

        // Chiều rộng riêng cho từng nút
        _btnAdd.Width = 165;
        _btnEdit.Width = 135;
        _btnDelete.Width = 125;
        _btnImport.Width = 125;
        _btnExport.Width = 125;

        // Thêm nút vào khu vực chức năng
        actionPanel.Controls.Add(_btnAdd);
        actionPanel.Controls.Add(_btnEdit);
        actionPanel.Controls.Add(_btnDelete);
        actionPanel.Controls.Add(_btnImport);
        actionPanel.Controls.Add(_btnExport);


        
        // =====================================
        // 3.5 BẢNG DANH SÁCH SẢN PHẨM
        // =====================================

        _dgvProducts = new DataGridView
        {
            Dock = DockStyle.Fill,

            // Nền và đường viền
            BackgroundColor = _panelColor,
            BorderStyle = BorderStyle.None,
            GridColor = Color.FromArgb(51, 65, 85),
            CellBorderStyle =
                DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle =
                DataGridViewHeaderBorderStyle.None,

            // Kích thước bảng
            AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill,
            AutoSizeRowsMode =
                DataGridViewAutoSizeRowsMode.None,

            ColumnHeadersHeight = 46,
            ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode.DisableResizing,

            // Quyền thao tác
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            MultiSelect = false,
            RowHeadersVisible = false,

            SelectionMode =
                DataGridViewSelectionMode.FullRowSelect,

            EnableHeadersVisualStyles = false,

            // Chiều cao hàng
            RowTemplate = { Height = 55 },

            // Hiển thị thanh cuộn
            ScrollBars = ScrollBars.Both,

            // Hiển thị gợi ý khi nội dung dài
            ShowCellToolTips = true
        };

        // -------------------------------------
        // HEADER BẢNG
        // -------------------------------------

        _dgvProducts.ColumnHeadersDefaultCellStyle.BackColor =
            Color.FromArgb(30, 41, 59);

        _dgvProducts.ColumnHeadersDefaultCellStyle.ForeColor =
            Color.FromArgb(226, 232, 240);

        _dgvProducts.ColumnHeadersDefaultCellStyle.Font =
            new Font("Segoe UI", 9, FontStyle.Bold);

        _dgvProducts.ColumnHeadersDefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleLeft;

        _dgvProducts.ColumnHeadersDefaultCellStyle.Padding =
            new Padding(10, 0, 5, 0);

        _dgvProducts.ColumnHeadersDefaultCellStyle.SelectionBackColor =
            Color.FromArgb(30, 41, 59);

        // -------------------------------------
        // NỘI DUNG BẢNG
        // -------------------------------------

        _dgvProducts.DefaultCellStyle.BackColor = _panelColor;

        _dgvProducts.DefaultCellStyle.ForeColor =
            Color.FromArgb(226, 232, 240);

        _dgvProducts.DefaultCellStyle.Font =
            new Font("Segoe UI", 9);

        _dgvProducts.DefaultCellStyle.SelectionBackColor =
            Color.FromArgb(51, 65, 85);

        _dgvProducts.DefaultCellStyle.SelectionForeColor =
            Color.White;

        _dgvProducts.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleLeft;

        _dgvProducts.DefaultCellStyle.Padding =
            new Padding(10, 0, 5, 0);

        _dgvProducts.DefaultCellStyle.WrapMode =
            DataGridViewTriState.False;

        // Hàng xen kẽ màu
        _dgvProducts.AlternatingRowsDefaultCellStyle.BackColor =
            Color.FromArgb(37, 49, 68);

        // -------------------------------------
        // TẠO CÁC CỘT
        // -------------------------------------

        _dgvProducts.Columns.Add("Id", "MÃ SP");
        _dgvProducts.Columns.Add("Name", "TÊN SẢN PHẨM");
        _dgvProducts.Columns.Add("Category", "DANH MỤC");
        _dgvProducts.Columns.Add("Price", "GIÁ BÁN");
        _dgvProducts.Columns.Add("Stock", "TỒN KHO");
        _dgvProducts.Columns.Add("Image", "HÌNH ẢNH");
        _dgvProducts.Columns.Add("Status", "TRẠNG THÁI");

        // -------------------------------------
        // ĐỘ RỘNG TƯƠNG ĐỐI CỦA CỘT
        // -------------------------------------

        _dgvProducts.Columns["Id"]!.FillWeight = 55;
        _dgvProducts.Columns["Name"]!.FillWeight = 170;
        _dgvProducts.Columns["Category"]!.FillWeight = 90;
        _dgvProducts.Columns["Price"]!.FillWeight = 100;
        _dgvProducts.Columns["Stock"]!.FillWeight = 75;
        _dgvProducts.Columns["Image"]!.FillWeight = 105;
        _dgvProducts.Columns["Status"]!.FillWeight = 115;

        // -------------------------------------
        // CHIỀU RỘNG TỐI THIỂU
        // -------------------------------------

        _dgvProducts.Columns["Id"]!.MinimumWidth = 55;
        _dgvProducts.Columns["Name"]!.MinimumWidth = 145;
        _dgvProducts.Columns["Category"]!.MinimumWidth = 85;
        _dgvProducts.Columns["Price"]!.MinimumWidth = 95;
        _dgvProducts.Columns["Stock"]!.MinimumWidth = 70;
        _dgvProducts.Columns["Image"]!.MinimumWidth = 85;
        _dgvProducts.Columns["Status"]!.MinimumWidth = 110;
            
        // =====================================
        // CĂN CHỈNH TIÊU ĐỀ VÀ NỘI DUNG CÁC CỘT
        // =====================================

// MÃ SẢN PHẨM
        _dgvProducts.Columns["Id"]!.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleCenter;

        _dgvProducts.Columns["Id"]!.HeaderCell.Style.Alignment =
            DataGridViewContentAlignment.MiddleCenter;

// TÊN SẢN PHẨM
        _dgvProducts.Columns["Name"]!.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleLeft;

        _dgvProducts.Columns["Name"]!.HeaderCell.Style.Alignment =
            DataGridViewContentAlignment.MiddleLeft;

// DANH MỤC
        _dgvProducts.Columns["Category"]!.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleCenter;

        _dgvProducts.Columns["Category"]!.HeaderCell.Style.Alignment =
            DataGridViewContentAlignment.MiddleCenter;

// GIÁ BÁN
        _dgvProducts.Columns["Price"]!.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleRight;

        _dgvProducts.Columns["Price"]!.HeaderCell.Style.Alignment =
            DataGridViewContentAlignment.MiddleRight;

// TỒN KHO
        _dgvProducts.Columns["Stock"]!.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleCenter;

        _dgvProducts.Columns["Stock"]!.HeaderCell.Style.Alignment =
            DataGridViewContentAlignment.MiddleCenter;

// HÌNH ẢNH
        _dgvProducts.Columns["Image"]!.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleCenter;

        _dgvProducts.Columns["Image"]!.HeaderCell.Style.Alignment =
            DataGridViewContentAlignment.MiddleCenter;

// TRẠNG THÁI
        _dgvProducts.Columns["Status"]!.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleCenter;

        _dgvProducts.Columns["Status"]!.HeaderCell.Style.Alignment =
            DataGridViewContentAlignment.MiddleCenter;


        // Không cho sắp xếp tự động khi nhấn tiêu đề
        foreach (DataGridViewColumn column in _dgvProducts.Columns)
        {
            column.SortMode =
                DataGridViewColumnSortMode.NotSortable;
        }


        // =====================================
        // 3.6 CHÂN TRANG
        // =====================================

        _lblResult = new Label
        {
            Text = "Tổng số sản phẩm: 0",
            Dock = DockStyle.Fill,
            ForeColor = _textColor,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // =====================================
        // 3.7 GHÉP GIAO DIỆN
        // =====================================

        mainLayout.Controls.Add(headerPanel, 0, 0);
        mainLayout.Controls.Add(statisticPanel, 0, 1);
        mainLayout.Controls.Add(filterPanel, 0, 2);
        mainLayout.Controls.Add(actionPanel, 0, 3);
        mainLayout.Controls.Add(_dgvProducts, 0, 4);
        mainLayout.Controls.Add(_lblResult, 0, 5);

        Controls.Add(mainLayout);

        // =====================================
        // 3.8 GẮN SỰ KIỆN
        // =====================================

        _txtSearch.TextChanged += (s, e) =>
            RefreshProductTable();

        _cboCategory.SelectedIndexChanged += (s, e) =>
            RefreshProductTable();

        _cboStatus.SelectedIndexChanged += (s, e) =>
            RefreshProductTable();

        _dgvProducts.CellFormatting +=
            DgvProducts_CellFormatting;

        _btnAdd.Click += BtnAdd_Click;
        _btnEdit.Click += BtnEdit_Click;
        _btnDelete.Click += BtnDelete_Click;

        _btnImport.Click += BtnImport_Click;
        _btnExport.Click += BtnExport_Click;
    }

    // =========================================
    // 4. TẠO LABEL
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
    // 5. TẠO BUTTON
    // =========================================

    private Button CreateButton(string text, Color color)
    {
        Button button = new Button
        {
            Text = text,
            Size = new Size(130, 38),
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
    // 6. TẠO THẺ THỐNG KÊ
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
            Margin = new Padding(0),
            MinimumSize = new Size(0, 105)
        };

        // Thanh màu bên trái
        Panel accent = new Panel
        {
            Dock = DockStyle.Left,
            Width = 5,
            BackColor = accentColor
        };

        // Tiêu đề thẻ
        Label lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(203, 213, 225),
            AutoSize = false,
            Location = new Point(20, 9),
            Size = new Size(420, 23),
            TextAlign = ContentAlignment.MiddleLeft,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        // Số liệu thống kê
        valueLabel = new Label
        {
            Text = "0",
            Font = new Font("Segoe UI", 23, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = false,
            Location = new Point(20, 32),
            Size = new Size(150, 40),
            TextAlign = ContentAlignment.MiddleLeft,
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };

        // Nội dung mô tả
        Label lblDescription = new Label
        {
            Text = description,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = Color.FromArgb(226, 232, 240),
            AutoSize = false,
            Location = new Point(22, 72),
            Size = new Size(420, 22),
            TextAlign = ContentAlignment.MiddleLeft,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        card.Controls.Add(accent);
        card.Controls.Add(lblTitle);
        card.Controls.Add(valueLabel);
        card.Controls.Add(lblDescription);

        return card;
    }

    // =========================================
    // 7. DỮ LIỆU SẢN PHẨM MẪU
    // =========================================

    private void LoadSampleData()
    {
        _products.Clear();

        _products.AddRange(new[]
        {
            new Product
            {
                Id = 1,
                Name = "Bắp rang bơ nhỏ",
                Category = "Food",
                Price = 35000,
                StockQuantity = 50,
                ImageUrl = "",
                IsActive = true
            },

            new Product
            {
                Id = 2,
                Name = "Bắp rang bơ lớn",
                Category = "Food",
                Price = 55000,
                StockQuantity = 35,
                ImageUrl = "",
                IsActive = true
            },

            new Product
            {
                Id = 3,
                Name = "Nước Coca-Cola",
                Category = "Drink",
                Price = 30000,
                StockQuantity = 45,
                ImageUrl = "",
                IsActive = true
            },

            new Product
            {
                Id = 4,
                Name = "Nước suối",
                Category = "Drink",
                Price = 20000,
                StockQuantity = 8,
                ImageUrl = "",
                IsActive = true
            },

            new Product
            {
                Id = 5,
                Name = "Combo Couple",
                Category = "Combo",
                Price = 120000,
                StockQuantity = 15,
                ImageUrl = "",
                IsActive = true
            },

            new Product
            {
                Id = 6,
                Name = "Combo Family",
                Category = "Combo",
                Price = 180000,
                StockQuantity = 5,
                ImageUrl = "",
                IsActive = false
            }
        });

        _nextId = _products.Max(p => p.Id) + 1;
    }

    // =========================================
    // 8. CHUYỂN DANH MỤC SANG TIẾNG VIỆT
    // =========================================

    private string GetCategoryText(string category)
    {
        return category switch
        {
            "Food" => "Đồ ăn",
            "Drink" => "Đồ uống",
            "Combo" => "Combo",
            _ => category
        };
    }

    // =========================================
    // 9. HIỂN THỊ DANH SÁCH SẢN PHẨM
    // =========================================

    private void RefreshProductTable()
    {
        _dgvProducts.Rows.Clear();

        string keyword = _txtSearch.Text
            .Trim()
            .ToLowerInvariant();

        string category =
            _cboCategory.SelectedItem?.ToString() ?? "Tất cả";

        string status =
            _cboStatus.SelectedItem?.ToString() ?? "Tất cả";

        var result = _products.Where(product =>
        {
            bool matchName = product.Name
                .ToLowerInvariant()
                .Contains(keyword);

            bool matchCategory =
                category == "Tất cả" ||
                GetCategoryText(product.Category) == category;

            bool matchStatus = status switch
            {
                "Đang kinh doanh" => product.IsActive,
                "Ngừng kinh doanh" => !product.IsActive,
                _ => true
            };

            return matchName && matchCategory && matchStatus;

        }).OrderBy(p => p.Id).ToList();

        foreach (Product product in result)
        {
            string imageName = string.IsNullOrWhiteSpace(
                product.ImageUrl)
                ? "Chưa có ảnh"
                : Path.GetFileName(product.ImageUrl);

            _dgvProducts.Rows.Add(
                product.Id,
                product.Name,
                GetCategoryText(product.Category),
                product.Price.ToString("N0") + " đ",
                product.StockQuantity,
                imageName,
                product.IsActive
                    ? "Đang kinh doanh"
                    : "Ngừng kinh doanh"
            );
        }

        UpdateStatistics();

        _lblResult.Text =
            $"Đang hiển thị {result.Count} / {_products.Count} sản phẩm";
    }

    // =========================================
    // 10. CẬP NHẬT THỐNG KÊ
    // =========================================

    private void UpdateStatistics()
    {
        _lblTotal.Text = _products.Count.ToString();

        _lblActive.Text = _products.Count(
            p => p.IsActive
        ).ToString();

        _lblLowStock.Text = _products.Count(
            p => p.StockQuantity <= 10 && p.IsActive
        ).ToString();
    }

    // =========================================
    // 11. ĐỔI MÀU TỒN KHO VÀ TRẠNG THÁI
    // =========================================

    private void DgvProducts_CellFormatting(
        object? sender,
        DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        string columnName =
            _dgvProducts.Columns[e.ColumnIndex].Name;

        // Sắp hết hàng
        if (columnName == "Stock" &&
            int.TryParse(e.Value?.ToString(), out int stock))
        {
            if (stock <= 10)
            {
                e.CellStyle.ForeColor =
                    Color.FromArgb(248, 113, 113);

                e.CellStyle.Font =
                    new Font("Segoe UI", 9, FontStyle.Bold);
            }
        }

        // Trạng thái kinh doanh
        if (columnName == "Status")
        {
            string status = e.Value?.ToString() ?? "";

            e.CellStyle.ForeColor =
                status == "Đang kinh doanh"
                    ? Color.FromArgb(52, 211, 153)
                    : Color.FromArgb(248, 113, 113);
        }
    }

    // =========================================
    // 12. THÊM SẢN PHẨM
    // =========================================

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        using ProductEditDialog dialog =
            new ProductEditDialog();

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        bool exists = _products.Any(p =>
            p.Name.Equals(
                dialog.ProductData.Name,
                StringComparison.OrdinalIgnoreCase
            )
        );

        if (exists)
        {
            MessageBox.Show("Tên sản phẩm đã tồn tại!");
            return;
        }

        Product newProduct = dialog.ProductData;

        newProduct.Id = _nextId++;

        _products.Add(newProduct);

        RefreshProductTable();

        MessageBox.Show(
            "Thêm sản phẩm thành công!",
            "Thông báo",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }

    // =========================================
    // 13. SỬA SẢN PHẨM
    // =========================================

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        if (_dgvProducts.SelectedRows.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn sản phẩm cần sửa!");
            return;
        }

        int id = Convert.ToInt32(
            _dgvProducts.SelectedRows[0].Cells["Id"].Value
        );

        Product? product = _products.FirstOrDefault(
            p => p.Id == id
        );

        if (product == null)
        {
            MessageBox.Show("Không tìm thấy sản phẩm!");
            return;
        }

        using ProductEditDialog dialog =
            new ProductEditDialog(product);

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        bool duplicate = _products.Any(p =>
            p.Id != id &&
            p.Name.Equals(
                dialog.ProductData.Name,
                StringComparison.OrdinalIgnoreCase
            )
        );

        if (duplicate)
        {
            MessageBox.Show("Tên sản phẩm đã tồn tại!");
            return;
        }

        int index = _products.FindIndex(p => p.Id == id);

        if (index < 0)
            return;

        Product updatedProduct = dialog.ProductData;

        updatedProduct.Id = id;

        _products[index] = updatedProduct;

        RefreshProductTable();

        MessageBox.Show("Cập nhật sản phẩm thành công!");
    }

    // =========================================
    // 14. XÓA SẢN PHẨM
    // =========================================

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (_dgvProducts.SelectedRows.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn sản phẩm cần xóa!");
            return;
        }

        int id = Convert.ToInt32(
            _dgvProducts.SelectedRows[0].Cells["Id"].Value
        );

        Product? product = _products.FirstOrDefault(
            p => p.Id == id
        );

        if (product == null)
            return;

        DialogResult confirm = MessageBox.Show(
            $"Bạn có chắc muốn xóa sản phẩm \"{product.Name}\"?",
            "Xác nhận xóa",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        );

        if (confirm != DialogResult.Yes)
            return;

        _products.Remove(product);

        RefreshProductTable();

        MessageBox.Show("Xóa sản phẩm thành công!");
    }

    // =========================================
    // 15. NHẬP KHO
    // =========================================

    private void BtnImport_Click(object? sender, EventArgs e)
    {
        if (_dgvProducts.SelectedRows.Count == 0)
        {
            MessageBox.Show(
                "Vui lòng chọn sản phẩm cần nhập kho!");
            return;
        }

        int id = Convert.ToInt32(
            _dgvProducts.SelectedRows[0].Cells["Id"].Value
        );

        Product? product = _products.FirstOrDefault(
            p => p.Id == id
        );

        if (product == null)
            return;

        using StockQuantityDialog dialog =
            new StockQuantityDialog("NHẬP KHO");

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        if (product.StockQuantity > int.MaxValue - dialog.Quantity)
        {
            MessageBox.Show("Số lượng tồn kho vượt giới hạn!");
            return;
        }

        product.StockQuantity += dialog.Quantity;

        RefreshProductTable();

        MessageBox.Show(
            $"Nhập kho thành công!\n" +
            $"Sản phẩm: {product.Name}\n" +
            $"Số lượng hiện tại: {product.StockQuantity}",
            "Thông báo",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }

    // =========================================
    // 16. XUẤT KHO
    // =========================================

    private void BtnExport_Click(object? sender, EventArgs e)
    {
        if (_dgvProducts.SelectedRows.Count == 0)
        {
            MessageBox.Show(
                "Vui lòng chọn sản phẩm cần xuất kho!");
            return;
        }

        int id = Convert.ToInt32(
            _dgvProducts.SelectedRows[0].Cells["Id"].Value
        );

        Product? product = _products.FirstOrDefault(
            p => p.Id == id
        );

        if (product == null)
            return;

        using StockQuantityDialog dialog =
            new StockQuantityDialog("XUẤT KHO");

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        if (dialog.Quantity > product.StockQuantity)
        {
            MessageBox.Show(
                "Số lượng xuất lớn hơn số lượng tồn kho!",
                "Không đủ hàng",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );

            return;
        }

        product.StockQuantity -= dialog.Quantity;

        RefreshProductTable();

        MessageBox.Show(
            $"Xuất kho thành công!\n" +
            $"Sản phẩm: {product.Name}\n" +
            $"Số lượng còn lại: {product.StockQuantity}",
            "Thông báo",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }
}

// =================================================
// 17. DIALOG THÊM / SỬA SẢN PHẨM
// =================================================

internal sealed class ProductEditDialog : Form
{
    public Product ProductData { get; private set; } = new();

    private TextBox _txtName = null!;
    private TextBox _txtImage = null!;

    private ComboBox _cboCategory = null!;

    private NumericUpDown _numPrice = null!;
    private NumericUpDown _numStock = null!;

    private CheckBox _chkActive = null!;

    public ProductEditDialog(Product? product = null)
    {
        BuildInterface(product);
    }

    // =====================================
    // XÂY DỰNG GIAO DIỆN DIALOG
    // =====================================

   
private void BuildInterface(Product? product)
{
    Text = product == null
        ? "Thêm sản phẩm"
        : "Chỉnh sửa sản phẩm";

    Size = new Size(740, 580);
    MinimumSize = new Size(680, 540);

    StartPosition = FormStartPosition.CenterParent;
    FormBorderStyle = FormBorderStyle.FixedDialog;

    MaximizeBox = false;
    MinimizeBox = false;

    BackColor = Color.FromArgb(15, 23, 42);
    ForeColor = Color.White;
    Font = new Font("Segoe UI", 10);

    Color background = Color.FromArgb(15, 23, 42);
    Color panel = Color.FromArgb(30, 41, 59);
    Color input = Color.FromArgb(15, 23, 42);
    Color textColor = Color.FromArgb(226, 232, 240);

    // =====================================
    // 1. LAYOUT CHÍNH
    // =====================================

    TableLayoutPanel mainLayout = new TableLayoutPanel
    {
        Dock = DockStyle.Fill,
        ColumnCount = 1,
        RowCount = 3,
        BackColor = background,
        Padding = new Padding(20)
    };

    mainLayout.ColumnStyles.Add(
        new ColumnStyle(SizeType.Percent, 100));

    mainLayout.RowStyles.Add(
        new RowStyle(SizeType.Absolute, 75));

    mainLayout.RowStyles.Add(
        new RowStyle(SizeType.Percent, 100));

    mainLayout.RowStyles.Add(
        new RowStyle(SizeType.Absolute, 62));

    // =====================================
    // 2. HEADER
    // =====================================

    Panel headerPanel = new Panel
    {
        Dock = DockStyle.Fill,
        BackColor = background,
        Margin = new Padding(0)
    };

    Label lblTitle = new Label
    {
        Text = product == null
            ? "THÊM SẢN PHẨM MỚI"
            : "CHỈNH SỬA SẢN PHẨM",

        Font = new Font("Segoe UI", 18, FontStyle.Bold),
        ForeColor = Color.White,
        AutoSize = true,
        Location = new Point(3, 5)
    };

    Label lblDescription = new Label
    {
        Text = "Nhập đầy đủ thông tin sản phẩm",
        Font = new Font("Segoe UI", 9),
        ForeColor = Color.FromArgb(148, 163, 184),
        AutoSize = true,
        Location = new Point(5, 45)
    };

    headerPanel.Controls.Add(lblTitle);
    headerPanel.Controls.Add(lblDescription);

    // =====================================
    // 3. KHU VỰC NHẬP LIỆU
    // =====================================

    TableLayoutPanel fieldsPanel = new TableLayoutPanel
    {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        RowCount = 3,
        BackColor = panel,
        Padding = new Padding(12),
        Margin = new Padding(0)
    };

    fieldsPanel.ColumnStyles.Add(
        new ColumnStyle(SizeType.Percent, 50));

    fieldsPanel.ColumnStyles.Add(
        new ColumnStyle(SizeType.Percent, 50));

    for (int i = 0; i < 3; i++)
    {
        fieldsPanel.RowStyles.Add(
            new RowStyle(SizeType.Percent, 33.33f));
    }

    // =====================================
    // TÊN SẢN PHẨM
    // =====================================

    _txtName = new TextBox
    {
        Font = new Font("Segoe UI", 10),
        BackColor = input,
        ForeColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle,
        PlaceholderText = "Nhập tên sản phẩm..."
    };

    fieldsPanel.Controls.Add(
        CreateField("Tên sản phẩm *", _txtName),
        0, 0
    );

    // =====================================
    // DANH MỤC
    // =====================================

    _cboCategory = new ComboBox
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Font = new Font("Segoe UI", 10),
        BackColor = input,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat
    };

    _cboCategory.Items.AddRange(new object[]
    {
        "Đồ ăn",
        "Đồ uống",
        "Combo"
    });

    _cboCategory.SelectedIndex = 0;

    fieldsPanel.Controls.Add(
        CreateField("Danh mục *", _cboCategory),
        1, 0
    );

    // =====================================
    // GIÁ BÁN
    // =====================================

    _numPrice = new NumericUpDown
    {
        Minimum = 1000,
        Maximum = 10000000,
        Increment = 1000,
        Value = 30000,
        ThousandsSeparator = true,
        Font = new Font("Segoe UI", 10),
        BackColor = input,
        ForeColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle
    };

    fieldsPanel.Controls.Add(
        CreateField("Giá bán (VNĐ) *", _numPrice),
        0, 1
    );

    // =====================================
    // TỒN KHO
    // =====================================

    _numStock = new NumericUpDown
    {
        Minimum = 0,
        Maximum = 100000,
        Increment = 1,
        Value = 10,
        ThousandsSeparator = true,
        Font = new Font("Segoe UI", 10),
        BackColor = input,
        ForeColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle
    };

    fieldsPanel.Controls.Add(
        CreateField("Số lượng tồn kho", _numStock),
        1, 1
    );

    // =====================================
    // HÌNH ẢNH
    // =====================================

    _txtImage = new TextBox
    {
        Font = new Font("Segoe UI", 9),
        BackColor = input,
        ForeColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle,
        ReadOnly = true
    };

    Button btnBrowse = new Button
    {
        Text = "Chọn ảnh",
        Dock = DockStyle.Fill,
        BackColor = Color.FromArgb(71, 85, 105),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        Cursor = Cursors.Hand,
        Margin = new Padding(5, 0, 0, 0)
    };

    btnBrowse.FlatAppearance.BorderSize = 0;

    TableLayoutPanel imagePanel = new TableLayoutPanel
    {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        RowCount = 1,
        BackColor = panel
    };

    imagePanel.ColumnStyles.Add(
        new ColumnStyle(SizeType.Percent, 100));

    imagePanel.ColumnStyles.Add(
        new ColumnStyle(SizeType.Absolute, 100));

    imagePanel.RowStyles.Add(
        new RowStyle(SizeType.Percent, 100));

    imagePanel.Controls.Add(_txtImage, 0, 0);
    imagePanel.Controls.Add(btnBrowse, 1, 0);

    btnBrowse.Click += BtnBrowse_Click;

    fieldsPanel.Controls.Add(
        CreateField("Hình ảnh sản phẩm", imagePanel),
        0, 2
    );

    // =====================================
    // TRẠNG THÁI
    // =====================================

    _chkActive = new CheckBox
    {
        Text = "Đang kinh doanh",
        Checked = true,
        AutoSize = false,
        Dock = DockStyle.Fill,
        ForeColor = Color.FromArgb(52, 211, 153),
        BackColor = panel,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        Padding = new Padding(8, 0, 0, 0)
    };

    fieldsPanel.Controls.Add(
        CreateField("Trạng thái", _chkActive),
        1, 2
    );

    // =====================================
    // 4. FOOTER - NÚT THAO TÁC
    // =====================================

    FlowLayoutPanel footerPanel = new FlowLayoutPanel
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.RightToLeft,
        WrapContents = false,
        BackColor = background,
        Padding = new Padding(0, 12, 0, 0)
    };

    Button btnSave = CreateDialogButton(
        "Lưu sản phẩm",
        Color.FromArgb(37, 99, 235)
    );

    Button btnCancel = CreateDialogButton(
        "Hủy",
        Color.FromArgb(71, 85, 105)
    );

    btnSave.Size = new Size(140, 38);
    btnCancel.Size = new Size(110, 38);

    // Nút Lưu nằm bên phải
    footerPanel.Controls.Add(btnSave);
    footerPanel.Controls.Add(btnCancel);

    btnSave.Click += BtnSave_Click;

    btnCancel.Click += (s, e) =>
    {
        DialogResult = DialogResult.Cancel;
        Close();
    };

    // =====================================
    // 5. GHÉP GIAO DIỆN
    // =====================================

    mainLayout.Controls.Add(headerPanel, 0, 0);
    mainLayout.Controls.Add(fieldsPanel, 0, 1);
    mainLayout.Controls.Add(footerPanel, 0, 2);

    Controls.Add(mainLayout);

    AcceptButton = btnSave;
    CancelButton = btnCancel;

    // Nếu là chỉnh sửa thì nạp thông tin hiện tại
    if (product != null)
    {
        LoadProduct(product);
    }
}


    // =====================================
    // TẠO TRƯỜNG NHẬP LIỆU
    // =====================================

    
    private Panel CreateField(string title, Control input)
    {
        Panel field = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(8),
            Padding = new Padding(5),
            BackColor = Color.FromArgb(30, 41, 59)
        };

        Label label = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 28,
            ForeColor = Color.FromArgb(226, 232, 240),
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        input.Dock = DockStyle.Bottom;
        input.Height = 36;
        input.Margin = new Padding(0);

        field.Controls.Add(input);
        field.Controls.Add(label);

        return field;
    }


    // =====================================
    // TẠO BUTTON DIALOG
    // =====================================

    private Button CreateDialogButton(
        string text,
        Color color)
    {
        Button button = new Button
        {
            Text = text,
            Size = new Size(125, 37),
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

    // =====================================
    // NẠP THÔNG TIN SẢN PHẨM KHI SỬA
    // =====================================

    private void LoadProduct(Product product)
    {
        _txtName.Text = product.Name;

        _cboCategory.SelectedItem = product.Category switch
        {
            "Food" => "Đồ ăn",
            "Drink" => "Đồ uống",
            "Combo" => "Combo",
            _ => "Đồ ăn"
        };

        _numPrice.Value = Math.Clamp(
            product.Price,
            _numPrice.Minimum,
            _numPrice.Maximum
        );

        _numStock.Value = Math.Clamp(
            product.StockQuantity,
            (int)_numStock.Minimum,
            (int)_numStock.Maximum
        );

        _txtImage.Text = product.ImageUrl;

        _chkActive.Checked = product.IsActive;
    }

    // =====================================
    // CHỌN ẢNH SẢN PHẨM
    // =====================================

    private void BtnBrowse_Click(object? sender, EventArgs e)
    {
        using OpenFileDialog dialog = new OpenFileDialog
        {
            Title = "Chọn hình ảnh sản phẩm",
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _txtImage.Text = dialog.FileName;
        }
    }

    // =====================================
    // LƯU SẢN PHẨM
    // =====================================

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtName.Text))
        {
            MessageBox.Show("Vui lòng nhập tên sản phẩm!");

            _txtName.Focus();

            return;
        }

        if (_cboCategory.SelectedItem == null)
        {
            MessageBox.Show("Vui lòng chọn danh mục!");
            return;
        }

        string category = _cboCategory.SelectedItem.ToString() switch
        {
            "Đồ ăn" => "Food",
            "Đồ uống" => "Drink",
            "Combo" => "Combo",
            _ => "Food"
        };

        ProductData = new Product
        {
            Name = _txtName.Text.Trim(),

            Category = category,

            Price = _numPrice.Value,

            StockQuantity = (int)_numStock.Value,

            ImageUrl = _txtImage.Text.Trim(),

            IsActive = _chkActive.Checked
        };

        DialogResult = DialogResult.OK;

        Close();
    }
}

// =================================================
// 18. DIALOG NHẬP KHO / XUẤT KHO
// =================================================

internal sealed class StockQuantityDialog : Form
{
    // Số lượng người dùng nhập
    public int Quantity => (int)_numQuantity.Value;

    private NumericUpDown _numQuantity = null!;

    public StockQuantityDialog(string title)
    {
        Text = title;

        Size = new Size(520, 390);
        MinimumSize = new Size(500, 370);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(15, 23, 42);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);
        BuildInterface(title);
    }

    // =====================================
    // XÂY DỰNG GIAO DIỆN NHẬP / XUẤT KHO
    // =====================================


    private void BuildInterface(string title)
    {
        bool isImport = title.Contains(
            "NHẬP",
            StringComparison.OrdinalIgnoreCase
        );

        Color background = Color.FromArgb(15, 23, 42);
        Color panel = Color.FromArgb(30, 41, 59);
        Color accentColor = isImport
            ? Color.FromArgb(16, 185, 129)
            : Color.FromArgb(234, 88, 12);

        // =====================================
        // 1. LAYOUT CHÍNH
        // =====================================

        TableLayoutPanel mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = background,
            Padding = new Padding(20)
        };

        mainLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 75));

        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 60));
        // =====================================
        // 2. HEADER
        // =====================================

        Panel headerPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = background
        };

        Panel accent = new Panel
        {
            Dock = DockStyle.Left,
            Width = 5,
            BackColor = accentColor
        };

        Label lblTitle = new Label
        {
            Text = isImport ? "NHẬP KHO SẢN PHẨM" : "XUẤT KHO SẢN PHẨM",
            Font = new Font("Segoe UI", 17, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(18, 5)
        };

        Label lblDescription = new Label
        {
            Text = isImport
                ? "Cập nhật số lượng sản phẩm nhập vào kho"
                : "Ghi nhận số lượng sản phẩm xuất khỏi kho",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(20, 43)
        };

        headerPanel.Controls.Add(lblTitle);
        headerPanel.Controls.Add(lblDescription);
        headerPanel.Controls.Add(accent);

        
// =====================================
// 3. KHU VỰC NHẬP SỐ LƯỢNG
// =====================================

        TableLayoutPanel quantityPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = panel,
            Padding = new Padding(18),
            Margin = new Padding(0, 5, 0, 5)
        };

        quantityPanel.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

// Chia chiều cao cố định để không bị chồng chữ
        quantityPanel.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 32));

        quantityPanel.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 52));

        quantityPanel.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 28));

// Nhãn số lượng
        Label lblQuantity = new Label
        {
            Text = "SỐ LƯỢNG SẢN PHẨM",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(203, 213, 225),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false
        };

// Ô nhập số lượng
        _numQuantity = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Minimum = 1,
            Maximum = 100000,
            Value = 1,
            ThousandsSeparator = true,
            TextAlign = HorizontalAlignment.Left,
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            BackColor = background,
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 4, 0, 4)
        };

// Dòng mô tả
        Label lblUnit = new Label
        {
            Text = "Đơn vị tính: sản phẩm",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Segoe UI", 9),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false
        };

// Đưa vào từng hàng riêng biệt
        quantityPanel.Controls.Add(lblQuantity, 0, 0);
        quantityPanel.Controls.Add(_numQuantity, 0, 1);
        quantityPanel.Controls.Add(lblUnit, 0, 2);


        // =====================================
        // 4. CÁC NÚT THAO TÁC
        // =====================================

        FlowLayoutPanel footerPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = background,
            Padding = new Padding(0, 10, 0, 0)
        };

        Button btnConfirm = new Button
        {
            Text = isImport ? "Xác nhận nhập kho" : "Xác nhận xuất kho",
            Size = new Size(165, 38),
            BackColor = accentColor,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(8, 0, 0, 0)
        };

        Button btnCancel = new Button
        {
            Text = "Hủy",
            Size = new Size(100, 38),
            BackColor = Color.FromArgb(71, 85, 105),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(8, 0, 0, 0)
        };

        btnConfirm.FlatAppearance.BorderSize = 0;
        btnCancel.FlatAppearance.BorderSize = 0;

        // Hiệu ứng khi rê chuột
        btnConfirm.FlatAppearance.MouseOverBackColor =
            ControlPaint.Light(accentColor, 0.15f);

        btnCancel.FlatAppearance.MouseOverBackColor =
            Color.FromArgb(100, 116, 139);

        footerPanel.Controls.Add(btnConfirm);
        footerPanel.Controls.Add(btnCancel);

        // =====================================
        // 5. GẮN SỰ KIỆN
        // =====================================

        btnConfirm.Click += (s, e) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        btnCancel.Click += (s, e) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        // =====================================
        // 6. GHÉP GIAO DIỆN
        // =====================================

        mainLayout.Controls.Add(headerPanel, 0, 0);
        mainLayout.Controls.Add(quantityPanel, 0, 1);
        mainLayout.Controls.Add(footerPanel, 0, 2);

        Controls.Add(mainLayout);

        AcceptButton = btnConfirm;
        CancelButton = btnCancel;
    }
}
