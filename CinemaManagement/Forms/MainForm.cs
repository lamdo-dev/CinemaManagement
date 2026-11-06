using System;
using System.Drawing;
using System.Windows.Forms;
using CinemaManagement.UserControls.Admin;
using CinemaManagement.UserControls.Staff;

namespace CinemaManagement.Forms
{
    public partial class MainForm : Form
    {
        private Panel pnlSidebar;
        private Panel pnlContent;
        private Button btnActive;

        public MainForm()
        {
            InitializeComponent();
            SetupAppLayout();
        }

        private void SetupAppLayout()
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(15, 20, 30);

            // Container hiển thị UserControl
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(18, 22, 34)
            };

            // Sidebar Menu
            pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 240,
                BackColor = Color.FromArgb(20, 26, 38),
                Padding = new Padding(10, 20, 10, 20)
            };

            Label lblLogo = new Label
            {
                Text = "🎬 Cinemo Admin",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 60,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlSidebar.Controls.Add(lblLogo);

            // Các nút chuyển đổi test
            var btnPrivileges = CreateNavBtn("4. Phân Quyền (Privileges)");
            var btnScanner = CreateNavBtn("3. Quét Vé (Scanner)");
            var btnPOS = CreateNavBtn("2. Bán Vé & Concession");
            var btnDashboard = CreateNavBtn("1. Dashboard Cinemo");

            btnDashboard.Click += (s, e) => SwitchScreen(new DashboardUC(), btnDashboard);
            btnPOS.Click += (s, e) => SwitchScreen(new POSUC(), btnPOS);
            btnScanner.Click += (s, e) => SwitchScreen(new TicketScannerUC(), btnScanner);
            btnPrivileges.Click += (s, e) => SwitchScreen(new UserManagementUC(), btnPrivileges);

            pnlSidebar.Controls.Add(btnPrivileges);
            pnlSidebar.Controls.Add(btnScanner);
            pnlSidebar.Controls.Add(btnPOS);
            pnlSidebar.Controls.Add(btnDashboard);

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlSidebar);

            // Mặc định nạp Dashboard
            SwitchScreen(new DashboardUC(), btnDashboard);
        }

        private Button CreateNavBtn(string text)
        {
            var btn = new Button
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 50,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(170, 185, 200),
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void SwitchScreen(UserControl uc, Button targetButton)
        {
            if (btnActive != null)
            {
                btnActive.BackColor = Color.Transparent;
                btnActive.ForeColor = Color.FromArgb(170, 185, 200);
            }

            btnActive = targetButton;
            btnActive.BackColor = Color.FromArgb(41, 98, 255);
            btnActive.ForeColor = Color.White;

            pnlContent.Controls.Clear();
            uc.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(uc);
        }
    }
}