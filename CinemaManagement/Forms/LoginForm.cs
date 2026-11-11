using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CinemaManagement.Models;
using CinemaManagement.Models.Enums;
using CinemaManagement.Services;
using CinemaManagement.Utils;

namespace CinemaManagement;

public partial class LoginForm : Form
{
    private bool isPasswordHidden = true;
    private bool isRegisterMode = false;

    // Controls bổ sung khi chuyển sang chế độ Đăng ký (Sign Up)
    private Label? lblFullName;
    private Panel? pnlFullNameBox;
    private TextBox? txtFullName;

    private readonly AuthService _authService = new();

    // Sự kiện đẩy ra bên ngoài khi đăng nhập hoặc đăng ký thành công
    public event EventHandler<User>? OnLoginSuccess;
    public event EventHandler<User>? OnRegisterSuccess;

    public LoginForm()
    {
        InitializeComponent();
        InitRegisterControls();
        ApplyCustomStyling();
        EnableFormDrag();

        // Nhấn Enter để đăng nhập / đăng ký nhanh
        txtUsername.KeyDown += OnEnterSubmit;
        txtPassword.KeyDown += OnEnterSubmit;
    }

    private void InitRegisterControls()
    {
        lblFullName = new Label
        {
            Text = "Full Name",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(55, 130),
            Size = new Size(390, 22),
            Visible = false
        };

        pnlFullNameBox = new Panel
        {
            BackColor = Color.White,
            Location = new Point(55, 154),
            Size = new Size(390, 42),
            Visible = false
        };

        txtFullName = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 11F),
            ForeColor = Color.FromArgb(50, 50, 50),
            Location = new Point(15, 11),
            Size = new Size(360, 20),
            PlaceholderText = "Enter your full name"
        };
        txtFullName.KeyDown += OnEnterSubmit;

        pnlFullNameBox.Controls.Add(txtFullName);
        pnlRightCard.Controls.Add(lblFullName);
        pnlRightCard.Controls.Add(pnlFullNameBox);
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
        if (pnlFullNameBox != null)
        {
            pnlFullNameBox.Paint += (s, e) => DrawRoundedBorder(e.Graphics, pnlFullNameBox.ClientRectangle, 16);
        }

        // Bo tròn nút Login
        btnLogin.Paint += (s, e) =>
        {
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

        Action<Control> attachEvents = (ctrl) =>
        {
            ctrl.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    dragging = true;
                    dragCursorPoint = Cursor.Position;
                    dragFormPoint = this.Location;
                }
            };
            ctrl.MouseMove += (s, e) =>
            {
                if (dragging)
                {
                    Point diff = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
                    this.Location = Point.Add(dragFormPoint, new Size(diff));
                }
            };
            ctrl.MouseUp += (s, e) => dragging = false;
        };

        if (pnlLeft != null) attachEvents(pnlLeft);
        if (pnlRightCard != null) attachEvents(pnlRightCard);
    }

    // Ẩn / hiện mật khẩu
    private void lblToggleEye_Click(object? sender, EventArgs e)
    {
        isPasswordHidden = !isPasswordHidden;
        txtPassword.PasswordChar = isPasswordHidden ? '●' : '\0';
        lblToggleEye.Text = isPasswordHidden ? "👁" : "🙈";
    }

    // Nút Đăng nhập hoặc Đăng ký
    private void btnLogin_Click(object? sender, EventArgs e)
    {
        if (isRegisterMode)
        {
            HandleRegister();
        }
        else
        {
            HandleLogin();
        }
    }

    private void HandleLogin()
    {
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            MessageBox.Show("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var user = _authService.Login(username, password);
        if (user == null)
        {
            MessageBox.Show("Tên đăng nhập hoặc mật khẩu không chính xác!", "Lỗi đăng nhập", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        UserSession.Login(user);
        OnLoginSuccess?.Invoke(this, user);

        MessageBox.Show($"Đăng nhập thành công!\nXin chào: {user.FullName} ({user.Role})", "Cineworld Login", MessageBoxButtons.OK, MessageBoxIcon.Information);

        this.Hide();

        // Khởi tạo MainForm và truyền vai trò vào
        MainForm mainForm = new MainForm(user.Role);
        mainForm.FormClosed += (s, args) => this.Close();
        mainForm.Show();
    }

    private void HandleRegister()
    {
        string fullName = txtFullName?.Text.Trim() ?? "";
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text.Trim();

        if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            MessageBox.Show("Vui lòng nhập đầy đủ Họ tên, Tên đăng nhập và Mật khẩu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var (success, message, createdUser) = _authService.Register(username, password, fullName);
        if (!success || createdUser == null)
        {
            MessageBox.Show(message, "Đăng ký tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        OnRegisterSuccess?.Invoke(this, createdUser);
        UserSession.CurrentUser = createdUser;

        var result = MessageBox.Show(
            $"🎉 {message}\n\nBạn có muốn đăng nhập thẳng vào hệ thống đặt vé ngay bây giờ không?",
            "Đăng ký thành công",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            this.Hide();
            MainForm mainForm = new MainForm(UserRole.Customer);
            mainForm.FormClosed += (s, args) => this.Close();
            mainForm.Show();
        }
        else
        {
            ToggleRegisterMode(false);
        }
    }

    // Quên mật khẩu
    private void lnkForgotPassword_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        string username = txtUsername.Text.Trim();
        string prompt = string.IsNullOrEmpty(username) ? "tài khoản của bạn" : $"tài khoản '{username}'";

        MessageBox.Show($"Hệ thống khôi phục mật khẩu:\n\nLink đặt lại mật khẩu đã được gửi đến Email đăng ký của {prompt}.\nVui lòng kiểm tra hộp thư!",
            "Khôi phục mật khẩu", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // Tạo tài khoản (Sign Up) / Chuyển đổi qua lại với Login
    private void lnkRegister_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        ToggleRegisterMode(!isRegisterMode);
    }

    private void ToggleRegisterMode(bool registerMode)
    {
        isRegisterMode = registerMode;

        if (isRegisterMode)
        {
            lblTitle.Text = "Sign Up";
            lblSubtitle.Text = "Create your Cineworld member account";

            if (lblFullName != null) lblFullName.Visible = true;
            if (pnlFullNameBox != null) pnlFullNameBox.Visible = true;

            lblUsername.Location = new Point(55, 206);
            pnlUsernameBox.Location = new Point(55, 230);

            lblPassword.Location = new Point(55, 282);
            pnlPasswordBox.Location = new Point(55, 306);

            lnkForgotPassword.Visible = false;

            btnLogin.Text = "Create Account";
            btnLogin.Location = new Point(55, 372);

            lblNoAccount.Text = "Already have an account?";
            lnkRegister.Text = "Login";
        }
        else
        {
            lblTitle.Text = "Login Form";
            lblSubtitle.Text = "Please enter your details";

            if (lblFullName != null) lblFullName.Visible = false;
            if (pnlFullNameBox != null) pnlFullNameBox.Visible = false;

            lblUsername.Location = new Point(55, 155);
            pnlUsernameBox.Location = new Point(55, 180);

            lblPassword.Location = new Point(55, 235);
            pnlPasswordBox.Location = new Point(55, 260);

            lnkForgotPassword.Visible = true;

            btnLogin.Text = "Login";
            btnLogin.Location = new Point(55, 365);

            lblNoAccount.Text = "Don't have an account?";
            lnkRegister.Text = "Sign Up";
        }
    }

    // Nút tắt Form
    private void lblClose_Click(object? sender, EventArgs e)
    {
        Application.Exit();
    }
}
