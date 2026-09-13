using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace NightreignRelicExtractor
{
    public class RelicPickerDialog : Form
    {
        public Program.RelicEntry SelectedRelic { get; private set; }
        public bool ClearSelected { get; private set; }

        private List<Program.RelicEntry> allRelics;
        private Dictionary<uint, Program.EffectInfo> effectsDb;
        private bool isDeepSlot;
        private string requiredColor;

        private TextBox txtSearch;
        private ComboBox cmbColorFilter;
        private CheckBox chkCompatibleOnly;
        private ListView lstRelics;
        private Label lblMatchCount;

        // Inspector Controls
        private Panel pnlInspector;
        private Label lblInspectTitle;
        private Label lblInspectColorBadge;
        private Label lblInspectTypeBadge;
        private Label lblInspectIdBadge;
        private Label lblInspectCompatBadge;
        private Panel pnlInspectEffects;
        private Label lblInspectEmpty;

        // Action Buttons
        private Button btnEquip;
        private Button btnClear;
        private Button btnCancel;

        public RelicPickerDialog(
            string charName,
            string vesselName,
            int slotNumber,
            bool isDeep,
            string reqColor,
            List<Program.RelicEntry> relics,
            Dictionary<uint, Program.EffectInfo> effects)
        {
            this.allRelics = relics ?? new List<Program.RelicEntry>();
            this.effectsDb = effects;
            this.isDeepSlot = isDeep;
            this.requiredColor = reqColor ?? "Any";

            InitializeComponent(charName, vesselName, slotNumber);
            ApplyFilters();
        }

        private void InitializeComponent(string charName, string vesselName, int slotNumber)
        {
            this.Text = string.Format("Equip Relic - {0} • {1} Slot {2}", charName, vesselName, slotNumber);
            this.Size = new Size(960, 660);
            this.MinimumSize = new Size(820, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(20, 22, 28);
            this.ForeColor = Color.FromArgb(235, 238, 245);
            this.Font = new Font("Segoe UI", 9f);

            // 1. Header panel
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Color.FromArgb(16, 18, 24),
                Padding = new Padding(16, 8, 16, 8)
            };

            Label lblTitle = new Label
            {
                Text = string.Format("Equip Relic: {0} • {1} (Slot {2})", charName, vesselName, slotNumber),
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 185, 65),
                Location = new Point(14, 6),
                AutoSize = true,
                UseMnemonic = false
            };
            pnlHeader.Controls.Add(lblTitle);

            string slotTypeDesc = isDeepSlot ? "Deep Relic" : "Normal Relic";
            Color badgeColor = GetColorForName(requiredColor);
            Label lblReq = new Label
            {
                Text = string.Format("Slot Requirement: {0} ({1})", requiredColor.ToUpper(), slotTypeDesc),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = badgeColor,
                Location = new Point(16, 30),
                AutoSize = true,
                UseMnemonic = false
            };
            pnlHeader.Controls.Add(lblReq);

            // 2. Search & Filter Panel
            Panel pnlFilters = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(26, 30, 40),
                Padding = new Padding(14, 8, 14, 8)
            };

            Label lblSearch = new Label
            {
                Text = "Search:",
                Location = new Point(14, 12),
                AutoSize = true,
                ForeColor = Color.FromArgb(170, 180, 195)
            };
            pnlFilters.Controls.Add(lblSearch);

            txtSearch = new TextBox
            {
                Location = new Point(68, 9),
                Width = 240,
                BackColor = Color.FromArgb(16, 18, 24),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };
            txtSearch.TextChanged += (s, e) => ApplyFilters();
            pnlFilters.Controls.Add(txtSearch);

            Label lblCol = new Label
            {
                Text = "Color:",
                Location = new Point(320, 12),
                AutoSize = true,
                ForeColor = Color.FromArgb(170, 180, 195)
            };
            pnlFilters.Controls.Add(lblCol);

            cmbColorFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(365, 9),
                Width = 110,
                BackColor = Color.FromArgb(16, 18, 24),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f)
            };
            cmbColorFilter.Items.AddRange(new object[] { "All Colors", "Red", "Blue", "Yellow", "Green" });
            cmbColorFilter.SelectedIndex = 0;
            cmbColorFilter.SelectedIndexChanged += (s, e) => ApplyFilters();
            pnlFilters.Controls.Add(cmbColorFilter);

            chkCompatibleOnly = new CheckBox
            {
                Text = "Compatible Only (" + requiredColor + ")",
                Checked = true,
                Location = new Point(490, 10),
                Width = 200,
                ForeColor = Color.FromArgb(120, 220, 160),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            chkCompatibleOnly.CheckedChanged += (s, e) => ApplyFilters();
            pnlFilters.Controls.Add(chkCompatibleOnly);

            lblMatchCount = new Label
            {
                Text = "",
                Location = new Point(705, 12),
                AutoSize = true,
                ForeColor = Color.FromArgb(160, 165, 180),
                Font = new Font("Segoe UI", 9f, FontStyle.Italic)
            };
            pnlFilters.Controls.Add(lblMatchCount);

            // 3. Bottom Action Bar
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.FromArgb(16, 18, 24),
                Padding = new Padding(14, 9, 14, 9)
            };

            btnEquip = new Button
            {
                Text = "⚔️ Equip Selected Relic",
                Location = new Point(14, 9),
                Width = 200,
                Height = 34,
                BackColor = Color.FromArgb(200, 160, 45),
                ForeColor = Color.FromArgb(15, 15, 20),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            btnEquip.FlatAppearance.BorderColor = Color.FromArgb(235, 195, 80);
            btnEquip.Click += BtnEquip_Click;
            pnlBottom.Controls.Add(btnEquip);

            btnClear = new Button
            {
                Text = "✕ Empty Slot",
                Location = new Point(224, 9),
                Width = 115,
                Height = 34,
                BackColor = Color.FromArgb(60, 36, 40),
                ForeColor = Color.FromArgb(255, 170, 170),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderColor = Color.FromArgb(100, 55, 60);
            btnClear.Click += (s, e) =>
            {
                this.ClearSelected = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            pnlBottom.Controls.Add(btnClear);

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(830, 9),
                Width = 100,
                Height = 34,
                BackColor = Color.FromArgb(40, 45, 55),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseMnemonic = false,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(70, 75, 90);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            pnlBottom.Controls.Add(btnCancel);

            // 4. Live Inspector Panel (Above bottom bar)
            pnlInspector = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 160,
                BackColor = Color.FromArgb(22, 25, 34),
                Padding = new Padding(16, 8, 16, 8)
            };
            pnlInspector.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(48, 56, 75), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlInspector.Width - 1, pnlInspector.Height - 1);
                }
            };

            // Inspector Header Row
            lblInspectTitle = new Label
            {
                Text = "Select a relic to inspect its full effect roll details",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(235, 195, 80),
                Location = new Point(14, 8),
                AutoSize = true,
                UseMnemonic = false
            };
            pnlInspector.Controls.Add(lblInspectTitle);

            lblInspectColorBadge = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Location = new Point(360, 10),
                AutoSize = true,
                UseMnemonic = false
            };
            pnlInspector.Controls.Add(lblInspectColorBadge);

            lblInspectTypeBadge = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(170, 180, 205),
                Location = new Point(440, 10),
                AutoSize = true,
                UseMnemonic = false
            };
            pnlInspector.Controls.Add(lblInspectTypeBadge);

            lblInspectIdBadge = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(130, 140, 160),
                Location = new Point(530, 10),
                AutoSize = true,
                UseMnemonic = false
            };
            pnlInspector.Controls.Add(lblInspectIdBadge);

            lblInspectCompatBadge = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Location = new Point(660, 9),
                AutoSize = true,
                UseMnemonic = false
            };
            pnlInspector.Controls.Add(lblInspectCompatBadge);

            // Inspector Effects Container
            pnlInspectEffects = new Panel
            {
                Location = new Point(14, 34),
                Size = new Size(916, 118),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                BackColor = Color.FromArgb(16, 18, 25),
                Padding = new Padding(10, 6, 10, 6)
            };
            pnlInspectEffects.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(38, 44, 60), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlInspectEffects.Width - 1, pnlInspectEffects.Height - 1);
                }
            };

            lblInspectEmpty = new Label
            {
                Text = "Click or use arrow keys on any relic in the list above to view all full effect names, stack multipliers, durations, and compatibility.",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(140, 145, 165),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlInspectEffects.Controls.Add(lblInspectEmpty);
            pnlInspector.Controls.Add(pnlInspectEffects);

            // 5. Relics List View (Fill center)
            lstRelics = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                BackColor = Color.FromArgb(14, 16, 21),
                ForeColor = Color.FromArgb(225, 230, 240),
                Font = new Font("Segoe UI", 9f),
                BorderStyle = BorderStyle.None
            };
            lstRelics.Columns.Add("Color", 85);
            lstRelics.Columns.Add("Name", 210);
            lstRelics.Columns.Add("Type", 85);
            lstRelics.Columns.Add("All Relic Effects", 450);
            lstRelics.Columns.Add("Relic ID", 95);

            lstRelics.SelectedIndexChanged += (s, e) => UpdateInspector();
            lstRelics.DoubleClick += (s, e) => BtnEquip_Click(s, e);
            lstRelics.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    BtnEquip_Click(s, e);
                    e.Handled = true;
                }
            };

            // Add controls in docking order
            this.Controls.Add(lstRelics);
            this.Controls.Add(pnlInspector);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlFilters);
            this.Controls.Add(pnlHeader);

            this.AcceptButton = btnEquip;
            this.CancelButton = btnCancel;
        }

        private static Color GetColorForName(string colorName)
        {
            if (string.Equals(colorName, "Red", StringComparison.OrdinalIgnoreCase)) return Color.FromArgb(240, 90, 90);
            if (string.Equals(colorName, "Blue", StringComparison.OrdinalIgnoreCase)) return Color.FromArgb(90, 160, 240);
            if (string.Equals(colorName, "Yellow", StringComparison.OrdinalIgnoreCase)) return Color.FromArgb(235, 195, 70);
            if (string.Equals(colorName, "Green", StringComparison.OrdinalIgnoreCase)) return Color.FromArgb(90, 215, 120);
            return Color.FromArgb(200, 205, 220); // Any
        }

        private void ApplyFilters()
        {
            lstRelics.BeginUpdate();
            lstRelics.Items.Clear();

            string search = txtSearch != null ? txtSearch.Text.Trim() : "";
            string selCol = (cmbColorFilter != null && cmbColorFilter.SelectedIndex > 0) ? cmbColorFilter.SelectedItem.ToString() : null;
            bool compOnly = chkCompatibleOnly != null && chkCompatibleOnly.Checked;

            int matchCount = 0;
            foreach (var r in allRelics)
            {
                if (r.Item == null) continue;

                // 1. Compatibility filter
                bool isDeepRelic = (r.Item.Type == "DeepRelic");
                bool slotTypeMatch = isDeepSlot ? isDeepRelic : !isDeepRelic;
                bool colorMatch = string.Equals(requiredColor, "Any", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(r.Item.Color, requiredColor, StringComparison.OrdinalIgnoreCase);

                if (compOnly && (!slotTypeMatch || !colorMatch))
                {
                    continue;
                }

                // 2. Explicit color filter dropdown
                if (selCol != null)
                {
                    if (!string.Equals(r.Item.Color, selCol, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                // Format effects
                var effNames = new List<string>();
                if (r.EffectIds != null)
                {
                    foreach (var effId in r.EffectIds)
                    {
                        effNames.Add(Program.GetFullEffectDisplay(effId));
                    }
                }
                string effectsSummary = string.Join(" • ", effNames);

                // 3. Search query
                if (!string.IsNullOrEmpty(search))
                {
                    bool match = r.Item.NameEn.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 effectsSummary.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 string.Format("0x{0:X8}", r.RelicId).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!match) continue;
                }

                matchCount++;
                string colIcon = r.Item.Color;
                if (colIcon == "Red") colIcon = "🔴 Red";
                else if (colIcon == "Blue") colIcon = "🔵 Blue";
                else if (colIcon == "Yellow") colIcon = "🟡 Yellow";
                else if (colIcon == "Green") colIcon = "🟢 Green";

                var lvi = new ListViewItem(colIcon);
                lvi.SubItems.Add(r.Item.NameEn);
                lvi.SubItems.Add(r.Item.Type);
                lvi.SubItems.Add(effectsSummary);
                lvi.SubItems.Add(string.Format("0x{0:X8}", r.RelicId));
                lvi.Tag = r;

                lvi.ForeColor = GetColorForName(r.Item.Color);
                lstRelics.Items.Add(lvi);
            }

            if (lblMatchCount != null)
            {
                lblMatchCount.Text = matchCount + " relics found";
            }
            lstRelics.EndUpdate();

            if (lstRelics.Items.Count > 0)
            {
                lstRelics.Items[0].Selected = true;
            }
            else
            {
                UpdateInspector();
            }
        }

        private void UpdateInspector()
        {
            if (lstRelics.SelectedItems.Count == 0)
            {
                lblInspectTitle.Text = "No Relic Selected";
                lblInspectTitle.ForeColor = Color.FromArgb(160, 165, 180);
                lblInspectColorBadge.Text = "";
                lblInspectTypeBadge.Text = "";
                lblInspectIdBadge.Text = "";
                lblInspectCompatBadge.Text = "";
                pnlInspectEffects.Controls.Clear();
                pnlInspectEffects.Controls.Add(lblInspectEmpty);
                return;
            }

            var r = lstRelics.SelectedItems[0].Tag as Program.RelicEntry;
            if (r == null || r.Item == null) return;

            pnlInspectEffects.Controls.Clear();

            // Header info
            lblInspectTitle.Text = r.Item.NameEn;
            lblInspectTitle.ForeColor = Color.FromArgb(235, 195, 80);

            lblInspectColorBadge.Text = "● " + (r.Item.Color ?? "Colorless").ToUpper();
            lblInspectColorBadge.ForeColor = GetColorForName(r.Item.Color);

            lblInspectTypeBadge.Text = "[" + r.Item.Type + "]";
            lblInspectIdBadge.Text = string.Format("ID: 0x{0:X8}", r.RelicId);

            // Compatibility check
            bool isDeepRelic = (r.Item.Type == "DeepRelic");
            bool slotTypeMatch = isDeepSlot ? isDeepRelic : !isDeepRelic;
            bool colorMatch = string.Equals(requiredColor, "Any", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(r.Item.Color, requiredColor, StringComparison.OrdinalIgnoreCase);

            if (slotTypeMatch && colorMatch)
            {
                lblInspectCompatBadge.Text = "✅ COMPATIBLE WITH SLOT";
                lblInspectCompatBadge.ForeColor = Color.FromArgb(90, 225, 130);
            }
            else if (!slotTypeMatch)
            {
                lblInspectCompatBadge.Text = isDeepSlot ? "⚠️ Requires Deep Relic" : "⚠️ Requires Normal Relic";
                lblInspectCompatBadge.ForeColor = Color.FromArgb(250, 130, 130);
            }
            else
            {
                lblInspectCompatBadge.Text = "⚠️ Requires " + requiredColor.ToUpper() + " Relic";
                lblInspectCompatBadge.ForeColor = Color.FromArgb(250, 130, 130);
            }

            // Build Effect Rows
            if (r.EffectIds == null || r.EffectIds.Count == 0)
            {
                Label lblNoEff = new Label
                {
                    Text = "No special effects associated with this relic.",
                    Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                    ForeColor = Color.FromArgb(150, 155, 170),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                pnlInspectEffects.Controls.Add(lblNoEff);
                return;
            }

            int y = 6;
            for (int idx = 0; idx < r.EffectIds.Count; idx++)
            {
                uint effId = r.EffectIds[idx];
                string effDisplay = Program.GetFullEffectDisplay(effId);

                Label lblBullet = new Label
                {
                    Text = string.Format("{0}.", idx + 1),
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(220, 185, 65),
                    Location = new Point(8, y),
                    Size = new Size(22, 22),
                    TextAlign = ContentAlignment.MiddleRight
                };
                pnlInspectEffects.Controls.Add(lblBullet);

                Label lblEffText = new Label
                {
                    Text = effDisplay,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(220, 235, 255),
                    Location = new Point(34, y + 1),
                    Size = new Size(740, 22),
                    AutoEllipsis = true,
                    UseMnemonic = false
                };
                pnlInspectEffects.Controls.Add(lblEffText);

                Label lblEffId = new Label
                {
                    Text = string.Format("0x{0:X4} ({0})", effId),
                    Font = new Font("Consolas", 8.5f),
                    ForeColor = Color.FromArgb(130, 140, 160),
                    Location = new Point(780, y + 2),
                    Size = new Size(125, 20),
                    TextAlign = ContentAlignment.MiddleRight,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                pnlInspectEffects.Controls.Add(lblEffId);

                y += 24;
            }
        }

        private void BtnEquip_Click(object sender, EventArgs e)
        {
            if (lstRelics.SelectedItems.Count > 0)
            {
                this.SelectedRelic = lstRelics.SelectedItems[0].Tag as Program.RelicEntry;
                this.ClearSelected = false;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show(this, "Please select a relic from the list to equip.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
