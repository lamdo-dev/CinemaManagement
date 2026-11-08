using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CinemaManagement.Models.Enums;

namespace CinemaManagement;

public partial class LoginForm : Form
{
    private bool isPasswordHidden = true;

    public LoginForm()
    {
        InitializeComponent();
        ApplyCustomStyling();
        EnableFormDrag();

        // Nhấn Enter để đăng nhập nhanh
        txtUsername.KeyDown += OnEnterSubmit;
        txtPassword.KeyDown += OnEnterSubmit;
    }

    private void OnEnterSubmit(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            btnLogin.PerformClick();
            e.SuppressKeyPress = true;
        }
    }

    private void ApplyCustomStyling()
    {
        // Bo góc các input textbox và button Login
        pnlUsernameBox.Paint += (s, e) => DrawRoundedBorder(e.Graphics, pnlUsernameBox.ClientRectangle, 16);
        pnlPasswordBox.Paint += (s, e) => DrawRoundedBorder(e.Graphics, pnlPasswordBox.ClientRectangle, 16);
        
        // Bo tròn nút Login
        btnLogin.Paint += (s, e) => {
            using var path = GetRoundedPath(btnLogin.ClientRectangle, 18);
            btnLogin.Region = new Region(path);
        };
    }

    private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        float d = radius * 2F;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private void DrawRoundedBorder(Graphics g, Rectangle rect, int radius)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = GetRoundedPath(new Rectangle(rect.X, rect.Y, rect.Width - 1, rect.Height - 1), radius);
        using var brush = new SolidBrush(Color.White);
        g.FillPath(brush, path);
    }

    // Cho phép nhấp giữ chuột để kéo di chuyển cửa sổ
    private void EnableFormDrag()
    {
        bool dragging = false;
        Point dragCursorPoint = Point.Empty;
        Point dragFormPoint = Point.Empty;

        Action<Control> attachEvents = (ctrl) => {
            ctrl.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    dragging = true;
                    dragCursorPoint = Cursor.Position;
                    dragFormPoint = this.Location;
                }
            };
            ctrl.MouseMove += (s, e) => {
                if (dragging)
                {
                    Point diff = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
                    this.Location = Point.Add(dragFormPoint, new Size(diff));
                }
            };
            ctrl.MouseUp += (s, e) => dragging = false;
        };

        attachEvents(pnlLeft);
        attachEvents(pnlRightCard);
    }

    // Ẩn / hiện mật khẩu
    private void lblToggleEye_Click(object? sender, EventArgs e)
    {
        isPasswordHidden = !isPasswordHidden;
        txtPassword.PasswordChar = isPasswordHidden ? '●' : '\0';
        lblToggleEye.Text = isPasswordHidden ? "👁" : "🙈";
    }

    // Nút đăng nhập
    private void btnLogin_Click(object? sender, EventArgs e)
    {
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            MessageBox.Show("Please enter both username and password!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Tự động phân quyền test
        UserRole userRole = username.ToLower() switch
        {
            "admin" => UserRole.Admin,
            "staff" => UserRole.Staff,
            _ => UserRole.Customer
        };

        MessageBox.Show($"Đăng nhập thành công! Quyền: {userRole}", "Cineworld Login", MessageBoxButtons.OK, MessageBoxIcon.Information);

        this.Hide();
        var mainForm = new MainForm(userRole);
        mainForm.FormClosed += (s, args) => this.Close();
        mainForm.Show();
    }

    // Quên mật khẩu
    private void lnkForgotPassword_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        string username = txtUsername.Text.Trim();
        string prompt = string.IsNullOrEmpty(username) ? "tài khoản của bạn" : $"tài khoản '{username}'";
        
        MessageBox.Show($"Hệ thống khôi phục mật khẩu:\n\nLink đặt lại mật khẩu đã được gửi đến Email đăng ký của {prompt}.\nVui lòng kiểm tra hộp thư!", 
            "Khôi phục mật khẩu", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // Tạo tài khoản (Sign Up)
    private void lnkRegister_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        MessageBox.Show("Mở màn hình Đăng ký tài khoản thành viên Cineworld mới.\n(Điền Họ tên, Số điện thoại, Email và Mật khẩu)", 
            "Đăng ký tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // Nút tắt Form
    private void lblClose_Click(object? sender, EventArgs e)
    {
        Application.Exit();
    }
}