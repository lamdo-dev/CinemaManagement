namespace CinemaManagement;

partial class LoginForm
{
    private System.ComponentModel.IContainer components = null;
    private System.Windows.Forms.Panel pnlLeft;
    private System.Windows.Forms.Panel pnlRightCard;
    private System.Windows.Forms.Label lblBrandCineworld;
    private System.Windows.Forms.Label lblIllustration;
    private System.Windows.Forms.Label lblTitle;
    private System.Windows.Forms.Label lblSubtitle;
    private System.Windows.Forms.Label lblUsername;
    private System.Windows.Forms.Panel pnlUsernameBox;
    private System.Windows.Forms.TextBox txtUsername;
    private System.Windows.Forms.Label lblPassword;
    private System.Windows.Forms.Panel pnlPasswordBox;
    private System.Windows.Forms.TextBox txtPassword;
    private System.Windows.Forms.Label lblToggleEye;
    private System.Windows.Forms.LinkLabel lnkForgotPassword;
    private System.Windows.Forms.Button btnLogin;
    private System.Windows.Forms.Label lblClose;
    private System.Windows.Forms.Panel pnlRegister;
    private System.Windows.Forms.Label lblNoAccount;
    private System.Windows.Forms.LinkLabel lnkRegister;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        this.pnlLeft = new System.Windows.Forms.Panel();
        this.lblBrandCineworld = new System.Windows.Forms.Label();
        this.lblIllustration = new System.Windows.Forms.Label();
        this.pnlRightCard = new System.Windows.Forms.Panel();
        this.lblClose = new System.Windows.Forms.Label();
        this.lblTitle = new System.Windows.Forms.Label();
        this.lblSubtitle = new System.Windows.Forms.Label();
        this.lblUsername = new System.Windows.Forms.Label();
        this.pnlUsernameBox = new System.Windows.Forms.Panel();
        this.txtUsername = new System.Windows.Forms.TextBox();
        this.lblPassword = new System.Windows.Forms.Label();
        this.pnlPasswordBox = new System.Windows.Forms.Panel();
        this.txtPassword = new System.Windows.Forms.TextBox();
        this.lblToggleEye = new System.Windows.Forms.Label();
        this.lnkForgotPassword = new System.Windows.Forms.LinkLabel();
        this.btnLogin = new System.Windows.Forms.Button();
        this.pnlRegister = new System.Windows.Forms.Panel();
        this.lblNoAccount = new System.Windows.Forms.Label();
        this.lnkRegister = new System.Windows.Forms.LinkLabel();

        this.SuspendLayout();

        // 
        // pnlLeft (Khu vực hình ảnh và thương hiệu Cineworld)
        // 
        this.pnlLeft.BackColor = System.Drawing.Color.FromArgb(14, 18, 17);
        this.pnlLeft.Dock = System.Windows.Forms.DockStyle.Left;
        this.pnlLeft.Width = 430;
        this.pnlLeft.Controls.Add(this.lblIllustration);
        this.pnlLeft.Controls.Add(this.lblBrandCineworld);

        // lblIllustration (Icon đại diện bắp nước & kính 3D cinema)
        this.lblIllustration.Text = "🍿🎬🕶️";
        this.lblIllustration.Font = new System.Drawing.Font("Segoe UI Emoji", 42F);
        this.lblIllustration.ForeColor = System.Drawing.Color.White;
        this.lblIllustration.Location = new System.Drawing.Point(30, 160);
        this.lblIllustration.Size = new System.Drawing.Size(370, 90);
        this.lblIllustration.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

        // lblBrandCineworld (Chữ màu vàng kim sang trọng)
        this.lblBrandCineworld.Text = "Cineworld";
        this.lblBrandCineworld.Font = new System.Drawing.Font("Segoe UI", 26F, System.Drawing.FontStyle.Bold);
        this.lblBrandCineworld.ForeColor = System.Drawing.Color.FromArgb(235, 179, 65);
        this.lblBrandCineworld.Location = new System.Drawing.Point(30, 360);
        this.lblBrandCineworld.Size = new System.Drawing.Size(370, 50);
        this.lblBrandCineworld.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

