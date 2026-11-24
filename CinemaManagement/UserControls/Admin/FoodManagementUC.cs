using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Models;
using CinemaManagement.Services;

namespace CinemaManagement.UserControls.Admin;

public partial class FoodManagementUC : UserControl
{
    private readonly FoodService _foodService = new();
    private readonly List<Product> _products = new();

    private Panel _root = null!;
    private TableLayoutPanel _layout = null!;
    private TableLayoutPanel _stats = null!;
    private TableLayoutPanel _filters = null!;
    private FlowLayoutPanel _actions = null!;
    private DataGridView _grid = null!;

    private TextBox _txtSearch = null!;
    private ComboBox _cboCategory = null!;
    private ComboBox _cboStatus = null!;

    private Label _lblTotal = null!;
    private Label _lblActive = null!;
    private Label _lblLowStock = null!;
    private Label _lblResult = null!;

    private Panel _cardTotal = null!;
    private Panel _cardActive = null!;
    private Panel _cardLowStock = null!;

    private Button _btnAdd = null!;
    private Button _btnEdit = null!;
    private Button _btnDelete = null!;
    private Button _btnImport = null!;
    private Button _btnExport = null!;

    private bool _updatingLayout;

    private readonly Color _background = Color.FromArgb(15, 23, 42);
    private readonly Color _panel = Color.FromArgb(30, 41, 59);
    private readonly Color _input = Color.FromArgb(15, 23, 42);
    private readonly Color _text = Color.FromArgb(226, 232, 240);
    private readonly Color _muted = Color.FromArgb(203, 213, 225);

    public FoodManagementUC()
    {
        AutoScaleMode = AutoScaleMode.Dpi; // WinForms tự scale theo DPI/Windows Display Scale.
        Dock = DockStyle.Fill;
        BackColor = _background;
        Font = new Font("Segoe UI", 10f, FontStyle.Regular);

        BuildInterface();

        SizeChanged += (_, _) => ApplyResponsiveLayout();

        LoadProductsFromDatabase();
    }

    // =====================================================
    // GIAO DIỆN CHÍNH
    // =====================================================

    private void BuildInterface()
    {
        _root = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = false,
            BackColor = _background
        };

