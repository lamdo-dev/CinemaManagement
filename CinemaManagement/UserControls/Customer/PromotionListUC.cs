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
    private readonly Color _goldHover = Color.FromArgb(255, 214, 65);

    private FlowLayoutPanel _promotionList = null!;
    private Label _lblCount = null!;
    private Label _lblEmpty = null!;

    public PromotionListUC()
    {
        AutoScaleMode = AutoScaleMode.Dpi;

        BuildInterface();
        LoadPromotions();
    }

    private void BuildInterface()
    {
        Dock = DockStyle.Fill;
        BackColor = _background;
        Padding = new Padding(24, 20, 24, 20);

        // -------------------------------------------------------------
        // 1. HEADER RESPONSIVE (TIÊU ĐỀ + SỐ LƯỢNG + NÚT LÀM MỚI)
        // -------------------------------------------------------------
        var tlpHeader = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = _background,
            Margin = new Padding(0),
            Padding = new Padding(0, 0, 0, 16)
        };
        tlpHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tlpHeader.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var tlpTitleBlock = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 16, 0)
        };
        tlpTitleBlock.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpTitleBlock.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tlpTitleBlock.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var title = new Label
        {
            Text = "🎟️ ƯU ĐÃI & KHUYẾN MÃI",
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4)
        };

        _lblCount = new Label
        {
            Text = "Đang có 0 ưu đãi",
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = Color.Silver,
            AutoSize = true,
            Margin = new Padding(2, 0, 0, 0)
        };

        tlpTitleBlock.Controls.Add(title, 0, 0);
        tlpTitleBlock.Controls.Add(_lblCount, 0, 1);

        var btnRefresh = new Button
        {
            Text = "↻ Làm mới",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(14, 7, 14, 7),
            Margin = new Padding(0, 4, 0, 0),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = _gold,
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnRefresh.FlatAppearance.BorderSize = 0;
        btnRefresh.MouseEnter += (s, e) => btnRefresh.BackColor = _goldHover;
        btnRefresh.MouseLeave += (s, e) => btnRefresh.BackColor = _gold;
        btnRefresh.Click += (s, e) => LoadPromotions();

        tlpHeader.Controls.Add(tlpTitleBlock, 0, 0);
        tlpHeader.Controls.Add(btnRefresh, 1, 0);

        // -------------------------------------------------------------
        // 2. DANH SÁCH THẺ KHUYẾN MÃI
        // -------------------------------------------------------------
        _promotionList = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 8, 0, 8),
            BackColor = _background
        };

        _lblEmpty = new Label
        {
            Text = "Hiện chưa có chương trình khuyến mãi.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.Silver,
            Font = new Font("Segoe UI", 12F)
        };

        Controls.Add(_promotionList);
        Controls.Add(_lblEmpty);
        Controls.Add(tlpHeader);

        _lblEmpty.BringToFront();
    }

    public void LoadPromotions()
    {
        try
        {
            _promotionList.SuspendLayout();
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
        finally
        {
            _promotionList.ResumeLayout();
        }
    }

    private Control CreatePromotionCard(Promotion promotion)
    {
        var card = new Panel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(330, 0),
            MaximumSize = new Size(360, 0),
            BackColor = _card,
            Margin = new Padding(0, 0, 16, 16),
            Padding = new Padding(18, 16, 18, 16)
        };

        var tlpCard = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(294, 0),
            ColumnCount = 1,
            RowCount = 6,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        tlpCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        for (int i = 0; i < 6; i++)
        {
            tlpCard.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        var lblDiscount = new Label
        {
            Text = promotion.DiscountType == "Phần trăm"
                ? $"GIẢM {promotion.DiscountValue:N0}%"
                : $"GIẢM {promotion.DiscountValue:N0} đ",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = _gold,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6)
        };

        var lblName = new Label
        {
            Text = promotion.Name,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            MaximumSize = new Size(294, 0),
            Margin = new Padding(0, 0, 0, 10)
        };

        var lblCode = new Label
        {
            Text = $"MÃ VOUCHER: {promotion.Code}",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.LightGreen,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6)
        };

        var lblCondition = new Label
        {
            Text = $"Đơn tối thiểu: {promotion.MinimumOrder:N0} đ",
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.Silver,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 3)
        };

        var lblDate = new Label
        {
            Text = $"Hạn dùng: {promotion.EndDate:dd/MM/yyyy}",
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.Silver,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 14)
        };

        var btnCopy = new Button
        {
            Text = "📋 Sao chép mã",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(12, 6, 12, 6),
            Margin = new Padding(0),
            Anchor = AnchorStyles.Right,
            BackColor = _gold,
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnCopy.FlatAppearance.BorderSize = 0;
        btnCopy.MouseEnter += (s, e) => btnCopy.BackColor = _goldHover;
        btnCopy.MouseLeave += (s, e) => btnCopy.BackColor = _gold;

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

        tlpCard.Controls.Add(lblDiscount, 0, 0);
        tlpCard.Controls.Add(lblName, 0, 1);
        tlpCard.Controls.Add(lblCode, 0, 2);
        tlpCard.Controls.Add(lblCondition, 0, 3);
        tlpCard.Controls.Add(lblDate, 0, 4);
        tlpCard.Controls.Add(btnCopy, 0, 5);

        card.Controls.Add(tlpCard);
        return card;
    }
}