using System;
using System.Windows.Forms;

namespace MeroHardwareDokan
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Register global exception handlers for debugging
            Application.ThreadException += (sender, e) => {
                try { System.IO.File.WriteAllText("crash_log.txt", e.Exception.ToString()); } catch { }
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => {
                try { System.IO.File.WriteAllText("crash_log.txt", e.ExceptionObject.ToString()); } catch { }
            };

            // Customizes application configurations (high DPI settings, default fonts, etc.)
            ApplicationConfiguration.Initialize();

            // Apply default theme first so the Activation UI is styled correctly
            Theme.ApplyThemePreset("Dark Slate");

            // Attempt to load the user's saved theme from database if it exists
            // Ensure database is initialized before loading themes or starting login
            bool dbInitialized = false;
            while (!dbInitialized)
            {
                try
                {
                    DatabaseHelper.InitializeDatabase();
                    dbInitialized = true;
                }
                catch (Exception ex)
                {
                    var result = MessageBox.Show(
                        $"Failed to connect and initialize database:\n{ex.Message}\n\nWould you like to configure the database connection settings?",
                        "Database Connection Error",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Error
                    );

                    if (result == DialogResult.Yes)
                    {
                        using (var configForm = new DatabaseConfigForm())
                        {
                            if (configForm.ShowDialog() != DialogResult.OK)
                            {
                                return; // Terminate app if user cancels config
                            }
                        }
                    }
                    else
                    {
                        return; // Terminate app if user chooses No
                    }
                }
            }

            // Attempt to load the user's saved theme from database if it exists
            try
            {
                LoadSavedThemePreset();
            }
            catch { }

            // Verify License bypassed
            /*
            if (!LicenseManager.IsLicenseValid())
            {
                using (ActivationForm activation = new ActivationForm())
                {
                    if (activation.ShowDialog() != DialogResult.OK)
                    {
                        return; // Terminate app if closed or failed activation
                    }
                }
            }
            */

            // Restartable Login Loop
            bool restart = true;
            while (restart)
            {
                restart = false;
                using (LoginForm login = new LoginForm())
                {
                    if (login.ShowDialog() == DialogResult.OK)
                    {
                        MainForm main = new MainForm();
                        Application.Run(main);
                        
                        // If user logged out, MainForm returns Retry - trigger login prompt again
                        if (main.DialogResult == DialogResult.Retry)
                        {
                            restart = true;
                        }
                    }
                }
            }
        }

        private static void LoadSavedThemePreset()
        {
            try
            {
                using (var conn = new Microsoft.Data.SqlClient.SqlConnection(DatabaseHelper.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new Microsoft.Data.SqlClient.SqlCommand("SELECT TOP 1 ThemePreset, FontSizePreset FROM AppProfile", conn))
                    {
                        using (var rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                string theme = rdr["ThemePreset"].ToString();
                                string fontSize = rdr["FontSizePreset"]?.ToString() ?? "Medium";

                                Theme.ApplyThemePreset(theme);
                                Theme.ApplyFontSizePreset(fontSize);
                            }
                        }
                    }
                }
            }
            catch { }
        }    
    }
}
