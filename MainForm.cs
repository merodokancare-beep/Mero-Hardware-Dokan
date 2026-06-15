using System;
using System.Drawing;
using System.Windows.Forms;

namespace MeroHardwareDokan
{
    public class MainForm : Form
    {
        private Panel sidebarPanel;
        private Panel sidebarTopPanel;
        private FlowLayoutPanel sidebarMenuPanel;
        private Panel sidebarBottomPanel;
        private Panel headerPanel;
        private Panel mainContentPanel;

        private Label lblHeaderTitle;
        private Label lblUserSession;
        private Label lblClock;
        private Label lblSubtitle;
        private Label lblDev;
        private Label lblContact;
        private Label lblEmailSupport;
        private System.Windows.Forms.Timer clockTimer;

        // Sidebar Navigation Buttons
        private Button btnDashboard;
        private Button btnCategory;
        private Button btnProducts;
        private Button btnStock;
        private Button btnPurchases;
        private Button btnSales;
        private Button btnSalesReturn;
        private Button btnCustomers;
        private Button btnSuppliers;
        private Button btnReports;
        private Button btnDailySettlement;
        private Button btnBackup;
        private Button btnUserManagement;
        private Button btnSettings;
        private Button btnLogout;
        private PictureBox picHeaderAvatar;
        private PictureBox picSidebarLogo;
        private Label lblLogoText;

        public MainForm()
        {
            InitializeComponent();
            RefreshThemeColors();
            
            // Load Dashboard view by default
            btnDashboard.PerformClick();
        }

