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

        // Win32: Force window above exclusive fullscreen layers
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        static readonly IntPtr HWND_TOPMOST   = new IntPtr(-1);
        const uint SWP_NOMOVE                  = 0x0002;
        const uint SWP_NOSIZE                  = 0x0001;
        const uint SWP_SHOWWINDOW              = 0x0040;
        const int  GWL_EXSTYLE                 = -20;
        const int  WS_EX_TOPMOST_FLAG          = 0x00000008;
        const int  WS_EX_LAYERED               = 0x00080000;
        const int  WS_EX_NOACTIVATE            = 0x08000000;

        const int WM_NCLBUTTONDOWN = 0xA1;
        const int HT_CAPTION = 0x2;

        private string saveFilePath;
        private ComboBox cmbCharacters;
        private DoubleBufferedFlowLayoutPanel pnlPresetCards;
        private DoubleBufferedPanel pnlAlert;
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

        // Called once when the OS window handle is created — apply TOPMOST at the Win32 level
        // so the overlay punches through exclusive fullscreen game windows.
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ForceTopmost();
        }

        private void ForceTopmost()
        {
            try
            {
                // Set WS_EX_TOPMOST | WS_EX_NOACTIVATE extended styles
                int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
                SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle | WS_EX_TOPMOST_FLAG | WS_EX_NOACTIVATE);
                // Place the window above all others including exclusive fullscreen
                SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            }
            catch { /* safe to ignore — fallback is TopMost = true which works for borderless */ }
        }

        private void InitializeOverlay()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;

            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(600, 580);
            this.BackColor = NightreignTheme.BgDark;
            this.ForeColor = NightreignTheme.TextPrimary;
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
            var pnlHeader = new DoubleBufferedPanel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = NightreignTheme.CardBg
            };
            pnlHeader.Paint += (s, e) =>
            {
                using (var pen = new Pen(NightreignTheme.BorderColor, 1f))
                {
                    e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
                }
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
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = NightreignTheme.GoldRune,
                AutoSize = true,
                Location = new Point(16, 12)
            };
            pnlHeader.Controls.Add(lblTitle);

            lblHotkeyHint = new Label
            {
                Text = "[ Press F10 or ESC to close ]",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = NightreignTheme.TextMuted,
                AutoSize = true,
                Location = new Point(18, 38)
            };
            pnlHeader.Controls.Add(lblHotkeyHint);

            Button btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Size = new Size(36, 36),
                Location = new Point(550, 12),
                BackColor = Color.Transparent,
                ForeColor = NightreignTheme.TextMuted,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 20, 25);
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.FromArgb(255, 100, 100);
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = NightreignTheme.TextMuted;
            btnClose.Click += (s, e) => this.Hide();
            pnlHeader.Controls.Add(btnClose);

            // Filter bar
            var pnlFilter = new DoubleBufferedPanel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = NightreignTheme.BgVoid,
                Padding = new Padding(16, 8, 16, 8)
            };
            pnlFilter.Paint += (s, e) =>
            {
                using (var pen = new Pen(NightreignTheme.BorderColor, 1f))
                {
                    e.Graphics.DrawLine(pen, 0, pnlFilter.Height - 1, pnlFilter.Width, pnlFilter.Height - 1);
                }
            };

            Label lblChar = new Label
            {
                Text = "Character:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = NightreignTheme.TextMuted,
                AutoSize = true,
                Location = new Point(16, 13)
            };
            pnlFilter.Controls.Add(lblChar);

            cmbCharacters = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(95, 10),
                Width = 165,
                BackColor = NightreignTheme.CardSlotBg,
                ForeColor = NightreignTheme.TextPrimary,
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
                Location = new Point(355, 9),
                Width = 225,
                Height = 30,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            NightreignTheme.StylePrimaryBlueButton(btnSnapshot);
            btnSnapshot.Click += BtnSnapshot_Click;
            pnlFilter.Controls.Add(btnSnapshot);

            // Alert Notification Panel (Initially Hidden)
            pnlAlert = new DoubleBufferedPanel
            {
                Dock = DockStyle.Bottom,
                Height = 85,
                BackColor = Color.FromArgb(14, 38, 26),
                Visible = false,
                Padding = new Padding(16, 10, 16, 10)
            };
            pnlAlert.Paint += (s, e) =>
            {
                using (var pen = new Pen(NightreignTheme.AccentGreen, 1.5f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlAlert.Width - 1, pnlAlert.Height - 1);
                }
            };

            lblAlertTitle = new Label
            {
                Text = "✅ LOADOUT APPLIED TO SAVE!",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = NightreignTheme.AccentGreen,
                AutoSize = true,
                Location = new Point(16, 10)
            };
            pnlAlert.Controls.Add(lblAlertTitle);

            lblAlertSub = new Label
            {
                Text = "👉 Quit to the Main Menu and click 'Continue' or 'Load Game' to activate in-game.",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = NightreignTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(16, 36)
            };
            pnlAlert.Controls.Add(lblAlertSub);

            // Presets scrollable container
            pnlPresetCards = new DoubleBufferedFlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = NightreignTheme.BgVoid,
                Padding = new Padding(14, 12, 14, 12)
            };

            this.Controls.Add(pnlPresetCards);
            this.Controls.Add(pnlAlert);
            this.Controls.Add(pnlFilter);
            this.Controls.Add(pnlHeader);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Draw 1.5px Elden Rune Gold border around the HUD overlay
            using (var pen = new Pen(NightreignTheme.GoldRune, 1.5f))
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
            // Re-assert TOPMOST every time we show — needed for exclusive fullscreen
            ForceTopmost();
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
                    ForeColor = NightreignTheme.TextMuted,
                    Width = 550,
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
            bool isCardHovered = false;
            var card = new DoubleBufferedPanel
            {
                Width = 550,
                Height = 88,
                BackColor = NightreignTheme.CardBg,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(12, 8, 12, 8)
            };

            card.MouseEnter += (s, e) => { isCardHovered = true; card.Invalidate(); };
            card.MouseLeave += (s, e) => { isCardHovered = false; card.Invalidate(); };
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(isCardHovered ? NightreignTheme.GoldRune : NightreignTheme.BorderColor, 1f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                }
            };

            Label lblName = new Label
            {
                Text = preset.Name,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = NightreignTheme.GoldRune,
                AutoSize = true,
                Location = new Point(12, 8),
                UseMnemonic = false
            };
            card.Controls.Add(lblName);

            Label lblCharVessel = new Label
            {
                Text = string.Format("{0} • {1} ({2} Relics)", preset.CharacterName, preset.VesselName ?? "Active Vessel", preset.RelicIds.Count),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = NightreignTheme.TextMuted,
                AutoSize = true,
                Location = new Point(14, 32),
                UseMnemonic = false
            };
            card.Controls.Add(lblCharVessel);

            // Relic slot indicators (colored dots/badges)
            var pnlDots = new DoubleBufferedFlowLayoutPanel
            {
                Location = new Point(14, 56),
                Size = new Size(380, 24),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            for (int i = 0; i < 6; i++)
            {
                Color dotColor = NightreignTheme.BorderColor;
                string tipText = "Empty Slot";

                if (i < preset.RelicIds.Count)
                {
                    string colorStr = (i < preset.RelicColors.Count) ? preset.RelicColors[i] : "";
                    dotColor = NightreignTheme.GetAffinityColor(colorStr);

                    string rName = (i < preset.RelicNames.Count) ? preset.RelicNames[i] : ("Relic " + (i + 1));
                    string rEff = (preset.RelicEffects != null && i < preset.RelicEffects.Count) ? preset.RelicEffects[i] : "";
                    tipText = string.Format("Slot {0}: {1} ({2})\n{3}", i + 1, rName, colorStr, string.IsNullOrEmpty(rEff) ? "Relic effects saved" : ("Effects:\n" + rEff));
                }

                Label dot = new Label
                {
                    Width = 14,
                    Height = 14,
                    Margin = new Padding(0, 2, 8, 0),
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
                Text = "⚡ " + Localization.Get("Apply"),
                Location = new Point(420, 24),
                Width = 112,
                Height = 36,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            NightreignTheme.StyleGoldButton(btnApply);
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
                prompt.Width = 420;
                prompt.Height = 190;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.Text = "Snapshot Current In-Game Loadout";
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.BackColor = NightreignTheme.BgDark;
                prompt.ForeColor = NightreignTheme.TextPrimary;
                prompt.MaximizeBox = false;
                prompt.MinimizeBox = false;

                Label lblPrompt = new Label
                {
                    Left = 20,
                    Top = 18,
                    Text = "Preset Name for " + charName + ":",
                    AutoSize = true,
                    ForeColor = NightreignTheme.TextMuted,
                    Font = new Font("Segoe UI", 9f)
                };
                TextBox txtName = new TextBox
                {
                    Left = 20,
                    Top = 42,
                    Width = 360,
                    Text = charName + " - " + DateTime.Now.ToString("MMM d Build"),
                    BackColor = NightreignTheme.CardSlotBg,
                    ForeColor = NightreignTheme.TextPrimary,
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Segoe UI", 9.5f)
                };
                Button btnOk = new Button
                {
                    Text = "Save Preset",
                    Left = 165,
                    Width = 110,
                    Top = 90,
                    Height = 32,
                    DialogResult = DialogResult.OK,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                NightreignTheme.StyleGoldButton(btnOk);

                Button btnCancel = new Button
                {
                    Text = "Cancel",
                    Left = 285,
                    Width = 95,
                    Top = 90,
                    Height = 32,
                    DialogResult = DialogResult.Cancel,
                    Font = new Font("Segoe UI", 9f),
                    Cursor = Cursors.Hand
                };
                NightreignTheme.StyleSecondaryButton(btnCancel);

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

        public void UpdateLanguage()
        {
            if (btnSnapshot != null) btnSnapshot.Text = Localization.Get("SnapshotCurrent");
            if (lblAlertSub != null) lblAlertSub.Text = Localization.Get("QuitMenuAlert");
            RefreshPresets();
        }
    }
}

