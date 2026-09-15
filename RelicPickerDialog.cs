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
            this.Size = new Size(980, 680);
            this.MinimumSize = new Size(840, 540);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = NightreignTheme.BgVoid;
            this.ForeColor = NightreignTheme.TextPrimary;
            this.Font = new Font("Segoe UI", 9f);
            this.DoubleBuffered = true;

            // 1. Header panel
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 58,
                BackColor = NightreignTheme.BgHeader,
                Padding = new Padding(16, 8, 16, 8)
            };
            pnlHeader.Paint += (s, e) =>
            {
                using (var pen = new Pen(NightreignTheme.BorderSubtle, 1))
                {
                    e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
                }
            };

            Label lblTitle = new Label
            {
                Text = string.Format("Equip Relic: {0} • {1} (Slot {2})", charName, vesselName, slotNumber),
                Font = NightreignTheme.FontCardTitle,
                ForeColor = NightreignTheme.GoldRune,
                Location = new Point(14, 8),
                AutoSize = true,
                UseMnemonic = false
            };
            pnlHeader.Controls.Add(lblTitle);

            string slotTypeDesc = isDeepSlot ? "Deep Relic" : "Normal Relic";
            Color badgeColor = NightreignTheme.GetAffinityColor(requiredColor);
            Label lblReq = new Label
            {
                Text = string.Format("Slot Requirement: {0} ({1})", requiredColor.ToUpper(), slotTypeDesc),
                Font = NightreignTheme.FontSmallBold,
                ForeColor = badgeColor,
                Location = new Point(16, 32),
                AutoSize = true,
                UseMnemonic = false
            };
            pnlHeader.Controls.Add(lblReq);

            // 2. Search & Filter Panel
            Panel pnlFilters = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = NightreignTheme.BgDark,
                Padding = new Padding(14, 8, 14, 8)
            };
            pnlFilters.Paint += (s, e) =>
            {
                using (var pen = new Pen(NightreignTheme.BorderSubtle, 1))
                {
                    e.Graphics.DrawLine(pen, 0, pnlFilters.Height - 1, pnlFilters.Width, pnlFilters.Height - 1);
                }
            };

            Label lblSearch = new Label
            {
                Text = "Search:",
                Location = new Point(14, 13),
                AutoSize = true,
                ForeColor = NightreignTheme.TextMuted,
                Font = NightreignTheme.FontSmallBold
            };
            pnlFilters.Controls.Add(lblSearch);

            txtSearch = new TextBox
            {
                Location = new Point(70, 10),
                Width = 240,
                BackColor = NightreignTheme.BgInput,
                ForeColor = NightreignTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };
            txtSearch.TextChanged += (s, e) => ApplyFilters();
            pnlFilters.Controls.Add(txtSearch);

            Label lblCol = new Label
            {
                Text = "Color:",
                Location = new Point(325, 13),
                AutoSize = true,
                ForeColor = NightreignTheme.TextMuted,
                Font = NightreignTheme.FontSmallBold
            };
            pnlFilters.Controls.Add(lblCol);

            cmbColorFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(372, 10),
                Width = 115,
                BackColor = NightreignTheme.BgInput,
                ForeColor = NightreignTheme.TextPrimary,
                Font = new Font("Segoe UI", 9.5f),
                FlatStyle = FlatStyle.Flat
            };
            cmbColorFilter.Items.AddRange(new object[] { "All Colors", "Red", "Blue", "Yellow", "Green" });
            cmbColorFilter.SelectedIndex = 0;
            cmbColorFilter.SelectedIndexChanged += (s, e) => ApplyFilters();
            pnlFilters.Controls.Add(cmbColorFilter);

            chkCompatibleOnly = new CheckBox
            {
                Text = "Compatible Only (" + requiredColor + ")",
                Checked = true,
                Location = new Point(502, 11),
                Width = 200,
                ForeColor = NightreignTheme.TextHighlight,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            chkCompatibleOnly.CheckedChanged += (s, e) => ApplyFilters();
            pnlFilters.Controls.Add(chkCompatibleOnly);

            lblMatchCount = new Label
            {
                Text = "",
                Location = new Point(715, 13),
                AutoSize = true,
                ForeColor = NightreignTheme.TextMuted,
                Font = new Font("Segoe UI", 9f, FontStyle.Italic)
            };
            pnlFilters.Controls.Add(lblMatchCount);

            // 3. Bottom Action Bar
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 54,
                BackColor = NightreignTheme.BgDark,
                Padding = new Padding(14, 9, 14, 9)
            };
            pnlBottom.Paint += (s, e) =>
            {
                using (var pen = new Pen(NightreignTheme.BorderSubtle, 1))
                {
                    e.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0);
                }
            };

            btnEquip = new Button
            {
                Text = "⚔️ Equip Selected Relic",
                Location = new Point(14, 9),
                Width = 210,
                Height = 36,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            NightreignTheme.StyleGoldButton(btnEquip);
            btnEquip.Click += BtnEquip_Click;
            pnlBottom.Controls.Add(btnEquip);

            btnClear = new Button
            {
                Text = "✕ Empty Slot",
                Location = new Point(234, 9),
                Width = 120,
                Height = 36,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            NightreignTheme.StyleDangerButton(btnClear);
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
                Location = new Point(850, 9),
                Width = 100,
                Height = 36,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            NightreignTheme.StyleSecondaryButton(btnCancel);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            pnlBottom.Controls.Add(btnCancel);

            // 4. Live Inspector Panel (Above bottom bar)
            pnlInspector = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 165,
                BackColor = NightreignTheme.BgCard,
                Padding = new Padding(16, 8, 16, 8)
            };
            pnlInspector.Paint += (s, e) =>
            {
                using (var pen = new Pen(NightreignTheme.BorderSubtle, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlInspector.Width - 1, pnlInspector.Height - 1);
                }
            };

            // Inspector Header Row
            lblInspectTitle = new Label
            {
                Text = "Select a relic to inspect its full effect roll details",
                Font = NightreignTheme.FontCardTitle,
                ForeColor = NightreignTheme.GoldRune,
                Location = new Point(14, 8),
                AutoSize = true,
                UseMnemonic = false
            };
            pnlInspector.Controls.Add(lblInspectTitle);

            lblInspectColorBadge = new Label
            {
                Text = "",
                Font = NightreignTheme.FontSmallBold,
                Location = new Point(360, 10),
                AutoSize = true,
                UseMnemonic = false
            };
            pnlInspector.Controls.Add(lblInspectColorBadge);

            lblInspectTypeBadge = new Label
            {
                Text = "",
                Font = NightreignTheme.FontSmallBold,
                ForeColor = NightreignTheme.TextMuted,
                Location = new Point(450, 10),
                AutoSize = true,
                UseMnemonic = false
            };
            pnlInspector.Controls.Add(lblInspectTypeBadge);

            lblInspectIdBadge = new Label
            {
                Text = "",
                Font = NightreignTheme.FontCode,
                ForeColor = NightreignTheme.TextSubtle,
                Location = new Point(545, 10),
                AutoSize = true,
                UseMnemonic = false
            };
            pnlInspector.Controls.Add(lblInspectIdBadge);

            lblInspectCompatBadge = new Label
            {
                Text = "",
                Font = NightreignTheme.FontSmallBold,
                Location = new Point(680, 9),
                AutoSize = true,
                UseMnemonic = false
            };
            pnlInspector.Controls.Add(lblInspectCompatBadge);

            // Inspector Effects Container
            pnlInspectEffects = new Panel
            {
                Location = new Point(14, 34),
                Size = new Size(936, 120),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                BackColor = Color.FromArgb(14, 16, 23),
                Padding = new Padding(10, 6, 10, 6)
            };
            pnlInspectEffects.Paint += (s, e) =>
            {
                using (var pen = new Pen(NightreignTheme.BorderSubtle, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlInspectEffects.Width - 1, pnlInspectEffects.Height - 1);
                }
            };

            lblInspectEmpty = new Label
            {
                Text = "Click or use arrow keys on any relic in the list above to view all full effect names, stack multipliers, durations, and compatibility.",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                ForeColor = NightreignTheme.TextMuted,
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
                BackColor = Color.FromArgb(12, 14, 20),
                ForeColor = NightreignTheme.TextPrimary,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.None
            };
            try
            {
                typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(lstRelics, true, null);
            }
            catch { }
            lstRelics.Columns.Add("Color", 95);
            lstRelics.Columns.Add("Name", 225);
            lstRelics.Columns.Add("Type", 95);
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
            return NightreignTheme.GetAffinityColor(colorName);
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
                lblInspectTitle.ForeColor = NightreignTheme.TextMuted;
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
            lblInspectTitle.ForeColor = NightreignTheme.GoldRune;

            lblInspectColorBadge.Text = "● " + (r.Item.Color ?? "Colorless").ToUpper();
            lblInspectColorBadge.ForeColor = NightreignTheme.GetAffinityColor(r.Item.Color);

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
                lblInspectCompatBadge.ForeColor = NightreignTheme.TextHighlight;
            }
            else if (!slotTypeMatch)
            {
                lblInspectCompatBadge.Text = isDeepSlot ? "⚠️ Requires Deep Relic" : "⚠️ Requires Normal Relic";
                lblInspectCompatBadge.ForeColor = NightreignTheme.TextDanger;
            }
            else
            {
                lblInspectCompatBadge.Text = "⚠️ Requires " + requiredColor.ToUpper() + " Relic";
                lblInspectCompatBadge.ForeColor = NightreignTheme.TextDanger;
            }

            // Build Effect Rows
            if (r.EffectIds == null || r.EffectIds.Count == 0)
            {
                Label lblNoEff = new Label
                {
                    Text = "No special effects associated with this relic.",
                    Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                    ForeColor = NightreignTheme.TextMuted,
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
                    ForeColor = NightreignTheme.GoldRune,
                    Location = new Point(8, y),
                    Size = new Size(22, 22),
                    TextAlign = ContentAlignment.MiddleRight
                };
                pnlInspectEffects.Controls.Add(lblBullet);

                Label lblEffText = new Label
                {
                    Text = effDisplay,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                    ForeColor = NightreignTheme.TextEffects,
                    Location = new Point(34, y + 1),
                    Size = new Size(740, 22),
                    AutoEllipsis = true,
                    UseMnemonic = false
                };
                pnlInspectEffects.Controls.Add(lblEffText);

                Label lblEffId = new Label
                {
                    Text = string.Format("0x{0:X4} ({0})", effId),
                    Font = NightreignTheme.FontCode,
                    ForeColor = NightreignTheme.TextSubtle,
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
