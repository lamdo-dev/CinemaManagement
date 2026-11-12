
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Models;

namespace CinemaManagement.UserControls.Customer
{
    public partial class FoodSelectionUC : UserControl
    {
        // Bảng màu Modern Dark Cinema
        private readonly Color ColorBg = Color.FromArgb(18, 19, 28);
        private readonly Color ColorCard = Color.FromArgb(28, 29, 40);
        private readonly Color ColorGold = Color.FromArgb(195, 155, 80);
        private readonly Color ColorTextMuted = Color.FromArgb(150, 150, 170);
        private readonly Color ColorControlBg = Color.FromArgb(42, 43, 58);

        private Dictionary<int, (string Name, decimal Price, int Qty, string ImgPath)>
            cartItems = new();

        private BookingContext? _bookingContext;

        private Panel pnlHeader = null!;
        private Button btnBack = null!;
        private FlowLayoutPanel flpFoodCatalog = null!;
        private Panel pnlCartSummary = null!;
        private FlowLayoutPanel flpCartItemList = null!;
        private Label lblTotalFoodPrice = null!;

        // Hai nút điều hướng
        private Button btnContinue = null!;
        private Button btnSkipFood = null!;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public decimal TicketTotalAmount { get; set; } = 0m;

        public event EventHandler<(
            Dictionary<int, (string Name, decimal Price, int Qty, string ImgPath)> foodCart,
            decimal totalFoodAmount)>? OnContinueToCheckout;

        public event EventHandler? OnBackClicked;

        // Constructor mặc định
        public FoodSelectionUC() : this(0m)
        {
        }

        // Constructor tương thích code cũ
        public FoodSelectionUC(decimal ticketTotalAmount)
        {
            TicketTotalAmount = ticketTotalAmount;

            InitializeComponents();
            LoadFoodCatalog();
            UpdateCartSummaryUI();
        }

        // Constructor dùng BookingContext chung
        public FoodSelectionUC(BookingContext bookingContext) : this()
        {
            SetBookingContext(bookingContext);
        }

        // Nhận BookingContext và khôi phục giỏ hàng
        public void SetBookingContext(BookingContext bookingContext)
        {
            _bookingContext = bookingContext
                              ?? throw new ArgumentNullException(nameof(bookingContext));

            // Nhận tổng tiền vé
            TicketTotalAmount = bookingContext.TicketAmount;

            // Khôi phục giỏ hàng đã lưu
            cartItems = new Dictionary<int, (
                string Name,
                decimal Price,
                int Qty,
                string ImgPath
            )>(bookingContext.FoodCart);

            // Cập nhật giao diện sản phẩm và giỏ hàng
            LoadFoodCatalog();
            UpdateCartSummaryUI();
        }

        private void InitializeComponents()
        {
            Dock = DockStyle.Fill;
            BackColor = ColorBg;

            // ==========================================
            // HEADER
            // ==========================================
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(14, 15, 22)
            };

            btnBack = new Button
            {
                Text = "← Quay lại",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(35, 36, 50),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 32),
                Location = new Point(20, 15),
                Cursor = Cursors.Hand
            };

            btnBack.FlatAppearance.BorderSize = 0;

            btnBack.Click += (s, e) =>
                OnBackClicked?.Invoke(this, EventArgs.Empty);

            // Stepper
            Panel pnlStepper = new Panel
            {
                Size = new Size(500, 40),
                Location = new Point(320, 12)
            };

            Label lblStep1 = new Label
            {
                Text = "① CHỌN SUẤT",
                Font = new Font("Segoe UI", 9),
                ForeColor = ColorTextMuted,
                Location = new Point(0, 10),
                AutoSize = true
            };

            Label lblDiv1 = new Label
            {
                Text = "───",
                ForeColor = ColorTextMuted,
                Location = new Point(105, 10),
                AutoSize = true
            };

            Label lblStep2 = new Label
            {
                Text = "② CHỌN GHẾ",
                Font = new Font("Segoe UI", 9),
                ForeColor = ColorTextMuted,
                Location = new Point(145, 10),
                AutoSize = true
            };

            Label lblDiv2 = new Label
            {
                Text = "───",
                ForeColor = ColorTextMuted,
                Location = new Point(245, 10),
                AutoSize = true
            };

            Label lblStep3 = new Label
            {
                Text = "③ BẮP NƯỚC",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = ColorGold,
                Location = new Point(285, 10),
                AutoSize = true
            };

