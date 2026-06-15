using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace MeroHardwareDokan
{
    public class ProductControl : UserControl
    {
        private TextBox txtSearch;
        private DataGridView gridProducts;
        private Button btnAdd;
        private Button btnEdit;
        private Button btnDelete;
        private int currentCatalogRowIndex = 0;
        private int catalogPageNumber = 1;

        public ProductControl()
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
            this.Font = Theme.MainFont;
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.AutoScroll = true;
            this.Size = new Size(950, 650);
            this.BackColor = Theme.Secondary;

            // Page Header
            Label lblHeader = new Label();
            lblHeader.Text = "Product Master Directory";
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
            searchPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            txtSearch = new TextBox();
            txtSearch.BorderStyle = BorderStyle.None;
            txtSearch.BackColor = Theme.Primary;
            txtSearch.ForeColor = Theme.TextLight;
            txtSearch.Font = Theme.MainFont;
            txtSearch.Dock = DockStyle.Fill;
            txtSearch.PlaceholderText = "Search by Code, Name, or Category...";
            txtSearch.TextChanged += TxtSearch_TextChanged;
            searchPanel.Controls.Add(txtSearch);
            this.Controls.Add(searchPanel);

            // GridView
            gridProducts = new DataGridView();
            gridProducts.Size = new Size(910, 460);
            gridProducts.Location = new Point(20, 115);
            gridProducts.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridProducts);
            this.Controls.Add(gridProducts);

            // Action Buttons Panel
            Panel actionPanel = new Panel();
            actionPanel.Size = new Size(910, 50);
            actionPanel.Location = new Point(20, 585);
            actionPanel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            
            btnAdd = new Button();
            btnAdd.Text = "+ Add Product";
            btnAdd.Size = new Size(135, 40);
            btnAdd.Location = new Point(0, 0);
            Theme.StyleSuccessButton(btnAdd);
            btnAdd.Click += BtnAdd_Click;
            actionPanel.Controls.Add(btnAdd);

            btnEdit = new Button();
            btnEdit.Text = "📝 Edit Selected";
            btnEdit.Size = new Size(135, 40);
            btnEdit.Location = new Point(145, 0);
            Theme.StylePrimaryButton(btnEdit);
            btnEdit.Click += BtnEdit_Click;
            actionPanel.Controls.Add(btnEdit);

            btnDelete = new Button();
            btnDelete.Text = "🗑️ Delete Selected";
            btnDelete.Size = new Size(135, 40);
            btnDelete.Location = new Point(290, 0);
            Theme.StyleDangerButton(btnDelete);
            btnDelete.Click += BtnDelete_Click;
            actionPanel.Controls.Add(btnDelete);

            Button btnManageCategories = new Button();
            btnManageCategories.Text = "📂 Categories";
            btnManageCategories.Size = new Size(150, 40);
            btnManageCategories.Location = new Point(435, 0);
            Theme.StylePrimaryButton(btnManageCategories);
            btnManageCategories.Click += BtnManageCategories_Click;
            actionPanel.Controls.Add(btnManageCategories);

            Button btnManageTaxSlabs = new Button();
            btnManageTaxSlabs.Text = "📑 Tax Slabs";
            btnManageTaxSlabs.Size = new Size(150, 40);
            btnManageTaxSlabs.Location = new Point(595, 0);
            Theme.StylePrimaryButton(btnManageTaxSlabs);
            btnManageTaxSlabs.Click += BtnManageTaxSlabs_Click;
            actionPanel.Controls.Add(btnManageTaxSlabs);

            Button btnPrintCatalog = new Button();
            btnPrintCatalog.Text = "🖨️ Print List";
            btnPrintCatalog.Size = new Size(155, 40);
            btnPrintCatalog.Location = new Point(755, 0);
            Theme.StylePrimaryButton(btnPrintCatalog);
            btnPrintCatalog.Click += BtnPrintCatalog_Click;
            actionPanel.Controls.Add(btnPrintCatalog);

            this.Controls.Add(actionPanel);
        }

        private void BtnManageCategories_Click(object sender, EventArgs e)
        {
            using (CategoryMasterDialog dlg = new CategoryMasterDialog())
            {
                dlg.ShowDialog();
            }
        }

        private void BtnManageTaxSlabs_Click(object sender, EventArgs e)
        {
            using (TaxSlabMasterDialog dlg = new TaxSlabMasterDialog())
            {
                dlg.ShowDialog();
            }
        }

        private void LoadProducts()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT Id, Code, Name, Category, Unit, PurchasePrice as [Cost Price], 
                               SalesPrice as [Sales Price], Stock as [Qty In Stock], MinStockLevel as [Min Level], TaxPercent as [Tax Slab (%)], Description 
                        FROM Products 
                        WHERE Code LIKE @search OR Name LIKE @search OR Category LIKE @search
                        ORDER BY Name ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        string searchVal = $"%"+txtSearch.Text.Trim()+"%";
                        cmd.Parameters.AddWithValue("@search", searchVal);

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gridProducts.DataSource = dt;
                        }
                    }
                }

                // Hide columns beautifully
                if (gridProducts.Columns["Id"] != null) gridProducts.Columns["Id"].Visible = false;
                if (gridProducts.Columns["Description"] != null) gridProducts.Columns["Description"].Visible = false;
                if (gridProducts.Columns["Qty In Stock"] != null) gridProducts.Columns["Qty In Stock"].Visible = false;

                // Configure column HeaderTexts and formatting
                if (gridProducts.Columns["Code"] != null) gridProducts.Columns["Code"].HeaderText = "Code";
                if (gridProducts.Columns["Name"] != null) gridProducts.Columns["Name"].HeaderText = "Product Name";
                if (gridProducts.Columns["Category"] != null) gridProducts.Columns["Category"].HeaderText = "Category";
                if (gridProducts.Columns["Unit"] != null) gridProducts.Columns["Unit"].HeaderText = "Unit";
                if (gridProducts.Columns["Cost Price"] != null)
                {
                    gridProducts.Columns["Cost Price"].HeaderText = "Cost Price";
                    gridProducts.Columns["Cost Price"].DefaultCellStyle.Format = "N2";
                }
                if (gridProducts.Columns["Sales Price"] != null)
                {
                    gridProducts.Columns["Sales Price"].HeaderText = "Sales Price";
                    gridProducts.Columns["Sales Price"].DefaultCellStyle.Format = "N2";
                }
                if (gridProducts.Columns["Qty In Stock"] != null) gridProducts.Columns["Qty In Stock"].HeaderText = "Stock Qty";
                if (gridProducts.Columns["Min Level"] != null) gridProducts.Columns["Min Level"].HeaderText = "Min Level";
                if (gridProducts.Columns["Tax Slab (%)"] != null)
                {
                    gridProducts.Columns["Tax Slab (%)"].HeaderText = "Tax Slab (%)";
                    gridProducts.Columns["Tax Slab (%)"].DefaultCellStyle.Format = "0.##";
                }

                // Configure Fill Weights for elegant proportional sizing
                if (gridProducts.Columns["Code"] != null) gridProducts.Columns["Code"].FillWeight = 50;
                if (gridProducts.Columns["Name"] != null) gridProducts.Columns["Name"].FillWeight = 130;
                if (gridProducts.Columns["Category"] != null) gridProducts.Columns["Category"].FillWeight = 80;
                if (gridProducts.Columns["Unit"] != null) gridProducts.Columns["Unit"].FillWeight = 50;
                if (gridProducts.Columns["Cost Price"] != null) gridProducts.Columns["Cost Price"].FillWeight = 70;
                if (gridProducts.Columns["Sales Price"] != null) gridProducts.Columns["Sales Price"].FillWeight = 70;
                if (gridProducts.Columns["Qty In Stock"] != null) gridProducts.Columns["Qty In Stock"].FillWeight = 75;
                if (gridProducts.Columns["Min Level"] != null) gridProducts.Columns["Min Level"].FillWeight = 60;
                if (gridProducts.Columns["Tax Slab (%)"] != null) gridProducts.Columns["Tax Slab (%)"].FillWeight = 65;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading products: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadProducts();
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (ProductDialog dlg = new ProductDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadProducts();
                }
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (gridProducts.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a product to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow selectedRow = gridProducts.SelectedRows[0];
            int id = Convert.ToInt32(selectedRow.Cells["Id"].Value);
            string code = selectedRow.Cells["Code"].Value.ToString();
            string name = selectedRow.Cells["Name"].Value.ToString();
            string category = selectedRow.Cells["Category"].Value?.ToString() ?? "";
            string unit = selectedRow.Cells["Unit"].Value?.ToString() ?? "Pcs";
            decimal cost = Convert.ToDecimal(selectedRow.Cells["Cost Price"].Value);
            decimal sales = Convert.ToDecimal(selectedRow.Cells["Sales Price"].Value);
            decimal stock = Convert.ToDecimal(selectedRow.Cells["Qty In Stock"].Value);
            decimal minLevel = Convert.ToDecimal(selectedRow.Cells["Min Level"].Value);
            decimal taxPercent = Convert.ToDecimal(selectedRow.Cells["Tax Slab (%)"].Value);
            string desc = selectedRow.Cells["Description"].Value?.ToString() ?? "";

            using (ProductDialog dlg = new ProductDialog(id, code, name, category, unit, cost, sales, stock, minLevel, desc, taxPercent))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadProducts();
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (gridProducts.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a product to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow selectedRow = gridProducts.SelectedRows[0];
            int id = Convert.ToInt32(selectedRow.Cells["Id"].Value);
            string name = selectedRow.Cells["Name"].Value.ToString();

            DialogResult confirm = MessageBox.Show($"Are you sure you want to permanently delete product '{name}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm == DialogResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("DELETE FROM Products WHERE Id = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    LoadProducts();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting product: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Nested Product Dialog for modal add/edit
        private class ProductDialog : Form
        {
            private int? productId = null;
            private TextBox txtCode;
            private TextBox txtName;
            private ComboBox comboCategory;
            private ComboBox comboUnit;
            private TextBox txtMinLevel;
            private TextBox txtCostPrice;
            private TextBox txtSalesPrice;
            private TextBox txtDescription;
            private ComboBox comboTaxSlab;
            private Button btnSave;
            private Button btnCancel;

            public ProductDialog()
            {
                InitializeComponent("Add Product");
                this.ActiveControl = txtCode;
            }

            public ProductDialog(int id, string code, string name, string category, string unit, decimal cost, decimal sales, decimal stock, decimal minLevel, string desc, decimal taxPercent)
            {
                this.productId = id;
                InitializeComponent("Edit Product");
                txtCode.Text = code;
                txtName.Text = name;
                
                // Add the category dynamically if it's not currently in the seeded database
                if (!string.IsNullOrEmpty(category) && !comboCategory.Items.Contains(category))
                {
                    comboCategory.Items.Add(category);
                }
                comboCategory.Text = category;

                // Select correct Unit in dropdown
                if (!string.IsNullOrEmpty(unit) && comboUnit.Items.Contains(unit))
                {
                    comboUnit.SelectedItem = unit;
                }
                else
                {
                    comboUnit.SelectedIndex = 0;
                }

                txtCostPrice.Text = cost.ToString("0.00");
                txtSalesPrice.Text = sales.ToString("0.00");
                txtMinLevel.Text = minLevel.ToString("0.###");
                txtDescription.Text = desc;

                // Select correct Tax Slab dropdown item
                bool taxFound = false;
                for (int i = 0; i < comboTaxSlab.Items.Count; i++)
                {
                    string itemStr = comboTaxSlab.Items[i].ToString();
                    if (itemStr.StartsWith($"{taxPercent:0.##}%") || 
                        (taxPercent == 0 && itemStr.Contains("Exempt")))
                    {
                        comboTaxSlab.SelectedIndex = i;
                        taxFound = true;
                        break;
                    }
                }
                if (!taxFound)
                {
                    comboTaxSlab.Items.Add($"{taxPercent:0.##}%");
                    comboTaxSlab.SelectedIndex = comboTaxSlab.Items.Count - 1;
                }
                
                // If editing, make code read-only to preserve reference integrity
                txtCode.ReadOnly = true;
                txtCode.BackColor = Theme.AlternateRow;
                this.ActiveControl = txtName;
            }

            protected override void OnLoad(EventArgs e)
            {
                base.OnLoad(e);
                if (txtCode.ReadOnly)
                    txtName.Focus();
                else
                    txtCode.Focus();
            }

            private void InitializeComponent(string title)
            {
                this.Text = title;
                this.ClientSize = new Size(480, 685); // Expanded height to accommodate prices
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

                int startY = 80;
                int gapY = 65;

                // Category
                Label lblCategory = new Label();
                lblCategory.Text = "Category";
                lblCategory.Location = new Point(20, startY);
                lblCategory.AutoSize = true;
                Theme.StyleLabel(lblCategory, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblCategory);

                comboCategory = new ComboBox();
                comboCategory.Size = new Size(210, 30);
                comboCategory.Location = new Point(20, startY + 20);
                comboCategory.DropDownStyle = ComboBoxStyle.DropDownList;
                Theme.StyleComboBox(comboCategory);
                this.Controls.Add(comboCategory);

                // Measuring Unit
                Label lblUnit = new Label();
                lblUnit.Text = "Measuring Unit *";
                lblUnit.Location = new Point(250, startY);
                lblUnit.AutoSize = true;
                Theme.StyleLabel(lblUnit, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblUnit);

                comboUnit = new ComboBox();
                comboUnit.Size = new Size(210, 30);
                comboUnit.Location = new Point(250, startY + 20);
                comboUnit.DropDownStyle = ComboBoxStyle.DropDownList;
                comboUnit.Items.AddRange(new string[] { "Pcs", "Kg", "Mtr", "Ltr", "Bag", "Roll", "Packet", "Box", "Dozen", "Feet", "Inch" });
                comboUnit.SelectedIndex = 0;
                Theme.StyleComboBox(comboUnit);
                this.Controls.Add(comboUnit);

                LoadCategories();

                // Code (SKU)
                Label lblCode = new Label();
                lblCode.Text = "Product Code / Barcode (SKU) *";
                lblCode.Location = new Point(20, startY + gapY);
                lblCode.AutoSize = true;
                Theme.StyleLabel(lblCode, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblCode);

                txtCode = new TextBox();
                txtCode.Size = new Size(420, 30);
                txtCode.Location = new Point(20, startY + gapY + 20);
                Theme.StyleTextBox(txtCode);
                txtCode.PreviewKeyDown += TxtCode_PreviewKeyDown;
                txtCode.KeyDown += TxtCode_KeyDown;
                this.Controls.Add(txtCode);

                // Name
                Label lblName = new Label();
                lblName.Text = "Product Name *";
                lblName.Location = new Point(20, startY + (gapY * 2));
                lblName.AutoSize = true;
                Theme.StyleLabel(lblName, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblName);

                txtName = new TextBox();
                txtName.Size = new Size(420, 30);
                txtName.Location = new Point(20, startY + (gapY * 2) + 20);
                Theme.StyleTextBox(txtName);
                this.Controls.Add(txtName);

                // Alert Qty Threshold
                Label lblMin = new Label();
                lblMin.Text = "Alert Qty Threshold *";
                lblMin.Location = new Point(20, startY + (gapY * 3));
                lblMin.AutoSize = true;
                Theme.StyleLabel(lblMin, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblMin);

                txtMinLevel = new TextBox();
                txtMinLevel.Size = new Size(420, 30);
                txtMinLevel.Location = new Point(20, startY + (gapY * 3) + 20);
                Theme.StyleTextBox(txtMinLevel);
                txtMinLevel.Text = "5";
                this.Controls.Add(txtMinLevel);

                // Cost Price (left)
                Label lblCostPrice = new Label();
                lblCostPrice.Text = "Cost Price *";
                lblCostPrice.Location = new Point(20, startY + (gapY * 4));
                lblCostPrice.AutoSize = true;
                Theme.StyleLabel(lblCostPrice, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblCostPrice);

                txtCostPrice = new TextBox();
                txtCostPrice.Size = new Size(210, 30);
                txtCostPrice.Location = new Point(20, startY + (gapY * 4) + 20);
                Theme.StyleTextBox(txtCostPrice);
                txtCostPrice.Text = "0.00";
                this.Controls.Add(txtCostPrice);

                // Sales Price (right)
                Label lblSalesPrice = new Label();
                lblSalesPrice.Text = "Sales Price *";
                lblSalesPrice.Location = new Point(250, startY + (gapY * 4));
                lblSalesPrice.AutoSize = true;
                Theme.StyleLabel(lblSalesPrice, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblSalesPrice);

                txtSalesPrice = new TextBox();
                txtSalesPrice.Size = new Size(210, 30);
                txtSalesPrice.Location = new Point(250, startY + (gapY * 4) + 20);
                Theme.StyleTextBox(txtSalesPrice);
                txtSalesPrice.Text = "0.00";
                this.Controls.Add(txtSalesPrice);

                // Tax Slab
                Label lblTaxSlab = new Label();
                lblTaxSlab.Text = "Tax Slab / VAT (%) *";
                lblTaxSlab.Location = new Point(20, startY + (gapY * 5));
                lblTaxSlab.AutoSize = true;
                Theme.StyleLabel(lblTaxSlab, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblTaxSlab);

                comboTaxSlab = new ComboBox();
                comboTaxSlab.Size = new Size(420, 30);
                comboTaxSlab.Location = new Point(20, startY + (gapY * 5) + 20);
                comboTaxSlab.DropDownStyle = ComboBoxStyle.DropDownList;
                Theme.StyleComboBox(comboTaxSlab);
                this.Controls.Add(comboTaxSlab);
                LoadTaxSlabs();

                // Description
                Label lblDesc = new Label();
                lblDesc.Text = "Description / Specifications";
                lblDesc.Location = new Point(20, startY + (gapY * 6));
                lblDesc.AutoSize = true;
                Theme.StyleLabel(lblDesc, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblDesc);

                txtDescription = new TextBox();
                txtDescription.Size = new Size(420, 50);
                txtDescription.Location = new Point(20, startY + (gapY * 6) + 20);
                txtDescription.Multiline = true;
                Theme.StyleTextBox(txtDescription);
                this.Controls.Add(txtDescription);

                // Action buttons
                btnSave = new Button();
                btnSave.Text = "Save Product";
                btnSave.Size = new Size(200, 45);
                btnSave.Location = new Point(20, 600);
                Theme.StyleSuccessButton(btnSave);
                btnSave.Click += BtnSave_Click;
                this.Controls.Add(btnSave);

                btnCancel = new Button();
                btnCancel.Text = "Cancel";
                btnCancel.Size = new Size(200, 45);
                btnCancel.Location = new Point(240, 600);
                Theme.StyleSecondaryButton(btnCancel);
                btnCancel.Click += (s, e) => this.Close();
                this.Controls.Add(btnCancel);

                this.AcceptButton = btnSave;
            }

            private void LoadTaxSlabs()
            {
                comboTaxSlab.Items.Clear();
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("SELECT Name, TaxPercent FROM TaxSlabs ORDER BY TaxPercent ASC", conn))
                        {
                            using (SqlDataReader rdr = cmd.ExecuteReader())
                            {
                                while (rdr.Read())
                                {
                                    string name = rdr["Name"].ToString();
                                    decimal pct = Convert.ToDecimal(rdr["TaxPercent"]);
                                    comboTaxSlab.Items.Add($"{pct:0.##}% ({name})");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading tax slabs for dropdown: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                if (comboTaxSlab.Items.Count == 0)
                {
                    comboTaxSlab.Items.Add("13% (Standard VAT)");
                    comboTaxSlab.Items.Add("0% (Exempt)");
                }
                comboTaxSlab.SelectedIndex = 0;
            }

            private void LoadCategories()
            {
                comboCategory.Items.Clear();
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("SELECT Name FROM Categories ORDER BY Name ASC", conn))
                        {
                            using (SqlDataReader rdr = cmd.ExecuteReader())
                            {
                                while (rdr.Read())
                                {
                                    comboCategory.Items.Add(rdr["Name"].ToString());
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading categories for dropdown: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                if (comboCategory.Items.Count == 0)
                {
                    comboCategory.Items.Add("Others");
                }
                comboCategory.SelectedIndex = 0;
            }

            private void TxtCode_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.IsInputKey = true;
                }
            }

            private void TxtCode_KeyDown(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    txtName.Focus();
                }
            }

            private void BtnSave_Click(object sender, EventArgs e)
            {
                string code = txtCode.Text.Trim();
                string name = txtName.Text.Trim();
                string category = comboCategory.SelectedItem?.ToString() ?? "Others";
                string unit = comboUnit.SelectedItem?.ToString() ?? "Pcs";
                string desc = txtDescription.Text.Trim();

                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Product Code and Name are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!decimal.TryParse(txtMinLevel.Text.Trim(), out decimal minLevel) || minLevel < 0)
                {
                    MessageBox.Show("Please enter a valid non-negative numeric value for threshold quantity.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!decimal.TryParse(txtCostPrice.Text.Trim(), out decimal costPrice) || costPrice < 0)
                {
                    MessageBox.Show("Please enter a valid non-negative decimal value for Cost Price.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!decimal.TryParse(txtSalesPrice.Text.Trim(), out decimal salesPrice) || salesPrice < 0)
                {
                    MessageBox.Show("Please enter a valid non-negative decimal value for Sales Price.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Extract tax slab percentage
                decimal taxPercent = 13.00m;
                string taxSel = comboTaxSlab.SelectedItem?.ToString() ?? "13%";
                string digits = "";
                foreach (char c in taxSel)
                {
                    if (char.IsDigit(c) || c == '.') digits += c;
                    else if (c == '%') break;
                }
                decimal.TryParse(digits, out taxPercent);

                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();

                        // Unique code validation on Add
                        if (productId == null)
                        {
                            using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM Products WHERE Code = @code", conn))
                            {
                                checkCmd.Parameters.AddWithValue("@code", code);
                                if ((int)checkCmd.ExecuteScalar() > 0)
                                {
                                    MessageBox.Show($"Product Code already exists! A product with code '{code}' is already registered in the catalog.", "Duplicate Product Code", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }
                            }

                            // INSERT
                            int insertedId = 0;
                            using (SqlCommand cmd = new SqlCommand(@"
                                INSERT INTO Products (Code, Name, Category, Unit, PurchasePrice, SalesPrice, Stock, MinStockLevel, TaxPercent, Description) 
                                OUTPUT INSERTED.Id
                                VALUES (@code, @name, @category, @unit, @purchasePrice, @salesPrice, 0.000, @minLevel, @taxPercent, @desc)", conn))
                            {
                                cmd.Parameters.AddWithValue("@code", code);
                                cmd.Parameters.AddWithValue("@name", name);
                                cmd.Parameters.AddWithValue("@category", category);
                                cmd.Parameters.AddWithValue("@unit", unit);
                                cmd.Parameters.AddWithValue("@purchasePrice", costPrice);
                                cmd.Parameters.AddWithValue("@salesPrice", salesPrice);
                                cmd.Parameters.AddWithValue("@minLevel", minLevel);
                                cmd.Parameters.AddWithValue("@taxPercent", taxPercent);
                                cmd.Parameters.AddWithValue("@desc", desc);
                                insertedId = Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            // Log Price History if prices > 0
                            if (costPrice > 0 || salesPrice > 0)
                            {
                                using (SqlCommand histCmd = new SqlCommand(@"
                                    INSERT INTO ProductPriceHistory (ProductId, OldPurchasePrice, NewPurchasePrice, OldSalesPrice, NewSalesPrice, ChangeDate, ChangedBy, Source)
                                    VALUES (@prodId, 0.00, @purchasePrice, 0.00, @salesPrice, GETDATE(), @userId, @source)", conn))
                                {
                                    histCmd.Parameters.AddWithValue("@prodId", insertedId);
                                    histCmd.Parameters.AddWithValue("@purchasePrice", costPrice);
                                    histCmd.Parameters.AddWithValue("@salesPrice", salesPrice);
                                    if (Session.UserId > 0)
                                        histCmd.Parameters.AddWithValue("@userId", Session.UserId);
                                    else
                                        histCmd.Parameters.AddWithValue("@userId", DBNull.Value);
                                    histCmd.Parameters.AddWithValue("@source", "Product Master Creation");
                                    histCmd.ExecuteNonQuery();
                                }
                            }
                        }
                        else
                        {
                            // UPDATE (code and stock are managed elsewhere/transactionally)
                            string updateSql = @"
                                DECLARE @oldPurchase DECIMAL(18,2);
                                DECLARE @oldSales DECIMAL(18,2);

                                SELECT @oldPurchase = PurchasePrice, @oldSales = SalesPrice 
                                FROM Products 
                                WHERE Id = @id;

                                UPDATE Products 
                                SET Name = @name, Category = @category, Unit = @unit, PurchasePrice = @purchasePrice, SalesPrice = @salesPrice, MinStockLevel = @minLevel, TaxPercent = @taxPercent, Description = @desc 
                                WHERE Id = @id;

                                IF (@oldPurchase <> @purchasePrice OR @oldSales <> @salesPrice)
                                BEGIN
                                    INSERT INTO ProductPriceHistory (ProductId, OldPurchasePrice, NewPurchasePrice, OldSalesPrice, NewSalesPrice, ChangeDate, ChangedBy, Source)
                                    VALUES (@id, @oldPurchase, @purchasePrice, @oldSales, @salesPrice, GETDATE(), @userId, @source);
                                END";
                            using (SqlCommand cmd = new SqlCommand(updateSql, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", productId.Value);
                                cmd.Parameters.AddWithValue("@name", name);
                                cmd.Parameters.AddWithValue("@category", category);
                                cmd.Parameters.AddWithValue("@unit", unit);
                                cmd.Parameters.AddWithValue("@purchasePrice", costPrice);
                                cmd.Parameters.AddWithValue("@salesPrice", salesPrice);
                                cmd.Parameters.AddWithValue("@minLevel", minLevel);
                                cmd.Parameters.AddWithValue("@taxPercent", taxPercent);
                                cmd.Parameters.AddWithValue("@desc", desc);
                                if (Session.UserId > 0)
                                    cmd.Parameters.AddWithValue("@userId", Session.UserId);
                                else
                                    cmd.Parameters.AddWithValue("@userId", DBNull.Value);
                                cmd.Parameters.AddWithValue("@source", "Product Master Update");
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving product details: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Nested Category Master Dialog
        private class CategoryMasterDialog : Form
        {
            private DataGridView gridCategories;
            private TextBox txtName;
            private Button btnAdd;
            private Button btnDelete;
            private Button btnClose;

            public CategoryMasterDialog()
            {
                InitializeComponent();
                LoadCategories();
                this.ActiveControl = txtName;
            }

            protected override void OnLoad(EventArgs e)
            {
                base.OnLoad(e);
                txtName.Focus();
            }

            private void InitializeComponent()
            {
                this.Text = "Manage Categories";
                this.ClientSize = new Size(440, 500); // Expanded width and sets inner client area precisely!
                this.Font = Theme.MainFont;
                this.AutoScaleDimensions = new SizeF(7F, 15F);
                this.AutoScaleMode = AutoScaleMode.Font;
                this.AutoScroll = true;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.StartPosition = FormStartPosition.CenterParent;
                this.BackColor = Theme.Primary;

                // Header
                Label lblHeader = new Label();
                lblHeader.Text = "Category Master";
                lblHeader.Location = new Point(20, 15);
                lblHeader.AutoSize = true;
                Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
                this.Controls.Add(lblHeader);

                // Add Panel
                Label lblName = new Label();
                lblName.Text = "Category Name *";
                lblName.Location = new Point(20, 60);
                lblName.AutoSize = true;
                Theme.StyleLabel(lblName, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblName);

                txtName = new TextBox();
                txtName.Size = new Size(250, 32); 
                txtName.Location = new Point(20, 85);
                Theme.StyleTextBox(txtName);
                this.Controls.Add(txtName);

                btnAdd = new Button();
                btnAdd.Text = "+ Add";
                btnAdd.Size = new Size(130, 38); 
                btnAdd.Location = new Point(290, 82); 
                Theme.StyleSuccessButton(btnAdd);
                btnAdd.Click += BtnAdd_Click;
                this.Controls.Add(btnAdd);

                // Grid View
                gridCategories = new DataGridView();
                gridCategories.Size = new Size(365, 270);
                gridCategories.Location = new Point(20, 130);
                Theme.StyleGrid(gridCategories);
                this.Controls.Add(gridCategories);

                // Delete & Close buttons
                btnDelete = new Button();
                btnDelete.Text = "🗑️ Delete Selected";
                btnDelete.Size = new Size(185, 42); 
                btnDelete.Location = new Point(20, 430); 
                Theme.StyleDangerButton(btnDelete);
                btnDelete.Click += BtnDelete_Click;
                this.Controls.Add(btnDelete);

                btnClose = new Button();
                btnClose.Text = "Close";
                btnClose.Size = new Size(185, 42); 
                btnClose.Location = new Point(235, 430); 
                Theme.StyleSecondaryButton(btnClose);
                btnClose.Click += (s, e) => this.Close();
                this.Controls.Add(btnClose);
            }

            private void LoadCategories()
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        string query = "SELECT Id, Name FROM Categories ORDER BY Name ASC";
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                DataTable dt = new DataTable();
                                da.Fill(dt);
                                gridCategories.DataSource = dt;
                            }
                        }
                    }

                    if (gridCategories.Columns["Id"] != null)
                        gridCategories.Columns["Id"].Visible = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading categories: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            private void BtnAdd_Click(object sender, EventArgs e)
            {
                string name = txtName.Text.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Category name cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();

                        // Unique Check
                        using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM Categories WHERE Name = @name", conn))
                        {
                            checkCmd.Parameters.AddWithValue("@name", name);
                            if ((int)checkCmd.ExecuteScalar() > 0)
                            {
                                MessageBox.Show("This category already exists.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }

                        using (SqlCommand cmd = new SqlCommand("INSERT INTO Categories (Name) VALUES (@name)", conn))
                        {
                            cmd.Parameters.AddWithValue("@name", name);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    txtName.Clear();
                    LoadCategories();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding category: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            private void BtnDelete_Click(object sender, EventArgs e)
            {
                if (gridCategories.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a category to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                DataGridViewRow row = gridCategories.SelectedRows[0];
                int id = Convert.ToInt32(row.Cells["Id"].Value);
                string name = row.Cells["Name"].Value.ToString();

                DialogResult confirm = MessageBox.Show($"Are you sure you want to delete category '{name}'?\nThis will not delete products belonging to this category.", 
                    "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm == DialogResult.Yes)
                {
                    try
                    {
                        using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                        {
                            conn.Open();
                            using (SqlCommand cmd = new SqlCommand("DELETE FROM Categories WHERE Id = @id", conn))
                            {
                                cmd.Parameters.AddWithValue("@id", id);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        LoadCategories();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting category: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private class TaxSlabMasterDialog : Form
        {
            private DataGridView gridTaxSlabs;
            private TextBox txtName;
            private TextBox txtPercent;
            private Button btnAdd;
            private Button btnDelete;
            private Button btnClose;

            public TaxSlabMasterDialog()
            {
                InitializeComponent();
                LoadTaxSlabs();
                this.ActiveControl = txtName;
            }

            protected override void OnLoad(EventArgs e)
            {
                base.OnLoad(e);
                txtName.Focus();
            }

            private void InitializeComponent()
            {
                this.Text = "Manage Tax Slabs";
                this.ClientSize = new Size(460, 520);
                this.Font = Theme.MainFont;
                this.AutoScaleDimensions = new SizeF(7F, 15F);
                this.AutoScaleMode = AutoScaleMode.Font;
                this.AutoScroll = true;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.StartPosition = FormStartPosition.CenterParent;
                this.BackColor = Theme.Primary;

                // Header
                Label lblHeader = new Label();
                lblHeader.Text = "Tax Slab Master";
                lblHeader.Location = new Point(20, 15);
                lblHeader.AutoSize = true;
                Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
                this.Controls.Add(lblHeader);

                // Add Panel
                Label lblName = new Label();
                lblName.Text = "Slab Name *";
                lblName.Location = new Point(20, 60);
                lblName.AutoSize = true;
                Theme.StyleLabel(lblName, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblName);

                txtName = new TextBox();
                txtName.Size = new Size(160, 32);
                txtName.Location = new Point(20, 85);
                Theme.StyleTextBox(txtName);
                txtName.PlaceholderText = "e.g. GST 18%";
                this.Controls.Add(txtName);

                Label lblPercent = new Label();
                lblPercent.Text = "Tax Rate (%) *";
                lblPercent.Location = new Point(190, 60);
                lblPercent.AutoSize = true;
                Theme.StyleLabel(lblPercent, Theme.TextLight, Theme.BoldFont);
                this.Controls.Add(lblPercent);

                txtPercent = new TextBox();
                txtPercent.Size = new Size(110, 32);
                txtPercent.Location = new Point(190, 85);
                Theme.StyleTextBox(txtPercent);
                txtPercent.PlaceholderText = "e.g. 18.00";
                this.Controls.Add(txtPercent);

                btnAdd = new Button();
                btnAdd.Text = "+ Add";
                btnAdd.Size = new Size(110, 38);
                btnAdd.Location = new Point(320, 82);
                Theme.StyleSuccessButton(btnAdd);
                btnAdd.Click += BtnAdd_Click;
                this.Controls.Add(btnAdd);

                // Grid View
                gridTaxSlabs = new DataGridView();
                gridTaxSlabs.Size = new Size(410, 270);
                gridTaxSlabs.Location = new Point(20, 130);
                Theme.StyleGrid(gridTaxSlabs);
                this.Controls.Add(gridTaxSlabs);

                // Delete & Close buttons
                btnDelete = new Button();
                btnDelete.Text = "🗑️ Delete Selected";
                btnDelete.Size = new Size(200, 42);
                btnDelete.Location = new Point(20, 440);
                Theme.StyleDangerButton(btnDelete);
                btnDelete.Click += BtnDelete_Click;
                this.Controls.Add(btnDelete);

                btnClose = new Button();
                btnClose.Text = "Close";
                btnClose.Size = new Size(200, 42);
                btnClose.Location = new Point(230, 440);
                Theme.StyleSecondaryButton(btnClose);
                btnClose.Click += (s, e) => this.Close();
                this.Controls.Add(btnClose);
            }

            private void LoadTaxSlabs()
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        string query = "SELECT Id, Name as [Slab Name], TaxPercent as [Tax Percent (%)] FROM TaxSlabs ORDER BY TaxPercent ASC";
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                DataTable dt = new DataTable();
                                da.Fill(dt);
                                gridTaxSlabs.DataSource = dt;
                            }
                        }
                    }

                    if (gridTaxSlabs.Columns["Id"] != null)
                        gridTaxSlabs.Columns["Id"].Visible = false;
                    
                    if (gridTaxSlabs.Columns["Tax Percent (%)"] != null)
                        gridTaxSlabs.Columns["Tax Percent (%)"].DefaultCellStyle.Format = "0.##";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading tax slabs: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            private void BtnAdd_Click(object sender, EventArgs e)
            {
                string name = txtName.Text.Trim();
                string percentText = txtPercent.Text.Trim();

                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Tax slab name cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!decimal.TryParse(percentText, out decimal percent) || percent < 0 || percent > 100)
                {
                    MessageBox.Show("Please enter a valid tax percentage between 0 and 100.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();

                        // Unique Check
                        using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM TaxSlabs WHERE Name = @name OR TaxPercent = @percent", conn))
                        {
                            checkCmd.Parameters.AddWithValue("@name", name);
                            checkCmd.Parameters.AddWithValue("@percent", percent);
                            if ((int)checkCmd.ExecuteScalar() > 0)
                            {
                                MessageBox.Show("A tax slab with this name or percentage already exists.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }

                        using (SqlCommand cmd = new SqlCommand("INSERT INTO TaxSlabs (Name, TaxPercent) VALUES (@name, @percent)", conn))
                        {
                            cmd.Parameters.AddWithValue("@name", name);
                            cmd.Parameters.AddWithValue("@percent", percent);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    txtName.Clear();
                    txtPercent.Clear();
                    LoadTaxSlabs();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding tax slab: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            private void BtnDelete_Click(object sender, EventArgs e)
            {
                if (gridTaxSlabs.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a tax slab to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                DataGridViewRow row = gridTaxSlabs.SelectedRows[0];
                int id = Convert.ToInt32(row.Cells["Id"].Value);
                string name = row.Cells["Slab Name"].Value.ToString();

                DialogResult confirm = MessageBox.Show($"Are you sure you want to delete tax slab '{name}'?\nThis will not delete products using this tax slab, but the slab will no longer be select-able.", 
                    "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm == DialogResult.Yes)
                {
                    try
                    {
                        using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                        {
                            conn.Open();
                            using (SqlCommand cmd = new SqlCommand("DELETE FROM TaxSlabs WHERE Id = @id", conn))
                            {
                                cmd.Parameters.AddWithValue("@id", id);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        LoadTaxSlabs();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting tax slab: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnPrintCatalog_Click(object sender, EventArgs e)
        {
            DataTable dt = gridProducts.DataSource as DataTable;
            if (dt == null || dt.Rows.Count == 0)
            {
                MessageBox.Show("No products available to print.", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            currentCatalogRowIndex = 0;
            catalogPageNumber = 1;

            PrintDocument catalogDoc = new PrintDocument();
            catalogDoc.DefaultPageSettings.Margins = new Margins(50, 50, 50, 50);
            
            catalogDoc.PrintPage += (s, ev) =>
            {
                Graphics g = ev.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Load shop profile info
                string shopName = "Mero Dokan Shop";
                string phone = "";
                string address = "";
                try
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 ShopName, Phone, Address FROM AppProfile", conn))
                        {
                            using (SqlDataReader rdr = cmd.ExecuteReader())
                            {
                                if (rdr.Read())
                                {
                                    shopName = rdr["ShopName"].ToString();
                                    phone = rdr["Phone"].ToString();
                                    address = rdr["Address"].ToString();
                                }
                            }
                        }
                    }
                }
                catch { }

                int startX = ev.MarginBounds.Left;
                int startY = ev.MarginBounds.Top;
                int width = ev.MarginBounds.Width;

                Font fontTitle = new Font("Segoe UI", 16F, FontStyle.Bold);
                Font fontSub = new Font("Segoe UI", 9F, FontStyle.Italic);
                Font fontHeader = new Font("Segoe UI", 10F, FontStyle.Bold);
                Font fontCell = new Font("Segoe UI", 9F, FontStyle.Regular);
                Font fontFooter = new Font("Segoe UI", 8F, FontStyle.Regular);

                Brush brush = Brushes.Black;
                Pen pen = new Pen(Color.Gray, 1);

                // Header Branding
                if (catalogPageNumber == 1)
                {
                    g.DrawString(shopName, fontTitle, brush, startX, startY);
                    g.DrawString($"{address} | Phone: {phone}", fontSub, brush, startX, startY + 28);
                    g.DrawString("PRODUCT DIRECTORY & PRICE LIST", fontHeader, brush, startX, startY + 46);
                    g.DrawLine(pen, startX, startY + 65, startX + width, startY + 65);
                    startY += 80;
                }
                else
                {
                    g.DrawString($"{shopName} - Product Catalog (Page {catalogPageNumber})", fontFooter, brush, startX, startY);
                    g.DrawLine(pen, startX, startY + 15, startX + width, startY + 15);
                    startY += 30;
                }

                // Table Layout Settings
                int colSN = startX;
                int colCode = startX + 50;
                int colName = startX + 170;
                int colCat = startX + 430;
                int colPrice = startX + 570;

                // Headers
                g.DrawLine(pen, startX, startY - 4, startX + width, startY - 4);
                
                g.DrawString("S.N.", fontHeader, brush, colSN + 5, startY);
                g.DrawString("Product Code", fontHeader, brush, colCode + 5, startY);
                g.DrawString("Product Name", fontHeader, brush, colName + 5, startY);
                g.DrawString("Category", fontHeader, brush, colCat + 5, startY);
                g.DrawString("Price (Rs.)", fontHeader, brush, colPrice + 5, startY);

                g.DrawLine(pen, startX, startY + 20, startX + width, startY + 20);

                // Draw vertical lines for header row
                int headerTop = startY - 4;
                int headerBottom = startY + 20;
                g.DrawLine(pen, startX, headerTop, startX, headerBottom);
                g.DrawLine(pen, startX + 50, headerTop, startX + 50, headerBottom);
                g.DrawLine(pen, startX + 170, headerTop, startX + 170, headerBottom);
                g.DrawLine(pen, startX + 430, headerTop, startX + 430, headerBottom);
                g.DrawLine(pen, startX + 570, headerTop, startX + 570, headerBottom);
                g.DrawLine(pen, startX + width, headerTop, startX + width, headerBottom);

                int rowY = startY + 24;
                int serialNumber = currentCatalogRowIndex + 1;
                while (currentCatalogRowIndex < dt.Rows.Count)
                {
                    // If row exceeds printable page boundaries, draw next page
                    if (rowY + 24 > ev.MarginBounds.Bottom)
                    {
                        catalogPageNumber++;
                        ev.HasMorePages = true;
                        return;
                    }

                    DataRow row = dt.Rows[currentCatalogRowIndex];
                    string code = row["Code"].ToString();
                    string name = row["Name"].ToString();
                    string cat = row["Category"]?.ToString() ?? "";
                    decimal price = Convert.ToDecimal(row["Sales Price"]);

                    int cellTop = rowY - 4;
                    int cellBottom = rowY + 20;

                    // Draw cells text
                    g.DrawString(serialNumber.ToString(), fontCell, brush, colSN + 5, rowY);
                    g.DrawString(code, fontCell, brush, colCode + 5, rowY);
                    
                    // Truncate name if too long for spacing
                    string displayName = name;
                    if (g.MeasureString(name, fontCell).Width > 240)
                    {
                        displayName = name.Substring(0, Math.Min(name.Length, 28)) + "..";
                    }
                    g.DrawString(displayName, fontCell, brush, colName + 5, rowY);
                    g.DrawString(cat, fontCell, brush, colCat + 5, rowY);
                    g.DrawString($"Rs. {price:N2}", fontCell, brush, colPrice + 5, rowY);

                    // Draw horizontal bottom border
                    g.DrawLine(pen, startX, cellBottom, startX + width, cellBottom);

                    // Draw vertical column borders
                    g.DrawLine(pen, startX, cellTop, startX, cellBottom);
                    g.DrawLine(pen, startX + 50, cellTop, startX + 50, cellBottom);
                    g.DrawLine(pen, startX + 170, cellTop, startX + 170, cellBottom);
                    g.DrawLine(pen, startX + 430, cellTop, startX + 430, cellBottom);
                    g.DrawLine(pen, startX + 570, cellTop, startX + 570, cellBottom);
                    g.DrawLine(pen, startX + width, cellTop, startX + width, cellBottom);

                    rowY += 24;
                    serialNumber++;
                    currentCatalogRowIndex++;
                }

                // Draw end of catalog footer
                g.DrawString($"Printed on {DateTime.Now:yyyy-MM-dd HH:mm} | Total Products: {dt.Rows.Count} | Page {catalogPageNumber}", fontFooter, brush, startX, ev.MarginBounds.Bottom + 10);
                ev.HasMorePages = false;
            };

            using (PrintPreviewDialog previewDlg = new PrintPreviewDialog())
            {
                previewDlg.Document = catalogDoc;
                previewDlg.Size = new Size(800, 600);
                previewDlg.StartPosition = FormStartPosition.CenterParent;
                previewDlg.ShowDialog();
            }
        }
    }
}

