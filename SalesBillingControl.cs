using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace MeroHardwareDokan
{
    public class SalesBillingControl : UserControl
    {
        private ComboBox comboCustomer;
        private TextBox txtProductCode;
        private TextBox txtQty;
        private TextBox txtPrice;
        private Label lblAvailableStock;
        private DataGridView gridCart;

        private Label lblSubTotal;
        private TextBox txtDiscount;
        private TextBox txtTax;
        private Label lblGrandTotal;
        private ComboBox comboPaymentMethod;
        private CheckBox chkTaxInclusive;
        private CheckBox chkExcludeTax;
        private CheckBox chkBillNotRequired;
        private ComboBox comboTaxType;
        private TextBox txtAmountPaid;
        private Label lblDueAmountVal;
        private bool isAutoUpdatingPaidAmount = false;

        private Button btnAddItem;
        private Label lblQty;

        // Barcode scanning state
        private int currentProductId = 0;
        private string currentProductCode = "";
        private string currentProductName = "";
        private Button btnRemoveItem;
        private Button btnCheckout;

        private DataTable cartTable;
        private decimal subTotal = 0;
        private decimal discount = 0;
        private decimal tax = 0;
        private decimal grandTotal = 0;
        private decimal currentSelectedStock = 0;
        private decimal currentProductTaxPercent = 13.00m;

        // Print Document Fields
        private PrintDocument invoiceDoc;
        private PrintPreviewDialog previewDlg;
        private int lastSaleId = 0;

        public SalesBillingControl()
        {
            InitializeComponent();
            LoadDropdownData();
            InitializeCart();
            this.ActiveControl = txtProductCode;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            txtProductCode.Focus();
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
            lblHeader.Text = "Retail Sales Billing Checkout Terminal";
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
            this.Controls.Add(lblHeader);

            // LEFT PANEL: Product checkout selection
            Panel entryPanel = Theme.CreateCard(360, 520);
            entryPanel.Location = new Point(20, 65);
            entryPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;

            Label lblEntryHeader = new Label();
            lblEntryHeader.Text = "Billing Operations";
            lblEntryHeader.Location = new Point(15, 15);
            Theme.StyleLabel(lblEntryHeader, Theme.TextLight, Theme.SubHeaderFont);
            entryPanel.Controls.Add(lblEntryHeader);

            // Customer Select
            Label lblCust = new Label();
            lblCust.Text = "Customer Name *";
            lblCust.Location = new Point(15, 55);
            lblCust.AutoSize = true;
            Theme.StyleLabel(lblCust, Theme.TextLight, Theme.BoldFont);
            entryPanel.Controls.Add(lblCust);

            comboCustomer = new ComboBox();
            comboCustomer.Size = new Size(330, 30);
            comboCustomer.Location = new Point(15, 78);
            comboCustomer.DropDownStyle = ComboBoxStyle.DropDownList;
            comboCustomer.BackColor = Theme.Primary;
            comboCustomer.ForeColor = Theme.TextLight;
            comboCustomer.Font = Theme.MainFont;
            entryPanel.Controls.Add(comboCustomer);

            // Product Select
            Label lblProd = new Label();
            lblProd.Text = "Enter Product Code / Barcode *";
            lblProd.Location = new Point(15, 130);
            lblProd.AutoSize = true;
            Theme.StyleLabel(lblProd, Theme.TextLight, Theme.BoldFont);
            entryPanel.Controls.Add(lblProd);

            txtProductCode = new TextBox();
            txtProductCode.Size = new Size(210, 30);
            txtProductCode.Location = new Point(15, 153);
            Theme.StyleTextBox(txtProductCode);
            txtProductCode.KeyDown += TxtProductCode_KeyDown;
            entryPanel.Controls.Add(txtProductCode);

            Button btnSearchProduct = new Button();
            btnSearchProduct.Text = "🔍 Find (F3)";
            btnSearchProduct.Size = new Size(110, 30);
            btnSearchProduct.Location = new Point(235, 152);
            Theme.StylePrimaryButton(btnSearchProduct);
            btnSearchProduct.Click += BtnSearchProduct_Click;
            entryPanel.Controls.Add(btnSearchProduct);

            // Live Stock Alert Info Label
            lblAvailableStock = new Label();
            lblAvailableStock.Text = "Available Stock: --";
            lblAvailableStock.Location = new Point(15, 195);
            lblAvailableStock.AutoSize = true;
            Theme.StyleLabel(lblAvailableStock, Theme.Warning, Theme.BoldFont);
            entryPanel.Controls.Add(lblAvailableStock);

            // Unit Sales Price
            Label lblPrice = new Label();
            lblPrice.Text = "Sales Unit Price (Rs.) *";
            lblPrice.Location = new Point(15, 230);
            lblPrice.AutoSize = true;
            Theme.StyleLabel(lblPrice, Theme.TextLight, Theme.BoldFont);
            entryPanel.Controls.Add(lblPrice);

            txtPrice = new TextBox();
            txtPrice.Size = new Size(330, 30);
            txtPrice.Location = new Point(15, 255);
            Theme.StyleTextBox(txtPrice);
            txtPrice.KeyDown += TxtQty_KeyDown;
            entryPanel.Controls.Add(txtPrice);

            // Quantity to Sell
            lblQty = new Label();
            lblQty.Text = "Sales Quantity *";
            lblQty.Location = new Point(15, 310);
            lblQty.AutoSize = true;
            Theme.StyleLabel(lblQty, Theme.TextLight, Theme.BoldFont);
            entryPanel.Controls.Add(lblQty);

            txtQty = new TextBox();
            txtQty.Size = new Size(330, 30);
            txtQty.Location = new Point(15, 335);
            Theme.StyleTextBox(txtQty);
            txtQty.Text = "1";
            txtQty.KeyDown += TxtQty_KeyDown;
            entryPanel.Controls.Add(txtQty);

            // Add Item Button
            btnAddItem = new Button();
            btnAddItem.Text = "🛒 Add to Cart";
            btnAddItem.Size = new Size(330, 45);
            btnAddItem.Location = new Point(15, 395);
            Theme.StyleSuccessButton(btnAddItem);
            btnAddItem.Click += BtnAddItem_Click;
            entryPanel.Controls.Add(btnAddItem);

            this.Controls.Add(entryPanel);

            // RIGHT PANEL: Cart Grid & Complex Live Calculator
            gridCart = new DataGridView();
            gridCart.Size = new Size(530, 310);
            gridCart.Location = new Point(400, 65);
            gridCart.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridCart);
            this.Controls.Add(gridCart);

            // Remove item button
            btnRemoveItem = new Button();
            btnRemoveItem.Text = "❌ Remove Item"; // Cleaner and fits perfectly in Large Font
            btnRemoveItem.Size = new Size(200, 40); // Expanded width and height to prevent text clipping
            btnRemoveItem.Location = new Point(400, 380); // Adjusted Y coordinate
            btnRemoveItem.UseCompatibleTextRendering = true; // Force high-DPI text rendering compatibility
            btnRemoveItem.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            Theme.StyleDangerButton(btnRemoveItem);
            btnRemoveItem.Click += BtnRemoveItem_Click;
            this.Controls.Add(btnRemoveItem);

            // Complex live checkout panel
            Panel checkoutPanel = Theme.CreateCard(530, 215);
            checkoutPanel.Location = new Point(400, 425);
            checkoutPanel.BackColor = Color.FromArgb(17, 24, 39);
            checkoutPanel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            // SubTotal
            Label lblSub = new Label();
            lblSub.Text = "SubTotal:";
            lblSub.Location = new Point(15, 10);
            Theme.StyleLabel(lblSub, Theme.TextDark, Theme.BoldFont);
            checkoutPanel.Controls.Add(lblSub);

            lblSubTotal = new Label();
            lblSubTotal.Text = "Rs. 0.00";
            lblSubTotal.Location = new Point(150, 10); // Shifted from 120 to 150 to prevent overlap
            lblSubTotal.AutoSize = true;
            Theme.StyleLabel(lblSubTotal, Theme.TextLight, Theme.BoldFont);
            checkoutPanel.Controls.Add(lblSubTotal);

            // Discount Input
            Label lblDisc = new Label();
            lblDisc.Text = "Discount (Rs.):";
            lblDisc.Location = new Point(15, 35);
            Theme.StyleLabel(lblDisc, Theme.TextDark, Theme.BoldFont);
            checkoutPanel.Controls.Add(lblDisc);

            txtDiscount = new TextBox();
            txtDiscount.Size = new Size(90, 24); // Slightly reduced width to fit beautifully
            txtDiscount.Location = new Point(150, 32); // Shifted from 120 to 150
            Theme.StyleTextBox(txtDiscount);
            txtDiscount.Text = "0.00";
            txtDiscount.TextChanged += CalculatorInput_Changed;
            checkoutPanel.Controls.Add(txtDiscount);

            // Tax (VAT / Tax) Calculated Output (Read-only)
            Label lblTx = new Label();
            lblTx.Text = "VAT / Tax (Rs.):";
            lblTx.Location = new Point(15, 60);
            Theme.StyleLabel(lblTx, Theme.TextDark, Theme.BoldFont);
            checkoutPanel.Controls.Add(lblTx);

            txtTax = new TextBox();
            txtTax.Size = new Size(90, 24); // Adjusted width
            txtTax.Location = new Point(150, 57); // Shifted from 120 to 150
            txtTax.ReadOnly = true;
            txtTax.BackColor = Theme.AlternateRow;
            Theme.StyleTextBox(txtTax);
            txtTax.Text = "0.00";
            checkoutPanel.Controls.Add(txtTax);

            // Payment Method
            Label lblPay = new Label();
            lblPay.Text = "Payment Mode:";
            lblPay.Location = new Point(15, 85);
            Theme.StyleLabel(lblPay, Theme.TextDark, Theme.BoldFont);
            checkoutPanel.Controls.Add(lblPay);

            comboPaymentMethod = new ComboBox();
            comboPaymentMethod.Size = new Size(90, 28); // Adjusted width
            comboPaymentMethod.Location = new Point(150, 82); // Shifted from 120 to 150
            comboPaymentMethod.DropDownStyle = ComboBoxStyle.DropDownList;
            comboPaymentMethod.Items.AddRange(new string[] { "Cash", "Card", "QR Pay" });
            comboPaymentMethod.SelectedIndex = 0;
            comboPaymentMethod.BackColor = Theme.Primary;
            comboPaymentMethod.ForeColor = Theme.TextLight;
            comboPaymentMethod.Font = Theme.MainFont;
            checkoutPanel.Controls.Add(comboPaymentMethod);

            // Tax Type (Local vs Interstate)
            Label lblTaxType = new Label();
            lblTaxType.Text = "GST Type:";
            lblTaxType.Location = new Point(15, 112);
            Theme.StyleLabel(lblTaxType, Theme.TextDark, Theme.BoldFont);
            checkoutPanel.Controls.Add(lblTaxType);

            comboTaxType = new ComboBox();
            comboTaxType.Size = new Size(90, 28);
            comboTaxType.Location = new Point(150, 109);
            comboTaxType.DropDownStyle = ComboBoxStyle.DropDownList;
            comboTaxType.Items.AddRange(new string[] { "Local", "Inter-State" });
            comboTaxType.SelectedIndex = 0;
            comboTaxType.BackColor = Theme.Primary;
            comboTaxType.ForeColor = Theme.TextLight;
            comboTaxType.Font = Theme.MainFont;
            comboTaxType.SelectedIndexChanged += CalculatorInput_Changed;
            checkoutPanel.Controls.Add(comboTaxType);

            // Tax Inclusive CheckBox
            chkTaxInclusive = new CheckBox();
            chkTaxInclusive.Text = "Tax Inclusive";
            chkTaxInclusive.Location = new Point(15, 138);
            chkTaxInclusive.ForeColor = Theme.TextLight;
            chkTaxInclusive.BackColor = Color.Transparent;
            chkTaxInclusive.Font = Theme.BoldFont;
            chkTaxInclusive.Size = new Size(115, 22);
            chkTaxInclusive.CheckedChanged += CalculatorInput_Changed;
            checkoutPanel.Controls.Add(chkTaxInclusive);

            // Exclude Tax CheckBox
            chkExcludeTax = new CheckBox();
            chkExcludeTax.Text = "Exclude Tax";
            chkExcludeTax.Location = new Point(135, 138);
            chkExcludeTax.ForeColor = Theme.TextLight;
            chkExcludeTax.BackColor = Color.Transparent;
            chkExcludeTax.Font = Theme.BoldFont;
            chkExcludeTax.Size = new Size(115, 22);
            chkExcludeTax.CheckedChanged += CalculatorInput_Changed;
            chkExcludeTax.Checked = true;
            checkoutPanel.Controls.Add(chkExcludeTax);

            // Amount Paid Input
            Label lblAmountPaid = new Label();
            lblAmountPaid.Text = "Amount Paid:";
            lblAmountPaid.Location = new Point(15, 164);
            Theme.StyleLabel(lblAmountPaid, Theme.TextDark, Theme.BoldFont);
            checkoutPanel.Controls.Add(lblAmountPaid);

            txtAmountPaid = new TextBox();
            txtAmountPaid.Size = new Size(90, 24);
            txtAmountPaid.Location = new Point(150, 161);
            Theme.StyleTextBox(txtAmountPaid);
            txtAmountPaid.Text = "0.00";
            txtAmountPaid.TextChanged += CalculatorInput_Changed;
            checkoutPanel.Controls.Add(txtAmountPaid);

            // Due Amount Display
            Label lblDueAmt = new Label();
            lblDueAmt.Text = "Due Amount:";
            lblDueAmt.Location = new Point(15, 190);
            Theme.StyleLabel(lblDueAmt, Theme.TextDark, Theme.BoldFont);
            checkoutPanel.Controls.Add(lblDueAmt);

            lblDueAmountVal = new Label();
            lblDueAmountVal.Text = "Rs. 0.00";
            lblDueAmountVal.Location = new Point(150, 190);
            lblDueAmountVal.AutoSize = true;
            Theme.StyleLabel(lblDueAmountVal, Theme.Success, Theme.BoldFont);
            checkoutPanel.Controls.Add(lblDueAmountVal);

            // Divider vertical
            Panel div = new Panel();
            div.Size = new Size(1, 195);
            div.Location = new Point(255, 10); // Shifted slightly to center perfectly
            div.BackColor = Theme.Secondary;
            checkoutPanel.Controls.Add(div);

            // Grand Total (Big display)
            Label lblGrand = new Label();
            lblGrand.Text = "GRAND TOTAL";
            lblGrand.Location = new Point(270, 15);
            lblGrand.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Theme.StyleLabel(lblGrand, Theme.TextDark, new Font("Segoe UI Semibold", 8F, FontStyle.Bold));
            checkoutPanel.Controls.Add(lblGrand);

            lblGrandTotal = new Label();
            lblGrandTotal.Text = "Rs. 0.00";
            lblGrandTotal.Location = new Point(270, 35);
            lblGrandTotal.AutoSize = true;
            lblGrandTotal.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Theme.StyleLabel(lblGrandTotal, Theme.Success, new Font("Segoe UI", 22F, FontStyle.Bold));
            checkoutPanel.Controls.Add(lblGrandTotal);

            // Bill Not Required Checkbox
            chkBillNotRequired = new CheckBox();
            chkBillNotRequired.Text = "Bill Not Required";
            chkBillNotRequired.Location = new Point(286, 100);
            chkBillNotRequired.AutoSize = true;
            chkBillNotRequired.Checked = true;
            chkBillNotRequired.ForeColor = Theme.TextLight;
            chkBillNotRequired.BackColor = Color.Transparent;
            chkBillNotRequired.Font = Theme.BoldFont;
            chkBillNotRequired.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            checkoutPanel.Controls.Add(chkBillNotRequired);

            // Checkout Button
            btnCheckout = new Button();
            btnCheckout.Text = "🖨️ Checkout & Print"; // Mixed-case fits much better under scaling
            btnCheckout.Size = new Size(245, 55);
            btnCheckout.Location = new Point(272, 125);
            btnCheckout.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCheckout.UseCompatibleTextRendering = true;
            Theme.StylePrimaryButton(btnCheckout);
            btnCheckout.Click += BtnCheckout_Click;
            checkoutPanel.Controls.Add(btnCheckout);

            this.Controls.Add(checkoutPanel);

            // Setup Print Elements
            invoiceDoc = new PrintDocument();
            invoiceDoc.PrintPage += InvoiceDoc_PrintPage;
            previewDlg = new PrintPreviewDialog();
            previewDlg.Document = invoiceDoc;
            previewDlg.Size = new Size(600, 700);
        }

        private void InitializeCart()
        {
            cartTable = new DataTable();
            cartTable.Columns.Add("ProductId", typeof(int));
            cartTable.Columns.Add("Code", typeof(string));
            cartTable.Columns.Add("Name", typeof(string));
            cartTable.Columns.Add("Qty", typeof(decimal));
            cartTable.Columns.Add("Unit", typeof(string));
            cartTable.Columns.Add("Price", typeof(decimal));
            cartTable.Columns.Add("TaxPercent", typeof(decimal));
            cartTable.Columns.Add("TaxAmount", typeof(decimal));
            cartTable.Columns.Add("Total", typeof(decimal));

            gridCart.DataSource = cartTable;
            
            if (gridCart.Columns["ProductId"] != null) gridCart.Columns["ProductId"].Visible = false;

            // Enable inline editing for Qty column in the grid
            gridCart.ReadOnly = false;
            foreach (DataGridViewColumn col in gridCart.Columns)
            {
                if (col.Name != "Qty")
                {
                    col.ReadOnly = true;
                }
                else
                {
                    col.ReadOnly = false;
                }
            }

            // Set Header Texts and Formatting
            if (gridCart.Columns["Code"] != null) gridCart.Columns["Code"].HeaderText = "Code";
            if (gridCart.Columns["Name"] != null) gridCart.Columns["Name"].HeaderText = "Product Name";
            if (gridCart.Columns["Qty"] != null)
            {
                gridCart.Columns["Qty"].HeaderText = "Qty";
                gridCart.Columns["Qty"].DefaultCellStyle.Format = "0.###";
            }
            if (gridCart.Columns["Unit"] != null) gridCart.Columns["Unit"].HeaderText = "Unit";
            if (gridCart.Columns["Price"] != null)
            {
                gridCart.Columns["Price"].HeaderText = "Price";
                gridCart.Columns["Price"].DefaultCellStyle.Format = "N2";
            }
            if (gridCart.Columns["TaxPercent"] != null)
            {
                gridCart.Columns["TaxPercent"].HeaderText = "Tax Slab (%)";
                gridCart.Columns["TaxPercent"].DefaultCellStyle.Format = "0.##";
            }
            if (gridCart.Columns["TaxAmount"] != null)
            {
                gridCart.Columns["TaxAmount"].HeaderText = "Tax Amount";
                gridCart.Columns["TaxAmount"].DefaultCellStyle.Format = "N2";
            }
            if (gridCart.Columns["Total"] != null)
            {
                gridCart.Columns["Total"].HeaderText = "Total";
                gridCart.Columns["Total"].DefaultCellStyle.Format = "N2";
            }

            // Set custom fill weights for beautiful proportional layout
            if (gridCart.Columns["Code"] != null) gridCart.Columns["Code"].FillWeight = 50;
            if (gridCart.Columns["Name"] != null) gridCart.Columns["Name"].FillWeight = 150;
            if (gridCart.Columns["Qty"] != null) gridCart.Columns["Qty"].FillWeight = 50;
            if (gridCart.Columns["Unit"] != null) gridCart.Columns["Unit"].FillWeight = 40;
            if (gridCart.Columns["Price"] != null) gridCart.Columns["Price"].FillWeight = 60;
            if (gridCart.Columns["TaxPercent"] != null) gridCart.Columns["TaxPercent"].FillWeight = 60;
            if (gridCart.Columns["TaxAmount"] != null) gridCart.Columns["TaxAmount"].FillWeight = 65;
            if (gridCart.Columns["Total"] != null) gridCart.Columns["Total"].FillWeight = 70;

            gridCart.CellValueChanged += GridCart_CellValueChanged;
            gridCart.CellValidating += GridCart_CellValidating;
        }

        private void GridCart_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (gridCart.Columns[e.ColumnIndex].Name == "Qty")
            {
                string input = e.FormattedValue.ToString();
                if (!decimal.TryParse(input, out decimal newQty) || newQty <= 0)
                {
                    MessageBox.Show("Please enter a valid positive quantity.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                    return;
                }

                int prodId = Convert.ToInt32(gridCart.Rows[e.RowIndex].Cells["ProductId"].Value);
                
                decimal dbStock = 0;
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("SELECT Stock FROM Products WHERE Id = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", prodId);
                            dbStock = Convert.ToDecimal(cmd.ExecuteScalar());
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Database error during validation: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    e.Cancel = true;
                    return;
                }

                if (newQty > dbStock)
                {
                    MessageBox.Show($"Insufficient stock! Active inventory only has {dbStock:0.###} unit(s) of this item.", "Out of Stock Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                    return;
                }
            }
        }

        private void GridCart_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (gridCart.Columns[e.ColumnIndex].Name == "Qty")
            {
                DataGridViewRow row = gridCart.Rows[e.RowIndex];
                decimal qty = Convert.ToDecimal(row.Cells["Qty"].Value);
                decimal price = Convert.ToDecimal(row.Cells["Price"].Value);
                
                row.Cells["Total"].Value = qty * price;

                CalculateCheckoutTotals();

                int prodId = Convert.ToInt32(row.Cells["ProductId"].Value);
                if (prodId == currentProductId)
                {
                    RefreshAvailableStockDisplay();
                }
            }
        }



        private void LoadDropdownData()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();

                    // Customers
                    using (SqlCommand cmd = new SqlCommand("SELECT Id, Name FROM Customers ORDER BY Name ASC", conn))
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            comboCustomer.DataSource = dt;
                            comboCustomer.DisplayMember = "Name";
                            comboCustomer.ValueMember = "Id";

                            // Set default to Walk-in Customer if exists
                            for (int i = 0; i < comboCustomer.Items.Count; i++)
                            {
                                DataRowView drv = comboCustomer.Items[i] as DataRowView;
                                if (drv["Name"].ToString() == "Walk-in Customer")
                                {
                                    comboCustomer.SelectedIndex = i;
                                    break;
                                }
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading checkout directories: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TxtProductCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                LoadProductByCode(txtProductCode.Text);
            }
            else if (e.KeyCode == Keys.F3)
            {
                e.SuppressKeyPress = true;
                BtnSearchProduct_Click(null, null);
            }
        }

        private void TxtQty_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                BtnAddItem_Click(null, null);
            }
        }

        private void LoadProductByCode(string code)
        {
            code = code.Trim();
            if (string.IsNullOrEmpty(code)) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT Id, Code, Name, SalesPrice, Stock, TaxPercent, Unit FROM Products WHERE Code = @code", conn))
                    {
                        cmd.Parameters.AddWithValue("@code", code);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                currentProductId = Convert.ToInt32(r["Id"]);
                                currentProductCode = r["Code"].ToString();
                                currentProductName = r["Name"].ToString();
                                txtPrice.Text = Convert.ToDecimal(r["SalesPrice"]).ToString("0.00");
                                currentSelectedStock = Convert.ToDecimal(r["Stock"]);
                                currentProductTaxPercent = Convert.ToDecimal(r["TaxPercent"]);
                                string unit = r["Unit"]?.ToString() ?? "Pcs";
                                if (lblQty != null) lblQty.Text = $"Sales Quantity ({unit}) *";
                                
                                RefreshAvailableStockDisplay();

                                // Automatically add the scanned product to the cart
                                BtnAddItem_Click(null, null);
                            }
                            else
                            {
                                lblAvailableStock.Text = "Product not found!";
                                lblAvailableStock.ForeColor = Theme.Danger;
                                currentProductId = 0;
                                currentProductCode = "";
                                currentProductName = "";
                                txtPrice.Text = "0.00";
                                currentProductTaxPercent = 0m;
                                if (lblQty != null) lblQty.Text = "Sales Quantity *";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading product: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshAvailableStockDisplay()
        {
            if (currentProductId == 0)
            {
                lblAvailableStock.Text = "Available Stock: --";
                lblAvailableStock.ForeColor = Theme.Warning;
                return;
            }

            decimal cartQty = 0;
            string unit = "Pcs";
            foreach (DataRow row in cartTable.Rows)
            {
                if ((int)row["ProductId"] == currentProductId)
                {
                    cartQty = (decimal)row["Qty"];
                    break;
                }
            }
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT Unit FROM Products WHERE Id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", currentProductId);
                        object res = cmd.ExecuteScalar();
                        if (res != null) unit = res.ToString();
                    }
                }
            }
            catch {}

            decimal available = currentSelectedStock - cartQty;
            lblAvailableStock.Text = $"Available Stock: {available:0.###} {unit}";
            
            if (available <= 0)
            {
                lblAvailableStock.ForeColor = Theme.Danger;
            }
            else if (available < 5)
            {
                lblAvailableStock.ForeColor = Theme.Warning;
            }
            else
            {
                lblAvailableStock.ForeColor = Theme.Success;
            }
        }

        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            if (currentProductId == 0)
            {
                MessageBox.Show("Please scan or enter a valid product code first.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int prodId = currentProductId;
            if (!decimal.TryParse(txtQty.Text.Trim(), out decimal qty) || qty <= 0)
            {
                MessageBox.Show("Please enter a valid quantity greater than 0.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtPrice.Text.Trim(), out decimal price) || price < 0)
            {
                MessageBox.Show("Please enter a valid sales price.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Real-time Stock Check
            decimal cartQty = 0;
            foreach (DataRow row in cartTable.Rows)
            {
                if ((int)row["ProductId"] == prodId)
                {
                    cartQty = (decimal)row["Qty"];
                    break;
                }
            }

            if ((cartQty + qty) > currentSelectedStock)
            {
                MessageBox.Show($"Insufficient stock! You only have {currentSelectedStock:0.###} in inventory. Cart currently holds {cartQty:0.###}.", "Out of Stock Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string code = currentProductCode;
            string name = currentProductName;
            
            string unit = "Pcs";
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT Unit FROM Products WHERE Id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", prodId);
                        object res = cmd.ExecuteScalar();
                        if (res != null) unit = res.ToString();
                    }
                }
            }
            catch {}

            bool found = false;
            foreach (DataRow row in cartTable.Rows)
            {
                if ((int)row["ProductId"] == prodId)
                {
                    row["Qty"] = (decimal)row["Qty"] + qty;
                    row["Price"] = price;
                    row["Total"] = (decimal)row["Qty"] * price;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                cartTable.Rows.Add(prodId, code, name, qty, unit, price, currentProductTaxPercent, 0m, qty * price);
            }

            CalculateCheckoutTotals();

            // Clear input fields for next barcode scan
            txtProductCode.Clear();
            txtPrice.Text = "0.00";
            txtQty.Text = "1";
            currentProductId = 0;
            currentProductCode = "";
            currentProductName = "";
            lblAvailableStock.Text = "Available Stock: --";
            lblAvailableStock.ForeColor = Theme.Warning;
            if (lblQty != null) lblQty.Text = "Sales Quantity *";
            txtProductCode.Focus();
        }

        private void BtnRemoveItem_Click(object sender, EventArgs e)
        {
            if (gridCart.SelectedRows.Count == 0)
            {
                MessageBox.Show("Select a checkout cart item to remove.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            gridCart.Rows.Remove(gridCart.SelectedRows[0]);
            CalculateCheckoutTotals();
            RefreshAvailableStockDisplay();
        }

        private void CalculatorInput_Changed(object sender, EventArgs e)
        {
            if (txtDiscount == null || txtTax == null || lblGrandTotal == null || cartTable == null)
                return;

            decimal.TryParse(txtDiscount.Text.Trim(), out discount);
            if (discount < 0) discount = 0;
            if (discount > subTotal) discount = subTotal;

            bool isInclusive = chkTaxInclusive != null && chkTaxInclusive.Checked;
            bool excludeTax = chkExcludeTax != null && chkExcludeTax.Checked;

            if (excludeTax)
            {
                if (chkTaxInclusive != null) { chkTaxInclusive.Enabled = false; }
                if (comboTaxType != null) { comboTaxType.Enabled = false; }
            }
            else
            {
                if (chkTaxInclusive != null) { chkTaxInclusive.Enabled = true; }
                if (comboTaxType != null) { comboTaxType.Enabled = true; }
            }

            decimal totalTax = 0;
            foreach (DataRow row in cartTable.Rows)
            {
                decimal itemTotal = Convert.ToDecimal(row["Total"]);
                decimal itemTaxPercent = Convert.ToDecimal(row["TaxPercent"]);
                
                decimal itemDiscount = subTotal > 0 ? (discount * itemTotal / subTotal) : 0;
                decimal taxableAmount;
                decimal itemTax;

                if (excludeTax)
                {
                    itemTax = 0m;
                }
                else if (isInclusive)
                {
                    taxableAmount = (itemTotal - itemDiscount) / (1 + itemTaxPercent / 100m);
                    itemTax = (itemTotal - itemDiscount) - taxableAmount;
                }
                else
                {
                    taxableAmount = itemTotal - itemDiscount;
                    itemTax = taxableAmount * (itemTaxPercent / 100m);
                }
                
                row["TaxAmount"] = itemTax;
                totalTax += itemTax;
            }

            tax = totalTax;
            txtTax.Text = tax.ToString("0.00");
            
            if (isInclusive || excludeTax)
            {
                grandTotal = subTotal - discount;
            }
            else
            {
                grandTotal = (subTotal - discount) + tax;
            }

            if (grandTotal < 0) grandTotal = 0;

            lblGrandTotal.Text = $"Rs. {grandTotal:N2}";

            // Calculate Paid Amount and Due Amount
            if (txtAmountPaid != null)
            {
                if (sender != txtAmountPaid && !isAutoUpdatingPaidAmount)
                {
                    isAutoUpdatingPaidAmount = true;
                    txtAmountPaid.Text = grandTotal.ToString("0.00");
                    isAutoUpdatingPaidAmount = false;
                }

                decimal paid = 0;
                decimal.TryParse(txtAmountPaid.Text.Trim(), out paid);
                if (paid < 0) paid = 0;

                decimal due = grandTotal - paid;
                if (due < 0) due = 0;

                if (lblDueAmountVal != null)
                {
                    lblDueAmountVal.Text = $"Rs. {due:N2}";
                    if (due > 0)
                    {
                        lblDueAmountVal.ForeColor = Theme.Danger;
                    }
                    else
                    {
                        lblDueAmountVal.ForeColor = Theme.Success;
                    }
                }
            }
        }

        private void CalculateCheckoutTotals()
        {
            subTotal = 0;
            foreach (DataRow row in cartTable.Rows)
            {
                subTotal += Convert.ToDecimal(row["Total"]);
            }
            lblSubTotal.Text = $"Rs. {subTotal:N2}";

            // Recalculate tax slabs and grand totals
            CalculatorInput_Changed(null, null);
        }

        private void BtnCheckout_Click(object sender, EventArgs e)
        {
            if (comboCustomer.SelectedValue == null)
            {
                MessageBox.Show("Please select a Customer.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cartTable.Rows.Count == 0)
            {
                MessageBox.Show("Billing cart is empty. Please add items to checkout.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Parse and validate Paid/Due Amounts
            decimal amountPaid = grandTotal;
            if (txtAmountPaid != null)
            {
                decimal.TryParse(txtAmountPaid.Text.Trim(), out amountPaid);
            }
            if (amountPaid < 0) amountPaid = 0;
            if (amountPaid > grandTotal) amountPaid = grandTotal;

            decimal dueAmount = grandTotal - amountPaid;
            if (dueAmount < 0) dueAmount = 0;

            int customerId = (int)comboCustomer.SelectedValue;

            // Restrict dues for Walk-in Customer
            if (dueAmount > 0)
            {
                string custName = comboCustomer.Text;
                if (custName == "Walk-in Customer")
                {
                    DialogResult res = MessageBox.Show(
                        "Dues cannot be recorded for 'Walk-in Customer'. Would you like to register a new customer to record these dues?", 
                        "Walk-in Customer Dues Warning", 
                        MessageBoxButtons.YesNo, 
                        MessageBoxIcon.Warning);

                    if (res == DialogResult.Yes)
                    {
                        using (CustomerControl.CustomerDialog dlg = new CustomerControl.CustomerDialog())
                        {
                            if (dlg.ShowDialog() == DialogResult.OK && dlg.NewCustomerId > 0)
                            {
                                LoadDropdownData();
                                comboCustomer.SelectedValue = dlg.NewCustomerId;
                                customerId = dlg.NewCustomerId;
                            }
                            else
                            {
                                return;
                            }
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }

            string paymentMode = comboPaymentMethod.SelectedItem?.ToString() ?? "Cash";
            string invoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{DateTime.Now:HHmmss}";

            using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // 1. Insert Sales Header
                    int saleId = 0;
                    string headerSql = @"
                        INSERT INTO Sales (InvoiceNumber, CustomerId, SaleDate, SubTotal, Discount, Tax, GrandTotal, PaymentMethod, CreatedBy, IsTaxInclusive, TaxType, PaidAmount, DueAmount) 
                        OUTPUT INSERTED.Id
                        VALUES (@invNum, @custId, GETDATE(), @sub, @disc, @tax, @grand, @pay, @user, @isIncl, @taxType, @paidAmount, @dueAmount)";

                    using (SqlCommand cmd = new SqlCommand(headerSql, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@invNum", invoiceNumber);
                        cmd.Parameters.AddWithValue("@custId", customerId);
                        cmd.Parameters.AddWithValue("@sub", subTotal);
                        cmd.Parameters.AddWithValue("@disc", discount);
                        cmd.Parameters.AddWithValue("@tax", tax);
                        cmd.Parameters.AddWithValue("@grand", grandTotal);
                        cmd.Parameters.AddWithValue("@pay", paymentMode);
                        cmd.Parameters.AddWithValue("@user", Session.UserId);
                        cmd.Parameters.AddWithValue("@isIncl", chkTaxInclusive.Checked ? 1 : 0);
                        cmd.Parameters.AddWithValue("@taxType", comboTaxType.SelectedItem?.ToString() ?? "Local");
                        cmd.Parameters.AddWithValue("@paidAmount", amountPaid);
                        cmd.Parameters.AddWithValue("@dueAmount", dueAmount);
                        saleId = (int)cmd.ExecuteScalar();
                    }

                    // 1b. Log initial payment in CustomerPayments
                    if (amountPaid > 0)
                    {
                        string paymentSql = @"
                            INSERT INTO CustomerPayments (CustomerId, SaleId, PaymentDate, AmountPaid, PaymentMethod, Remarks, CreatedBy)
                            VALUES (@custId, @saleId, GETDATE(), @amountPaid, @payMethod, @remarks, @user)";
                        using (SqlCommand cmd = new SqlCommand(paymentSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@custId", customerId);
                            cmd.Parameters.AddWithValue("@saleId", saleId);
                            cmd.Parameters.AddWithValue("@amountPaid", amountPaid);
                            cmd.Parameters.AddWithValue("@payMethod", paymentMode);
                            cmd.Parameters.AddWithValue("@remarks", "Initial Checkout Payment");
                            cmd.Parameters.AddWithValue("@user", Session.UserId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // 2. Insert Sale Details and Decrement Stock
                    foreach (DataRow row in cartTable.Rows)
                    {
                        int prodId = (int)row["ProductId"];
                        decimal qty = (decimal)row["Qty"];
                        decimal unitPrice = (decimal)row["Price"];
                        decimal total = (decimal)row["Total"];

                        // Details Insert (archiving the exact current purchase cost to protect reports)
                        string detailsSql = @"
                            INSERT INTO SaleDetails (SaleId, ProductId, Quantity, UnitPrice, Total, PurchaseCostAtSale, TaxPercent, TaxAmount) 
                            SELECT @saleId, @prodId, @qty, @price, @total, PurchasePrice, @taxPercent, @taxAmount 
                            FROM Products 
                            WHERE Id = @prodId";

                        using (SqlCommand cmd = new SqlCommand(detailsSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@saleId", saleId);
                            cmd.Parameters.AddWithValue("@prodId", prodId);
                            cmd.Parameters.AddWithValue("@qty", qty);
                            cmd.Parameters.AddWithValue("@price", unitPrice);
                            cmd.Parameters.AddWithValue("@total", total);
                            cmd.Parameters.AddWithValue("@taxPercent", chkExcludeTax.Checked ? 0m : Convert.ToDecimal(row["TaxPercent"]));
                            cmd.Parameters.AddWithValue("@taxAmount", Convert.ToDecimal(row["TaxAmount"]));
                            cmd.ExecuteNonQuery();
                        }

                        // Decrement Product Stock Level
                        string stockSql = @"
                            UPDATE Products 
                            SET Stock = Stock - @qty 
                            WHERE Id = @id";

                        using (SqlCommand cmd = new SqlCommand(stockSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@qty", qty);
                            cmd.Parameters.AddWithValue("@id", prodId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    
                    lastSaleId = saleId;
                    MessageBox.Show($"Sale transaction completed successfully!\nInvoice No: {invoiceNumber}", "Checkout Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Launch Print Preview Dialog if Bill Not Required is unchecked!
                    if (!chkBillNotRequired.Checked)
                    {
                        previewDlg.ShowDialog();
                    }

                    // Clear Screen
                    cartTable.Clear();
                    txtDiscount.Text = "0.00";
                    txtTax.Text = "0";
                    if (txtAmountPaid != null) txtAmountPaid.Text = "0.00";
                    if (chkExcludeTax != null) chkExcludeTax.Checked = true;
                    CalculateCheckoutTotals();
                    
                    txtProductCode.Clear();
                    txtPrice.Text = "0.00";
                    txtQty.Text = "1";
                    currentProductId = 0;
                    currentProductCode = "";
                    currentProductName = "";
                    lblAvailableStock.Text = "Available Stock: --";
                    lblAvailableStock.ForeColor = Theme.Warning;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Failed to checkout: {ex.Message}", "Transaction Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Print Invoice Subsystem Drawing (GDI+ style)
        private void InvoiceDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int startX = 50;
            int startY = 50;

            Font fTitle = new Font("Segoe UI", 18F, FontStyle.Bold);
            Font fSubTitle = new Font("Segoe UI", 9F, FontStyle.Italic);
            Font fRegular = new Font("Segoe UI", 10F, FontStyle.Regular);
            Font fBold = new Font("Segoe UI", 10F, FontStyle.Bold);
            
            Brush bDark = new SolidBrush(Color.Black);
            Pen pLine = new Pen(Color.Gray, 1);

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
                        cmd.Parameters.AddWithValue("@id", lastSaleId);
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
                g.DrawString($"Database Error Rendering Print: {ex.Message}", fRegular, bDark, startX, startY);
                return;
            }

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
                        cmd.Parameters.AddWithValue("@id", lastSaleId);
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
                        cmd.Parameters.AddWithValue("@saleId", lastSaleId);
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

        private void BtnSearchProduct_Click(object sender, EventArgs e)
        {
            using (ProductSearchDialog dlg = new ProductSearchDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(dlg.SelectedProductCode))
                {
                    txtProductCode.Text = dlg.SelectedProductCode;
                    LoadProductByCode(dlg.SelectedProductCode);
                }
            }
        }

        // Nested Search Form to find products by Name, Code, Category at checkout
        private class ProductSearchDialog : Form
        {
            public string SelectedProductCode { get; private set; }
            private TextBox txtSearch;
            private DataGridView gridProducts;
            private Button btnSelect;
            private Button btnCancel;

            public ProductSearchDialog()
            {
                InitializeComponent();
                LoadProducts();
                this.ActiveControl = txtSearch;
            }

            protected override void OnLoad(EventArgs e)
            {
                base.OnLoad(e);
                txtSearch.Focus();
            }

            private void InitializeComponent()
            {
                this.Text = "Search Products";
                this.ClientSize = new Size(600, 450);
                this.Font = Theme.MainFont;
                this.AutoScaleDimensions = new SizeF(7F, 15F);
                this.AutoScaleMode = AutoScaleMode.Font;
                this.AutoScroll = true;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.StartPosition = FormStartPosition.CenterParent;
                this.BackColor = Theme.Primary;

                txtSearch = new TextBox();
                txtSearch.Location = new Point(20, 20);
                txtSearch.Size = new Size(545, 30);
                txtSearch.PlaceholderText = "Search by Name, Code, or Category...";
                Theme.StyleTextBox(txtSearch);
                txtSearch.TextChanged += TxtSearch_TextChanged;
                this.Controls.Add(txtSearch);

                gridProducts = new DataGridView();
                gridProducts.Location = new Point(20, 70);
                gridProducts.Size = new Size(545, 270);
                Theme.StyleGrid(gridProducts);
                gridProducts.CellDoubleClick += GridProducts_CellDoubleClick;
                this.Controls.Add(gridProducts);

                btnSelect = new Button();
                btnSelect.Text = "Select Product";
                btnSelect.Size = new Size(150, 40);
                btnSelect.Location = new Point(20, 360);
                Theme.StyleSuccessButton(btnSelect);
                btnSelect.Click += BtnSelect_Click;
                this.Controls.Add(btnSelect);

                btnCancel = new Button();
                btnCancel.Text = "Cancel";
                btnCancel.Size = new Size(150, 40);
                btnCancel.Location = new Point(190, 360);
                Theme.StyleSecondaryButton(btnCancel);
                btnCancel.Click += (s, e) => this.Close();
                this.Controls.Add(btnCancel);

                this.AcceptButton = btnSelect;
            }

            private void LoadProducts()
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        string query = @"
                            SELECT Code, Name, Category, SalesPrice as [Price], Stock as [Available Stock]
                            FROM Products
                            WHERE Name LIKE @search OR Code LIKE @search OR Category LIKE @search
                            ORDER BY Name ASC";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@search", $"%{txtSearch.Text.Trim()}%");
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                DataTable dt = new DataTable();
                                da.Fill(dt);
                                gridProducts.DataSource = dt;
                            }
                        }
                    }

                    if (gridProducts.Columns["Code"] != null) gridProducts.Columns["Code"].FillWeight = 80;
                    if (gridProducts.Columns["Name"] != null) gridProducts.Columns["Name"].FillWeight = 180;
                    if (gridProducts.Columns["Category"] != null) gridProducts.Columns["Category"].FillWeight = 90;
                    if (gridProducts.Columns["Price"] != null)
                    {
                        gridProducts.Columns["Price"].FillWeight = 70;
                        gridProducts.Columns["Price"].DefaultCellStyle.Format = "N2";
                    }
                    if (gridProducts.Columns["Available Stock"] != null) gridProducts.Columns["Available Stock"].FillWeight = 70;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error searching products: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            private void TxtSearch_TextChanged(object sender, EventArgs e)
            {
                LoadProducts();
            }

            private void GridProducts_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex >= 0)
                {
                    SelectAndClose();
                }
            }

            private void BtnSelect_Click(object sender, EventArgs e)
            {
                SelectAndClose();
            }

            private void SelectAndClose()
            {
                if (gridProducts.SelectedRows.Count > 0)
                {
                    SelectedProductCode = gridProducts.SelectedRows[0].Cells["Code"].Value.ToString();
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Please select a product from the list.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}

