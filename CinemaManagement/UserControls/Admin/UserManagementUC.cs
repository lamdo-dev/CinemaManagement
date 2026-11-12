using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Models;
using CinemaManagement.Models.Enums;
using CinemaManagement.Utils;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CinemaManagement.UserControls.Admin
{
    public partial class UserManagementUC : UserControl
    {
        private const string ConnectionString = "Data Source=cinema.db";

        private readonly Color ColorPrimary = Color.FromArgb(124, 58, 237);
        private readonly Color ColorDark = Color.FromArgb(30, 41, 59);
        private readonly Color ColorBg = Color.FromArgb(245, 246, 250);
        private readonly Color ColorMuted = Color.FromArgb(100, 116, 139);

        private List<User> _allUsers = new();
        private int _selectedUserId = 0;

        // Controls
        private TextBox txtSearch = null!;
        private ComboBox cboFilterRole = null!;
        private DataGridView dgvUsers = null!;
        private Label lblCount = null!;

        private Label lblFormHeader = null!;
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private TextBox txtFullName = null!;
        private TextBox txtEmail = null!;
        private TextBox txtPhone = null!;
        private ComboBox cboRole = null!;
        private NumericUpDown numPoints = null!;

        private Button btnAdd = null!;
        private Button btnUpdate = null!;
        private Button btnDelete = null!;
        private Button btnResetForm = null!;

        public UserManagementUC()
        {
            InitializeComponentManual();
            LoadUsersFromDatabase();
        }

        // =====================================================
        // 1. KHỞI TẠO GIAO DIỆN
        // =====================================================

        private void InitializeComponentManual()
        {
            Dock = DockStyle.Fill;
            BackColor = ColorBg;
            Padding = new Padding(20);

            // -------------------------------------------------
            // TOP BAR: TIÊU ĐỀ + TÌM KIẾM & LỌC
            // -------------------------------------------------
            Panel pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 76,
                BackColor = ColorBg,
                Padding = new Padding(0, 0, 0, 12)
            };

            Panel pnlTitle = new Panel
            {
                Dock = DockStyle.Fill
            };

            Label lblTitle = new Label
            {
                Text = "👥 QUẢN LÝ TÀI KHOẢN NGƯỜI DÙNG",
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = ColorDark,
                Location = new Point(0, 2),
                AutoSize = true
            };

            lblCount = new Label
            {
                Text = "Tổng số tài khoản: 0",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = ColorMuted,
                Location = new Point(2, 36),
                AutoSize = true
            };

            pnlTitle.Controls.AddRange(new Control[] { lblTitle, lblCount });

            Panel pnlFilter = new Panel
            {
                Dock = DockStyle.Right,
                Width = 490
            };

            txtSearch = new TextBox
            {
                Location = new Point(0, 16),
                Size = new Size(230, 30),
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "🔍 Tìm username, họ tên, SĐT..."
            };
            txtSearch.TextChanged += (s, e) => ApplyFilter();

            cboFilterRole = new ComboBox
            {
                Location = new Point(240, 16),
                Size = new Size(145, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };
            cboFilterRole.Items.AddRange(new object[]
            {
                "Tất cả vai trò",
                "Admin",
                "Staff",
                "Customer"
            });
            cboFilterRole.SelectedIndex = 0;
            cboFilterRole.SelectedIndexChanged += (s, e) => ApplyFilter();

            Button btnReload = new Button
            {
                Text = "🔄 Tải lại",
                Location = new Point(395, 15),
                Size = new Size(95, 31),
                BackColor = Color.White,
                ForeColor = ColorDark,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnReload.FlatAppearance.BorderColor = Color.LightGray;
            btnReload.Click += (s, e) => LoadUsersFromDatabase();

            pnlFilter.Controls.AddRange(new Control[]
            {
                txtSearch,
                cboFilterRole,
                btnReload
            });

            pnlTop.Controls.Add(pnlTitle);
            pnlTop.Controls.Add(pnlFilter);

            // -------------------------------------------------
            // RIGHT PANEL: FORM THÊM / SỬA TÀI KHOẢN
            // -------------------------------------------------
            Panel pnlForm = new Panel
            {
                Dock = DockStyle.Right,
                Width = 360,
                BackColor = Color.White,
                Padding = new Padding(20),
                AutoScroll = true
            };

            lblFormHeader = new Label
            {
                Text = "➕ THÊM TÀI KHOẢN MỚI",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = ColorPrimary,
                Location = new Point(20, 16),
                Size = new Size(320, 28)
            };

            int y = 54;
            int step = 58;

            // Username
            pnlForm.Controls.Add(CreateFieldLabel("Tên đăng nhập (*):", y));
            txtUsername = CreateTextBox(y + 20, "Ví dụ: staff_quay01");
            pnlForm.Controls.Add(txtUsername);
            y += step;

            // Password
            pnlForm.Controls.Add(CreateFieldLabel("Mật khẩu (* khi tạo mới):", y));
            txtPassword = CreateTextBox(y + 20, "Để trống nếu giữ mật khẩu cũ");
            pnlForm.Controls.Add(txtPassword);
            y += step;

            // FullName
            pnlForm.Controls.Add(CreateFieldLabel("Họ và tên (*):", y));
            txtFullName = CreateTextBox(y + 20, "Nhập họ tên đầy đủ");
            pnlForm.Controls.Add(txtFullName);
            y += step;

            // Role
            pnlForm.Controls.Add(CreateFieldLabel("Vai trò hệ thống (*):", y));
            cboRole = new ComboBox
            {
                Location = new Point(20, y + 20),
                Size = new Size(315, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };
            cboRole.Items.AddRange(new object[]
            {
                "Admin - Quản trị viên",
                "Staff - Nhân viên quầy / Soát vé",
                "Customer - Khách hàng"
            });
            cboRole.SelectedIndex = 1;
            pnlForm.Controls.Add(cboRole);
            y += step;

            // Email
            pnlForm.Controls.Add(CreateFieldLabel("Email:", y));
            txtEmail = CreateTextBox(y + 20, "example@cinema.vn");
            pnlForm.Controls.Add(txtEmail);
            y += step;

            // Phone & Points
            pnlForm.Controls.Add(CreateFieldLabel("Số điện thoại:", y));
            txtPhone = new TextBox
            {
                Location = new Point(20, y + 20),
                Size = new Size(185, 28),
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "09xxxxxxxx"
            };
            pnlForm.Controls.Add(txtPhone);

            Label lblPts = new Label
            {
                Text = "Điểm thưởng:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = ColorMuted,
                Location = new Point(215, y),
                AutoSize = true
            };
            pnlForm.Controls.Add(lblPts);

            numPoints = new NumericUpDown
            {
                Location = new Point(215, y + 20),
                Size = new Size(120, 28),
                Font = new Font("Segoe UI", 10),
                Minimum = 0,
                Maximum = 10000000,
                Increment = 10
            };
            pnlForm.Controls.Add(numPoints);
            y += step + 10;

            // Buttons Row 1: Thêm & Cập nhật
            btnAdd = new Button
            {
                Text = "➕ Thêm mới",
                Location = new Point(20, y),
                Size = new Size(152, 40),
                BackColor = ColorPrimary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += BtnAdd_Click;

            btnUpdate = new Button
            {
                Text = "💾 Lưu cập nhật",
                Location = new Point(183, y),
                Size = new Size(152, 40),
                BackColor = Color.FromArgb(14, 165, 233),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnUpdate.FlatAppearance.BorderSize = 0;
            btnUpdate.Click += BtnUpdate_Click;

            y += 48;

            // Buttons Row 2: Xóa & Nhập mới
            btnDelete = new Button
            {
                Text = "🗑 Xóa tài khoản",
                Location = new Point(20, y),
                Size = new Size(152, 38),
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Click += BtnDelete_Click;

            btnResetForm = new Button
            {
                Text = "✨ Nhập mới",
                Location = new Point(183, y),
                Size = new Size(152, 38),
                BackColor = Color.FromArgb(226, 232, 240),
                ForeColor = ColorDark,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnResetForm.FlatAppearance.BorderSize = 0;
            btnResetForm.Click += (s, e) => ResetForm();

            pnlForm.Controls.Add(lblFormHeader);
            pnlForm.Controls.Add(btnAdd);
            pnlForm.Controls.Add(btnUpdate);
            pnlForm.Controls.Add(btnDelete);
            pnlForm.Controls.Add(btnResetForm);

            Panel pnlSpacer = new Panel
            {
                Dock = DockStyle.Right,
                Width = 16,
                BackColor = ColorBg
            };

            // -------------------------------------------------
            // CENTER: DATAGRIDVIEW DANH SÁCH TÀI KHOẢN
            // -------------------------------------------------
            dgvUsers = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 42,
                RowTemplate = { Height = 38 },
                Font = new Font("Segoe UI", 9.5f),
                GridColor = Color.FromArgb(235, 238, 245)
            };

            dgvUsers.ColumnHeadersDefaultCellStyle.BackColor = ColorDark;
            dgvUsers.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvUsers.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", 9.5f, FontStyle.Bold);

            dgvUsers.DefaultCellStyle.SelectionBackColor =
                Color.FromArgb(237, 233, 254);
            dgvUsers.DefaultCellStyle.SelectionForeColor = ColorDark;

            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colId",
                HeaderText = "ID",
                FillWeight = 35
            });
            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colUsername",
                HeaderText = "Tên đăng nhập",
                FillWeight = 95
            });
            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colFullName",
                HeaderText = "Họ và tên",
                FillWeight = 125
            });
            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colRole",
                HeaderText = "Vai trò",
                FillWeight = 75
            });
            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colEmail",
                HeaderText = "Email",
                FillWeight = 115
            });
            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPhone",
                HeaderText = "Số ĐT",
                FillWeight = 85
            });
            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPoints",
                HeaderText = "Điểm",
                FillWeight = 55
            });

            dgvUsers.CellClick += DgvUsers_CellClick;
            dgvUsers.CellFormatting += DgvUsers_CellFormatting;

            Controls.Add(dgvUsers);
            Controls.Add(pnlSpacer);
            Controls.Add(pnlForm);
            Controls.Add(pnlTop);
        }

        private Label CreateFieldLabel(string text, int top)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = ColorMuted,
                Location = new Point(20, top),
                AutoSize = true
            };
        }

        private TextBox CreateTextBox(int top, string placeholder)
        {
            return new TextBox
            {
                Location = new Point(20, top),
                Size = new Size(315, 28),
                Font = new Font("Segoe UI", 10),
                PlaceholderText = placeholder
            };
        }

        // =====================================================
        // 2. ĐỒNG BỘ CỘT & TẢI DỮ LIỆU TỪ SQLITE
        // =====================================================

        private void EnsureUsersColumns(SqliteConnection connection)
        {
            var existingCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(\"Users\");";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    existingCols.Add(reader["name"]?.ToString() ?? string.Empty);
                }
            }

            if (!existingCols.Contains("Email"))
                connection.Execute("ALTER TABLE Users ADD COLUMN Email TEXT DEFAULT '';");
            if (!existingCols.Contains("Phone"))
                connection.Execute("ALTER TABLE Users ADD COLUMN Phone TEXT DEFAULT '';");
            if (!existingCols.Contains("RewardPoints"))
                connection.Execute("ALTER TABLE Users ADD COLUMN RewardPoints INTEGER NOT NULL DEFAULT 0;");
            if (!existingCols.Contains("CreatedAt"))
                connection.Execute("ALTER TABLE Users ADD COLUMN CreatedAt TEXT;");
        }

        private void LoadUsersFromDatabase()
        {
            try
            {
                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();

                EnsureUsersColumns(connection);

                _allUsers = connection.Query<User>(@"
                    SELECT 
                        rowid AS Id,
                        Username,
                        PasswordHash,
                        FullName,
                        Role,
                        COALESCE(Email, '') AS Email,
                        COALESCE(Phone, '') AS Phone,
                        COALESCE(RewardPoints, 0) AS RewardPoints,
                        COALESCE(NULLIF(CreatedAt, ''), datetime('now', 'localtime')) AS CreatedAt
                    FROM Users
                    ORDER BY Role ASC, rowid ASC;").ToList();

                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Không thể tải danh sách người dùng:\n{ex.Message}",
                    "Lỗi cơ sở dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ApplyFilter()
        {
            string keyword = (txtSearch.Text ?? string.Empty)
                .Trim()
                .ToLowerInvariant();

            int roleFilterIndex = cboFilterRole.SelectedIndex;

            var filtered = _allUsers.Where(u =>
            {
                bool matchRole = roleFilterIndex switch
                {
                    1 => u.Role == UserRole.Admin,
                    2 => u.Role == UserRole.Staff,
                    3 => u.Role == UserRole.Customer,
                    _ => true
                };

                if (!matchRole)
                    return false;

                if (string.IsNullOrEmpty(keyword))
                    return true;

                return u.Username.ToLowerInvariant().Contains(keyword) ||
                       u.FullName.ToLowerInvariant().Contains(keyword) ||
                       (u.Email ?? "").ToLowerInvariant().Contains(keyword) ||
                       (u.Phone ?? "").ToLowerInvariant().Contains(keyword);
            }).ToList();

            dgvUsers.Rows.Clear();

            foreach (var u in filtered)
            {
                int rowIndex = dgvUsers.Rows.Add(
                    u.Id,
                    u.Username,
                    u.FullName,
                    u.Role.ToString(),
                    string.IsNullOrWhiteSpace(u.Email) ? "-" : u.Email,
                    string.IsNullOrWhiteSpace(u.Phone) ? "-" : u.Phone,
                    u.RewardPoints
                );

                dgvUsers.Rows[rowIndex].Tag = u;
            }

            lblCount.Text =
                $"Hiển thị {filtered.Count} / {_allUsers.Count} tài khoản";
        }

        private void DgvUsers_CellFormatting(
            object? sender,
            DataGridViewCellFormattingEventArgs e)
        {
            if (dgvUsers.Columns[e.ColumnIndex].Name == "colRole" &&
                e.Value is string roleStr)
            {
                e.CellStyle!.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);

                e.CellStyle.ForeColor = roleStr switch
                {
                    "Admin" => Color.FromArgb(124, 58, 237),
                    "Staff" => Color.FromArgb(2, 132, 199),
                    _ => Color.FromArgb(71, 85, 105)
                };
            }
        }

        // =====================================================
        // 3. CHỌN DÒNG & ĐỔ DỮ LIỆU LÊN FORM
        // =====================================================

        private void DgvUsers_CellClick(
            object? sender,
            DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (dgvUsers.Rows[e.RowIndex].Tag is not User user)
                return;

            _selectedUserId = user.Id;

            lblFormHeader.Text = $"✏️ SỬA TÀI KHOẢN #{user.Id}";
            txtUsername.Text = user.Username;
            txtUsername.ReadOnly = true;
            txtUsername.BackColor = Color.FromArgb(241, 245, 249);

            txtPassword.Text = string.Empty;
            txtFullName.Text = user.FullName;
            txtEmail.Text = user.Email;
            txtPhone.Text = user.Phone;

            cboRole.SelectedIndex = user.Role switch
            {
                UserRole.Admin => 0,
                UserRole.Staff => 1,
                _ => 2
            };

            numPoints.Value = Math.Clamp(
                user.RewardPoints,
                (int)numPoints.Minimum,
                (int)numPoints.Maximum);

            btnAdd.Enabled = false;
            btnUpdate.Enabled = true;
            btnDelete.Enabled = true;
        }

        private void ResetForm()
        {
            _selectedUserId = 0;

            lblFormHeader.Text = "➕ THÊM TÀI KHOẢN MỚI";
            txtUsername.Text = string.Empty;
            txtUsername.ReadOnly = false;
            txtUsername.BackColor = Color.White;

            txtPassword.Text = string.Empty;
            txtFullName.Text = string.Empty;
            txtEmail.Text = string.Empty;
            txtPhone.Text = string.Empty;
            cboRole.SelectedIndex = 1;
            numPoints.Value = 0;

            btnAdd.Enabled = true;
            btnUpdate.Enabled = false;
            btnDelete.Enabled = false;

            dgvUsers.ClearSelection();
        }

        private UserRole GetSelectedRole()
        {
            return cboRole.SelectedIndex switch
            {
                0 => UserRole.Admin,
                1 => UserRole.Staff,
                _ => UserRole.Customer
            };
        }

        // =====================================================
        // 4. THÊM / CẬP NHẬT / XÓA TÀI KHOẢN
        // =====================================================

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();
            string fullName = txtFullName.Text.Trim();
            string email = txtEmail.Text.Trim();
            string phone = txtPhone.Text.Trim();
            UserRole role = GetSelectedRole();
            int points = (int)numPoints.Value;

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(fullName))
            {
                MessageBox.Show(
                    "Vui lòng nhập đầy đủ Tên đăng nhập, Mật khẩu và Họ tên!",
                    "Thiếu thông tin",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();

                int exists = connection.ExecuteScalar<int>(
                    @"SELECT COUNT(1) FROM Users 
                      WHERE LOWER(Username) = LOWER(@Username);",
                    new { Username = username });

                if (exists > 0)
                {
                    MessageBox.Show(
                        $"Tên đăng nhập '{username}' đã tồn tại!",
                        "Trùng tên đăng nhập",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    txtUsername.Focus();
                    return;
                }

                connection.Execute(@"
                    INSERT INTO Users 
                        (Username, PasswordHash, FullName, Role, Email, Phone, RewardPoints, CreatedAt)
                    VALUES 
                        (@Username, @PasswordHash, @FullName, @Role, @Email, @Phone, @RewardPoints, @CreatedAt);",
                    new
                    {
                        Username = username,
                        PasswordHash = password,
                        FullName = fullName,
                        Role = (int)role,
                        Email = email,
                        Phone = phone,
                        RewardPoints = points,
                        CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    });

                MessageBox.Show(
                    $"Đã tạo tài khoản '{username}' ({role}) thành công!",
                    "Thành công",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                LoadUsersFromDatabase();
                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi thêm tài khoản:\n{ex.Message}",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            if (_selectedUserId <= 0)
                return;

            string fullName = txtFullName.Text.Trim();
            string newPassword = txtPassword.Text.Trim();
            string email = txtEmail.Text.Trim();
            string phone = txtPhone.Text.Trim();
            UserRole newRole = GetSelectedRole();
            int points = (int)numPoints.Value;

            if (string.IsNullOrWhiteSpace(fullName))
            {
                MessageBox.Show(
                    "Họ và tên không được để trống!",
                    "Thiếu thông tin",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (UserSession.CurrentUser != null &&
                string.Equals(txtUsername.Text.Trim(), UserSession.CurrentUser.Username, StringComparison.OrdinalIgnoreCase) &&
                newRole != UserRole.Admin)
            {
                MessageBox.Show(
                    "Bạn không thể tự hạ quyền Admin của tài khoản đang đăng nhập!",
                    "Thao tác bị chặn",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();

                if (!string.IsNullOrWhiteSpace(newPassword))
                {
                    connection.Execute(@"
                        UPDATE Users
                        SET FullName = @FullName,
                            PasswordHash = @PasswordHash,
                            Role = @Role,
                            Email = @Email,
                            Phone = @Phone,
                            RewardPoints = @RewardPoints
                        WHERE rowid = @Id;",
                        new
                        {
                            Id = _selectedUserId,
                            FullName = fullName,
                            PasswordHash = newPassword,
                            Role = (int)newRole,
                            Email = email,
                            Phone = phone,
                            RewardPoints = points
                        });
                }
                else
                {
                    connection.Execute(@"
                        UPDATE Users
                        SET FullName = @FullName,
                            Role = @Role,
                            Email = @Email,
                            Phone = @Phone,
                            RewardPoints = @RewardPoints
                        WHERE rowid = @Id;",
                        new
                        {
                            Id = _selectedUserId,
                            FullName = fullName,
                            Role = (int)newRole,
                            Email = email,
                            Phone = phone,
                            RewardPoints = points
                        });
                }

                MessageBox.Show(
                    "Đã cập nhật thông tin tài khoản thành công!",
                    "Thành công",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                LoadUsersFromDatabase();
                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi cập nhật tài khoản:\n{ex.Message}",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (_selectedUserId <= 0)
                return;

            if (UserSession.CurrentUser != null &&
                string.Equals(txtUsername.Text.Trim(), UserSession.CurrentUser.Username, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    "Bạn không thể xóa chính tài khoản Admin đang đăng nhập!",
                    "Thao tác bị chặn",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string username = txtUsername.Text;

            if (MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa tài khoản '{username}' không?\nThao tác này không thể hoàn tác.",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                using var connection = new SqliteConnection(ConnectionString);
                connection.Open();

                connection.Execute(
                    "DELETE FROM Users WHERE rowid = @Id;",
                    new { Id = _selectedUserId });

                MessageBox.Show(
                    $"Đã xóa tài khoản '{username}'.",
                    "Đã xóa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                LoadUsersFromDatabase();
                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi xóa tài khoản:\n{ex.Message}",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}