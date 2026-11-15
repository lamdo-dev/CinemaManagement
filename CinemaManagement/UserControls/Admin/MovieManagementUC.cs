using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Dialogs;
using CinemaManagement.Models;
using CinemaManagement.Models.Enums;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CinemaManagement.UserControls.Admin;

public partial class MovieManagementUC : UserControl
{
    private const string ConnectionString = "Data Source=cinema.db";

    private List<Movie> movies = new();

    private DataGridView dgvMovies = null!;
    private TextBox txtSearch = null!;
    private ComboBox cboStatus = null!;
    private Label lblTotal = null!;

    private Button btnAdd = null!;
    private Button btnEdit = null!;
    private Button btnDelete = null!;
    private Button btnReload = null!;

    private readonly Color backgroundColor = Color.FromArgb(15, 23, 42);
    private readonly Color panelColor = Color.FromArgb(30, 41, 59);
    private readonly Color accentColor = Color.FromArgb(37, 99, 235);
    private readonly Color textColor = Color.FromArgb(226, 232, 240);

    public MovieManagementUC()
    {
        InitializeComponent();

        Dock = DockStyle.Fill;
        BackColor = backgroundColor;
        Padding = new Padding(10);

        BuildInterface();
        LoadMoviesFromDatabase();
    }

    // =====================================
    // 1. XÂY DỰNG GIAO DIỆN
    // =====================================

    private void BuildInterface()
    {
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
            Text = "Quản lý danh sách phim trong hệ thống rạp chiếu (Dữ liệu lưu trực tiếp vào cinema.db)",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(12, 52)
        };

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
            Font = new Font("Segoe UI", 10),
            PlaceholderText = "Nhập tên phim hoặc thể loại..."
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

        btnAdd = CreateButton("+ Thêm phim", accentColor);
        btnEdit = CreateButton("Sửa phim", Color.FromArgb(217, 119, 6));
        btnDelete = CreateButton("Xóa phim", Color.FromArgb(220, 38, 38));
        btnReload = CreateButton("🔄 Tải lại", Color.FromArgb(71, 85, 105));

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
        buttonPanel.Controls.Add(btnReload);

