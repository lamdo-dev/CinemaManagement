
namespace CinemaManagement.UserControls.Admin;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Dialogs;
using CinemaManagement.Models;
using CinemaManagement.Models.Enums;
using CinemaManagement.Repositories;

public partial class MovieManagementUC : UserControl
{
    private readonly MovieRepository _movieRepository = new();
    private List<Movie> _allMovies = new();

    private DataGridView dgvMovies = null!;
    private TextBox txtSearch = null!;
    private ComboBox cboStatus = null!;
    private Label lblTotal = null!;

    private Button btnAdd = null!;
    private Button btnEdit = null!;
    private Button btnDelete = null!;
    private Button btnReload = null!;

    private FlowLayoutPanel filterPanel = null!;
    private FlowLayoutPanel buttonPanel = null!;

    private readonly Color backgroundColor = Color.FromArgb(15, 23, 42);
    private readonly Color panelColor = Color.FromArgb(30, 41, 59);
    private readonly Color accentColor = Color.FromArgb(37, 99, 235);
    private readonly Color textColor = Color.FromArgb(226, 232, 240);

    public MovieManagementUC()
    {
        InitializeComponent();

        Dock = DockStyle.Fill;
        BackColor = backgroundColor;
        Padding = new Padding(15);

        BuildInterface();
        UpdateResponsiveLayout();

        LoadMovieData();

        SizeChanged += MovieManagementUC_SizeChanged;
    }

    // =====================================
    // 1. XÂY DỰNG GIAO DIỆN
    // =====================================

    private void BuildInterface()
    {
        // Tiêu đề
        Panel headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 85,
            BackColor = backgroundColor
        };

        Label lblTitle = new Label
        {
            Text = "QUẢN LÝ PHIM",
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(0, 0)
        };

        Label lblDescription = new Label
        {
            Text = "Quản lý danh sách phim trong hệ thống rạp chiếu",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(2, 45)
        };

        headerPanel.Controls.Add(lblTitle);
        headerPanel.Controls.Add(lblDescription);

