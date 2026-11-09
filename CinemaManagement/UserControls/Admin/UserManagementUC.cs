using System.Drawing;
using System.Windows.Forms;

namespace CinemaManagement.UserControls.Admin
{
    public partial class UserManagementUC : UserControl
    {
        public UserManagementUC()
        {
            InitializeComponentManual();
        }

        private void InitializeComponentManual()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.White;
            this.Padding = new Padding(25);

            // Header Section
            Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 90 };
            Label lblTitle = new Label
            {
                Text = "ROLE PRIVILEGES",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Top = 5,
                Left = 0,
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = "Feature: Zones  |  Sub-feature: Zone Listings",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.Gray,
                Top = 45,
                Left = 0,
                AutoSize = true
            };
            pnlTop.Controls.AddRange(new Control[] { lblTitle, lblSubtitle });

            // Permissions Matrix Table
            DataGridView dgvPrivileges = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 42,
                RowTemplate = { Height = 40 }
            };

            dgvPrivileges.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(41, 98, 255);
            dgvPrivileges.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvPrivileges.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            dgvPrivileges.Columns.Add("colRole", "ROLES");
            dgvPrivileges.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "View", Name = "colView" });
            dgvPrivileges.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Edit", Name = "colEdit" });
            dgvPrivileges.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Add", Name = "colAdd" });
            dgvPrivileges.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Delete", Name = "colDelete" });
            dgvPrivileges.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Seat Map", Name = "colSeatMap" });
            dgvPrivileges.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Edit Event", Name = "colEvent" });

            dgvPrivileges.Rows.Add("Admin", true, true, true, true, true, true);
            dgvPrivileges.Rows.Add("Management", true, true, true, false, true, false);
            dgvPrivileges.Rows.Add("Staff / Cashier", true, false, true, false, true, false);
            dgvPrivileges.Rows.Add("Buyer / Customer", true, false, false, false, false, false);

            // Bottom Actions (Save / Cancel)
            Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 60 };
            Button btnSave = new Button
            {
                Text = "SAVE",
                Dock = DockStyle.Right,
                Width = 110,
                BackColor = Color.FromArgb(41, 98, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;

            Button btnCancel = new Button
            {
                Text = "CANCEL",
                Dock = DockStyle.Right,
                Width = 100,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            pnlBottom.Controls.AddRange(new Control[] { btnCancel, btnSave });

            this.Controls.Add(dgvPrivileges);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlTop);
        }
    }
}