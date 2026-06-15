using System;
using System.Drawing;
using System.Windows.Forms;

namespace MeroHardwareDokan
{
    public static class Theme
    {
        // Modern Slate and Indigo Color Palette
        public static Color Primary { get; set; } = Color.FromArgb(15, 23, 42);       // Slate 900 (Sidebar bg)
        public static Color Secondary { get; set; } = Color.FromArgb(30, 41, 59);     // Slate 800 (Main Form & Cards)
        public static Color AlternateRow { get; set; } = Color.FromArgb(51, 65, 85);  // Slate 700 (Grid alternating row)
        public static Color Accent { get; set; } = Color.FromArgb(99, 102, 241);      // Indigo 500 (Primary Action Button)
        public static Color AccentHover { get; set; } = Color.FromArgb(79, 70, 229);  // Indigo 600 (Action Hover)
        
        public static Color TextLight { get; set; } = Color.FromArgb(248, 250, 252);  // Slate 50 (Header & prominent text)
        public static Color TextDark { get; set; } = Color.FromArgb(148, 163, 184);   // Slate 400 (Muted labels)
        
        public static Color Success { get; set; } = Color.FromArgb(16, 185, 129);     // Emerald 500 (Profit, Low stock alert fine, Save)
        public static Color Warning { get; set; } = Color.FromArgb(245, 158, 11);     // Amber 500 (Low stock warn)
        public static Color Danger { get; set; } = Color.FromArgb(239, 68, 68);       // Red 500 (Loss, Delete, Close)

        public static Color AdjustBrightness(Color color, float correctionFactor)
        {
            float red = color.R;
            float green = color.G;
            float blue = color.B;

            if (correctionFactor < 0)
            {
                correctionFactor = 1 + correctionFactor;
                red *= correctionFactor;
                green *= correctionFactor;
                blue *= correctionFactor;
            }
            else
            {
                red = (255 - red) * correctionFactor + red;
                green = (255 - green) * correctionFactor + green;
                blue = (255 - blue) * correctionFactor + blue;
            }

            return Color.FromArgb(color.A, 
                Math.Min(255, Math.Max(0, (int)red)), 
                Math.Min(255, Math.Max(0, (int)green)), 
                Math.Min(255, Math.Max(0, (int)blue)));
        }

