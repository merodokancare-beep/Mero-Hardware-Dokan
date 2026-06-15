using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace MeroHardwareDokan
{
    public class CustomerControl : UserControl
    {
        private TextBox txtSearch;
        private DataGridView gridCustomers;
        private Button btnAdd;
        private Button btnEdit;
        private Button btnDelete;
        private Button btnDuesHistory;

        public CustomerControl()
        {
            InitializeComponent();
            LoadCustomers();
            this.ActiveControl = txtSearch;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            txtSearch.Focus();
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
            lblHeader.Text = "Customer CRM Directory";
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
            this.Controls.Add(lblHeader);

            // Search Panel container
            Panel searchPanel = new Panel();
            searchPanel.Size = new Size(300, 36);
            searchPanel.Location = new Point(20, 65);
            searchPanel.BackColor = Theme.Primary;
            searchPanel.Padding = new Padding(8, 8, 8, 8);

            txtSearch = new TextBox();
            txtSearch.BorderStyle = BorderStyle.None;
            txtSearch.BackColor = Theme.Primary;
            txtSearch.ForeColor = Theme.TextLight;
            txtSearch.Font = Theme.MainFont;
            txtSearch.Dock = DockStyle.Fill;
            txtSearch.PlaceholderText = "Search by Name or Phone...";
            txtSearch.TextChanged += TxtSearch_TextChanged;
            searchPanel.Controls.Add(txtSearch);
            this.Controls.Add(searchPanel);

            // GridView
            gridCustomers = new DataGridView();
            gridCustomers.Size = new Size(910, 460);
            gridCustomers.Location = new Point(20, 115);
            Theme.StyleGrid(gridCustomers);
            this.Controls.Add(gridCustomers);

            // Action Buttons Panel
            Panel actionPanel = new Panel();
            actionPanel.Size = new Size(910, 50);
            actionPanel.Location = new Point(20, 585);
            
            btnAdd = new Button();
            btnAdd.Text = "+ Add Customer";
            btnAdd.Size = new Size(160, 40);
            btnAdd.Location = new Point(0, 0);
            Theme.StyleSuccessButton(btnAdd);
            btnAdd.Click += BtnAdd_Click;
            actionPanel.Controls.Add(btnAdd);

            btnEdit = new Button();
            btnEdit.Text = "📝 Edit Selected";
            btnEdit.Size = new Size(160, 40);
            btnEdit.Location = new Point(180, 0);
            Theme.StylePrimaryButton(btnEdit);
            btnEdit.Click += BtnEdit_Click;
            actionPanel.Controls.Add(btnEdit);

            btnDelete = new Button();
            btnDelete.Text = "🗑️ Delete Selected";
            btnDelete.Size = new Size(160, 40);
            btnDelete.Location = new Point(360, 0);
            Theme.StyleDangerButton(btnDelete);
            btnDelete.Click += BtnDelete_Click;
            actionPanel.Controls.Add(btnDelete);

            btnDuesHistory = new Button();
            btnDuesHistory.Text = "💰 Manage Dues";
            btnDuesHistory.Size = new Size(160, 40);
            btnDuesHistory.Location = new Point(540, 0);
            Theme.StylePrimaryButton(btnDuesHistory);
            btnDuesHistory.Click += BtnDuesHistory_Click;
            actionPanel.Controls.Add(btnDuesHistory);

            this.Controls.Add(actionPanel);
        }

        private void LoadCustomers()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT c.Id, c.Name, c.Phone, c.Email, c.Address,
                               ISNULL((SELECT SUM(DueAmount) FROM Sales WHERE CustomerId = c.Id), 0) AS [Outstanding Dues],
                               c.CreatedAt as [Registered Date] 
                        FROM Customers c
                        WHERE c.Name LIKE @search OR c.Phone LIKE @search OR c.Address LIKE @search
                        ORDER BY c.Name ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        string searchVal = $"%{txtSearch.Text.Trim()}%";
                        cmd.Parameters.AddWithValue("@search", searchVal);

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridCustomers.DataSource = dt;

                            if (gridCustomers.Columns["Outstanding Dues"] != null)
                            {
                                gridCustomers.Columns["Outstanding Dues"].DefaultCellStyle.Format = "N2";
                            }
                        }
                    }
                }

                // Hide Id column beautifully
                if (gridCustomers.Columns["Id"] != null)
                {
                    gridCustomers.Columns["Id"].Visible = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading customers: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadCustomers();
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (CustomerDialog dlg = new CustomerDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadCustomers();
                }
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (gridCustomers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a customer to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow selectedRow = gridCustomers.SelectedRows[0];
            int customerId = Convert.ToInt32(selectedRow.Cells["Id"].Value);
            string name = selectedRow.Cells["Name"].Value.ToString();
            string phone = selectedRow.Cells["Phone"].Value?.ToString() ?? "";
            string email = selectedRow.Cells["Email"].Value?.ToString() ?? "";
            string address = selectedRow.Cells["Address"].Value?.ToString() ?? "";

            using (CustomerDialog dlg = new CustomerDialog(customerId, name, phone, email, address))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadCustomers();
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (gridCustomers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a customer to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow selectedRow = gridCustomers.SelectedRows[0];
            int customerId = Convert.ToInt32(selectedRow.Cells["Id"].Value);
            string customerName = selectedRow.Cells["Name"].Value.ToString();

            if (customerName == "Walk-in Customer")
            {
                MessageBox.Show("Cannot delete the system-seeded default 'Walk-in Customer'.", "Action Restrained", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult confirm = MessageBox.Show($"Are you sure you want to permanently delete customer '{customerName}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm == DialogResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("DELETE FROM Customers WHERE Id = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", customerId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    LoadCustomers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting customer: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnDuesHistory_Click(object sender, EventArgs e)
        {
            if (gridCustomers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a customer to manage dues.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow selectedRow = gridCustomers.SelectedRows[0];
            int customerId = Convert.ToInt32(selectedRow.Cells["Id"].Value);
            string customerName = selectedRow.Cells["Name"].Value.ToString();

            using (ManageCustomerDuesDialog dlg = new ManageCustomerDuesDialog(customerId, customerName))
            {
                dlg.ShowDialog();
                LoadCustomers(); // Reload dues when closed
            }
        }

        // Dialog to manage invoices with outstanding balances and show repayments
        private class ManageCustomerDuesDialog : Form
        {
            private int customerId;
            private string customerName;
            private Label lblCustomerInfo;
            private Label lblTotalDues;
            private DataGridView gridInvoices;
            private DataGridView gridPayments;
            private Button btnRecordPayment;
            private Button btnClose;

            public ManageCustomerDuesDialog(int customerId, string customerName)
            {
                this.customerId = customerId;
                this.customerName = customerName;
                InitializeComponent();
                LoadInvoiceDues();
                LoadPaymentHistory();
            }

            private void InitializeComponent()
            {
                this.Text = "Manage Customer Dues - " + customerName;
                this.ClientSize = new Size(850, 600);
                this.Font = Theme.MainFont;
                this.AutoScaleDimensions = new SizeF(7F, 15F);
                this.AutoScaleMode = AutoScaleMode.Font;
                this.AutoScroll = true;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.StartPosition = FormStartPosition.CenterParent;
                this.BackColor = Theme.Primary;

                // Title
                lblCustomerInfo = new Label();
                lblCustomerInfo.Text = $"Customer: {customerName}";
                lblCustomerInfo.Location = new Point(20, 15);
                lblCustomerInfo.AutoSize = true;
                Theme.StyleLabel(lblCustomerInfo, Theme.TextLight, Theme.HeaderFont);
                this.Controls.Add(lblCustomerInfo);

                lblTotalDues = new Label();
                lblTotalDues.Text = "Total Dues: Rs. 0.00";
                lblTotalDues.Location = new Point(550, 15);
                lblTotalDues.AutoSize = true;
                Theme.StyleLabel(lblTotalDues, Theme.Warning, Theme.HeaderFont);
                this.Controls.Add(lblTotalDues);

                // Grid 1 Title
                Label lblGrid1 = new Label();
                lblGrid1.Text = "Outstanding Invoices";
                lblGrid1.Location = new Point(20, 60);
                lblGrid1.AutoSize = true;
                Theme.StyleLabel(lblGrid1, Theme.TextLight, Theme.SubHeaderFont);
                this.Controls.Add(lblGrid1);

                // Grid 1: Outstanding Invoices
                gridInvoices = new DataGridView();
                gridInvoices.Location = new Point(20, 85);
                gridInvoices.Size = new Size(800, 180);
                Theme.StyleGrid(gridInvoices);
                this.Controls.Add(gridInvoices);

                // Grid 2 Title
                Label lblGrid2 = new Label();
                lblGrid2.Text = "Payment History Log";
                lblGrid2.Location = new Point(20, 280);
                lblGrid2.AutoSize = true;
                Theme.StyleLabel(lblGrid2, Theme.TextLight, Theme.SubHeaderFont);
                this.Controls.Add(lblGrid2);

                // Grid 2: Payments History
                gridPayments = new DataGridView();
                gridPayments.Location = new Point(20, 305);
                gridPayments.Size = new Size(800, 180);
                Theme.StyleGrid(gridPayments);
                this.Controls.Add(gridPayments);

                // Buttons
                btnRecordPayment = new Button();
                btnRecordPayment.Text = "Record Repayment";
                btnRecordPayment.Size = new Size(180, 40);
                btnRecordPayment.Location = new Point(20, 500);
                Theme.StyleSuccessButton(btnRecordPayment);
                btnRecordPayment.Click += BtnRecordPayment_Click;
                this.Controls.Add(btnRecordPayment);

                btnClose = new Button();
                btnClose.Text = "Close";
                btnClose.Size = new Size(150, 40);
                btnClose.Location = new Point(220, 500);
                Theme.StyleSecondaryButton(btnClose);
                btnClose.Click += (s, e) => this.Close();
                this.Controls.Add(btnClose);
            }

            private void LoadInvoiceDues()
            {
                decimal totalDues = 0;
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        string query = @"
                            SELECT Id, InvoiceNumber as [Invoice No], SaleDate as [Sale Date],
                                   GrandTotal as [Grand Total], PaidAmount as [Paid Amount], DueAmount as [Due Amount]
                            FROM Sales
                            WHERE CustomerId = @custId AND DueAmount > 0
                            ORDER BY SaleDate ASC";
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@custId", customerId);
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                DataTable dt = new DataTable();
                                da.Fill(dt);
                                gridInvoices.DataSource = dt;

                                // Format columns
                                if (gridInvoices.Columns["Grand Total"] != null) gridInvoices.Columns["Grand Total"].DefaultCellStyle.Format = "N2";
                                if (gridInvoices.Columns["Paid Amount"] != null) gridInvoices.Columns["Paid Amount"].DefaultCellStyle.Format = "N2";
                                if (gridInvoices.Columns["Due Amount"] != null) gridInvoices.Columns["Due Amount"].DefaultCellStyle.Format = "N2";
                                if (gridInvoices.Columns["Id"] != null) gridInvoices.Columns["Id"].Visible = false;

                                foreach (DataRow row in dt.Rows)
                                {
                                    totalDues += Convert.ToDecimal(row["Due Amount"]);
                                }
                            }
                        }
                    }
                    lblTotalDues.Text = $"Total Dues: Rs. {totalDues:N2}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading dues invoices: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            private void LoadPaymentHistory()
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        string query = @"
                            SELECT cp.PaymentDate as [Payment Date], s.InvoiceNumber as [Invoice No],
                                   cp.AmountPaid as [Amount Paid], cp.PaymentMethod as [Mode], cp.Remarks
                            FROM CustomerPayments cp
                            INNER JOIN Sales s ON cp.SaleId = s.Id
                            WHERE cp.CustomerId = @custId
                            ORDER BY cp.PaymentDate DESC";
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@custId", customerId);
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                DataTable dt = new DataTable();
                                da.Fill(dt);
                                gridPayments.DataSource = dt;

                                if (gridPayments.Columns["Amount Paid"] != null) gridPayments.Columns["Amount Paid"].DefaultCellStyle.Format = "N2";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading payment history: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            private void BtnRecordPayment_Click(object sender, EventArgs e)
            {
                if (gridInvoices.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select an outstanding invoice from the list to pay.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                DataGridViewRow row = gridInvoices.SelectedRows[0];
                int saleId = Convert.ToInt32(row.Cells["Id"].Value);
                string invoiceNo = row.Cells["Invoice No"].Value.ToString();
                decimal currentDue = Convert.ToDecimal(row.Cells["Due Amount"].Value);

                using (RecordRepaymentDialog dlg = new RecordRepaymentDialog(customerId, saleId, invoiceNo, currentDue))
                {
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        LoadInvoiceDues();
                        LoadPaymentHistory();
                    }
                }
            }
        }

        // Dialog to capture repayment details
        private class RecordRepaymentDialog : Form
        {
            private int customerId;
            private int saleId;
            private string invoiceNo;
            private decimal currentDue;

            private TextBox txtAmount;
            private ComboBox comboMethod;
            private TextBox txtRemarks;
            private Button btnSave;
            private Button btnCancel;

            public RecordRepaymentDialog(int customerId, int saleId, string invoiceNo, decimal currentDue)
            {
                this.customerId = customerId;
                this.saleId = saleId;
                this.invoiceNo = invoiceNo;
                this.currentDue = currentDue;
                InitializeComponent();
                this.ActiveControl = txtAmount;
            }

            protected override void OnLoad(EventArgs e)
            {
                base.OnLoad(e);
                txtAmount.Focus();
            }

            private void InitializeComponent()
            {
                this.Text = "Record Repayment - " + invoiceNo;
                this.ClientSize = new Size(380, 360);
                this.Font = Theme.MainFont;
                this.AutoScaleDimensions = new SizeF(7F, 15F);
                this.AutoScaleMode = AutoScaleMode.Font;
                this.AutoScroll = true;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.StartPosition = FormStartPosition.CenterParent;
                this.BackColor = Theme.Primary;

                Label lblHeader = new Label();
                lblHeader.Text = $"Repayment for {invoiceNo}";
                lblHeader.Location = new Point(20, 15);
                lblHeader.AutoSize = true;
                Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
                this.Controls.Add(lblHeader);

                Label lblDue = new Label();
                lblDue.Text = $"Remaining Balance Due: Rs. {currentDue:N2}";
                lblDue.Location = new Point(20, 50);
                lblDue.AutoSize = true;
                Theme.StyleLabel(lblDue, Theme.Warning, Theme.BoldFont);
                this.Controls.Add(lblDue);

                // Amount
                Label lblAmount = new Label();
                lblAmount.Text = "Repayment Amount (Rs.) *";
                lblAmount.Location = new Point(20, 85);
                lblAmount.AutoSize = true;
                Theme.StyleLabel(lblAmount, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblAmount);

                txtAmount = new TextBox();
                txtAmount.Size = new Size(320, 30);
                txtAmount.Location = new Point(20, 110);
                Theme.StyleTextBox(txtAmount);
                txtAmount.Text = currentDue.ToString("0.00"); // Pre-fill with current due
                this.Controls.Add(txtAmount);

                // Mode
                Label lblMode = new Label();
                lblMode.Text = "Payment Mode";
                lblMode.Location = new Point(20, 150);
                lblMode.AutoSize = true;
                Theme.StyleLabel(lblMode, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblMode);

                comboMethod = new ComboBox();
                comboMethod.Size = new Size(320, 28);
                comboMethod.Location = new Point(20, 175);
                comboMethod.DropDownStyle = ComboBoxStyle.DropDownList;
                comboMethod.Items.AddRange(new string[] { "Cash", "Card", "QR Pay" });
                comboMethod.SelectedIndex = 0;
                comboMethod.BackColor = Theme.Primary;
                comboMethod.ForeColor = Theme.TextLight;
                comboMethod.Font = Theme.MainFont;
                this.Controls.Add(comboMethod);

                // Remarks
                Label lblRemarks = new Label();
                lblRemarks.Text = "Remarks";
                lblRemarks.Location = new Point(20, 215);
                lblRemarks.AutoSize = true;
                Theme.StyleLabel(lblRemarks, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblRemarks);

                txtRemarks = new TextBox();
                txtRemarks.Size = new Size(320, 30);
                txtRemarks.Location = new Point(20, 240);
                Theme.StyleTextBox(txtRemarks);
                txtRemarks.PlaceholderText = "e.g. Received by cash, Bank transfer";
                this.Controls.Add(txtRemarks);

                // Save/Cancel Buttons
                btnSave = new Button();
                btnSave.Text = "Save Repayment";
                btnSave.Size = new Size(150, 40);
                btnSave.Location = new Point(20, 290);
                Theme.StyleSuccessButton(btnSave);
                btnSave.Click += BtnSave_Click;
                this.Controls.Add(btnSave);

                btnCancel = new Button();
                btnCancel.Text = "Cancel";
                btnCancel.Size = new Size(150, 40);
                btnCancel.Location = new Point(190, 290);
                Theme.StyleSecondaryButton(btnCancel);
                btnCancel.Click += (s, e) => this.Close();
                this.Controls.Add(btnCancel);

                this.AcceptButton = btnSave;
            }

            private void BtnSave_Click(object sender, EventArgs e)
            {
                if (!decimal.TryParse(txtAmount.Text.Trim(), out decimal amount) || amount <= 0)
                {
                    MessageBox.Show("Please enter a valid repayment amount greater than 0.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (amount > currentDue)
                {
                    MessageBox.Show($"Repayment amount cannot exceed the active balance due (Rs. {currentDue:N2}).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string method = comboMethod.SelectedItem?.ToString() ?? "Cash";
                string remarks = txtRemarks.Text.Trim();

                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    SqlTransaction transaction = conn.BeginTransaction();

                    try
                    {
                        // 1. Insert CustomerPayments record
                        string paymentSql = @"
                            INSERT INTO CustomerPayments (CustomerId, SaleId, PaymentDate, AmountPaid, PaymentMethod, Remarks, CreatedBy)
                            VALUES (@custId, @saleId, GETDATE(), @amountPaid, @method, @remarks, @user)";
                        using (SqlCommand cmd = new SqlCommand(paymentSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@custId", customerId);
                            cmd.Parameters.AddWithValue("@saleId", saleId);
                            cmd.Parameters.AddWithValue("@amountPaid", amount);
                            cmd.Parameters.AddWithValue("@method", method);
                            cmd.Parameters.AddWithValue("@remarks", string.IsNullOrEmpty(remarks) ? "Repayment" : remarks);
                            cmd.Parameters.AddWithValue("@user", Session.UserId);
                            cmd.ExecuteNonQuery();
                        }

                        // 2. Update Sales table PaidAmount and DueAmount
                        string salesSql = @"
                            UPDATE Sales 
                            SET PaidAmount = PaidAmount + @amount,
                                DueAmount = DueAmount - @amount
                            WHERE Id = @saleId";
                        using (SqlCommand cmd = new SqlCommand(salesSql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@amount", amount);
                            cmd.Parameters.AddWithValue("@saleId", saleId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        MessageBox.Show("Repayment recorded successfully!", "Repayment Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Failed to record repayment: {ex.Message}", "Transaction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // Nested Customer Dialog for modal add/edit
        public class CustomerDialog : Form
        {
            public int NewCustomerId { get; private set; } = -1;
            private int? customerId = null;
            private TextBox txtName;
            private TextBox txtPhone;
            private TextBox txtEmail;
            private TextBox txtAddress;
            private Button btnSave;
            private Button btnCancel;

            public CustomerDialog()
            {
                InitializeComponent("Add Customer");
                this.ActiveControl = txtName;
            }

            public CustomerDialog(int id, string name, string phone, string email, string address)
            {
                this.customerId = id;
                InitializeComponent("Edit Customer");
                txtName.Text = name;
                txtPhone.Text = phone;
                txtEmail.Text = email;
                txtAddress.Text = address;
                this.ActiveControl = txtName;
            }

            protected override void OnLoad(EventArgs e)
            {
                base.OnLoad(e);
                txtName.Focus();
            }

            private void InitializeComponent(string title)
            {
                this.Text = title;
                this.ClientSize = new Size(400, 480); // Sets inner client area precisely
                this.Font = Theme.MainFont;
                this.AutoScaleDimensions = new SizeF(7F, 15F);
                this.AutoScaleMode = AutoScaleMode.Font;
                this.AutoScroll = true;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.StartPosition = FormStartPosition.CenterParent;
                this.BackColor = Theme.Primary;

                // Form header
                Label lblHeader = new Label();
                lblHeader.Text = title;
                lblHeader.Location = new Point(20, 20);
                lblHeader.AutoSize = true;
                Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
                this.Controls.Add(lblHeader);

                // Fields placement
                int startY = 80;
                int gapY = 70;

                // Name
                Label lblName = new Label();
                lblName.Text = "Full Name *";
                lblName.Location = new Point(20, startY);
                lblName.AutoSize = true;
                Theme.StyleLabel(lblName, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblName);

                txtName = new TextBox();
                txtName.Size = new Size(340, 30);
                txtName.Location = new Point(20, startY + 25);
                Theme.StyleTextBox(txtName);
                this.Controls.Add(txtName);

                // Phone
                Label lblPhone = new Label();
                lblPhone.Text = "Phone Number";
                lblPhone.Location = new Point(20, startY + gapY);
                lblPhone.AutoSize = true;
                Theme.StyleLabel(lblPhone, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblPhone);

                txtPhone = new TextBox();
                txtPhone.Size = new Size(340, 30);
                txtPhone.Location = new Point(20, startY + gapY + 25);
                Theme.StyleTextBox(txtPhone);
                this.Controls.Add(txtPhone);

                // Email
                Label lblEmail = new Label();
                lblEmail.Text = "Email Address";
                lblEmail.Location = new Point(20, startY + (gapY * 2));
                lblEmail.AutoSize = true;
                Theme.StyleLabel(lblEmail, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblEmail);

                txtEmail = new TextBox();
                txtEmail.Size = new Size(340, 30);
                txtEmail.Location = new Point(20, startY + (gapY * 2) + 25);
                Theme.StyleTextBox(txtEmail);
                this.Controls.Add(txtEmail);

                // Address
                Label lblAddress = new Label();
                lblAddress.Text = "Address";
                lblAddress.Location = new Point(20, startY + (gapY * 3));
                lblAddress.AutoSize = true;
                Theme.StyleLabel(lblAddress, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblAddress);

                txtAddress = new TextBox();
                txtAddress.Size = new Size(340, 30);
                txtAddress.Location = new Point(20, startY + (gapY * 3) + 25);
                Theme.StyleTextBox(txtAddress);
                this.Controls.Add(txtAddress);

                // Action buttons
                btnSave = new Button();
                btnSave.Text = "Save Details";
                btnSave.Size = new Size(160, 40);
                btnSave.Location = new Point(20, 380);
                Theme.StyleSuccessButton(btnSave);
                btnSave.Click += BtnSave_Click;
                this.Controls.Add(btnSave);

                btnCancel = new Button();
                btnCancel.Text = "Cancel";
                btnCancel.Size = new Size(160, 40);
                btnCancel.Location = new Point(200, 380);
                Theme.StyleSecondaryButton(btnCancel);
                btnCancel.Click += (s, e) => this.Close();
                this.Controls.Add(btnCancel);

                this.AcceptButton = btnSave;
            }

            private void BtnSave_Click(object sender, EventArgs e)
            {
                string name = txtName.Text.Trim();
                string phone = txtPhone.Text.Trim();
                string email = txtEmail.Text.Trim();
                string address = txtAddress.Text.Trim();

                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Customer Name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(phone))
                {
                    try
                    {
                        using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                        {
                            conn.Open();
                            string checkSql = customerId == null 
                                ? "SELECT COUNT(*) FROM Customers WHERE Phone = @phone"
                                : "SELECT COUNT(*) FROM Customers WHERE Phone = @phone AND Id != @id";

                            using (SqlCommand checkCmd = new SqlCommand(checkSql, conn))
                            {
                                checkCmd.Parameters.AddWithValue("@phone", phone);
                                if (customerId != null)
                                {
                                    checkCmd.Parameters.AddWithValue("@id", customerId.Value);
                                }

                                int exists = (int)checkCmd.ExecuteScalar();
                                if (exists > 0)
                                {
                                    MessageBox.Show("A customer with this mobile number already exists.", "Duplicate Customer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error validating customer details: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        if (customerId == null)
                        {
                            // INSERT
                            using (SqlCommand cmd = new SqlCommand(@"
                                INSERT INTO Customers (Name, Phone, Email, Address) 
                                OUTPUT INSERTED.Id
                                VALUES (@name, @phone, @email, @address)", conn))
                            {
                                cmd.Parameters.AddWithValue("@name", name);
                                cmd.Parameters.AddWithValue("@phone", phone);
                                cmd.Parameters.AddWithValue("@email", email);
                                cmd.Parameters.AddWithValue("@address", address);
                                NewCustomerId = (int)cmd.ExecuteScalar();
                            }
                        }
                        else
                        {
                            // UPDATE
                            using (SqlCommand cmd = new SqlCommand(@"
                                UPDATE Customers 
                                SET Name = @name, Phone = @phone, Email = @email, Address = @address 
                                WHERE Id = @id", conn))
                            {
                                cmd.Parameters.AddWithValue("@id", customerId.Value);
                                cmd.Parameters.AddWithValue("@name", name);
                                cmd.Parameters.AddWithValue("@phone", phone);
                                cmd.Parameters.AddWithValue("@email", email);
                                cmd.Parameters.AddWithValue("@address", address);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving customer details: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}

