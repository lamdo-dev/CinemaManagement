
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Repositories;
using CinemaManagement.Models;
using CinemaManagement.Models.Enums;

namespace CinemaManagement.UserControls.Admin;

public partial class RoomManagementUC : UserControl
{
    // REPOSITORY VÀ DỮ LIỆU
    private readonly RoomRepository _roomRepository = new();

    private List<Room> rooms = new();
    private List<Seat> seats = new();

    private long? selectedRoomId;
    private int? selectedSeatId;
    private Room? currentSeatMapRoom;
    private bool refreshingGrid = false;

    // CÁC CONTROL
    private DataGridView dgvRooms = null!;
    private TextBox txtSearch = null!;
    private ComboBox cboCapacity = null!;
    private ComboBox cboSeatType = null!;

    private FlowLayoutPanel seatMapHost = null!;

    private Label lblTotalRooms = null!;
    private Label lblTotalSeats = null!;
    private Label lblSeatInfo = null!;

    private Button btnAdd = null!;
    private Button btnEdit = null!;
    private Button btnDelete = null!;
    private Button btnApplySeatType = null!;

    private SplitContainer splitContainer = null!;

    // MÀU GIAO DIỆN
    private readonly Color backgroundColor =
        Color.FromArgb(15, 23, 42);

    private readonly Color panelColor =
        Color.FromArgb(30, 41, 59);

    private readonly Color accentColor =
        Color.FromArgb(37, 99, 235);

    private readonly Color textColor =
        Color.FromArgb(226, 232, 240);

    // CONSTRUCTOR
    public RoomManagementUC()
    {
        InitializeComponent();

        Dock = DockStyle.Fill;
        BackColor = backgroundColor;

        BuildInterface();

        SizeChanged += RoomManagementUC_SizeChanged;

        // Chỉ tính SplitterDistance sau khi control có kích thước thực tế.
        HandleCreated += (s, e) =>
        {
            BeginInvoke(new Action(() =>
            {
                if (!IsDisposed)
                    UpdateResponsiveLayout();
            }));
        };

        LoadRoomData();
    }

