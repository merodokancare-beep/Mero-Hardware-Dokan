using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace MeroHardwareDokan
{
    public class CategoryControl : UserControl
    {
        private TextBox txtSearch;
        private TextBox txtName;
        private DataGridView gridCategories;
        private Button btnAdd;
        private Button btnDelete;

        public CategoryControl()
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
            this.Font = Theme.MainFont;
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.AutoScroll = true;
            this.Size = new Size(950, 650);
            this.BackColor = Theme.Secondary;

            // Page Header
            Label lblHeader = new Label();
            lblHeader.Text = "Category Master Catalog";
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
            this.Controls.Add(lblHeader);

            // Add Category Panel
            Label lblAddHeader = new Label();
            lblAddHeader.Text = "Add New Category";
            lblAddHeader.Location = new Point(20, 65);
            lblAddHeader.AutoSize = true;
            Theme.StyleLabel(lblAddHeader, Theme.TextDark, Theme.BoldFont);
            this.Controls.Add(lblAddHeader);

            txtName = new TextBox();
            txtName.Size = new Size(250, 36);
            txtName.Location = new Point(20, 90);
            Theme.StyleTextBox(txtName);
            this.Controls.Add(txtName);

            btnAdd = new Button();
            btnAdd.Text = "➕ Add Category";
            btnAdd.Size = new Size(150, 32);
            btnAdd.Location = new Point(285, 87);
            Theme.StyleSuccessButton(btnAdd);
            btnAdd.Click += BtnAdd_Click;
            this.Controls.Add(btnAdd);

            // Search Panel container
            Label lblSearchHeader = new Label();
            lblSearchHeader.Text = "Search Categories";
            lblSearchHeader.Location = new Point(480, 65);
            lblSearchHeader.AutoSize = true;
            Theme.StyleLabel(lblSearchHeader, Theme.TextDark, Theme.BoldFont);
            this.Controls.Add(lblSearchHeader);

            Panel searchPanel = new Panel();
            searchPanel.Size = new Size(300, 32);
            searchPanel.Location = new Point(480, 90);
            searchPanel.BackColor = Theme.Primary;
            searchPanel.Padding = new Padding(8, 8, 8, 8);

            txtSearch = new TextBox();
            txtSearch.BorderStyle = BorderStyle.None;
            txtSearch.BackColor = Theme.Primary;
            txtSearch.ForeColor = Theme.TextLight;
            txtSearch.Font = Theme.MainFont;
            txtSearch.Dock = DockStyle.Fill;
            txtSearch.PlaceholderText = "Type to search categories...";
            txtSearch.TextChanged += TxtSearch_TextChanged;
            searchPanel.Controls.Add(txtSearch);
            this.Controls.Add(searchPanel);

            // GridView
            gridCategories = new DataGridView();
            gridCategories.Size = new Size(910, 420);
            gridCategories.Location = new Point(20, 145);
            gridCategories.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Theme.StyleGrid(gridCategories);
            this.Controls.Add(gridCategories);

            // Action Buttons Panel
            Panel actionPanel = new Panel();
            actionPanel.Size = new Size(910, 50);
            actionPanel.Location = new Point(20, 580);
            actionPanel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            btnDelete = new Button();
            btnDelete.Text = "🗑️ Delete Selected Category";
            btnDelete.Size = new Size(250, 40);
            btnDelete.Location = new Point(0, 0);
            Theme.StyleDangerButton(btnDelete);
            btnDelete.Click += BtnDelete_Click;
            actionPanel.Controls.Add(btnDelete);

            this.Controls.Add(actionPanel);
        }

        private void LoadCategories()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    string query = "SELECT Id, Name FROM Categories WHERE Name LIKE @search ORDER BY Name ASC";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        string searchVal = $"%{txtSearch?.Text.Trim() ?? ""}%";
                        cmd.Parameters.AddWithValue("@search", searchVal);

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
                
                if (gridCategories.Columns["Name"] != null)
                    gridCategories.Columns["Name"].HeaderText = "Category Name";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading categories: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadCategories();
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

            if (name == "Others")
            {
                MessageBox.Show("Cannot delete the system-default 'Others' category.", "Action Restrained", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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
}

