using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace MeroHardwareDokan
{
    public class DashboardControl : UserControl
    {
        private Panel cardSales;
        private Panel cardPurchases;
        private Panel cardProducts;
        private Panel cardLowStock;

        private Label lblSalesVal;
        private Label lblPurchasesVal;
        private Label lblProductsVal;
        private Label lblLowStockVal;

        private DataGridView gridLowStock;
        private Panel chartPanel;

        private decimal totalSales = 0;
        private decimal totalPurchases = 0;
        private decimal totalCOGS = 0;
        private int productCount = 0;
        private int lowStockCount = 0;

        public DashboardControl()
        {
            InitializeComponent();
            LoadDashboardData();
        }

        private void InitializeComponent()
        {
            this.Font = Theme.MainFont;
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.AutoScroll = true;
            this.Size = new Size(950, 650);
            this.BackColor = Theme.Secondary;
            this.DoubleBuffered = true;

            // Welcome Header
            Label lblWelcome = new Label();
            lblWelcome.Text = $"Welcome Back, {Session.FullName}";
            lblWelcome.Location = new Point(20, 15);
            lblWelcome.AutoSize = true;
            Theme.StyleLabel(lblWelcome, Theme.TextLight, Theme.HeaderFont);
            this.Controls.Add(lblWelcome);

            Label lblRole = new Label();
            lblRole.Text = $"Role: {Session.Role} | Current Session Details";
            lblRole.Location = new Point(22, 45);
            lblRole.AutoSize = true;
            Theme.StyleLabel(lblRole, Theme.TextDark, Theme.MainFont);
            this.Controls.Add(lblRole);

            // Card Layout
            int cardW = 210;
            int cardH = 100;
            int gap = 20;

            // 1. Total Sales Card
            cardSales = Theme.CreateCard(cardW, cardH);
            cardSales.Location = new Point(20, 80);
            cardSales.BackColor = Color.FromArgb(17, 24, 39); // Deep dark
            lblSalesVal = CreateCardContent(cardSales, "TOTAL REVENUE", "Rs. 0.00", Theme.Success);
            this.Controls.Add(cardSales);

            // 2. Total Purchases Card
            cardPurchases = Theme.CreateCard(cardW, cardH);
            cardPurchases.Location = new Point(20 + cardW + gap, 80);
            cardPurchases.BackColor = Color.FromArgb(17, 24, 39);
            lblPurchasesVal = CreateCardContent(cardPurchases, "TOTAL PURCHASES", "Rs. 0.00", Theme.TextLight);
            this.Controls.Add(cardPurchases);

            // 3. Product Count Card
            cardProducts = Theme.CreateCard(cardW, cardH);
            cardProducts.Location = new Point(20 + (cardW + gap) * 2, 80);
            cardProducts.BackColor = Color.FromArgb(17, 24, 39);
            lblProductsVal = CreateCardContent(cardProducts, "TOTAL PRODUCTS", "0 Items", Theme.Accent);
            this.Controls.Add(cardProducts);

            // 4. Low Stock Warning Card
            cardLowStock = Theme.CreateCard(cardW, cardH);
            cardLowStock.Location = new Point(20 + (cardW + gap) * 3, 80);
            cardLowStock.BackColor = Color.FromArgb(17, 24, 39);
            lblLowStockVal = CreateCardContent(cardLowStock, "LOW STOCK WARNING", "0 Items", Theme.Danger);
            this.Controls.Add(cardLowStock);

            // Chart Container Panel
            Label lblChartTitle = new Label();
            lblChartTitle.Text = "Analytics Overview (Revenue vs Cost)";
            lblChartTitle.Location = new Point(20, 200);
            lblChartTitle.AutoSize = true;
            Theme.StyleLabel(lblChartTitle, Theme.TextLight, Theme.SubHeaderFont);
            this.Controls.Add(lblChartTitle);

            chartPanel = new Panel();
            chartPanel.Size = new Size(440, 390);
            chartPanel.Location = new Point(20, 230);
            chartPanel.BackColor = Color.FromArgb(17, 24, 39);
            chartPanel.Paint += ChartPanel_Paint;
            this.Controls.Add(chartPanel);

            // Low Stock Table Container
            Label lblTableTitle = new Label();
            lblTableTitle.Text = "Critical Stock Replenishment Needed";
            lblTableTitle.Location = new Point(480, 200);
            lblTableTitle.AutoSize = true;
            Theme.StyleLabel(lblTableTitle, Theme.TextLight, Theme.SubHeaderFont);
            this.Controls.Add(lblTableTitle);

            gridLowStock = new DataGridView();
            gridLowStock.Size = new Size(450, 390);
            gridLowStock.Location = new Point(480, 230);
            Theme.StyleGrid(gridLowStock);
            this.Controls.Add(gridLowStock);
        }

        private Label CreateCardContent(Panel card, string header, string initVal, Color valColor)
        {
            Label lblHeader = new Label();
            lblHeader.Text = header;
            lblHeader.Location = new Point(12, 12);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextDark, new Font("Segoe UI Semibold", 8F, FontStyle.Bold));
            card.Controls.Add(lblHeader);

            Label lblVal = new Label();
            lblVal.Text = initVal;
            lblVal.Location = new Point(12, 40);
            lblVal.AutoSize = true;
            Theme.StyleLabel(lblVal, valColor, new Font("Segoe UI", 16F, FontStyle.Bold));
            card.Controls.Add(lblVal);

            return lblVal;
        }

        private void LoadDashboardData()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    // 1. Get Sales Summary (Net Sales = Gross Sales - Refunds)
                    decimal salesRevenue = 0;
                    decimal returnedRefund = 0;

                    using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(SUM(GrandTotal), 0) FROM Sales", conn))
                    {
                        salesRevenue = (decimal)cmd.ExecuteScalar();
                    }

                    using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(SUM(TotalRefund), 0) FROM SalesReturns", conn))
                    {
                        returnedRefund = (decimal)cmd.ExecuteScalar();
                    }

                    totalSales = salesRevenue - returnedRefund;
                    lblSalesVal.Text = $"Rs. {totalSales:N2}";

                    // 2. Get Purchases Summary (calculated as total purchase cost of product in stock)
                    string purchasesQuery = "SELECT ISNULL(SUM(Stock * PurchasePrice), 0) FROM Products";

                    using (SqlCommand cmd = new SqlCommand(purchasesQuery, conn))
                    {
                        totalPurchases = (decimal)cmd.ExecuteScalar();
                        lblPurchasesVal.Text = $"Rs. {totalPurchases:N2}";
                    }

                    // 2.b Get Cost of Goods Sold (COGS) for the chart (Net COGS = Gross COGS - Resellable Return Cost)
                    decimal grossCogs = 0;
                    decimal resellableReturnCost = 0;

                    string grossCogsQuery = "SELECT ISNULL(SUM(Quantity * PurchaseCostAtSale), 0) FROM SaleDetails";

                    using (SqlCommand cmd = new SqlCommand(grossCogsQuery, conn))
                    {
                        grossCogs = (decimal)cmd.ExecuteScalar();
                    }

                    string returnCostQuery = @"
                        SELECT ISNULL(SUM(srd.Quantity * sd.PurchaseCostAtSale), 0)
                        FROM SalesReturnDetails srd
                        INNER JOIN SalesReturns sr ON srd.ReturnId = sr.Id
                        INNER JOIN SaleDetails sd ON sr.SaleId = sd.SaleId AND srd.ProductId = sd.ProductId
                        WHERE srd.ItemCondition = 'Resellable'";

                    using (SqlCommand cmd = new SqlCommand(returnCostQuery, conn))
                    {
                        resellableReturnCost = (decimal)cmd.ExecuteScalar();
                    }

                    totalCOGS = grossCogs - resellableReturnCost;

                    // 3. Get Products Count
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Products", conn))
                    {
                        productCount = (int)cmd.ExecuteScalar();
                        lblProductsVal.Text = $"{productCount} SKU(s)";
                    }

                    // 4. Get Low Stock Alert Count
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Products WHERE Stock <= MinStockLevel", conn))
                    {
                        lowStockCount = (int)cmd.ExecuteScalar();
                        lblLowStockVal.Text = $"{lowStockCount} Item(s)";
                        if (lowStockCount > 0)
                        {
                            cardLowStock.BackColor = Color.FromArgb(45, 15, 15); // subtle red bg
                        }
                    }

                    // 5. Load Low Stock List into Grid
                    using (SqlDataAdapter da = new SqlDataAdapter(@"
                        SELECT Code, Name, Stock as [Stock], MinStockLevel as [Min Level] 
                        FROM Products 
                        WHERE Stock <= MinStockLevel 
                        ORDER BY Stock ASC", conn))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        gridLowStock.DataSource = dt;

                        // Set custom column fill weights to give appropriate spacing and prevent text clipping
                        if (gridLowStock.Columns["Code"] != null) gridLowStock.Columns["Code"].FillWeight = 70;
                        if (gridLowStock.Columns["Name"] != null) gridLowStock.Columns["Name"].FillWeight = 150;
                        if (gridLowStock.Columns["Stock"] != null)
                        {
                            gridLowStock.Columns["Stock"].FillWeight = 90;
                            gridLowStock.Columns["Stock"].DefaultCellStyle.Format = "0.###";
                        }
                        if (gridLowStock.Columns["Min Level"] != null)
                        {
                            gridLowStock.Columns["Min Level"].FillWeight = 90;
                            gridLowStock.Columns["Min Level"].DefaultCellStyle.Format = "0.###";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard data: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ChartPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int w = chartPanel.Width;
            int h = chartPanel.Height;

            // Draw clean background grid lines
            Pen gridPen = new Pen(Color.FromArgb(30, 41, 59), 1);
            for (int i = 50; i < h - 50; i += 50)
            {
                g.DrawLine(gridPen, 50, i, w - 20, i);
            }

            // Draw Y and X axis
            Pen axisPen = new Pen(Theme.TextDark, 2);
            g.DrawLine(axisPen, 50, 50, 50, h - 50); // Y axis starts at 50 to avoid title overlap
            g.DrawLine(axisPen, 50, h - 50, w - 20, h - 50); // X axis

            // Draw beautiful custom bars
            decimal maxVal = Math.Max(totalSales, totalCOGS);
            if (maxVal == 0) maxVal = 1000; // prevent division by zero

            int chartH = h - 140; // Reduced from h - 100 to leave ample vertical spacing for labels
            int barW = 80;
            int barGap = 60;

            // 1. Sales Bar
            int salesBarH = (int)((totalSales / maxVal) * chartH);
            Rectangle salesRect = new Rectangle(50 + barGap, h - 50 - salesBarH, barW, salesBarH);
            using (Brush b = new SolidBrush(Theme.Success))
            {
                g.FillRectangle(b, salesRect);
            }
            
            // Dynamically center 'Revenue' label below Sales bar
            string revenueLabel = "Revenue";
            SizeF revLabelSize = g.MeasureString(revenueLabel, Theme.BoldFont);
            float revLabelX = 50 + barGap + (barW - revLabelSize.Width) / 2f;
            g.DrawString(revenueLabel, Theme.BoldFont, new SolidBrush(Theme.TextLight), revLabelX, h - 40);

            // Dynamically center Sales amount above Sales bar
            string salesText = $"Rs. {totalSales:N0}";
            SizeF salesTextSize = g.MeasureString(salesText, Theme.MainFont);
            float salesTextX = 50 + barGap + (barW - salesTextSize.Width) / 2f;
            float salesTextY = h - 50 - salesBarH - salesTextSize.Height - 5;
            g.DrawString(salesText, Theme.MainFont, new SolidBrush(Theme.TextLight), salesTextX, salesTextY);

            // 2. Cost (Goods) Bar (using Cost of Goods Sold - COGS)
            int cogsBarH = (int)((totalCOGS / maxVal) * chartH);
            Rectangle cogsRect = new Rectangle(50 + barGap + barW + barGap, h - 50 - cogsBarH, barW, cogsBarH);
            using (Brush b = new SolidBrush(Theme.Accent))
            {
                g.FillRectangle(b, cogsRect);
            }

            // Dynamically center 'Cost (Goods)' label below Cost bar
            string costLabel = "Cost (Goods)";
            SizeF costLabelSize = g.MeasureString(costLabel, Theme.BoldFont);
            float costLabelX = 50 + barGap + barW + barGap + (barW - costLabelSize.Width) / 2f;
            g.DrawString(costLabel, Theme.BoldFont, new SolidBrush(Theme.TextLight), costLabelX, h - 40);

            // Dynamically center Cost amount above Cost bar
            string cogsText = $"Rs. {totalCOGS:N0}";
            SizeF cogsTextSize = g.MeasureString(cogsText, Theme.MainFont);
            float cogsTextX = 50 + barGap + barW + barGap + (barW - cogsTextSize.Width) / 2f;
            float cogsTextY = h - 50 - cogsBarH - cogsTextSize.Height - 5;
            g.DrawString(cogsText, Theme.MainFont, new SolidBrush(Theme.TextLight), cogsTextX, cogsTextY);

            // Net Profit Label - Positioned at Y = 15 for a clean, non-overlapping header
            decimal netProfit = totalSales - totalCOGS;
            Color pColor = netProfit >= 0 ? Theme.Success : Theme.Danger;
            string pSign = netProfit >= 0 ? "+" : "";
            string pText = $"Net Performance: {pSign}Rs. {netProfit:N2}";
            g.DrawString(pText, Theme.SubHeaderFont, new SolidBrush(pColor), 50, 15);
        }
    }
}