    // 1. TẢI DỮ LIỆU TỪ SQLITE
    private void LoadRoomData(long? preferredRoomId = null)
    {
        try
        {
            rooms = _roomRepository.GetAll();

            RefreshRoomTable(preferredRoomId);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Không thể tải dữ liệu phòng chiếu:\n" + ex.Message,
                "Lỗi tải dữ liệu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    // 2. XÂY DỰNG GIAO DIỆN
    private void BuildInterface()
    {
        TableLayoutPanel mainLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = backgroundColor,
            Padding = new Padding(0)
        };

        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 85));

        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 90));

        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 75));

        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 55));

        mainLayout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        // HEADER
        Panel headerPanel = new()
        {
            Dock = DockStyle.Fill,
            BackColor = backgroundColor
        };

        Label lblTitle = new()
        {
            Text = "QUẢN LÝ PHÒNG CHIẾU",
            Font = new Font("Segoe UI", 21, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(5, 5)
        };

        Label lblDescription = new()
        {
            Text = "Quản lý danh sách phòng và sơ đồ ghế ngồi",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(7, 48)
        };

        headerPanel.Controls.Add(lblTitle);
        headerPanel.Controls.Add(lblDescription);

        // THỐNG KÊ
        TableLayoutPanel statisticPanel = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = backgroundColor,
            Padding = new Padding(0, 5, 0, 5),
            Margin = new Padding(0)
        };

        statisticPanel.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 50));

        statisticPanel.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 50));

        statisticPanel.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        Panel cardRoom = CreateStatisticCard(
            "TỔNG SỐ PHÒNG",
            Color.FromArgb(37, 99, 235),
            out lblTotalRooms
        );

        Panel cardSeat = CreateStatisticCard(
            "TỔNG SỐ GHẾ",
            Color.FromArgb(16, 185, 129),
            out lblTotalSeats
        );

        cardRoom.Margin = new Padding(0, 0, 8, 0);
        cardSeat.Margin = new Padding(8, 0, 0, 0);

        statisticPanel.Controls.Add(cardRoom, 0, 0);
        statisticPanel.Controls.Add(cardSeat, 1, 0);

        // TÌM KIẾM VÀ LỌC
        FlowLayoutPanel filterPanel = new()
        {
            Dock = DockStyle.Fill,
            BackColor = panelColor,
            Padding = new Padding(12, 12, 8, 8),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = true
        };

        Label lblSearch = new()
        {
            Text = "Tìm phòng:",
            AutoSize = false,
            Width = 90,
            Height = 32,
            ForeColor = textColor,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Margin = new Padding(3, 3, 5, 3)
        };

        txtSearch = new TextBox
        {
            Width = 260,
            Height = 32,
            Font = new Font("Segoe UI", 10),
            Margin = new Padding(3, 3, 20, 3)
        };

        Label lblCapacity = new()
        {
            Text = "Sức chứa:",
            AutoSize = false,
            Width = 85,
            Height = 32,
            ForeColor = textColor,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Margin = new Padding(3, 3, 5, 3)
        };

        cboCapacity = new ComboBox
        {
            Width = 180,
            Height = 32,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9),
            Margin = new Padding(3, 3, 3, 3)
        };

        cboCapacity.Items.AddRange(new object[]
        {
            "Tất cả",
            "Dưới 50 ghế",
            "50 - 80 ghế",
            "Trên 80 ghế"
        });

        cboCapacity.SelectedIndex = 0;

        filterPanel.Controls.Add(lblSearch);
        filterPanel.Controls.Add(txtSearch);
        filterPanel.Controls.Add(lblCapacity);
        filterPanel.Controls.Add(cboCapacity);

        // CÁC NÚT CHỨC NĂNG
        FlowLayoutPanel actionPanel = new()
        {
            Dock = DockStyle.Fill,
            BackColor = backgroundColor,
            Padding = new Padding(0, 8, 0, 5),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = true
        };

        btnAdd = CreateButton(
            "+ Thêm phòng",
            accentColor
        );

        btnEdit = CreateButton(
            "Sửa phòng",
            Color.FromArgb(217, 119, 6)
        );

        btnDelete = CreateButton(
            "Xóa phòng",
            Color.FromArgb(220, 38, 38)
        );

        actionPanel.Controls.Add(btnAdd);
        actionPanel.Controls.Add(btnEdit);
        actionPanel.Controls.Add(btnDelete);

        // DANH SÁCH PHÒNG VÀ SƠ ĐỒ GHẾ
        splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 5,
            BackColor = backgroundColor,

            // Không thiết lập kích thước cố định ở đây.
            Panel1MinSize = 0,
            Panel2MinSize = 0
        };

        BuildRoomList(splitContainer.Panel1);
        BuildSeatMap(splitContainer.Panel2);

        mainLayout.Controls.Add(headerPanel, 0, 0);
        mainLayout.Controls.Add(statisticPanel, 0, 1);
        mainLayout.Controls.Add(filterPanel, 0, 2);
        mainLayout.Controls.Add(actionPanel, 0, 3);
        mainLayout.Controls.Add(splitContainer, 0, 4);

        Controls.Add(mainLayout);

        // CẬP NHẬT KHI SPLITCONTAINER THAY ĐỔI KÍCH THƯỚC
        splitContainer.SizeChanged += (s, e) =>
        {
            ApplyResponsiveSplitterLayout();
        };

        // SỰ KIỆN
        txtSearch.TextChanged += (s, e) =>
        {
            RefreshRoomTable();
        };

        cboCapacity.SelectedIndexChanged += (s, e) =>
        {
            RefreshRoomTable();
        };

        btnAdd.Click += BtnAdd_Click;
        btnEdit.Click += BtnEdit_Click;
        btnDelete.Click += BtnDelete_Click;

        btnApplySeatType.Click += BtnApplySeatType_Click;
    }

    // 3. RESPONSIVE LAYOUT
    private void RoomManagementUC_SizeChanged(
        object? sender,
        EventArgs e)
    {
        UpdateResponsiveLayout();
    }

    private void UpdateResponsiveLayout()
    {
        if (IsDisposed || !IsHandleCreated)
            return;

        int width = ClientSize.Width;

        if (width <= 0)
            return;

        bool compact = width < 1100;

        if (txtSearch != null && !txtSearch.IsDisposed)
            txtSearch.Width = compact ? 190 : 260;

        if (cboCapacity != null && !cboCapacity.IsDisposed)
            cboCapacity.Width = compact ? 145 : 180;

        if (dgvRooms != null && !dgvRooms.IsDisposed)
            dgvRooms.ScrollBars = ScrollBars.Both;

        if (seatMapHost != null && !seatMapHost.IsDisposed)
            seatMapHost.AutoScroll = true;

        ApplyResponsiveSplitterLayout();
    }

    // TỰ ĐỘNG ĐIỀU CHỈNH SPLITTER
    private void ApplyResponsiveSplitterLayout()
    {
        if (splitContainer == null ||
            splitContainer.IsDisposed ||
            !splitContainer.IsHandleCreated)
        {
            return;
        }

        int width = splitContainer.ClientSize.Width;
        int splitterWidth = splitContainer.SplitterWidth;
        int availableWidth = width - splitterWidth;

        if (availableWidth <= 1)
            return;

        bool compact = ClientSize.Width < 1100;

        int panel1Min = compact ? 120 : 180;
        int panel2Min = compact ? 120 : 220;

        // Nếu màn hình nhỏ, giảm kích thước tối thiểu.
        if (panel1Min + panel2Min > availableWidth)
        {
            panel1Min = availableWidth / 2;
            panel2Min = availableWidth - panel1Min;
        }

        // Tạm bỏ giới hạn cũ.
        splitContainer.Panel1MinSize = 0;
        splitContainer.Panel2MinSize = 0;

        int desiredDistance = compact
            ? availableWidth / 2
            : (int)(availableWidth * 0.38);

        int maxDistance = availableWidth - panel2Min;

        if (maxDistance < panel1Min)
            return;

        int distance = Math.Clamp(
            desiredDistance,
            panel1Min,
            maxDistance
        );

        // Đặt khoảng cách hợp lệ trước.
        splitContainer.SplitterDistance = distance;

        // Sau đó mới đặt kích thước tối thiểu.
        splitContainer.Panel1MinSize = panel1Min;
        splitContainer.Panel2MinSize = panel2Min;
    }

    // 4. XÂY DỰNG BẢNG DANH SÁCH PHÒNG
    private void BuildRoomList(Panel parent)
    {
        parent.BackColor = panelColor;
        parent.Padding = new Padding(10);

        Label lblHeader = new()
        {
            Text = "DANH SÁCH PHÒNG CHIẾU",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 42,
            TextAlign = ContentAlignment.MiddleLeft
        };

        dgvRooms = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = panelColor,
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

            ScrollBars = ScrollBars.Both
        };

        dgvRooms.ColumnHeadersDefaultCellStyle.BackColor =
            accentColor;

        dgvRooms.ColumnHeadersDefaultCellStyle.ForeColor =
            Color.White;

        dgvRooms.ColumnHeadersDefaultCellStyle.Font =
            new Font("Segoe UI", 9, FontStyle.Bold);

        dgvRooms.ColumnHeadersHeight = 42;

        dgvRooms.DefaultCellStyle.BackColor = panelColor;
        dgvRooms.DefaultCellStyle.ForeColor = Color.White;

        dgvRooms.DefaultCellStyle.SelectionBackColor =
            Color.FromArgb(51, 65, 85);

        dgvRooms.DefaultCellStyle.SelectionForeColor =
            Color.White;

        dgvRooms.DefaultCellStyle.Font =
            new Font("Segoe UI", 9);

        dgvRooms.AlternatingRowsDefaultCellStyle.BackColor =
            Color.FromArgb(38, 50, 70);

        dgvRooms.RowTemplate.Height = 42;

        dgvRooms.Columns.Add("Id", "Mã");
        dgvRooms.Columns.Add("Name", "Tên phòng");
        dgvRooms.Columns.Add("Rows", "Hàng");
        dgvRooms.Columns.Add("Columns", "Cột");
        dgvRooms.Columns.Add("TotalSeats", "Số ghế");

        dgvRooms.Columns["Id"]!.MinimumWidth = 45;
        dgvRooms.Columns["Name"]!.MinimumWidth = 100;
        dgvRooms.Columns["Rows"]!.MinimumWidth = 50;
        dgvRooms.Columns["Columns"]!.MinimumWidth = 50;
        dgvRooms.Columns["TotalSeats"]!.MinimumWidth = 65;

        dgvRooms.Columns["Id"]!.FillWeight = 45;
        dgvRooms.Columns["Name"]!.FillWeight = 130;
        dgvRooms.Columns["Rows"]!.FillWeight = 55;
        dgvRooms.Columns["Columns"]!.FillWeight = 55;
        dgvRooms.Columns["TotalSeats"]!.FillWeight = 70;

        parent.Controls.Add(dgvRooms);
        parent.Controls.Add(lblHeader);

        dgvRooms.SelectionChanged += DgvRooms_SelectionChanged;
    }

    // 5. XÂY DỰNG SƠ ĐỒ GHẾ
    private void BuildSeatMap(Panel parent)
    {
        parent.BackColor = panelColor;
        parent.Padding = new Padding(10);

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = panelColor
        };

        layout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 40));

        layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 38));

        layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 48));

        layout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 40));

        Label lblTitle = new()
        {
            Text = "SƠ ĐỒ GHẾ NGỒI",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        // CHÚ THÍCH MÀU GHẾ
        FlowLayoutPanel legendPanel = new()
        {
            Dock = DockStyle.Fill,
            BackColor = panelColor,
            WrapContents = true,
            AutoScroll = true
        };

        legendPanel.Controls.Add(
            CreateLegendItem(
                "Ghế thường",
                Color.FromArgb(71, 85, 105)
            )
        );

        legendPanel.Controls.Add(
            CreateLegendItem(
                "Ghế VIP",
                Color.FromArgb(217, 119, 6)
            )
        );

        legendPanel.Controls.Add(
            CreateLegendItem(
                "Ghế đôi",
                Color.FromArgb(124, 58, 237)
            )
        );

        // CHỌN LOẠI GHẾ
        FlowLayoutPanel seatToolPanel = new()
        {
            Dock = DockStyle.Fill,
            BackColor = panelColor,
            WrapContents = true,
            AutoScroll = true,
            Padding = new Padding(0, 5, 0, 0)
        };

        Label lblType = new()
        {
            Text = "Loại ghế:",
            AutoSize = true,
            ForeColor = textColor,
            Margin = new Padding(3, 7, 5, 0)
        };

        cboSeatType = new ComboBox
        {
            Width = 145,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9),
            Margin = new Padding(0, 1, 10, 0)
        };

        cboSeatType.Items.AddRange(new object[]
        {
            "Ghế thường",
            "Ghế VIP",
            "Ghế đôi"
        });

        cboSeatType.SelectedIndex = 0;

        btnApplySeatType = CreateButton(
            "Áp dụng loại ghế",
            Color.FromArgb(16, 185, 129)
        );

        seatToolPanel.Controls.Add(lblType);
        seatToolPanel.Controls.Add(cboSeatType);
        seatToolPanel.Controls.Add(btnApplySeatType);

        // KHU VỰC HIỂN THỊ GHẾ
        seatMapHost = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = backgroundColor,
            WrapContents = false,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(10),
            Margin = new Padding(0)
        };

        // Khi vùng quản trị thay đổi kích thước, dựng lại sơ đồ để ghế
        // luôn đủ rộng và mã A10/B11... không bị cắt.
        seatMapHost.SizeChanged += (s, e) =>
        {
            if (currentSeatMapRoom != null &&
                !seatMapHost.IsDisposed &&
                seatMapHost.ClientSize.Width > 0)
            {
                RenderSeatMap(currentSeatMapRoom);
            }
        };

        lblSeatInfo = new Label
        {
            Text = "Chưa chọn ghế nào.",
            Dock = DockStyle.Fill,
            ForeColor = textColor,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        layout.Controls.Add(lblTitle, 0, 0);
        layout.Controls.Add(legendPanel, 0, 1);
        layout.Controls.Add(seatToolPanel, 0, 2);
        layout.Controls.Add(seatMapHost, 0, 3);
        layout.Controls.Add(lblSeatInfo, 0, 4);

        parent.Controls.Add(layout);
    }

    // 6. TẠO THẺ THỐNG KÊ
    private Panel CreateStatisticCard(
        string title,
        Color cardAccentColor,
        out Label valueLabel)
    {
        Panel card = new()
        {
            Dock = DockStyle.Fill,
            BackColor = panelColor,
            Margin = new Padding(0)
        };

        Panel accentBar = new()
        {
            Location = new Point(0, 0),
            Size = new Size(5, 75),
            BackColor = cardAccentColor,
            Anchor = AnchorStyles.Top |
                     AnchorStyles.Bottom |
                     AnchorStyles.Left
        };

        Label lblTitle = new()
        {
            Text = title,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(20, 9)
        };

        valueLabel = new Label
        {
            Text = "0",
            Font = new Font("Segoe UI", 21, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 30)
        };

        card.Controls.Add(accentBar);
        card.Controls.Add(lblTitle);
        card.Controls.Add(valueLabel);

        return card;
    }

    // 7. TẠO CHÚ THÍCH LOẠI GHẾ
    private Panel CreateLegendItem(string text, Color color)
    {
        Panel item = new()
        {
            Width = 115,
            Height = 30,
            BackColor = panelColor,
            Margin = new Padding(0, 3, 5, 0)
        };

        Panel colorBox = new()
        {
            Size = new Size(15, 15),
            Location = new Point(2, 6),
            BackColor = color
        };

        Label label = new()
        {
            Text = text,
            AutoSize = true,
            ForeColor = textColor,
            Font = new Font("Segoe UI", 8),
            Location = new Point(22, 5)
        };

        item.Controls.Add(colorBox);
        item.Controls.Add(label);

        return item;
    }

    // 8. TẠO BUTTON
    private Button CreateButton(string text, Color color)
    {
        Button button = new()
        {
            Text = text,
            Size = new Size(130, 38),
            MinimumSize = new Size(105, 36),
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 12, 5),
            UseVisualStyleBackColor = false
        };

        button.FlatAppearance.BorderSize = 0;

        return button;
    }

    // 9. CẬP NHẬT DANH SÁCH PHÒNG
    private void RefreshRoomTable(long? preferredRoomId = null)
    {
        if (dgvRooms == null)
            return;

        string keyword = txtSearch.Text.Trim();

        string capacity =
            cboCapacity.SelectedItem?.ToString() ?? "Tất cả";

        List<Room> result = rooms.Where(room =>
        {
            bool matchName = room.Name.Contains(
                keyword,
                StringComparison.OrdinalIgnoreCase
            );

            bool matchCapacity = capacity switch
            {
                "Dưới 50 ghế" => room.TotalSeats < 50,

                "50 - 80 ghế" =>
                    room.TotalSeats >= 50 &&
                    room.TotalSeats <= 80,

                "Trên 80 ghế" => room.TotalSeats > 80,

                _ => true
            };

            return matchName && matchCapacity;

        }).ToList();

        refreshingGrid = true;

        try
        {
            dgvRooms.Rows.Clear();

            foreach (Room room in result)
            {
                dgvRooms.Rows.Add(
                    room.Id,
                    room.Name,
                    room.TotalRows,
                    room.TotalColumns,
                    room.TotalSeats
                );
            }

            dgvRooms.ClearSelection();

            long? targetId = preferredRoomId ?? selectedRoomId;

            int selectedIndex = -1;

            if (targetId.HasValue)
            {
                selectedIndex = result.FindIndex(
                    r => r.Id == targetId.Value
                );
            }

            if (selectedIndex < 0 && result.Count > 0)
                selectedIndex = 0;

            if (selectedIndex >= 0)
            {
                dgvRooms.Rows[selectedIndex].Selected = true;

                dgvRooms.CurrentCell =
                    dgvRooms.Rows[selectedIndex].Cells[0];
            }
        }
        finally
        {
            refreshingGrid = false;
        }

        UpdateStatistics();

        if (dgvRooms.SelectedRows.Count > 0)
        {
            DisplaySelectedRoom();
        }
        else
        {
            selectedRoomId = null;
            selectedSeatId = null;
            seats.Clear();

            RenderSeatMap(null);
        }
    }

    // 10. CẬP NHẬT THỐNG KÊ
    private void UpdateStatistics()
    {
        lblTotalRooms.Text = rooms.Count.ToString();

        lblTotalSeats.Text = rooms.Sum(
            room => room.TotalSeats
        ).ToString();
    }

    // 11. SỰ KIỆN CHỌN PHÒNG
    private void DgvRooms_SelectionChanged(
        object? sender,
        EventArgs e)
    {
        if (refreshingGrid)
            return;

        DisplaySelectedRoom();
    }

    private void DisplaySelectedRoom()
    {
        if (dgvRooms.SelectedRows.Count == 0)
            return;

        long roomId = Convert.ToInt64(
            dgvRooms.SelectedRows[0].Cells["Id"].Value
        );

        Room? room = rooms.FirstOrDefault(
            r => r.Id == roomId
        );

        if (room == null)
            return;

        selectedRoomId = room.Id;
        selectedSeatId = null;

        try
        {
            seats = _roomRepository.GetSeatsByRoomId(roomId);

            RenderSeatMap(room);
        }
        catch (Exception ex)
        {
            seats.Clear();
            RenderSeatMap(null);

            MessageBox.Show(
                "Không thể tải sơ đồ ghế:\n" + ex.Message,
                "Lỗi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    // 12. HIỂN THỊ SƠ ĐỒ GHẾ
    private void RenderSeatMap(Room? room)
    {
        if (seatMapHost == null || seatMapHost.IsDisposed)
            return;

        currentSeatMapRoom = room;
        seatMapHost.SuspendLayout();
        seatMapHost.Controls.Clear();
        lblSeatInfo.Text = "Chưa chọn ghế nào.";

        if (room == null)
        {
            Label empty = new()
            {
                Text = "Không có phòng chiếu để hiển thị.",
                ForeColor = Color.White,
                AutoSize = true,
                Font = new Font("Segoe UI", 10),
                Margin = new Padding(15)
            };
            seatMapHost.Controls.Add(empty);
            seatMapHost.ResumeLayout();
            return;
        }

        int cols = Math.Max(1, room.TotalColumns);
        int rows = Math.Max(1, room.TotalRows);
        int hostWidth = Math.Max(180, seatMapHost.ClientSize.Width -
            seatMapHost.Padding.Horizontal - 24);

        const int rowHeaderWidth = 34;
        const int horizontalGap = 4;
        const int verticalGap = 4;
        const int horizontalPadding = 8;

        int usableWidth = hostWidth - rowHeaderWidth -
            horizontalPadding * 2 - horizontalGap * cols;

        // Mã ghế dạng A10/B12 cần tối thiểu 48px để đọc rõ.
        // Khi màn hình hẹp, giữ kích thước tối thiểu và cho cuộn ngang.
        int cellWidth = Math.Clamp(usableWidth / cols, 48, 64);
        int cellHeight = Math.Clamp(
            (seatMapHost.ClientSize.Height - 100) / Math.Max(rows, 1),
            34, 46);

        int gridWidth = rowHeaderWidth + horizontalPadding * 2 +
            cols * cellWidth + cols * horizontalGap;
        int gridHeight = 42 + rows * (cellHeight + verticalGap) + 12;

        TableLayoutPanel grid = new()
        {
            AutoSize = false,
            Size = new Size(gridWidth, gridHeight),
            ColumnCount = cols + 1,
            RowCount = rows + 1,
            BackColor = backgroundColor,
            Padding = new Padding(4),
            Margin = new Padding(Math.Max(4, (hostWidth - gridWidth) / 2), 8, 8, 8)
        };

        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, rowHeaderWidth));
        for (int col = 0; col < cols; col++)
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, cellWidth + horizontalGap));

        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        for (int row = 0; row < rows; row++)
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, cellHeight + verticalGap));

        Label lblScreen = new()
        {
            Text = "MÀN HÌNH CHIẾU",
            BackColor = Color.FromArgb(41, 128, 185),
            ForeColor = Color.White,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Margin = new Padding(2, 2, 2, 8)
        };
        grid.Controls.Add(lblScreen, 1, 0);
        grid.SetColumnSpan(lblScreen, cols);

        for (int row = 0; row < rows; row++)
        {
            string rowName = ((char)('A' + row)).ToString();

            Label lblRow = new()
            {
                Text = rowName,
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Margin = new Padding(0)
            };
            grid.Controls.Add(lblRow, 0, row + 1);

            for (int col = 0; col < cols; col++)
            {
                Seat? seat = seats.FirstOrDefault(
                    item => item.RowIndex == row &&
                            item.ColumnIndex == col);

                if (seat == null)
                    continue;

                Seat currentSeat = seat;
                // Tạo nhãn từ tọa độ hàng/cột để giao diện không bị ảnh hưởng
                // nếu dữ liệu SeatNumber cũ trong database bị sai định dạng.
                string displaySeatCode = $"{rowName}{col + 1}";

                Button seatButton = new()
                {
                    Text = displaySeatCode,
                    Size = new Size(cellWidth, cellHeight),
                    Dock = DockStyle.Fill,
                    BackColor = GetSeatColor(seat.Type),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", cellWidth >= 56 ? 9f : 8f, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    UseVisualStyleBackColor = false,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Margin = new Padding(2),
                    Padding = new Padding(0),
                    Tag = seat.Id
                };

                seatButton.FlatAppearance.BorderColor =
                    selectedSeatId == seat.Id ? Color.White : panelColor;
                seatButton.FlatAppearance.BorderSize =
                    selectedSeatId == seat.Id ? 3 : 1;

                seatButton.Click += (s, e) =>
                {
                    selectedSeatId = currentSeat.Id;
                    RenderSeatMap(room);
                    lblSeatInfo.Text =
                        $"Ghế: {displaySeatCode}  |  " +
                        $"Loại: {GetSeatTypeText(currentSeat.Type)}  |  " +
                        $"Hệ số giá: {currentSeat.PriceMultiplier}";
                };

                grid.Controls.Add(seatButton, col + 1, row + 1);
            }
        }

        seatMapHost.Controls.Add(grid);
        seatMapHost.ResumeLayout();
    }

    // 13. LẤY MÀU THEO LOẠI GHẾ
    private Color GetSeatColor(SeatType type)
    {
        return type switch
        {
            SeatType.Standard =>
                Color.FromArgb(71, 85, 105),

            SeatType.VIP =>
                Color.FromArgb(217, 119, 6),

            SeatType.Couple =>
                Color.FromArgb(124, 58, 237),

            _ => Color.Gray
        };
    }

    // 14. LẤY TÊN LOẠI GHẾ
    private string GetSeatTypeText(SeatType type)
    {
        return type switch
        {
            SeatType.Standard => "Ghế thường",
            SeatType.VIP => "Ghế VIP",
            SeatType.Couple => "Ghế đôi",
            _ => "Không xác định"
        };
    }

    // 15. LẤY HỆ SỐ GIÁ GHẾ
    private double GetMultiplier(SeatType type)
    {
        return type switch
        {
            SeatType.Standard => 1.0,
            SeatType.VIP => 1.2,
            SeatType.Couple => 2.0,
            _ => 1.0
        };
    }

    // 16. TẠO GHẾ CHO PHÒNG
    private List<Seat> CreateSeatsForRoom(Room room)
    {
        List<Seat> newSeats = new();

        for (int row = 0; row < room.TotalRows; row++)
        {
            for (int col = 0; col < room.TotalColumns; col++)
            {
                SeatType type = SeatType.Standard;

                newSeats.Add(new Seat
                {
                    RoomId = checked((int)room.Id),

                    SeatNumber =
                        $"{(char)('A' + row)}{col + 1}",

                    RowIndex = row,
                    ColumnIndex = col,

                    Type = type,

                    PriceMultiplier = GetMultiplier(type)
                });
            }
        }

        return newSeats;
    }

    // 17. ÁP DỤNG LOẠI GHẾ VÀ LƯU SQLITE
    private void BtnApplySeatType_Click(
        object? sender,
        EventArgs e)
    {
        if (!selectedSeatId.HasValue ||
            !selectedRoomId.HasValue)
        {
            MessageBox.Show(
                "Vui lòng chọn một ghế trước!",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );

            return;
        }

        Seat? selectedSeat = seats.FirstOrDefault(
            s => s.Id == selectedSeatId.Value
        );

        if (selectedSeat == null)
            return;

        SeatType newType = cboSeatType.SelectedIndex switch
        {
            0 => SeatType.Standard,
            1 => SeatType.VIP,
            2 => SeatType.Couple,
            _ => SeatType.Standard
        };

        SeatType oldType = selectedSeat.Type;
        double oldMultiplier = selectedSeat.PriceMultiplier;

        selectedSeat.Type = newType;
        selectedSeat.PriceMultiplier = GetMultiplier(newType);

        try
        {
            if (!_roomRepository.UpdateSeat(selectedSeat))
            {
                throw new InvalidOperationException(
                    "Không tìm thấy ghế cần cập nhật trong database."
                );
            }

            Room? room = rooms.FirstOrDefault(
                r => r.Id == selectedRoomId.Value
            );

            RenderSeatMap(room);

            lblSeatInfo.Text =
                $"Ghế: {selectedSeat.SeatNumber}  |  " +
                $"Loại: {GetSeatTypeText(newType)}  |  " +
                $"Hệ số giá: {selectedSeat.PriceMultiplier}";

            MessageBox.Show(
                "Đã cập nhật loại ghế và lưu vào SQLite!",
                "Thành công",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        catch (Exception ex)
        {
            selectedSeat.Type = oldType;
            selectedSeat.PriceMultiplier = oldMultiplier;

            MessageBox.Show(
                "Không thể cập nhật loại ghế:\n" + ex.Message,
                "Lỗi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );

            LoadRoomData(selectedRoomId);
        }
    }

    // 18. THÊM PHÒNG
    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        using RoomEditorDialog dialog = new RoomEditorDialog();

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            if (_roomRepository.ExistsByName(dialog.RoomName))
            {
                MessageBox.Show(
                    "Tên phòng đã tồn tại!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            Room newRoom = new()
            {
                Name = dialog.RoomName,
                TotalRows = dialog.Rows,
                TotalColumns = dialog.Columns,
                TotalSeats = dialog.Rows * dialog.Columns
            };

            List<Seat> newSeats = CreateSeatsForRoom(newRoom);

            long newRoomId = _roomRepository.Add(
                newRoom,
                newSeats
            );

            MessageBox.Show(
                "Thêm phòng và sơ đồ ghế thành công!",
                "Thành công",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            LoadRoomData(newRoomId);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Không thể thêm phòng:\n" + ex.Message,
                "Lỗi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    // 19. SỬA PHÒNG
    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        if (!selectedRoomId.HasValue)
        {
            MessageBox.Show(
                "Vui lòng chọn phòng cần sửa!"
            );

            return;
        }

        Room? room = rooms.FirstOrDefault(
            r => r.Id == selectedRoomId.Value
        );

        if (room == null)
            return;

        using RoomEditorDialog dialog =
            new RoomEditorDialog(room);

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            if (_roomRepository.ExistsByName(
                dialog.RoomName,
                room.Id))
            {
                MessageBox.Show(
                    "Tên phòng đã được sử dụng!"
                );

                return;
            }

            bool dimensionsChanged =
                room.TotalRows != dialog.Rows ||
                room.TotalColumns != dialog.Columns;

            Room updatedRoom = new()
            {
                Id = room.Id,
                Name = dialog.RoomName,
                TotalRows = dialog.Rows,
                TotalColumns = dialog.Columns,
                TotalSeats = dialog.Rows * dialog.Columns
            };

            List<Seat> updatedSeats = dimensionsChanged
                ? CreateSeatsForRoom(updatedRoom)
                : seats;

            bool success = _roomRepository.Update(
                updatedRoom,
                updatedSeats,
                dimensionsChanged
            );

            if (!success)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy phòng cần cập nhật."
                );
            }

            MessageBox.Show(
                "Cập nhật phòng thành công!",
                "Thành công",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            LoadRoomData(updatedRoom.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Không thể cập nhật phòng:\n" + ex.Message,
                "Lỗi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );

            LoadRoomData(room.Id);
        }
    }

    // 20. XÓA PHÒNG
    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (!selectedRoomId.HasValue)
        {
            MessageBox.Show(
                "Vui lòng chọn phòng cần xóa!"
            );

            return;
        }

        Room? room = rooms.FirstOrDefault(
            r => r.Id == selectedRoomId.Value
        );

        if (room == null)
            return;

        DialogResult confirm = MessageBox.Show(
            $"Bạn có chắc muốn xóa {room.Name}?",
            "Xác nhận xóa",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        );

        if (confirm != DialogResult.Yes)
            return;

        try
        {
            bool success = _roomRepository.Delete(room.Id);

            if (!success)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy phòng cần xóa."
                );
            }

            selectedRoomId = null;
            selectedSeatId = null;

            MessageBox.Show(
                "Xóa phòng thành công!",
                "Thành công",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            LoadRoomData();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Không thể xóa phòng.\n" +
                "Phòng có thể đang được sử dụng trong lịch chiếu.\n\n" +
                ex.Message,
                "Lỗi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );

            LoadRoomData(room.Id);
        }
    }
}

// 21. FORM THÊM / SỬA PHÒNG CHIẾU
internal sealed class RoomEditorDialog : Form
{
    private TextBox txtName = null!;
    private NumericUpDown numRows = null!;
    private NumericUpDown numColumns = null!;

    public string RoomName => txtName.Text.Trim();

    public int Rows => (int)numRows.Value;

    public int Columns => (int)numColumns.Value;

    public RoomEditorDialog(Room? room = null)
    {
        Text = room == null
            ? "Thêm phòng chiếu"
            : "Chỉnh sửa phòng chiếu";

        Size = new Size(500, 370);

        StartPosition = FormStartPosition.CenterParent;

        FormBorderStyle = FormBorderStyle.FixedDialog;

        MaximizeBox = false;
        MinimizeBox = false;

        BackColor = Color.FromArgb(15, 23, 42);
        ForeColor = Color.White;

        Font = new Font("Segoe UI", 10);

        BuildDialogInterface();

        if (room != null)
        {
            txtName.Text = room.Name;

            numRows.Value = Math.Clamp(
                room.TotalRows,
                (int)numRows.Minimum,
                (int)numRows.Maximum
            );

            numColumns.Value = Math.Clamp(
                room.TotalColumns,
                (int)numColumns.Minimum,
                (int)numColumns.Maximum
            );
        }
    }

    private void BuildDialogInterface()
    {
        Label lblTitle = new()
        {
            Text = "THÔNG TIN PHÒNG CHIẾU",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(25, 20)
        };

        Label lblName = CreateLabel(
            "Tên phòng:",
            35,
            90
        );

        txtName = new TextBox
        {
            Location = new Point(155, 85),
            Size = new Size(280, 30),
            Font = new Font("Segoe UI", 10)
        };

        Label lblRows = CreateLabel(
            "Số hàng ghế:",
            35,
            145
        );

        numRows = new NumericUpDown
        {
            Location = new Point(155, 140),
            Size = new Size(130, 30),
            Minimum = 1,
            Maximum = 20,
            Value = 6,
            Font = new Font("Segoe UI", 10)
        };

        Label lblColumns = CreateLabel(
            "Số cột ghế:",
            35,
            200
        );

        numColumns = new NumericUpDown
        {
            Location = new Point(155, 195),
            Size = new Size(130, 30),
            Minimum = 1,
            Maximum = 20,
            Value = 8,
            Font = new Font("Segoe UI", 10)
        };

        Button btnSave = CreateDialogButton(
            "Lưu",
            Color.FromArgb(37, 99, 235)
        );

        btnSave.Location = new Point(250, 270);

        Button btnCancel = CreateDialogButton(
            "Hủy",
            Color.FromArgb(71, 85, 105)
        );

        btnCancel.Location = new Point(345, 270);

        btnSave.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show(
                    "Vui lòng nhập tên phòng!"
                );

                txtName.Focus();
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        };

        btnCancel.Click += (s, e) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        Controls.Add(lblTitle);
        Controls.Add(lblName);
        Controls.Add(txtName);
        Controls.Add(lblRows);
        Controls.Add(numRows);
        Controls.Add(lblColumns);
        Controls.Add(numColumns);
        Controls.Add(btnSave);
        Controls.Add(btnCancel);

        AcceptButton = btnSave;
        CancelButton = btnCancel;
    }

    private Label CreateLabel(string text, int x, int y)
    {
        return new Label
        {
            Text = text,
            Location = new Point(x, y),
            AutoSize = true,
            ForeColor = Color.FromArgb(226, 232, 240),
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
    }

    private Button CreateDialogButton(
        string text,
        Color color)
    {
        Button button = new()
        {
            Text = text,
            Size = new Size(85, 38),
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Cursor = Cursors.Hand
        };

        button.FlatAppearance.BorderSize = 0;

        return button;
    }
}