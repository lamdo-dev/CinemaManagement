using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CinemaManagement.Models;
using CinemaManagement.Models.Enums;
using CinemaManagement.Services;
using CinemaManagement.Utils;

namespace CinemaManagement;

public partial class LoginForm : Form
{

    private enum FormMode
    {
        Login,
        Register,
        ForgotPasswordStep1,
        ForgotPasswordStep2
    }

    private readonly AuthService _authService = new();
    public event EventHandler<User>? OnLoginSuccess;
    public event EventHandler<User>? OnRegisterSuccess;

    private FormMode _currentMode = FormMode.Login;
    private bool _isPasswordHidden = true;
    private string _generatedOtp = "";
    private string _recoveryEmail = "";

    private readonly Color _colCardBg           = Color.FromArgb(255, 255, 255);
    private readonly Color _colOceanBlue        = Color.FromArgb(0, 168, 255);
    private readonly Color _colOceanBlueHover   = Color.FromArgb(51, 185, 255);
    private readonly Color _colDeepOcean        = Color.FromArgb(0, 122, 237);
    private readonly Color _colInputBg          = Color.FromArgb(244, 248, 253);
    private readonly Color _colInputBorder      = Color.FromArgb(220, 233, 247);
    private readonly Color _colInputFocusBorder = Color.FromArgb(0, 168, 255);
    private readonly Color _colTextDark         = Color.FromArgb(30, 48, 72);
    private readonly Color _colTextMuted        = Color.FromArgb(120, 140, 170);

    private Panel pnlLeft = null!;
    private Panel pnlRight = null!;
    private Label lblClose = null!;

    private Label lblTitle = null!;
    private Label lblSubtitle = null!;

    private Panel pnlFullName = null!;
    private TextBox txtFullName = null!;
    private Label lblFullNameTitle = null!;

    private Panel pnlEmail = null!;
    private TextBox txtEmail = null!;
    private Label lblEmailTitle = null!;

    private Panel pnlUsername = null!;
    private TextBox txtUsername = null!;
    private Label lblUsernameTitle = null!;

    private Panel pnlPassword = null!;
    private TextBox txtPassword = null!;
    private Label lblPasswordTitle = null!;
    private Label lblToggleEye = null!;

    private Panel pnlOtp = null!;
    private TextBox txtOtp = null!;
    private Label lblOtpTitle = null!;

    private Panel pnlNewPassword = null!;
    private TextBox txtNewPassword = null!;
    private Label lblNewPasswordTitle = null!;

    private Panel pnlConfirmPassword = null!;
    private TextBox txtConfirmPassword = null!;
    private Label lblConfirmPasswordTitle = null!;

    private LinkLabel lnkForgotPassword = null!;
    private Button btnAction = null!;
    private Panel pnlSwitchMode = null!;
    private Label lblSwitchPrompt = null!;
    private LinkLabel lnkSwitchAction = null!;

    private readonly Random _rand = new();

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


        SetStyle(ControlStyles.AllPaintingInWmPaint | 
                 ControlStyles.OptimizedDoubleBuffer | 
                 ControlStyles.UserPaint | 
                 ControlStyles.ResizeRedraw, true);

