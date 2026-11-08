using System.Drawing;
using System.Windows.Forms;

namespace CinemaManagement.UserControls.Admin;

public partial class FoodManagementUC : UserControl
{
    public FoodManagementUC()
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
            Text = "Quản Lý Danh Mục Bắp Nước & Tồn Kho",
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

        dgv.Columns.Add("Id", "Mã");
        dgv.Columns.Add("Name", "Tên Mặt Hàng");
        dgv.Columns.Add("Category", "Loại");
        dgv.Columns.Add("Price", "Giá Bán");
        dgv.Columns.Add("Stock", "Tồn Kho");

        dgv.Rows.Add("F01", "Bắp Rang Bơ (Ngọt) - Size L", "Bắp", "55,000 đ", "140 phần");
        dgv.Rows.Add("F02", "Bắp Phô Mai Cay - Size L", "Bắp", "65,000 đ", "95 phần");
        dgv.Rows.Add("D01", "Coca-Cola Tươi 32oz", "Nước", "35,000 đ", "420 ly");
        dgv.Rows.Add("CB01", "Combo Solo (1 Bắp L + 1 Nước L)", "Combo", "85,000 đ", "Không giới hạn");
        dgv.Rows.Add("CB02", "Combo Couple (1 Bắp L + 2 Nước L)", "Combo", "115,000 đ", "Không giới hạn");

        grp.Controls.Add(dgv);
        this.Controls.Add(grp);
    }
}