
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Models;
using CinemaManagement.Services;

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

        // Service lấy dữ liệu sản phẩm từ SQLite
        private readonly FoodService _foodService = new();

        private Panel pnlHeader = null!;
        private Button btnBack = null!;
        private FlowLayoutPanel flpFoodCatalog = null!;
        private Panel pnlCartSummary = null!;
        private FlowLayoutPanel flpCartItemList = null!;
        private Label lblTotalFoodPrice = null!;

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

            TicketTotalAmount = bookingContext.TicketAmount;

            // Khôi phục giỏ hàng đã lưu
            cartItems = new Dictionary<int, (
                string Name,
                decimal Price,
                int Qty,
                string ImgPath
            )>(bookingContext.FoodCart);

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

            // Nút bỏ qua bắp nước
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

            pnlCartFooter.Controls.Add(lblTotalLabel);
            pnlCartFooter.Controls.Add(lblTotalFoodPrice);
            pnlCartFooter.Controls.Add(btnContinue);
            pnlCartFooter.Controls.Add(btnSkipFood);

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
        // TẢI DANH SÁCH SẢN PHẨM TỪ SQLITE
        // ==========================================
        private void LoadFoodCatalog()
        {
            flpFoodCatalog.Controls.Clear();

            try
            {
                var products = _foodService.GetAll()
                    .Where(p => p.IsActive && p.StockQuantity > 0)
                    .ToList();

                // Loại bỏ sản phẩm không còn được bán hoặc hết hàng
                var availableIds = products.Select(p => p.Id).ToHashSet();

                foreach (int id in cartItems.Keys
                             .Where(id => !availableIds.Contains(id))
                             .ToList())
                {
                    cartItems.Remove(id);
                }

                foreach (var product in products)
                {
                    int currentQty = cartItems.TryGetValue(
                        product.Id,
                        out var cartItem)
                        ? cartItem.Qty
                        : 0;

                    // Không khôi phục số lượng vượt tồn kho
                    currentQty = Math.Min(currentQty, product.StockQuantity);

                    if (currentQty > 0)
                    {
                        cartItems[product.Id] = (
                            product.Name,
                            product.Price,
                            currentQty,
                            product.ImageUrl ?? string.Empty
                        );
                    }
                    else
                    {
                        cartItems.Remove(product.Id);
                    }

                    Panel card = CreateFoodCard(
                        product.Id,
                        product.Name,
                        product.Price,
                        product.Category,
                        product.ImageUrl ?? string.Empty,
                        product.StockQuantity,
                        currentQty
                    );

                    flpFoodCatalog.Controls.Add(card);
                }

                if (products.Count == 0)
                {
                    Label lblEmpty = new Label
                    {
                        Text = "Hiện chưa có sản phẩm bắp nước còn hàng.",
                        ForeColor = ColorTextMuted,
                        Font = new Font("Segoe UI", 11, FontStyle.Bold),
                        AutoSize = true,
                        Margin = new Padding(20)
                    };

                    flpFoodCatalog.Controls.Add(lblEmpty);
                }

                SyncBookingContext();
            }
            catch (Exception ex)
            {
                Label lblError = new Label
                {
                    Text = "Không thể tải danh sách sản phẩm từ cơ sở dữ liệu.",
                    ForeColor = Color.IndianRed,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    AutoSize = true,
                    Margin = new Padding(20)
                };

                flpFoodCatalog.Controls.Add(lblError);

                MessageBox.Show(
                    "Không thể tải danh sách sản phẩm từ cơ sở dữ liệu:\n" + ex.Message,
                    "Lỗi tải sản phẩm",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        // ==========================================
        // TẠO CARD SẢN PHẨM
        // ==========================================
        private Panel CreateFoodCard(
            int id,
            string name,
            decimal price,
            string category,
            string imgPath,
            int stockQuantity,
            int initialQty)
        {
            Panel card = new Panel
            {
                Size = new Size(240, 310),
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

            // Hỗ trợ đường dẫn ảnh cục bộ
            try
            {
                if (!string.IsNullOrWhiteSpace(imgPath) &&
                    System.IO.File.Exists(imgPath))
                {
                    using var stream = new System.IO.FileStream(
                        imgPath,
                        System.IO.FileMode.Open,
                        System.IO.FileAccess.Read,
                        System.IO.FileShare.ReadWrite
                    );

                    using var sourceImage = Image.FromStream(stream);
                    picImg.Image = new Bitmap(sourceImage);
                }
            }
            catch
            {
                // Không có ảnh hoặc ảnh lỗi thì vẫn hiển thị card
            }

            Label lblName = new Label
            {
                Text = name,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(12, 138),
                Size = new Size(216, 22),
                AutoEllipsis = true
            };

            Label lblCategory = new Label
            {
                Text = category,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = ColorTextMuted,
                Location = new Point(12, 162),
                Size = new Size(216, 20),
                AutoEllipsis = true
            };

            Label lblPrice = new Label
            {
                Text = $"{price.ToString("N0", new CultureInfo("vi-VN"))} đ",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = ColorGold,
                Location = new Point(12, 190),
                AutoSize = true
            };

            Label lblStock = new Label
            {
                Text = $"Còn lại: {stockQuantity}",
                Font = new Font("Segoe UI", 8),
                ForeColor = ColorTextMuted,
                Location = new Point(12, 218),
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
                Location = new Point(120, 260),
                Cursor = Cursors.Hand
            };

            btnMinus.FlatAppearance.BorderSize = 0;

            Label lblQty = new Label
            {
                Text = initialQty.ToString(),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(154, 260),
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
                Location = new Point(192, 260),
                Cursor = Cursors.Hand
            };

            btnPlus.FlatAppearance.BorderSize = 0;

            // Giảm số lượng
            btnMinus.Click += (s, e) =>
            {
                int qty = int.Parse(lblQty.Text);

                if (qty <= 0)
                    return;

                qty--;
                lblQty.Text = qty.ToString();

                UpdateCartItem(
                    id,
                    name,
                    price,
                    qty,
                    imgPath
                );
            };

            // Tăng số lượng và kiểm tra tồn kho
            btnPlus.Click += (s, e) =>
            {
                int qty = int.Parse(lblQty.Text);

                if (qty >= stockQuantity)
                {
                    MessageBox.Show(
                        $"Sản phẩm chỉ còn {stockQuantity} trong kho.",
                        "Thông báo tồn kho",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    return;
                }

                qty++;
                lblQty.Text = qty.ToString();

                UpdateCartItem(
                    id,
                    name,
                    price,
                    qty,
                    imgPath
                );
            };

            card.Controls.AddRange(new Control[]
            {
                picImg,
                lblName,
                lblCategory,
                lblPrice,
                lblStock,
                btnMinus,
                lblQty,
                btnPlus
            });

            return card;
        }

        // ==========================================
        // CẬP NHẬT MỘT SẢN PHẨM TRONG GIỎ
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
            if (_bookingContext == null)
            {
                MessageBox.Show(
                    "Không tìm thấy thông tin đặt vé. Vui lòng thử lại.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            if (!_bookingContext.CanCheckout)
            {
                MessageBox.Show(
                    "Vui lòng hoàn thành bước chọn phim, suất chiếu và ghế.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            decimal totalFood = cartItems.Sum(
                item => item.Value.Price * item.Value.Qty
            );

            SyncBookingContext();

            var foodCartCopy = new Dictionary<int, (
                string Name,
                decimal Price,
                int Qty,
                string ImgPath
            )>(cartItems);

            OnContinueToCheckout?.Invoke(
                this,
                (foodCartCopy, totalFood)
            );
        }

        // ==========================================
        // BỎ QUA BẮP NƯỚC VÀ ĐI THẲNG ĐẾN THANH TOÁN
        // ==========================================
        private void BtnSkipFood_Click(object? sender, EventArgs e)
        {
            if (_bookingContext == null)
            {
                MessageBox.Show(
                    "Không tìm thấy thông tin đặt vé. Vui lòng thử lại.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            if (!_bookingContext.CanCheckout)
            {
                MessageBox.Show(
                    "Vui lòng hoàn thành bước chọn phim, suất chiếu và ghế trước khi thanh toán.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            cartItems.Clear();

            SyncBookingContext();

            var emptyFoodCart = new Dictionary<int, (
                string Name,
                decimal Price,
                int Qty,
                string ImgPath
            )>();

            OnContinueToCheckout?.Invoke(
                this,
                (emptyFoodCart, 0m)
            );
        }
    }
}
