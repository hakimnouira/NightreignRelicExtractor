using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Web.Script.Serialization;

namespace NightreignRelicExtractor
{
    public class PresetBrowserDialog : Form
    {
        private string saveFilePath;
        private ComboBox cmbCharFilter;
        private TextBox txtSearch;
        private FlowLayoutPanel pnlCards;
        private Label lblCount;

        public LoadoutPreset SelectedPresetToLoad { get; private set; }

        public PresetBrowserDialog(string currentSavePath)
        {
            this.saveFilePath = currentSavePath;
            InitializeComponent();
            RefreshPresets();
        }

        private void InitializeComponent()
        {
            this.Text = "Preset Browser & Build Manager";
            this.Size = new Size(960, 680);
            this.MinimumSize = new Size(800, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(18, 20, 26);
            this.ForeColor = Color.FromArgb(235, 238, 245);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // Header Bar
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(24, 28, 38),
                Padding = new Padding(16, 10, 16, 10)
            };

            Label lblTitle = new Label
            {
                Text = "📂 PRESET BROWSER & BUILD MANAGER",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(212, 175, 55),
                Location = new Point(14, 8),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);

            Label lblSub = new Label
            {
                Text = "Browse, load into Vessel Builder, or apply presets directly to your save file.",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(160, 170, 190),
                Location = new Point(16, 32),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblSub);

            Button btnImportJson = new Button
            {
                Text = "📥 Import JSON",
                Location = new Point(680, 14),
                Width = 120,
                Height = 32,
                BackColor = Color.FromArgb(35, 55, 80),
                ForeColor = Color.FromArgb(180, 220, 255),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnImportJson.FlatAppearance.BorderColor = Color.FromArgb(60, 95, 140);
            btnImportJson.Click += BtnImportJson_Click;
            pnlHeader.Controls.Add(btnImportJson);

            Button btnExportJson = new Button
            {
                Text = "📤 Export All",
                Location = new Point(810, 14),
                Width = 115,
                Height = 32,
                BackColor = Color.FromArgb(40, 45, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnExportJson.FlatAppearance.BorderColor = Color.FromArgb(70, 75, 95);
            btnExportJson.Click += BtnExportJson_Click;
            pnlHeader.Controls.Add(btnExportJson);

            this.Controls.Add(pnlHeader);

            // Filter Bar
            Panel pnlFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(20, 23, 30),
                Padding = new Padding(16, 6, 16, 6)
            };

            Label lblFilter = new Label
            {
                Text = "Filter by Character:",
                Location = new Point(14, 12),
                AutoSize = true,
                ForeColor = Color.FromArgb(180, 185, 200)
            };
            pnlFilter.Controls.Add(lblFilter);

            cmbCharFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(140, 8),
                Width = 150,
                BackColor = Color.FromArgb(34, 38, 50),
                ForeColor = Color.White
            };
            cmbCharFilter.Items.Add("All");
            foreach (var ch in SaveRelicWriter.CHARACTERS) cmbCharFilter.Items.Add(ch);
            cmbCharFilter.SelectedIndex = 0;
            cmbCharFilter.SelectedIndexChanged += (s, e) => RefreshPresets();
            pnlFilter.Controls.Add(cmbCharFilter);

            Label lblSearch = new Label
            {
                Text = "Search:",
                Location = new Point(310, 12),
                AutoSize = true,
                ForeColor = Color.FromArgb(180, 185, 200)
            };
            pnlFilter.Controls.Add(lblSearch);

            txtSearch = new TextBox
            {
                Location = new Point(365, 9),
                Width = 220,
                BackColor = Color.FromArgb(34, 38, 50),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtSearch.TextChanged += (s, e) => RefreshPresets();
            pnlFilter.Controls.Add(txtSearch);

            lblCount = new Label
            {
                Text = "0 presets",
                Location = new Point(600, 12),
                AutoSize = true,
                ForeColor = Color.FromArgb(140, 145, 160)
            };
            pnlFilter.Controls.Add(lblCount);

            this.Controls.Add(pnlFilter);

            // Cards Scroll Container
            pnlCards = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.FromArgb(15, 17, 22),
                Padding = new Padding(16, 12, 16, 12)
            };
            this.Controls.Add(pnlCards);

            // Re-order docking: Header first, Filter second, Cards fill
            pnlCards.BringToFront();
        }

        private void RefreshPresets()
        {
            pnlCards.SuspendLayout();
            pnlCards.Controls.Clear();

            string selectedChar = cmbCharFilter.SelectedItem as string ?? "All";
            string query = txtSearch.Text.Trim().ToLowerInvariant();

            var presets = PresetManager.LoadPresets();
            int displayed = 0;

            foreach (var p in presets)
            {
                if (selectedChar != "All" && !string.Equals(p.CharacterName, selectedChar, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrEmpty(query))
                {
                    bool matchName = (p.Name != null && p.Name.ToLowerInvariant().Contains(query));
                    bool matchDesc = (p.Description != null && p.Description.ToLowerInvariant().Contains(query));
                    bool matchChar = (p.CharacterName != null && p.CharacterName.ToLowerInvariant().Contains(query));
                    if (!matchName && !matchDesc && !matchChar) continue;
                }

                pnlCards.Controls.Add(CreatePresetCard(p));
                displayed++;
            }

            lblCount.Text = string.Format("{0} preset{1}", displayed, displayed == 1 ? "" : "s");
            pnlCards.ResumeLayout();
        }

        private Panel CreatePresetCard(LoadoutPreset preset)
        {
            var card = new Panel
            {
                Width = 905,
                Height = 115,
                BackColor = Color.FromArgb(24, 28, 38),
                Margin = new Padding(0, 0, 0, 10),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Title line
            Label lblName = new Label
            {
                Text = preset.Name ?? "Unnamed Preset",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 185, 65),
                Location = new Point(12, 8),
                AutoSize = true,
                UseMnemonic = false
            };
            card.Controls.Add(lblName);

            Label lblCharVessel = new Label
            {
                Text = string.Format("•  {0}  |  {1}", preset.CharacterName ?? "Unknown", preset.VesselName ?? "Urn"),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(170, 180, 200),
                Location = new Point(lblName.Right + 10, 10),
                AutoSize = true,
                UseMnemonic = false
            };
            card.Controls.Add(lblCharVessel);

            // Description
            if (!string.IsNullOrEmpty(preset.Description))
            {
                Label lblDesc = new Label
                {
                    Text = preset.Description,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                    ForeColor = Color.FromArgb(145, 155, 175),
                    Location = new Point(14, 32),
                    Size = new Size(580, 18),
                    AutoEllipsis = true,
                    UseMnemonic = false
                };
                card.Controls.Add(lblDesc);
            }

            // Relic badges line (up to 3)
            int badgeX = 14;
            int badgeY = 56;
            if (preset.RelicIds != null)
            {
                for (int i = 0; i < Math.Min(3, preset.RelicIds.Count); i++)
                {
                    uint rid = preset.RelicIds[i];
                    string rName = (preset.RelicNames != null && i < preset.RelicNames.Count) ? preset.RelicNames[i] : string.Format("0x{0:X8}", rid);
                    string rColor = (preset.RelicColors != null && i < preset.RelicColors.Count) ? preset.RelicColors[i] : "Any";
                    Color tagCol = GetColorForBadge(rColor);

                    Panel badge = new Panel
                    {
                        Location = new Point(badgeX, badgeY),
                        Height = 24,
                        BackColor = Color.FromArgb(32, 36, 48),
                        BorderStyle = BorderStyle.FixedSingle,
                        Cursor = Cursors.Default
                    };

                    Label dot = new Label
                    {
                        Text = "●",
                        ForeColor = tagCol,
                        Font = new Font("Segoe UI", 8f),
                        Location = new Point(4, 3),
                        AutoSize = true
                    };
                    badge.Controls.Add(dot);

                    Label txt = new Label
                    {
                        Text = string.Format("Slot {0}: {1}", i + 1, rName),
                        ForeColor = Color.FromArgb(220, 225, 235),
                        Font = new Font("Segoe UI", 8.5f),
                        Location = new Point(18, 3),
                        AutoSize = true,
                        UseMnemonic = false
                    };
                    badge.Controls.Add(txt);
                    badge.Width = txt.Right + 8;

                    card.Controls.Add(badge);
                    badgeX += badge.Width + 8;
                }
            }

            // Action Buttons
            Button btnLoad = new Button
            {
                Text = "⚱️ Load in Builder",
                Location = new Point(610, 18),
                Width = 135,
                Height = 32,
                BackColor = Color.FromArgb(30, 60, 95),
                ForeColor = Color.FromArgb(190, 225, 255),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLoad.FlatAppearance.BorderColor = Color.FromArgb(60, 110, 170);
            btnLoad.Click += (s, e) =>
            {
                this.SelectedPresetToLoad = preset;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            card.Controls.Add(btnLoad);

            Button btnEquip = new Button
            {
                Text = "⚡ Equip & Save",
                Location = new Point(755, 18),
                Width = 135,
                Height = 32,
                BackColor = Color.FromArgb(190, 150, 40),
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnEquip.FlatAppearance.BorderColor = Color.FromArgb(230, 190, 70);
            btnEquip.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(saveFilePath) || !File.Exists(saveFilePath))
                {
                    MessageBox.Show(this, "Save file not selected or not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                try
                {
                    string bak;
                    SaveRelicWriter.ApplyPreset(saveFilePath, preset, out bak, setActiveVessel: true);
                    MessageBox.Show(this, string.Format("Loadout '{0}' applied to save file!\n\nBackup created at:\n{1}\n\n👉 Now exit to Main Menu and click 'Continue' to reload.", preset.Name, bak), "Preset Applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                catch (Exception ex)
                {
                    MessageBox.Show(this, "Failed to apply preset: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            card.Controls.Add(btnEquip);

            Button btnDel = new Button
            {
                Text = "🗑️ Delete",
                Location = new Point(795, 60),
                Width = 95,
                Height = 26,
                BackColor = Color.FromArgb(40, 25, 30),
                ForeColor = Color.FromArgb(255, 120, 130),
                Font = new Font("Segoe UI", 8f),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDel.FlatAppearance.BorderColor = Color.FromArgb(100, 40, 50);
            btnDel.Click += (s, e) =>
            {
                if (MessageBox.Show(this, "Delete preset '" + preset.Name + "'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    PresetManager.DeletePreset(preset.Id);
                    RefreshPresets();
                }
            };
            card.Controls.Add(btnDel);

            return card;
        }

        private Color GetColorForBadge(string col)
        {
            if (string.IsNullOrEmpty(col)) return Color.Gray;
            if (col.IndexOf("Red", StringComparison.OrdinalIgnoreCase) >= 0) return Color.FromArgb(235, 75, 75);
            if (col.IndexOf("Blue", StringComparison.OrdinalIgnoreCase) >= 0) return Color.FromArgb(70, 140, 245);
            if (col.IndexOf("Yellow", StringComparison.OrdinalIgnoreCase) >= 0) return Color.FromArgb(220, 185, 45);
            if (col.IndexOf("Green", StringComparison.OrdinalIgnoreCase) >= 0) return Color.FromArgb(50, 185, 90);
            return Color.FromArgb(160, 130, 210);
        }

        private void BtnExportJson_Click(object sender, EventArgs e)
        {
            var presets = PresetManager.LoadPresets();
            var serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(presets);
            Clipboard.SetText(json);
            MessageBox.Show(this, "All presets exported to clipboard as JSON!", "Export Presets", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnImportJson_Click(object sender, EventArgs e)
        {
            using (var dlg = new Form())
            {
                dlg.Text = "Import Presets from JSON";
                dlg.Size = new Size(600, 400);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = Color.FromArgb(22, 25, 33);
                dlg.ForeColor = Color.White;

                Label lblPrompt = new Label
                {
                    Text = "Paste preset JSON below (raw JSON or ChatGPT output with ```json code fences):",
                    Location = new Point(14, 12),
                    AutoSize = true,
                    ForeColor = Color.FromArgb(180, 190, 210)
                };
                dlg.Controls.Add(lblPrompt);

                TextBox txtJson = new TextBox
                {
                    Location = new Point(16, 38),
                    Size = new Size(550, 260),
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                    BackColor = Color.FromArgb(30, 34, 46),
                    ForeColor = Color.White,
                    Font = new Font("Consolas", 9f)
                };
                dlg.Controls.Add(txtJson);

                Button btnOk = new Button
                {
                    Text = "Import Presets",
                    Location = new Point(430, 310),
                    Width = 135,
                    Height = 32,
                    BackColor = Color.FromArgb(35, 65, 105),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.OK
                };
                dlg.Controls.Add(btnOk);

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    string err;
                    var imported = AIPromptBuilder.ParseAIResponse(txtJson.Text, out err);
                    if (imported == null || imported.Count == 0)
                    {
                        MessageBox.Show(this, "Failed to parse JSON: " + err, "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }


                    foreach (var imp in imported)
                    {
                        PresetManager.AddOrUpdatePreset(imp);
                    }
                    RefreshPresets();
                    MessageBox.Show(this, string.Format("Successfully imported {0} preset{1}!", imported.Count, imported.Count == 1 ? "" : "s"), "Import Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}
