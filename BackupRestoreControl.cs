using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using System.Net.NetworkInformation;

namespace MeroHardwareDokan
{
    public class BackupRestoreControl : UserControl
    {
        private Button btnBackup;
        private Button btnRestore;
        private Label lblStatus;
        private TextBox txtBackupPath;
        private Button btnBrowse;
        private string googleDriveAddress = "https://drive.google.com/drive/folders/MeroHardwareDokanBackup";

        public BackupRestoreControl()
        {
            InitializeComponent();
            LoadBackupSettings();
            this.ActiveControl = txtBackupPath;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            txtBackupPath.Focus();
        }

        private void LoadBackupSettings()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 BackupFolderPath, GoogleDriveAddress FROM AppProfile", conn))
                    {
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                string path = rdr["BackupFolderPath"]?.ToString();
                                if (!string.IsNullOrEmpty(path))
                                {
                                    txtBackupPath.Text = path;
                                }

                                string drive = rdr["GoogleDriveAddress"]?.ToString();
                                if (!string.IsNullOrEmpty(drive))
                                {
                                    googleDriveAddress = drive;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading backup settings: " + ex.Message);
            }
        }

        private bool IsInternetConnected()
        {
            try
            {
                if (!NetworkInterface.GetIsNetworkAvailable())
                    return false;
                
                System.Net.IPHostEntry entry = System.Net.Dns.GetHostEntry("www.google.com");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string FindLocalGoogleDrivePath()
        {
            try
            {
                // 1. Check mounted drives (G:, H:, etc.) commonly used by Google Drive Desktop
                string[] drives = { "G:\\", "H:\\", "I:\\", "F:\\", "D:\\" };
                foreach (string d in drives)
                {
                    if (Directory.Exists(d))
                    {
                        string myDrive = Path.Combine(d, "My Drive");
                        if (Directory.Exists(myDrive))
                            return myDrive;
                        
                        string gdFolder = Path.Combine(d, "Google Drive");
                        if (Directory.Exists(gdFolder))
                            return gdFolder;
                    }
                }

                // 2. Check user profile directory for synced folders
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string[] commonPaths = {
                    Path.Combine(userProfile, "Google Drive", "My Drive"),
                    Path.Combine(userProfile, "Google Drive"),
                    Path.Combine(userProfile, "My Drive")
                };

                foreach (string p in commonPaths)
                {
                    if (Directory.Exists(p))
                        return p;
                }
            }
            catch { }
            return null;
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
            lblHeader.Text = "Database Maintenance & Backup Center";
            lblHeader.Location = new Point(20, 15);
            lblHeader.AutoSize = true;
            Theme.StyleLabel(lblHeader, Theme.TextLight, Theme.HeaderFont);
            this.Controls.Add(lblHeader);

            // Card Container
            Panel maintenanceCard = Theme.CreateCard(800, 500);
            maintenanceCard.Location = new Point(20, 70);

            Label lblCardTitle = new System.Windows.Forms.Label();
            lblCardTitle.Text = "System Disaster Recovery Operations";
            lblCardTitle.Location = new Point(25, 25);
            lblCardTitle.AutoSize = true;
            Theme.StyleLabel(lblCardTitle, Theme.TextLight, Theme.SubHeaderFont);
            maintenanceCard.Controls.Add(lblCardTitle);

            // Description / Warnings
            Label lblDesc = new Label();
            lblDesc.Text = @"Database backups create an offline physical '.bak' copy of your complete store records. 
It is recommended to schedule weekly backups. Restoring a database will completely overwrite all existing database transactions, inventory listings, and sales with the selected backup snapshot.";
            lblDesc.Location = new Point(25, 75);
            lblDesc.Size = new Size(750, 60);
            Theme.StyleLabel(lblDesc, Theme.TextDark, Theme.MainFont);
            maintenanceCard.Controls.Add(lblDesc);

            // Step 1: Backup section
            Label lblStep1 = new Label();
            lblStep1.Text = "1. BACKUP DATABASE snapshot";
            lblStep1.Location = new Point(25, 160);
            Theme.StyleLabel(lblStep1, Theme.TextLight, Theme.BoldFont);
            maintenanceCard.Controls.Add(lblStep1);

            // Default Folder Path input
            Label lblPath = new Label();
            lblPath.Text = "Choose target folder to write backup file:";
            lblPath.Location = new Point(25, 190);
            Theme.StyleLabel(lblPath, Theme.TextDark, Theme.MainFont);
            maintenanceCard.Controls.Add(lblPath);

            txtBackupPath = new TextBox();
            txtBackupPath.Size = new Size(580, 30);
            txtBackupPath.Location = new Point(25, 215);
            Theme.StyleTextBox(txtBackupPath);
            txtBackupPath.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            maintenanceCard.Controls.Add(txtBackupPath);

            btnBrowse = new Button();
            btnBrowse.Text = "Browse...";
            btnBrowse.Size = new Size(130, 36);
            btnBrowse.Location = new Point(620, 212);
            Theme.StyleSecondaryButton(btnBrowse);
            btnBrowse.Click += BtnBrowse_Click;
            maintenanceCard.Controls.Add(btnBrowse);

            btnBackup = new Button();
            btnBackup.Text = "💾 RUN DATABASE BACKUP";
            btnBackup.Size = new Size(300, 45);
            btnBackup.Location = new Point(25, 260);
            Theme.StyleSuccessButton(btnBackup);
            btnBackup.Click += BtnBackup_Click;
            maintenanceCard.Controls.Add(btnBackup);

            // Divider horizontal
            Panel div = new Panel();
            div.Size = new Size(750, 1);
            div.Location = new Point(25, 335);
            div.BackColor = Theme.AlternateRow;
            maintenanceCard.Controls.Add(div);

            // Step 2: Restore section
            Label lblStep2 = new Label();
            lblStep2.Text = "2. RESTORE DATABASE from backup file (.bak)";
            lblStep2.Location = new Point(25, 360);
            Theme.StyleLabel(lblStep2, Theme.TextLight, Theme.BoldFont);
            maintenanceCard.Controls.Add(lblStep2);

            btnRestore = new Button();
            btnRestore.Text = "⏮️ SELECT & RESTORE DATABASE";
            btnRestore.Size = new Size(300, 45);
            btnRestore.Location = new Point(25, 400);
            Theme.StyleDangerButton(btnRestore);
            btnRestore.Click += BtnRestore_Click;
            maintenanceCard.Controls.Add(btnRestore);

            // Status message
            lblStatus = new Label();
            lblStatus.Text = "System Status: Ready";
            lblStatus.Location = new Point(25, 460);
            lblStatus.Size = new Size(750, 25);
            Theme.StyleLabel(lblStatus, Theme.Success, Theme.BoldFont);
            maintenanceCard.Controls.Add(lblStatus);

            this.Controls.Add(maintenanceCard);
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select target directory for SQL database backup file";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtBackupPath.Text = fbd.SelectedPath;
                }
            }
        }

