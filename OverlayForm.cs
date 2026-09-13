using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NightreignRelicExtractor
{
    public class OverlayForm : Form
    {
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        const int WM_NCLBUTTONDOWN = 0xA1;
        const int HT_CAPTION = 0x2;

        private string saveFilePath;
        private ComboBox cmbCharacters;
        private FlowLayoutPanel pnlPresetCards;
        private Panel pnlAlert;
        private Label lblAlertTitle;
        private Label lblAlertSub;
        private Button btnSnapshot;
        private Label lblHotkeyHint;

        public OverlayForm(string savePath)
        {
            this.saveFilePath = savePath;
            InitializeOverlay();
            RefreshPresets();
        }

        public void UpdateSavePath(string path)
        {
            this.saveFilePath = path;
            RefreshPresets();
        }

        private void InitializeOverlay()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(580, 560);
            this.BackColor = Color.FromArgb(17, 20, 26);
            this.ForeColor = Color.FromArgb(235, 238, 245);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            this.KeyPreview = true;
            this.DoubleBuffered = true;

            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.F10)
                {
                    this.Hide();
                    e.Handled = true;
                }
            };

            // Header bar
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(24, 28, 38)
            };
            pnlHeader.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };

            Label lblTitle = new Label
            {
                Text = "⚔️ NIGHTREIGN LOADOUT OVERLAY",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 185, 65),
                AutoSize = true,
                Location = new Point(16, 12)
            };
            pnlHeader.Controls.Add(lblTitle);

            lblHotkeyHint = new Label
            {
                Text = "[ Press F10 or ESC to close ]",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(150, 155, 170),
                AutoSize = true,
                Location = new Point(18, 38)
            };
            pnlHeader.Controls.Add(lblHotkeyHint);

            Button btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Size = new Size(36, 36),
                Location = new Point(530, 12),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(180, 185, 200),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Hide();
            pnlHeader.Controls.Add(btnClose);

            // Filter bar
            Panel pnlFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Color.FromArgb(20, 24, 32),
                Padding = new Padding(16, 8, 16, 8)
            };

            Label lblChar = new Label
            {
                Text = "Character:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 205, 220),
                AutoSize = true,
                Location = new Point(16, 12)
            };
            pnlFilter.Controls.Add(lblChar);

            cmbCharacters = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(95, 9),
                Width = 160,
                BackColor = Color.FromArgb(32, 36, 48),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f)
            };
            cmbCharacters.Items.Add("All Characters");
            foreach (var ch in SaveRelicWriter.CHARACTERS)
            {
                cmbCharacters.Items.Add(ch);
            }
            cmbCharacters.SelectedIndex = 0;
            cmbCharacters.SelectedIndexChanged += (s, e) => RefreshPresets();
            pnlFilter.Controls.Add(cmbCharacters);

            btnSnapshot = new Button
            {
                Text = "💾 Snapshot Current Relics",
                Location = new Point(350, 8),
                Width = 205,
                Height = 30,
                BackColor = Color.FromArgb(35, 55, 80),
                ForeColor = Color.FromArgb(180, 220, 255),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSnapshot.FlatAppearance.BorderColor = Color.FromArgb(60, 95, 140);
            btnSnapshot.Click += BtnSnapshot_Click;
            pnlFilter.Controls.Add(btnSnapshot);

            // Alert Notification Panel (Initially Hidden)
            pnlAlert = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 85,
                BackColor = Color.FromArgb(16, 42, 28), // Dark emerald
                Visible = false,
                Padding = new Padding(16, 10, 16, 10)
            };

            lblAlertTitle = new Label
            {
                Text = "✅ LOADOUT APPLIED TO SAVE!",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 245, 140),
                AutoSize = true,
                Location = new Point(16, 10)
            };
            pnlAlert.Controls.Add(lblAlertTitle);

            lblAlertSub = new Label
            {
                Text = "👉 Quit to the Main Menu and click 'Continue' or 'Load Game' to activate in-game.",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 245, 230),
                AutoSize = true,
                Location = new Point(16, 36)
            };
            pnlAlert.Controls.Add(lblAlertSub);

            // Presets scrollable container
            pnlPresetCards = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.FromArgb(14, 16, 22),
                Padding = new Padding(12, 10, 12, 10)
            };

            this.Controls.Add(pnlPresetCards);
            this.Controls.Add(pnlAlert);
            this.Controls.Add(pnlFilter);
            this.Controls.Add(pnlHeader);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Draw 2px Elden Gold border around the overlay
            using (var pen = new Pen(Color.FromArgb(212, 175, 55), 2))
            {
                e.Graphics.DrawRectangle(pen, 1, 1, this.Width - 2, this.Height - 2);
            }
        }

        public void ShowOverlay(string currentSavePath = null)
        {
            if (!string.IsNullOrEmpty(currentSavePath))
            {
                this.saveFilePath = currentSavePath;
            }

            pnlAlert.Visible = false;
            RefreshPresets();

            this.Show();
            this.BringToFront();
            this.Activate();
        }

        public void ToggleOverlay(string currentSavePath = null)
        {
            if (this.Visible)
            {
                this.Hide();
            }
            else
            {
                ShowOverlay(currentSavePath);
            }
        }

        public void RefreshPresets()
        {
            pnlPresetCards.SuspendLayout();
            pnlPresetCards.Controls.Clear();

            string selectedChar = cmbCharacters != null && cmbCharacters.SelectedIndex > 0
                ? cmbCharacters.SelectedItem.ToString()
                : null;

            var presets = PresetManager.GetPresetsForCharacter(selectedChar);

            // If empty, suggest creating default starter presets
            if (presets.Count == 0)
            {
                Label lblEmpty = new Label
                {
                    Text = "No presets saved yet for " + (selectedChar ?? "any character") + ".\n\nClick '💾 Snapshot Current Relics' above to capture what you currently have equipped in-game, or create presets in the main window.",
                    Font = new Font("Segoe UI", 10f, FontStyle.Italic),
                    ForeColor = Color.FromArgb(160, 165, 180),
                    Width = 530,
                    Height = 120,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                pnlPresetCards.Controls.Add(lblEmpty);
            }
            else
            {
                foreach (var preset in presets)
                {
                    Panel card = CreatePresetCard(preset);
                    pnlPresetCards.Controls.Add(card);
                }
            }

            pnlPresetCards.ResumeLayout();
        }

        private Panel CreatePresetCard(LoadoutPreset preset)
        {
            Panel card = new Panel
            {
                Width = 530,
                Height = 84,
                BackColor = Color.FromArgb(24, 27, 36),
                Margin = new Padding(0, 0, 0, 8),
                Padding = new Padding(12, 8, 12, 8)
            };

            // Custom border paint
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(50, 56, 72), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                }
            };

            Label lblName = new Label
            {
                Text = preset.Name,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(225, 195, 95),
                AutoSize = true,
                Location = new Point(10, 8)
            };
            card.Controls.Add(lblName);

            Label lblCharVessel = new Label
            {
                Text = string.Format("{0} • {1} ({2} Relics)", preset.CharacterName, preset.VesselName ?? "Active Vessel", preset.RelicIds.Count),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(170, 175, 190),
                AutoSize = true,
                Location = new Point(12, 32)
            };
            card.Controls.Add(lblCharVessel);

            // Relic slot indicators (colored dots/badges)
            FlowLayoutPanel pnlDots = new FlowLayoutPanel
            {
                Location = new Point(12, 54),
                Size = new Size(360, 22),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            for (int i = 0; i < 6; i++)
            {
                Color dotColor = Color.FromArgb(60, 65, 80);
                string tipText = "Empty Slot";

                if (i < preset.RelicIds.Count)
                {
                    string colorStr = (i < preset.RelicColors.Count) ? preset.RelicColors[i] : "";
                    if (colorStr == "Red") dotColor = Color.FromArgb(220, 80, 80);
                    else if (colorStr == "Blue") dotColor = Color.FromArgb(80, 150, 240);
                    else if (colorStr == "Yellow") dotColor = Color.FromArgb(240, 200, 70);
                    else if (colorStr == "Green") dotColor = Color.FromArgb(80, 210, 110);
                    else dotColor = Color.FromArgb(180, 180, 180);

                    tipText = (i < preset.RelicNames.Count) ? preset.RelicNames[i] : ("Relic " + (i + 1));
                }

                Label dot = new Label
                {
                    Width = 14,
                    Height = 14,
                    Margin = new Padding(0, 2, 6, 0),
                    BackColor = dotColor
                };
                var tt = new ToolTip();
                tt.SetToolTip(dot, tipText);
                pnlDots.Controls.Add(dot);
            }
            card.Controls.Add(pnlDots);

            // Apply Button
            Button btnApply = new Button
            {
                Text = "⚡ Apply",
                Location = new Point(415, 24),
                Width = 100,
                Height = 36,
                BackColor = Color.FromArgb(200, 160, 45), // Golden Elden Ring button
                ForeColor = Color.FromArgb(15, 15, 20),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnApply.FlatAppearance.BorderColor = Color.FromArgb(235, 195, 80);
            btnApply.Click += (s, e) => ApplyPresetToSave(preset);
            card.Controls.Add(btnApply);

            return card;
        }

        private void ApplyPresetToSave(LoadoutPreset preset)
        {
            if (string.IsNullOrEmpty(saveFilePath) || !File.Exists(saveFilePath))
            {
                MessageBox.Show(this, "Save file path not configured or file not found.\nPlease specify your NR0000.co2 path in the main window.", "Save File Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string backupPath;
                SaveRelicWriter.ApplyPreset(saveFilePath, preset, out backupPath);

                // Play notification chime
                try { SystemSounds.Asterisk.Play(); } catch { }

                // Show prominent in-game notification banner
                lblAlertTitle.Text = string.Format("✅ LOADOUT '{0}' APPLIED TO SAVE!", preset.Name.ToUpper());
                lblAlertSub.Text = "👉 Quit to the Main Menu and click 'Continue' to reload with your new relics.";
                pnlAlert.Visible = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to apply loadout to save file:\n" + ex.Message, "Apply Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSnapshot_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(saveFilePath) || !File.Exists(saveFilePath))
            {
                MessageBox.Show(this, "Save file not found. Please locate NR0000.co2 in the main window.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string charName = cmbCharacters.SelectedIndex > 0 ? cmbCharacters.SelectedItem.ToString() : "Wylder";

            using (var prompt = new Form())
            {
                prompt.Width = 400;
                prompt.Height = 180;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.Text = "Snapshot Current In-Game Loadout";
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.BackColor = Color.FromArgb(24, 26, 34);
                prompt.ForeColor = Color.White;

                Label lblPrompt = new Label { Left = 20, Top = 18, Text = "Preset Name for " + charName + ":", AutoSize = true };
                TextBox txtName = new TextBox { Left = 20, Top = 42, Width = 340, Text = charName + " - " + DateTime.Now.ToString("MMM d Build"), BackColor = Color.FromArgb(36, 40, 52), ForeColor = Color.White };
                Button btnOk = new Button { Text = "Save Preset", Left = 160, Width = 100, Top = 85, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(200, 160, 45), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat };
                Button btnCancel = new Button { Text = "Cancel", Left = 270, Width = 90, Top = 85, DialogResult = DialogResult.Cancel, BackColor = Color.FromArgb(50, 55, 68), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

                prompt.Controls.Add(lblPrompt);
                prompt.Controls.Add(txtName);
                prompt.Controls.Add(btnOk);
                prompt.Controls.Add(btnCancel);
                prompt.AcceptButton = btnOk;
                prompt.CancelButton = btnCancel;

                if (prompt.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(txtName.Text.Trim()))
                {
                    try
                    {
                        var snapshot = SaveRelicWriter.SnapshotActiveLoadout(saveFilePath, charName, txtName.Text.Trim(), Program.ItemsDb);
                        PresetManager.AddOrUpdatePreset(snapshot);
                        RefreshPresets();
                        try { SystemSounds.Asterisk.Play(); } catch { }
                        lblAlertTitle.Text = string.Format("💾 SNAPSHOT '{0}' SAVED AS PRESET!", snapshot.Name.ToUpper());
                        lblAlertSub.Text = "You can now switch back to this build at any time!";
                        pnlAlert.Visible = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, "Failed to capture loadout: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
