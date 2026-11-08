using System;
using System.Drawing;
using System.Windows.Forms;

namespace CinemaManagement.UserControls.Admin;

public partial class MovieManagementUC : UserControl
{
    private DataGridView dgvMovies = null!;
    private TextBox txtTitle = null!;
    private TextBox txtDuration = null!;
    private ComboBox cmbGenre = null!;
    private ComboBox cmbStatus = null!;
    private Button btnAdd = null!;
    private Button btnEdit = null!;
    private Button btnDelete = null!;
    private Button btnClear = null!;

    public MovieManagementUC()
    {
        InitializeComponent();
        BuildUI();
    }

    private void BuildUI()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.FromArgb(248, 249, 250);
        this.Padding = new Padding(15);

        // Khung trên: Nhập thông tin phim
        var grpInfo = new GroupBox
        {
            Text = "Thông Tin Phim",
            Dock = DockStyle.Top,
            Height = 135,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(44, 62, 80),
            Padding = new Padding(15)
        };

        var lblT = new Label { Text = "Tên phim:", Location = new Point(20, 35), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Regular) };
        txtTitle = new TextBox { Location = new Point(95, 32), Width = 280, Font = new Font("Segoe UI", 9.5F) };

        var lblD = new Label { Text = "Thời lượng (phút):", Location = new Point(400, 35), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Regular) };
        txtDuration = new TextBox { Location = new Point(520, 32), Width = 90, Font = new Font("Segoe UI", 9.5F) };

        var lblG = new Label { Text = "Thể loại:", Location = new Point(20, 75), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Regular) };
        cmbGenre = new ComboBox { Location = new Point(95, 72), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
        cmbGenre.Items.AddRange(new object[] { "Hành Động", "Tâm Lý - Tình Cảm", "Hài Kịch", "Kinh Dị", "Khoa Học Viễn Tưởng", "Hoạt Hình" });
        cmbGenre.SelectedIndex = 0;

        var lblS = new Label { Text = "Trạng thái:", Location = new Point(400, 75), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Regular) };
        cmbStatus = new ComboBox { Location = new Point(520, 72), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
        cmbStatus.Items.AddRange(new object[] { "Đang chiếu", "Sắp chiếu", "Đã ngưng chiếu" });
        cmbStatus.SelectedIndex = 0;

        // Các nút bấm chức năng
        btnAdd = new Button { Text = "➕ Thêm", Location = new Point(695, 30), Size = new Size(85, 32), BackColor = Color.FromArgb(46, 204, 113), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        btnEdit = new Button { Text = "✏️ Sửa", Location = new Point(790, 30), Size = new Size(85, 32), BackColor = Color.FromArgb(52, 152, 219), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        btnDelete = new Button { Text = "🗑️ Xóa", Location = new Point(695, 70), Size = new Size(85, 32), BackColor = Color.FromArgb(231, 76, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        btnClear = new Button { Text = "🔄 Mới", Location = new Point(790, 70), Size = new Size(85, 32), BackColor = Color.FromArgb(149, 165, 166), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };

        btnAdd.Click += (s, e) => {
            if (string.IsNullOrWhiteSpace(txtTitle.Text)) { MessageBox.Show("Vui lòng nhập tên phim!"); return; }
            dgvMovies.Rows.Add(dgvMovies.Rows.Count + 1, txtTitle.Text, cmbGenre.SelectedItem, txtDuration.Text + " phút", cmbStatus.SelectedItem);
            MessageBox.Show("Đã thêm phim mới thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearInput();
        };

        btnDelete.Click += (s, e) => {
            if (dgvMovies.SelectedRows.Count > 0)
            {
                dgvMovies.Rows.RemoveAt(dgvMovies.SelectedRows[0].Index);
                MessageBox.Show("Đã xóa phim khỏi danh sách!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        };

        btnClear.Click += (s, e) => ClearInput();

        grpInfo.Controls.AddRange(new Control[] { lblT, txtTitle, lblD, txtDuration, lblG, cmbGenre, lblS, cmbStatus, btnAdd, btnEdit, btnDelete, btnClear });

        // Khung dưới: DataGridView hiển thị danh sách
        var grpList = new GroupBox
        {
            Text = "Danh Sách Phim Chiếu Rạp",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(44, 62, 80),
            Padding = new Padding(10)
        };

        dgvMovies = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            ReadOnly = true,
            RowHeadersVisible = false,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
        };

        dgvMovies.Columns.Add("Id", "Mã");
        dgvMovies.Columns.Add("Title", "Tên Phim");
        dgvMovies.Columns.Add("Genre", "Thể Loại");
        dgvMovies.Columns.Add("Duration", "Thời Lượng");
        dgvMovies.Columns.Add("Status", "Trạng Thái");

        // Dữ liệu mẫu demo
        dgvMovies.Rows.Add(1, "Mai", "Tâm Lý - Tình Cảm", "131 phút", "Đang chiếu");
        dgvMovies.Rows.Add(2, "Đất Rừng Phương Nam", "Hành Động - Lịch Sử", "110 phút", "Đang chiếu");
        dgvMovies.Rows.Add(3, "Dune: Hành Tinh Cát 2", "Khoa Học Viễn Tưởng", "166 phút", "Đang chiếu");
        dgvMovies.Rows.Add(4, "Kung Fu Panda 4", "Hoạt Hình - Hài", "94 phút", "Sắp chiếu");
        dgvMovies.Rows.Add(5, "Godzilla x Kong", "Hành Động - Quái Thú", "115 phút", "Sắp chiếu");

        dgvMovies.CellClick += (s, e) => {
            if (e.RowIndex >= 0)
            {
                var row = dgvMovies.Rows[e.RowIndex];
                txtTitle.Text = row.Cells[1].Value?.ToString();
                txtDuration.Text = row.Cells[3].Value?.ToString()?.Replace(" phút", "");
            }
        };

        grpList.Controls.Add(dgvMovies);
        this.Controls.Add(grpList);
        this.Controls.Add(grpInfo);
    }

    private void ClearInput()
    {
        txtTitle.Clear();
        txtDuration.Clear();
        cmbGenre.SelectedIndex = 0;
        cmbStatus.SelectedIndex = 0;
    }
}