        private void InitializeComponent()
        {
            this.ClientSize = new Size(1250, 740); // Guarantees exact client area layout space
            this.MinimumSize = new Size(1000, 560); // Safe minimum sizing to fit 768p scaled laptop screens!
            this.WindowState = FormWindowState.Maximized; // Auto-maximizes to fit client screen size perfectly!
            this.Font = Theme.MainFont;
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.AutoScroll = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Theme.Secondary;
            this.Text = "MeroHardwareDokan - BDT Retail & Shop Management System";
            this.Icon = SystemIcons.Application;

            // 1. LEFT SIDEBAR PANEL
            sidebarPanel = new Panel();
            sidebarPanel.Width = 240;
            sidebarPanel.Dock = DockStyle.Left;
            sidebarPanel.BackColor = Theme.Primary;
            this.Controls.Add(sidebarPanel);

            // 1a. TOP PANEL (Logo, Subtitle, Divider)
            sidebarTopPanel = new Panel();
            sidebarTopPanel.Height = 95;
            sidebarTopPanel.Dock = DockStyle.Top;
            sidebarTopPanel.BackColor = Color.Transparent;
            sidebarPanel.Controls.Add(sidebarTopPanel);

            // 1b. BOTTOM PANEL (Log Out button)
            sidebarBottomPanel = new Panel();
            sidebarBottomPanel.Height = 65;
            sidebarBottomPanel.Dock = DockStyle.Bottom;
            sidebarBottomPanel.BackColor = Color.Transparent;
            sidebarPanel.Controls.Add(sidebarBottomPanel);

            // 1c. MIDDLE MENU PANEL (Scrollable Navigation FlowLayout)
            sidebarMenuPanel = new FlowLayoutPanel();
            sidebarMenuPanel.Dock = DockStyle.Fill;
            sidebarMenuPanel.FlowDirection = FlowDirection.TopDown;
            sidebarMenuPanel.WrapContents = false;
            sidebarMenuPanel.AutoScroll = true;
            sidebarMenuPanel.BackColor = Color.Transparent;
            sidebarMenuPanel.Padding = new Padding(0, 5, 0, 5);
            sidebarPanel.Controls.Add(sidebarMenuPanel);

            // Circular Logo in Sidebar - inside sidebarTopPanel
            picSidebarLogo = new PictureBox();
            picSidebarLogo.Size = new Size(40, 40);
            picSidebarLogo.Location = new Point(15, 15);
            picSidebarLogo.SizeMode = PictureBoxSizeMode.Zoom;
            picSidebarLogo.BackColor = Color.FromArgb(17, 24, 39);
            // Apply perfect circular clipping region
            using (System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath())
            {
                gp.AddEllipse(0, 0, picSidebarLogo.Width, picSidebarLogo.Height);
                picSidebarLogo.Region = new Region(gp);
            }
            sidebarTopPanel.Controls.Add(picSidebarLogo);

            // Logo Name inside Sidebar next to circular logo - inside sidebarTopPanel
            lblLogoText = new Label();
            lblLogoText.Text = "MeroHardwareDokan";
            lblLogoText.Location = new Point(62, 21);
            lblLogoText.AutoSize = true;
            Theme.StyleLabel(lblLogoText, Theme.TextLight, new Font("Segoe UI", 14F, FontStyle.Bold));
            sidebarTopPanel.Controls.Add(lblLogoText);

            lblSubtitle = new Label();
            lblSubtitle.Text = "RETAIL MANAGER v1.0";
            lblSubtitle.Location = new Point(18, 62); // Adjusted from 55 to prevent overlapping with logo
            lblSubtitle.AutoSize = true;
            lblSubtitle.UseCompatibleTextRendering = true; // Prevents clipping of text on high-DPI
            Theme.StyleLabel(lblSubtitle, Theme.TextDark, new Font("Segoe UI Semibold", 8F, FontStyle.Bold)); // Increased to 8F for scaling legibility
            sidebarTopPanel.Controls.Add(lblSubtitle);

            // Spacer line - inside sidebarTopPanel
            Panel sidebarDivider = new Panel();
            sidebarDivider.Size = new Size(210, 1);
            sidebarDivider.Location = new Point(15, 85); // Adjusted from 80
            sidebarDivider.BackColor = Theme.AlternateRow;
            sidebarTopPanel.Controls.Add(sidebarDivider);

            // Initialize Sidebar Buttons - inside sidebarMenuPanel
            int btnHeight = 44;

            btnDashboard = CreateNavButton("  📊   Dashboard Overview", btnHeight);
            btnDashboard.Click += (s, e) => ShowView(new DashboardControl(), btnDashboard, "Dashboard & Sales Performance");
            sidebarMenuPanel.Controls.Add(btnDashboard);

            btnCategory = CreateNavButton("  📂   Category Master", btnHeight);
            btnCategory.Click += (s, e) => ShowView(new CategoryControl(), btnCategory, "Category Master Catalog");
            sidebarMenuPanel.Controls.Add(btnCategory);

            btnProducts = CreateNavButton("  📦   Product Master", btnHeight);
            btnProducts.Click += (s, e) => ShowView(new ProductControl(), btnProducts, "Product Master Catalog");
            sidebarMenuPanel.Controls.Add(btnProducts);

            btnPurchases = CreateNavButton("  📥   Purchase Entry", btnHeight);
            btnPurchases.Click += (s, e) => ShowView(new PurchaseControl(), btnPurchases, "New Purchase / Goods Receipt");
            sidebarMenuPanel.Controls.Add(btnPurchases);

            btnStock = CreateNavButton("  📈   Stock & Inventory", btnHeight);
            btnStock.Click += (s, e) => ShowView(new StockControl(), btnStock, "Inventory Stock Ledger");
            sidebarMenuPanel.Controls.Add(btnStock);

            btnSales = CreateNavButton("  🛒   Sales Billing", btnHeight);
            btnSales.Click += (s, e) => ShowView(new SalesBillingControl(), btnSales, "Retail Checkout Counter");
            sidebarMenuPanel.Controls.Add(btnSales);

            btnSalesReturn = CreateNavButton("  🔄   Sales Returns", btnHeight);
            btnSalesReturn.Click += (s, e) => ShowView(new SalesReturnControl(), btnSalesReturn, "Sales Returns & Customer Refunds");
            sidebarMenuPanel.Controls.Add(btnSalesReturn);

            btnCustomers = CreateNavButton("  👥   Customer Directory", btnHeight);
            btnCustomers.Click += (s, e) => ShowView(new CustomerControl(), btnCustomers, "Customer Directory & CRM");
            sidebarMenuPanel.Controls.Add(btnCustomers);

            btnSuppliers = CreateNavButton("  🏢   Supplier Directory", btnHeight);
            btnSuppliers.Click += (s, e) => ShowView(new SupplierControl(), btnSuppliers, "Supplier & Vendor Directory");
            sidebarMenuPanel.Controls.Add(btnSuppliers);

            btnReports = CreateNavButton("  📉   Analytical Reports", btnHeight);
            btnReports.Click += (s, e) => ShowView(new ReportControl(), btnReports, "Reports & Business Intelligence");
            sidebarMenuPanel.Controls.Add(btnReports);

            btnDailySettlement = CreateNavButton("  💰   Daily Settlement", btnHeight);
            btnDailySettlement.Click += (s, e) => ShowView(new DailySettlementControl(), btnDailySettlement, "Daily Cash Register & Reconciliation");
            sidebarMenuPanel.Controls.Add(btnDailySettlement);

            btnBackup = CreateNavButton("  ⚙️   System Backup", btnHeight);
            btnBackup.Click += (s, e) => ShowView(new BackupRestoreControl(), btnBackup, "Database Disaster Recovery & Backup");
            sidebarMenuPanel.Controls.Add(btnBackup);

            btnUserManagement = CreateNavButton("  👥   User Management", btnHeight);
            btnUserManagement.Click += (s, e) => ShowView(new UserManagementControl(), btnUserManagement, "Employee Access & User Management");
            sidebarMenuPanel.Controls.Add(btnUserManagement);

            btnSettings = CreateNavButton("  ⚙️   Profile & Settings", btnHeight);
            btnSettings.Click += (s, e) => {
                var control = new ProfileSettingsControl();
                control.OnSettingsSaved += RefreshThemeColors;
                ShowView(control, btnSettings, "Shop Profile & Application Settings Panel");
            };
            sidebarMenuPanel.Controls.Add(btnSettings);

            // Log Out button at bottom of sidebar - inside sidebarBottomPanel
            btnLogout = new Button();
            btnLogout.Text = "  🚪   Log Out System";
            btnLogout.Size = new Size(205, btnHeight);
            btnLogout.Location = new Point(12, 10);
            Theme.StyleDangerButton(btnLogout);
            btnLogout.Click += BtnLogout_Click;
            sidebarBottomPanel.Controls.Add(btnLogout);

            // Set explicit docking order for sidebar sub-panels to prevent overlapping
            sidebarTopPanel.SendToBack();
            sidebarBottomPanel.SendToBack();
            sidebarMenuPanel.BringToFront();

            // 2. TOP HEADER PANEL
            headerPanel = new Panel();
            headerPanel.Height = 115;
            headerPanel.Dock = DockStyle.Top;
            headerPanel.BackColor = Theme.Primary;
            headerPanel.Padding = new Padding(20, 10, 20, 10);
            this.Controls.Add(headerPanel);

            lblHeaderTitle = new Label();
            lblHeaderTitle.Text = "Dashboard Overview";
            lblHeaderTitle.Location = new Point(20, 30);
            lblHeaderTitle.AutoSize = true;
            Theme.StyleLabel(lblHeaderTitle, Theme.TextLight, Theme.HeaderFont);
            headerPanel.Controls.Add(lblHeaderTitle);

            lblUserSession = new Label();
            lblUserSession.Text = $"Logged in as: {Session.FullName} ({Session.Role})";
            lblUserSession.Location = new Point(22, 65);
            lblUserSession.AutoSize = true;
            Theme.StyleLabel(lblUserSession, Theme.TextDark, Theme.MainFont);
            headerPanel.Controls.Add(lblUserSession);

            // Right-side Info Stack Container (Developer details, contact, email, and clock stacked & left-aligned)
            FlowLayoutPanel rightInfoPanel = new FlowLayoutPanel();
            rightInfoPanel.FlowDirection = FlowDirection.TopDown;
            rightInfoPanel.WrapContents = false;
            rightInfoPanel.AutoSize = true;
            rightInfoPanel.Location = new Point(this.Width - 355, 12);
            rightInfoPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            rightInfoPanel.BackColor = Color.Transparent;

            // Develop By Info
            lblDev = new Label();
            lblDev.Text = "Developed By: Brinda Dev Tech";
            lblDev.AutoSize = true;
            lblDev.Margin = new Padding(0, 0, 0, 2);
            Theme.StyleLabel(lblDev, Theme.TextLight, new Font("Segoe UI Semibold", 8F, FontStyle.Regular));
            rightInfoPanel.Controls.Add(lblDev);

            // Contact
            lblContact = new Label();
            lblContact.Text = "📞 +91-7797606232";
            lblContact.AutoSize = true;
            lblContact.Margin = new Padding(0, 0, 0, 2);
            Theme.StyleLabel(lblContact, Theme.TextLight, new Font("Segoe UI", 7.5F, FontStyle.Regular));
            rightInfoPanel.Controls.Add(lblContact);

            // Email
            lblEmailSupport = new Label();
            lblEmailSupport.Text = "📧 merodokancare@gmail.com";
            lblEmailSupport.AutoSize = true;
            lblEmailSupport.Margin = new Padding(0, 0, 0, 2);
            Theme.StyleLabel(lblEmailSupport, Theme.TextLight, new Font("Segoe UI", 7.5F, FontStyle.Regular));
            rightInfoPanel.Controls.Add(lblEmailSupport);

            // Clock (positioned at the bottom of the stack)
            lblClock = new Label();
            lblClock.Text = DateTime.Now.ToString("dd MMM yyyy, hh:mm:ss tt");
            lblClock.AutoSize = true;
            lblClock.Margin = new Padding(0, 0, 0, 0);
            Theme.StyleLabel(lblClock, Theme.TextLight, Theme.BoldFont);
            rightInfoPanel.Controls.Add(lblClock);

            headerPanel.Controls.Add(rightInfoPanel);

            // Profile avatar at top right
            picHeaderAvatar = new PictureBox();
            picHeaderAvatar.Size = new Size(60, 60);
            picHeaderAvatar.Location = new Point(this.Width - 85, 27);
            picHeaderAvatar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            picHeaderAvatar.SizeMode = PictureBoxSizeMode.Zoom;
            picHeaderAvatar.BorderStyle = BorderStyle.None;
            picHeaderAvatar.BackColor = Color.FromArgb(17, 24, 39);
            // Apply perfect circular clipping region
            using (System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath())
            {
                gp.AddEllipse(0, 0, picHeaderAvatar.Width, picHeaderAvatar.Height);
                picHeaderAvatar.Region = new Region(gp);
            }
            // Draw a premium circular border around the avatar
            picHeaderAvatar.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (Pen pen = new Pen(Theme.Accent, 2f))
                {
                    e.Graphics.DrawEllipse(pen, 1, 1, picHeaderAvatar.Width - 3, picHeaderAvatar.Height - 3);
                }
            };
            headerPanel.Controls.Add(picHeaderAvatar);

