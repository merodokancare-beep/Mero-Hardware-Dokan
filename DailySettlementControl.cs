using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace MeroHardwareDokan
{
    public class DailySettlementControl : UserControl
    {
        // Date Selection
        private DateTimePicker dtpSettlementDate;

        // Metric Card Labels (for dynamic value updates)
        private Label lblGrossSalesVal;
        private Label lblDuesCreatedVal;
        private Label lblCardQRSalesVal;
        private Label lblCashSalesVal;
        private Label lblOpeningCashVal;
        private Label lblDueCollectionsVal;
        private Label lblRefundsVal;
        private Label lblExpectedCashVal;

        // Reconciliation Input Controls
        private TextBox txtOpeningCash;
        private TextBox txtActualCash;
        private TextBox txtExpectedCashDisplay;
        private TextBox txtVariance;
        private TextBox txtRemarks;
        private Button btnSave;
        private Label lblStatus;

        // History Log
        private DataGridView gridHistory;

        // Today's calculated metrics (State variables)
        private decimal todayGrossSales = 0m;
        private decimal todayDuesCreated = 0m;
        private decimal todayCardQRSales = 0m;
        private decimal todayCashSales = 0m; // Calculated: Gross - DuesCreated - CardQR
        private decimal todayDueCollections = 0m; // Previous day dues collected today in Cash
        private decimal todayRefunds = 0m; // Cash refunds today
        private decimal expectedCash = 0m; // Opening + CashSales + DueCollections - Refunds

        private bool isUpdatingInputs = false;

        public DailySettlementControl()
        {
            InitializeComponent();
            LoadDailyMetrics(DateTime.Today);
            LoadHistoryLog();
        }

        private void InitializeComponent()
        {
            this.Font = Theme.MainFont;
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.AutoScroll = true;
            this.Size = new Size(950, 650);
            this.BackColor = Theme.Secondary;

            // Page Header Title
            Label lblHeader = new Label();
            lblHeader.Text = "Daily Cash Register & Settlement Terminal";
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
            this.Controls.Add(lblHeader);

            Label lblSub = new Label();
            lblSub.Text = "Reconcile daily cash in drawer, track sales channels, collections, and refunds";
            lblSub.Location = new Point(22, 45);
            lblSub.AutoSize = true;
            Theme.StyleLabel(lblSub, Theme.TextDark, Theme.MainFont);
            this.Controls.Add(lblSub);

            // Date Picker (Top Right)
            dtpSettlementDate = new DateTimePicker();
            dtpSettlementDate.Format = DateTimePickerFormat.Short;
            dtpSettlementDate.Size = new Size(160, 28);
            dtpSettlementDate.Location = new Point(765, 20);
            dtpSettlementDate.Font = Theme.BoldFont;
            dtpSettlementDate.CalendarMonthBackground = Theme.Primary;
            dtpSettlementDate.CalendarForeColor = Theme.TextLight;
            dtpSettlementDate.ValueChanged += DtpSettlementDate_ValueChanged;
            this.Controls.Add(dtpSettlementDate);

            Label lblDatePick = new Label();
            lblDatePick.Text = "Settlement Date:";
            lblDatePick.Location = new Point(640, 25);
            lblDatePick.AutoSize = true;
            Theme.StyleLabel(lblDatePick, Theme.TextLight, Theme.BoldFont);
            this.Controls.Add(lblDatePick);

            // ==========================================
            // ROW 1: SALES & TRANSACTIONS BREAKDOWN CARDS
            // ==========================================
            int cardW = 215;
            int cardH = 70;
            int startX = 20;
            int gapX = 15;
            int row1Y = 80;

            // 1. Gross Sales Card
            Panel cardGrossSales = Theme.CreateCard(cardW, cardH);
            cardGrossSales.Location = new Point(startX, row1Y);
            cardGrossSales.BackColor = Color.FromArgb(17, 24, 39);
            lblGrossSalesVal = CreateCardContent(cardGrossSales, "TOTAL SALES (GROSS)", "Rs. 0.00", Theme.TextLight);
            this.Controls.Add(cardGrossSales);

            // 2. Dues Created Card
            Panel cardDuesCreated = Theme.CreateCard(cardW, cardH);
            cardDuesCreated.Location = new Point(startX + cardW + gapX, row1Y);
            cardDuesCreated.BackColor = Color.FromArgb(17, 24, 39);
            lblDuesCreatedVal = CreateCardContent(cardDuesCreated, "DUES CREATED TODAY", "Rs. 0.00", Theme.Warning);
            this.Controls.Add(cardDuesCreated);

            // 3. Card/QR Sales Card
            Panel cardCardQRSales = Theme.CreateCard(cardW, cardH);
            cardCardQRSales.Location = new Point(startX + (cardW + gapX) * 2, row1Y);
            cardCardQRSales.BackColor = Color.FromArgb(17, 24, 39);
            lblCardQRSalesVal = CreateCardContent(cardCardQRSales, "CARD / QR PAYMENTS", "Rs. 0.00", Theme.Accent);
            this.Controls.Add(cardCardQRSales);

            // 4. Net Cash Sales Today Card
            Panel cardCashSales = Theme.CreateCard(cardW, cardH);
            cardCashSales.Location = new Point(startX + (cardW + gapX) * 3, row1Y);
            cardCashSales.BackColor = Color.FromArgb(17, 24, 39);
            lblCashSalesVal = CreateCardContent(cardCashSales, "TODAY'S CASH SALES", "Rs. 0.00", Theme.Success);
            this.Controls.Add(cardCashSales);

            // ==========================================
            // ROW 2: CASH DRAWER CALCULATIONS CARDS
            // ==========================================
            int row2Y = 160;

            // 5. Opening Cash Card
            Panel cardOpeningCash = Theme.CreateCard(cardW, cardH);
            cardOpeningCash.Location = new Point(startX, row2Y);
            cardOpeningCash.BackColor = Color.FromArgb(17, 24, 39);
            lblOpeningCashVal = CreateCardContent(cardOpeningCash, "OPENING CASH IN DRAWER", "Rs. 0.00", Theme.TextLight);
            this.Controls.Add(cardOpeningCash);

            // 6. Due Collections Card
            Panel cardDueCollections = Theme.CreateCard(cardW, cardH);
            cardDueCollections.Location = new Point(startX + cardW + gapX, row2Y);
            cardDueCollections.BackColor = Color.FromArgb(17, 24, 39);
            lblDueCollectionsVal = CreateCardContent(cardDueCollections, "PAST DUES COLLECTED (CASH)", "Rs. 0.00", Theme.Success);
            this.Controls.Add(cardDueCollections);

            // 7. Cash Refunds Card
            Panel cardRefunds = Theme.CreateCard(cardW, cardH);
            cardRefunds.Location = new Point(startX + (cardW + gapX) * 2, row2Y);
            cardRefunds.BackColor = Color.FromArgb(17, 24, 39);
            lblRefundsVal = CreateCardContent(cardRefunds, "CASH REFUNDS TODAY", "Rs. 0.00", Theme.Danger);
            this.Controls.Add(cardRefunds);

            // 8. Expected Cash Card
            Panel cardExpectedCash = Theme.CreateCard(cardW, cardH);
            cardExpectedCash.Location = new Point(startX + (cardW + gapX) * 3, row2Y);
            cardExpectedCash.BackColor = Color.FromArgb(17, 24, 39);
            lblExpectedCashVal = CreateCardContent(cardExpectedCash, "EXPECTED CASH IN DRAWER", "Rs. 0.00", Theme.Accent);
            this.Controls.Add(cardExpectedCash);

            // ==========================================
            // SPLIT VIEW: RECONCILIATION & HISTORY LOG
            // ==========================================
            int row3Y = 245;

            // Left Card: Reconciliation Form
            Panel panelReconcile = Theme.CreateCard(440, 385);
            panelReconcile.Location = new Point(20, row3Y);
            panelReconcile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;
            this.Controls.Add(panelReconcile);

            Label lblRecHeader = new Label();
            lblRecHeader.Text = "Register Reconciliation Form";
            lblRecHeader.Location = new Point(15, 15);
            lblRecHeader.AutoSize = true;
            Theme.StyleLabel(lblRecHeader, Theme.TextLight, Theme.SubHeaderFont);
            panelReconcile.Controls.Add(lblRecHeader);

            // Opening Cash Input
            Label lblInputOpening = new Label();
            lblInputOpening.Text = "Opening Cash (Rs.)";
            lblInputOpening.Location = new Point(20, 55);
            lblInputOpening.AutoSize = true;
            Theme.StyleLabel(lblInputOpening, Theme.TextLight, Theme.BoldFont);
            panelReconcile.Controls.Add(lblInputOpening);

            txtOpeningCash = new TextBox();
            txtOpeningCash.Location = new Point(20, 80);
            txtOpeningCash.Size = new Size(185, 28);
            Theme.StyleTextBox(txtOpeningCash);
            txtOpeningCash.TextChanged += InputCash_TextChanged;
            panelReconcile.Controls.Add(txtOpeningCash);

            // Actual Cash Input
            Label lblInputActual = new Label();
            lblInputActual.Text = "Actual Cash Counted (Rs.) *";
            lblInputActual.Location = new Point(225, 55);
            lblInputActual.AutoSize = true;
            Theme.StyleLabel(lblInputActual, Theme.TextLight, Theme.BoldFont);
            panelReconcile.Controls.Add(lblInputActual);

            txtActualCash = new TextBox();
            txtActualCash.Location = new Point(225, 80);
            txtActualCash.Size = new Size(185, 28);
            Theme.StyleTextBox(txtActualCash);
            txtActualCash.TextChanged += InputCash_TextChanged;
            panelReconcile.Controls.Add(txtActualCash);

            // Expected Cash Display
            Label lblInputExpected = new Label();
            lblInputExpected.Text = "Expected Cash in Drawer";
            lblInputExpected.Location = new Point(20, 125);
            lblInputExpected.AutoSize = true;
            Theme.StyleLabel(lblInputExpected, Theme.TextLight, Theme.BoldFont);
            panelReconcile.Controls.Add(lblInputExpected);

            txtExpectedCashDisplay = new TextBox();
            txtExpectedCashDisplay.Location = new Point(20, 150);
            txtExpectedCashDisplay.Size = new Size(185, 28);
            txtExpectedCashDisplay.ReadOnly = true;
            Theme.StyleTextBox(txtExpectedCashDisplay);
            panelReconcile.Controls.Add(txtExpectedCashDisplay);

            // Variance Display
            Label lblInputVariance = new Label();
            lblInputVariance.Text = "Variance (Actual - Expected)";
            lblInputVariance.Location = new Point(225, 125);
            lblInputVariance.AutoSize = true;
            Theme.StyleLabel(lblInputVariance, Theme.TextLight, Theme.BoldFont);
            panelReconcile.Controls.Add(lblInputVariance);

            txtVariance = new TextBox();
            txtVariance.Location = new Point(225, 150);
            txtVariance.Size = new Size(185, 28);
            txtVariance.ReadOnly = true;
            txtVariance.Font = Theme.BoldFont;
            Theme.StyleTextBox(txtVariance);
            panelReconcile.Controls.Add(txtVariance);

            // Remarks Multiline
            Label lblInputRemarks = new Label();
            lblInputRemarks.Text = "Audit Remarks / Variance Explanation";
            lblInputRemarks.Location = new Point(20, 195);
            lblInputRemarks.AutoSize = true;
            Theme.StyleLabel(lblInputRemarks, Theme.TextLight, Theme.BoldFont);
            panelReconcile.Controls.Add(lblInputRemarks);

            txtRemarks = new TextBox();
            txtRemarks.Multiline = true;
            txtRemarks.Location = new Point(20, 220);
            txtRemarks.Size = new Size(390, 65);
            Theme.StyleTextBox(txtRemarks);
            panelReconcile.Controls.Add(txtRemarks);

            // Save Settlement Button
            btnSave = new Button();
            btnSave.Text = "💾 Save Daily Settlement";
            btnSave.Size = new Size(390, 42);
            btnSave.Location = new Point(20, 300);
            Theme.StyleSuccessButton(btnSave);
            btnSave.Click += BtnSave_Click;
            panelReconcile.Controls.Add(btnSave);

            // Status label
            lblStatus = new Label();
            lblStatus.Text = "Ready to record today's cash reconciliation details.";
            lblStatus.Location = new Point(20, 350);
            lblStatus.Size = new Size(390, 20);
            Theme.StyleLabel(lblStatus, Theme.TextDark, new Font("Segoe UI Italic", 9F));
            panelReconcile.Controls.Add(lblStatus);

            // Right Area: Historical Logs Title
            Label lblHistoryTitle = new Label();
            lblHistoryTitle.Text = "Reconciliation History Log";
            lblHistoryTitle.Location = new Point(480, row3Y);
            lblHistoryTitle.AutoSize = true;
            Theme.StyleLabel(lblHistoryTitle, Theme.TextLight, Theme.SubHeaderFont);
            this.Controls.Add(lblHistoryTitle);

            // Grid History
            gridHistory = new DataGridView();
            gridHistory.Location = new Point(480, row3Y + 30);
            gridHistory.Size = new Size(450, 355);
            gridHistory.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridHistory);
            gridHistory.SelectionChanged += GridHistory_SelectionChanged;
            this.Controls.Add(gridHistory);
        }

        private Label CreateCardContent(Panel card, string header, string initVal, Color valColor)
        {
            Label lblHeader = new Label();
            lblHeader.Text = header;
            lblHeader.Location = new Point(10, 8);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextDark, new Font("Segoe UI Semibold", 7.5F, FontStyle.Bold));
            card.Controls.Add(lblHeader);

            Label lblVal = new Label();
            lblVal.Text = initVal;
            lblVal.Location = new Point(10, 30);
            lblVal.AutoSize = true;
            Theme.StyleLabel(lblVal, valColor, new Font("Segoe UI", 14F, FontStyle.Bold));
            card.Controls.Add(lblVal);

            return lblVal;
        }

        private void DtpSettlementDate_ValueChanged(object sender, EventArgs e)
        {
            LoadDailyMetrics(dtpSettlementDate.Value);
        }

        private void LoadDailyMetrics(DateTime targetDate)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    // Step 1: Check if Daily Settlement has already been saved for targetDate
                    bool hasSettled = false;
                    decimal dbOpening = 0m;
                    decimal dbActual = 0m;
                    string dbRemarks = "";

                    string checkSql = "SELECT COUNT(*) FROM DailySettlements WHERE CAST(SettlementDate as DATE) = @date";
                    using (SqlCommand cmd = new SqlCommand(checkSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@date", targetDate.Date);
                        hasSettled = (int)cmd.ExecuteScalar() > 0;
                    }

                    if (hasSettled)
                    {
                        // Fetch stored values directly from Database
                        string selectSql = @"
                            SELECT OpeningCash, CashSales, DueCollections, CardQRSales, DuesCreated, ExpectedCash, ActualCash, Variance, Remarks, Refunds
                            FROM DailySettlements
                            WHERE CAST(SettlementDate as DATE) = @date";

                        using (SqlCommand cmd = new SqlCommand(selectSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@date", targetDate.Date);
                            using (SqlDataReader rdr = cmd.ExecuteReader())
                            {
                                if (rdr.Read())
                                {
                                    dbOpening = Convert.ToDecimal(rdr["OpeningCash"]);
                                    todayCashSales = Convert.ToDecimal(rdr["CashSales"]);
                                    todayDueCollections = Convert.ToDecimal(rdr["DueCollections"]);
                                    todayCardQRSales = Convert.ToDecimal(rdr["CardQRSales"]);
                                    todayDuesCreated = Convert.ToDecimal(rdr["DuesCreated"]);
                                    expectedCash = Convert.ToDecimal(rdr["ExpectedCash"]);
                                    dbActual = Convert.ToDecimal(rdr["ActualCash"]);
                                    dbRemarks = rdr["Remarks"]?.ToString() ?? "";
                                    todayRefunds = Convert.ToDecimal(rdr["Refunds"]);

                                    // Gross sales is CashSales + DuesCreated + CardQRSales
                                    todayGrossSales = todayCashSales + todayDuesCreated + todayCardQRSales;
                                }
                            }
                        }

                        // Update State & UI
                        isUpdatingInputs = true;
                        txtOpeningCash.Text = dbOpening.ToString("0.00");
                        txtActualCash.Text = dbActual.ToString("0.00");
                        txtRemarks.Text = dbRemarks;
                        isUpdatingInputs = false;

                        btnSave.Text = "🔄 Update Daily Settlement";
                        Theme.StylePrimaryButton(btnSave);
                        lblStatus.Text = $"Settlement loaded from database (Completed).";
                        lblStatus.ForeColor = Theme.Success;
                    }
                    else
                    {
                        // Run calculations dynamically for a new settlement record
                        // 1. Fetch opening cash as the actual closing cash of the latest settlement before targetDate
                        string prevOpeningSql = @"
                            SELECT TOP 1 ActualCash 
                            FROM DailySettlements 
                            WHERE CAST(SettlementDate as DATE) < @date 
                            ORDER BY SettlementDate DESC";

                        using (SqlCommand cmd = new SqlCommand(prevOpeningSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@date", targetDate.Date);
                            object result = cmd.ExecuteScalar();
                            dbOpening = result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0m;
                        }

                        // 2. Fetch Gross Sales and Dues Created Today
                        string salesSql = @"
                            SELECT ISNULL(SUM(GrandTotal), 0) as GrossSales, ISNULL(SUM(DueAmount), 0) as DuesCreated 
                            FROM Sales 
                            WHERE CAST(SaleDate as DATE) = @date";

                        using (SqlCommand cmd = new SqlCommand(salesSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@date", targetDate.Date);
                            using (SqlDataReader rdr = cmd.ExecuteReader())
                            {
                                if (rdr.Read())
                                {
                                    todayGrossSales = Convert.ToDecimal(rdr["GrossSales"]);
                                    todayDuesCreated = Convert.ToDecimal(rdr["DuesCreated"]);
                                }
                            }
                        }

                        // 3. Fetch Card/QR payments collected today for today's sales
                        string cardSql = @"
                            SELECT ISNULL(SUM(cp.AmountPaid), 0) 
                            FROM CustomerPayments cp
                            INNER JOIN Sales s ON cp.SaleId = s.Id
                            WHERE CAST(cp.PaymentDate as DATE) = @date 
                              AND CAST(s.SaleDate as DATE) = @date
                              AND cp.PaymentMethod IN ('Card', 'QR Pay')";

                        using (SqlCommand cmd = new SqlCommand(cardSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@date", targetDate.Date);
                            todayCardQRSales = (decimal)cmd.ExecuteScalar();
                        }

                        // 4. Calculate Net Cash Sales Today
                        todayCashSales = todayGrossSales - todayDuesCreated - todayCardQRSales;

                        // 5. Fetch previous dues collected today (Cash only)
                        string dueCollectionsSql = @"
                            SELECT ISNULL(SUM(cp.AmountPaid), 0) 
                            FROM CustomerPayments cp
                            INNER JOIN Sales s ON cp.SaleId = s.Id
                            WHERE CAST(cp.PaymentDate as DATE) = @date 
                              AND CAST(s.SaleDate as DATE) < @date
                              AND cp.PaymentMethod = 'Cash'";

                        using (SqlCommand cmd = new SqlCommand(dueCollectionsSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@date", targetDate.Date);
                            todayDueCollections = (decimal)cmd.ExecuteScalar();
                        }

                        // 6. Fetch Cash refunds today
                        string refundsSql = @"
                            SELECT ISNULL(SUM(sr.TotalRefund), 0) 
                            FROM SalesReturns sr
                            INNER JOIN Sales s ON sr.SaleId = s.Id
                            WHERE CAST(sr.ReturnDate as DATE) = @date 
                              AND s.PaymentMethod = 'Cash'";

                        using (SqlCommand cmd = new SqlCommand(refundsSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@date", targetDate.Date);
                            todayRefunds = (decimal)cmd.ExecuteScalar();
                        }

                        // Update State & UI
                        isUpdatingInputs = true;
                        txtOpeningCash.Text = dbOpening.ToString("0.00");
                        txtActualCash.Text = "0.00";
                        txtRemarks.Text = "";
                        isUpdatingInputs = false;

                        btnSave.Text = "💾 Save Daily Settlement";
                        Theme.StyleSuccessButton(btnSave);
                        lblStatus.Text = "New settlement calculations. Reconcile and save.";
                        lblStatus.ForeColor = Theme.TextDark;
                    }

                    // Update Metrics Cards Display
                    lblGrossSalesVal.Text = $"Rs. {todayGrossSales:N2}";
                    lblDuesCreatedVal.Text = $"Rs. {todayDuesCreated:N2}";
                    lblCardQRSalesVal.Text = $"Rs. {todayCardQRSales:N2}";
                    lblCashSalesVal.Text = $"Rs. {todayCashSales:N2}";
                    lblOpeningCashVal.Text = $"Rs. {dbOpening:N2}";
                    lblDueCollectionsVal.Text = $"Rs. {todayDueCollections:N2}";
                    lblRefundsVal.Text = $"Rs. {todayRefunds:N2}";

                    // Run Variance Update
                    RecalculateVariance();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading daily metrics: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RecalculateVariance()
        {
            decimal opening = 0m;
            decimal actual = 0m;

            decimal.TryParse(txtOpeningCash.Text.Trim(), out opening);
            decimal.TryParse(txtActualCash.Text.Trim(), out actual);

            // Expected Cash Math Formula
            expectedCash = opening + todayCashSales + todayDueCollections - todayRefunds;

            decimal variance = actual - expectedCash;

            txtExpectedCashDisplay.Text = expectedCash.ToString("0.00");
            lblExpectedCashVal.Text = $"Rs. {expectedCash:N2}";

            txtVariance.Text = (variance >= 0 ? "+" : "") + variance.ToString("0.00");

            if (variance == 0m)
            {
                txtVariance.BackColor = Theme.Success;
                txtVariance.ForeColor = Theme.TextLight;
            }
            else if (variance > 0m)
            {
                txtVariance.BackColor = Theme.Warning;
                txtVariance.ForeColor = Color.Black;
            }
            else
            {
                txtVariance.BackColor = Theme.Danger;
                txtVariance.ForeColor = Theme.TextLight;
            }
        }

        private void InputCash_TextChanged(object sender, EventArgs e)
        {
            if (isUpdatingInputs) return;
            RecalculateVariance();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            decimal opening = 0m;
            decimal actual = 0m;

            if (!decimal.TryParse(txtOpeningCash.Text.Trim(), out opening) || opening < 0)
            {
                MessageBox.Show("Please enter a valid non-negative opening cash value.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtActualCash.Text.Trim(), out actual) || actual < 0)
            {
                MessageBox.Show("Please enter a valid non-negative actual cash counted value.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal variance = actual - expectedCash;
            string remarks = txtRemarks.Text.Trim();
            DateTime targetDate = dtpSettlementDate.Value;

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    bool alreadyExists = false;
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM DailySettlements WHERE CAST(SettlementDate as DATE) = @date", conn))
                    {
                        cmd.Parameters.AddWithValue("@date", targetDate.Date);
                        alreadyExists = (int)cmd.ExecuteScalar() > 0;
                    }

                    if (alreadyExists)
                    {
                        DialogResult overwrite = MessageBox.Show($"A daily settlement has already been saved for {targetDate.ToShortDateString()}.\nWould you like to overwrite it with current values?", "Confirm Overwrite", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (overwrite != DialogResult.Yes) return;

                        string updateSql = @"
                            UPDATE DailySettlements 
                            SET OpeningCash = @opening,
                                CashSales = @cashSales,
                                DueCollections = @dueCollections,
                                CardQRSales = @cardSales,
                                DuesCreated = @duesCreated,
                                ExpectedCash = @expected,
                                ActualCash = @actual,
                                Variance = @variance,
                                Remarks = @remarks,
                                Refunds = @refunds,
                                SettlementBy = @userId
                            WHERE CAST(SettlementDate as DATE) = @date";

                        using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@opening", opening);
                            cmd.Parameters.AddWithValue("@cashSales", todayCashSales);
                            cmd.Parameters.AddWithValue("@dueCollections", todayDueCollections);
                            cmd.Parameters.AddWithValue("@cardSales", todayCardQRSales);
                            cmd.Parameters.AddWithValue("@duesCreated", todayDuesCreated);
                            cmd.Parameters.AddWithValue("@expected", expectedCash);
                            cmd.Parameters.AddWithValue("@actual", actual);
                            cmd.Parameters.AddWithValue("@variance", variance);
                            cmd.Parameters.AddWithValue("@remarks", remarks);
                            cmd.Parameters.AddWithValue("@refunds", todayRefunds);
                            cmd.Parameters.AddWithValue("@userId", Session.UserId);
                            cmd.Parameters.AddWithValue("@date", targetDate.Date);
                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("Daily settlement updated successfully!", "Settlement Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        string insertSql = @"
                            INSERT INTO DailySettlements (SettlementDate, OpeningCash, CashSales, DueCollections, CardQRSales, DuesCreated, ExpectedCash, ActualCash, Variance, Remarks, Refunds, SettlementBy)
                            VALUES (@date, @opening, @cashSales, @dueCollections, @cardSales, @duesCreated, @expected, @actual, @variance, @remarks, @refunds, @userId)";

                        using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@date", targetDate);
                            cmd.Parameters.AddWithValue("@opening", opening);
                            cmd.Parameters.AddWithValue("@cashSales", todayCashSales);
                            cmd.Parameters.AddWithValue("@dueCollections", todayDueCollections);
                            cmd.Parameters.AddWithValue("@cardSales", todayCardQRSales);
                            cmd.Parameters.AddWithValue("@duesCreated", todayDuesCreated);
                            cmd.Parameters.AddWithValue("@expected", expectedCash);
                            cmd.Parameters.AddWithValue("@actual", actual);
                            cmd.Parameters.AddWithValue("@variance", variance);
                            cmd.Parameters.AddWithValue("@remarks", remarks);
                            cmd.Parameters.AddWithValue("@refunds", todayRefunds);
                            cmd.Parameters.AddWithValue("@userId", Session.UserId);
                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("Daily settlement saved successfully!", "Settlement Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    LoadHistoryLog();
                    LoadDailyMetrics(targetDate);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save settlement: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadHistoryLog()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT ds.SettlementDate as [Date],
                               ds.OpeningCash as [Opening],
                               (ds.CashSales + ds.DueCollections - ds.Refunds) as [Cash Inflow],
                               ds.ExpectedCash as [Expected],
                               ds.ActualCash as [Actual],
                               ds.Variance as [Variance],
                               u.Username as [Reconciled By]
                        FROM DailySettlements ds
                        LEFT JOIN Users u ON ds.SettlementBy = u.Id
                        ORDER BY ds.SettlementDate DESC";

                    using (SqlDataAdapter da = new SqlDataAdapter(query, conn))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        gridHistory.DataSource = dt;

                        // Formatting columns nicely
                        if (gridHistory.Columns["Opening"] != null) gridHistory.Columns["Opening"].DefaultCellStyle.Format = "N2";
                        if (gridHistory.Columns["Cash Inflow"] != null) gridHistory.Columns["Cash Inflow"].DefaultCellStyle.Format = "N2";
                        if (gridHistory.Columns["Expected"] != null) gridHistory.Columns["Expected"].DefaultCellStyle.Format = "N2";
                        if (gridHistory.Columns["Actual"] != null) gridHistory.Columns["Actual"].DefaultCellStyle.Format = "N2";
                        if (gridHistory.Columns["Variance"] != null) gridHistory.Columns["Variance"].DefaultCellStyle.Format = "N2";
                        if (gridHistory.Columns["Date"] != null) gridHistory.Columns["Date"].DefaultCellStyle.Format = "yyyy-MM-dd";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading history log: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GridHistory_SelectionChanged(object sender, EventArgs e)
        {
            if (gridHistory.SelectedRows.Count > 0)
            {
                DataGridViewRow row = gridHistory.SelectedRows[0];
                if (row.Cells["Date"].Value != null && row.Cells["Date"].Value != DBNull.Value)
                {
                    DateTime date = Convert.ToDateTime(row.Cells["Date"].Value);
                    dtpSettlementDate.Value = date;
                }
            }
        }
    }
}
