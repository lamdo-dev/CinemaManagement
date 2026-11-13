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
    private FlowLayoutPanel flpContent = null!;
    private Label lblClose = null!;

    private Label lblTitle = null!;
    private Label lblSubtitle = null!;

    private Panel pnlGroupFullName = null!;
    private TextBox txtFullName = null!;

    private Panel pnlGroupEmail = null!;
    private TextBox txtEmail = null!;

    private Panel pnlGroupUsername = null!;
    private TextBox txtUsername = null!;

    private Panel pnlGroupPassword = null!;
    private TextBox txtPassword = null!;
    private Label lblToggleEye = null!;

    private Panel pnlGroupOtp = null!;
    private TextBox txtOtp = null!;

    private Panel pnlGroupNewPassword = null!;
    private TextBox txtNewPassword = null!;

    private Panel pnlGroupConfirmPassword = null!;
    private TextBox txtConfirmPassword = null!;

    private Panel pnlForgotWrapper = null!;
    private LinkLabel lnkForgotPassword = null!;
    private Button btnAction = null!;
    private FlowLayoutPanel pnlSwitchMode = null!;
    private Label lblSwitchPrompt = null!;
    private LinkLabel lnkSwitchAction = null!;

    private readonly Random _rand = new();

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

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        if (this.Width > 0 && this.Height > 0)
        {
            this.Region = new Region(GetRoundedPath(new Rectangle(0, 0, this.Width, this.Height), 26));
        }
    }

    #region Khởi tạo Layout (Responsive Auto-Scale)

    private void BuildOceanCuteLayout()
    {
        pnlLeft = new Panel
        {
            Dock = DockStyle.Left,
            Width = 420
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
            BackColor = _colCardBg
        };
        pnlRight.Paint += RenderRightPanelBorder;

        lblClose = new Label
        {
            Text = "✕",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(170, 185, 205),
            Size = new Size(34, 34),
            Location = new Point(pnlRight.Width - 46, 12),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter
        };
        lblClose.MouseEnter += (s, e) => { lblClose.ForeColor = _colOceanBlue; };
        lblClose.MouseLeave += (s, e) => { lblClose.ForeColor = Color.FromArgb(170, 185, 205); };
        lblClose.Click += (s, e) => Application.Exit();
        pnlRight.Controls.Add(lblClose);

        // Container chính dùng FlowLayoutPanel để tự động xếp dòng theo DPI mà không bao giờ đè chữ
        flpContent = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        lblTitle = new Label
        {
            Text = "WELCOME BACK ✨",
            Font = new Font("Segoe UI", 20F, FontStyle.Bold),
            ForeColor = _colTextDark,
            AutoSize = true,
            MaximumSize = new Size(460, 0),
            Margin = new Padding(0, 0, 0, 4),
            BackColor = Color.Transparent
        };

        lblSubtitle = new Label
        {
            Text = "Ready for an exciting movie journey? 🍿",
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = _colTextMuted,
            AutoSize = true,
            MaximumSize = new Size(460, 0),
            Margin = new Padding(2, 0, 0, 22),
            BackColor = Color.Transparent
        };

        flpContent.Controls.Add(lblTitle);
        flpContent.Controls.Add(lblSubtitle);

        (pnlGroupFullName, txtFullName) = CreateInputGroup("HỌ VÀ TÊN", "Nhập họ và tên của bạn...", "👤");
        flpContent.Controls.Add(pnlGroupFullName);

        (pnlGroupEmail, txtEmail) = CreateInputGroup("EMAIL ĐĂNG KÝ", "example@domain.com", "✉");
        flpContent.Controls.Add(pnlGroupEmail);

        (pnlGroupUsername, txtUsername) = CreateInputGroup("TÊN ĐĂNG NHẬP", "Tên tài khoản của bạn", "★");
        flpContent.Controls.Add(pnlGroupUsername);

        (pnlGroupPassword, txtPassword) = CreateInputGroup("MẬT KHẨU", "Mật khẩu bảo mật", "🔒", hasEyeToggle: true);
        txtPassword.PasswordChar = '●';
        flpContent.Controls.Add(pnlGroupPassword);

        (pnlGroupOtp, txtOtp) = CreateInputGroup("MÃ OTP (6 CHỮ SỐ)", "Nhập mã OTP gồm 6 chữ số", "🔑");
        flpContent.Controls.Add(pnlGroupOtp);

        (pnlGroupNewPassword, txtNewPassword) = CreateInputGroup("MẬT KHẨU MỚI", "Nhập mật khẩu mới của bạn", "🔒");
        txtNewPassword.PasswordChar = '●';
        flpContent.Controls.Add(pnlGroupNewPassword);

        (pnlGroupConfirmPassword, txtConfirmPassword) = CreateInputGroup("XÁC NHẬN MẬT KHẨU MỚI", "Nhập lại mật khẩu mới", "🛡");
        txtConfirmPassword.PasswordChar = '●';
        flpContent.Controls.Add(pnlGroupConfirmPassword);

        pnlForgotWrapper = new Panel
        {
            Width = 430,
            Height = 26,
            Margin = new Padding(0, 0, 0, 8),
            BackColor = Color.Transparent
        };

        lnkForgotPassword = new LinkLabel
        {
            Text = "Quên mật khẩu?",
            Font = new Font("Segoe UI", 9.25F),
            LinkColor = _colOceanBlue,
            ActiveLinkColor = _colDeepOcean,
            VisitedLinkColor = _colOceanBlue,
            AutoSize = true,
            Dock = DockStyle.Right,
            TextAlign = ContentAlignment.MiddleRight,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand,
            LinkBehavior = LinkBehavior.HoverUnderline
        };
        lnkForgotPassword.LinkClicked += (s, e) => SwitchViewMode(FormMode.ForgotPasswordStep1);
        pnlForgotWrapper.Controls.Add(lnkForgotPassword);
        flpContent.Controls.Add(pnlForgotWrapper);

        btnAction = new Button
        {
            Text = "ĐĂNG NHẬP NGAY",
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = _colOceanBlue,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Size = new Size(430, 46),
            Margin = new Padding(0, 8, 0, 14)
        };
        btnAction.FlatAppearance.BorderSize = 0;
        btnAction.Paint += RenderOceanButton;
        btnAction.MouseEnter += (s, e) => { btnAction.BackColor = _colOceanBlueHover; btnAction.Invalidate(); };
        btnAction.MouseLeave += (s, e) => { btnAction.BackColor = _colOceanBlue; btnAction.Invalidate(); };
        btnAction.Click += btnAction_Click;
        flpContent.Controls.Add(btnAction);

        pnlSwitchMode = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 4, 0, 0)
        };

        lblSwitchPrompt = new Label
        {
            Text = "Bạn chưa có tài khoản?",
            Font = new Font("Segoe UI", 9.25F),
            ForeColor = _colTextMuted,
            AutoSize = true,
            Margin = new Padding(0, 2, 4, 0)
        };

        lnkSwitchAction = new LinkLabel
        {
            Text = "Đăng ký thành viên",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            LinkColor = _colOceanBlue,
            ActiveLinkColor = _colDeepOcean,
            VisitedLinkColor = _colOceanBlue,
            AutoSize = true,
            Margin = new Padding(0, 2, 0, 0),
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
        flpContent.Controls.Add(pnlSwitchMode);

        pnlRight.Controls.Add(flpContent);
        pnlRight.Resize += (s, e) => CenterContentVertically();
        flpContent.SizeChanged += (s, e) => CenterContentVertically();

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

    private void CenterContentVertically()
    {
        if (pnlRight == null || flpContent == null) return;

        int x = Math.Max(30, (pnlRight.ClientSize.Width - flpContent.Width) / 2);
        int y = Math.Max(45, (pnlRight.ClientSize.Height - flpContent.Height) / 2);
        flpContent.Location = new Point(x, y);

        // Căn giữa dòng chuyển chế độ bên dưới nút bấm
        if (pnlSwitchMode != null && btnAction != null)
        {
            int leftMargin = Math.Max(0, (btnAction.Width - pnlSwitchMode.Width) / 2);
            pnlSwitchMode.Margin = new Padding(leftMargin, 4, 0, 0);
        }
    }

    private (Panel pnlGroup, TextBox txtField) CreateInputGroup(string title, string placeholder, string icon, bool hasEyeToggle = false)
    {
        var pnlGroup = new Panel
        {
            Width = 430,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 12),
            BackColor = Color.Transparent
        };

        var lbl = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 8.25F, FontStyle.Bold),
            ForeColor = Color.FromArgb(90, 115, 145),
            AutoSize = true,
            Location = new Point(0, 0),
            BackColor = Color.Transparent
        };

        var pnlBox = new Panel
        {
            Width = 430,
            Height = 42,
            BackColor = _colInputBg
        };

        // Đặt vị trí ô nhập ngay dưới nhãn tiêu đề theo chiều cao thực tế của Font ở DPI hiện tại
        lbl.SizeChanged += (s, e) => { pnlBox.Top = lbl.Bottom + 5; };
        pnlBox.Top = lbl.PreferredHeight + 5;

        var lblIcon = new Label
        {
            Text = icon,
            Font = new Font("Segoe UI", 11F),
            ForeColor = _colOceanBlue,
            Width = 36,
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        var pnlTextWrapper = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(4, 10, 6, 8),
            BackColor = Color.Transparent
        };

        var txt = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 10.5F),
            ForeColor = _colTextDark,
            BackColor = _colInputBg,
            Dock = DockStyle.Fill,
            PlaceholderText = placeholder
        };
        pnlTextWrapper.Controls.Add(txt);

        if (hasEyeToggle)
        {
            lblToggleEye = new Label
            {
                Text = "👁",
                Font = new Font("Segoe UI Emoji", 11.5F),
                ForeColor = _colTextMuted,
                Width = 38,
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            lblToggleEye.Click += TogglePasswordVisibility;
            pnlBox.Controls.Add(lblToggleEye);
        }

        pnlBox.Controls.Add(pnlTextWrapper);
        pnlBox.Controls.Add(lblIcon);

        bool isFocused = false;
        txt.GotFocus += (s, e) => { isFocused = true; pnlBox.Invalidate(); };
        txt.LostFocus += (s, e) => { isFocused = false; pnlBox.Invalidate(); };

        pnlBox.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, pnlBox.Width - 1, pnlBox.Height - 1);
            using var path = GetRoundedPath(rect, 12);

            using var bgBrush = new SolidBrush(_colInputBg);
            e.Graphics.FillPath(bgBrush, path);

            using var pen = new Pen(isFocused ? _colInputFocusBorder : _colInputBorder, isFocused ? 1.8F : 1.0F);
            e.Graphics.DrawPath(pen, path);
        };

        pnlGroup.Controls.Add(lbl);
        pnlGroup.Controls.Add(pnlBox);

        return (pnlGroup, txt);
    }

    #endregion

    #region Quản lý Chế độ hiển thị

    private void HideAllInputs()
    {
        pnlGroupFullName.Visible = false;
        pnlGroupEmail.Visible = false;
        pnlGroupUsername.Visible = false;
        pnlGroupPassword.Visible = false;
        pnlGroupOtp.Visible = false;
        pnlGroupNewPassword.Visible = false;
        pnlGroupConfirmPassword.Visible = false;
        pnlForgotWrapper.Visible = false;
    }

    private void SwitchViewMode(FormMode targetMode)
    {
        _currentMode = targetMode;
        flpContent.SuspendLayout();
        HideAllInputs();

        switch (_currentMode)
        {
            case FormMode.Login:
            {
                lblTitle.Text = "WELCOME BACK ✨";
                lblSubtitle.Text = "Ready for an exciting movie journey? 🍿";

                pnlGroupUsername.Visible = true;
                pnlGroupPassword.Visible = true;
                pnlForgotWrapper.Visible = true;

                btnAction.Text = "ĐĂNG NHẬP NGAY";
                lblSwitchPrompt.Text = "Bạn chưa có tài khoản?";
                lnkSwitchAction.Text = "Đăng ký thành viên";
                break;
            }

            case FormMode.Register:
            {
                lblTitle.Text = "TẠO TÀI KHOẢN MỚI ✨";
                lblSubtitle.Text = "Tham gia Cineworld VIP để nhận hàng ngàn vé ưu đãi!";

                pnlGroupFullName.Visible = true;
                pnlGroupUsername.Visible = true;
                pnlGroupPassword.Visible = true;

                btnAction.Text = "TẠO TÀI KHOẢN ";
                lblSwitchPrompt.Text = "Đã có tài khoản rồi?";
                lnkSwitchAction.Text = "Đăng nhập ngay";
                break;
            }

            case FormMode.ForgotPasswordStep1:
            {
                lblTitle.Text = "KHÔI PHỤC MẬT KHẨU ✉";
                lblSubtitle.Text = "Nhập email của bạn để nhận mã xác minh OTP tức thì.";

                pnlGroupEmail.Visible = true;

                btnAction.Text = "GỬI MÃ XÁC NHẬN";
                lblSwitchPrompt.Text = "Đã nhớ mật khẩu?";
                lnkSwitchAction.Text = "Quay về Đăng nhập";
                break;
            }

            case FormMode.ForgotPasswordStep2:
            {
                lblTitle.Text = "ĐẶT LẠI MẬT KHẨU 🔑";
                lblSubtitle.Text = $"Nhập mã OTP đã gửi tới {_recoveryEmail}";

                pnlGroupOtp.Visible = true;
                pnlGroupNewPassword.Visible = true;
                pnlGroupConfirmPassword.Visible = true;

                btnAction.Text = "XÁC NHẬN ĐỔI MẬT KHẨU";
                lblSwitchPrompt.Text = "Chưa nhận được mã?";
                lnkSwitchAction.Text = "Gửi lại OTP";
                break;
            }
        }

        flpContent.ResumeLayout(true);
        CenterContentVertically();
        pnlRight.Invalidate();
    }

    #endregion

    #region Render Giao diện & Vẽ Đồ họa

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

        int boxWidth = pnlLeft.Width - 50;
        int boxHeight = 260;
        int boxY = (pnlLeft.Height - boxHeight) / 2;

        using (var darkBackdrop = new SolidBrush(Color.FromArgb(55, 0, 0, 0)))
        {
            g.FillRectangle(darkBackdrop, 25, boxY, boxWidth, boxHeight);
        }

        using var fontCinema = new Font("Segoe UI Emoji", 40F);
        using var brushWhite = new SolidBrush(Color.White);
        string icons = "🍿  🎬  🐳";
        var sizeIcons = g.MeasureString(icons, fontCinema);
        g.DrawString(icons, fontCinema, brushWhite, (pnlLeft.Width - sizeIcons.Width) / 2, boxY + 20);

        using var fontBrand = new Font("Segoe UI", 30F, FontStyle.Bold);
        string brand = "CINEWORLD";
        var sizeBrand = g.MeasureString(brand, fontBrand);
        float brandX = (pnlLeft.Width - sizeBrand.Width) / 2;
        float brandY = boxY + 125;

        using (var shadowBrush = new SolidBrush(Color.FromArgb(170, 0, 0, 0)))
        {
            g.DrawString(brand, fontBrand, shadowBrush, brandX + 2, brandY + 2);
        }
        g.DrawString(brand, fontBrand, Brushes.White, brandX, brandY);

        using var fontTagline = new Font("Segoe UI", 10F, FontStyle.Bold);
        using var brushTagline = new SolidBrush(Color.FromArgb(245, 250, 255));
        string tagline = "FRESH & COZY CINEMA EXPERIENCE";
        var sizeTag = g.MeasureString(tagline, fontTagline);
        g.DrawString(tagline, fontTagline, brushTagline, (pnlLeft.Width - sizeTag.Width) / 2, boxY + 188);

        using var pPen = new Pen(Color.FromArgb(200, 255, 255, 255), 2F);
        g.DrawLine(pPen, 75, boxY + 225, pnlLeft.Width - 75, boxY + 225);
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

    #region Kéo thả Form & Phím tắt

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
        attachEvents(flpContent);
        attachEvents(lblTitle);
        attachEvents(lblSubtitle);
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

    #region Nghiệp vụ Đăng nhập, Đăng ký & Đổi mật khẩu

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
    {
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            MessageBox.Show("Bạn ơi, hãy nhập đầy đủ Tên đăng nhập và Mật khẩu nhé! ✨",
                "Nhắc nhở nhẹ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var user = _authService.Login(username, password);
        if (user == null)
        {
            MessageBox.Show("Tên đăng nhập hoặc mật khẩu chưa đúng rồi, bạn thử lại nhé! ❌",
                "Lỗi đăng nhập", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        UserSession.Login(user);
        OnLoginSuccess?.Invoke(this, user);

        MessageBox.Show($"Chào mừng trở lại rạp Cineworld!\nChúc {user.FullName} có trải nghiệm xem phim thật tuyệt vời! 🍿🎉",
            "Đăng nhập thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

        this.Hide();

        MainForm mainForm = new MainForm(user.Role);
        mainForm.FormClosed += (s, args) => this.Close();
        mainForm.Show();
    }

    private void HandleRegister()
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

    #endregion
}