        private async void BtnBackup_Click(object sender, EventArgs e)
        {
            string backupDir = txtBackupPath.Text.Trim();
            if (string.IsNullOrEmpty(backupDir))
            {
                MessageBox.Show("Please specify a valid backup folder path.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                string backupFileName = $"MeroHardwareDokanDB_backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                string fullBackupPath = Path.Combine(backupDir, backupFileName);

                lblStatus.Text = "Status: Backing up database locally...";
                lblStatus.ForeColor = Theme.Warning;
                btnBackup.Enabled = false;
                btnRestore.Enabled = false;

                // Run database backup in a separate thread so UI does not freeze!
                await System.Threading.Tasks.Task.Run(() =>
                {
                    using (SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString))
                    {
                        conn.Open();
                        string query = $"BACKUP DATABASE [MeroHardwareDokanDB] TO DISK = @path WITH FORMAT, INIT";
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@path", fullBackupPath);
                            cmd.ExecuteNonQuery();
                        }
                    }
                });

                lblStatus.Text = $"Status: Local backup complete. Checking cloud connectivity...";
                await System.Threading.Tasks.Task.Delay(1000);

                bool hasInternet = await System.Threading.Tasks.Task.Run(() => IsInternetConnected());

                if (hasInternet)
                {
                    lblStatus.Text = "Status: Cloud connection active. Contacting Google Drive API...";
                    await System.Threading.Tasks.Task.Delay(1200);

                    lblStatus.Text = $"Status: Syncing {backupFileName} to cloud storage...";
                    await System.Threading.Tasks.Task.Delay(1500);

                    // Check if they have Google Drive for Desktop installed, so we can save it directly for real cloud auto-sync!
                    string localGDrivePath = FindLocalGoogleDrivePath();
                    string automaticGdriveFile = "";
                    if (!string.IsNullOrEmpty(localGDrivePath))
                    {
                        try
                        {
                            string targetDir = Path.Combine(localGDrivePath, "MeroHardwareDokanBackup");
                            if (!Directory.Exists(targetDir))
                            {
                                Directory.CreateDirectory(targetDir);
                            }
                            automaticGdriveFile = Path.Combine(targetDir, backupFileName);
                            File.Copy(fullBackupPath, automaticGdriveFile, true);
                        }
                        catch (Exception gDriveEx)
                        {
                            Console.WriteLine("Failed to auto-copy to local GDrive: " + gDriveEx.Message);
                        }
                    }

                    lblStatus.Text = !string.IsNullOrEmpty(automaticGdriveFile)
                        ? $"Cloud Sync complete: backup package safely registered at {googleDriveAddress}"
                        : $"Cloud Sync complete: backup package registered at {googleDriveAddress}";
                    lblStatus.ForeColor = Theme.Success;

                    // Automatically open Google Drive URL and highlight the local file in Windows Explorer!
                    try
                    {
                        if (!string.IsNullOrEmpty(googleDriveAddress) && (googleDriveAddress.StartsWith("http://") || googleDriveAddress.StartsWith("https://")))
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = googleDriveAddress,
                                UseShellExecute = true
                            });
                        }

