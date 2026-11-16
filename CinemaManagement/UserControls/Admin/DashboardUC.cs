
using System;
using System.Drawing;
using System.Windows.Forms;

namespace CinemaManagement.UserControls.Admin
{
    public partial class DashboardUC : UserControl
    {
        private FlowLayoutPanel pnlStats = null!;
        private Panel pnlBanner = null!;
        private Panel pnlSeatingReport = null!;

        private Panel cardRevenue = null!;
        private Panel cardSold = null!;
        private Panel cardCanceled = null!;

        private Label lblMovieTitle = null!;
        private Label lblRealtime = null!;

        private const int CardHeight = 100;
        private const int CardGap = 12;

        public DashboardUC()
        {
            InitializeComponentManual();

            SizeChanged += DashboardUC_SizeChanged;

            UpdateResponsiveLayout();
        }

        private void InitializeComponentManual()
        {
            Dock = DockStyle.Fill;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(18, 22, 34);
            Padding = new Padding(16);

            // =========================================
            // MAIN LAYOUT
            // =========================================

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.FromArgb(18, 22, 34),
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            mainLayout.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            mainLayout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 120));

            mainLayout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 190));

            mainLayout.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            // =========================================
            // STATISTICS
            // =========================================

            pnlStats = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                WrapContents = true,
                AutoScroll = true,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 5, 0, 5),
                Margin = new Padding(0)
            };

            cardRevenue = CreateStatCard(
                "DOANH THU VÉ",
                "$42,375 USD",
                "+12%",
                Color.FromArgb(46, 204, 113));

            cardSold = CreateStatCard(
                "VÉ ĐÃ BÁN",
                "7,956,445",
                "+8.4%",
                Color.FromArgb(46, 204, 113));

            cardCanceled = CreateStatCard(
                "VÉ ĐÃ HỦY",
                "875",
                "-2.1%",
                Color.FromArgb(231, 76, 60));

            pnlStats.Controls.Add(cardRevenue);
            pnlStats.Controls.Add(cardSold);
            pnlStats.Controls.Add(cardCanceled);

            // =========================================
            // MOVIE BANNER
            // =========================================

            pnlBanner = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(26, 32, 48),
                Padding = new Padding(22),
                Margin = new Padding(0, 5, 0, 10)
            };

            lblMovieTitle = new Label
            {
                Text = "THE MAGICIAN'S ELEPHANT",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            lblRealtime = new Label
            {
                Text = "Realtime Ticket Sold\n1,345,987",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlBanner.Controls.Add(lblRealtime);
            pnlBanner.Controls.Add(lblMovieTitle);

            // =========================================
            // SEATING REPORT
            // =========================================

            pnlSeatingReport = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(24, 29, 44),
                Padding = new Padding(20),
                Margin = new Padding(0, 5, 0, 0)
            };

            Label lblSeatingTitle = new Label
            {
                Text = "Seating Report - Studio 1",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 38,
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlSeatingReport.Controls.Add(lblSeatingTitle);

            // =========================================
            // ADD TO MAIN LAYOUT
            // =========================================

            mainLayout.Controls.Add(pnlStats, 0, 0);
            mainLayout.Controls.Add(pnlBanner, 0, 1);
            mainLayout.Controls.Add(pnlSeatingReport, 0, 2);

            Controls.Add(mainLayout);
        }

        // =========================================
        // CREATE STATISTIC CARD
        // =========================================

        private Panel CreateStatCard(
            string title,
            string value,
            string change,
            Color changeColor)
        {
            Panel card = new Panel
            {
                Height = CardHeight,
                BackColor = Color.FromArgb(28, 35, 52),
                Margin = new Padding(0, 0, CardGap, 5),
                Padding = new Padding(12),
                MinimumSize = new Size(180, CardHeight)
            };

            Label lblTitle = new Label
            {
                Text = title,
                ForeColor = Color.LightSlateGray,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 25,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            Label lblChange = new Label
            {
                Text = change,
                ForeColor = changeColor,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Dock = DockStyle.Right,
                Width = 65,
                TextAlign = ContentAlignment.MiddleRight
            };

            Label lblValue = new Label
            {
                Text = value,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            card.Controls.Add(lblValue);
            card.Controls.Add(lblChange);
            card.Controls.Add(lblTitle);

            return card;
        }

        // =========================================
        // RESPONSIVE LAYOUT
        // =========================================

        private void DashboardUC_SizeChanged(
            object? sender,
            EventArgs e)
        {
            UpdateResponsiveLayout();
        }

        private void UpdateResponsiveLayout()
        {
            if (pnlStats == null ||
                pnlStats.IsDisposed ||
                pnlBanner == null ||
                pnlBanner.IsDisposed)
            {
                return;
            }

            int availableWidth = pnlStats.ClientSize.Width;

            if (availableWidth <= 0)
                return;

            int columns;

            if (availableWidth >= 850)
            {
                columns = 3;
            }
            else if (availableWidth >= 550)
            {
                columns = 2;
            }
            else
            {
                columns = 1;
            }

            int cardWidth =
                (availableWidth - (columns - 1) * CardGap - 10)
                / columns;

            cardWidth = Math.Max(180, cardWidth);

            cardRevenue.Width = cardWidth;
            cardSold.Width = cardWidth;
            cardCanceled.Width = cardWidth;

            int rows = (int)Math.Ceiling(3.0 / columns);

            int statsHeight = rows * (CardHeight + 5) + 10;

            if (pnlStats.Parent is TableLayoutPanel layout)
            {
                layout.RowStyles[0].Height = statsHeight;
                layout.PerformLayout();
            }

            // Thu nhỏ cỡ chữ banner trên vùng hiển thị hẹp.
            lblMovieTitle.Font = availableWidth < 600
                ? new Font("Segoe UI", 11F, FontStyle.Bold)
                : new Font("Segoe UI", 14F, FontStyle.Bold);

            lblRealtime.Font = availableWidth < 600
                ? new Font("Segoe UI", 14F, FontStyle.Bold)
                : new Font("Segoe UI", 18F, FontStyle.Bold);
        }
    }
}
