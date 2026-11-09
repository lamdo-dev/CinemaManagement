using System.Drawing;
using System.Windows.Forms;

namespace CinemaManagement.UserControls.Admin
{
    public partial class DashboardUC : UserControl
    {
        public DashboardUC()
        {
            InitializeComponentManual();
        }

        private void InitializeComponentManual()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(18, 22, 34);

            // Stat Cards Container
            FlowLayoutPanel pnlStats = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 110,
                Padding = new Padding(20, 15, 20, 0),
                BackColor = Color.Transparent
            };

            pnlStats.Controls.Add(CreateStatCard("Revenue Ticket", "$42,375 USD", "+12%", Color.FromArgb(46, 204, 113)));
            pnlStats.Controls.Add(CreateStatCard("Chair Sold", "7,956,445", "+8.4%", Color.FromArgb(46, 204, 113)));
            pnlStats.Controls.Add(CreateStatCard("Ticket Canceled", "875", "-2.1%", Color.FromArgb(231, 76, 60)));

            // Hero banner section
            Panel pnlBanner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 220,
                Padding = new Padding(25),
                BackColor = Color.FromArgb(26, 32, 48),
                Margin = new Padding(20)
            };

            Label lblMovieTitle = new Label
            {
                Text = "THE MAGICIAN'S ELEPHANT",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Top = 20,
                Left = 20
            };

            Label lblRealtime = new Label
            {
                Text = "Realtime Ticket Sold\n1,345,987",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Top = 60,
                Left = 20
            };
            pnlBanner.Controls.AddRange(new Control[] { lblMovieTitle, lblRealtime });

            // Seating Report Section (Phía dưới)
            Panel pnlSeatingReport = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(24, 29, 44),
                Margin = new Padding(20),
                Padding = new Padding(20)
            };

            Label lblSeatingTitle = new Label
            {
                Text = "Seating Report - Studio 1",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top
            };
            pnlSeatingReport.Controls.Add(lblSeatingTitle);

            this.Controls.Add(pnlSeatingReport);
            this.Controls.Add(pnlBanner);
            this.Controls.Add(pnlStats);
        }

        private Panel CreateStatCard(string title, string value, string change, Color changeColor)
        {
            Panel card = new Panel
            {
                Width = 240,
                Height = 85,
                BackColor = Color.FromArgb(28, 35, 52),
                Margin = new Padding(10, 0, 10, 0),
                Padding = new Padding(12)
            };

            Label lblT = new Label { Text = title, ForeColor = Color.LightSlateGray, Font = new Font("Segoe UI", 9), Dock = DockStyle.Top };
            Label lblV = new Label { Text = value, ForeColor = Color.White, Font = new Font("Segoe UI", 14, FontStyle.Bold), Dock = DockStyle.Fill };
            Label lblC = new Label { Text = change, ForeColor = changeColor, Font = new Font("Segoe UI", 9, FontStyle.Bold), Dock = DockStyle.Right, Width = 60, TextAlign = ContentAlignment.MiddleRight };

            card.Controls.Add(lblV);
            card.Controls.Add(lblC);
            card.Controls.Add(lblT);
            return card;
        }
    }
}