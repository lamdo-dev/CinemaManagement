using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CinemaManagement.Forms
{
    public partial class LoginForm : Form
    {
        private Panel pnlLeft;
        private Panel pnlCard;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnLogin;
        private Label lblForgot;

        public LoginForm()
        {
            InitializeComponentManual();
        }

        private void InitializeComponentManual()
        {
            this.Size = new Size(1000, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(17, 24, 22);

            // Left Poster / Brand area
            pnlLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 480,
                BackColor = Color.FromArgb(20, 32, 28)
            };

            Label lblBrand = new Label
            {
                Text = "Cineworld",
                ForeColor = Color.FromArgb(212, 175, 55),
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                Dock = DockStyle.Bottom,
                Height = 100,
                TextAlign = ContentAlignment.TopCenter
            };
            pnlLeft.Controls.Add(lblBrand);

            // Right Login Card area
            pnlCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(19, 44, 38),
                Padding = new Padding(60, 80, 60, 60)
            };

            Label lblTitle = new Label
            {
                Text = "Login Form",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                Top = 70,
                Left = 60,
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = "Please enter your details",
                ForeColor = Color.FromArgb(160, 185, 175),
                Font = new Font("Segoe UI", 10),
                Top = 115,
                Left = 62,
                AutoSize = true
            };

            Label lblUser = new Label
            {
                Text = "Username",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Top = 170,
                Left = 60,
                AutoSize = true
            };

            txtUsername = new TextBox
            {
                Top = 200,
                Left = 60,
                Width = 360,
                Font = new Font("Segoe UI", 12),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblPass = new Label
            {
                Text = "Password",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Top = 250,
                Left = 60,
                AutoSize = true
            };

            txtPassword = new TextBox
            {
                Top = 280,
                Left = 60,
                Width = 360,
                Font = new Font("Segoe UI", 12),
                UseSystemPasswordChar = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            lblForgot = new Label
            {
                Text = "Forgot Password?",
                ForeColor = Color.FromArgb(180, 200, 195),
                Font = new Font("Segoe UI", 9),
                Top = 320,
                Left = 310,
                AutoSize = true,
                Cursor = Cursors.Hand
            };

            btnLogin = new Button
            {
                Text = "Login",
                Top = 380,
                Left = 60,
                Width = 360,
                Height = 45,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(170, 138, 77),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;

            pnlCard.Controls.AddRange(new Control[] { lblTitle, lblSubtitle, lblUser, txtUsername, lblPass, txtPassword, lblForgot, btnLogin });
            this.Controls.Add(pnlCard);
            this.Controls.Add(pnlLeft);
        }
    }
}