            pnlStepper.Controls.AddRange(new Control[]
            {
                lblStep1, lblDiv1, lblStep2, lblDiv2, lblStep3
            });

            pnlHeader.Controls.Add(btnBack);
            pnlHeader.Controls.Add(pnlStepper);

            // ==========================================
            // GIỎ HÀNG BÊN PHẢI
            // ==========================================
            pnlCartSummary = new Panel
            {
                Dock = DockStyle.Right,
                Width = 360,
                BackColor = Color.FromArgb(23, 24, 34),
                Padding = new Padding(15)
            };

            Panel pnlCartHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45
            };

            Label lblCartTitle = new Label
            {
                Text = "🍿 Đơn Bắp Nước",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(0, 8),
                AutoSize = true
            };

            Button btnClearCart = new Button
            {
                Text = "Xóa tất cả",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = ColorTextMuted,
                BackColor = Color.FromArgb(35, 36, 50),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(75, 26),
                Location = new Point(255, 8),
                Cursor = Cursors.Hand
            };

            btnClearCart.FlatAppearance.BorderSize = 0;

            btnClearCart.Click += (s, e) =>
            {
                cartItems.Clear();
                SyncBookingContext();
                LoadFoodCatalog();
                UpdateCartSummaryUI();
            };

            pnlCartHeader.Controls.Add(lblCartTitle);
            pnlCartHeader.Controls.Add(btnClearCart);

            // ==========================================
            // DANH SÁCH GIỎ HÀNG
            // ==========================================
            flpCartItemList = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(0, 10, 0, 10),
                WrapContents = false,
                FlowDirection = FlowDirection.TopDown
            };

            // ==========================================
            // FOOTER GIỎ HÀNG
            // Tăng chiều cao để chứa hai nút
            // ==========================================
            Panel pnlCartFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 175
            };

            Label lblTotalLabel = new Label
            {
                Text = "Tổng tiền bắp nước:",
                Font = new Font("Segoe UI", 10),
                ForeColor = ColorTextMuted,
                Location = new Point(0, 10),
                AutoSize = true
            };

            lblTotalFoodPrice = new Label
            {
                Text = "0 đ",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = ColorGold,
                Location = new Point(0, 32),
                AutoSize = true
            };

            // Nút tiếp tục thanh toán
            btnContinue = new Button
            {
                Text = "TIẾP TỤC THANH TOÁN →",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.Black,
                BackColor = ColorGold,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(330, 40),
                Location = new Point(0, 75),
                Cursor = Cursors.Hand
            };

            btnContinue.FlatAppearance.BorderSize = 0;
            btnContinue.Click += BtnContinue_Click;

            // ==========================================
            // NÚT BỎ QUA BẮP NƯỚC
            // ==========================================
            btnSkipFood = new Button
            {
                Text = "BỎ QUA BẮP NƯỚC →",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(55, 57, 72),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(330, 38),
                Location = new Point(0, 122),
                Cursor = Cursors.Hand
            };

            btnSkipFood.FlatAppearance.BorderSize = 0;

            btnSkipFood.FlatAppearance.MouseOverBackColor =
                Color.FromArgb(75, 77, 95);

            btnSkipFood.Click += BtnSkipFood_Click;

            // Thêm các control vào Footer
            pnlCartFooter.Controls.Add(lblTotalLabel);
            pnlCartFooter.Controls.Add(lblTotalFoodPrice);
            pnlCartFooter.Controls.Add(btnContinue);
            pnlCartFooter.Controls.Add(btnSkipFood);

            // Thêm các control vào giỏ hàng
            pnlCartSummary.Controls.Add(flpCartItemList);
            pnlCartSummary.Controls.Add(pnlCartHeader);
            pnlCartSummary.Controls.Add(pnlCartFooter);

            // ==========================================
            // DANH SÁCH SẢN PHẨM
            // ==========================================
            flpFoodCatalog = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                AutoScroll = true,
                WrapContents = true
            };

            Controls.Add(flpFoodCatalog);
            Controls.Add(pnlCartSummary);
            Controls.Add(pnlHeader);
        }

        // ==========================================
        // TẢI DANH SÁCH SẢN PHẨM
        // ==========================================
        private void LoadFoodCatalog()
        {
            flpFoodCatalog.Controls.Clear();

            var products = new List<(
                int Id, string Name, decimal Price, string Desc
            )>
            {
                (1, "Coca Cola Regular 375ml", 35000m,
                    "Nước ngọt có ga hương vị truyền thống mát lạnh."),

                (2, "Sprite Regular 375ml", 35000m,
                    "Nước giải khát hương chanh sảng khoái."),

                (3, "Fanta Orange 375ml", 35000m,
                    "Nước ngọt hương cam tươi mát."),

                (4, "Bắp Rang Bơ Thơm Lừng L", 55000m,
                    "Bắp rang bơ vàng óng, giòn rụm đậm vị."),

                (5, "Bắp Rang Phô Mai L", 65000m,
                    "Bắp rang phủ bột phô mai béo ngậy cực cuốn."),

                (6, "Combo Solo (1 Bắp L + 1 Nước L)", 85000m,
                    "Tiết kiệm 10.000đ cho suất 1 người xem."),

                (7, "Combo Couple (1 Bắp L + 2 Nước L)", 115000m,
                    "Trọn gói bắp nước hoàn hảo cho cặp đôi."),

                (8, "Combo Family (2 Bắp L + 3 Nước L)", 195000m,
                    "Ưu đãi lớn dành cho nhóm bạn và gia đình.")
            };

            foreach (var prod in products)
            {
                int currentQty = cartItems.ContainsKey(prod.Id)
                    ? cartItems[prod.Id].Qty
                    : 0;

                Panel card = CreateFoodCard(
                    prod.Id,
                    prod.Name,
                    prod.Price,
                    prod.Desc,
                    currentQty);

                flpFoodCatalog.Controls.Add(card);
            }
        }

        // ==========================================
        // TẠO CARD SẢN PHẨM
        // ==========================================
        private Panel CreateFoodCard(
            int id,
            string name,
            decimal price,
            string desc,
            int initialQty)
        {
            Panel card = new Panel
            {
                Size = new Size(240, 290),
                BackColor = ColorCard,
                Margin = new Padding(0, 0, 15, 15)
            };

            PictureBox picImg = new PictureBox
            {
                Location = new Point(12, 12),
                Size = new Size(216, 120),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(20, 21, 30)
            };

            Label lblName = new Label
            {
                Text = name,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(12, 138),
                Size = new Size(216, 22),
                AutoEllipsis = true
            };

            Label lblDesc = new Label
            {
                Text = desc,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = ColorTextMuted,
                Location = new Point(12, 162),
                Size = new Size(216, 36)
            };

            Label lblPrice = new Label
            {
                Text = $"{price.ToString("N0", new CultureInfo("vi-VN"))} đ",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = ColorGold,
                Location = new Point(12, 210),
                AutoSize = true
            };

            Button btnMinus = new Button
            {
                Text = "-",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = ColorControlBg,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(32, 32),
                Location = new Point(120, 245),
                Cursor = Cursors.Hand
            };

            btnMinus.FlatAppearance.BorderSize = 0;

            Label lblQty = new Label
            {
                Text = initialQty.ToString(),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(154, 245),
                Size = new Size(36, 32),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(20, 21, 30)
            };

            Button btnPlus = new Button
            {
                Text = "+",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = ColorControlBg,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(32, 32),
                Location = new Point(192, 245),
                Cursor = Cursors.Hand
            };

            btnPlus.FlatAppearance.BorderSize = 0;

            // Giảm số lượng
            btnMinus.Click += (s, e) =>
            {
                int qty = int.Parse(lblQty.Text);

                if (qty > 0)
                {
                    qty--;
                    lblQty.Text = qty.ToString();

                    UpdateCartItem(id, name, price, qty, "");
                }
            };

            // Tăng số lượng
            btnPlus.Click += (s, e) =>
            {
                int qty = int.Parse(lblQty.Text);

                qty++;
                lblQty.Text = qty.ToString();

                UpdateCartItem(id, name, price, qty, "");
            };

            card.Controls.AddRange(new Control[]
            {
                picImg,
                lblName,
                lblDesc,
                lblPrice,
                btnMinus,
                lblQty,
                btnPlus
            });

            return card;
        }

        // ==========================================
        // CẬP NHẬT MỘT SẢN PHẨM
        // ==========================================
        private void UpdateCartItem(
            int id,
            string name,
            decimal price,
            int qty,
            string imgPath)
        {
            if (qty <= 0)
            {
                cartItems.Remove(id);
            }
            else
            {
                cartItems[id] = (name, price, qty, imgPath);
            }

            SyncBookingContext();
            UpdateCartSummaryUI();
        }

        // ==========================================
        // ĐỒNG BỘ GIỎ HÀNG VÀO BOOKINGCONTEXT
        // ==========================================
        private void SyncBookingContext()
        {
            if (_bookingContext == null)
                return;

            _bookingContext.SetFoodCart(cartItems);
        }

        // ==========================================
        // CẬP NHẬT GIAO DIỆN GIỎ HÀNG
        // ==========================================
        private void UpdateCartSummaryUI()
        {
            flpCartItemList.Controls.Clear();

            decimal totalAmount = 0m;

            foreach (var item in cartItems)
            {
                decimal itemTotal = item.Value.Price * item.Value.Qty;
                totalAmount += itemTotal;

                Panel row = new Panel
                {
                    Size = new Size(330, 45),
                    Margin = new Padding(0, 0, 0, 8),
                    BackColor = Color.FromArgb(32, 33, 46)
                };

                Label lblItemName = new Label
                {
                    Text = item.Value.Name,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(8, 5),
                    Size = new Size(200, 18),
                    AutoEllipsis = true
                };

                Label lblItemPrice = new Label
                {
                    Text = $"{item.Value.Qty} x {item.Value.Price.ToString("N0", new CultureInfo("vi-VN"))}đ",
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = ColorTextMuted,
                    Location = new Point(8, 24),
                    AutoSize = true
                };

                Label lblItemTotal = new Label
                {
                    Text = $"{itemTotal.ToString("N0", new CultureInfo("vi-VN"))} đ",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = ColorGold,
                    Location = new Point(220, 12),
                    Size = new Size(100, 20),
                    TextAlign = ContentAlignment.MiddleRight
                };

                row.Controls.AddRange(new Control[]
                {
                    lblItemName,
                    lblItemPrice,
                    lblItemTotal
                });

                flpCartItemList.Controls.Add(row);
            }

            lblTotalFoodPrice.Text =
                $"{totalAmount.ToString("N0", new CultureInfo("vi-VN"))} đ";

            btnContinue.Enabled = true;
            btnSkipFood.Enabled = true;
        }

        // ==========================================
        // TIẾP TỤC THANH TOÁN
        // ==========================================
        private void BtnContinue_Click(object? sender, EventArgs e)
        {
            // Kiểm tra BookingContext
            if (_bookingContext == null)
            {
                MessageBox.Show(
                    "Không tìm thấy thông tin đặt vé. Vui lòng thử lại.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            // Kiểm tra dữ liệu đặt ghế
            if (!_bookingContext.CanCheckout)
            {
                MessageBox.Show(
                    "Vui lòng hoàn thành bước chọn phim, suất chiếu và ghế.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            // Tính tổng tiền bắp nước
            decimal totalFood = cartItems.Sum(
                item => item.Value.Price * item.Value.Qty);

            // Đồng bộ giỏ hàng
            SyncBookingContext();

            // Tạo bản sao giỏ hàng để gửi sang CheckoutUC
            var foodCartCopy = new Dictionary<int, (
                string Name,
                decimal Price,
                int Qty,
                string ImgPath
            )>(cartItems);

            // Chuyển dữ liệu sang MainForm
            OnContinueToCheckout?.Invoke(
                this,
                (foodCartCopy, totalFood));
        }

        // ==========================================
        // BỎ QUA BẮP NƯỚC VÀ ĐI THẲNG ĐẾN THANH TOÁN
        // ==========================================
        private void BtnSkipFood_Click(object? sender, EventArgs e)
        {
            // Kiểm tra BookingContext
            if (_bookingContext == null)
            {
                MessageBox.Show(
                    "Không tìm thấy thông tin đặt vé. Vui lòng thử lại.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            // Kiểm tra thông tin đặt ghế
            if (!_bookingContext.CanCheckout)
            {
                MessageBox.Show(
                    "Vui lòng hoàn thành bước chọn phim, suất chiếu và ghế trước khi thanh toán.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            // Xóa toàn bộ bắp nước trong giỏ hàng
            cartItems.Clear();

            // Đồng bộ giỏ hàng rỗng vào BookingContext
            SyncBookingContext();

            // Tạo giỏ hàng rỗng để chuyển sang CheckoutUC
            var emptyFoodCart = new Dictionary<int, (
                string Name,
                decimal Price,
                int Qty,
                string ImgPath
            )>();

            // Chuyển thẳng đến thanh toán
            // Tổng tiền bắp nước = 0 đồng
            OnContinueToCheckout?.Invoke(
                this,
                (emptyFoodCart, 0m));
        }
    }
}