        BuildOceanCuteLayout();
        EnableSmoothFormDrag();
        SwitchViewMode(FormMode.Login);
    }

    #region Layout Construction & Theming

    private void BuildOceanCuteLayout()

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
        this.Size = new Size(980, 590);
        this.Region = new Region(GetRoundedPath(new Rectangle(0, 0, this.Width, this.Height), 26));

        pnlLeft = new Panel
        {
            Dock = DockStyle.Left,
            Width = 430
        };

        try
        {
            string cinemaImgUrl = "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?q=80&w=800&auto=format&fit=crop";
            using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var stream = client.GetStreamAsync(cinemaImgUrl).GetAwaiter().GetResult();
            pnlLeft.BackgroundImage = Image.FromStream(stream);
            pnlLeft.BackgroundImageLayout = ImageLayout.Stretch;
        }
        catch
        {
        }

        pnlLeft.Paint += RenderLeftCinemaBanner;

        pnlRight = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = _colCardBg,
            Padding = new Padding(50, 30, 50, 30)
        };
        pnlRight.Paint += RenderRightPanelBorder;

        lblClose = new Label
        {
            Text = "✕",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(170, 185, 205),
            Size = new Size(32, 32),
            Location = new Point(pnlRight.Width - 45, 15),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter
        };
        lblClose.MouseEnter += (s, e) => { lblClose.ForeColor = _colOceanBlue; };
        lblClose.MouseLeave += (s, e) => { lblClose.ForeColor = Color.FromArgb(170, 185, 205); };
        lblClose.Click += (s, e) => Application.Exit();
        pnlRight.Controls.Add(lblClose);

        lblTitle = new Label
        {
            Text = "WELCOME BACK ✨",
            Font = new Font("Segoe UI", 21F, FontStyle.Bold),
            ForeColor = _colTextDark,
            Location = new Point(50, 32),
            Size = new Size(440, 38),
            BackColor = Color.Transparent
        };

        lblSubtitle = new Label
        {
            Text = "Ready for an exciting movie journey? 🍿",
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = _colTextMuted,
            Location = new Point(52, 72),
            Size = new Size(440, 24),
            BackColor = Color.Transparent
        };

        pnlRight.Controls.Add(lblTitle);
        pnlRight.Controls.Add(lblSubtitle);

        (lblFullNameTitle, pnlFullName, txtFullName) = CreateInputGroup("HỌ VÀ TÊN", "Nhập họ và tên của bạn...", "👤");
        pnlRight.Controls.Add(lblFullNameTitle);
        pnlRight.Controls.Add(pnlFullName);

        (lblEmailTitle, pnlEmail, txtEmail) = CreateInputGroup("EMAIL ĐĂNG KÝ", "example@domain.com", "✉");
        pnlRight.Controls.Add(lblEmailTitle);
        pnlRight.Controls.Add(pnlEmail);

        (lblUsernameTitle, pnlUsername, txtUsername) = CreateInputGroup("TÊN ĐĂNG NHẬP", "Tên tài khoản của bạn", "★");
        pnlRight.Controls.Add(lblUsernameTitle);
        pnlRight.Controls.Add(pnlUsername);

        (lblPasswordTitle, pnlPassword, txtPassword) = CreateInputGroup("MẬT KHẨU", "Mật khẩu bảo mật", "🔒");
        txtPassword.PasswordChar = '●';

        lblToggleEye = new Label
        {
            Text = "👁",
            Font = new Font("Segoe UI Emoji", 11.5F),
            ForeColor = _colTextMuted,
            Size = new Size(32, 32),
            Location = new Point(pnlPassword.Width - 38, 5),
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };
        lblToggleEye.Click += TogglePasswordVisibility;
        pnlPassword.Controls.Add(lblToggleEye);

        pnlRight.Controls.Add(lblPasswordTitle);
        pnlRight.Controls.Add(pnlPassword);

        (lblOtpTitle, pnlOtp, txtOtp) = CreateInputGroup("MÃ OTP (6 CHỮ SỐ)", "Nhập mã OTP gồm 6 chữ số", "🔑");
        pnlRight.Controls.Add(lblOtpTitle);
        pnlRight.Controls.Add(pnlOtp);

        (lblNewPasswordTitle, pnlNewPassword, txtNewPassword) = CreateInputGroup("MẬT KHẨU MỚI", "Nhập mật khẩu mới của bạn", "🔒");
        txtNewPassword.PasswordChar = '●';
        pnlRight.Controls.Add(lblNewPasswordTitle);
        pnlRight.Controls.Add(pnlNewPassword);

        (lblConfirmPasswordTitle, pnlConfirmPassword, txtConfirmPassword) = CreateInputGroup("XÁC NHẬN MẬT KHẨU MỚI", "Nhập lại mật khẩu mới", "🛡");
        txtConfirmPassword.PasswordChar = '●';
        pnlRight.Controls.Add(lblConfirmPasswordTitle);
        pnlRight.Controls.Add(pnlConfirmPassword);

        lnkForgotPassword = new LinkLabel
        {
            Text = "Quên mật khẩu?",
            Font = new Font("Segoe UI", 9.25F),
            LinkColor = _colOceanBlue,
            ActiveLinkColor = _colDeepOcean,
            VisitedLinkColor = _colOceanBlue,
            Size = new Size(160, 22),
            TextAlign = ContentAlignment.MiddleRight,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand,
            LinkBehavior = LinkBehavior.HoverUnderline
        };
        lnkForgotPassword.LinkClicked += (s, e) => SwitchViewMode(FormMode.ForgotPasswordStep1);
        pnlRight.Controls.Add(lnkForgotPassword);

        btnAction = new Button
        {
            Text = "ĐĂNG NHẬP NGAY",
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = _colOceanBlue,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Size = new Size(420, 46)
        };
        btnAction.FlatAppearance.BorderSize = 0;
        btnAction.Paint += RenderOceanButton;
        btnAction.MouseEnter += (s, e) => { btnAction.BackColor = _colOceanBlueHover; btnAction.Invalidate(); };
        btnAction.MouseLeave += (s, e) => { btnAction.BackColor = _colOceanBlue; btnAction.Invalidate(); };
        btnAction.Click += btnAction_Click;
        pnlRight.Controls.Add(btnAction);

        pnlSwitchMode = new Panel
        {
            Size = new Size(420, 30),
            BackColor = Color.Transparent
        };

        lblSwitchPrompt = new Label
        {
            Text = "Bạn chưa có tài khoản?",
            Font = new Font("Segoe UI", 9.25F),
            ForeColor = _colTextMuted,
            AutoSize = false,
            Size = new Size(180, 24),
            TextAlign = ContentAlignment.MiddleRight,
            Location = new Point(20, 3)
        };

        lnkSwitchAction = new LinkLabel
        {
            Text = "Đăng ký thành viên",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            LinkColor = _colOceanBlue,
            ActiveLinkColor = _colDeepOcean,
            VisitedLinkColor = _colOceanBlue,
            AutoSize = false,
            Size = new Size(190, 24),
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(205, 2),
            Cursor = Cursors.Hand,
            LinkBehavior = LinkBehavior.HoverUnderline
        };
        lnkSwitchAction.LinkClicked += (s, e) =>
        {
            if (_currentMode == FormMode.Login)
                SwitchViewMode(FormMode.Register);
            else
                SwitchViewMode(FormMode.Login);
        };

        pnlSwitchMode.Controls.Add(lblSwitchPrompt);
        pnlSwitchMode.Controls.Add(lnkSwitchAction);
        pnlRight.Controls.Add(pnlSwitchMode);

        txtFullName.KeyDown += OnKeyDownSubmit;
        txtEmail.KeyDown += OnKeyDownSubmit;
        txtUsername.KeyDown += OnKeyDownSubmit;
        txtPassword.KeyDown += OnKeyDownSubmit;
        txtOtp.KeyDown += OnKeyDownSubmit;
        txtNewPassword.KeyDown += OnKeyDownSubmit;
        txtConfirmPassword.KeyDown += OnKeyDownSubmit;

        this.Controls.Add(pnlRight);
        this.Controls.Add(pnlLeft);
    }

    private (Label lblTitle, Panel pnlBox, TextBox txtField) CreateInputGroup(string title, string placeholder, string icon)
    {

        var lbl = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 8.25F, FontStyle.Bold),
            ForeColor = Color.FromArgb(90, 115, 145),
            Size = new Size(420, 18),
            BackColor = Color.Transparent
        };

        var pnl = new Panel
        {
            Size = new Size(420, 42),
            BackColor = _colInputBg
        };

        var lblIcon = new Label
        {
            Text = icon,
            Font = new Font("Segoe UI", 11F),
            ForeColor = _colOceanBlue,
            Size = new Size(30, 30),
            Location = new Point(12, 6),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        var txt = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 10.5F),
            ForeColor = _colTextDark,
            BackColor = _colInputBg,
            Location = new Point(48, 11),
            Size = new Size(325, 20),
            PlaceholderText = placeholder
        };

        bool isFocused = false;
        txt.GotFocus += (s, e) => { isFocused = true; pnl.Invalidate(); };
        txt.LostFocus += (s, e) => { isFocused = false; pnl.Invalidate(); };

        pnl.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);
            using var path = GetRoundedPath(rect, 12);

            using var bgBrush = new SolidBrush(_colInputBg);
            e.Graphics.FillPath(bgBrush, path);

            using var pen = new Pen(isFocused ? _colInputFocusBorder : _colInputBorder, isFocused ? 1.8F : 1.0F);
            e.Graphics.DrawPath(pen, path);

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

        pnl.Controls.Add(lblIcon);
        pnl.Controls.Add(txt);

        return (lbl, pnl, txt);
    }

    #endregion

    #region Layout Dynamic Modes & Transitions

    private void HideAllInputs()
    {
        lblFullNameTitle.Visible = pnlFullName.Visible = false;
        lblEmailTitle.Visible = pnlEmail.Visible = false;
        lblUsernameTitle.Visible = pnlUsername.Visible = false;
        lblPasswordTitle.Visible = pnlPassword.Visible = false;
        lblOtpTitle.Visible = pnlOtp.Visible = false;
        lblNewPasswordTitle.Visible = pnlNewPassword.Visible = false;
        lblConfirmPasswordTitle.Visible = pnlConfirmPassword.Visible = false;
        lnkForgotPassword.Visible = false;
    }

    private void SwitchViewMode(FormMode targetMode)
    {
        _currentMode = targetMode;
        HideAllInputs();

        switch (_currentMode)
        {
            case FormMode.Login:
            {
                lblTitle.Text = "WELCOME BACK ✨";
                lblSubtitle.Text = "Ready for an exciting movie journey? 🍿";

                int startY = 150;
                lblUsernameTitle.Visible = pnlUsername.Visible = true;
                lblUsernameTitle.Location = new Point(50, startY);
                pnlUsername.Location = new Point(50, startY + 22);

                lblPasswordTitle.Visible = pnlPassword.Visible = true;
                lblPasswordTitle.Location = new Point(50, startY + 82);
                pnlPassword.Location = new Point(50, startY + 104);

                lnkForgotPassword.Visible = true;
                lnkForgotPassword.Location = new Point(300, startY + 154);

                btnAction.Text = "ĐĂNG NHẬP NGAY";
                btnAction.Location = new Point(50, startY + 188);

                pnlSwitchMode.Location = new Point(50, startY + 252);
                lblSwitchPrompt.Text = "Bạn chưa có tài khoản?";
                lnkSwitchAction.Text = "Đăng ký thành viên";
                break;
            }

            case FormMode.Register:
            {
                lblTitle.Text = "TẠO TÀI KHOẢN MỚI ✨";
                lblSubtitle.Text = "Tham gia Cineworld VIP để nhận hàng ngàn vé ưu đãi!";

                int startY = 120;
                lblFullNameTitle.Visible = pnlFullName.Visible = true;
                lblFullNameTitle.Location = new Point(50, startY);
                pnlFullName.Location = new Point(50, startY + 22);

                lblUsernameTitle.Visible = pnlUsername.Visible = true;
                lblUsernameTitle.Location = new Point(50, startY + 74);
                pnlUsername.Location = new Point(50, startY + 96);

                lblPasswordTitle.Visible = pnlPassword.Visible = true;
                lblPasswordTitle.Location = new Point(50, startY + 148);
                pnlPassword.Location = new Point(50, startY + 170);

                btnAction.Text = "TẠO TÀI KHOẢN VIP";
                btnAction.Location = new Point(50, startY + 230);

                pnlSwitchMode.Location = new Point(50, startY + 290);
                lblSwitchPrompt.Text = "Đã có tài khoản rồi?";
                lnkSwitchAction.Text = "Đăng nhập ngay";
                break;
            }

            case FormMode.ForgotPasswordStep1:
            {
                lblTitle.Text = "KHÔI PHỤC MẬT KHẨU ✉";
                lblSubtitle.Text = "Nhập email của bạn để nhận mã xác minh OTP tức thì.";

                int startY = 160;
                lblEmailTitle.Visible = pnlEmail.Visible = true;
                lblEmailTitle.Location = new Point(50, startY);
                pnlEmail.Location = new Point(50, startY + 24);

                btnAction.Text = "GỬI MÃ XÁC NHẬN";
                btnAction.Location = new Point(50, startY + 95);

                pnlSwitchMode.Location = new Point(50, startY + 160);
                lblSwitchPrompt.Text = "Đã nhớ mật khẩu?";
                lnkSwitchAction.Text = "Quay về Đăng nhập";
                break;
            }

            case FormMode.ForgotPasswordStep2:
            {
                lblTitle.Text = "ĐẶT LẠI MẬT KHẨU 🔑";
                lblSubtitle.Text = $"Nhập mã OTP đã gửi tới {_recoveryEmail}";

                int startY = 110;
                lblOtpTitle.Visible = pnlOtp.Visible = true;
                lblOtpTitle.Location = new Point(50, startY);
                pnlOtp.Location = new Point(50, startY + 20);

                lblNewPasswordTitle.Visible = pnlNewPassword.Visible = true;
                lblNewPasswordTitle.Location = new Point(50, startY + 70);
                pnlNewPassword.Location = new Point(50, startY + 90);

                lblConfirmPasswordTitle.Visible = pnlConfirmPassword.Visible = true;
                lblConfirmPasswordTitle.Location = new Point(50, startY + 140);
                pnlConfirmPassword.Location = new Point(50, startY + 160);

                btnAction.Text = "XÁC NHẬN ĐỔI MẬT KHẨU";
                btnAction.Location = new Point(50, startY + 220);

                pnlSwitchMode.Location = new Point(50, startY + 280);
                lblSwitchPrompt.Text = "Chưa nhận được mã?";
                lnkSwitchAction.Text = "Gửi lại OTP";
                break;
            }
        }

        pnlRight.Invalidate();
    }

    #endregion

    #region Left Panel Graphics

    private void RenderLeftCinemaBanner(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (var overlayBrush = new LinearGradientBrush(
            pnlLeft.ClientRectangle,
            Color.FromArgb(90, 0, 140, 240),
            Color.FromArgb(135, 0, 70, 170),
            90F))
        {
            g.FillRectangle(overlayBrush, pnlLeft.ClientRectangle);
        }

        using (var darkBackdrop = new SolidBrush(Color.FromArgb(55, 0, 0, 0)))
        {
            g.FillRectangle(darkBackdrop, 25, 140, pnlLeft.Width - 50, 260);
        }

        using var fontCinema = new Font("Segoe UI Emoji", 42F);
        using var brushWhite = new SolidBrush(Color.White);
        string icons = "🍿  🎬  🐳";
        var sizeIcons = g.MeasureString(icons, fontCinema);
        g.DrawString(icons, fontCinema, brushWhite, (pnlLeft.Width - sizeIcons.Width) / 2, 160);

        using var fontBrand = new Font("Segoe UI", 32F, FontStyle.Bold);
        string brand = "CINEWORLD";
        var sizeBrand = g.MeasureString(brand, fontBrand);
        float brandX = (pnlLeft.Width - sizeBrand.Width) / 2;
        float brandY = 270;

        using (var shadowBrush = new SolidBrush(Color.FromArgb(170, 0, 0, 0)))
        {
            g.DrawString(brand, fontBrand, shadowBrush, brandX + 2, brandY + 2);
        }
        g.DrawString(brand, fontBrand, Brushes.White, brandX, brandY);

        using var fontTagline = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        using var brushTagline = new SolidBrush(Color.FromArgb(245, 250, 255));
        string tagline = "FRESH & COZY CINEMA EXPERIENCE";
        var sizeTag = g.MeasureString(tagline, fontTagline);
        g.DrawString(tagline, fontTagline, brushTagline, (pnlLeft.Width - sizeTag.Width) / 2, 335);

        using var pPen = new Pen(Color.FromArgb(200, 255, 255, 255), 2F);
        g.DrawLine(pPen, 80, 375, 350, 375);
    }

    private void RenderRightPanelBorder(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using var borderPen = new Pen(Color.FromArgb(235, 242, 250), 1.0F);
        g.DrawLine(borderPen, 0, 0, 0, pnlRight.Height);
    }

    private void RenderOceanButton(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, btnAction.Width - 1, btnAction.Height - 1);

        using var path = GetRoundedPath(rect, 14);
        btnAction.Region = new Region(path);

        using var brush = new LinearGradientBrush(rect, 
            btnAction.BackColor, _colDeepOcean, 45F);
        g.FillPath(brush, path);

        using var textBrush = new SolidBrush(Color.White);
        using var sf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString(btnAction.Text, btnAction.Font, textBrush, rect, sf);
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

    #endregion

    #region Interaction & Form Dragging

    private void EnableSmoothFormDrag()
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


        attachEvents(pnlLeft);
        attachEvents(pnlRight);
        attachEvents(lblTitle);
        attachEvents(lblSubtitle);

        if (pnlLeft != null) attachEvents(pnlLeft);
        if (pnlRightCard != null) attachEvents(pnlRightCard);

    }

    private void TogglePasswordVisibility(object? sender, EventArgs e)
    {
        _isPasswordHidden = !_isPasswordHidden;
        txtPassword.PasswordChar = _isPasswordHidden ? '●' : '\0';
        lblToggleEye.Text = _isPasswordHidden ? "👁" : "🙈";
    }

    private void OnKeyDownSubmit(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            btnAction.PerformClick();
            e.SuppressKeyPress = true;
        }
    }


    #endregion

    #region Business Authentication Logic

    private void btnAction_Click(object? sender, EventArgs e)
    {
        switch (_currentMode)
        {
            case FormMode.Login:
                HandleLogin();
                break;
            case FormMode.Register:
                HandleRegister();
                break;
            case FormMode.ForgotPasswordStep1:
                HandleSendOtp();
                break;
            case FormMode.ForgotPasswordStep2:
                HandleResetPasswordConfirm();
                break;
        }
    }

    private void HandleLogin()

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

            MessageBox.Show("Bạn ơi, hãy nhập đầy đủ Tên đăng nhập và Mật khẩu nhé! ✨", 
                "Nhắc nhở nhẹ", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            MessageBox.Show("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            return;
        }

        var user = _authService.Login(username, password);
        if (user == null)
        {

            MessageBox.Show("Tên đăng nhập hoặc mật khẩu chưa đúng rồi, bạn thử lại nhé! ❌", 
                "Lỗi đăng nhập", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        UserSession.CurrentUser = user;
        OnLoginSuccess?.Invoke(this, user);

        MessageBox.Show($"Chào mừng trở lại rạp Cineworld!\nChúc {user.FullName} có trải nghiệm xem phim thật tuyệt vời! 🍿🎉", 
            "Đăng nhập thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

        this.Hide();


            MessageBox.Show("Tên đăng nhập hoặc mật khẩu không chính xác!", "Lỗi đăng nhập", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        UserSession.CurrentUser = user;
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
        string fullName = txtFullName.Text.Trim();
        string username = txtUsername.Text.Trim();

        string password = txtPassword.Text.Trim();

        if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            MessageBox.Show("Vui lòng điền đầy đủ Họ tên, Tên đăng nhập và Mật khẩu nhé! ✨", 
                "Nhắc nhở nhẹ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            $"🎉 {message}\n\nBạn có muốn vào thẳng hệ thống đặt vé xem phim ngay bây giờ không? 🍿",
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
            SwitchViewMode(FormMode.Login);
        }
    }

    private void HandleSendOtp()
    {
        string email = txtEmail.Text.Trim();

        if (string.IsNullOrEmpty(email))
        {
            MessageBox.Show("Bạn vui lòng điền địa chỉ Email để nhận mã OTP nhé! ✉", 
                "Nhập Email", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtEmail.Focus();
            return;
        }

        string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        if (!Regex.IsMatch(email, emailPattern))
        {
            MessageBox.Show("Email có vẻ chưa đúng định dạng rồi, bạn kiểm tra lại nhé (vd: tenban@gmail.com)!", 
                "Lỗi định dạng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtEmail.Focus();
            return;
        }

        _recoveryEmail = email;
        _generatedOtp = _rand.Next(100000, 999999).ToString();

        MessageBox.Show($"Mã xác thực đã được gửi tới:\n📧 {_recoveryEmail}\n\nMã OTP của bạn là: {_generatedOtp} ✨\n(Hiệu lực trong vòng 5 phút).",
            "Gửi mã thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

        txtOtp.Clear();
        txtNewPassword.Clear();
        txtConfirmPassword.Clear();
        SwitchViewMode(FormMode.ForgotPasswordStep2);

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

    private void HandleResetPasswordConfirm()
    {
        string enteredOtp = txtOtp.Text.Trim();
        string newPass = txtNewPassword.Text.Trim();
        string confirmPass = txtConfirmPassword.Text.Trim();

        if (string.IsNullOrEmpty(enteredOtp))
        {
            MessageBox.Show("Vui lòng nhập mã OTP gồm 6 chữ số đã được gửi tới email của bạn! 🔑", 
                "Thiếu OTP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtOtp.Focus();
            return;
        }

        if (enteredOtp != _generatedOtp)
        {
            MessageBox.Show("Mã OTP không chính xác hoặc đã hết hạn! Vui lòng kiểm tra lại. ❌", 
                "OTP sai", MessageBoxButtons.OK, MessageBoxIcon.Error);
            txtOtp.Focus();
            return;
        }

        if (string.IsNullOrEmpty(newPass) || string.IsNullOrEmpty(confirmPass))
        {
            MessageBox.Show("Vui lòng nhập mật khẩu mới và xác nhận lại mật khẩu! 🔒", 
                "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (newPass.Length < 6)
        {
            MessageBox.Show("Mật khẩu mới phải có ít nhất 6 ký tự để đảm bảo an toàn! 🛡", 
                "Mật khẩu quá ngắn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtNewPassword.Focus();
            return;
        }

        if (newPass != confirmPass)
        {
            MessageBox.Show("Mật khẩu mới và mật khẩu xác nhận không trùng khớp nhau! ⚠", 
                "Không trùng khớp", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtConfirmPassword.Focus();
            return;
        }

        MessageBox.Show("🎉 Chúc mừng! Mật khẩu của bạn đã được thay đổi thành công.\nHãy đăng nhập bằng mật khẩu mới nhé! 🍿", 
            "Đổi mật khẩu thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

        SwitchViewMode(FormMode.Login);
    }

