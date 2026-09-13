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
        private Button btnEquip;
        private Button btnClear;
        private Button btnCancel;
        private Label lblMatchCount;

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
            this.Size = new Size(780, 560);
            this.MinimumSize = new Size(680, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(20, 22, 28);
            this.ForeColor = Color.FromArgb(235, 238, 245);
            this.Font = new Font("Segoe UI", 9f);

            // Header panel
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 58,
                BackColor = Color.FromArgb(16, 18, 24),
                Padding = new Padding(16, 8, 16, 8)
            };

            Label lblTitle = new Label
            {
                Text = string.Format("Equip Relic: {0} • {1} (Slot {2})", charName, vesselName, slotNumber),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 185, 65),
                Location = new Point(14, 6),
                AutoSize = true
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
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblReq);

            // Search & Filter Panel
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
                Width = 220,
                BackColor = Color.FromArgb(16, 18, 24),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtSearch.TextChanged += (s, e) => ApplyFilters();
            pnlFilters.Controls.Add(txtSearch);

            Label lblCol = new Label
            {
                Text = "Color:",
                Location = new Point(300, 12),
                AutoSize = true,
                ForeColor = Color.FromArgb(170, 180, 195)
            };
            pnlFilters.Controls.Add(lblCol);

            cmbColorFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(345, 9),
                Width = 105,
                BackColor = Color.FromArgb(16, 18, 24),
                ForeColor = Color.White
            };
            cmbColorFilter.Items.AddRange(new object[] { "All Colors", "Red", "Blue", "Yellow", "Green" });
            cmbColorFilter.SelectedIndex = 0;
            cmbColorFilter.SelectedIndexChanged += (s, e) => ApplyFilters();
            pnlFilters.Controls.Add(cmbColorFilter);

            chkCompatibleOnly = new CheckBox
            {
                Text = "Compatible Only (" + requiredColor + ")",
                Checked = true,
                Location = new Point(465, 10),
                Width = 180,
                ForeColor = Color.FromArgb(120, 220, 160),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            chkCompatibleOnly.CheckedChanged += (s, e) => ApplyFilters();
            pnlFilters.Controls.Add(chkCompatibleOnly);

            lblMatchCount = new Label
            {
                Text = "",
                Location = new Point(650, 12),
                AutoSize = true,
                ForeColor = Color.FromArgb(160, 165, 180)
            };
            pnlFilters.Controls.Add(lblMatchCount);

            // Bottom action panel
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(16, 18, 24),
                Padding = new Padding(14, 8, 14, 8)
            };

            btnEquip = new Button
            {
                Text = "Equip Selected Relic",
                Location = new Point(14, 8),
                Width = 180,
                Height = 32,
                BackColor = Color.FromArgb(200, 160, 45),
                ForeColor = Color.FromArgb(15, 15, 20),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnEquip.FlatAppearance.BorderColor = Color.FromArgb(235, 195, 80);
            btnEquip.Click += BtnEquip_Click;
            pnlBottom.Controls.Add(btnEquip);

            btnClear = new Button
            {
                Text = "Empty Slot",
                Location = new Point(204, 8),
                Width = 110,
                Height = 32,
                BackColor = Color.FromArgb(60, 40, 40),
                ForeColor = Color.FromArgb(255, 170, 170),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderColor = Color.FromArgb(100, 60, 60);
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
                Location = new Point(650, 8),
                Width = 95,
                Height = 32,
                BackColor = Color.FromArgb(40, 45, 55),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(70, 75, 90);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            pnlBottom.Controls.Add(btnCancel);

            // List View
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
            lstRelics.Columns.Add("Color", 75);
            lstRelics.Columns.Add("Name", 190);
            lstRelics.Columns.Add("Type", 85);
            lstRelics.Columns.Add("Effects", 300);
            lstRelics.Columns.Add("Relic ID", 85);

            lstRelics.DoubleClick += (s, e) => BtnEquip_Click(s, e);
            lstRelics.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    BtnEquip_Click(s, e);
                    e.Handled = true;
                }
            };

            this.Controls.Add(lstRelics);
            this.Controls.Add(pnlFilters);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlBottom);

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
                if (compOnly)
                {
                    bool isDeepRelic = (r.Item.Type == "DeepRelic");
                    if (isDeepSlot && !isDeepRelic) continue;
                    if (!isDeepSlot && isDeepRelic) continue;

                    if (!string.Equals(requiredColor, "Any", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.Equals(r.Item.Color, requiredColor, StringComparison.OrdinalIgnoreCase))
                            continue;
                    }
                }

                // 2. Explicit color filter dropdown
                if (selCol != null)
                {
                    if (!string.Equals(r.Item.Color, selCol, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                // Format effects
                var effNames = new List<string>();
                if (effectsDb != null && r.EffectIds != null)
                {
                    foreach (var effId in r.EffectIds)
                    {
                        Program.EffectInfo eff;
                        if (effectsDb.TryGetValue(effId, out eff))
                        {
                            effNames.Add(eff.NameEn);
                        }
                    }
                }
                string effectsSummary = string.Join("; ", effNames);

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
                lblMatchCount.Text = matchCount + " relics";
            }
            lstRelics.EndUpdate();
            if (lstRelics.Items.Count > 0)
            {
                lstRelics.Items[0].Selected = true;
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