            // Clock updates
            clockTimer = new System.Windows.Forms.Timer();
            clockTimer.Interval = 1000;
            clockTimer.Tick += (s, e) => lblClock.Text = DateTime.Now.ToString("dd MMM yyyy, hh:mm:ss tt");
            clockTimer.Start();

            // 3. MAIN CONTENT CONTAINER PANEL
            mainContentPanel = new Panel();
            mainContentPanel.Dock = DockStyle.Fill;
            mainContentPanel.AutoScroll = true;
            mainContentPanel.BackColor = Theme.Secondary;
            mainContentPanel.Padding = new Padding(10);
            this.Controls.Add(mainContentPanel);
            
            // Set explicit docking order: sidebar first (back), then header (middle), then main content (front/last)
            headerPanel.SendToBack();
            sidebarPanel.SendToBack();
            mainContentPanel.BringToFront();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (this.WindowState != FormWindowState.Minimized)
            {
                this.PerformLayout();
                mainContentPanel?.PerformLayout();
                if (mainContentPanel != null && mainContentPanel.Controls.Count > 0)
                {
                    mainContentPanel.Controls[0].PerformLayout();
                    mainContentPanel.Controls[0].Invalidate(true);
                }
            }
        }

        private Button CreateNavButton(string text, int height)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Size = new Size(205, height);
            btn.Margin = new Padding(12, 4, 12, 4);
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = Color.Transparent;
            btn.ForeColor = Theme.TextLight;
            btn.Font = Theme.BoldFont;
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Theme.Secondary;
            btn.Cursor = Cursors.Hand;
            return btn;
        }

        private void ShowView(UserControl view, Button activeBtn, string headerTitle)
        {
            // Update Active State Visuals on Sidebar Buttons
            Button[] navButtons = { btnDashboard, btnCategory, btnProducts, btnStock, btnPurchases, btnSales, btnSalesReturn, btnCustomers, btnSuppliers, btnReports, btnDailySettlement, btnBackup, btnUserManagement, btnSettings };
            foreach (var b in navButtons)
            {
                b.BackColor = Color.Transparent;
                b.FlatAppearance.MouseOverBackColor = Theme.Secondary;
            }

            activeBtn.BackColor = Theme.Accent;
            activeBtn.FlatAppearance.MouseOverBackColor = Theme.AccentHover;

            // Swap out current Control inside Main Panel
            mainContentPanel.Controls.Clear();
            mainContentPanel.AutoScroll = true;

            // Configure views with Anchor instead of Dock so parent scrollbars are triggered correctly
            view.MinimumSize = new Size(950, 650);
            view.Location = new Point(10, 10);
            view.Size = new Size(
                Math.Max(950, mainContentPanel.ClientSize.Width - 20),
                Math.Max(650, mainContentPanel.ClientSize.Height - 20)
            );
            view.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            view.Dock = DockStyle.None;

            mainContentPanel.Controls.Add(view);

            // Update Header labels
            lblHeaderTitle.Text = headerTitle;
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            DialogResult logout = MessageBox.Show("Are you sure you want to log out of MeroHardwareDokan?", "Confirm Log Out", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (logout == DialogResult.Yes)
            {
                Session.Clear();
                
                // Hide MainForm, reload login, and restart loop in Program.cs
                this.DialogResult = DialogResult.Retry;
                this.Close();
            }
        }

        public void RefreshThemeColors()
        {
            // Apply font size scale changes recursively across all active controls instantly!
            Theme.UpdateFontRecursively(this);

            // Update forms and panels backcolors
            this.BackColor = Theme.Secondary;
            sidebarPanel.BackColor = Theme.Primary;
            headerPanel.BackColor = Theme.Primary;
            mainContentPanel.BackColor = Theme.Secondary;

            // Update header labels forecolors
            lblHeaderTitle.ForeColor = Theme.TextLight;
            lblUserSession.ForeColor = Theme.TextDark;
            lblClock.ForeColor = Theme.TextLight;
            if (lblDev != null) lblDev.ForeColor = Theme.TextLight;
            if (lblContact != null) lblContact.ForeColor = Theme.TextLight;
            if (lblEmailSupport != null) lblEmailSupport.ForeColor = Theme.TextLight;

            // Update sidebar logo/subtitle forecolors
            if (lblLogoText != null) lblLogoText.ForeColor = Theme.TextLight;
            if (lblSubtitle != null) lblSubtitle.ForeColor = Theme.TextDark;

            // Redraw navigation buttons
            Button[] navButtons = { btnDashboard, btnCategory, btnProducts, btnStock, btnPurchases, btnSales, btnSalesReturn, btnCustomers, btnSuppliers, btnReports, btnDailySettlement, btnBackup, btnUserManagement, btnSettings };
            foreach (var b in navButtons)
            {
                if (b == null) continue;
                b.ForeColor = Theme.TextLight;
                b.FlatAppearance.MouseOverBackColor = Theme.Secondary;
                if (b.BackColor != Color.Transparent)
                {
                    b.BackColor = Theme.Accent;
                    b.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
                }
            }
            
            // Reload user session text dynamically from profile details
            try
            {
                using (var conn = new Microsoft.Data.SqlClient.SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new Microsoft.Data.SqlClient.SqlCommand("SELECT TOP 1 ShopName FROM AppProfile", conn))
                    {
                        object shopName = cmd.ExecuteScalar();
                        if (shopName != null && shopName != DBNull.Value)
                        {
                            this.Text = $"{shopName} - BDT Retail & Shop Management System";
                        }
                    }
                }
            }
            catch { }

            // Reload sidebar avatar and circular logo
            LoadUserAvatar();
            LoadSidebarLogoAndName();

            // Apply Role-Based Access Control and adjust dynamic positions of sidebar buttons
            SetupRoleBasedAccess();

            // Force refresh main panels
            this.Invalidate(true);
        }

        private void LoadUserAvatar()
        {
            try
            {
                using (var conn = new Microsoft.Data.SqlClient.SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new Microsoft.Data.SqlClient.SqlCommand("SELECT TOP 1 ProfilePicPath FROM AppProfile", conn))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            string path = result.ToString();
                            if (File.Exists(path))
                            {
                                // Dispose active image to avoid lock error
                                if (picHeaderAvatar.Image != null)
                                {
                                    picHeaderAvatar.Image.Dispose();
                                    picHeaderAvatar.Image = null;
                                }

                                byte[] bytes = File.ReadAllBytes(path);
                                using (var ms = new System.IO.MemoryStream(bytes))
                                {
                                    picHeaderAvatar.Image = Image.FromStream(ms);
                                }
                                return;
                            }
                        }
                    }
                }
            }
            catch { }
            
            // Fallback: clear image if none exists
            if (picHeaderAvatar.Image != null)
            {
                picHeaderAvatar.Image.Dispose();
                picHeaderAvatar.Image = null;
            }
        }

        private void LoadSidebarLogoAndName()
        {
            string shopName = "MeroHardwareDokan";
            string logoPath = "";
            try
            {
                using (var conn = new Microsoft.Data.SqlClient.SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new Microsoft.Data.SqlClient.SqlCommand("SELECT TOP 1 ShopName, LogoPath FROM AppProfile", conn))
                    {
                        using (var rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                shopName = rdr["ShopName"].ToString();
                                logoPath = rdr["LogoPath"]?.ToString();
                            }
                        }
                    }
                }
            }
            catch { }

            // Adjust font size and truncate name dynamically to prevent sidebar overflow while showing at least 20 characters
            if (shopName.Length > 22)
            {
                lblLogoText.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
                lblLogoText.Text = shopName.Substring(0, 20) + "..";
            }
            else if (shopName.Length > 12)
            {
                lblLogoText.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
                lblLogoText.Text = shopName;
            }
            else
            {
                lblLogoText.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
                lblLogoText.Text = shopName;
            }

            // Load logo image or fallback to drawing a premium placeholder
            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
            {
                try
                {
                    // Dispose previous image to avoid lock error
                    if (picSidebarLogo.Image != null)
                    {
                        picSidebarLogo.Image.Dispose();
                        picSidebarLogo.Image = null;
                    }

                    byte[] bytes = File.ReadAllBytes(logoPath);
                    using (var ms = new System.IO.MemoryStream(bytes))
                    {
                        picSidebarLogo.Image = Image.FromStream(ms);
                    }
                    return;
                }
                catch { }
            }

            // Fallback: draw a beautiful round icon containing first letter
            try
            {
                Bitmap bmp = new Bitmap(picSidebarLogo.Width, picSidebarLogo.Height);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    
                    // Draw clean solid background
                    using (Brush bgBrush = new SolidBrush(Theme.Accent))
                    {
                        g.FillEllipse(bgBrush, 0, 0, bmp.Width, bmp.Height);
                    }

                    // Draw first letter centered in bold white
                    using (Font font = new Font("Segoe UI", 12F, FontStyle.Bold))
                    using (Brush fgBrush = new SolidBrush(Color.White))
                    {
                        string fallbackChar = string.IsNullOrEmpty(shopName) ? "M" : shopName.Substring(0, 1).ToUpper();
                        SizeF size = g.MeasureString(fallbackChar, font);
                        g.DrawString(fallbackChar, font, fgBrush, (bmp.Width - size.Width) / 2f, (bmp.Height - size.Height) / 2f);
                    }
                }

                if (picSidebarLogo.Image != null)
                {
                    picSidebarLogo.Image.Dispose();
                }
                picSidebarLogo.Image = bmp;
            }
            catch { }
        }

        private void SetupRoleBasedAccess()
        {
            bool isAdmin = string.Equals(Session.Role, "Admin", StringComparison.OrdinalIgnoreCase);

            if (btnUserManagement != null) btnUserManagement.Visible = isAdmin;
            if (btnReports != null) btnReports.Visible = isAdmin;
            if (btnBackup != null) btnBackup.Visible = isAdmin;
            if (btnSettings != null) btnSettings.Visible = isAdmin;
        }
    }
}
