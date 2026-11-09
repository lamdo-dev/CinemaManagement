using System.Drawing;
using System.Windows.Forms;

namespace CinemaManagement.UserControls.Staff
{
    public partial class POSUC : UserControl
    {
        public POSUC()
        {
            InitializeComponentManual();
        }

        private void InitializeComponentManual()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(245, 246, 250);

            // Right Order/Billing Panel
            Panel pnlOrder = new Panel
            {
                Dock = DockStyle.Right,
                Width = 360,
                BackColor = Color.White,
                Padding = new Padding(20)
            };

            Label lblOrderTab = new Label
            {
                Text = "Items  |  Payments",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(124, 58, 237),
                Dock = DockStyle.Top,
                Height = 35
            };

            ListBox lstItems = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10),
                ItemHeight = 28
            };
            lstItems.Items.AddRange(new object[] {
                "2 Adult - Avatar 2               $25.00",
                "1 Senior citizen                 $12.50",
                "1 Doritos Nacho Chips            $6.00",
                "2 Coca Cola regular 375ml        $2.50"
            });

            Panel pnlTotal = new Panel { Dock = DockStyle.Bottom, Height = 130 };
            Label lblTotal = new Label
            {
                Text = "Total: $46.00",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 40
            };

            Button btnPay = new Button
            {
                Text = "Pay $46.00",
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(124, 58, 237),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnPay.FlatAppearance.BorderSize = 0;

            pnlTotal.Controls.AddRange(new Control[] { lblTotal, btnPay });
            pnlOrder.Controls.AddRange(new Control[] { lstItems, lblTotal, pnlTotal, lblOrderTab });

            // Center / Left Content: Movies & Concessions selection
            FlowLayoutPanel flpItems = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20)
            };

            // Sample item cards (Movie / Concession)
            flpItems.Controls.Add(CreateItemCard("Coca Cola 375 ml", "$1.25"));
            flpItems.Controls.Add(CreateItemCard("Sprite 375 ml", "$1.25"));
            flpItems.Controls.Add(CreateItemCard("Popcorn Caramel L", "$4.50"));
            flpItems.Controls.Add(CreateItemCard("The Northman (09:00 AM)", "$10.00"));

            this.Controls.Add(flpItems);
            this.Controls.Add(pnlOrder);
        }

        private Panel CreateItemCard(string name, string price)
        {
            Panel card = new Panel
            {
                Width = 180,
                Height = 220,
                BackColor = Color.White,
                Margin = new Padding(12)
            };

            Panel imgBox = new Panel { Dock = DockStyle.Top, Height = 140, BackColor = Color.FromArgb(220, 225, 230) };
            Label lblName = new Label { Text = name, Font = new Font("Segoe UI", 9, FontStyle.Bold), Dock = DockStyle.Top, Height = 35, TextAlign = ContentAlignment.MiddleCenter };
            Label lblPrice = new Label { Text = price, ForeColor = Color.DarkSlateBlue, Font = new Font("Segoe UI", 10, FontStyle.Bold), Dock = DockStyle.Bottom, TextAlign = ContentAlignment.MiddleCenter };

            card.Controls.AddRange(new Control[] { lblName, lblPrice, imgBox });
            return card;
        }
    }
}