        public static void ApplyThemePreset(string name)
        {
            // Reset to dark default text colors first, then light themes override them
            TextLight = Color.FromArgb(248, 250, 252);
            TextDark = Color.FromArgb(148, 163, 184);

            if (name.StartsWith("CUSTOM|"))
            {
                try
                {
                    string[] parts = name.Split('|');
                    Primary = ColorTranslator.FromHtml(parts[1]);
                    Secondary = ColorTranslator.FromHtml(parts[2]);
                    Accent = ColorTranslator.FromHtml(parts[3]);
                    
                    bool isLight = parts[4] == "1";
                    AccentHover = AdjustBrightness(Accent, -0.15f);
                    AlternateRow = AdjustBrightness(Secondary, isLight ? -0.06f : 0.12f);

                    if (isLight)
                    {
                        TextLight = Color.FromArgb(15, 23, 42); // slate-900
                        TextDark = Color.FromArgb(71, 85, 105); // slate-600
                    }
                }
                catch
                {
                    ApplyThemePreset("Dark Slate");
                }
                return;
            }

            if (name == "Emerald Mint")
            {
                Primary = Color.FromArgb(6, 78, 59);
                Secondary = Color.FromArgb(16, 24, 32);
                AlternateRow = Color.FromArgb(32, 45, 58);
                Accent = Color.FromArgb(16, 185, 129);
                AccentHover = Color.FromArgb(5, 150, 105);
            }
            else if (name == "Deep Olive")
            {
                Primary = Color.FromArgb(18, 28, 20);
                Secondary = Color.FromArgb(28, 40, 30);
                AlternateRow = Color.FromArgb(44, 58, 46);
                Accent = Color.FromArgb(34, 197, 94);
                AccentHover = Color.FromArgb(22, 163, 74);
            }
            else if (name == "Cyberpunk Purple")
            {
                Primary = Color.FromArgb(24, 18, 36);
                Secondary = Color.FromArgb(38, 28, 56);
                AlternateRow = Color.FromArgb(58, 44, 84);
                Accent = Color.FromArgb(168, 85, 247);
                AccentHover = Color.FromArgb(147, 51, 234);
            }
            else if (name == "Midnight Blue")
            {
                Primary = Color.FromArgb(10, 18, 36);
                Secondary = Color.FromArgb(20, 32, 58);
                AlternateRow = Color.FromArgb(38, 52, 86);
                Accent = Color.FromArgb(14, 165, 233);
                AccentHover = Color.FromArgb(2, 132, 199);
            }
            else if (name == "Sunset Crimson")
            {
                Primary = Color.FromArgb(30, 16, 20);
                Secondary = Color.FromArgb(48, 25, 30);
                AlternateRow = Color.FromArgb(68, 38, 44);
                Accent = Color.FromArgb(239, 68, 110);
                AccentHover = Color.FromArgb(219, 39, 87);
            }
            else if (name == "Ocean Breeze")
            {
                Primary = Color.FromArgb(11, 26, 32);
                Secondary = Color.FromArgb(22, 42, 50);
                AlternateRow = Color.FromArgb(36, 62, 72);
                Accent = Color.FromArgb(20, 184, 166);
                AccentHover = Color.FromArgb(13, 148, 136);
            }
            else if (name == "Forest Moss")
            {
                Primary = Color.FromArgb(16, 26, 18);
                Secondary = Color.FromArgb(28, 42, 30);
                AlternateRow = Color.FromArgb(44, 60, 46);
                Accent = Color.FromArgb(132, 204, 22);
                AccentHover = Color.FromArgb(101, 163, 13);
            }
            else if (name == "Rose Gold")
            {
                Primary = Color.FromArgb(26, 20, 24);
                Secondary = Color.FromArgb(44, 34, 40);
                AlternateRow = Color.FromArgb(64, 50, 58);
                Accent = Color.FromArgb(244, 114, 182);
                AccentHover = Color.FromArgb(236, 72, 153);
            }
            // ==========================================
            // LIGHT THEMES (WHITE / LIGHT GREEN FAMILY)
            // ==========================================
            else if (name == "Pure Alabaster")
            {
                Primary = Color.FromArgb(241, 245, 249); // slate-100
                Secondary = Color.FromArgb(255, 255, 255); // White
                AlternateRow = Color.FromArgb(226, 232, 240); // slate-200
                Accent = Color.FromArgb(79, 70, 229); // indigo-600
                AccentHover = Color.FromArgb(67, 56, 202); // indigo-700
                TextLight = Color.FromArgb(15, 23, 42); // slate-900
                TextDark = Color.FromArgb(71, 85, 105); // slate-600
            }
            else if (name == "Snowy Mint")
            {
                Primary = Color.FromArgb(240, 253, 244); // mintish light green
                Secondary = Color.FromArgb(255, 255, 255); // White
                AlternateRow = Color.FromArgb(220, 252, 231);
                Accent = Color.FromArgb(16, 185, 129); // emerald mint
                AccentHover = Color.FromArgb(5, 150, 105);
                TextLight = Color.FromArgb(6, 78, 59); // deep dark green
                TextDark = Color.FromArgb(52, 73, 62);
            }
            else if (name == "Nordic Light")
            {
                Primary = Color.FromArgb(240, 244, 248); // clean cool gray
                Secondary = Color.FromArgb(255, 255, 255); // White
                AlternateRow = Color.FromArgb(217, 226, 236);
                Accent = Color.FromArgb(14, 116, 144);
                AccentHover = Color.FromArgb(8, 86, 109);
                TextLight = Color.FromArgb(16, 42, 67); // deep dark slate blue
                TextDark = Color.FromArgb(72, 101, 129);
            }
            else if (name == "Soft Peach")
            {
                Primary = Color.FromArgb(254, 243, 199); // cream amber
                Secondary = Color.FromArgb(255, 255, 255); // White
                AlternateRow = Color.FromArgb(253, 230, 138);
                Accent = Color.FromArgb(217, 119, 6);
                AccentHover = Color.FromArgb(180, 83, 9);
                TextLight = Color.FromArgb(67, 20, 7); // dark warm brown
                TextDark = Color.FromArgb(120, 53, 4);
            }
            else // Default "Dark Slate"
            {
                Primary = Color.FromArgb(15, 23, 42);
                Secondary = Color.FromArgb(30, 41, 59);
                AlternateRow = Color.FromArgb(51, 65, 85);
                Accent = Color.FromArgb(99, 102, 241);
                AccentHover = Color.FromArgb(79, 70, 229);
            }
        }

        public static string FontSizePreset { get; set; } = "Medium";
        public static Font HeaderFont { get; set; } = new Font("Segoe UI Semibold", 14F, FontStyle.Bold);
        public static Font SubHeaderFont { get; set; } = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
        public static Font MainFont { get; set; } = new Font("Segoe UI", 10F, FontStyle.Regular);
        public static Font BoldFont { get; set; } = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);

