
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using CinemaManagement.Models;
using CinemaManagement.Models.Enums;
using CinemaManagement.Dialogs;
using CinemaManagement.Repositories;

namespace CinemaManagement.UserControls.Admin;

public partial class MovieManagementUC : UserControl
{
    // Repository kết nối và thao tác với bảng Movies
    private readonly MovieRepository _movieRepository = new();

    // Các thành phần giao diện
    private DataGridView dgvMovies = null!;
    private TextBox txtSearch = null!;
    private ComboBox cboStatus = null!;
    private Label lblTotal = null!;

    private Button btnAdd = null!;
    private Button btnEdit = null!;
    private Button btnDelete = null!;

    // Màu giao diện đồng bộ với MainForm
    private readonly Color backgroundColor =
        Color.FromArgb(15, 23, 42);

    private readonly Color panelColor =
        Color.FromArgb(30, 41, 59);

    private readonly Color accentColor =
        Color.FromArgb(37, 99, 235);

    private readonly Color textColor =
        Color.FromArgb(226, 232, 240);

    public MovieManagementUC()
    {
        InitializeComponent();

        Dock = DockStyle.Fill;
        BackColor = backgroundColor;
        Padding = new Padding(10);

        BuildInterface();

        // Tải dữ liệu phim từ SQLite
        LoadMovieData();
    }

    // =====================================
    // 1. XÂY DỰNG GIAO DIỆN
    // =====================================

    private void BuildInterface()
    {
        // Tiêu đề trang
        Label lblTitle = new Label
        {
            Text = "QUẢN LÝ PHIM",
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(10, 10)
        };

        Label lblDescription = new Label
        {
            Text = "Quản lý danh sách phim trong hệ thống rạp chiếu",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(12, 52)
        };

        // Khu vực tìm kiếm
        Panel filterPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 75,
            BackColor = panelColor,
            Padding = new Padding(15),
            Margin = new Padding(0, 15, 0, 10)
        };

        Label lblSearch = new Label
        {
            Text = "Tìm kiếm phim:",
            ForeColor = textColor,
            AutoSize = true,
            Location = new Point(15, 27)
        };

        txtSearch = new TextBox
        {
            Location = new Point(145, 21),
            Size = new Size(250, 30),
            Font = new Font("Segoe UI", 10)
        };

        Label lblStatus = new Label
        {
            Text = "Trạng thái:",
            ForeColor = textColor,
            AutoSize = true,
            Location = new Point(425, 27)
        };

        cboStatus = new ComboBox
        {
            Location = new Point(515, 21),
            Size = new Size(180, 30),
            Font = new Font("Segoe UI", 10),
            DropDownStyle = ComboBoxStyle.DropDownList
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

        // Các nút chức năng
        btnAdd = CreateButton(
            "+ Thêm phim",
            accentColor
        );

        btnEdit = CreateButton(
            "Sửa phim",
            Color.FromArgb(217, 119, 6)
        );

        btnDelete = CreateButton(
            "Xóa phim",
            Color.FromArgb(220, 38, 38)
        );

        FlowLayoutPanel buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 55,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = backgroundColor,
            Padding = new Padding(0, 5, 0, 5),
            WrapContents = false
        };

        buttonPanel.Controls.Add(btnAdd);
        buttonPanel.Controls.Add(btnEdit);
        buttonPanel.Controls.Add(btnDelete);

        // Bảng danh sách phim
        dgvMovies = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = panelColor,
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

        // Header bảng
        dgvMovies.ColumnHeadersDefaultCellStyle.BackColor =
            accentColor;

        dgvMovies.ColumnHeadersDefaultCellStyle.ForeColor =
            Color.White;

        dgvMovies.ColumnHeadersDefaultCellStyle.Font =
            new Font("Segoe UI", 10, FontStyle.Bold);

        dgvMovies.ColumnHeadersHeight = 45;

        // Nội dung bảng
        dgvMovies.DefaultCellStyle.BackColor =
            Color.FromArgb(30, 41, 59);

        dgvMovies.DefaultCellStyle.ForeColor =
            Color.White;

        dgvMovies.DefaultCellStyle.Font =
            new Font("Segoe UI", 9.5f);

        dgvMovies.DefaultCellStyle.SelectionBackColor =
            Color.FromArgb(51, 65, 85);

        dgvMovies.DefaultCellStyle.SelectionForeColor =
            Color.White;

        dgvMovies.RowTemplate.Height = 40;