        _layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = _background,
            Padding = new Padding(12),
            Margin = new Padding(0)
        };

        _layout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

        // Phải thêm header vào hàng đầu tiên của _layout.
        // Trước đây chỉ gọi BuildHeader() nhưng không đưa control vào giao diện,
        // nên vùng tiêu đề trống dù các thẻ thống kê vẫn hiển thị.
        _layout.Controls.Add(BuildHeader(), 0, 0);
        BuildStatistics();
        BuildFilters();
        BuildActions();
        BuildGrid();
        BuildFooter();

        _root.Controls.Add(_layout);
        Controls.Add(_root);

        ApplyResponsiveLayout();
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = _background,
            Margin = new Padding(0),
            Padding = new Padding(10, 4, 0, 4)
        };

        header.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        header.RowStyles.Add(
            new RowStyle(SizeType.Percent, 58));

        header.RowStyles.Add(
            new RowStyle(SizeType.Percent, 42));

        var lblTitle = new Label
        {
            Text = "QUẢN LÝ BẮP NƯỚC",
            Dock = DockStyle.Fill,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 21F, FontStyle.Bold),
            ForeColor = Color.White,
            Margin = new Padding(0)
        };

        var lblSubtitle = new Label
        {
            Text = "Quản lý sản phẩm, combo và số lượng tồn kho",
            Dock = DockStyle.Fill,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.FromArgb(190, 205, 225),
            Margin = new Padding(0)
        };

        header.Controls.Add(lblTitle, 0, 0);
        header.Controls.Add(lblSubtitle, 0, 1);

        return header;
    }

    private void BuildStatistics()
    {
        _stats = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = _background,
            Padding = new Padding(0, 5, 0, 5),
            Margin = new Padding(0)
        };

        _cardTotal = CreateStatisticCard(
            "TỔNG SẢN PHẨM",
            "Tất cả sản phẩm trong hệ thống",
            Color.FromArgb(37, 99, 235),
            out _lblTotal);

        _cardActive = CreateStatisticCard(
            "ĐANG KINH DOANH",
            "Sản phẩm đang được bán",
            Color.FromArgb(16, 185, 129),
            out _lblActive);

        _cardLowStock = CreateStatisticCard(
            "SẮP HẾT HÀNG",
            "Sản phẩm còn không quá 10 đơn vị",
            Color.FromArgb(245, 158, 11),
            out _lblLowStock);

        _stats.Controls.Add(_cardTotal, 0, 0);
        _stats.Controls.Add(_cardActive, 1, 0);
        _stats.Controls.Add(_cardLowStock, 2, 0);

        _layout.Controls.Add(_stats, 0, 1);
    }

    private Panel CreateStatisticCard(
        string title,
        string description,
        Color accent,
        out Label value)
    {
        Panel card = new()
        {
            Dock = DockStyle.Fill,
            BackColor = _panel,
            Margin = new Padding(5),
            Padding = new Padding(0),
            MinimumSize = new Size(0, 0)
        };

        Panel line = new()
        {
            Dock = DockStyle.Left,
            Width = 5,
            BackColor = accent
        };

        // Dùng TableLayoutPanel thay cho tọa độ cố định.
        // Các hàng tự chia theo chiều cao thực tế, hạn chế cắt dấu tiếng Việt
        // và cắt phần trên/dưới của chữ khi Windows dùng DPI 125% hoặc 150%.
        TableLayoutPanel content = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = _panel,
            Padding = new Padding(13, 3, 8, 3),
            Margin = new Padding(0)
        };

        content.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 36));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 30));

        Label lblTitle = new()
        {
            Text = title,
            Dock = DockStyle.Fill,
            AutoSize = false,
            AutoEllipsis = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = _text,
            Margin = new Padding(0)
        };

        value = new Label
        {
            Text = "0",
            Dock = DockStyle.Fill,
            AutoSize = false,
            AutoEllipsis = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = Color.White,
            Margin = new Padding(0)
        };

        Label lblDescription = new()
        {
            Text = description,
            Dock = DockStyle.Fill,
            AutoSize = false,
            AutoEllipsis = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            ForeColor = _muted,
            Margin = new Padding(0)
        };

        content.Controls.Add(lblTitle, 0, 0);
        content.Controls.Add(value, 0, 1);
        content.Controls.Add(lblDescription, 0, 2);

        card.Controls.Add(content);
        card.Controls.Add(line);

        return card;
    }

    private void BuildFilters()
    {
        _filters = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 1,
            BackColor = _panel,
            Padding = new Padding(8, 6, 8, 6),
            Margin = new Padding(0)
        };

        _txtSearch = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10.5f),
            BackColor = _input,
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Nhập tên sản phẩm...",
            Margin = new Padding(3, 4, 8, 4)
        };

        _cboCategory = CreateComboBox();
        _cboCategory.Items.AddRange(new object[]
        {
            "Tất cả", "Đồ ăn", "Đồ uống", "Combo"
        });
        _cboCategory.SelectedIndex = 0;

        _cboStatus = CreateComboBox();
        _cboStatus.Items.AddRange(new object[]
        {
            "Tất cả", "Đang kinh doanh", "Ngừng kinh doanh"
        });
        _cboStatus.SelectedIndex = 0;

        _txtSearch.TextChanged += (_, _) => RefreshProductTable();
        _cboCategory.SelectedIndexChanged += (_, _) => RefreshProductTable();
        _cboStatus.SelectedIndexChanged += (_, _) => RefreshProductTable();

        _layout.Controls.Add(_filters, 0, 2);

        RebuildFilterLayout(_root.ClientSize.Width);
    }

    private ComboBox CreateComboBox()
    {
        return new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10.5f),
            BackColor = _input,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(3, 4, 5, 4)
        };
    }

    private Label CreateFilterLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            AutoSize = false,
            AutoEllipsis = false,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = _text,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Margin = new Padding(5, 2, 5, 2),
            MinimumSize = new Size(0, 30)
        };
    }

    private void RebuildFilterLayout(int width)
    {
        if (_filters == null || _filters.IsDisposed)
            return;

        // Dùng chiều rộng thực tế của vùng lọc, không dùng kích thước màn hình.
        width = _filters.ClientSize.Width > 0
            ? _filters.ClientSize.Width
            : width;

        _filters.SuspendLayout();

        try
        {
            _filters.Controls.Clear();
            _filters.ColumnStyles.Clear();
            _filters.RowStyles.Clear();
            _filters.Padding = new Padding(10, 7, 10, 7);

            // MỨC 1: rộng - ba trường trên cùng một hàng.
            if (width >= 1250)
            {
                _filters.ColumnCount = 6;
                _filters.RowCount = 1;

                _filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
                _filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                _filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
                _filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
                _filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
                _filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));

                _filters.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

                _filters.Controls.Add(CreateFilterLabel("Tìm kiếm:"), 0, 0);
                _filters.Controls.Add(_txtSearch, 1, 0);
                _filters.Controls.Add(CreateFilterLabel("Danh mục:"), 2, 0);
                _filters.Controls.Add(_cboCategory, 3, 0);
                _filters.Controls.Add(CreateFilterLabel("Trạng thái:"), 4, 0);
                _filters.Controls.Add(_cboStatus, 5, 0);
            }
            // MỨC 2: trung bình - tìm kiếm hàng riêng, hai bộ lọc ở hàng dưới.
            else if (width >= 760)
            {
                _filters.ColumnCount = 4;
                _filters.RowCount = 2;

                _filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
                _filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                _filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
                _filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

                _filters.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
                _filters.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

                _filters.Controls.Add(CreateFilterLabel("Tìm kiếm:"), 0, 0);
                _filters.Controls.Add(_txtSearch, 1, 0);
                _filters.SetColumnSpan(_txtSearch, 3);

                _filters.Controls.Add(CreateFilterLabel("Danh mục:"), 0, 1);
                _filters.Controls.Add(_cboCategory, 1, 1);
                _filters.Controls.Add(CreateFilterLabel("Trạng thái:"), 2, 1);
                _filters.Controls.Add(_cboStatus, 3, 1);
            }
            // MỨC 3: hẹp - mỗi trường một hàng, nhãn luôn có đủ chỗ.
            else
            {
                _filters.ColumnCount = 2;
                _filters.RowCount = 3;

                _filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
                _filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                for (int row = 0; row < 3; row++)
                    _filters.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333f));

                _filters.Controls.Add(CreateFilterLabel("Tìm kiếm:"), 0, 0);
                _filters.Controls.Add(_txtSearch, 1, 0);
                _filters.Controls.Add(CreateFilterLabel("Danh mục:"), 0, 1);
                _filters.Controls.Add(_cboCategory, 1, 1);
                _filters.Controls.Add(CreateFilterLabel("Trạng thái:"), 0, 2);
                _filters.Controls.Add(_cboStatus, 1, 2);
            }

            _txtSearch.Dock = DockStyle.Fill;
            _cboCategory.Dock = DockStyle.Fill;
            _cboStatus.Dock = DockStyle.Fill;

            _txtSearch.Font = new Font("Segoe UI", 10.5f);
            _cboCategory.Font = new Font("Segoe UI", 10.5f);
            _cboStatus.Font = new Font("Segoe UI", 10.5f);
        }
        finally
        {
            _filters.ResumeLayout(true);
        }
    }

    private void BuildActions()
    {
        _actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = true,
            BackColor = _background,
            Padding = new Padding(0, 7, 0, 0),
            Margin = new Padding(0)
        };

        _btnAdd = CreateButton(
            "+ Thêm sản phẩm", Color.FromArgb(37, 99, 235), 155);

        _btnEdit = CreateButton(
            "Sửa sản phẩm", Color.FromArgb(217, 119, 6), 130);

        _btnDelete = CreateButton(
            "Xóa sản phẩm", Color.FromArgb(220, 38, 38), 125);

        _btnImport = CreateButton(
            "Nhập kho", Color.FromArgb(16, 185, 129), 115);

        _btnExport = CreateButton(
            "Xuất kho", Color.FromArgb(234, 88, 12), 115);

        _btnAdd.Click += BtnAdd_Click;
        _btnEdit.Click += BtnEdit_Click;
        _btnDelete.Click += BtnDelete_Click;
        _btnImport.Click += BtnImport_Click;
        _btnExport.Click += BtnExport_Click;

        _actions.Controls.AddRange(new Control[]
        {
            _btnAdd, _btnEdit, _btnDelete, _btnImport, _btnExport
        });

        _layout.Controls.Add(_actions, 0, 3);
    }

    private Button CreateButton(string text, Color color, int width)
    {
        Button button = new()
        {
            Text = text,
            Size = new Size(width, 36),
            BackColor = color,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 7)
        };

        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor =
            ControlPaint.Light(color, 0.15f);

        return button;
    }

    private void BuildGrid()
    {
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = _panel,
            BorderStyle = BorderStyle.None,
            GridColor = Color.FromArgb(51, 65, 85),

            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,

            ColumnHeadersHeight = 42,
            RowTemplate = { Height = 42 },

            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,

            ReadOnly = true,
            MultiSelect = false,
            RowHeadersVisible = false,

            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            EnableHeadersVisualStyles = false,
            ScrollBars = ScrollBars.Vertical,
            
        };

        _grid.ColumnHeadersDefaultCellStyle.BackColor = _panel;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = _text;
        _grid.ColumnHeadersDefaultCellStyle.Font =
            new Font("Segoe UI", 9.5f, FontStyle.Bold);
        _grid.ColumnHeadersDefaultCellStyle.Padding =
            new Padding(5, 0, 4, 0);

        _grid.DefaultCellStyle.BackColor = _panel;
        _grid.DefaultCellStyle.ForeColor = _text;
        _grid.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f);
        _grid.DefaultCellStyle.SelectionBackColor =
            Color.FromArgb(51, 65, 85);
        _grid.DefaultCellStyle.SelectionForeColor = Color.White;
        _grid.DefaultCellStyle.Padding = new Padding(5, 0, 4, 0);

        _grid.AlternatingRowsDefaultCellStyle.BackColor =
            Color.FromArgb(37, 49, 68);

        _grid.Columns.Add("Id", "MÃ SP");
        _grid.Columns.Add("Name", "TÊN SẢN PHẨM");
        _grid.Columns.Add("Category", "DANH MỤC");
        _grid.Columns.Add("Price", "GIÁ BÁN");
        _grid.Columns.Add("Stock", "TỒN KHO");
        _grid.Columns.Add("Image", "HÌNH ẢNH");
        _grid.Columns.Add("Status", "TRẠNG THÁI");

        int[] minWidths = { 55, 145, 85, 95, 70, 85, 110 };

        for (int i = 0; i < _grid.Columns.Count; i++)
        {
            _grid.Columns[i].MinimumWidth = minWidths[i];
            _grid.Columns[i].SortMode =
                DataGridViewColumnSortMode.NotSortable;
        }

        _grid.Columns["Id"]!.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleCenter;

        _grid.Columns["Category"]!.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleCenter;

        _grid.Columns["Price"]!.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleRight;

        _grid.Columns["Stock"]!.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleCenter;

        _grid.Columns["Image"]!.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleCenter;

        _grid.Columns["Status"]!.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleCenter;

        _grid.CellFormatting += Grid_CellFormatting;

        _layout.Controls.Add(_grid, 0, 4);
    }

    private void BuildFooter()
    {
        _lblResult = new Label
        {
            Text = "Tổng số sản phẩm: 0",
            Dock = DockStyle.Fill,
            ForeColor = _text,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _layout.Controls.Add(_lblResult, 0, 5);
    }

    // =====================================================
    // RESPONSIVE LAYOUT
    // =====================================================

    private void ApplyResponsiveLayout()
    {
        if (_updatingLayout || _root == null || _root.IsDisposed)
            return;

        _updatingLayout = true;

        try
        {
            int width = _root.ClientSize.Width;

            if (width <= 0)
                return;

            bool mobile = width < 650;
            bool compact = width < 1050;

            _layout.SuspendLayout();
            _stats.SuspendLayout();
            _actions.SuspendLayout();

            // Các khu vực phía trên được thu gọn để dành diện tích cho bảng.
            _layout.RowStyles[0].Height = 76;
            _layout.RowStyles[1].Height = mobile ? 246 : 108;
            _layout.RowStyles[2].Height = width >= 1250 ? 58 :
                width >= 760 ? 96 : 138;
            _layout.RowStyles[3].Height = mobile ? 80 : 46;
            _layout.RowStyles[4].SizeType = SizeType.Percent;
            _layout.RowStyles[4].Height = 100;
            _layout.RowStyles[5].Height = 26;

            // Thẻ thống kê: 3 cột trên màn hình thông thường,
            // xếp dọc khi vùng nội dung quá hẹp.
            _stats.Controls.Clear();
            _stats.ColumnStyles.Clear();
            _stats.RowStyles.Clear();

            if (mobile)
            {
                _stats.ColumnCount = 1;
                _stats.RowCount = 3;

                _stats.ColumnStyles.Add(
                    new ColumnStyle(SizeType.Percent, 100));

                for (int i = 0; i < 3; i++)
                {
                    _stats.RowStyles.Add(
                        new RowStyle(SizeType.Percent, 33.33f));
                }

                _cardTotal.Margin = new Padding(0, 2, 0, 2);
                _cardActive.Margin = new Padding(0, 2, 0, 2);
                _cardLowStock.Margin = new Padding(0, 2, 0, 2);

                _stats.Controls.Add(_cardTotal, 0, 0);
                _stats.Controls.Add(_cardActive, 0, 1);
                _stats.Controls.Add(_cardLowStock, 0, 2);
            }
            else
            {
                _stats.ColumnCount = 3;
                _stats.RowCount = 1;

                for (int i = 0; i < 3; i++)
                {
                    _stats.ColumnStyles.Add(
                        new ColumnStyle(SizeType.Percent, 33.33f));
                }

                _stats.RowStyles.Add(
                    new RowStyle(SizeType.Percent, 100));

                _cardTotal.Margin = new Padding(0, 2, 5, 2);
                _cardActive.Margin = new Padding(3, 2, 3, 2);
                _cardLowStock.Margin = new Padding(5, 2, 0, 2);

                _stats.Controls.Add(_cardTotal, 0, 0);
                _stats.Controls.Add(_cardActive, 1, 0);
                _stats.Controls.Add(_cardLowStock, 2, 0);
            }

            RebuildFilterLayout(width);

            // Các nút luôn nằm gọn trên một hàng khi đủ chiều rộng.
            _actions.WrapContents = false;
            _actions.AutoScroll = false;
            _actions.Padding = new Padding(0, 2, 0, 0);

            float buttonFont = mobile ? 9.5f : compact ? 10f : 10.5f;

            foreach (Control control in _actions.Controls)
            {
                control.Font = new Font(
                    "Segoe UI", buttonFont, FontStyle.Bold);
                control.Height = 36;
            }

            _grid.ColumnHeadersHeight = mobile ? 40 : 44;
            _grid.RowTemplate.Height = mobile ? 40 : 44;

            // Luôn cho các cột lấp đầy toàn bộ chiều ngang bảng.
            // Tránh tình trạng bảng chỉ chiếm một phần màn hình như trước.
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            float[] fillWeights = { 7, 22, 12, 13, 9, 15, 18 };
            for (int i = 0; i < _grid.Columns.Count; i++)
            {
                _grid.Columns[i].FillWeight = fillWeights[i];
                _grid.Columns[i].MinimumWidth = mobile ? 48 : 58;
            }

            float gridFont = mobile ? 9f : compact ? 10f : 10.5f;

            _grid.DefaultCellStyle.Font =
                new Font("Segoe UI", gridFont);

            _grid.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", gridFont, FontStyle.Bold);

            _stats.ResumeLayout(true);
            _actions.ResumeLayout(true);
            _layout.ResumeLayout(true);
        }
        finally
        {
            _updatingLayout = false;
        }
    }

    // =====================================================
    // LOAD VÀ HIỂN THỊ DỮ LIỆU
    // =====================================================

    private void LoadProductsFromDatabase()
    {
        try
        {
            _products.Clear();
            _products.AddRange(_foodService.GetAll());

            RefreshProductTable();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Không thể tải danh sách sản phẩm từ database.\n\n" +
                ex.Message,
                "Lỗi tải dữ liệu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

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

    private void RefreshProductTable()
    {
        if (_grid == null || _txtSearch == null)
            return;

        _grid.Rows.Clear();

        string keyword = _txtSearch.Text.Trim()
            .ToLowerInvariant();

        string category =
            _cboCategory.SelectedItem?.ToString() ?? "Tất cả";

        string status =
            _cboStatus.SelectedItem?.ToString() ?? "Tất cả";

        var result = _products.Where(product =>
        {
            bool matchName = (product.Name ?? "")
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
            string imageName = string.IsNullOrWhiteSpace(product.ImageUrl)
                ? "Chưa có ảnh"
                : Path.GetFileName(product.ImageUrl);

            _grid.Rows.Add(
                product.Id,
                product.Name,
                GetCategoryText(product.Category),
                product.Price.ToString("N0") + " đ",
                product.StockQuantity,
                imageName,
                product.IsActive
                    ? "Đang kinh doanh"
                    : "Ngừng kinh doanh");
        }

        UpdateStatistics();

        _lblResult.Text =
            $"Đang hiển thị {result.Count} / {_products.Count} sản phẩm";
    }

    private void UpdateStatistics()
    {
        _lblTotal.Text = _products.Count.ToString();

        _lblActive.Text = _products.Count(
            p => p.IsActive).ToString();

        _lblLowStock.Text = _products.Count(
            p => p.StockQuantity <= 10 && p.IsActive).ToString();
    }

    private void Grid_CellFormatting(
        object? sender,
        DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        string columnName = _grid.Columns[e.ColumnIndex].Name;

        if (columnName == "Stock" &&
            int.TryParse(e.Value?.ToString(), out int stock) &&
            stock <= 10)
        {
            e.CellStyle.ForeColor = Color.FromArgb(248, 113, 113);
            e.CellStyle.Font = new Font(
                "Segoe UI", 9.5f, FontStyle.Bold);
        }

        if (columnName == "Status")
        {
            e.CellStyle.ForeColor =
                e.Value?.ToString() == "Đang kinh doanh"
                    ? Color.FromArgb(52, 211, 153)
                    : Color.FromArgb(248, 113, 113);
        }
    }

    // =====================================================
    // THÊM SẢN PHẨM
    // =====================================================

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        using ProductEditDialog dialog = new();

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            _foodService.Add(dialog.ProductData);
            LoadProductsFromDatabase();

            MessageBox.Show(
                "Thêm sản phẩm thành công!",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Không thể thêm sản phẩm",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    // =====================================================
    // SỬA SẢN PHẨM
    // =====================================================

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        if (_grid.SelectedRows.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn sản phẩm cần sửa!");
            return;
        }

        int id = Convert.ToInt32(
            _grid.SelectedRows[0].Cells["Id"].Value);

        try
        {
            Product? product = _foodService.GetById(id);

            if (product == null)
            {
                MessageBox.Show("Không tìm thấy sản phẩm!");
                return;
            }

            using ProductEditDialog dialog = new(product);

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            Product updated = dialog.ProductData;
            updated.Id = id;

            _foodService.Update(updated);
            LoadProductsFromDatabase();

            MessageBox.Show(
                "Cập nhật sản phẩm thành công!",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Không thể cập nhật sản phẩm",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    // =====================================================
    // XÓA SẢN PHẨM
    // =====================================================

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (_grid.SelectedRows.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn sản phẩm cần xóa!");
            return;
        }

        int id = Convert.ToInt32(
            _grid.SelectedRows[0].Cells["Id"].Value);

        Product? product = _products.FirstOrDefault(p => p.Id == id);

        if (product == null)
            return;

        DialogResult confirm = MessageBox.Show(
            $"Bạn có chắc muốn xóa sản phẩm \"{product.Name}\"?",
            "Xác nhận xóa",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
            return;

        try
        {
            _foodService.Delete(id);
            LoadProductsFromDatabase();

            MessageBox.Show(
                "Xóa sản phẩm thành công!",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Không thể xóa sản phẩm",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    // =====================================================
    // NHẬP KHO
    // =====================================================

    private void BtnImport_Click(object? sender, EventArgs e)
    {
        if (_grid.SelectedRows.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn sản phẩm cần nhập kho!");
            return;
        }

        int id = Convert.ToInt32(
            _grid.SelectedRows[0].Cells["Id"].Value);

        Product? product = _products.FirstOrDefault(p => p.Id == id);

        if (product == null)
            return;

        using StockQuantityDialog dialog =
            new("NHẬP KHO");

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            _foodService.ImportStock(id, dialog.Quantity);
            LoadProductsFromDatabase();

            Product? updated = _products.FirstOrDefault(p => p.Id == id);

            MessageBox.Show(
                $"Nhập kho thành công!\n" +
                $"Sản phẩm: {product.Name}\n" +
                $"Số lượng hiện tại: {updated?.StockQuantity}",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Không thể nhập kho",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    // =====================================================
    // XUẤT KHO
    // =====================================================

    private void BtnExport_Click(object? sender, EventArgs e)
    {
        if (_grid.SelectedRows.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn sản phẩm cần xuất kho!");
            return;
        }

        int id = Convert.ToInt32(
            _grid.SelectedRows[0].Cells["Id"].Value);

        Product? product = _products.FirstOrDefault(p => p.Id == id);

        if (product == null)
            return;

        using StockQuantityDialog dialog =
            new("XUẤT KHO");

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            _foodService.ExportStock(id, dialog.Quantity);
            LoadProductsFromDatabase();

            Product? updated = _products.FirstOrDefault(p => p.Id == id);

            MessageBox.Show(
                $"Xuất kho thành công!\n" +
                $"Sản phẩm: {product.Name}\n" +
                $"Số lượng còn lại: {updated?.StockQuantity}",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Không thể xuất kho",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
}

// =========================================================
// DIALOG THÊM / SỬA SẢN PHẨM
// =========================================================

internal sealed class ProductEditDialog : Form
{
    public Product ProductData { get; private set; } = new();

    private TextBox _txtName = null!;
    private TextBox _txtImage = null!;
    private ComboBox _cboCategory = null!;
    private NumericUpDown _numPrice = null!;
    private NumericUpDown _numStock = null!;
    private CheckBox _chkActive = null!;

    private readonly Color _background = Color.FromArgb(15, 23, 42);
    private readonly Color _panel = Color.FromArgb(30, 41, 59);
    private readonly Color _input = Color.FromArgb(15, 23, 42);

    public ProductEditDialog(Product? product = null)
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        BuildInterface(product);
    }

    private void BuildInterface(Product? product)
    {
        Text = product == null ? "Thêm sản phẩm" : "Chỉnh sửa sản phẩm";

        Size = new Size(740, 580);
        MinimumSize = new Size(600, 520);
        StartPosition = FormStartPosition.CenterParent;

        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;

        BackColor = _background;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);

        TableLayoutPanel main = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(18),
            BackColor = _background
        };

        main.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

        Label title = new()
        {
            Text = product == null
                ? "THÊM SẢN PHẨM MỚI"
                : "CHỈNH SỬA SẢN PHẨM",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 17, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft
        };

        TableLayoutPanel fields = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            BackColor = _panel,
            Padding = new Padding(12)
        };

        fields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 50));
        fields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 50));

        for (int i = 0; i < 3; i++)
            fields.RowStyles.Add(
                new RowStyle(SizeType.Percent, 33.33f));

        _txtName = new TextBox
        {
            Font = new Font("Segoe UI", 10),
            BackColor = _input,
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Nhập tên sản phẩm..."
        };

        fields.Controls.Add(
            CreateField("Tên sản phẩm *", _txtName), 0, 0);

        _cboCategory = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10),
            BackColor = _input,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        _cboCategory.Items.AddRange(new object[]
        {
            "Đồ ăn", "Đồ uống", "Combo"
        });
        _cboCategory.SelectedIndex = 0;

        fields.Controls.Add(
            CreateField("Danh mục *", _cboCategory), 1, 0);

        _numPrice = new NumericUpDown
        {
            Minimum = 1000,
            Maximum = 10000000,
            Increment = 1000,
            Value = 30000,
            ThousandsSeparator = true,
            Font = new Font("Segoe UI", 10),
            BackColor = _input,
            ForeColor = Color.White
        };

        fields.Controls.Add(
            CreateField("Giá bán (VNĐ) *", _numPrice), 0, 1);

        _numStock = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 100000,
            Value = 10,
            ThousandsSeparator = true,
            Font = new Font("Segoe UI", 10),
            BackColor = _input,
            ForeColor = Color.White
        };

        fields.Controls.Add(
            CreateField("Số lượng tồn kho", _numStock), 1, 1);

        _txtImage = new TextBox
        {
            ReadOnly = true,
            Font = new Font("Segoe UI", 9),
            BackColor = _input,
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        Button btnBrowse = new()
        {
            Text = "Chọn ảnh",
            Width = 100,
            Dock = DockStyle.Right,
            BackColor = Color.FromArgb(71, 85, 105),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };

        btnBrowse.FlatAppearance.BorderSize = 0;

        Panel imagePanel = new()
        {
            Dock = DockStyle.Fill,
            BackColor = _panel,
            Padding = new Padding(2)
        };

        _txtImage.Dock = DockStyle.Fill;
        imagePanel.Controls.Add(_txtImage);
        imagePanel.Controls.Add(btnBrowse);

        btnBrowse.Click += BtnBrowse_Click;

        fields.Controls.Add(
            CreateField("Hình ảnh sản phẩm", imagePanel), 0, 2);

        _chkActive = new CheckBox
        {
            Text = "Đang kinh doanh",
            Checked = true,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(52, 211, 153),
            BackColor = _panel,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        fields.Controls.Add(
            CreateField("Trạng thái", _chkActive), 1, 2);

        FlowLayoutPanel footer = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 10, 0, 0),
            BackColor = _background
        };

        Button btnSave = CreateDialogButton(
            "Lưu sản phẩm", Color.FromArgb(37, 99, 235), 140);

        Button btnCancel = CreateDialogButton(
            "Hủy", Color.FromArgb(71, 85, 105), 100);

        footer.Controls.Add(btnSave);
        footer.Controls.Add(btnCancel);

        btnSave.Click += BtnSave_Click;

        btnCancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        main.Controls.Add(title, 0, 0);
        main.Controls.Add(fields, 0, 1);
        main.Controls.Add(footer, 0, 2);

        Controls.Add(main);

        AcceptButton = btnSave;
        CancelButton = btnCancel;

        if (product != null)
            LoadProduct(product);
    }

    private Panel CreateField(string title, Control input)
    {
        Panel field = new()
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(7),
            Padding = new Padding(5),
            BackColor = _panel
        };

        Label label = new()
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 27,
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

    private Button CreateDialogButton(
        string text, Color color, int width)
    {
        Button button = new()
        {
            Text = text,
            Size = new Size(width, 38),
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
            _numPrice.Maximum);

        _numStock.Value = Math.Clamp(
            product.StockQuantity,
            (int)_numStock.Minimum,
            (int)_numStock.Maximum);

        _txtImage.Text = product.ImageUrl;
        _chkActive.Checked = product.IsActive;
    }

    private void BtnBrowse_Click(object? sender, EventArgs e)
    {
        using OpenFileDialog dialog = new()
        {
            Title = "Chọn hình ảnh sản phẩm",
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            _txtImage.Text = dialog.FileName;
    }

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

// =========================================================
// DIALOG NHẬP KHO / XUẤT KHO
// =========================================================

internal sealed class StockQuantityDialog : Form
{
    private NumericUpDown _numQuantity = null!;

    public int Quantity => (int)_numQuantity.Value;

    public StockQuantityDialog(string title)
    {
        AutoScaleMode = AutoScaleMode.Dpi;

        Text = title;
        Size = new Size(500, 350);
        MinimumSize = new Size(380, 300);

        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;

        BackColor = Color.FromArgb(15, 23, 42);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);

        BuildInterface(title);
    }

    private void BuildInterface(string title)
    {
        bool isImport = title.Contains(
            "NHẬP", StringComparison.OrdinalIgnoreCase);

        Color background = Color.FromArgb(15, 23, 42);
        Color panel = Color.FromArgb(30, 41, 59);

        Color accent = isImport
            ? Color.FromArgb(16, 185, 129)
            : Color.FromArgb(234, 88, 12);

        TableLayoutPanel main = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(20),
            BackColor = background
        };

        main.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

        Label heading = new()
        {
            Text = isImport ? "NHẬP KHO SẢN PHẨM" : "XUẤT KHO SẢN PHẨM",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft
        };

        TableLayoutPanel quantityPanel = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = panel,
            Padding = new Padding(18)
        };

        quantityPanel.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        quantityPanel.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 35));
        quantityPanel.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 55));
        quantityPanel.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 30));

        Label lblQuantity = new()
        {
            Text = "SỐ LƯỢNG SẢN PHẨM",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(203, 213, 225),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _numQuantity = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Minimum = 1,
            Maximum = 100000,
            Value = 1,
            ThousandsSeparator = true,
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            BackColor = background,
            ForeColor = Color.White,
            Margin = new Padding(0, 5, 0, 5)
        };

        Label unit = new()
        {
            Text = "Đơn vị tính: sản phẩm",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Segoe UI", 9),
            TextAlign = ContentAlignment.MiddleLeft
        };

        quantityPanel.Controls.Add(lblQuantity, 0, 0);
        quantityPanel.Controls.Add(_numQuantity, 0, 1);
        quantityPanel.Controls.Add(unit, 0, 2);

        FlowLayoutPanel footer = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 10, 0, 0),
            BackColor = background
        };

        Button btnConfirm = new()
        {
            Text = isImport ? "Xác nhận nhập kho" : "Xác nhận xuất kho",
            Size = new Size(165, 38),
            BackColor = accent,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(7, 0, 0, 0)
        };

        Button btnCancel = new()
        {
            Text = "Hủy",
            Size = new Size(90, 38),
            BackColor = Color.FromArgb(71, 85, 105),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(7, 0, 0, 0)
        };

        btnConfirm.FlatAppearance.BorderSize = 0;
        btnCancel.FlatAppearance.BorderSize = 0;

        btnConfirm.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        btnCancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        footer.Controls.Add(btnConfirm);
        footer.Controls.Add(btnCancel);

        main.Controls.Add(heading, 0, 0);
        main.Controls.Add(quantityPanel, 0, 1);
        main.Controls.Add(footer, 0, 2);

        Controls.Add(main);

        AcceptButton = btnConfirm;
        CancelButton = btnCancel;
    }
}