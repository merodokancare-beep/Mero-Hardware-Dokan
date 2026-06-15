using System;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace MeroHardwareDokan
{
    public static class DatabaseHelper
    {
        public class DbConfig
        {
            public string Server { get; set; } = "(localdb)\\MSSQLLocalDB";
            public string Database { get; set; } = "MeroHardwareDokanDB";
            public bool IntegratedSecurity { get; set; } = true;
            public string Username { get; set; } = "";
            public string Password { get; set; } = "";
            public int ConnectionTimeout { get; set; } = 30;
            public int ConnectRetryCount { get; set; } = 3;
            public int ConnectRetryInterval { get; set; } = 10;
            public bool TrustServerCertificate { get; set; } = true;
            public bool Encrypt { get; set; } = false;
        }

        private static DbConfig _cachedConfig = null;
        private static string _cachedLocalDbServer = null;
        private static string _cachedLocalDbPipe = null;
        private static DateTime _lastResolvedTime = DateTime.MinValue;

        private static DbConfig GetCachedConfig()
        {
            if (_cachedConfig == null)
            {
                _cachedConfig = LoadConfig();
            }
            return _cachedConfig;
        }

        public static string ConnectionString
        {
            get
            {
                return BuildConnectionString(GetCachedConfig());
            }
            set
            {
            }
        }

        public static string MasterConnectionString
        {
            get
            {
                try
                {
                    var builder = new SqlConnectionStringBuilder(ConnectionString);
                    builder.InitialCatalog = "master";
                    return builder.ConnectionString;
                }
                catch
                {
                    return "Server=(localdb)\\MSSQLLocalDB;Integrated Security=True;Encrypt=False;";
                }
            }
        }

        public static string GetConfigFilePath()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string localFile = Path.Combine(appDir, "dbconfig.json");
            try
            {
                // Test write permissions
                string testFile = Path.Combine(appDir, "test_write.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return localFile;
            }
            catch
            {
                // Fallback to LocalApplicationData
                string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MeroHardwareDokan");
                if (!Directory.Exists(appDataDir))
                {
                    Directory.CreateDirectory(appDataDir);
                }
                return Path.Combine(appDataDir, "dbconfig.json");
            }
        }

        public static DbConfig LoadConfig()
        {
            string path = GetConfigFilePath();
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var config = JsonSerializer.Deserialize<DbConfig>(json);
                    if (config != null)
                    {
                        return config;
                    }
                }
                catch { }
            }

            // Return default config and save it
            var defaultConfig = new DbConfig();
            SaveConfig(defaultConfig);
            return defaultConfig;
        }

        public static void SaveConfig(DbConfig config)
        {
            try
            {
                string path = GetConfigFilePath();
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(path, json);
                
                _cachedConfig = config;
                
                // Clear the connection pool when saving new configuration to discard stale connections
                SqlConnection.ClearAllPools();
            }
            catch { }
        }

        private static void LoadConnectionString()
        {
            GetCachedConfig();
        }

        public static string BuildConnectionString(DbConfig config)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder();
                builder.DataSource = ResolveLocalDbServerName(config.Server);
                builder.InitialCatalog = config.Database;
                builder.IntegratedSecurity = config.IntegratedSecurity;
                if (!config.IntegratedSecurity)
                {
                    builder.UserID = config.Username;
                    builder.Password = config.Password;
                }
                builder.ConnectTimeout = config.ConnectionTimeout;
                builder.ConnectRetryCount = config.ConnectRetryCount;
                builder.ConnectRetryInterval = config.ConnectRetryInterval;
                builder.TrustServerCertificate = config.TrustServerCertificate;
                builder.Encrypt = config.Encrypt;
                return builder.ConnectionString;
            }
            catch
            {
                return "Server=(localdb)\\MSSQLLocalDB;Database=MeroHardwareDokanDB;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;";
            }
        }

        public static void InitializeDatabase()
        {
            // 1. Create Database if it doesn't exist
            using (SqlConnection masterConn = new SqlConnection(MasterConnectionString))
            {
                masterConn.Open();
                bool dbExists = false;
                using (SqlCommand cmd = new SqlCommand("SELECT database_id FROM sys.databases WHERE name = 'MeroHardwareDokanDB'", masterConn))
                {
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        dbExists = true;
                    }
                }

                if (!dbExists)
                {
                    using (SqlCommand cmd = new SqlCommand("CREATE DATABASE MeroHardwareDokanDB", masterConn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            // 2. Create Tables inside MeroHardwareDokanDB
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                conn.Open();

                // Users Table
                ExecuteNonQuery(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                    BEGIN
                        CREATE TABLE Users (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            Username NVARCHAR(50) NOT NULL UNIQUE,
                            PasswordHash NVARCHAR(255) NOT NULL,
                            FullName NVARCHAR(100) NOT NULL,
                            Role NVARCHAR(20) NOT NULL,
                            CreatedAt DATETIME DEFAULT GETDATE()
                        )
                    END", conn);

                // Customers Table
                ExecuteNonQuery(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Customers')
                    BEGIN
                        CREATE TABLE Customers (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            Name NVARCHAR(100) NOT NULL,
                            Phone NVARCHAR(20) NULL,
                            Email NVARCHAR(100) NULL,
                            Address NVARCHAR(200) NULL,
                            CreatedAt DATETIME DEFAULT GETDATE()
                        )
                    END", conn);

                // Suppliers Table
                ExecuteNonQuery(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Suppliers')
                    BEGIN
                        CREATE TABLE Suppliers (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            Name NVARCHAR(100) NOT NULL,
                            ContactPerson NVARCHAR(100) NULL,
                            Phone NVARCHAR(20) NULL,
                            Email NVARCHAR(100) NULL,
                            Address NVARCHAR(200) NULL,
                            CreatedAt DATETIME DEFAULT GETDATE()
                        )
                    END", conn);

                // Categories Table
                ExecuteNonQuery(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Categories')
                    BEGIN
                        CREATE TABLE Categories (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            Name NVARCHAR(100) UNIQUE NOT NULL
                        )
                    END", conn);

                // TaxSlabs Table
                ExecuteNonQuery(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TaxSlabs')
                    BEGIN
                        CREATE TABLE TaxSlabs (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            Name NVARCHAR(50) UNIQUE NOT NULL,
                            TaxPercent DECIMAL(5,2) NOT NULL DEFAULT 0.00
                        )
                    END", conn);

                // Products Table
                ExecuteNonQuery(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
                    BEGIN
                        CREATE TABLE Products (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            Code NVARCHAR(50) NOT NULL UNIQUE,
                            Name NVARCHAR(150) NOT NULL,
                            Description NVARCHAR(500) NULL,
                            Category NVARCHAR(100) NULL,
                            Unit NVARCHAR(50) NOT NULL DEFAULT 'Pcs',
                            PurchasePrice DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                            SalesPrice DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                            Stock DECIMAL(18,3) NOT NULL DEFAULT 0.000,
                            MinStockLevel DECIMAL(18,3) NOT NULL DEFAULT 5.000,
                            TaxPercent DECIMAL(5,2) NOT NULL DEFAULT 13.00,
                            CreatedAt DATETIME DEFAULT GETDATE()
                        )
                    END
                    ELSE
                    BEGIN
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'TaxPercent')
                        BEGIN
                            ALTER TABLE Products ADD TaxPercent DECIMAL(5,2) NOT NULL DEFAULT 13.00;
                        END
                        IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Products' AND COLUMN_NAME = 'Unit')
                        BEGIN
                            ALTER TABLE Products ADD Unit NVARCHAR(50) NOT NULL DEFAULT 'Pcs';
                        END
                        IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Products' AND COLUMN_NAME = 'Stock' AND DATA_TYPE = 'int')
                        BEGIN
                            ALTER TABLE Products ALTER COLUMN Stock DECIMAL(18,3) NOT NULL;
                        END
                        IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Products' AND COLUMN_NAME = 'MinStockLevel' AND DATA_TYPE = 'int')
                        BEGIN
                            ALTER TABLE Products ALTER COLUMN MinStockLevel DECIMAL(18,3) NOT NULL;
                        END
                    END", conn);

                // Purchases Table
                ExecuteNonQuery(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Purchases')
                    BEGIN
                        CREATE TABLE Purchases (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            PurchaseNumber NVARCHAR(50) NOT NULL UNIQUE,
                            SupplierId INT NULL FOREIGN KEY REFERENCES Suppliers(Id) ON DELETE SET NULL,
                            PurchaseDate DATETIME NOT NULL DEFAULT GETDATE(),
                            TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                            CreatedBy INT NULL FOREIGN KEY REFERENCES Users(Id)
                        )
                    END", conn);

                // PurchaseDetails Table
                ExecuteNonQuery(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PurchaseDetails')
                    BEGIN
                        CREATE TABLE PurchaseDetails (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            PurchaseId INT FOREIGN KEY REFERENCES Purchases(Id) ON DELETE CASCADE,
                            ProductId INT FOREIGN KEY REFERENCES Products(Id) ON DELETE CASCADE,
                            Quantity DECIMAL(18,3) NOT NULL,
                            PurchasePrice DECIMAL(18,2) NOT NULL
                        )
                    END
                    ELSE
                    BEGIN
                        IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PurchaseDetails' AND COLUMN_NAME = 'Quantity' AND DATA_TYPE = 'int')
                        BEGIN
                            ALTER TABLE PurchaseDetails ALTER COLUMN Quantity DECIMAL(18,3) NOT NULL;
                        END
                    END", conn);

                // Sales Table
                ExecuteNonQuery(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Sales')
                    BEGIN
                        CREATE TABLE Sales (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            InvoiceNumber NVARCHAR(50) NOT NULL UNIQUE,
                            CustomerId INT NULL FOREIGN KEY REFERENCES Customers(Id) ON DELETE SET NULL,
                            SaleDate DATETIME NOT NULL DEFAULT GETDATE(),
                            SubTotal DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                            Discount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                            Tax DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                            GrandTotal DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                            PaymentMethod NVARCHAR(50) NOT NULL DEFAULT 'Cash',
                            CreatedBy INT NULL FOREIGN KEY REFERENCES Users(Id),
                            IsTaxInclusive BIT NOT NULL DEFAULT 0,
                            TaxType NVARCHAR(20) NOT NULL DEFAULT 'Local',
                            PaidAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                            DueAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00
                        )
                    END
                    ELSE
                    BEGIN
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'IsTaxInclusive')
                        BEGIN
                            ALTER TABLE Sales ADD IsTaxInclusive BIT NOT NULL DEFAULT 0;
                        END
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'TaxType')
                        BEGIN
                            ALTER TABLE Sales ADD TaxType NVARCHAR(20) NOT NULL DEFAULT 'Local';
                        END
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'PaidAmount')
                        BEGIN
                            ALTER TABLE Sales ADD PaidAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                        END
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'DueAmount')
                        BEGIN
                            ALTER TABLE Sales ADD DueAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                            -- Backfill existing sales as fully paid
                            EXEC('UPDATE Sales SET PaidAmount = GrandTotal, DueAmount = 0.00');
                        END
                    END", conn);

                // SaleDetails Table
                ExecuteNonQuery(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SaleDetails')
                    BEGIN
                        CREATE TABLE SaleDetails (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            SaleId INT FOREIGN KEY REFERENCES Sales(Id) ON DELETE CASCADE,
                            ProductId INT FOREIGN KEY REFERENCES Products(Id) ON DELETE CASCADE,
                            Quantity DECIMAL(18,3) NOT NULL,
                            UnitPrice DECIMAL(18,2) NOT NULL,
                            Total DECIMAL(18,2) NOT NULL,
                            PurchaseCostAtSale DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                            TaxPercent DECIMAL(5,2) NOT NULL DEFAULT 0.00,
                            TaxAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00
                        )
                    END
                    ELSE
                    BEGIN
                        IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SaleDetails' AND COLUMN_NAME = 'Quantity' AND DATA_TYPE = 'int')
                        BEGIN
                            ALTER TABLE SaleDetails ALTER COLUMN Quantity DECIMAL(18,3) NOT NULL;
                        END
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleDetails') AND name = 'PurchaseCostAtSale')
                        BEGIN
                            ALTER TABLE SaleDetails ADD PurchaseCostAtSale DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                            
                            -- Backfill existing rows with current products' purchase price
                            EXEC('UPDATE sd
                                  SET sd.PurchaseCostAtSale = p.PurchasePrice
                                  FROM SaleDetails sd
                                  INNER JOIN Products p ON sd.ProductId = p.Id');
                        END
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleDetails') AND name = 'TaxPercent')
                        BEGIN
                            ALTER TABLE SaleDetails ADD TaxPercent DECIMAL(5,2) NOT NULL DEFAULT 0.00;
                        END
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SaleDetails') AND name = 'TaxAmount')
                        BEGIN
                            ALTER TABLE SaleDetails ADD TaxAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00;
                        END
                    END", conn);

                // CustomerPayments Table
                ExecuteNonQuery(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CustomerPayments')
                    BEGIN
                        CREATE TABLE CustomerPayments (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(Id) ON DELETE CASCADE,
                            SaleId INT NOT NULL FOREIGN KEY REFERENCES Sales(Id) ON DELETE CASCADE,
                            PaymentDate DATETIME NOT NULL DEFAULT GETDATE(),
                            AmountPaid DECIMAL(18,2) NOT NULL,
                            PaymentMethod NVARCHAR(50) NOT NULL DEFAULT 'Cash',
                            Remarks NVARCHAR(200) NULL,
                            CreatedBy INT NULL FOREIGN KEY REFERENCES Users(Id)
                        )
                    END", conn);

                // ProductPriceHistory Table
                ExecuteNonQuery(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductPriceHistory')
                    BEGIN
                        CREATE TABLE ProductPriceHistory (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            ProductId INT NOT NULL FOREIGN KEY REFERENCES Products(Id) ON DELETE CASCADE,
                            OldPurchasePrice DECIMAL(18,2) NOT NULL,
                            NewPurchasePrice DECIMAL(18,2) NOT NULL,
                            OldSalesPrice DECIMAL(18,2) NOT NULL,
                            NewSalesPrice DECIMAL(18,2) NOT NULL,
                            ChangeDate DATETIME NOT NULL DEFAULT GETDATE(),
                            ChangedBy INT NULL FOREIGN KEY REFERENCES Users(Id),
                            Source NVARCHAR(100) NOT NULL
                        )
                    END", conn);

                // SalesReturns Table
                ExecuteNonQuery(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SalesReturns')
                    BEGIN
                        CREATE TABLE SalesReturns (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            ReturnNumber NVARCHAR(50) UNIQUE NOT NULL,
                            SaleId INT NOT NULL FOREIGN KEY REFERENCES Sales(Id) ON DELETE CASCADE,
                            ReturnDate DATETIME NOT NULL DEFAULT GETDATE(),
                            TotalRefund DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                            CreatedBy INT NULL FOREIGN KEY REFERENCES Users(Id)
                        )
                    END", conn);

                // SalesReturnDetails Table
                ExecuteNonQuery(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SalesReturnDetails')
                    BEGIN
                        CREATE TABLE SalesReturnDetails (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            ReturnId INT NOT NULL FOREIGN KEY REFERENCES SalesReturns(Id) ON DELETE CASCADE,
                            ProductId INT NOT NULL FOREIGN KEY REFERENCES Products(Id),
                            Quantity DECIMAL(18,3) NOT NULL,
                            RefundPrice DECIMAL(18,2) NOT NULL,
                            Total DECIMAL(18,2) NOT NULL,
                            ItemCondition NVARCHAR(50) NOT NULL
                        )
                    END
                    ELSE
                    BEGIN
                        IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SalesReturnDetails' AND COLUMN_NAME = 'Quantity' AND DATA_TYPE = 'int')
                        BEGIN
                            ALTER TABLE SalesReturnDetails ALTER COLUMN Quantity DECIMAL(18,3) NOT NULL;
                        END
                    END", conn);

                // DailySettlements Table
                ExecuteNonQuery(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DailySettlements')
                    BEGIN
                        CREATE TABLE DailySettlements (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            SettlementDate DATETIME NOT NULL DEFAULT GETDATE(),
                            OpeningCash DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                            CashSales DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                            DueCollections DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                            CardQRSales DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                            DuesCreated DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                            ExpectedCash DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                            ActualCash DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                            Variance DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                            SettlementBy INT NULL FOREIGN KEY REFERENCES Users(Id),
                            Remarks NVARCHAR(500) NULL,
                            Refunds DECIMAL(18,2) NOT NULL DEFAULT 0.00
                        )
                    END", conn);

                // AppProfile Configuration Table
                ExecuteNonQuery(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AppProfile')
                    BEGIN
                        CREATE TABLE AppProfile (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            OwnerName NVARCHAR(100) NOT NULL DEFAULT 'Shop Owner',
                            ShopName NVARCHAR(150) NOT NULL DEFAULT 'Mero Dokan Shop',
                            Phone NVARCHAR(50) NOT NULL DEFAULT '+977-1-4200000',
                            Email NVARCHAR(100) NOT NULL DEFAULT 'contact@MeroHardwareDokan.com',
                            Address NVARCHAR(200) NOT NULL DEFAULT 'Kathmandu, Nepal',
                            LogoPath NVARCHAR(500) NULL,
                            ProfilePicPath NVARCHAR(500) NULL,
                            ThemePreset NVARCHAR(50) NOT NULL DEFAULT 'Dark Slate',
                            FontSizePreset NVARCHAR(50) NOT NULL DEFAULT 'Medium',
                            BackupFolderPath NVARCHAR(500) NOT NULL DEFAULT 'D:\MeroHardwareDokan\DailyDatabaseBackup',
                            GoogleDriveAddress NVARCHAR(500) NOT NULL DEFAULT 'https://drive.google.com/drive/folders/MeroHardwareDokanBackup',
                            GSTIN NVARCHAR(50) NULL
                        )
                    END
                    ELSE
                    BEGIN
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'FontSizePreset')
                        BEGIN
                            ALTER TABLE AppProfile ADD FontSizePreset NVARCHAR(50) NOT NULL DEFAULT 'Medium';
                        END
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'BackupFolderPath')
                        BEGIN
                            ALTER TABLE AppProfile ADD BackupFolderPath NVARCHAR(500) NOT NULL DEFAULT 'D:\MeroHardwareDokan\DailyDatabaseBackup';
                        END
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'GoogleDriveAddress')
                        BEGIN
                            ALTER TABLE AppProfile ADD GoogleDriveAddress NVARCHAR(500) NOT NULL DEFAULT 'https://drive.google.com/drive/folders/MeroHardwareDokanBackup';
                        END
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppProfile') AND name = 'GSTIN')
                        BEGIN
                            ALTER TABLE AppProfile ADD GSTIN NVARCHAR(50) NULL;
                        END
                    END", conn);

                // 3. Seed Default Admin User if none exists
                int userCount = 0;
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Users", conn))
                {
                    userCount = (int)cmd.ExecuteScalar();
                }

                if (userCount == 0)
                {
                    string adminPassHash = HashPassword("admin");
                    using (SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO Users (Username, PasswordHash, FullName, Role) 
                        VALUES (@username, @password, @fullname, @role)", conn))
                    {
                        cmd.Parameters.AddWithValue("@username", "admin");
                        cmd.Parameters.AddWithValue("@password", adminPassHash);
                        cmd.Parameters.AddWithValue("@fullname", "System Administrator");
                        cmd.Parameters.AddWithValue("@role", "Admin");
                        cmd.ExecuteNonQuery();
                    }

                    // Seed some default customers and suppliers for presentation
                    using (SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO Customers (Name, Phone, Email, Address) VALUES 
                        ('Walk-in Customer', '0000000000', 'walkin@MeroHardwareDokan.com', 'Local'),
                        ('Hari Prasad', '9841234567', 'hari@gmail.com', 'Kathmandu'),
                        ('Sita Kumari', '9851234567', 'sita@yahoo.com', 'Lalitpur');
                        
                        INSERT INTO Suppliers (Name, ContactPerson, Phone, Email, Address) VALUES 
                        ('KTM Distributors', 'Ramesh Sen', '9801122334', 'ktmdist@gmail.com', 'New Road, Kathmandu'),
                        ('National Wholesalers', 'Binod Chaudhary', '9812233445', 'national@wholesaler.com', 'Birgunj');", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                // 4. Seed Default Categories if none exist
                int categoryCount = 0;
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Categories", conn))
                {
                    categoryCount = (int)cmd.ExecuteScalar();
                }

                if (categoryCount == 0)
                {
                    using (SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO Categories (Name) VALUES 
                        ('Groceries'),
                        ('Beverages'),
                        ('Snacks'),
                        ('Electronics'),
                        ('Clothing'),
                        ('Cosmetics'),
                        ('Others')", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                // 5. Seed Default AppProfile if none exists
                int profileCount = 0;
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM AppProfile", conn))
                {
                    profileCount = (int)cmd.ExecuteScalar();
                }

                if (profileCount == 0)
                {
                    using (SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO AppProfile (OwnerName, ShopName, Phone, Email, Address, ThemePreset, BackupFolderPath, GoogleDriveAddress) 
                        VALUES ('Shop Owner', 'Mero Dokan Shop', '+977-1-4200000', 'contact@MeroHardwareDokan.com', 'Kathmandu, Nepal', 'Dark', 'D:\MeroHardwareDokan\DailyDatabaseBackup', 'https://drive.google.com/drive/folders/MeroHardwareDokanBackup')", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                // 6. Seed Default Tax Slabs if none exist
                int taxSlabsCount = 0;
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM TaxSlabs", conn))
                {
                    taxSlabsCount = (int)cmd.ExecuteScalar();
                }

                if (taxSlabsCount == 0)
                {
                    using (SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO TaxSlabs (Name, TaxPercent) VALUES 
                        ('GST 0% (Exempt)', 0.00),
                        ('GST 5%', 5.00),
                        ('GST 12%', 12.00),
                        ('GST 18%', 18.00),
                        ('GST 28%', 28.00),
                        ('VAT 13% (Standard)', 13.00)", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private static void ExecuteNonQuery(string sql, SqlConnection conn)
        {
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public static string HashPassword(string password)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private static string FindSqlLocalDBPath()
        {
            try
            {
                var info = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "sqllocaldb",
                    Arguments = "-v",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                using (var proc = System.Diagnostics.Process.Start(info))
                {
                    proc.WaitForExit(1000);
                    return "sqllocaldb";
                }
            }
            catch { }

            var searchFolders = new System.Collections.Generic.List<string>();
            string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            
            string[] versions = { "160", "150", "140", "130", "120", "110" };
            foreach (var ver in versions)
            {
                if (!string.IsNullOrEmpty(pf))
                {
                    searchFolders.Add(Path.Combine(pf, @"Microsoft SQL Server\" + ver + @"\Tools\Binn"));
                }
                if (!string.IsNullOrEmpty(pf86))
                {
                    searchFolders.Add(Path.Combine(pf86, @"Microsoft SQL Server\" + ver + @"\Tools\Binn"));
                }
            }

            foreach (var folder in searchFolders)
            {
                string fullPath = Path.Combine(folder, "SqlLocalDB.exe");
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return null;
        }

        private static void GetLocalDBInfo(string localDbPath, string instanceName, out string state, out string pipeName)
        {
            state = "Stopped";
            pipeName = null;
            try
            {
                var info = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = localDbPath,
                    Arguments = "info \"" + instanceName + "\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                using (var proc = System.Diagnostics.Process.Start(info))
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(2000);
                    
                    if (!string.IsNullOrEmpty(output))
                    {
                        string[] lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            string trimmed = line.Trim();
                            if (trimmed.StartsWith("State:", StringComparison.OrdinalIgnoreCase))
                            {
                                int idx = trimmed.IndexOf(':');
                                if (idx != -1)
                                {
                                    state = trimmed.Substring(idx + 1).Trim();
                                }
                            }
                            int pipeIdx = trimmed.IndexOf("np:", StringComparison.OrdinalIgnoreCase);
                            if (pipeIdx != -1)
                            {
                                pipeName = trimmed.Substring(pipeIdx).Trim();
                            }
                        }
                    }
                }
            }
            catch { }
        }

        public static string ResolveLocalDbServerName(string serverName)
        {
            if (string.IsNullOrEmpty(serverName))
                return serverName;

            if (serverName.StartsWith("(localdb)\\", StringComparison.OrdinalIgnoreCase))
            {
                if (serverName.Equals(_cachedLocalDbServer, StringComparison.OrdinalIgnoreCase) && 
                    (DateTime.UtcNow - _lastResolvedTime).TotalSeconds < 10 &&
                    !string.IsNullOrEmpty(_cachedLocalDbPipe))
                {
                    return _cachedLocalDbPipe;
                }

                string instanceName = serverName.Substring(10).Trim();
                string localDbPath = FindSqlLocalDBPath();
                if (!string.IsNullOrEmpty(localDbPath))
                {
                    string state = "Stopped";
                    string pipeName = null;

                    // 1. Get initial state
                    GetLocalDBInfo(localDbPath, instanceName, out state, out pipeName);

                    // 2. If it is stopped or starting, trigger start command and clear connection pools
                    bool wasStopped = !state.Equals("Running", StringComparison.OrdinalIgnoreCase);
                    if (wasStopped)
                    {
                        try
                        {
                            SqlConnection.ClearAllPools();
                        }
                        catch { }

                        try
                        {
                            var startInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = localDbPath,
                                Arguments = "start \"" + instanceName + "\"",
                                CreateNoWindow = true,
                                UseShellExecute = false,
                                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                            };
                            using (var proc = System.Diagnostics.Process.Start(startInfo))
                            {
                                proc.WaitForExit(3000);
                            }
                        }
                        catch { }
                    }

                    // 3. Poll until state is "Running" and pipeName is available (up to 10 seconds timeout)
                    int attempts = 0;
                    while (attempts < 20) // 20 * 500ms = 10 seconds
                    {
                        GetLocalDBInfo(localDbPath, instanceName, out state, out pipeName);
                        if (state.Equals("Running", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(pipeName))
                        {
                            break;
                        }
                        System.Threading.Thread.Sleep(500);
                        attempts++;
                    }

                    if (!string.IsNullOrEmpty(pipeName))
                    {
                        _cachedLocalDbServer = serverName;
                        _cachedLocalDbPipe = pipeName;
                        _lastResolvedTime = DateTime.UtcNow;
                        return pipeName;
                    }
                }
            }
            return serverName;
        }
    }
}