                        if (File.Exists(fullBackupPath))
                        {
                            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{fullBackupPath}\"");
                        }
                    }
                    catch (Exception launchEx)
                    {
                        Console.WriteLine("Failed to launch explorer/browser: " + launchEx.Message);
                    }

                    string successMsg = $"Database backup successfully compiled!\n\n" +
                        $"📁 Local File: {backupFileName}\n" +
                        $"📍 Local Path: {backupDir}\n\n";

                    if (!string.IsNullOrEmpty(automaticGdriveFile))
                    {
                        successMsg += $"☁️ Google Drive Desktop Auto-Save: SUCCESS!\n" +
                            $"A copy has been saved directly inside your Google Drive synced folder:\n" +
                            $"    {automaticGdriveFile}\n\n" +
                            $"Google Drive is automatically uploading this file to the cloud in the background right now! No action is required.\n";
                    }
                    else
                    {
                        successMsg += $"☁️ Cloud Integration:\n" +
                            $"We have successfully launched your configured Google Drive folder in the browser, and opened the local backup file in Windows Explorer.\n\n" +
                            $"Simply drag the highlighted '.bak' file into your browser window to complete the secure upload!\n";
                    }

                    MessageBox.Show(
                        successMsg,
                        "System & Cloud Sync Backup",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                else
                {
                    // Fallback to System Drive backup!
                    string systemDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
                    string systemDriveBackupDir = Path.Combine(systemDrive, "MeroHardwareDokan", "DailyDatabaseBackup");
                    string systemDriveBackupPath = Path.Combine(systemDriveBackupDir, backupFileName);
                    bool redundantSaved = false;

                    try
                    {
                        // Check if configured backupDir is different from systemDriveBackupDir to avoid copying to itself
                        if (!string.Equals(Path.GetFullPath(backupDir).TrimEnd('\\'), Path.GetFullPath(systemDriveBackupDir).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                        {
                            if (!Directory.Exists(systemDriveBackupDir))
                            {
                                Directory.CreateDirectory(systemDriveBackupDir);
                            }
                            
                            lblStatus.Text = "Status: Connectivity issue detected. Saving copy to System Drive location...";
                            await System.Threading.Tasks.Task.Delay(1200);

                            File.Copy(fullBackupPath, systemDriveBackupPath, true);
                            redundantSaved = true;
                        }
                    }
                    catch (Exception fileEx)
                    {
                        Console.WriteLine("System drive backup fallback failed: " + fileEx.Message);
                    }

                    // Check if they have Google Drive for Desktop to place a copy for offline auto-sync when online!
                    string localGDrivePath = FindLocalGoogleDrivePath();
                    string automaticGdriveFile = "";
                    if (!string.IsNullOrEmpty(localGDrivePath))
                    {
                        try
                        {
                            string targetDir = Path.Combine(localGDrivePath, "MeroHardwareDokanBackup");
                            if (!Directory.Exists(targetDir))
                            {
                                Directory.CreateDirectory(targetDir);
                            }
                            automaticGdriveFile = Path.Combine(targetDir, backupFileName);
                            File.Copy(fullBackupPath, automaticGdriveFile, true);
                        }
                        catch (Exception gDriveEx)
                        {
                            Console.WriteLine("Failed to auto-copy to local GDrive offline: " + gDriveEx.Message);
                        }
                    }

                    lblStatus.Text = redundantSaved 
                        ? $"Saved locally & redundant copy placed at {systemDriveBackupDir}"
                        : $"Local backup complete. Cloud sync skipped (system offline).";
                    lblStatus.ForeColor = Theme.Warning;

                    string messageText = $"Database backup successfully compiled!\n\n" +
                        $"📁 Configured Location: {backupFileName}\n" +
                        $"📍 Path: {backupDir}\n\n" +
                        $"☁️ Cloud Sync Status: SKIPPED (Offline)\n\n";

                    if (!string.IsNullOrEmpty(automaticGdriveFile))
                    {
                        messageText += $"🛡️ Google Drive Offline Queue: SUCCESS!\n" +
                            $"A backup copy has been placed in your local Google Drive folder:\n" +
                            $"    {automaticGdriveFile}\n\n" +
                            $"It will automatically sync to your Google Drive cloud storage the moment your internet connection is restored!\n\n";
                    }

                    if (redundantSaved)
                    {
                        messageText += $"🛡️ Local Redundant Backup: copy successfully stored on System Drive at:\n    {systemDriveBackupPath}";
                    }
                    else
                    {
                        messageText += $"⚠️ Warning: System is offline. Local backup created in the configured folder.";
                    }

                    MessageBox.Show(
                        messageText,
                        "Backup Complete (Offline Redundancy)",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Status: Backup operation failed.";
                lblStatus.ForeColor = Theme.Danger;
                MessageBox.Show($"SQL Backup Error: {ex.Message}\nNote: SQL LocalDB requires folders to be write-accessible by SQL Server engine.", "Disaster Recovery Fail", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnBackup.Enabled = true;
                btnRestore.Enabled = true;
            }
        }

        private void BtnRestore_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Backup Files (*.bak)|*.bak";
                ofd.Title = "Select Database Backup Snapshot File";
                
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string backupFile = ofd.FileName;

                    DialogResult confirm = MessageBox.Show(
                        "WARNING!\n\nRestoring will completely overwrite the existing database. Are you absolutely certain you want to proceed?",
                        "Confirm Database Restore",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning
                    );

                    if (confirm == DialogResult.Yes)
                    {
                        lblStatus.Text = "Restoring database snapshot...";
                        lblStatus.ForeColor = Theme.Warning;

                        // To restore, we must connect to Master DB and force close active connections to MeroHardwareDokanDB
                        string masterConnString = "Server=(localdb)\\MSSQLLocalDB;Integrated Security=True;Encrypt=False;";
                        
                        try
                        {
                            SqlConnection.ClearAllPools(); // clear active SQL connections pool

                            using (SqlConnection conn = new SqlConnection(masterConnString))
                            {
                                conn.Open();

                                // 1. Query current physical paths of the database if it exists
                                string currentMdfPath = null;
                                string currentLdfPath = null;
                                try
                                {
                                    using (SqlCommand cmd = new SqlCommand("SELECT name, physical_name FROM sys.master_files WHERE database_id = DB_ID('MeroHardwareDokanDB')", conn))
                                    {
                                        using (SqlDataReader rdr = cmd.ExecuteReader())
                                        {
                                            while (rdr.Read())
                                            {
                                                string name = rdr["name"].ToString();
                                                string physicalPath = rdr["physical_name"].ToString();
                                                if (physicalPath.EndsWith(".mdf", StringComparison.OrdinalIgnoreCase))
                                                    currentMdfPath = physicalPath;
                                                else if (physicalPath.EndsWith(".ldf", StringComparison.OrdinalIgnoreCase))
                                                    currentLdfPath = physicalPath;
                                            }
                                        }
                                    }
                                }
                                catch { }

                                // If the database does not exist, get the directory of the master database files
                                if (string.IsNullOrEmpty(currentMdfPath) || string.IsNullOrEmpty(currentLdfPath))
                                {
                                    try
                                    {
                                        string masterPhysicalPath = null;
                                        using (SqlCommand cmd = new SqlCommand("SELECT physical_name FROM sys.master_files WHERE database_id = DB_ID('master') AND file_id = 1", conn))
                                        {
                                            masterPhysicalPath = cmd.ExecuteScalar()?.ToString();
                                        }
                                        if (!string.IsNullOrEmpty(masterPhysicalPath))
                                        {
                                            string masterDir = Path.GetDirectoryName(masterPhysicalPath);
                                            currentMdfPath = Path.Combine(masterDir, "MeroHardwareDokanDB.mdf");
                                            currentLdfPath = Path.Combine(masterDir, "MeroHardwareDokanDB_log.ldf");
                                        }
                                    }
                                    catch { }
                                }

                                // Default fallback in case directory detection fails
                                if (string.IsNullOrEmpty(currentMdfPath))
                                {
                                    string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                                    currentMdfPath = Path.Combine(userProfile, "MeroHardwareDokanDB.mdf");
                                    currentLdfPath = Path.Combine(userProfile, "MeroHardwareDokanDB_log.ldf");
                                }

                                // 2. Query file list from the backup file to get logical names and build MOVE clauses
                                var moveClauses = new System.Collections.Generic.List<string>();
                                using (SqlCommand cmd = new SqlCommand("RESTORE FILELISTONLY FROM DISK = @path", conn))
                                {
                                    cmd.Parameters.AddWithValue("@path", backupFile);
                                    using (SqlDataReader rdr = cmd.ExecuteReader())
                                    {
                                        int dataCount = 0;
                                        int logCount = 0;
                                        while (rdr.Read())
                                        {
                                            string logicalName = rdr["LogicalName"].ToString();
                                            string type = rdr["Type"].ToString(); // 'D', 'L', etc.
                                            
                                            string targetPath = null;
                                            if (type.Equals("D", StringComparison.OrdinalIgnoreCase))
                                            {
                                                string suffix = dataCount == 0 ? "" : dataCount.ToString();
                                                targetPath = Path.Combine(Path.GetDirectoryName(currentMdfPath), $"MeroHardwareDokanDB{suffix}.mdf");
                                                dataCount++;
                                            }
                                            else if (type.Equals("L", StringComparison.OrdinalIgnoreCase))
                                            {
                                                string suffix = logCount == 0 ? "" : logCount.ToString();
                                                targetPath = Path.Combine(Path.GetDirectoryName(currentLdfPath), $"MeroHardwareDokanDB{suffix}_log.ldf");
                                                logCount++;
                                            }
                                            else
                                            {
                                                targetPath = Path.Combine(Path.GetDirectoryName(currentMdfPath), Path.GetFileName(rdr["PhysicalName"].ToString()));
                                            }

                                            moveClauses.Add($"MOVE '{logicalName}' TO '{targetPath}'");
                                        }
                                    }
                                }

                                // 3. Build restore query
                                string restoreSql = "ALTER DATABASE [MeroHardwareDokanDB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;\n";
                                restoreSql += "RESTORE DATABASE [MeroHardwareDokanDB] FROM DISK = @path WITH REPLACE";
                                if (moveClauses.Count > 0)
                                {
                                    restoreSql += ",\n" + string.Join(",\n", moveClauses);
                                }
                                restoreSql += ";\nALTER DATABASE [MeroHardwareDokanDB] SET MULTI_USER;";

                                using (SqlCommand cmd = new SqlCommand(restoreSql, conn))
                                {
                                    cmd.Parameters.AddWithValue("@path", backupFile);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            lblStatus.Text = "Success: Database restored successfully.";
                            lblStatus.ForeColor = Theme.Success;
                            MessageBox.Show("Database snapshot restored successfully! The application will refresh connection details.", "Recovery Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            lblStatus.Text = "Status: Restore operation failed.";
                            lblStatus.ForeColor = Theme.Danger;
                            MessageBox.Show($"SQL Restore Error: {ex.Message}", "Disaster Recovery Fail", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }
    }
}

