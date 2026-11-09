using System.Drawing;
using System.Windows.Forms;

namespace CinemaManagement.UserControls.Staff
{
    public partial class TicketScannerUC : UserControl
    {
        public TicketScannerUC()
        {
            InitializeComponentManual();
        }

        private void InitializeComponentManual()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(15, 20, 30);

            // Left Side: QR Scanner Box
            Panel pnlCamera = new Panel
            {
                Dock = DockStyle.Left,
                Width = 420,
                Padding = new Padding(25),
                BackColor = Color.FromArgb(20, 26, 38)
            };

            Panel qrFrame = new Panel
            {
                Height = 280,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(10, 15, 25),
                BorderStyle = BorderStyle.FixedSingle
            };

            Panel pnlResult = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15)
            };

            Label lblStatus = new Label
            {
                Text = "SUCCESSFUL VALIDATION",
                ForeColor = Color.FromArgb(46, 204, 113),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 30
            };

            Label lblTicketDetail = new Label
            {
                Text = "SEAT: Orchestra Row F, Seat 12\nEVENT: HAMILTON - 7:30 PM\nNAME: Sarah Jenkins",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Dock = DockStyle.Fill
            };

            Button btnScanNext = new Button
            {
                Text = "Scan Next",
                Dock = DockStyle.Bottom,
                Height = 45,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            pnlResult.Controls.AddRange(new Control[] { lblTicketDetail, lblStatus, btnScanNext });
            pnlCamera.Controls.AddRange(new Control[] { pnlResult, qrFrame });

            // Right Side: Live Door List
            Panel pnlDoorList = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(25),
                BackColor = Color.FromArgb(15, 20, 30)
            };

            Label lblHeader = new Label
            {
                Text = "LIVE DOOR LIST\nGUESTS CHECKED IN: 412 / 500 (82.4%)",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 60
            };

            DataGridView dgvLogs = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(20, 26, 38),
                ForeColor = Color.White,
                GridColor = Color.FromArgb(35, 45, 60),
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                DefaultCellStyle = { BackColor = Color.FromArgb(20, 26, 38), SelectionBackColor = Color.FromArgb(40, 50, 75) }
            };

            dgvLogs.Columns.Add("colTime", "TIMESTAMP");
            dgvLogs.Columns.Add("colName", "NAME");
            dgvLogs.Columns.Add("colSeat", "SEAT");
            dgvLogs.Columns.Add("colStatus", "STATUS");

            dgvLogs.Rows.Add("7:24:12 PM", "Sarah Jenkins", "Orch Row F, Seat 12", "Valid");
            dgvLogs.Rows.Add("7:24:08 PM", "Mark Thompson", "Mezz Row B, Seat 5", "Valid");
            dgvLogs.Rows.Add("7:23:22 PM", "DUPLICATE TICKET", "ALERT", "Warning");

            pnlDoorList.Controls.AddRange(new Control[] { dgvLogs, lblHeader });

            this.Controls.Add(pnlDoorList);
            this.Controls.Add(pnlCamera);
        }
    }
}