        dgvMovies = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = panelColor,
            BorderStyle = BorderStyle.None,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false
        };

        dgvMovies.ColumnHeadersDefaultCellStyle.BackColor = accentColor;
        dgvMovies.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgvMovies.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        dgvMovies.ColumnHeadersHeight = 45;

        dgvMovies.DefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
        dgvMovies.DefaultCellStyle.ForeColor = Color.White;
        dgvMovies.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f);
        dgvMovies.DefaultCellStyle.SelectionBackColor = Color.FromArgb(51, 65, 85);
        dgvMovies.DefaultCellStyle.SelectionForeColor = Color.White;
        dgvMovies.RowTemplate.Height = 40;

        dgvMovies.Columns.Add("Id", "Mã phim");
        dgvMovies.Columns.Add("Title", "Tên phim");
        dgvMovies.Columns.Add("Genre", "Thể loại");
        dgvMovies.Columns.Add("Duration", "Thời lượng");
        dgvMovies.Columns.Add("Director", "Đạo diễn");
        dgvMovies.Columns.Add("ReleaseDate", "Ngày khởi chiếu");
        dgvMovies.Columns.Add("Status", "Trạng thái");

        dgvMovies.Columns["Id"]!.FillWeight = 45;
        dgvMovies.Columns["Title"]!.FillWeight = 140;

        lblTotal = new Label
        {
            Text = "Tổng số phim: 0",
            Dock = DockStyle.Bottom,
            Height = 35,
            ForeColor = textColor,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        Controls.Add(dgvMovies);
        Controls.Add(lblTotal);
        Controls.Add(buttonPanel);
        Controls.Add(filterPanel);
        Controls.Add(lblDescription);
        Controls.Add(lblTitle);

        txtSearch.TextChanged += (s, e) => RenderMovieGrid();
        cboStatus.SelectedIndexChanged += (s, e) => RenderMovieGrid();

        btnAdd.Click += BtnAdd_Click;
        btnEdit.Click += BtnEdit_Click;
        btnDelete.Click += BtnDelete_Click;
        btnReload.Click += (s, e) => LoadMoviesFromDatabase();
    }

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
    // 2. ĐỒNG BỘ BẢNG & TẢI DỮ LIỆU TỪ SQLITE
    // =====================================

    private void EnsureMoviesTable(SqliteConnection connection)
    {
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Movies (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Description TEXT,
                DurationMinutes INTEGER NOT NULL DEFAULT 120,
                Director TEXT,
                Genre TEXT,
                PosterUrl TEXT,
                ReleaseDate TEXT,
                Status INTEGER NOT NULL DEFAULT 0
            );");

        var existingCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "PRAGMA table_info(\"Movies\");";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                existingCols.Add(reader["name"]?.ToString() ?? string.Empty);
            }
        }

        if (!existingCols.Contains("Description"))
            connection.Execute("ALTER TABLE Movies ADD COLUMN Description TEXT;");
        if (!existingCols.Contains("DurationMinutes"))
            connection.Execute("ALTER TABLE Movies ADD COLUMN DurationMinutes INTEGER NOT NULL DEFAULT 120;");
        if (!existingCols.Contains("Director"))
            connection.Execute("ALTER TABLE Movies ADD COLUMN Director TEXT;");
        if (!existingCols.Contains("Genre"))
            connection.Execute("ALTER TABLE Movies ADD COLUMN Genre TEXT;");
        if (!existingCols.Contains("PosterUrl"))
            connection.Execute("ALTER TABLE Movies ADD COLUMN PosterUrl TEXT;");
        if (!existingCols.Contains("ReleaseDate"))
            connection.Execute("ALTER TABLE Movies ADD COLUMN ReleaseDate TEXT;");
        if (!existingCols.Contains("Status"))
            connection.Execute("ALTER TABLE Movies ADD COLUMN Status INTEGER NOT NULL DEFAULT 0;");
    }

    private void LoadMoviesFromDatabase()
    {
        try
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            EnsureMoviesTable(connection);

            var rows = connection.Query(@"
                SELECT 
                    rowid AS Id,
                    COALESCE(Title, '') AS Title,
                    COALESCE(Description, '') AS Description,
                    COALESCE(DurationMinutes, 120) AS DurationMinutes,
                    COALESCE(Director, '') AS Director,
                    COALESCE(Genre, '') AS Genre,
                    COALESCE(PosterUrl, '') AS PosterUrl,
                    COALESCE(ReleaseDate, '') AS ReleaseDate,
                    COALESCE(Status, 0) AS Status
                FROM Movies
                ORDER BY rowid DESC;").ToList();

            movies = rows.Select(r => new Movie
            {
                Id = Convert.ToInt32(r.Id),
                Title = (string)r.Title,
                Description = (string)r.Description,
                DurationMinutes = Convert.ToInt32(r.DurationMinutes),
                Director = (string)r.Director,
                Genre = (string)r.Genre,
                PosterUrl = (string)r.PosterUrl,
                ReleaseDate = ParseReleaseDate((string)r.ReleaseDate),
                Status = (MovieStatus)Convert.ToInt32(r.Status)
            }).ToList();

            RenderMovieGrid();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Lỗi khi tải danh sách phim từ cơ sở dữ liệu:\n{ex.Message}",
                "Lỗi SQLite",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    private static DateTime ParseReleaseDate(string rawDate)
    {
        if (DateTime.TryParseExact(
            rawDate,
            new[] { "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "dd/MM/yyyy" },
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime parsedExact))
        {
            return parsedExact;
        }

        if (DateTime.TryParse(rawDate, out DateTime parsed))
            return parsed;

        return DateTime.Today;
    }

    private void RenderMovieGrid()
    {
        dgvMovies.Rows.Clear();

        string keyword = txtSearch.Text.Trim().ToLowerInvariant();
        string selectedStatus = cboStatus.SelectedItem?.ToString() ?? "Tất cả";

        var result = movies.Where(movie =>
        {
            bool matchName =
                movie.Title.ToLowerInvariant().Contains(keyword) ||
                movie.Genre.ToLowerInvariant().Contains(keyword) ||
                movie.Director.ToLowerInvariant().Contains(keyword);

            bool matchStatus = selectedStatus switch
            {
                "Đang chiếu" => movie.Status == MovieStatus.Showing,
                "Sắp chiếu" => movie.Status == MovieStatus.ComingSoon,
                "Đã kết thúc" => movie.Status == MovieStatus.Ended,
                _ => true
            };

            return matchName && matchStatus;
        }).ToList();

        foreach (Movie movie in result)
        {
            int rowIndex = dgvMovies.Rows.Add(
                movie.Id,
                movie.Title,
                movie.Genre,
                movie.DurationMinutes + " phút",
                movie.Director,
                movie.ReleaseDate.ToString("dd/MM/yyyy"),
                GetStatusText(movie.Status)
            );
            dgvMovies.Rows[rowIndex].Tag = movie;
        }

        lblTotal.Text = $"Tổng số phim: {result.Count} / {movies.Count}";
    }

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
    // 3. THÊM / SỬA / XÓA PHIM TRONG SQLITE
    // =====================================

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        using MovieEditDialog dialog = new MovieEditDialog();

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            Movie m = dialog.MovieData;

            try
            {
                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();

                connection.Execute(@"
                    INSERT INTO Movies 
                        (Title, Description, DurationMinutes, Director, Genre, PosterUrl, ReleaseDate, Status)
                    VALUES 
                        (@Title, @Description, @DurationMinutes, @Director, @Genre, @PosterUrl, @ReleaseDate, @Status);",
                    new
                    {
                        m.Title,
                        m.Description,
                        m.DurationMinutes,
                        m.Director,
                        m.Genre,
                        m.PosterUrl,
                        ReleaseDate = m.ReleaseDate.ToString("yyyy-MM-dd"),
                        Status = (int)m.Status
                    });

                LoadMoviesFromDatabase();

                MessageBox.Show(
                    "Thêm phim mới vào cơ sở dữ liệu thành công!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi lưu phim mới:\n{ex.Message}",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        if (dgvMovies.SelectedRows.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn phim cần sửa!");
            return;
        }

        if (dgvMovies.SelectedRows[0].Tag is not Movie selectedMovie)
            return;

        using MovieEditDialog dialog = new MovieEditDialog(selectedMovie);

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            Movie m = dialog.MovieData;

            try
            {
                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();

                connection.Execute(@"
                    UPDATE Movies
                    SET Title = @Title,
                        Description = @Description,
                        DurationMinutes = @DurationMinutes,
                        Director = @Director,
                        Genre = @Genre,
                        PosterUrl = @PosterUrl,
                        ReleaseDate = @ReleaseDate,
                        Status = @Status
                    WHERE rowid = @Id;",
                    new
                    {
                        Id = selectedMovie.Id,
                        m.Title,
                        m.Description,
                        m.DurationMinutes,
                        m.Director,
                        m.Genre,
                        m.PosterUrl,
                        ReleaseDate = m.ReleaseDate.ToString("yyyy-MM-dd"),
                        Status = (int)m.Status
                    });

                LoadMoviesFromDatabase();

                MessageBox.Show(
                    "Cập nhật thông tin phim thành công!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi cập nhật phim:\n{ex.Message}",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (dgvMovies.SelectedRows.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn phim cần xóa!");
            return;
        }

        if (dgvMovies.SelectedRows[0].Tag is not Movie selectedMovie)
            return;

        DialogResult confirm = MessageBox.Show(
            $"Bạn có chắc chắn muốn xóa phim '{selectedMovie.Title}' không?",
            "Xác nhận xóa",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        );

        if (confirm == DialogResult.Yes)
        {
            try
            {
                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();

                connection.Execute(
                    "DELETE FROM Movies WHERE rowid = @Id;",
                    new { Id = selectedMovie.Id });

                LoadMoviesFromDatabase();
                MessageBox.Show("Xóa phim thành công!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi xóa phim:\n{ex.Message}",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}