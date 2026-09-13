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
        private ToolTip tipHelp;
        private Dictionary<uint, Program.RelicEntry> relicLookup = new Dictionary<uint, Program.RelicEntry>();

        public LoadoutPreset SelectedPresetToLoad { get; private set; }

        public PresetBrowserDialog(string currentSavePath)
        {
            this.saveFilePath = currentSavePath;
            tipHelp = new ToolTip { ShowAlways = true, InitialDelay = 200, ReshowDelay = 100, AutoPopDelay = 10000 };
            LoadRelicInventory();
            InitializeComponent();
            RefreshPresets();
        }

        private void LoadRelicInventory()
        {
            try { Program.EnsureDatabasesLoaded(); } catch { }

            if (string.IsNullOrEmpty(saveFilePath) || !File.Exists(saveFilePath))
            {
                saveFilePath = SaveRelicWriter.FindDefaultSaveFile();
            }

            if (!string.IsNullOrEmpty(saveFilePath) && File.Exists(saveFilePath))
            {
                try
                {
                    byte[] cleanData = Program.DecryptSaveFileReadOnly(saveFilePath);
                    if (cleanData != null && Program.ItemsDb != null)
                    {
                        var relics = Program.ExtractRelics(cleanData, Program.ItemsDb);
                        if (relics != null)
                        {
                            foreach (var r in relics)
                            {
                                if (r != null && !relicLookup.ContainsKey(r.RelicId))
                                    relicLookup[r.RelicId] = r;
                            }
                        }
                    }
                }
                catch { }
            }
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
                Text = "Browse, inspect relic effects, load into Vessel Builder, or apply presets directly to your save file.",
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
                    bool matchRelic = false;

                    if (p.RelicNames != null)
                    {
                        foreach (var rn in p.RelicNames)
                        {
                            if (rn != null && rn.ToLowerInvariant().Contains(query)) { matchRelic = true; break; }
                        }
                    }
                    if (!matchRelic && p.RelicEffects != null)
                    {
                        foreach (var re in p.RelicEffects)
                        {
                            if (re != null && re.ToLowerInvariant().Contains(query)) { matchRelic = true; break; }
                        }
                    }
                    if (!matchRelic && p.RelicIds != null)
                    {
                        for (int i = 0; i < p.RelicIds.Count; i++)
                        {
                            var effs = GetEffectsForRelic(p, i, p.RelicIds[i]);
                            foreach (var eff in effs)
                            {
                                if (eff.ToLowerInvariant().Contains(query)) { matchRelic = true; break; }
                            }
                            if (matchRelic) break;
                        }
                    }

                    if (!matchName && !matchDesc && !matchChar && !matchRelic) continue;
                }

                pnlCards.Controls.Add(CreatePresetCard(p));
                displayed++;
            }

            lblCount.Text = string.Format("{0} preset{1}", displayed, displayed == 1 ? "" : "s");
            pnlCards.ResumeLayout();
        }

        private Panel CreatePresetCard(LoadoutPreset preset)
        {
            int relicCount = (preset.RelicIds != null) ? preset.RelicIds.Count : 0;
            int numRows = (relicCount <= 3) ? 1 : 2;
            int cardHeight = (relicCount == 0) ? 88 : (56 + (numRows * 94) + 10);

            var card = new Panel
            {
                Width = 905,
                Height = cardHeight,
                BackColor = Color.FromArgb(24, 28, 38),
                Margin = new Padding(0, 0, 0, 12),
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
                    Size = new Size(480, 18),
                    AutoEllipsis = true,
                    UseMnemonic = false
                };
                card.Controls.Add(lblDesc);
            }

            // Action Buttons
            Button btnLoad = new Button
            {
                Text = "⚱️ Load in Builder",
                Location = new Point(505, 10),
                Width = 130,
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
                Location = new Point(645, 10),
                Width = 145,
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
                string targetSave = saveFilePath;
                if (string.IsNullOrEmpty(targetSave) || !File.Exists(targetSave))
                    targetSave = SaveRelicWriter.FindDefaultSaveFile();

                if (string.IsNullOrEmpty(targetSave) || !File.Exists(targetSave))
                {
                    MessageBox.Show(this, "Save file not selected or not found.\nPlease load or select your save file first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                try
                {
                    string bak;
                    SaveRelicWriter.ApplyPreset(targetSave, preset, out bak, setActiveVessel: true);
                    MessageBox.Show(this, string.Format("Loadout '{0}' applied to save file!\n\nBackup created at:\n{1}\n(Max 4 backups kept, oldest auto-deleted)\n\n👉 Now exit to Main Menu and click 'Continue' to reload.", preset.Name, bak), "Preset Applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                Location = new Point(800, 10),
                Width = 92,
                Height = 32,
                BackColor = Color.FromArgb(40, 25, 30),
                ForeColor = Color.FromArgb(255, 120, 130),
                Font = new Font("Segoe UI", 8.5f),
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

            // Relic slot boxes with full effect listings
            if (relicCount == 0)
            {
                Label lblNoRelics = new Label
                {
                    Text = "(No relics configured in this preset)",
                    ForeColor = Color.FromArgb(120, 130, 150),
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                    Location = new Point(14, 56),
                    AutoSize = true
                };
                card.Controls.Add(lblNoRelics);
            }
            else
            {
                int maxRelics = Math.Min(6, relicCount);
                for (int i = 0; i < maxRelics; i++)
                {
                    int row = i / 3;
                    int col = i % 3;

                    int colX = 12 + col * 294;
                    int rowY = 54 + row * 94;
                    int boxW = 286;
                    int boxH = 88;

                    uint rid = preset.RelicIds[i];
                    string rName = GetRelicName(preset, i, rid);
                    string rColor = GetRelicColor(preset, i, rid);
                    Color tagCol = GetColorForBadge(rColor);

                    Panel slotBox = new Panel
                    {
                        Location = new Point(colX, rowY),
                        Size = new Size(boxW, boxH),
                        BackColor = Color.FromArgb(16, 19, 27),
                        BorderStyle = BorderStyle.FixedSingle
                    };

                    Label dot = new Label
                    {
                        Text = "●",
                        ForeColor = tagCol,
                        Font = new Font("Segoe UI", 8.5f),
                        Location = new Point(5, 4),
                        AutoSize = true
                    };
                    slotBox.Controls.Add(dot);

                    Label txtTitle = new Label
                    {
                        Text = string.Format("Slot {0}: {1}", i + 1, rName),
                        ForeColor = Color.FromArgb(235, 240, 255),
                        Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                        Location = new Point(20, 4),
                        Size = new Size(boxW - 25, 18),
                        AutoEllipsis = true,
                        UseMnemonic = false
                    };
                    tipHelp.SetToolTip(txtTitle, string.Format("Slot {0}: {1} ({2})", i + 1, rName, rColor));
                    slotBox.Controls.Add(txtTitle);

                    Panel div = new Panel
                    {
                        Location = new Point(5, 24),
                        Size = new Size(boxW - 10, 1),
                        BackColor = Color.FromArgb(40, 46, 62)
                    };
                    slotBox.Controls.Add(div);

                    // Relic effects
                    var effs = GetEffectsForRelic(preset, i, rid);
                    int effY = 28;
                    if (effs != null && effs.Count > 0)
                    {
                        for (int eIdx = 0; eIdx < Math.Min(3, effs.Count); eIdx++)
                        {
                            string effectText = effs[eIdx];
                            Label lblEff = new Label
                            {
                                Text = "✦ " + effectText,
                                ForeColor = Color.FromArgb(175, 200, 235),
                                Font = new Font("Segoe UI", 7.75f),
                                Location = new Point(6, effY),
                                Size = new Size(boxW - 12, 17),
                                AutoEllipsis = true,
                                UseMnemonic = false
                            };
                            tipHelp.SetToolTip(lblEff, effectText);
                            slotBox.Controls.Add(lblEff);
                            effY += 18;
                        }
                    }
                    else
                    {
                        Label lblEff = new Label
                        {
                            Text = "• Standard Relic (No passive)",
                            ForeColor = Color.FromArgb(120, 130, 150),
                            Font = new Font("Segoe UI", 7.75f, FontStyle.Italic),
                            Location = new Point(6, 28),
                            Size = new Size(boxW - 12, 17),
                            AutoEllipsis = true,
                            UseMnemonic = false
                        };
                        slotBox.Controls.Add(lblEff);
                    }

                    card.Controls.Add(slotBox);
                }
            }

            return card;
        }

        private string GetRelicName(LoadoutPreset preset, int slotIndex, uint relicId)
        {
            if (preset.RelicNames != null && slotIndex < preset.RelicNames.Count && !string.IsNullOrEmpty(preset.RelicNames[slotIndex]))
                return preset.RelicNames[slotIndex];
            if (relicId != 0 && relicLookup.ContainsKey(relicId) && relicLookup[relicId].Item != null)
                return relicLookup[relicId].Item.NameEn;
            if (relicId != 0)
                return string.Format("0x{0:X8}", relicId);
            return "Empty Slot";
        }

        private string GetRelicColor(LoadoutPreset preset, int slotIndex, uint relicId)
        {
            if (preset.RelicColors != null && slotIndex < preset.RelicColors.Count && !string.IsNullOrEmpty(preset.RelicColors[slotIndex]))
                return preset.RelicColors[slotIndex];
            if (relicId != 0 && relicLookup.ContainsKey(relicId) && relicLookup[relicId].Item != null)
                return relicLookup[relicId].Item.Color;
            return "Any";
        }

        private List<string> GetEffectsForRelic(LoadoutPreset preset, int slotIndex, uint relicId)
        {
            var list = new List<string>();

            if (preset.RelicEffects != null && slotIndex < preset.RelicEffects.Count && !string.IsNullOrEmpty(preset.RelicEffects[slotIndex]))
            {
                string raw = preset.RelicEffects[slotIndex];
                string[] parts;
                if (raw.Contains(" • "))
                    parts = raw.Split(new string[] { " • " }, StringSplitOptions.RemoveEmptyEntries);
                else if (raw.Contains("\n"))
                    parts = raw.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                else if (raw.Contains(";"))
                    parts = raw.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                else
                    parts = new string[] { raw };

                foreach (var p in parts)
                {
                    string s = p.Trim();
                    if (!string.IsNullOrEmpty(s)) list.Add(s);
                }
                if (list.Count > 0) return list;
            }

            if (relicId != 0 && relicLookup.ContainsKey(relicId))
            {
                var r = relicLookup[relicId];
                if (r.EffectIds != null && r.EffectIds.Count > 0)
                {
                    foreach (var eid in r.EffectIds)
                    {
                        string disp = Program.GetFullEffectDisplay(eid);
                        if (!string.IsNullOrEmpty(disp)) list.Add(disp);
                    }
                    if (list.Count > 0) return list;
                }
            }

            return list;
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
