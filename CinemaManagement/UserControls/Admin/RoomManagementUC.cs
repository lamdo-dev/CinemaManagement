using System.Drawing;
using System.Windows.Forms;

namespace CinemaManagement.UserControls.Admin;

public partial class RoomManagementUC : UserControl
{
    public RoomManagementUC()
    {
        InitializeComponent();
        BuildUI();
    }

    private void BuildUI()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.FromArgb(248, 249, 250);
        this.Padding = new Padding(15);

        var grp = new GroupBox
        {
            Text = "Danh Sách Phòng Chiếu & Sức Chứa",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(44, 62, 80),
            Padding = new Padding(10)
        };

        var dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            ReadOnly = true,
            RowHeadersVisible = false,
            Font = new Font("Segoe UI", 9.5F)
        };

        dgv.Columns.Add("Id", "Mã Phòng");
        dgv.Columns.Add("Name", "Tên Phòng");
        dgv.Columns.Add("Type", "Định Dạng");
        dgv.Columns.Add("Seats", "Tổng Số Ghế");
        dgv.Columns.Add("Status", "Trạng Thái");

        dgv.Rows.Add("P01", "Cinema 01", "Standard 2D", "80 ghế (10 hàng x 8)", "Sẵn sàng");
        dgv.Rows.Add("P02", "Cinema 02 (VIP)", "VIP Lounge 2D", "64 ghế (8 hàng x 8)", "Sẵn sàng");
        dgv.Rows.Add("P03", "Cinema 03 (IMAX)", "IMAX 3D Laser", "120 ghế (12 hàng x 10)", "Sẵn sàng");
        dgv.Rows.Add("P04", "Cinema 04 (4DX)", "4DX Motion Effects", "48 ghế (6 hàng x 8)", "Bảo trì");

        grp.Controls.Add(dgv);
        this.Controls.Add(grp);
    }
}