        public static void ApplyFontSizePreset(string preset)
        {
            FontSizePreset = preset;
            float scale = 1.0f;
            if (preset == "Small") scale = 0.85f;
            else if (preset == "Large") scale = 1.25f; // Scaled up by 25% for high visibility!

            HeaderFont = new Font("Segoe UI Semibold", 14F * scale, FontStyle.Bold);
            SubHeaderFont = new Font("Segoe UI Semibold", 11F * scale, FontStyle.Bold);
            MainFont = new Font("Segoe UI", 10F * scale, FontStyle.Regular);
            BoldFont = new Font("Segoe UI Semibold", 10F * scale, FontStyle.Bold);
        }

        public static void UpdateFontRecursively(Control container)
        {
            if (container == null) return;

            try
            {
                if (container is Label lbl)
                {
                    if (lbl.Font != null)
                    {
                        if (lbl.Font.Size >= 13F)
                            lbl.Font = HeaderFont;
                        else if (lbl.Font.Size >= 11F)
                            lbl.Font = SubHeaderFont;
                        else if (lbl.Font.Bold)
                            lbl.Font = BoldFont;
                        else
                            lbl.Font = MainFont;
                    }
                }
                else if (container is TextBox txt)
                {
                    txt.Font = MainFont;
                }
                else if (container is Button btn)
                {
                    // Prevent custom circular clipping logos or sidebar items from resizing incorrectly
                    if (btn.Name != "btnExit" && btn.Name != "btnCopy" && btn.Name != "btnActivate")
                    {
                        btn.Font = BoldFont;
                    }
                }
                else if (container is ComboBox cb)
                {
                    cb.Font = MainFont;
                }
                else if (container is DataGridView dgv)
                {
                    dgv.Font = MainFont;
                    dgv.ColumnHeadersDefaultCellStyle.Font = BoldFont;
                    dgv.DefaultCellStyle.Font = MainFont;
                    dgv.AlternatingRowsDefaultCellStyle.Font = MainFont;
                }
            }
            catch { }

            // Apply recursively to all children
            foreach (Control child in container.Controls)
            {
                UpdateFontRecursively(child);
            }
        }

        public static void StyleButton(Button btn, Color bg, Color fg)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = bg;
            btn.ForeColor = fg;
            btn.Font = BoldFont;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = bg == Accent ? AccentHover : ControlPaint.Light(bg);
            btn.Cursor = Cursors.Hand;
            if (btn.Height > 0 && btn.Height <= 32)
            {
                btn.Padding = new Padding(0);
            }
            else
            {
                btn.Padding = new Padding(8, 4, 8, 4);
            }
        }

        public static void StylePrimaryButton(Button btn)
        {
            StyleButton(btn, Accent, TextLight);
        }

        public static void StyleDangerButton(Button btn)
        {
            StyleButton(btn, Danger, TextLight);
        }

        public static void StyleSuccessButton(Button btn)
        {
            StyleButton(btn, Success, TextLight);
        }

        public static void StyleSecondaryButton(Button btn)
        {
            StyleButton(btn, AlternateRow, TextLight);
        }

        public static void StyleTextBox(TextBox txt)
        {
            txt.BackColor = Secondary;
            txt.ForeColor = TextLight;
            txt.Font = MainFont;
            txt.BorderStyle = BorderStyle.FixedSingle;
        }

        public static void StyleComboBox(ComboBox combo)
        {
            combo.BackColor = Secondary;
            combo.ForeColor = TextLight;
            combo.Font = MainFont;
            combo.FlatStyle = FlatStyle.Flat;
        }

        public static void StyleLabel(Label lbl, Color color, Font font)
        {
            lbl.ForeColor = color;
            lbl.Font = font;
        }

        public static void StyleGrid(DataGridView grid)
        {
            grid.EnableHeadersVisualStyles = false;
            grid.BackgroundColor = Secondary;
            grid.GridColor = AlternateRow;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.ReadOnly = true;
            grid.RowTemplate.Height = 35;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Column Header Style
            grid.ColumnHeadersDefaultCellStyle.BackColor = Primary;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextLight;
            grid.ColumnHeadersDefaultCellStyle.Font = BoldFont;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Primary;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextLight;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 48;

            // Default Row Style
            grid.DefaultCellStyle.BackColor = Secondary;
            grid.DefaultCellStyle.ForeColor = TextLight;
            grid.DefaultCellStyle.Font = MainFont;
            grid.DefaultCellStyle.SelectionBackColor = Accent;
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(248, 250, 252); // Ensures high contrast on selection!

            // Alternating Row Style
            grid.AlternatingRowsDefaultCellStyle.BackColor = AlternateRow;
            grid.AlternatingRowsDefaultCellStyle.ForeColor = TextLight;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = Accent;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.FromArgb(248, 250, 252); // High contrast!
        }

        public static Panel CreateCard(int width, int height)
        {
            Panel card = new Panel();
            card.Size = new Size(width, height);
            card.BackColor = Secondary;
            card.Padding = new Padding(12);
            return card;
        }
    }
}

