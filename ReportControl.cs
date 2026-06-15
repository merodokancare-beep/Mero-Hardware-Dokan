using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace MeroHardwareDokan
{
    public class ReportControl : UserControl
    {
        private Panel tabHeaderPanel;
        private Panel tabContentPanel;
        private Panel panelDailySales;
        private Panel panelProfitLoss;
        private Panel panelPurchaseInward;

        private Button btnTabSales;
        private Button btnTabPL;
        private Button btnTabPurchases;
        
        // Sales Report Tab controls
        private DateTimePicker salesFromDate;
        private DateTimePicker salesToDate;
        private DataGridView gridSalesReport;
        private Button btnSalesSearch;
        private Label lblSalesSummary;
        private Button btnReprintBill;

        // Print components for reprint duplicate copy
        private PrintDocument reprintDoc;
        private PrintPreviewDialog reprintPreviewDlg;
        private int printSaleId = 0;

        // Profit/Loss Tab controls
        private DateTimePicker plFromDate;
        private DateTimePicker plToDate;
        private Button btnCalculatePL;
        private Panel cardRevenue;
        private Panel cardCOGS;
        private Panel cardNetProfit;
        private Label lblRevenueVal;
        private Label lblCOGSVal;
        private Label lblNetVal;
        private Label lblPLSummary;

        // Purchase History Tab controls
        private DateTimePicker purchaseFromDate;
        private DateTimePicker purchaseToDate;
        private ComboBox comboFilterSupplier;
        private ComboBox comboPurchaseReportType;
        private DataGridView gridPurchaseReport;
        private Button btnPurchaseSearch;
        private Label lblPurchaseSummary;

        // Price History Tracker Tab controls
        private Panel panelPriceHistory;
        private Button btnTabPriceHistory;
        private ComboBox comboHistoryProduct;
        private DataGridView gridPriceHistory;
        private Label lblCostTrendVal;
        private Label lblSalesTrendVal;
        private Label lblCostCompareTitle;
        private Label lblSalesCompareTitle;
        private Panel cardCostTrend;
        private Panel cardSalesTrend;

        public ReportControl()
        {
            InitializeComponent();
            LoadDailySales();
            CalculateProfitLoss();
            LoadSuppliersFilterDropdown();
            LoadPurchaseHistory();
            LoadPriceHistoryProductsDropdown();
            LoadPriceHistoryLog();
        }

        private void InitializeComponent()
        {
            this.Font = Theme.MainFont;
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.AutoScroll = true;
            this.Size = new Size(950, 650);
            this.BackColor = Theme.Secondary;

            // Page Header
            Label lblHeader = new Label();
            lblHeader.Text = "Reports & Business intelligence Center";
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
            this.Controls.Add(lblHeader);

            // Top Header Panel for Buttons
            tabHeaderPanel = new Panel();
            tabHeaderPanel.Location = new Point(20, 65);
            tabHeaderPanel.Size = new Size(910, 45);
            tabHeaderPanel.BackColor = Color.Transparent;
            tabHeaderPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(tabHeaderPanel);

            bool isAdmin = Session.Role == "Admin";

            // Tab Buttons
            btnTabSales = new Button();
            btnTabSales.Text = "Daily Sales Register";
            btnTabSales.Size = new Size(190, 40);
            btnTabSales.Location = new Point(0, 0);
            btnTabSales.Click += (s, e) => ShowTab(panelDailySales, btnTabSales);
            tabHeaderPanel.Controls.Add(btnTabSales);

            int currentX = 195;

            if (isAdmin)
            {
                btnTabPL = new Button();
                btnTabPL.Text = "Profit & Loss Analytics";
                btnTabPL.Size = new Size(200, 40);
                btnTabPL.Location = new Point(currentX, 0);
                btnTabPL.Click += (s, e) => ShowTab(panelProfitLoss, btnTabPL);
                tabHeaderPanel.Controls.Add(btnTabPL);
                currentX += 205;

                btnTabPurchases = new Button();
                btnTabPurchases.Text = "Purchase Inward Register";
                btnTabPurchases.Size = new Size(230, 40);
                btnTabPurchases.Location = new Point(currentX, 0);
                btnTabPurchases.Click += (s, e) => ShowTab(panelPurchaseInward, btnTabPurchases);
                tabHeaderPanel.Controls.Add(btnTabPurchases);
                currentX += 235;
            }

            btnTabPriceHistory = new Button();
            btnTabPriceHistory.Text = "Price History Tracker";
            btnTabPriceHistory.Size = new Size(220, 40);
            btnTabPriceHistory.Location = new Point(currentX, 0);
            btnTabPriceHistory.Click += (s, e) => ShowTab(panelPriceHistory, btnTabPriceHistory);
            tabHeaderPanel.Controls.Add(btnTabPriceHistory);

            // Main Tab Content Panel
            tabContentPanel = new Panel();
            tabContentPanel.Location = new Point(20, 115);
            tabContentPanel.Size = new Size(910, 510);
            tabContentPanel.BackColor = Theme.Secondary;
            tabContentPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            this.Controls.Add(tabContentPanel);

            // Sub Panels for actual content (using DockStyle.Fill for responsive auto-stretching)
            panelDailySales = new Panel();
            panelDailySales.Dock = DockStyle.Fill;
            panelDailySales.BackColor = Theme.Secondary;
            tabContentPanel.Controls.Add(panelDailySales);

            panelProfitLoss = new Panel();
            panelProfitLoss.Dock = DockStyle.Fill;
            panelProfitLoss.BackColor = Theme.Secondary;
            tabContentPanel.Controls.Add(panelProfitLoss);

            panelPurchaseInward = new Panel();
            panelPurchaseInward.Dock = DockStyle.Fill;
            panelPurchaseInward.BackColor = Theme.Secondary;
            tabContentPanel.Controls.Add(panelPurchaseInward);

            panelPriceHistory = new Panel();
            panelPriceHistory.Dock = DockStyle.Fill;
            panelPriceHistory.BackColor = Theme.Secondary;
            tabContentPanel.Controls.Add(panelPriceHistory);

            // Initialize content inside the panels
            InitializeSalesTab(panelDailySales);
            InitializePLTab(panelProfitLoss);
            InitializePurchaseTab(panelPurchaseInward);
            InitializePriceHistoryTab(panelPriceHistory);

            // Default view: Show Daily Sales tab
            ShowTab(panelDailySales, btnTabSales);

            // Setup Reprint Elements
            reprintDoc = new PrintDocument();
            reprintDoc.PrintPage += ReprintDoc_PrintPage;
            reprintPreviewDlg = new PrintPreviewDialog();
            reprintPreviewDlg.Document = reprintDoc;
            reprintPreviewDlg.Size = new Size(600, 700);
        }

        private void ShowTab(Panel selectedPanel, Button activeBtn)
        {
            panelDailySales.Visible = false;
            panelProfitLoss.Visible = false;
            panelPurchaseInward.Visible = false;
            panelPriceHistory.Visible = false;

            selectedPanel.Visible = true;

            StyleTabButton(btnTabSales, btnTabSales == activeBtn);
            if (btnTabPL != null) StyleTabButton(btnTabPL, btnTabPL == activeBtn);
            if (btnTabPurchases != null) StyleTabButton(btnTabPurchases, btnTabPurchases == activeBtn);
            StyleTabButton(btnTabPriceHistory, btnTabPriceHistory == activeBtn);
        }

        private void StyleTabButton(Button btn, bool isActive)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.Font = Theme.BoldFont;
            btn.Cursor = Cursors.Hand;
            btn.Padding = new Padding(8, 4, 8, 4);

            if (isActive)
            {
                btn.BackColor = Theme.Accent; // Indigo Accent
                btn.ForeColor = Theme.TextLight;
                btn.FlatAppearance.BorderSize = 0; // Seamless borderless look
                btn.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
            }
            else
            {
                btn.BackColor = Color.FromArgb(17, 24, 39); // Match card bg depth
                btn.ForeColor = Theme.TextDark; // Slate 400 (Muted)
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = Theme.AlternateRow; // Slate 700
                btn.FlatAppearance.MouseOverBackColor = Theme.Secondary; // Slate 800 hover
            }
        }

        private void InitializeSalesTab(Panel page)
        {
            // Filters
            Label lblFrom = new Label();
            lblFrom.Text = "From Date:";
            lblFrom.Location = new Point(20, 10);
            lblFrom.AutoSize = true;
            Theme.StyleLabel(lblFrom, Theme.TextLight, Theme.BoldFont);
            page.Controls.Add(lblFrom);

            salesFromDate = new DateTimePicker();
            salesFromDate.Size = new Size(150, 28);
            salesFromDate.Location = new Point(20, 32);
            salesFromDate.Format = DateTimePickerFormat.Short;
            salesFromDate.Value = DateTime.Today.AddDays(-7);
            page.Controls.Add(salesFromDate);

            Label lblTo = new Label();
            lblTo.Text = "To Date:";
            lblTo.Location = new Point(190, 10);
            lblTo.AutoSize = true;
            Theme.StyleLabel(lblTo, Theme.TextLight, Theme.BoldFont);
            page.Controls.Add(lblTo);

            salesToDate = new DateTimePicker();
            salesToDate.Size = new Size(150, 28);
            salesToDate.Location = new Point(190, 32);
            salesToDate.Format = DateTimePickerFormat.Short;
            salesToDate.Value = DateTime.Today;
            page.Controls.Add(salesToDate);

            btnSalesSearch = new Button();
            btnSalesSearch.Text = "🔍 Generate Log";
            btnSalesSearch.Size = new Size(200, 36); // Enlarge width for distinct visibility and prevent truncation
            btnSalesSearch.Location = new Point(360, 28); // Perfectly centered vertical alignment
            Theme.StylePrimaryButton(btnSalesSearch);
            btnSalesSearch.Click += (s, e) => LoadDailySales();
            page.Controls.Add(btnSalesSearch);

            // GridView
            gridSalesReport = new DataGridView();
            gridSalesReport.Size = new Size(870, 380);
            gridSalesReport.Location = new Point(20, 75);
            gridSalesReport.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridSalesReport);
            page.Controls.Add(gridSalesReport);

            // Summary Label
            lblSalesSummary = new Label();
            lblSalesSummary.Text = "Invoices: 0  •  Discount: Rs. 0.00  •  VAT: Rs. 0.00  •  Revenue Collected: Rs. 0.00";
            lblSalesSummary.AutoSize = false;
            lblSalesSummary.Size = new Size(630, 30);
            lblSalesSummary.Location = new Point(260, 460);
            lblSalesSummary.TextAlign = ContentAlignment.MiddleRight;
            Theme.StyleLabel(lblSalesSummary, Theme.TextLight, Theme.SubHeaderFont);
            lblSalesSummary.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            page.Controls.Add(lblSalesSummary);

            // Reprint Duplicate Bill Button
            btnReprintBill = new Button();
            btnReprintBill.Text = "🖨️ Reprint Duplicate";
            btnReprintBill.Size = new Size(220, 36);
            btnReprintBill.Location = new Point(20, 460);
            btnReprintBill.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            Theme.StylePrimaryButton(btnReprintBill);
            btnReprintBill.Click += BtnReprintBill_Click;
            page.Controls.Add(btnReprintBill);
        }

        private void InitializePLTab(Panel page)
        {
            // Filters
            Label lblFrom = new Label();
            lblFrom.Text = "From Date:";
            lblFrom.Location = new Point(20, 10);
            lblFrom.AutoSize = true;
            Theme.StyleLabel(lblFrom, Theme.TextLight, Theme.BoldFont);
            page.Controls.Add(lblFrom);

            plFromDate = new DateTimePicker();
            plFromDate.Size = new Size(150, 28);
            plFromDate.Location = new Point(20, 32);
            plFromDate.Format = DateTimePickerFormat.Short;
            plFromDate.Value = DateTime.Today.AddDays(-30);
            page.Controls.Add(plFromDate);

            Label lblTo = new Label();
            lblTo.Text = "To Date:";
            lblTo.Location = new Point(190, 10);
            lblTo.AutoSize = true;
            Theme.StyleLabel(lblTo, Theme.TextLight, Theme.BoldFont);
            page.Controls.Add(lblTo);

            plToDate = new DateTimePicker();
            plToDate.Size = new Size(150, 28);
            plToDate.Location = new Point(190, 32);
            plToDate.Format = DateTimePickerFormat.Short;
            plToDate.Value = DateTime.Today;
            page.Controls.Add(plToDate);

            btnCalculatePL = new Button();
            btnCalculatePL.Text = "📊 Compute Analytics";
            btnCalculatePL.Size = new Size(210, 36); // Enlarge for consistent styling and alignment
            btnCalculatePL.Location = new Point(360, 28); // Align horizontally and vertically with other tabs
            Theme.StylePrimaryButton(btnCalculatePL);
            btnCalculatePL.Click += (s, e) => CalculateProfitLoss();
            page.Controls.Add(btnCalculatePL);

            // Responsive Layout Table for Cards
            TableLayoutPanel layoutCards = new TableLayoutPanel();
            layoutCards.Location = new Point(20, 85);
            layoutCards.Size = new Size(870, 130);
            layoutCards.ColumnCount = 3;
            layoutCards.RowCount = 1;
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            layoutCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            layoutCards.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            layoutCards.BackColor = Color.Transparent;
            page.Controls.Add(layoutCards);

            // 1. Total Revenue Card
            cardRevenue = Theme.CreateCard(270, 120);
            cardRevenue.Dock = DockStyle.Fill;
            cardRevenue.Margin = new Padding(0, 0, 15, 0);
            cardRevenue.BackColor = Color.FromArgb(17, 24, 39);
            lblRevenueVal = CreatePLCardContent(cardRevenue, "TOTAL REVENUE (RETAIL SALES)", "Rs. 0.00", Theme.TextLight);
            layoutCards.Controls.Add(cardRevenue, 0, 0);

            // 2. Total COGS Card
            cardCOGS = Theme.CreateCard(270, 120);
            cardCOGS.Dock = DockStyle.Fill;
            cardCOGS.Margin = new Padding(15, 0, 15, 0);
            cardCOGS.BackColor = Color.FromArgb(17, 24, 39);
            lblCOGSVal = CreatePLCardContent(cardCOGS, "COST OF GOODS SOLD (COGS)", "Rs. 0.00", Theme.TextDark);
            layoutCards.Controls.Add(cardCOGS, 1, 0);

            // 3. Net Performance Card
            cardNetProfit = Theme.CreateCard(270, 120);
            cardNetProfit.Dock = DockStyle.Fill;
            cardNetProfit.Margin = new Padding(15, 0, 0, 0);
            cardNetProfit.BackColor = Color.FromArgb(17, 24, 39);
            lblNetVal = CreatePLCardContent(cardNetProfit, "NET MARGIN (PROFIT / LOSS)", "Rs. 0.00", Theme.Success);
            layoutCards.Controls.Add(cardNetProfit, 2, 0);

            // Detailed Analytics Explanation
            lblPLSummary = new Label();
            lblPLSummary.Text = "P&L Summary will appear above. Sales and Costs are filtered based on the date range defined.";
            lblPLSummary.Location = new Point(20, 235);
            lblPLSummary.Size = new Size(870, 250);
            lblPLSummary.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleLabel(lblPLSummary, Theme.TextDark, Theme.MainFont);
            page.Controls.Add(lblPLSummary);
        }

        private Label CreatePLCardContent(Panel card, string header, string initVal, Color valColor)
        {
            Label lblHeader = new Label();
            lblHeader.Text = header;
            lblHeader.Location = new Point(12, 12);
            lblHeader.AutoSize = true;
            lblHeader.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            Theme.StyleLabel(lblHeader, Theme.TextDark, new Font("Segoe UI Semibold", 8F, FontStyle.Bold));
            card.Controls.Add(lblHeader);

            Label lblVal = new Label();
            lblVal.Text = initVal;
            lblVal.Location = new Point(12, 38);
            lblVal.Size = new Size(card.Width - 24, 60);
            lblVal.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            Theme.StyleLabel(lblVal, valColor, new Font("Segoe UI", 18F, FontStyle.Bold));
            card.Controls.Add(lblVal);

            return lblVal;
        }

        private void LoadDailySales()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT s.InvoiceNumber as [Invoice No], s.SaleDate as [Sale Date], c.Name as [Customer],
                               s.SubTotal as [SubTotal], s.Discount as [Discount], s.Tax as [Tax], 
                               s.GrandTotal as [Grand Total], s.PaidAmount as [Paid Amount], s.DueAmount as [Due Amount],
                               s.PaymentMethod as [Pay Mode]
                        FROM Sales s
                        LEFT JOIN Customers c ON s.CustomerId = c.Id
                        WHERE CAST(s.SaleDate as DATE) BETWEEN @from AND @to
                        ORDER BY s.SaleDate DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@from", salesFromDate.Value.Date);
                        cmd.Parameters.AddWithValue("@to", salesToDate.Value.Date);

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridSalesReport.DataSource = dt;

                            // Apply custom column weights and formats
                            if (gridSalesReport.Columns["SubTotal"] != null) gridSalesReport.Columns["SubTotal"].DefaultCellStyle.Format = "N2";
                            if (gridSalesReport.Columns["Discount"] != null) gridSalesReport.Columns["Discount"].DefaultCellStyle.Format = "N2";
                            if (gridSalesReport.Columns["Tax"] != null) gridSalesReport.Columns["Tax"].DefaultCellStyle.Format = "N2";
                            if (gridSalesReport.Columns["Grand Total"] != null) gridSalesReport.Columns["Grand Total"].DefaultCellStyle.Format = "N2";
                            if (gridSalesReport.Columns["Paid Amount"] != null) gridSalesReport.Columns["Paid Amount"].DefaultCellStyle.Format = "N2";
                            if (gridSalesReport.Columns["Due Amount"] != null) gridSalesReport.Columns["Due Amount"].DefaultCellStyle.Format = "N2";

                            if (gridSalesReport.Columns["Invoice No"] != null) gridSalesReport.Columns["Invoice No"].FillWeight = 85;
                            if (gridSalesReport.Columns["Sale Date"] != null) gridSalesReport.Columns["Sale Date"].FillWeight = 100;
                            if (gridSalesReport.Columns["Customer"] != null) gridSalesReport.Columns["Customer"].FillWeight = 120;
                            if (gridSalesReport.Columns["SubTotal"] != null) gridSalesReport.Columns["SubTotal"].FillWeight = 75;
                            if (gridSalesReport.Columns["Discount"] != null) gridSalesReport.Columns["Discount"].FillWeight = 70;
                            if (gridSalesReport.Columns["Tax"] != null) gridSalesReport.Columns["Tax"].FillWeight = 65;
                            if (gridSalesReport.Columns["Grand Total"] != null) gridSalesReport.Columns["Grand Total"].FillWeight = 80;
                            if (gridSalesReport.Columns["Paid Amount"] != null) gridSalesReport.Columns["Paid Amount"].FillWeight = 80;
                            if (gridSalesReport.Columns["Due Amount"] != null) gridSalesReport.Columns["Due Amount"].FillWeight = 80;
                            if (gridSalesReport.Columns["Pay Mode"] != null) gridSalesReport.Columns["Pay Mode"].FillWeight = 70;
                        }
                    }

                    // Compute Summary Numbers
                    string sumQuery = @"
                        SELECT COUNT(*), ISNULL(SUM(Discount), 0), ISNULL(SUM(Tax), 0), ISNULL(SUM(GrandTotal), 0)
                        FROM Sales
                        WHERE CAST(SaleDate as DATE) BETWEEN @from AND @to";

                    using (SqlCommand cmd = new SqlCommand(sumQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@from", salesFromDate.Value.Date);
                        cmd.Parameters.AddWithValue("@to", salesToDate.Value.Date);

                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                int count = r.GetInt32(0);
                                decimal discountTotal = r.GetDecimal(1);
                                decimal taxTotal = r.GetDecimal(2);
                                decimal grandTotal = r.GetDecimal(3);

                                lblSalesSummary.Text = $"Invoices: {count}  •  Discount: Rs. {discountTotal:N2}  •  VAT: Rs. {taxTotal:N2}  •  Revenue Collected: Rs. {grandTotal:N2}";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading sales logs: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CalculateProfitLoss()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    DateTime fromDate = plFromDate.Value.Date;
                    DateTime toDate = plToDate.Value.Date;

                    // 1. Get Revenue (Sum of Sales GrandTotal minus Sales Returns TotalRefund)
                    decimal salesRevenue = 0;
                    decimal returnedRefund = 0;
                    
                    using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(SUM(GrandTotal), 0) FROM Sales WHERE CAST(SaleDate as DATE) BETWEEN @from AND @to", conn))
                    {
                        cmd.Parameters.AddWithValue("@from", fromDate);
                        cmd.Parameters.AddWithValue("@to", toDate);
                        salesRevenue = (decimal)cmd.ExecuteScalar();
                    }

                    using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(SUM(TotalRefund), 0) FROM SalesReturns WHERE CAST(ReturnDate as DATE) BETWEEN @from AND @to", conn))
                    {
                        cmd.Parameters.AddWithValue("@from", fromDate);
                        cmd.Parameters.AddWithValue("@to", toDate);
                        returnedRefund = (decimal)cmd.ExecuteScalar();
                    }

                    decimal revenue = salesRevenue - returnedRefund;
                    lblRevenueVal.Text = $"Rs. {revenue:N2}";

                    // 2. Get Cost of Goods Sold (COGS)
                    // Calculate COGS by multiplying sold quantity by its historical purchase cost, and subtract the cost of resellable returns
                    decimal grossCogs = 0;
                    decimal resellableReturnCost = 0;

                    string grossCogsQuery = @"
                        SELECT ISNULL(SUM(sd.Quantity * sd.PurchaseCostAtSale), 0)
                        FROM SaleDetails sd
                        INNER JOIN Sales s ON sd.SaleId = s.Id
                        WHERE CAST(s.SaleDate as DATE) BETWEEN @from AND @to";

                    using (SqlCommand cmd = new SqlCommand(grossCogsQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@from", fromDate);
                        cmd.Parameters.AddWithValue("@to", toDate);
                        grossCogs = (decimal)cmd.ExecuteScalar();
                    }

                    string returnCostQuery = @"
                        SELECT ISNULL(SUM(srd.Quantity * sd.PurchaseCostAtSale), 0)
                        FROM SalesReturnDetails srd
                        INNER JOIN SalesReturns sr ON srd.ReturnId = sr.Id
                        INNER JOIN SaleDetails sd ON sr.SaleId = sd.SaleId AND srd.ProductId = sd.ProductId
                        WHERE srd.ItemCondition = 'Resellable' 
                          AND CAST(sr.ReturnDate as DATE) BETWEEN @from AND @to";

                    using (SqlCommand cmd = new SqlCommand(returnCostQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@from", fromDate);
                        cmd.Parameters.AddWithValue("@to", toDate);
                        resellableReturnCost = (decimal)cmd.ExecuteScalar();
                    }

                    decimal cogs = grossCogs - resellableReturnCost;
                    lblCOGSVal.Text = $"Rs. {cogs:N2}";

                    // 3. Net Margin Performance
                    decimal netPerformance = revenue - cogs;
                    lblNetVal.Text = $"Rs. {netPerformance:N2}";

                    if (netPerformance >= 0)
                    {
                        lblNetVal.ForeColor = Theme.Success;
                        cardNetProfit.BackColor = Color.FromArgb(15, 35, 20); // Subtle green highlight
                    }
                    else
                    {
                        lblNetVal.ForeColor = Theme.Danger;
                        cardNetProfit.BackColor = Color.FromArgb(45, 15, 15); // Subtle red highlight
                    }

                    // 4. Details breakdown text
                    decimal marginPercent = revenue > 0 ? (netPerformance / revenue) * 100 : 0;
                    lblPLSummary.Text = $@"--- Profit & Loss Statement (Analytica Summary) ---

Date Filter Period: {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}

1. Revenue Summary:
   - Gross Customer Sales Billing: Rs. {salesRevenue:N2}
   - Customer Returns & Refunds Issued: Rs. {returnedRefund:N2}
   - NET OPERATING REVENUE: Rs. {revenue:N2}

2. Cost of Goods Sold (COGS):
   - Gross Cost of Sold Goods: Rs. {grossCogs:N2}
   - Deductions for Resellable Returns: Rs. {resellableReturnCost:N2}
   - NET COST OF GOODS SOLD (COGS): Rs. {cogs:N2}

3. Net Earnings Performance:
   - Operating Net Profit / Loss Margin: Rs. {netPerformance:N2}
   - Net Profit Margin Index: {marginPercent:F2}% 

[Instruction] Gross sales billing represents customer checkouts. Returns & Refunds represent money refunded to customers. Resellable returns deduct from COGS, whereas damaged returns remain in COGS as a pure inventory loss. Positive net margin indicates healthy shop performance.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error calculating Profit & Loss: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializePurchaseTab(Panel page)
        {
            // Filters
            Label lblFrom = new Label();
            lblFrom.Text = "From Date:";
            lblFrom.Location = new Point(20, 10);
            lblFrom.AutoSize = true;
            Theme.StyleLabel(lblFrom, Theme.TextLight, Theme.BoldFont);
            page.Controls.Add(lblFrom);

            purchaseFromDate = new DateTimePicker();
            purchaseFromDate.Size = new Size(120, 28);
            purchaseFromDate.Location = new Point(20, 32);
            purchaseFromDate.Format = DateTimePickerFormat.Short;
            purchaseFromDate.Value = DateTime.Today.AddDays(-7);
            page.Controls.Add(purchaseFromDate);

            Label lblTo = new Label();
            lblTo.Text = "To Date:";
            lblTo.Location = new Point(160, 10);
            lblTo.AutoSize = true;
            Theme.StyleLabel(lblTo, Theme.TextLight, Theme.BoldFont);
            page.Controls.Add(lblTo);

            purchaseToDate = new DateTimePicker();
            purchaseToDate.Size = new Size(120, 28);
            purchaseToDate.Location = new Point(160, 32);
            purchaseToDate.Format = DateTimePickerFormat.Short;
            purchaseToDate.Value = DateTime.Today;
            page.Controls.Add(purchaseToDate);

            Label lblSupp = new Label();
            lblSupp.Text = "Supplier:";
            lblSupp.Location = new Point(300, 10);
            lblSupp.AutoSize = true;
            Theme.StyleLabel(lblSupp, Theme.TextLight, Theme.BoldFont);
            page.Controls.Add(lblSupp);

            comboFilterSupplier = new ComboBox();
            comboFilterSupplier.Size = new Size(180, 28);
            comboFilterSupplier.Location = new Point(300, 32);
            comboFilterSupplier.DropDownStyle = ComboBoxStyle.DropDownList;
            Theme.StyleComboBox(comboFilterSupplier);
            comboFilterSupplier.BackColor = Theme.Primary; 
            page.Controls.Add(comboFilterSupplier);

            Label lblType = new Label();
            lblType.Text = "Report Type:";
            lblType.Location = new Point(500, 10);
            lblType.AutoSize = true;
            Theme.StyleLabel(lblType, Theme.TextLight, Theme.BoldFont);
            page.Controls.Add(lblType);

            comboPurchaseReportType = new ComboBox();
            comboPurchaseReportType.Size = new Size(180, 28);
            comboPurchaseReportType.Location = new Point(500, 32);
            comboPurchaseReportType.DropDownStyle = ComboBoxStyle.DropDownList;
            Theme.StyleComboBox(comboPurchaseReportType);
            comboPurchaseReportType.BackColor = Theme.Primary;
            comboPurchaseReportType.Items.AddRange(new string[] { "Invoice Summary", "Product-wise History", "Category-wise History" });
            comboPurchaseReportType.SelectedIndex = 0;
            page.Controls.Add(comboPurchaseReportType);

            btnPurchaseSearch = new Button();
            btnPurchaseSearch.Text = "🔍 Generate Log";
            btnPurchaseSearch.Size = new Size(200, 36); // Enlarge width for distinct visibility and prevent truncation
            btnPurchaseSearch.Location = new Point(700, 28); // Perfectly centered vertical alignment
            Theme.StylePrimaryButton(btnPurchaseSearch);
            btnPurchaseSearch.Click += (s, e) => LoadPurchaseHistory();
            page.Controls.Add(btnPurchaseSearch);

            // GridView
            gridPurchaseReport = new DataGridView();
            gridPurchaseReport.Size = new Size(870, 380);
            gridPurchaseReport.Location = new Point(20, 75);
            gridPurchaseReport.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridPurchaseReport);
            page.Controls.Add(gridPurchaseReport);

            // Summary Label
            lblPurchaseSummary = new Label();
            lblPurchaseSummary.Text = "Inward Invoices: 0  •  Total Valuation: Rs. 0.00";
            lblPurchaseSummary.AutoSize = false;
            lblPurchaseSummary.Size = new Size(870, 30);
            lblPurchaseSummary.Location = new Point(20, 460);
            lblPurchaseSummary.TextAlign = ContentAlignment.MiddleRight;
            Theme.StyleLabel(lblPurchaseSummary, Theme.TextLight, Theme.SubHeaderFont);
            lblPurchaseSummary.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            page.Controls.Add(lblPurchaseSummary);
        }

        private void LoadSuppliersFilterDropdown()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT Id, Name FROM Suppliers ORDER BY Name ASC", conn))
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);

                            DataRow newRow = dt.NewRow();
                            newRow["Id"] = -1;
                            newRow["Name"] = "-- All Suppliers --";
                            dt.Rows.InsertAt(newRow, 0);

                            comboFilterSupplier.DataSource = dt;
                            comboFilterSupplier.DisplayMember = "Name";
                            comboFilterSupplier.ValueMember = "Id";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading suppliers filter list: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadPurchaseHistory()
        {
            try
            {
                int supplierId = -1;
                if (comboFilterSupplier.SelectedValue != null)
                {
                    if (comboFilterSupplier.SelectedValue is int)
                    {
                        supplierId = (int)comboFilterSupplier.SelectedValue;
                    }
                    else if (comboFilterSupplier.SelectedValue is DataRowView drv)
                    {
                        supplierId = (int)drv["Id"];
                    }
                }

                string reportType = comboPurchaseReportType?.SelectedItem?.ToString() ?? "Invoice Summary";

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    string query = "";
                    string sumQuery = "";

                    if (reportType == "Invoice Summary")
                    {
                        query = @"
                            SELECT p.PurchaseNumber as [Purchase No], 
                                   p.PurchaseDate as [Purchase Date], 
                                   s.Name as [Supplier], 
                                   p.TotalAmount as [Total Cost], 
                                   u.FullName as [Created By]
                            FROM Purchases p
                            LEFT JOIN Suppliers s ON p.SupplierId = s.Id
                            LEFT JOIN Users u ON p.CreatedBy = u.Id
                            WHERE CAST(p.PurchaseDate as DATE) BETWEEN @from AND @to";

                        if (supplierId != -1)
                        {
                            query += " AND p.SupplierId = @supplierId";
                        }

                        query += " ORDER BY p.PurchaseDate DESC";

                        sumQuery = @"
                            SELECT COUNT(*), ISNULL(SUM(TotalAmount), 0)
                            FROM Purchases
                            WHERE CAST(PurchaseDate as DATE) BETWEEN @from AND @to";

                        if (supplierId != -1)
                        {
                            sumQuery += " AND SupplierId = @supplierId";
                        }
                    }
                    else if (reportType == "Product-wise History")
                    {
                        query = @"
                            SELECT prod.Code as [Product Code],
                                   prod.Name as [Product Name],
                                   prod.Category as [Category],
                                   SUM(pd.Quantity) as [Qty Purchased],
                                   CAST(AVG(pd.PurchasePrice) AS DECIMAL(18,2)) as [Avg Price],
                                   SUM(pd.Quantity * pd.PurchasePrice) as [Total Investment]
                            FROM PurchaseDetails pd
                            INNER JOIN Purchases p ON pd.PurchaseId = p.Id
                            INNER JOIN Products prod ON pd.ProductId = prod.Id
                            WHERE CAST(p.PurchaseDate as DATE) BETWEEN @from AND @to";

                        if (supplierId != -1)
                        {
                            query += " AND p.SupplierId = @supplierId";
                        }

                        query += " GROUP BY prod.Code, prod.Name, prod.Category ORDER BY [Total Investment] DESC";

                        sumQuery = @"
                            SELECT COUNT(DISTINCT pd.ProductId), ISNULL(SUM(pd.Quantity * pd.PurchasePrice), 0)
                            FROM PurchaseDetails pd
                            INNER JOIN Purchases p ON pd.PurchaseId = p.Id
                            WHERE CAST(p.PurchaseDate as DATE) BETWEEN @from AND @to";

                        if (supplierId != -1)
                        {
                            sumQuery += " AND p.SupplierId = @supplierId";
                        }
                    }
                    else if (reportType == "Category-wise History")
                    {
                        query = @"
                            SELECT prod.Category as [Category],
                                   SUM(pd.Quantity) as [Total Qty Purchased],
                                   SUM(pd.Quantity * pd.PurchasePrice) as [Total Investment]
                            FROM PurchaseDetails pd
                            INNER JOIN Purchases p ON pd.PurchaseId = p.Id
                            INNER JOIN Products prod ON pd.ProductId = prod.Id
                            WHERE CAST(p.PurchaseDate as DATE) BETWEEN @from AND @to";

                        if (supplierId != -1)
                        {
                            query += " AND p.SupplierId = @supplierId";
                        }

                        query += " GROUP BY prod.Category ORDER BY [Total Investment] DESC";

                        sumQuery = @"
                            SELECT COUNT(DISTINCT prod.Category), ISNULL(SUM(pd.Quantity * pd.PurchasePrice), 0)
                            FROM PurchaseDetails pd
                            INNER JOIN Purchases p ON pd.PurchaseId = p.Id
                            INNER JOIN Products prod ON pd.ProductId = prod.Id
                            WHERE CAST(p.PurchaseDate as DATE) BETWEEN @from AND @to";

                        if (supplierId != -1)
                        {
                            sumQuery += " AND p.SupplierId = @supplierId";
                        }
                    }

                    // Populate Grid
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@from", purchaseFromDate.Value.Date);
                        cmd.Parameters.AddWithValue("@to", purchaseToDate.Value.Date);
                        if (supplierId != -1)
                        {
                            cmd.Parameters.AddWithValue("@supplierId", supplierId);
                        }

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridPurchaseReport.DataSource = dt;

                            // Apply custom column weights and formats depending on report type
                            if (reportType == "Invoice Summary")
                            {
                                if (gridPurchaseReport.Columns["Total Cost"] != null)
                                {
                                    gridPurchaseReport.Columns["Total Cost"].DefaultCellStyle.Format = "N2";
                                    gridPurchaseReport.Columns["Total Cost"].FillWeight = 100;
                                }
                                if (gridPurchaseReport.Columns["Purchase No"] != null) gridPurchaseReport.Columns["Purchase No"].FillWeight = 100;
                                if (gridPurchaseReport.Columns["Purchase Date"] != null) gridPurchaseReport.Columns["Purchase Date"].FillWeight = 120;
                                if (gridPurchaseReport.Columns["Supplier"] != null) gridPurchaseReport.Columns["Supplier"].FillWeight = 150;
                                if (gridPurchaseReport.Columns["Created By"] != null) gridPurchaseReport.Columns["Created By"].FillWeight = 100;
                            }
                            else if (reportType == "Product-wise History")
                            {
                                if (gridPurchaseReport.Columns["Avg Price"] != null)
                                {
                                    gridPurchaseReport.Columns["Avg Price"].DefaultCellStyle.Format = "N2";
                                    gridPurchaseReport.Columns["Avg Price"].FillWeight = 80;
                                }
                                if (gridPurchaseReport.Columns["Total Investment"] != null)
                                {
                                    gridPurchaseReport.Columns["Total Investment"].DefaultCellStyle.Format = "N2";
                                    gridPurchaseReport.Columns["Total Investment"].FillWeight = 100;
                                }
                                if (gridPurchaseReport.Columns["Product Code"] != null) gridPurchaseReport.Columns["Product Code"].FillWeight = 80;
                                if (gridPurchaseReport.Columns["Product Name"] != null) gridPurchaseReport.Columns["Product Name"].FillWeight = 180;
                                if (gridPurchaseReport.Columns["Category"] != null) gridPurchaseReport.Columns["Category"].FillWeight = 90;
                                if (gridPurchaseReport.Columns["Qty Purchased"] != null) gridPurchaseReport.Columns["Qty Purchased"].FillWeight = 70;
                            }
                            else if (reportType == "Category-wise History")
                            {
                                if (gridPurchaseReport.Columns["Total Investment"] != null)
                                {
                                    gridPurchaseReport.Columns["Total Investment"].DefaultCellStyle.Format = "N2";
                                    gridPurchaseReport.Columns["Total Investment"].FillWeight = 120;
                                }
                                if (gridPurchaseReport.Columns["Category"] != null) gridPurchaseReport.Columns["Category"].FillWeight = 180;
                                if (gridPurchaseReport.Columns["Total Qty Purchased"] != null) gridPurchaseReport.Columns["Total Qty Purchased"].FillWeight = 100;
                            }
                        }
                    }

                    // Compute Summary Numbers
                    using (SqlCommand cmd = new SqlCommand(sumQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@from", purchaseFromDate.Value.Date);
                        cmd.Parameters.AddWithValue("@to", purchaseToDate.Value.Date);
                        if (supplierId != -1)
                        {
                            cmd.Parameters.AddWithValue("@supplierId", supplierId);
                        }

                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                int count = r.GetInt32(0);
                                decimal sumVal = r.GetDecimal(1);

                                if (reportType == "Invoice Summary")
                                {
                                    lblPurchaseSummary.Text = $"Inward Invoices: {count}  •  Total Valuation: Rs. {sumVal:N2}";
                                }
                                else if (reportType == "Product-wise History")
                                {
                                    lblPurchaseSummary.Text = $"Products Restocked: {count}  •  Total Valuation: Rs. {sumVal:N2}";
                                }
                                else if (reportType == "Category-wise History")
                                {
                                    lblPurchaseSummary.Text = $"Categories Restocked: {count}  •  Total Valuation: Rs. {sumVal:N2}";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading purchase logs: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // --- NEW PRICE HISTORY TRACKER SUBSYSTEM ---
        // --- NEW PRICE HISTORY TRACKER SUBSYSTEM ---
        private void InitializePriceHistoryTab(Panel page)
        {
            // Filter Header Label
            Label lblProd = new Label();
            lblProd.Text = "Select Product to Track Price History:";
            lblProd.Location = new Point(20, 10);
            lblProd.AutoSize = true;
            Theme.StyleLabel(lblProd, Theme.TextLight, Theme.BoldFont);
            page.Controls.Add(lblProd);

            // Product Dropdown Selection
            comboHistoryProduct = new ComboBox();
            comboHistoryProduct.Size = new Size(350, 30);
            comboHistoryProduct.Location = new Point(20, 32);
            comboHistoryProduct.DropDownStyle = ComboBoxStyle.DropDownList;
            comboHistoryProduct.BackColor = Theme.Primary;
            comboHistoryProduct.ForeColor = Theme.TextLight;
            comboHistoryProduct.Font = Theme.MainFont;
            comboHistoryProduct.SelectedIndexChanged += ComboHistoryProduct_SelectedIndexChanged;
            page.Controls.Add(comboHistoryProduct);

            // Responsive Layout Table for Cards
            TableLayoutPanel layoutTrends = new TableLayoutPanel();
            layoutTrends.Location = new Point(20, 75);
            layoutTrends.Size = new Size(870, 105);
            layoutTrends.ColumnCount = 2;
            layoutTrends.RowCount = 1;
            layoutTrends.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            layoutTrends.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            layoutTrends.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            layoutTrends.BackColor = Color.Transparent;
            page.Controls.Add(layoutTrends);

            // 1. Cost Price Trend
            cardCostTrend = Theme.CreateCard(420, 100);
            cardCostTrend.Dock = DockStyle.Fill;
            cardCostTrend.Margin = new Padding(0, 0, 15, 0);
            cardCostTrend.BackColor = Color.FromArgb(17, 24, 39);
            
            lblCostCompareTitle = new Label();
            lblCostCompareTitle.Text = "PURCHASE COST TREND (1 YEAR AGO VS NOW)";
            lblCostCompareTitle.Location = new Point(12, 10);
            lblCostCompareTitle.Size = new Size(390, 15);
            lblCostCompareTitle.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            Theme.StyleLabel(lblCostCompareTitle, Theme.TextDark, new Font("Segoe UI Semibold", 8F, FontStyle.Bold));
            cardCostTrend.Controls.Add(lblCostCompareTitle);

            lblCostTrendVal = new Label();
            lblCostTrendVal.Text = "Loading cost history...";
            lblCostTrendVal.Location = new Point(12, 32);
            lblCostTrendVal.Size = new Size(390, 50);
            lblCostTrendVal.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            Theme.StyleLabel(lblCostTrendVal, Theme.TextLight, new Font("Segoe UI", 16F, FontStyle.Bold));
            cardCostTrend.Controls.Add(lblCostTrendVal);

            layoutTrends.Controls.Add(cardCostTrend, 0, 0);

            // 2. Sales Price Trend
            cardSalesTrend = Theme.CreateCard(420, 100);
            cardSalesTrend.Dock = DockStyle.Fill;
            cardSalesTrend.Margin = new Padding(15, 0, 0, 0);
            cardSalesTrend.BackColor = Color.FromArgb(17, 24, 39);

            lblSalesCompareTitle = new Label();
            lblSalesCompareTitle.Text = "RETAIL SALES PRICE TREND (1 YEAR AGO VS NOW)";
            lblSalesCompareTitle.Location = new Point(12, 10);
            lblSalesCompareTitle.Size = new Size(390, 15);
            lblSalesCompareTitle.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            Theme.StyleLabel(lblSalesCompareTitle, Theme.TextDark, new Font("Segoe UI Semibold", 8F, FontStyle.Bold));
            cardSalesTrend.Controls.Add(lblSalesCompareTitle);

            lblSalesTrendVal = new Label();
            lblSalesTrendVal.Text = "Loading retail history...";
            lblSalesTrendVal.Location = new Point(12, 32);
            lblSalesTrendVal.Size = new Size(390, 50);
            lblSalesTrendVal.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            Theme.StyleLabel(lblSalesTrendVal, Theme.TextLight, new Font("Segoe UI", 16F, FontStyle.Bold));
            cardSalesTrend.Controls.Add(lblSalesTrendVal);

            layoutTrends.Controls.Add(cardSalesTrend, 1, 0);

            // Historical Log DataGrid
            gridPriceHistory = new DataGridView();
            gridPriceHistory.Size = new Size(870, 290);
            gridPriceHistory.Location = new Point(20, 190);
            gridPriceHistory.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridPriceHistory);
            page.Controls.Add(gridPriceHistory);
        }

        private void LoadPriceHistoryProductsDropdown()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT Id, Code + ' - ' + Name as DisplayName FROM Products ORDER BY Name ASC", conn))
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            
                            comboHistoryProduct.SelectedIndexChanged -= ComboHistoryProduct_SelectedIndexChanged;
                            comboHistoryProduct.DataSource = dt;
                            comboHistoryProduct.DisplayMember = "DisplayName";
                            comboHistoryProduct.ValueMember = "Id";
                            comboHistoryProduct.SelectedIndexChanged += ComboHistoryProduct_SelectedIndexChanged;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading price tracker products list: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ComboHistoryProduct_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadPriceHistoryLog();
        }

        private void LoadPriceHistoryLog()
        {
            if (comboHistoryProduct.SelectedValue == null) return;

            int prodId = 0;
            if (comboHistoryProduct.SelectedValue is int)
            {
                prodId = (int)comboHistoryProduct.SelectedValue;
            }
            else if (comboHistoryProduct.SelectedValue is DataRowView drv)
            {
                prodId = (int)drv["Id"];
            }
            else
            {
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    // 1. Populate the Grid
                    string query = @"
                        SELECT ChangeDate as [Date of Change], 
                               OldPurchasePrice as [Old Cost], 
                               NewPurchasePrice as [New Cost], 
                               OldSalesPrice as [Old Sales], 
                               NewSalesPrice as [New Sales], 
                               Source as [Update Event Source]
                        FROM ProductPriceHistory
                        WHERE ProductId = @prodId
                        ORDER BY ChangeDate DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@prodId", prodId);
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridPriceHistory.DataSource = dt;

                            // Apply custom column formatting and proportional weights
                            if (gridPriceHistory.Columns["Old Cost"] != null)
                            {
                                gridPriceHistory.Columns["Old Cost"].DefaultCellStyle.Format = "N2";
                                gridPriceHistory.Columns["Old Cost"].FillWeight = 80;
                            }
                            if (gridPriceHistory.Columns["New Cost"] != null)
                            {
                                gridPriceHistory.Columns["New Cost"].DefaultCellStyle.Format = "N2";
                                gridPriceHistory.Columns["New Cost"].FillWeight = 80;
                            }
                            if (gridPriceHistory.Columns["Old Sales"] != null)
                            {
                                gridPriceHistory.Columns["Old Sales"].DefaultCellStyle.Format = "N2";
                                gridPriceHistory.Columns["Old Sales"].FillWeight = 80;
                            }
                            if (gridPriceHistory.Columns["New Sales"] != null)
                            {
                                gridPriceHistory.Columns["New Sales"].DefaultCellStyle.Format = "N2";
                                gridPriceHistory.Columns["New Sales"].FillWeight = 80;
                            }
                            if (gridPriceHistory.Columns["Date of Change"] != null) gridPriceHistory.Columns["Date of Change"].FillWeight = 120;
                            if (gridPriceHistory.Columns["Update Event Source"] != null) gridPriceHistory.Columns["Update Event Source"].FillWeight = 180;
                        }
                    }

                    // 2. Fetch current prices
                    decimal curCost = 0, curSales = 0;
                    using (SqlCommand cmd = new SqlCommand("SELECT PurchasePrice, SalesPrice FROM Products WHERE Id = @prodId", conn))
                    {
                        cmd.Parameters.AddWithValue("@prodId", prodId);
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                curCost = Convert.ToDecimal(rdr["PurchasePrice"]);
                                curSales = Convert.ToDecimal(rdr["SalesPrice"]);
                            }
                        }
                    }

                    // 3. Fetch price 1 Year Ago (from 365 days ago, or if no history exists, fallback to earliest log, or current price if zero logs)
                    decimal oldCost = curCost;
                    decimal oldSales = curSales;
                    DateTime oneYearAgo = DateTime.Now.AddYears(-1);
                    DateTime costLogDate = DateTime.Now;
                    DateTime salesLogDate = DateTime.Now;
                    bool hasHistoricalLogs = false;

                    // Query log closest to 1 year ago (but not after today)
                    string historicalPriceQuery = @"
                        SELECT TOP 1 OldPurchasePrice, OldSalesPrice, ChangeDate 
                        FROM ProductPriceHistory 
                        WHERE ProductId = @prodId AND ChangeDate <= @oneYearAgo 
                        ORDER BY ChangeDate DESC";

                    using (SqlCommand cmd = new SqlCommand(historicalPriceQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@prodId", prodId);
                        cmd.Parameters.AddWithValue("@oneYearAgo", oneYearAgo);
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                oldCost = Convert.ToDecimal(rdr["OldPurchasePrice"]);
                                oldSales = Convert.ToDecimal(rdr["OldSalesPrice"]);
                                costLogDate = Convert.ToDateTime(rdr["ChangeDate"]);
                                salesLogDate = costLogDate;
                                hasHistoricalLogs = true;
                            }
                        }
                    }

                    // If no log is older than 1 year, try getting the earliest log available (which represents its oldest known price)
                    if (!hasHistoricalLogs)
                    {
                        string earliestLogQuery = @"
                            SELECT TOP 1 OldPurchasePrice, OldSalesPrice, ChangeDate 
                            FROM ProductPriceHistory 
                            WHERE ProductId = @prodId 
                            ORDER BY ChangeDate ASC";

                        using (SqlCommand cmd = new SqlCommand(earliestLogQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@prodId", prodId);
                            using (SqlDataReader rdr = cmd.ExecuteReader())
                            {
                                if (rdr.Read())
                                {
                                    oldCost = Convert.ToDecimal(rdr["OldPurchasePrice"]);
                                    oldSales = Convert.ToDecimal(rdr["OldSalesPrice"]);
                                    costLogDate = Convert.ToDateTime(rdr["ChangeDate"]);
                                    salesLogDate = costLogDate;
                                    hasHistoricalLogs = true;
                                }
                            }
                        }
                    }

                    // Compute Percentage Changes and display beautifully
                    string costDateLabel = hasHistoricalLogs ? $"SINCE {costLogDate:yyyy-MM-dd}" : "SINCE CREATION";
                    lblCostCompareTitle.Text = $"PURCHASE COST TREND ({costDateLabel} VS NOW)";
                    
                    decimal costDiff = curCost - oldCost;
                    decimal costPercent = oldCost > 0 ? (costDiff / oldCost) * 100 : 0;
                    string costSign = costDiff >= 0 ? "+" : "";
                    
                    lblCostTrendVal.Text = $"Rs. {oldCost:N2} ➡️ Rs. {curCost:N2}  ({costSign}{costPercent:F1}%)";
                    if (costDiff > 0)
                    {
                        lblCostTrendVal.ForeColor = Theme.Warning; // Orange/Yellow alert for inflation
                        cardCostTrend.BackColor = Color.FromArgb(45, 30, 15);
                    }
                    else if (costDiff < 0)
                    {
                        lblCostTrendVal.ForeColor = Theme.Success; // Green for cost reduction
                        cardCostTrend.BackColor = Color.FromArgb(15, 35, 20);
                    }
                    else
                    {
                        lblCostTrendVal.ForeColor = Theme.TextLight;
                        cardCostTrend.BackColor = Color.FromArgb(17, 24, 39);
                    }

                    string salesDateLabel = hasHistoricalLogs ? $"SINCE {salesLogDate:yyyy-MM-dd}" : "SINCE CREATION";
                    lblSalesCompareTitle.Text = $"RETAIL SALES PRICE TREND ({salesDateLabel} VS NOW)";
                    
                    decimal salesDiff = curSales - oldSales;
                    decimal salesPercent = oldSales > 0 ? (salesDiff / oldSales) * 100 : 0;
                    string salesSign = salesDiff >= 0 ? "+" : "";

                    lblSalesTrendVal.Text = $"Rs. {oldSales:N2} ➡️ Rs. {curSales:N2}  ({salesSign}{salesPercent:F1}%)";
                    if (salesDiff > 0)
                    {
                        lblSalesTrendVal.ForeColor = Theme.Success; // Green for price appreciation (more profit)
                        cardSalesTrend.BackColor = Color.FromArgb(15, 35, 20);
                    }
                    else if (salesDiff < 0)
                    {
                        lblSalesTrendVal.ForeColor = Theme.Danger; // Red for markdown / sales drop
                        cardSalesTrend.BackColor = Color.FromArgb(45, 15, 15);
                    }
                    else
                    {
                        lblSalesTrendVal.ForeColor = Theme.TextLight;
                        cardSalesTrend.BackColor = Color.FromArgb(17, 24, 39);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading price history: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnReprintBill_Click(object sender, EventArgs e)
        {
            if (gridSalesReport.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select an invoice from the Daily Sales Register to reprint.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string invoiceNo = gridSalesReport.SelectedRows[0].Cells["Invoice No"].Value.ToString();

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT Id FROM Sales WHERE InvoiceNumber = @invNum", conn))
                    {
                        cmd.Parameters.AddWithValue("@invNum", invoiceNo);
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            printSaleId = Convert.ToInt32(result);
                            reprintPreviewDlg.ShowDialog();
                        }
                        else
                        {
                            MessageBox.Show("Could not find the selected sale transaction in the database.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error retrieving transaction details: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReprintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int startX = 50;
            int startY = 50;

            Font fTitle = new Font("Segoe UI", 18F, FontStyle.Bold);
            Font fSubTitle = new Font("Segoe UI", 9F, FontStyle.Italic);
            Font fRegular = new Font("Segoe UI", 10F, FontStyle.Regular);
            Font fBold = new Font("Segoe UI", 10F, FontStyle.Bold);
            Font fDuplicate = new Font("Segoe UI", 12F, FontStyle.Bold);

            // Fetch checkout details from database dynamically for print
            string invNum = "", custName = "", custPhone = "", custAddr = "", dateStr = "", paymentMode = "";
            decimal sub = 0, disc = 0, tx = 0, grand = 0;
            bool isInclusive = false;
            string taxType = "Local";
            decimal paidAmt = 0;
            decimal dueAmt = 0;

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT s.InvoiceNumber, s.SaleDate, s.SubTotal, s.Discount, s.Tax, s.GrandTotal, s.PaymentMethod,
                                c.Name, c.Phone, c.Address, s.IsTaxInclusive, s.TaxType, s.PaidAmount, s.DueAmount 
                        FROM Sales s
                        LEFT JOIN Customers c ON s.CustomerId = c.Id
                        WHERE s.Id = @id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", printSaleId);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                invNum = r.GetString(0);
                                dateStr = r.GetDateTime(1).ToString("yyyy-MM-dd HH:mm");
                                sub = r.GetDecimal(2);
                                disc = r.GetDecimal(3);
                                tx = r.GetDecimal(4);
                                grand = r.GetDecimal(5);
                                paymentMode = r.GetString(6);
                                custName = r.GetString(7);
                                custPhone = r.IsDBNull(8) ? "" : r.GetString(8);
                                custAddr = r.IsDBNull(9) ? "" : r.GetString(9);
                                isInclusive = r.GetBoolean(10);
                                taxType = r.GetString(11);
                                paidAmt = r.GetDecimal(12);
                                dueAmt = r.GetDecimal(13);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                using (Brush bDark = new SolidBrush(Color.Black))
                {
                    g.DrawString($"Database Error Rendering Print: {ex.Message}", fRegular, bDark, startX, startY);
                }
                return;
            }

            using (Brush bDark = new SolidBrush(Color.Black))
            using (Brush bDuplicate = new SolidBrush(Theme.Danger))
            using (Pen pLine = new Pen(Color.Gray, 1))
            {
                // Fetch profile settings dynamically for branding
                string shopName = "Mero Dokan Shop", shopPhone = "+977-1-4200000", shopEmail = "contact@MeroHardwareDokan.com", shopAddress = "Kathmandu, Nepal", logoPath = "", shopGSTIN = "";
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 ShopName, Phone, Email, Address, LogoPath, GSTIN FROM AppProfile", conn))
                        {
                            using (SqlDataReader rdr = cmd.ExecuteReader())
                            {
                                if (rdr.Read())
                                {
                                    shopName = rdr["ShopName"].ToString();
                                    shopPhone = rdr["Phone"].ToString();
                                    shopEmail = rdr["Email"].ToString();
                                    shopAddress = rdr["Address"].ToString();
                                    logoPath = rdr["LogoPath"]?.ToString();
                                    shopGSTIN = rdr["GSTIN"]?.ToString();
                                }
                            }
                        }
                    }
                }
                catch { }

                // Header Section
                int textShiftX = 0;
                if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                {
                    try
                    {
                        using (Image logo = Image.FromFile(logoPath))
                        {
                            g.DrawImage(logo, startX, startY - 10, 60, 60);
                            textShiftX = 75;
                        }
                    }
                    catch { }
                }

                g.DrawString(shopName, fTitle, bDark, startX + textShiftX, startY);
                g.DrawString($"{shopAddress} | Phone: {shopPhone} | Email: {shopEmail}", fSubTitle, bDark, startX + textShiftX, startY + 30);
                
                int headerOffset = 0;
                if (!string.IsNullOrEmpty(shopGSTIN))
                {
                    g.DrawString($"GSTIN: {shopGSTIN}", fSubTitle, bDark, startX + textShiftX, startY + 48);
                    headerOffset = 20;
                }

                // Draw a prominent "DUPLICATE COPY" banner
                g.DrawString("*** DUPLICATE COPY ***", fDuplicate, bDuplicate, 400, startY - 12);

                // Draw scannable invoice number QR Code
                BarcodeHelper.DrawQRCode(g, invNum, 660, startY - 12, 60);

                g.DrawLine(pLine, startX, startY + 50 + headerOffset, 750, startY + 50 + headerOffset);

                // Customer Info Block
                g.DrawString($"Invoice No:  {invNum}", fBold, bDark, startX, startY + 65 + headerOffset);
                g.DrawString($"Invoice Date: {dateStr}", fRegular, bDark, 480, startY + 65 + headerOffset);
                
                g.DrawString($"Bill To:     {custName}", fRegular, bDark, startX, startY + 90 + headerOffset);
                g.DrawString($"Address:     {custAddr}", fRegular, bDark, startX, startY + 110 + headerOffset);
                g.DrawString($"Phone No:    {custPhone}", fRegular, bDark, startX, startY + 130 + headerOffset);

                g.DrawLine(pLine, startX, startY + 160 + headerOffset, 750, startY + 160 + headerOffset);

                // Table Headers
                int col1_left = startX;
                int col2_left = startX + 270;
                int col3_left = startX + 370;
                int col4_left = startX + 460;
                int col5_left = startX + 580;
                int col_right = 750;

                int rowY = startY + 175 + headerOffset;

                // Draw header top horizontal border
                g.DrawLine(pLine, col1_left, rowY - 4, col_right, rowY - 4);

                g.DrawString("Product / Description", fBold, bDark, col1_left + 5, rowY);
                g.DrawString("Tax Slab", fBold, bDark, col2_left + 5, rowY);
                g.DrawString("Qty", fBold, bDark, col3_left + 5, rowY);
                g.DrawString("Rate", fBold, bDark, col4_left + 5, rowY);
                g.DrawString("Total Cost", fBold, bDark, col5_left + 5, rowY);

                // Draw header bottom horizontal border
                g.DrawLine(pLine, col1_left, rowY + 20, col_right, rowY + 20);

                // Draw header vertical borders
                int headerTop = rowY - 4;
                int headerBottom = rowY + 20;
                g.DrawLine(pLine, col1_left, headerTop, col1_left, headerBottom);
                g.DrawLine(pLine, col2_left, headerTop, col2_left, headerBottom);
                g.DrawLine(pLine, col3_left, headerTop, col3_left, headerBottom);
                g.DrawLine(pLine, col4_left, headerTop, col4_left, headerBottom);
                g.DrawLine(pLine, col5_left, headerTop, col5_left, headerBottom);
                g.DrawLine(pLine, col_right, headerTop, col_right, headerBottom);

                rowY += 24;

                // Render Items
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        string detailsQuery = @"
                            SELECT p.Name, sd.Quantity, sd.UnitPrice, sd.Total, sd.TaxPercent, p.Unit 
                            FROM SaleDetails sd
                            INNER JOIN Products p ON sd.ProductId = p.Id
                            WHERE sd.SaleId = @id";

                        using (SqlCommand cmd = new SqlCommand(detailsQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", printSaleId);
                            using (SqlDataReader r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    string pName = r.GetString(0);
                                    decimal qty = r.GetDecimal(1);
                                    decimal rate = r.GetDecimal(2);
                                    decimal total = r.GetDecimal(3);
                                    decimal taxPct = r.GetDecimal(4);
                                    string unit = r.IsDBNull(5) ? "Pcs" : r.GetString(5);

                                    int cellTop = rowY - 4;
                                    int cellBottom = rowY + 20;

                                    // Draw cells text
                                    g.DrawString(pName, fRegular, bDark, col1_left + 5, rowY);
                                    g.DrawString($"{taxPct:0.##}%", fRegular, bDark, col2_left + 5, rowY);
                                    g.DrawString($"{qty:0.###} {unit}", fRegular, bDark, col3_left + 5, rowY);
                                    g.DrawString($"Rs. {rate:F2}", fRegular, bDark, col4_left + 5, rowY);
                                    g.DrawString($"Rs. {total:F2}", fRegular, bDark, col5_left + 5, rowY);

                                    // Draw horizontal bottom border
                                    g.DrawLine(pLine, col1_left, cellBottom, col_right, cellBottom);

                                    // Draw vertical column borders
                                    g.DrawLine(pLine, col1_left, cellTop, col1_left, cellBottom);
                                    g.DrawLine(pLine, col2_left, cellTop, col2_left, cellBottom);
                                    g.DrawLine(pLine, col3_left, cellTop, col3_left, cellBottom);
                                    g.DrawLine(pLine, col4_left, cellTop, col4_left, cellBottom);
                                    g.DrawLine(pLine, col5_left, cellTop, col5_left, cellBottom);
                                    g.DrawLine(pLine, col_right, cellTop, col_right, cellBottom);

                                    rowY += 24;
                                }
                            }
                        }
                    }
                }
                catch { }

                rowY += 15;

                // Summary Totals
                int col4 = col4_left + 10;
                int col5 = col5_left + 10;
                int summaryX = col4 - 40;

                if (isInclusive)
                {
                    g.DrawString("Sub Total (Tax Incl.):", fRegular, bDark, summaryX, rowY);
                    g.DrawString($"Rs. {sub:N2}", fRegular, bDark, col5, rowY);
                    rowY += 20;

                    g.DrawString("Discount Amount:", fRegular, bDark, summaryX, rowY);
                    g.DrawString($"- Rs. {disc:N2}", fRegular, bDark, col5, rowY);
                    rowY += 20;

                    if (tx > 0)
                    {
                        decimal taxableVal = grand - tx;
                        g.DrawString("Taxable Value:", fRegular, bDark, summaryX, rowY);
                        g.DrawString($"Rs. {taxableVal:N2}", fRegular, bDark, col5, rowY);
                        rowY += 20;

                        if (taxType == "Inter-State" || taxType == "InterState")
                        {
                            g.DrawString("IGST (Included):", fRegular, bDark, summaryX, rowY);
                            g.DrawString($"Rs. {tx:N2}", fRegular, bDark, col5, rowY);
                            rowY += 20;
                        }
                        else
                        {
                            g.DrawString("CGST (Included):", fRegular, bDark, summaryX, rowY);
                            g.DrawString($"Rs. {tx / 2:N2}", fRegular, bDark, col5, rowY);
                            rowY += 20;

                            g.DrawString("SGST (Included):", fRegular, bDark, summaryX, rowY);
                            g.DrawString($"Rs. {tx / 2:N2}", fRegular, bDark, col5, rowY);
                            rowY += 20;
                        }
                    }
                }
                else
                {
                    g.DrawString("Sub Total:", fRegular, bDark, summaryX, rowY);
                    g.DrawString($"Rs. {sub:N2}", fRegular, bDark, col5, rowY);
                    rowY += 20;

                    g.DrawString("Discount Amount:", fRegular, bDark, summaryX, rowY);
                    g.DrawString($"- Rs. {disc:N2}", fRegular, bDark, col5, rowY);
                    rowY += 20;

                    if (tx > 0)
                    {
                        if (taxType == "Inter-State" || taxType == "InterState")
                        {
                            g.DrawString("IGST Amount:", fRegular, bDark, summaryX, rowY);
                            g.DrawString($"Rs. {tx:N2}", fRegular, bDark, col5, rowY);
                            rowY += 20;
                        }
                        else
                        {
                            g.DrawString("CGST Amount:", fRegular, bDark, summaryX, rowY);
                            g.DrawString($"Rs. {tx / 2:N2}", fRegular, bDark, col5, rowY);
                            rowY += 20;

                            g.DrawString("SGST Amount:", fRegular, bDark, summaryX, rowY);
                            g.DrawString($"Rs. {tx / 2:N2}", fRegular, bDark, col5, rowY);
                            rowY += 20;
                        }
                    }
                }

                rowY += 5;
                g.DrawLine(pLine, summaryX, rowY - 5, 750, rowY - 5);

                g.DrawString("GRAND TOTAL:", fBold, bDark, summaryX, rowY);
                g.DrawString($"Rs. {grand:N2}", fBold, bDark, col5, rowY);
                rowY += 25;

                g.DrawString("Amount Paid:", fRegular, bDark, summaryX, rowY);
                g.DrawString($"Rs. {paidAmt:N2}", fRegular, bDark, col5, rowY);
                rowY += 18;

                g.DrawString("Balance Due:", fBold, bDark, summaryX, rowY);
                g.DrawString($"Rs. {dueAmt:N2}", fBold, bDark, col5, rowY);
                
                g.DrawString($"Payment Mode: {paymentMode}", fBold, bDark, startX, rowY - 18);
                rowY += 25;

                // Fetch and draw repayment history if there is any
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        string pQuery = @"
                            SELECT PaymentDate, AmountPaid, PaymentMethod, Remarks 
                            FROM CustomerPayments 
                            WHERE SaleId = @saleId 
                            ORDER BY PaymentDate ASC";
                        using (SqlCommand cmd = new SqlCommand(pQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@saleId", printSaleId);
                            using (SqlDataReader rdr = cmd.ExecuteReader())
                            {
                                bool hasHistory = false;
                                while (rdr.Read())
                                {
                                    if (!hasHistory)
                                    {
                                        rowY += 10;
                                        g.DrawLine(pLine, startX, rowY, 750, rowY);
                                        rowY += 10;
                                        g.DrawString("Payment History Logs:", fBold, bDark, startX, rowY);
                                        rowY += 18;
                                        hasHistory = true;
                                    }
                                    DateTime pDate = rdr.GetDateTime(0);
                                    decimal pAmount = rdr.GetDecimal(1);
                                    string pMethod = rdr.GetString(2);
                                    string pRemarks = rdr.IsDBNull(3) ? "" : rdr.GetString(3);

                                    string logLine = $"• {pDate:yyyy-MM-dd HH:mm} - Paid Rs. {pAmount:N2} via {pMethod} ({pRemarks})";
                                    g.DrawString(logLine, fRegular, bDark, startX + 15, rowY);
                                    rowY += 18;
                                }
                            }
                        }
                    }
                }
                catch { }

                rowY += 10;
                g.DrawLine(pLine, startX, rowY, 750, rowY);
                rowY += 15;

                // Footer Message
                g.DrawString("Thank you for shopping at Mero Dokan! Please visit us again.", fBold, bDark, startX + 130, rowY);
            }
        }
    }
}