        // 
        // pnlRightCard (Card bên phải nền xanh đậm theo ảnh)
        // 
        this.pnlRightCard.BackColor = System.Drawing.Color.FromArgb(23, 45, 39);
        this.pnlRightCard.Dock = System.Windows.Forms.DockStyle.Fill;
        this.pnlRightCard.Controls.Add(this.lblClose);
        this.pnlRightCard.Controls.Add(this.lblTitle);
        this.pnlRightCard.Controls.Add(this.lblSubtitle);
        this.pnlRightCard.Controls.Add(this.lblUsername);
        this.pnlRightCard.Controls.Add(this.pnlUsernameBox);
        this.pnlRightCard.Controls.Add(this.lblPassword);
        this.pnlRightCard.Controls.Add(this.pnlPasswordBox);
        this.pnlRightCard.Controls.Add(this.lnkForgotPassword);
        this.pnlRightCard.Controls.Add(this.btnLogin);
        this.pnlRightCard.Controls.Add(this.pnlRegister);

        // Nút tắt Form (X) ở góc phải
        this.lblClose.Text = "✕";
        this.lblClose.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this.lblClose.ForeColor = System.Drawing.Color.FromArgb(160, 175, 170);
        this.lblClose.Location = new System.Drawing.Point(460, 15);
        this.lblClose.Size = new System.Drawing.Size(30, 30);
        this.lblClose.Cursor = System.Windows.Forms.Cursors.Hand;
        this.lblClose.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        this.lblClose.Click += new System.EventHandler(this.lblClose_Click);

        // lblTitle: Login Form
        this.lblTitle.Text = "Login Form";
        this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
        this.lblTitle.ForeColor = System.Drawing.Color.White;
        this.lblTitle.Location = new System.Drawing.Point(55, 60);
        this.lblTitle.Size = new System.Drawing.Size(390, 40);