        // Khu vực tìm kiếm và lọc
        filterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 95,
            BackColor = panelColor,
            Padding = new Padding(12, 15, 12, 10),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = true
        };

        Label lblSearch = new Label
        {
            Text = "Tìm kiếm phim:",
            ForeColor = textColor,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            AutoSize = false,
            Width = 115,
            Height = 34,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(3, 3, 5, 3)
        };

        txtSearch = new TextBox
        {
            Size = new Size(350, 30),
            Font = new Font("Segoe UI", 10),
            PlaceholderText = "Tên phim, thể loại, đạo diễn...",
            Margin = new Padding(3, 5, 25, 3),
            Anchor = AnchorStyles.Left
        };

        Label lblStatus = new Label
        {
            Text = "Trạng thái:",
            ForeColor = textColor,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            AutoSize = false,
            Width = 85,
            Height = 34,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(3, 3, 5, 3)
        };

        cboStatus = new ComboBox
        {
            Size = new Size(190, 32),
            Font = new Font("Segoe UI", 10),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(3, 5, 3, 3)
        };

        cboStatus.Items.AddRange(new object[]
        {
            "Tất cả",
            "Đang chiếu",
            "Sắp chiếu",
            "Đã kết thúc"
        });

        cboStatus.SelectedIndex = 0;

        filterPanel.Controls.Add(lblSearch);
        filterPanel.Controls.Add(txtSearch);
        filterPanel.Controls.Add(lblStatus);
        filterPanel.Controls.Add(cboStatus);

        // Thanh nút chức năng
        btnAdd = CreateButton("+ Thêm phim", accentColor);
        btnEdit = CreateButton("Sửa phim", Color.FromArgb(217, 119, 6));
        btnDelete = CreateButton("Xóa phim", Color.FromArgb(220, 38, 38));
        btnReload = CreateButton("Tải lại", Color.FromArgb(71, 85, 105));

        buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 100,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = backgroundColor,
            Padding = new Padding(0, 12, 0, 8),
            WrapContents = true,
            AutoScroll = true
        };

        buttonPanel.Controls.Add(btnAdd);
        buttonPanel.Controls.Add(btnEdit);
        buttonPanel.Controls.Add(btnDelete);
        buttonPanel.Controls.Add(btnReload);

        // Bảng danh sách phim
        dgvMovies = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = panelColor,
            BorderStyle = BorderStyle.None,

            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,

            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,

            ReadOnly = true,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,

            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,

            ScrollBars = ScrollBars.Both,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None
        };

        // Header bảng
        dgvMovies.ColumnHeadersDefaultCellStyle.BackColor = accentColor;
        dgvMovies.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgvMovies.ColumnHeadersDefaultCellStyle.Font =
            new Font("Segoe UI", 10, FontStyle.Bold);

        dgvMovies.ColumnHeadersHeight = 45;
        dgvMovies.ColumnHeadersHeightSizeMode =
            DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        // Nội dung bảng
        dgvMovies.DefaultCellStyle.BackColor = panelColor;
        dgvMovies.DefaultCellStyle.ForeColor = Color.White;
        dgvMovies.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f);
        dgvMovies.DefaultCellStyle.SelectionBackColor =
            Color.FromArgb(51, 65, 85);
        dgvMovies.DefaultCellStyle.SelectionForeColor = Color.White;

        dgvMovies.AlternatingRowsDefaultCellStyle.BackColor =
            Color.FromArgb(38, 50, 70);

        dgvMovies.RowTemplate.Height = 40;

        // Các cột dữ liệu
        dgvMovies.Columns.Add("Id", "Mã phim");
        dgvMovies.Columns.Add("Title", "Tên phim");
        dgvMovies.Columns.Add("Genre", "Thể loại");
        dgvMovies.Columns.Add("Duration", "Thời lượng");
        dgvMovies.Columns.Add("Director", "Đạo diễn");
        dgvMovies.Columns.Add("ReleaseDate", "Ngày khởi chiếu");
        dgvMovies.Columns.Add("Status", "Trạng thái");

        // Giới hạn chiều rộng tối thiểu để tránh cột quá nhỏ
        dgvMovies.Columns["Id"]!.MinimumWidth = 55;
        dgvMovies.Columns["Title"]!.MinimumWidth = 150;
        dgvMovies.Columns["Genre"]!.MinimumWidth = 100;
        dgvMovies.Columns["Duration"]!.MinimumWidth = 90;
        dgvMovies.Columns["Director"]!.MinimumWidth = 120;
        dgvMovies.Columns["ReleaseDate"]!.MinimumWidth = 115;
        dgvMovies.Columns["Status"]!.MinimumWidth = 110;

        dgvMovies.Columns["Id"]!.FillWeight = 45;
        dgvMovies.Columns["Title"]!.FillWeight = 150;
        dgvMovies.Columns["Genre"]!.FillWeight = 100;
        dgvMovies.Columns["Duration"]!.FillWeight = 80;
        dgvMovies.Columns["Director"]!.FillWeight = 120;
        dgvMovies.Columns["ReleaseDate"]!.FillWeight = 110;
        dgvMovies.Columns["Status"]!.FillWeight = 100;

        // Chân trang
        lblTotal = new Label
        {
            Text = "Tổng số phim: 0",
            Dock = DockStyle.Bottom,
            Height = 38,
            ForeColor = textColor,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(5, 0, 0, 0),
            BackColor = backgroundColor
        };

        // Thứ tự thêm control để Dock hoạt động đúng
        Controls.Add(dgvMovies);
        Controls.Add(lblTotal);
        Controls.Add(buttonPanel);
        Controls.Add(filterPanel);
        Controls.Add(headerPanel);

        // Sự kiện tìm kiếm và lọc
        txtSearch.TextChanged += (s, e) => RenderMovieGrid();
        cboStatus.SelectedIndexChanged += (s, e) => RenderMovieGrid();

        // Sự kiện các nút chức năng
        btnAdd.Click += BtnAdd_Click;
        btnEdit.Click += BtnEdit_Click;
        btnDelete.Click += BtnDelete_Click;
        btnReload.Click += (s, e) => LoadMovieData();
    }

    // =====================================
    // 2. TẠO NÚT CHỨC NĂNG
    // =====================================

    private Button CreateButton(string text, Color color)
    {
        Button button = new Button
        {
            Text = text,
            Size = new Size(130, 38),
            MinimumSize = new Size(105, 36),

            BackColor = color,
            ForeColor = Color.White,

            Font = new Font("Segoe UI", 9, FontStyle.Bold),

            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,

            Margin = new Padding(0, 0, 12, 8),

            UseVisualStyleBackColor = false
        };

        button.FlatAppearance.BorderSize = 0;

        return button;
    }

    // =====================================
    // 3. ĐIỀU CHỈNH GIAO DIỆN RESPONSIVE
    // =====================================

    private void MovieManagementUC_SizeChanged(object? sender, EventArgs e)
    {
        UpdateResponsiveLayout();
    }

    private void UpdateResponsiveLayout()
    {
        if (dgvMovies == null || dgvMovies.IsDisposed)
            return;

        int availableWidth = ClientSize.Width;

        if (availableWidth <= 0)
            return;

        bool compact = availableWidth < 950;

        // Điều chỉnh chiều rộng ô tìm kiếm
        if (txtSearch != null && !txtSearch.IsDisposed)
        {
            txtSearch.Width = compact ? 220 : 350;
        }

        // Điều chỉnh chiều rộng ComboBox
        if (cboStatus != null && !cboStatus.IsDisposed)
        {
            cboStatus.Width = compact ? 155 : 190;
        }

        // Khi màn hình hẹp, cho phép các cột cuộn ngang
        dgvMovies.ScrollBars = ScrollBars.Both;

        // Cho phép các nút xuống dòng khi không đủ chiều rộng
        if (buttonPanel != null && !buttonPanel.IsDisposed)
        {
            buttonPanel.WrapContents = true;
        }

        // Cho phép khu vực tìm kiếm tự xuống dòng
        if (filterPanel != null && !filterPanel.IsDisposed)
        {
            filterPanel.WrapContents = true;
        }
    }

    // =====================================
    // 4. TẢI VÀ HIỂN THỊ DANH SÁCH PHIM
    // =====================================

    private void LoadMovieData()
    {
        try
        {
            _allMovies = _movieRepository.GetAll();

            RenderMovieGrid();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Không thể tải danh sách phim từ database.\n\n" + ex.Message,
                "Lỗi tải dữ liệu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    private void RenderMovieGrid()
    {
        if (dgvMovies == null || txtSearch == null || cboStatus == null)
            return;

        dgvMovies.Rows.Clear();

        string keyword = txtSearch.Text.Trim();

        string selectedStatus =
            cboStatus.SelectedItem?.ToString() ?? "Tất cả";

        var result = _allMovies.Where(movie =>
        {
            bool matchKeyword =
                string.IsNullOrEmpty(keyword) ||
                (movie.Title ?? string.Empty).Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase
                ) ||
                (movie.Genre ?? string.Empty).Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase
                ) ||
                (movie.Director ?? string.Empty).Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase
                );

            bool matchStatus = selectedStatus switch
            {
                "Đang chiếu" => movie.Status == MovieStatus.Showing,
                "Sắp chiếu" => movie.Status == MovieStatus.ComingSoon,
                "Đã kết thúc" => movie.Status == MovieStatus.Ended,
                _ => true
            };

            return matchKeyword && matchStatus;

        }).ToList();

        foreach (Movie movie in result)
        {
            dgvMovies.Rows.Add(
                movie.Id,
                movie.Title,
                movie.Genre,
                movie.DurationMinutes + " phút",
                movie.Director,
                movie.ReleaseDate.ToString("dd/MM/yyyy"),
                GetStatusText(movie.Status)
            );
        }

        lblTotal.Text =
            $"Tổng số phim: {result.Count} / {_allMovies.Count}";
    }

    // =====================================
    // 5. HIỂN THỊ TRẠNG THÁI PHIM
    // =====================================

    private string GetStatusText(MovieStatus status)
    {
        return status switch
        {
            MovieStatus.Showing => "Đang chiếu",
            MovieStatus.ComingSoon => "Sắp chiếu",
            MovieStatus.Ended => "Đã kết thúc",
            _ => "Không xác định"
        };
    }

    // =====================================
    // 6. THÊM PHIM
    // =====================================

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        try
        {
            using MovieEditDialog dialog = new MovieEditDialog();

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                int newId = _movieRepository.Add(dialog.MovieData);

                LoadMovieData();

                MessageBox.Show(
                    $"Thêm phim thành công!\nMã phim: {newId}",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Không thể thêm phim vào database.\n\n" + ex.Message,
                "Lỗi thêm phim",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    // =====================================
    // 7. SỬA PHIM
    // =====================================

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        try
        {
            if (dgvMovies.SelectedRows.Count == 0)
            {
                MessageBox.Show(
                    "Vui lòng chọn phim cần sửa!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            int id = Convert.ToInt32(
                dgvMovies.SelectedRows[0].Cells["Id"].Value
            );

            Movie? selectedMovie = _movieRepository.GetById(id);

            if (selectedMovie == null)
            {
                MessageBox.Show(
                    "Không tìm thấy phim!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                LoadMovieData();

                return;
            }

            using MovieEditDialog dialog =
                new MovieEditDialog(selectedMovie);

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                dialog.MovieData.Id = id;

                bool updated = _movieRepository.Update(dialog.MovieData);

                if (updated)
                {
                    LoadMovieData();

                    MessageBox.Show(
                        "Cập nhật phim thành công!",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                else
                {
                    MessageBox.Show(
                        "Không có dữ liệu phim nào được cập nhật.",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Không thể cập nhật phim.\n\n" + ex.Message,
                "Lỗi cập nhật",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    // =====================================
    // 8. XÓA PHIM
    // =====================================

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        try
        {
            if (dgvMovies.SelectedRows.Count == 0)
            {
                MessageBox.Show(
                    "Vui lòng chọn phim cần xóa!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            int id = Convert.ToInt32(
                dgvMovies.SelectedRows[0].Cells["Id"].Value
            );

            DialogResult confirm = MessageBox.Show(
                "Bạn có chắc chắn muốn xóa phim này?",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirm != DialogResult.Yes)
                return;

            bool deleted = _movieRepository.Delete(id);

            if (deleted)
            {
                LoadMovieData();

                MessageBox.Show(
                    "Xóa phim thành công!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            else
            {
                MessageBox.Show(
                    "Không tìm thấy phim cần xóa.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Không thể xóa phim.\n\n" +
                "Có thể phim đang được sử dụng trong lịch chiếu.\n\n" +
                ex.Message,
                "Lỗi xóa phim",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }
}
