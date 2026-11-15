
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Models;
using CinemaManagement.Services;

namespace CinemaManagement.UserControls.Customer;

public class PromotionListUC : UserControl
{
    private readonly PromotionService _service = new();

    private readonly Color _background = Color.FromArgb(14, 15, 22);
    private readonly Color _card = Color.FromArgb(24, 25, 36);
    private readonly Color _gold = Color.FromArgb(245, 197, 24);

    private FlowLayoutPanel _promotionList = null!;
    private Label _lblCount = null!;
    private Label _lblEmpty = null!;

    public PromotionListUC()
    {
        BuildInterface();
        LoadPromotions();
    }

    private void BuildInterface()
    {
        Dock = DockStyle.Fill;
        BackColor = _background;
        Padding = new Padding(25);

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 85,
            BackColor = _background
        };

        var title = new Label
        {
            Text = "🎟️ ƯU ĐÃI & KHUYẾN MÃI",
            Font = new Font("Segoe UI", 19, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(5, 5),
            AutoSize = true
        };

        _lblCount = new Label
        {
            Text = "Đang có 0 ưu đãi",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.Silver,
            Location = new Point(7, 45),
            AutoSize = true
        };

        var btnRefresh = new Button
        {
            Text = "↻ Làm mới",
            Size = new Size(110, 36),
            Location = new Point(650, 15),
            BackColor = _gold,
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        btnRefresh.FlatAppearance.BorderSize = 0;
        btnRefresh.Click += (s, e) => LoadPromotions();

        header.Controls.Add(title);
        header.Controls.Add(_lblCount);
        header.Controls.Add(btnRefresh);

        _promotionList = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(5, 15, 5, 5),
            BackColor = _background
        };

        _lblEmpty = new Label
        {
            Text = "Hiện chưa có chương trình khuyến mãi.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.Silver,
            Font = new Font("Segoe UI", 12)
        };

        Controls.Add(_promotionList);
        Controls.Add(_lblEmpty);
        Controls.Add(header);

        _lblEmpty.BringToFront();
    }

    public void LoadPromotions()
    {
        try
        {
            _promotionList.Controls.Clear();

            var today = DateTime.Today;

            var promotions = _service.GetAll()
                .Where(p =>
                    p.IsEnabled &&
                    p.StartDate.Date <= today &&
                    p.EndDate.Date >= today)
                .OrderBy(p => p.EndDate)
                .ToList();

            _lblCount.Text = $"Đang có {promotions.Count} ưu đãi";
            _lblEmpty.Visible = promotions.Count == 0;

            foreach (var promotion in promotions)
            {
                _promotionList.Controls.Add(
                    CreatePromotionCard(promotion));
            }
        }
        catch (Exception ex)
        {
            _lblEmpty.Visible = true;
            _lblEmpty.Text =
                "Không thể tải danh sách khuyến mãi.\n" + ex.Message;

            _lblCount.Text = "Lỗi tải dữ liệu";
        }
    }

    private Panel CreatePromotionCard(Promotion promotion)
    {
        var card = new Panel
        {
            Size = new Size(330, 205),
            BackColor = _card,
            Margin = new Padding(10),
            Padding = new Padding(15)
        };

        var lblDiscount = new Label
        {
            Text = promotion.DiscountType == "Phần trăm"
                ? $"GIẢM {promotion.DiscountValue:N0}%"
                : $"GIẢM {promotion.DiscountValue:N0} đ",
            Font = new Font("Segoe UI", 17, FontStyle.Bold),
            ForeColor = _gold,
            Location = new Point(15, 12),
            AutoSize = true
        };

        var lblName = new Label
        {
            Text = promotion.Name,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(15, 53),
            Size = new Size(295, 28),
            AutoEllipsis = true
        };

        var lblCode = new Label
        {
            Text = $"MÃ VOUCHER: {promotion.Code}",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.LightGreen,
            Location = new Point(15, 88),
            AutoSize = true
        };

        var lblCondition = new Label
        {
            Text = $"Đơn tối thiểu: {promotion.MinimumOrder:N0} đ",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.Silver,
            Location = new Point(15, 117),
            AutoSize = true
        };

        var lblDate = new Label
        {
            Text = $"Hạn dùng: {promotion.EndDate:dd/MM/yyyy}",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.Silver,
            Location = new Point(15, 140),
            AutoSize = true
        };

        var btnCopy = new Button
        {
            Text = "Sao chép mã",
            Location = new Point(190, 160),
            Size = new Size(120, 30),
            BackColor = _gold,
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };

        btnCopy.FlatAppearance.BorderSize = 0;

        btnCopy.Click += (s, e) =>
        {
            Clipboard.SetText(promotion.Code);

            MessageBox.Show(
                $"Đã sao chép mã {promotion.Code}.\n" +
                "Bạn có thể nhập mã này tại bước thanh toán.",
                "Khuyến mãi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        };

        card.Controls.AddRange(new Control[]
        {
            lblDiscount,
            lblName,
            lblCode,
            lblCondition,
            lblDate,
            btnCopy
        });

        return card;
    }
}