        // Tạo các cột
        dgvMovies.Columns.Add("Id", "Mã phim");
        dgvMovies.Columns.Add("Title", "Tên phim");
        dgvMovies.Columns.Add("Genre", "Thể loại");
        dgvMovies.Columns.Add("Duration", "Thời lượng");
        dgvMovies.Columns.Add("Director", "Đạo diễn");
        dgvMovies.Columns.Add("ReleaseDate", "Ngày khởi chiếu");
        dgvMovies.Columns.Add("Status", "Trạng thái");

        // Chân trang
        lblTotal = new Label
        {
            Text = "Tổng số phim: 0",
            Dock = DockStyle.Bottom,
            Height = 35,
            ForeColor = textColor,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Thêm các thành phần vào UserControl
        Controls.Add(dgvMovies);
        Controls.Add(lblTotal);
        Controls.Add(buttonPanel);
        Controls.Add(filterPanel);
        Controls.Add(lblDescription);
        Controls.Add(lblTitle);

        // Sự kiện tìm kiếm
        txtSearch.TextChanged += (s, e) => LoadMovieData();

        // Sự kiện lọc trạng thái
        cboStatus.SelectedIndexChanged +=
            (s, e) => LoadMovieData();

        // Sự kiện các nút chức năng
        btnAdd.Click += BtnAdd_Click;
        btnEdit.Click += BtnEdit_Click;
        btnDelete.Click += BtnDelete_Click;
    }

    // =====================================
    // 2. TẠO BUTTON
    // =====================================

    private Button CreateButton(string text, Color color)
    {
        Button button = new Button
        {
            Text = text,
            Size = new Size(135, 38),
            BackColor = color,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 12, 0)
        };

        button.FlatAppearance.BorderSize = 0;

        return button;
    }

    // =====================================
    // 3. HIỂN THỊ DANH SÁCH PHIM TỪ DATABASE
    // =====================================

    private void LoadMovieData()
    {
        try
        {
            dgvMovies.Rows.Clear();

            string keyword = txtSearch.Text.Trim();

            string selectedStatus =
                cboStatus.SelectedItem?.ToString() ?? "Tất cả";

            // Lấy dữ liệu từ SQLite
            List<Movie> result = string.IsNullOrWhiteSpace(keyword)
                ? _movieRepository.GetAll()
                : _movieRepository.Search(keyword);

            // Lọc phim theo trạng thái
            result = result.Where(movie => selectedStatus switch
            {
                "Đang chiếu" =>
                    movie.Status == MovieStatus.Showing,

                "Sắp chiếu" =>
                    movie.Status == MovieStatus.ComingSoon,

                "Đã kết thúc" =>
                    movie.Status == MovieStatus.Ended,

                _ => true

            }).ToList();

            // Hiển thị dữ liệu lên DataGridView
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

            lblTotal.Text = $"Tổng số phim: {result.Count}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Không thể tải danh sách phim từ database.\n\n" +
                ex.Message,
                "Lỗi tải dữ liệu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    // =====================================
    // 4. HIỂN THỊ TRẠNG THÁI TIẾNG VIỆT
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
    // 5. NÚT THÊM PHIM
    // =====================================

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        try
        {
            using MovieEditDialog dialog = new MovieEditDialog();

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                // Lấy thông tin phim từ Dialog
                Movie newMovie = dialog.MovieData;

                // Thêm phim vào database
                int newId = _movieRepository.Add(newMovie);

                // Cập nhật lại danh sách
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
                "Không thể thêm phim vào database.\n\n" +
                ex.Message,
                "Lỗi thêm phim",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    // =====================================
    // 6. NÚT SỬA PHIM
    // =====================================

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        try
        {
            // Kiểm tra đã chọn phim chưa
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

            // Lấy mã phim được chọn
            int id = Convert.ToInt32(
                dgvMovies.SelectedRows[0].Cells["Id"].Value
            );

            // Lấy thông tin phim từ database
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

            // Mở cửa sổ chỉnh sửa
            using MovieEditDialog dialog =
                new MovieEditDialog(selectedMovie);

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                // Đảm bảo giữ nguyên mã phim
                dialog.MovieData.Id = id;

                // Cập nhật database
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
                "Không thể cập nhật phim.\n\n" +
                ex.Message,
                "Lỗi cập nhật",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    // =====================================
    // 7. NÚT XÓA PHIM
    // =====================================

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        try
        {
            // Kiểm tra đã chọn phim chưa
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

            // Lấy mã phim được chọn
            int id = Convert.ToInt32(
                dgvMovies.SelectedRows[0].Cells["Id"].Value
            );

            // Xác nhận xóa
            DialogResult confirm = MessageBox.Show(
                "Bạn có chắc chắn muốn xóa phim này?",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            // Xóa phim trong database
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