        // lblSubtitle
        this.lblSubtitle.Text = "Please enter your details";
        this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9.5F);
        this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(175, 195, 190);
        this.lblSubtitle.Location = new System.Drawing.Point(55, 105);
        this.lblSubtitle.Size = new System.Drawing.Size(390, 25);

        // lblUsername
        this.lblUsername.Text = "Username";
        this.lblUsername.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.lblUsername.ForeColor = System.Drawing.Color.White;
        this.lblUsername.Location = new System.Drawing.Point(55, 155);
        this.lblUsername.Size = new System.Drawing.Size(390, 22);

        // pnlUsernameBox (Khung viền trắng bo tròn ô nhập)
        this.pnlUsernameBox.BackColor = System.Drawing.Color.White;
        this.pnlUsernameBox.Location = new System.Drawing.Point(55, 180);
        this.pnlUsernameBox.Size = new System.Drawing.Size(390, 42);
        this.pnlUsernameBox.Controls.Add(this.txtUsername);

        this.txtUsername.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this.txtUsername.Font = new System.Drawing.Font("Segoe UI", 11F);
        this.txtUsername.ForeColor = System.Drawing.Color.FromArgb(50, 50, 50);
        this.txtUsername.Location = new System.Drawing.Point(15, 11);
        this.txtUsername.Size = new System.Drawing.Size(360, 20);
        this.txtUsername.PlaceholderText = "Enter username";

        // lblPassword
        this.lblPassword.Text = "Password";
        this.lblPassword.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.lblPassword.ForeColor = System.Drawing.Color.White;
        this.lblPassword.Location = new System.Drawing.Point(55, 235);
        this.lblPassword.Size = new System.Drawing.Size(390, 22);

        // pnlPasswordBox
        this.pnlPasswordBox.BackColor = System.Drawing.Color.White;
        this.pnlPasswordBox.Location = new System.Drawing.Point(55, 260);
        this.pnlPasswordBox.Size = new System.Drawing.Size(390, 42);
        this.pnlPasswordBox.Controls.Add(this.txtPassword);
        this.pnlPasswordBox.Controls.Add(this.lblToggleEye);

        this.txtPassword.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this.txtPassword.Font = new System.Drawing.Font("Segoe UI", 11F);
        this.txtPassword.ForeColor = System.Drawing.Color.FromArgb(50, 50, 50);
        this.txtPassword.Location = new System.Drawing.Point(15, 11);
        this.txtPassword.Size = new System.Drawing.Size(330, 20);
        this.txtPassword.PasswordChar = '●';
        this.txtPassword.PlaceholderText = "Enter password";

        // Icon mắt ẩn / hiện mật khẩu
        this.lblToggleEye.Text = "👁";
        this.lblToggleEye.Font = new System.Drawing.Font("Segoe UI Emoji", 11F);
        this.lblToggleEye.ForeColor = System.Drawing.Color.Gray;
        this.lblToggleEye.Location = new System.Drawing.Point(350, 6);
        this.lblToggleEye.Size = new System.Drawing.Size(30, 30);
        this.lblToggleEye.Cursor = System.Windows.Forms.Cursors.Hand;
        this.lblToggleEye.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        this.lblToggleEye.Click += new System.EventHandler(this.lblToggleEye_Click);

        // lnkForgotPassword
        this.lnkForgotPassword.Text = "Forgot Password?";
        this.lnkForgotPassword.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lnkForgotPassword.LinkColor = System.Drawing.Color.FromArgb(200, 220, 215);
        this.lnkForgotPassword.ActiveLinkColor = System.Drawing.Color.FromArgb(235, 179, 65);
        this.lnkForgotPassword.Location = new System.Drawing.Point(270, 310);
        this.lnkForgotPassword.Size = new System.Drawing.Size(175, 22);
        this.lnkForgotPassword.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
        this.lnkForgotPassword.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkForgotPassword_LinkClicked);

        // btnLogin (Nút màu vàng nâu/đồng chuẩn như hình)
        this.btnLogin.Text = "Login";
        this.btnLogin.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
        this.btnLogin.BackColor = System.Drawing.Color.FromArgb(170, 137, 85);
        this.btnLogin.ForeColor = System.Drawing.Color.White;
        this.btnLogin.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnLogin.FlatAppearance.BorderSize = 0;
        this.btnLogin.Cursor = System.Windows.Forms.Cursors.Hand;
        this.btnLogin.Location = new System.Drawing.Point(55, 365);
        this.btnLogin.Size = new System.Drawing.Size(390, 44);
        this.btnLogin.UseVisualStyleBackColor = false;
        this.btnLogin.Click += new System.EventHandler(this.btnLogin_Click);

        // pnlRegister (Dòng Tạo tài khoản mới)
        this.pnlRegister.Location = new System.Drawing.Point(55, 425);
        this.pnlRegister.Size = new System.Drawing.Size(390, 30);
        this.pnlRegister.Controls.Add(this.lblNoAccount);
        this.pnlRegister.Controls.Add(this.lnkRegister);

        this.lblNoAccount.Text = "Don't have an account?";
        this.lblNoAccount.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblNoAccount.ForeColor = System.Drawing.Color.FromArgb(180, 200, 195);
        this.lblNoAccount.Location = new System.Drawing.Point(70, 5);
        this.lblNoAccount.Size = new System.Drawing.Size(145, 20);
        this.lblNoAccount.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

        this.lnkRegister.Text = "Sign Up";
        this.lnkRegister.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
        this.lnkRegister.LinkColor = System.Drawing.Color.FromArgb(235, 179, 65);
        this.lnkRegister.ActiveLinkColor = System.Drawing.Color.White;
        this.lnkRegister.Location = new System.Drawing.Point(220, 4);
        this.lnkRegister.Size = new System.Drawing.Size(90, 20);
        this.lnkRegister.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.lnkRegister.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkRegister_LinkClicked);

        // LoginForm settings
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(940, 540);
        this.Controls.Add(this.pnlRightCard);
        this.Controls.Add(this.pnlLeft);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.ResumeLayout(false);
    }

    #endregion
}