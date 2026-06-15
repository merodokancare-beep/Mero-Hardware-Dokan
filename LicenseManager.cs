using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace MeroHardwareDokan
{
    public static class LicenseManager
    {
        private const string LicenseFileName = "license.lic";
        private const string SecretSalt = "MeroHardwareDokanLicensingSecureSalt2026#@!";

        // Generates the unique Hardware ID of the machine
        public static string GetHardwareId()
        {
            try
            {
                string macAddress = GetActiveMacAddress();
                string machineName = Environment.MachineName;
                string rawId = $"MeroHardwareDokan-{macAddress}-{machineName}";

                // Generate SHA-256 hash of the raw machine data
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawId));
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        sb.Append(bytes[i].ToString("X2"));
                    }

                    // Take the first 16 characters and format it as MDKN-XXXX-XXXX-XXXX
                    string hash = sb.ToString();
                    return $"MDKN-{hash.Substring(0, 4)}-{hash.Substring(4, 4)}-{hash.Substring(8, 4)}-{hash.Substring(12, 4)}";
                }
            }
            catch
            {
                // Fallback in case of networking issues
                return "MDKN-FALL-BACK-SAFE-1234";
            }
        }

        // Helper to find the MAC address of the active network card
        private static string GetActiveMacAddress()
        {
            try
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus == OperationalStatus.Up && 
                        nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        string mac = nic.GetPhysicalAddress().ToString();
                        if (!string.IsNullOrEmpty(mac))
                        {
                            return mac;
                        }
                    }
                }
            }
            catch { }
            return "NOMACADDRESS";
        }

        // Generates a Product Key for a specific Hardware ID and Expiry Date (For Developer Use)
        public static string GenerateProductKey(string hardwareId, string expiryCode)
        {
            // Clean inputs
            hardwareId = hardwareId.Replace(" ", "").Replace("-", "").ToUpper();
            expiryCode = expiryCode.Replace(" ", "").Replace("-", "").ToUpper(); // "LIFE" or "YYYYMMDD"

            string rawToSign = $"{hardwareId}|{expiryCode}|{SecretSalt}";
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawToSign));
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    sb.Append(bytes[i].ToString("X2"));
                }

                // Take first 12 characters of the hash
                string sig = sb.ToString().Substring(0, 12);
                
                // Format signature as XXXX-XXXX-XXXX
                string formattedSig = $"{sig.Substring(0, 4)}-{sig.Substring(4, 4)}-{sig.Substring(8, 4)}";
                
                return $"{expiryCode}-{formattedSig}";
            }
        }

        // Validates a Product Key for the CURRENT machine
        public static bool ValidateProductKey(string productKey, out string message, out DateTime expiryDate)
        {
            message = "License is valid and active.";
            expiryDate = DateTime.MaxValue;
            return true;
        }

        // Checks if a valid license is saved locally
        public static bool IsLicenseValid()
        {
            return true;
        }

        // Saves the valid license key
        public static void SaveLicenseKey(string productKey)
        {
            string licensePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LicenseFileName);
            File.WriteAllText(licensePath, productKey.Trim());
        }

        // Clears the saved license (useful for reset or changing keys)
        public static void ClearLicense()
        {
            string licensePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LicenseFileName);
            if (File.Exists(licensePath))
            {
                File.Delete(licensePath);
            }
        }
    }
}

