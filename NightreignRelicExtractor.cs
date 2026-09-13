using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace NightreignRelicExtractor
{
    public static class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;

        // Nightreign AES-128-CBC Decryption Key
        public static readonly byte[] DS2_KEY = new byte[]
        {
            0x18, 0xf6, 0x32, 0x66, 0x05, 0xbd, 0x17, 0x8a,
            0x55, 0x24, 0x52, 0x3a, 0xc0, 0xa0, 0xc6, 0x09
        };

        public const int IV_SIZE = 16;
        public const int BND4_HEADER_LEN = 64;
        public const int BND4_ENTRY_HEADER_LEN = 32;
        public const uint EMPTY_EFFECT = 0xFFFFFFFF;

        public static readonly HashSet<byte> VALID_B3 = new HashSet<byte> { 0x80, 0x81, 0x82, 0x83, 0x84, 0x85 };
        public static readonly HashSet<byte> VALID_B4 = new HashSet<byte> { 0x80, 0x90, 0xC0 };

        public static readonly Dictionary<string, string> NOTE_TRANSLATIONS = new Dictionary<string, string>
        {
            { "+5%/+10%/+15%", "+5% / +10% / +15%" },
            { "+50%", "+50%" },
            { "+50回復", "+50 HP healed" },
            { "1.05倍、永続", "1.05x (Permanent)" },
            { "10%割引", "10% Discount" },
            { "10秒間1.15倍", "1.15x for 10s" },
            { "10秒間1.1倍", "1.10x for 10s" },
            { "15秒間約15%", "~15% for 15s" },
            { "15秒間約25%", "~25% for 15s" },
            { "1HP毎に武器種によるダメージ加算", "Bonus dmg based on HP & weapon type" },
            { "1つにつき耐性+75（加算）", "+75 Resistance (Additive)" },
            { "1ヒット+3", "+3 per hit" },
            { "1人あたり3.5%、最大10.5%", "3.5% per ally (Max 10.5%)" },
            { "1体あたり約1.07倍", "~1.07x per kill" },
            { "1個（消費しない）", "1 pc (Non-consumable)" },
            { "2個", "2 pcs" },
            { "2個、投擲壺の攻撃力上昇の対象", "2 pcs (Affected by throwing pot boost)" },
            { "2HP/秒", "2 HP/s" },
            { "20秒間", "20s" },
            { "3個", "3 pcs" },
            { "5%/7.5%/10%ほど増加", "5% / 7.5% / 10% increase" },
            { "5%/7.5%/10%ほど軽減", "5% / 7.5% / 10% reduction" },
            { "600ルーン", "600 Runes" },
            { "8個、投擲ナイフの攻撃力上昇の対象", "8 pcs (Affected by throwing knife boost)" },
            { "FPが最大値の5%回復", "Restores 5% max FP" },
            { "キャラクター固有効果", "Character Specific" },
            { "クールタイムあり", "Has cooldown" },
            { "リゲインを有効化する", "Enables regain" },
            { "一か所につき約1.18倍(乗算)、永続", "~1.18x per mechanism (Permanent)" },
            { "味方のスタミナ回復速度+4/s、ローリング3回分程の範囲", "Allies +4/s Stamina Recovery" },
            { "固定+15", "+15" },
            { "左優先", "Left priority" },
            { "左優先、1つのみ有効", "Only 1 active (Left priority)" },
            { "敵撃破毎に固定20回復", "+20 HP per kill" },
            { "最大FP+25、精神力+との重複可", "+25 Max FP" },
            { "最大HPの2.5%", "2.5% Max HP" },
            { "最大スタミナ+10、持久力+との重複可", "+10 Max Stamina" },
            { "残りHP40%以下で発動、最大HPの0.5%+1/秒、50秒", "HP <= 40%: 0.5% Max HP + 1/s for 50s" },
            { "深層版は重複可", "Deep version stacks" },
            { "異なる武器種同士は重複可", "Stacks across different weapon types" },
            { "約1%", "~1%" },
            { "約1.05倍", "~1.05x" },
            { "約1.14倍", "~1.14x" },
            { "約1.15倍", "~1.15x" },
            { "約1.17倍", "~1.17x" },
            { "約1.24倍", "~1.24x" },
            { "約10%カット", "~10% dmg cut" },
            { "約15%カット、HP40%未満で発動、永続", "~15% dmg cut below 40% HP (Permanent)" },
            { "約30秒間1.1倍", "~1.10x for ~30s" },
            { "約5%", "~5%" },
            { "約8%", "~8%" },
            { "自分-10%、味方30%", "Self -10%, Allies +30%" },
            { "複数あっても1つのみ有効（左優先）", "Only 1 active (Left priority)" },
            { "食物系アイテム使用時に適用", "Applies to food items" },
            { "最大10スタックまで内部加算", "Max 10 stacks" },
            { "iフレーム延長", "Extended i-frames" },
            { "+10%(乗算)", "+10% (Multiplicative)" },
            { "-3ずつ", "-3 each" },
            { "0.9倍", "0.9x" },
            { "約0.85倍", "~0.85x" }
        };

        public class ItemInfo
        {
            public int Id;
            public string Key;
            public string NameEn;
            public string Color;
            public string Type;
        }

        public class EffectInfo
        {
            public uint Id;
            public string Key;
            public string NameEn;
            public string StackNotes;
        }

        public class RelicEntry
        {
            public uint RelicId;
            public int ItemId;
            public ItemInfo Item;
            public List<uint> EffectIds = new List<uint>();
            public ushort SortKey;
            public int Position;
        }

        public class ExtractionResult
        {
            public string InputFileName;
            public string SaveDirectory;
            public string CsvPath;
            public string XlsxPath;
            public int RelicsFound;
            public bool OriginalSaveModified = false;
        }

        public static Dictionary<int, ItemInfo> ItemsDb;
        public static Dictionary<uint, EffectInfo> EffectsDb;

        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("--screenshot", StringComparison.OrdinalIgnoreCase))
            {
                string shotPath = args.Length > 1 ? args[1] : "assets/screenshot.png";
                TakeFormScreenshot(shotPath);
                return 0;
            }

            if (args.Length > 0 && args[0].Equals("--screenshot-overlay", StringComparison.OrdinalIgnoreCase))
            {
                string shotPath = args.Length > 1 ? args[1] : "assets/screenshot_overlay.png";
                TakeOverlayScreenshot(shotPath);
                return 0;
            }

            if (args.Length > 0 && args[0].Equals("--screenshot-loadouts", StringComparison.OrdinalIgnoreCase))
            {
                string shotPath = args.Length > 1 ? args[1] : "assets/screenshot_loadouts.png";
                TakeLoadoutsScreenshot(shotPath);
                return 0;
            }

            // If arguments provided and not explicitly requesting GUI, run in CLI mode
            if (args.Length > 0 && !args[0].Equals("--gui", StringComparison.OrdinalIgnoreCase))
            {
                return RunCli(args[0]);
            }

            // Otherwise, hide the console window and launch the GUI
            IntPtr consoleHwnd = GetConsoleWindow();
            if (consoleHwnd != IntPtr.Zero)
            {
                ShowWindow(consoleHwnd, SW_HIDE);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
            return 0;
        }

        private static void TakeFormScreenshot(string outputPath)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var form = new MainForm())
            {
                form.Show();
                form.TriggerExtraction();
                Application.DoEvents();
                System.Threading.Thread.Sleep(500);
                Application.DoEvents();

                using (var bmp = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
                    string dir = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    bmp.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
                }
                form.Close();
            }
        }

        private static void TakeOverlayScreenshot(string outputPath)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            EnsureDatabasesLoaded();
            string savePath = File.Exists("NR0000.co2") ? Path.GetFullPath("NR0000.co2") : "";
            using (var form = new OverlayForm(savePath))
            {
                form.Show();
                Application.DoEvents();
                System.Threading.Thread.Sleep(500);
                Application.DoEvents();

                using (var bmp = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
                    string dir = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    bmp.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
                }
                form.Close();
            }
        }

        private static void TakeLoadoutsScreenshot(string outputPath)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            EnsureDatabasesLoaded();
            using (var form = new MainForm())
            {
                form.Show();
                form.ShowTabPublic(false);
                Application.DoEvents();
                System.Threading.Thread.Sleep(500);
                Application.DoEvents();

                using (var bmp = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
                    string dir = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    bmp.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
                }
                form.Close();
            }
        }

        private static int RunCli(string targetPath)
        {
            try
            {
                string saveFilePath = targetPath.Trim('"', '\'');
                if (!File.Exists(saveFilePath))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Save file not found: " + saveFilePath);
                    Console.ResetColor();
                    return 1;
                }

                EnsureDatabasesLoaded();
                var result = ProcessExtraction(saveFilePath);

                Console.WriteLine("Nightreign Relic Extractor");
                Console.WriteLine("Input: " + result.InputFileName);
                Console.WriteLine("Relics found: " + result.RelicsFound);
                Console.WriteLine("CSV: " + Path.GetFileName(result.CsvPath));
                Console.WriteLine("Excel: " + Path.GetFileName(result.XlsxPath));
                Console.WriteLine("Original save modified: NO");
                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: " + ex.Message);
                Console.ResetColor();
                return 1;
            }
        }

        public static void EnsureDatabasesLoaded()
        {
            if (ItemsDb != null && EffectsDb != null) return;
            LoadDatabases(out ItemsDb, out EffectsDb);
        }

        private static void LoadDatabases(out Dictionary<int, ItemInfo> itemsDb, out Dictionary<uint, EffectInfo> effectsDb)
        {
            itemsDb = new Dictionary<int, ItemInfo>();
            effectsDb = new Dictionary<uint, EffectInfo>();

            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

            string itemsJson = ReadResourceOrFile("items_data.json");
            if (!string.IsNullOrEmpty(itemsJson))
            {
                var root = (Dictionary<string, object>)serializer.DeserializeObject(itemsJson);
                if (root != null && root.ContainsKey("items"))
                {
                    var items = (Dictionary<string, object>)root["items"];
                    foreach (var kvp in items)
                    {
                        int id;
                        if (int.TryParse(kvp.Key, out id))
                        {
                            var itemDict = kvp.Value as Dictionary<string, object>;
                            if (itemDict != null)
                            {
                                itemsDb[id] = new ItemInfo
                                {
                                    Id = id,
                                    Key = itemDict.ContainsKey("key") ? Convert.ToString(itemDict["key"]) : "",
                                    NameEn = itemDict.ContainsKey("name_en") ? Convert.ToString(itemDict["name_en"]) : "",
                                    Color = itemDict.ContainsKey("color") ? Convert.ToString(itemDict["color"]) : "",
                                    Type = itemDict.ContainsKey("type") ? Convert.ToString(itemDict["type"]) : ""
                                };
                            }
                        }
                    }
                }
            }

            string effectsJson = ReadResourceOrFile("effects_data.json");
            if (!string.IsNullOrEmpty(effectsJson))
            {
                var root = (Dictionary<string, object>)serializer.DeserializeObject(effectsJson);
                if (root != null && root.ContainsKey("effects"))
                {
                    var effects = (Dictionary<string, object>)root["effects"];
                    foreach (var kvp in effects)
                    {
                        uint id;
                        if (uint.TryParse(kvp.Key, out id))
                        {
                            var effDict = kvp.Value as Dictionary<string, object>;
                            if (effDict != null)
                            {
                                effectsDb[id] = new EffectInfo
                                {
                                    Id = id,
                                    Key = effDict.ContainsKey("key") ? Convert.ToString(effDict["key"]) : "",
                                    NameEn = effDict.ContainsKey("name_en") ? Convert.ToString(effDict["name_en"]) : "",
                                    StackNotes = effDict.ContainsKey("stackNotes") ? Convert.ToString(effDict["stackNotes"]) : ""
                                };
                            }
                        }
                    }
                }
            }
        }

        private static string ReadResourceOrFile(string filename)
        {
            var asm = Assembly.GetExecutingAssembly();
            foreach (string resName in asm.GetManifestResourceNames())
            {
                if (resName.EndsWith(filename, StringComparison.OrdinalIgnoreCase))
                {
                    using (var stream = asm.GetManifestResourceStream(resName))
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }

            if (File.Exists(filename)) return File.ReadAllText(filename, Encoding.UTF8);
            string subPath = Path.Combine("resources", filename);
            if (File.Exists(subPath)) return File.ReadAllText(subPath, Encoding.UTF8);
            return null;
        }

        public static ExtractionResult ProcessExtraction(string saveFilePath)
        {
            saveFilePath = Path.GetFullPath(saveFilePath);
            string saveDir = Path.GetDirectoryName(saveFilePath);
            string saveFileName = Path.GetFileName(saveFilePath);

            byte[] cleanData = DecryptSaveFileReadOnly(saveFilePath);
            if (cleanData == null || cleanData.Length == 0)
            {
                throw new InvalidDataException("Failed to decrypt or parse save data from " + saveFileName);
            }

            List<RelicEntry> relics = ExtractRelics(cleanData, ItemsDb);
            if (relics.Count == 0)
            {
                throw new InvalidDataException("No relics found in save file: " + saveFileName);
            }

            string csvPath = Path.Combine(saveDir, "relics.csv");
            ExportCsv(csvPath, relics, EffectsDb);

            string xlsxPath = Path.Combine(saveDir, "relics.xlsx");
            ExportExcel(xlsxPath, relics, EffectsDb);

            return new ExtractionResult
            {
                InputFileName = saveFileName,
                SaveDirectory = saveDir,
                CsvPath = csvPath,
                XlsxPath = xlsxPath,
                RelicsFound = relics.Count,
                OriginalSaveModified = false
            };
        }

        public static byte[] DecryptSaveFileReadOnly(string path)
        {
            byte[] raw;
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                raw = new byte[fs.Length];
                int totalRead = 0;
                while (totalRead < raw.Length)
                {
                    int bytesRead = fs.Read(raw, totalRead, raw.Length - totalRead);
                    if (bytesRead <= 0) break;
                    totalRead += bytesRead;
                }
            }

            if (raw.Length < 64 || raw[0] != 0x42 || raw[1] != 0x4E || raw[2] != 0x44 || raw[3] != 0x34)
            {
                throw new InvalidDataException("BND4 magic header not found. File is not a valid Nightreign save file.");
            }

            int numEntries = BitConverter.ToInt32(raw, 12);
            if (numEntries <= 0) throw new InvalidDataException("Save file contains 0 BND4 entries.");

            int pos = BND4_HEADER_LEN;
            if (pos + BND4_ENTRY_HEADER_LEN > raw.Length) throw new InvalidDataException("File too small for entry header.");

            if (raw[pos] != 0x40 || raw[pos + 1] != 0x00 || raw[pos + 2] != 0x00 || raw[pos + 3] != 0x00 ||
                raw[pos + 4] != 0xFF || raw[pos + 5] != 0xFF || raw[pos + 6] != 0xFF || raw[pos + 7] != 0xFF)
            {
                throw new InvalidDataException("Entry 0 header magic does not match expected format.");
            }

            int entrySize = BitConverter.ToInt32(raw, pos + 8);
            int entryDataOffset = BitConverter.ToInt32(raw, pos + 16);

            if (entrySize <= IV_SIZE || entryDataOffset <= 0 || entryDataOffset + entrySize > raw.Length)
            {
                throw new InvalidDataException("Invalid entry data size or offset in save container.");
            }

            byte[] iv = new byte[IV_SIZE];
            Array.Copy(raw, entryDataOffset, iv, 0, IV_SIZE);

            int payloadSize = entrySize - IV_SIZE;
            byte[] decrypted;

            using (var aes = new RijndaelManaged())
            {
                aes.KeySize = 128;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                aes.Key = DS2_KEY;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                {
                    decrypted = decryptor.TransformFinalBlock(raw, entryDataOffset + IV_SIZE, payloadSize);
                }
            }

            if (decrypted.Length <= 4) throw new InvalidDataException("Decrypted data is too small.");

            byte[] cleanData = new byte[decrypted.Length - 4];
            Array.Copy(decrypted, 4, cleanData, 0, cleanData.Length);
            return cleanData;
        }

        public static List<RelicEntry> ExtractRelics(byte[] cleanData, Dictionary<int, ItemInfo> itemsDb)
        {
            var potentialSlots = new List<RelicEntry>();

            for (int pos = 0; pos <= cleanData.Length - 80; pos++)
            {
                byte b3 = cleanData[pos + 2];
                byte b4 = cleanData[pos + 3];

                if (VALID_B3.Contains(b3) && VALID_B4.Contains(b4))
                {
                    int slotSize = (b4 == 0x90) ? 16 : 80;
                    if (pos + slotSize > cleanData.Length) continue;

                    uint relicId = BitConverter.ToUInt32(cleanData, pos);
                    int itemId = cleanData[pos + 4] | (cleanData[pos + 5] << 8) | (cleanData[pos + 6] << 16);

                    ItemInfo item;
                    if (!itemsDb.TryGetValue(itemId, out item)) continue;

                    if (item.Type != "Relic" && item.Type != "DeepRelic" && item.Type != "UniqueRelic")
                    {
                        continue;
                    }

                    var effList = new List<uint>();
                    for (int k = 0; k < 4; k++)
                    {
                        uint effId = BitConverter.ToUInt32(cleanData, pos + 16 + k * 4);
                        if (effId != EMPTY_EFFECT)
                        {
                            effList.Add(effId);
                        }
                    }

                    if (effList.Count == 0) continue;

                    potentialSlots.Add(new RelicEntry
                    {
                        RelicId = relicId,
                        ItemId = itemId,
                        Item = item,
                        EffectIds = effList,
                        Position = pos
                    });
                }
            }

            var sortKeyMap = new Dictionary<uint, ushort>();
            for (int i = 0; i <= cleanData.Length - 10; i++)
            {
                if (cleanData[i + 4] == 0x01 && cleanData[i + 5] == 0x00 &&
                    cleanData[i + 6] == 0x00 && cleanData[i + 7] == 0x00)
                {
                    uint candidateId = BitConverter.ToUInt32(cleanData, i);
                    if (!sortKeyMap.ContainsKey(candidateId))
                    {
                        ushort sk = BitConverter.ToUInt16(cleanData, i + 8);
                        sortKeyMap[candidateId] = sk;
                    }
                }
            }

            foreach (var slot in potentialSlots)
            {
                ushort sk;
                if (sortKeyMap.TryGetValue(slot.RelicId, out sk))
                {
                    slot.SortKey = sk;
                }
            }

            potentialSlots.Sort((a, b) =>
            {
                int cmp = b.SortKey.CompareTo(a.SortKey);
                if (cmp != 0) return cmp;
                return a.Position.CompareTo(b.Position);
            });

            return potentialSlots;
        }

        public static string FormatEffectValue(EffectInfo info, uint rawEffId)
        {
            if (info == null)
            {
                return rawEffId.ToString();
            }

            string name = info.NameEn ?? "";
            var plusMatch = Regex.Match(name, @"Plus (\d+)", RegexOptions.IgnoreCase);
            string plusVal = plusMatch.Success ? ("+" + plusMatch.Groups[1].Value) : null;

            string note = info.StackNotes ?? "";
            string translatedNote;
            if (NOTE_TRANSLATIONS.TryGetValue(note, out translatedNote))
            {
                note = translatedNote;
            }

            if (!string.IsNullOrEmpty(plusVal) && !string.IsNullOrEmpty(note))
            {
                if (note.Contains(plusVal)) return note;
                return plusVal + " (" + note + ")";
            }
            if (!string.IsNullOrEmpty(plusVal)) return plusVal;
            if (!string.IsNullOrEmpty(note)) return note;
            return "";
        }

        public static string GetFullEffectDisplay(uint effId)
        {
            if (EffectsDb == null) return string.Format("Effect 0x{0:X8}", effId);
            EffectInfo eff;
            if (EffectsDb.TryGetValue(effId, out eff))
            {
                string val = FormatEffectValue(eff, effId);
                if (!string.IsNullOrEmpty(val) && !eff.NameEn.Contains(val) && !eff.NameEn.Contains("Plus"))
                {
                    return string.Format("{0} [{1}]", eff.NameEn, val);
                }
                return eff.NameEn;
            }
            return string.Format("Unknown Effect (0x{0:X})", effId);
        }

        private static void ExportCsv(string path, List<RelicEntry> relics, Dictionary<uint, EffectInfo> effectsDb)
        {
            using (var sw = new StreamWriter(path, false, new UTF8Encoding(true)))
            {
                sw.WriteLine("#,Relic ID,Item ID,Relic Name,Color,Relic Type,Effect 1 ID,Effect 1,Effect 1 Value,Effect 2 ID,Effect 2,Effect 2 Value,Effect 3 ID,Effect 3,Effect 3 Value,Effect 4 ID,Effect 4,Effect 4 Value");

                for (int i = 0; i < relics.Count; i++)
                {
                    var r = relics[i];
                    var cols = new List<string>();
                    cols.Add((i + 1).ToString());
                    cols.Add(r.RelicId.ToString());
                    cols.Add(r.ItemId.ToString());
                    cols.Add(EscapeCsv(r.Item != null ? r.Item.NameEn : ("Unknown " + r.ItemId)));
                    cols.Add(EscapeCsv(r.Item != null ? r.Item.Color : ""));
                    cols.Add(EscapeCsv(r.Item != null ? r.Item.Type : ""));

                    for (int effIdx = 0; effIdx < 4; effIdx++)
                    {
                        if (effIdx < r.EffectIds.Count)
                        {
                            uint effId = r.EffectIds[effIdx];
                            EffectInfo effInfo;
                            effectsDb.TryGetValue(effId, out effInfo);

                            string effName = (effInfo != null && !string.IsNullOrEmpty(effInfo.NameEn)) ? effInfo.NameEn : "UNKNOWN";
                            string effVal = FormatEffectValue(effInfo, effId);

                            cols.Add(effId.ToString());
                            cols.Add(EscapeCsv(effName));
                            cols.Add(EscapeCsv(effVal));
                        }
                        else
                        {
                            cols.Add("");
                            cols.Add("");
                            cols.Add("");
                        }
                    }

                    sw.WriteLine(string.Join(",", cols.ToArray()));
                }
            }
        }

        private static string EscapeCsv(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r"))
            {
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            }
            return s;
        }

        private static void ExportExcel(string path, List<RelicEntry> relics, Dictionary<uint, EffectInfo> effectsDb)
        {
            if (File.Exists(path)) File.Delete(path);

            string[] headers = new string[]
            {
                "#", "Relic ID", "Item ID", "Relic Name", "Color", "Relic Type",
                "Effect 1 ID", "Effect 1", "Effect 1 Value",
                "Effect 2 ID", "Effect 2", "Effect 2 Value",
                "Effect 3 ID", "Effect 3", "Effect 3 Value",
                "Effect 4 ID", "Effect 4", "Effect 4 Value"
            };

            double[] colWidths = new double[headers.Length];
            for (int c = 0; c < headers.Length; c++)
            {
                colWidths[c] = headers[c].Length;
            }

            var rowsData = new List<List<string>>();
            for (int i = 0; i < relics.Count; i++)
            {
                var r = relics[i];
                var row = new List<string>();
                row.Add((i + 1).ToString());
                row.Add(r.RelicId.ToString());
                row.Add(r.ItemId.ToString());
                row.Add(r.Item != null ? r.Item.NameEn : ("Unknown " + r.ItemId));
                row.Add(r.Item != null ? r.Item.Color : "");
                row.Add(r.Item != null ? r.Item.Type : "");

                for (int effIdx = 0; effIdx < 4; effIdx++)
                {
                    if (effIdx < r.EffectIds.Count)
                    {
                        uint effId = r.EffectIds[effIdx];
                        EffectInfo effInfo;
                        effectsDb.TryGetValue(effId, out effInfo);

                        string effName = (effInfo != null && !string.IsNullOrEmpty(effInfo.NameEn)) ? effInfo.NameEn : "UNKNOWN";
                        string effVal = FormatEffectValue(effInfo, effId);

                        row.Add(effId.ToString());
                        row.Add(effName);
                        row.Add(effVal);
                    }
                    else
                    {
                        row.Add("");
                        row.Add("");
                        row.Add("");
                    }
                }

                for (int c = 0; c < row.Count; c++)
                {
                    if (!string.IsNullOrEmpty(row[c]))
                    {
                        if (row[c].Length > colWidths[c])
                        {
                            colWidths[c] = row[c].Length;
                        }
                    }
                }
                rowsData.Add(row);
            }

            for (int c = 0; c < colWidths.Length; c++)
            {
                colWidths[c] = Math.Max(colWidths[c] + 3.5, 10.0);
            }
            colWidths[0] = 6.0;

            using (var fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                WriteZipEntry(archive, "[Content_Types].xml", @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
  <Default Extension=""xml"" ContentType=""application/xml""/>
  <Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>
  <Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>
  <Override PartName=""/xl/styles.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml""/>
</Types>");

                WriteZipEntry(archive, "_rels/.rels", @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>
</Relationships>");

                WriteZipEntry(archive, "xl/_rels/workbook.xml.rels", @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/>
  <Relationship Id=""rId2"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"" Target=""styles.xml""/>
</Relationships>");

                WriteZipEntry(archive, "xl/workbook.xml", @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
  <sheets>
    <sheet name=""Relics"" sheetId=""1"" r:id=""rId1""/>
  </sheets>
</workbook>");

                WriteZipEntry(archive, "xl/styles.xml", @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<styleSheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
  <fonts count=""2"">
    <font><name val=""Calibri""/><sz val=""11""/></font>
    <font><b/><color rgb=""FFFFFFFF""/><name val=""Calibri""/><sz val=""11""/></font>
  </fonts>
  <fills count=""3"">
    <fill><patternFill patternType=""none""/></fill>
    <fill><patternFill patternType=""gray125""/></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FF2C3E50""/></patternFill></fill>
  </fills>
  <borders count=""2"">
    <border><left/><right/><top/><bottom/></border>
    <border>
      <left style=""thin""><color rgb=""FFD0D7DE""/></left>
      <right style=""thin""><color rgb=""FFD0D7DE""/></right>
      <top style=""thin""><color rgb=""FFD0D7DE""/></top>
      <bottom style=""thin""><color rgb=""FFD0D7DE""/></bottom>
    </border>
  </borders>
  <cellXfs count=""3"">
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0""/>
    <xf numFmtId=""0"" fontId=""1"" fillId=""2"" borderId=""1"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyBorder=""1""/>
    <xf numFmtId=""1"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyBorder=""1""/>
  </cellXfs>
</styleSheet>");

                var sb = new StringBuilder();
                sb.Append(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
  <sheetViews>
    <sheetView tabSelected=""1"" workbookViewId=""0"">
      <pane ySplit=""1"" topLeftCell=""A2"" activePane=""bottomLeft"" state=""frozen""/>
    </sheetView>
  </sheetViews>
  <sheetFormatPr defaultRowHeight=""20""/>
  <cols>
");
                for (int c = 0; c < colWidths.Length; c++)
                {
                    int colNum = c + 1;
                    sb.AppendFormat(@"    <col min=""{0}"" max=""{0}"" width=""{1:F1}"" customWidth=""1""/>" + "\n", colNum, colWidths[c]);
                }
                sb.Append(@"  </cols>
  <sheetData>
    <row r=""1"" ht=""24"" customHeight=""1"">
");
                for (int c = 0; c < headers.Length; c++)
                {
                    string cellRef = GetCellRef(c, 1);
                    sb.AppendFormat(@"      <c r=""{0}"" t=""inlineStr"" s=""1""><is><t>{1}</t></is></c>" + "\n", cellRef, EscapeXml(headers[c]));
                }
                sb.Append("    </row>\n");

                for (int r = 0; r < rowsData.Count; r++)
                {
                    int rowNum = r + 2;
                    sb.AppendFormat(@"    <row r=""{0}"" ht=""20"">" + "\n", rowNum);
                    var row = rowsData[r];

                    for (int c = 0; c < row.Count; c++)
                    {
                        string cellRef = GetCellRef(c, rowNum);
                        string val = row[c];
                        if (string.IsNullOrEmpty(val)) continue;

                        long numVal;
                        if ((c == 0 || c == 1 || c == 2 || c == 6 || c == 9 || c == 12 || c == 15) && long.TryParse(val, out numVal))
                        {
                            sb.AppendFormat(@"      <c r=""{0}"" s=""0""><v>{1}</v></c>" + "\n", cellRef, numVal);
                        }
                        else
                        {
                            sb.AppendFormat(@"      <c r=""{0}"" t=""inlineStr"" s=""0""><is><t>{1}</t></is></c>" + "\n", cellRef, EscapeXml(val));
                        }
                    }
                    sb.Append("    </row>\n");
                }

                sb.Append("  </sheetData>\n");
                string lastCell = GetCellRef(headers.Length - 1, rowsData.Count + 1);
                sb.AppendFormat(@"  <autoFilter ref=""A1:{0}""/>" + "\n", lastCell);
                sb.Append("</worksheet>");

                WriteZipEntry(archive, "xl/worksheets/sheet1.xml", sb.ToString());
            }
        }

        private static void WriteZipEntry(ZipArchive archive, string path, string content)
        {
            var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
            using (var writer = new StreamWriter(entry.Open(), Encoding.UTF8))
            {
                writer.Write(content);
            }
        }

        private static string GetCellRef(int colIndex, int rowNum)
        {
            string colLetter = "";
            while (colIndex >= 0)
            {
                colLetter = (char)('A' + (colIndex % 26)) + colLetter;
                colIndex = (colIndex / 26) - 1;
            }
            return colLetter + rowNum;
        }

        private static string EscapeXml(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;")
                    .Replace("\"", "&quot;")
                    .Replace("'", "&apos;");
        }
    }

    public class MainForm : Form
    {
        [DllImport("user32.dll")]
        static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        const int HOTKEY_ID = 9001;
        const int WM_HOTKEY = 0x0312;
        const int VK_F10 = 0x79;

        private TextBox txtFilePath;
        private Button btnBrowse;
        private Button btnAutoDetect;
        private Button btnExtract;
        private TextBox txtResult;
        private Button btnOpenCsv;
        private Button btnOpenExcel;
        private Button btnOpenFolder;
        private Button btnGoToBuilder;
        private Label lblStatus;

        private Button btnNavExtract;
        private Button btnNavBuilder;
        private Button btnNavOverlay;
        private Button btnQuickOverlay;
        private Panel pnlExtractView;
        private Panel pnlBuilderView;
        private Panel pnlOverlayView;

        // Vessel Builder controls
        private ComboBox cmbBuilderChar;
        private ComboBox cmbBuilderVessel;
        private Label lblBuilderActiveStatus;
        private CheckBox chkBuilderSetActive;
        private Button btnBuilderReload;
        private Panel pnlBuilderSlots;
        private Button btnBuilderSave;
        private Button btnBuilderPreset;
        private Button btnBuilderClearAll;
        private Label lblBuilderSafety;
        private VesselDetailInfo currentVesselDetail;
        private Panel[] slotPanels = new Panel[6];
        private Label[] lblSlotHeaders = new Label[6];
        private Label[] lblSlotReqColors = new Label[6];
        private Label[] lblSlotRelicNames = new Label[6];
        private Label[] lblSlotRelicEffects = new Label[6];
        private Button[] btnSlotChanges = new Button[6];
        private Button[] btnSlotClears = new Button[6];

        // Overlay & Presets tab controls
        private ComboBox cmbMainCharFilter;
        private CheckBox chkEnableHotkey;
        private FlowLayoutPanel pnlMainPresetCards;
        private Button btnMainSnapshot;
        private Button btnMainOpenOverlay;
        private Label lblOverlayStatus;
        private ToolTip slotTooltip = new ToolTip();

        private OverlayForm overlayForm = null;
        private Program.ExtractionResult lastResult = null;

        public MainForm()
        {
            Program.EnsureDatabasesLoaded();
            InitializeComponent();
            TryAutoDetectFile();
            AutoSeedDefaultPresets();
            RefreshMainPresets();
            LoadSelectedVesselData();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                RegisterHotKey(this.Handle, HOTKEY_ID, 0, VK_F10);
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                UnregisterHotKey(this.Handle, HOTKEY_ID);
            }
            catch { }

            if (overlayForm != null && !overlayForm.IsDisposed)
            {
                overlayForm.Dispose();
            }
            base.OnFormClosing(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                if (chkEnableHotkey == null || chkEnableHotkey.Checked)
                {
                    ToggleOverlay();
                    return;
                }
            }
            base.WndProc(ref m);
        }

        public void ToggleOverlay()
        {
            string savePath = txtFilePath != null ? txtFilePath.Text.Trim('"', '\'') : "";
            if (overlayForm == null || overlayForm.IsDisposed)
            {
                overlayForm = new OverlayForm(savePath);
            }
            overlayForm.ToggleOverlay(savePath);
        }

        private void InitializeComponent()
        {
            this.Text = "Nightreign Relic Extractor & Loadout Switcher";
            this.Size = new Size(960, 740);
            this.MinimumSize = new Size(960, 740);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(20, 22, 28);
            this.ForeColor = Color.FromArgb(235, 238, 245);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            this.AllowDrop = true;

            slotTooltip.AutoPopDelay = 15000;
            slotTooltip.InitialDelay = 200;
            slotTooltip.ReshowDelay = 100;
            slotTooltip.ShowAlways = true;

            this.DragEnter += Form_DragEnter;
            this.DragDrop += Form_DragDrop;

            // Header Panel
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.FromArgb(16, 18, 24),
                Padding = new Padding(18, 10, 18, 10)
            };

            Label lblTitle = new Label
            {
                Text = "Nightreign Relic Extractor & Loadout Switcher",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(212, 175, 55),
                AutoSize = true,
                UseMnemonic = false,
                Location = new Point(16, 10)
            };

            Label lblSub = new Label
            {
                Text = "Extract relics, build vessel loadouts, and hotkey switch builds in-game (F10 Overlay)",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(160, 165, 180),
                AutoSize = true,
                Location = new Point(18, 36)
            };

            btnQuickOverlay = new Button
            {
                Text = "🎮 In-Game Overlay (F10)",
                Location = new Point(720, 14),
                Width = 205,
                Height = 38,
                BackColor = Color.FromArgb(40, 58, 85),
                ForeColor = Color.FromArgb(190, 225, 255),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            btnQuickOverlay.FlatAppearance.BorderColor = Color.FromArgb(70, 110, 165);
            btnQuickOverlay.Click += (s, e) => ToggleOverlay();

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);
            pnlHeader.Controls.Add(btnQuickOverlay);

            // Tab Navigation Bar
            Panel pnlNav = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.FromArgb(24, 27, 35)
            };

            btnNavExtract = new Button
            {
                Text = "📥 Relic Extractor",
                Location = new Point(0, 0),
                Width = 175,
                Height = 38,
                BackColor = Color.FromArgb(34, 38, 50),
                ForeColor = Color.FromArgb(220, 185, 65),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            btnNavExtract.FlatAppearance.BorderSize = 0;
            btnNavExtract.Click += (s, e) => ShowTab(0);

            btnNavBuilder = new Button
            {
                Text = "⚱️ Vessel Builder",
                Location = new Point(175, 0),
                Width = 195,
                Height = 38,
                BackColor = Color.FromArgb(24, 27, 35),
                ForeColor = Color.FromArgb(170, 175, 190),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            btnNavBuilder.FlatAppearance.BorderSize = 0;
            btnNavBuilder.Click += (s, e) => ShowTab(1);

            btnNavOverlay = new Button
            {
                Text = "⚔️ Loadouts & Overlay (F10)",
                Location = new Point(370, 0),
                Width = 250,
                Height = 38,
                BackColor = Color.FromArgb(24, 27, 35),
                ForeColor = Color.FromArgb(170, 175, 190),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            btnNavOverlay.FlatAppearance.BorderSize = 0;
            btnNavOverlay.Click += (s, e) => ShowTab(2);

            pnlNav.Controls.Add(btnNavExtract);
            pnlNav.Controls.Add(btnNavBuilder);
            pnlNav.Controls.Add(btnNavOverlay);

            // Common Save File Selection Bar
            Panel pnlFileBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 58,
                BackColor = Color.FromArgb(20, 23, 30),
                Padding = new Padding(18, 6, 18, 6)
            };

            Label lblFilePrompt = new Label
            {
                Text = "Nightreign Save File (NR0000.co2):",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 205, 220),
                AutoSize = true,
                Location = new Point(16, 6)
            };
            pnlFileBar.Controls.Add(lblFilePrompt);

            txtFilePath = new TextBox
            {
                Location = new Point(18, 26),
                Width = 665,
                Height = 26,
                BackColor = Color.FromArgb(32, 36, 46),
                ForeColor = Color.FromArgb(240, 240, 240),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };
            txtFilePath.TextChanged += (s, e) =>
            {
                if (overlayForm != null && !overlayForm.IsDisposed)
                {
                    overlayForm.UpdateSavePath(txtFilePath.Text.Trim('"', '\''));
                }
                LoadSelectedVesselData();
            };
            pnlFileBar.Controls.Add(txtFilePath);

            btnBrowse = new Button
            {
                Text = "Browse...",
                Location = new Point(695, 25),
                Width = 105,
                Height = 27,
                BackColor = Color.FromArgb(42, 47, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            btnBrowse.FlatAppearance.BorderColor = Color.FromArgb(70, 75, 90);
            btnBrowse.Click += BtnBrowse_Click;
            pnlFileBar.Controls.Add(btnBrowse);

            btnAutoDetect = new Button
            {
                Text = "Auto-Detect",
                Location = new Point(810, 25),
                Width = 115,
                Height = 27,
                BackColor = Color.FromArgb(42, 47, 60),
                ForeColor = Color.FromArgb(175, 195, 255),
                FlatStyle = FlatStyle.Flat,
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            btnAutoDetect.FlatAppearance.BorderColor = Color.FromArgb(70, 75, 90);
            btnAutoDetect.Click += (s, e) => { TryAutoDetectFile(true); AutoSeedDefaultPresets(); RefreshMainPresets(); LoadSelectedVesselData(); };
            pnlFileBar.Controls.Add(btnAutoDetect);

            // Create Tab View Containers
            InitializeExtractView();
            InitializeBuilderView();
            InitializeOverlayView();

            this.Controls.Add(pnlExtractView);
            this.Controls.Add(pnlBuilderView);
            this.Controls.Add(pnlOverlayView);
            this.Controls.Add(pnlNav);
            this.Controls.Add(pnlFileBar);
            this.Controls.Add(pnlHeader);

            // Default to Extract view
            ShowTab(0);
        }

        public void ShowTab(int tabIndex)
        {
            btnNavExtract.BackColor = (tabIndex == 0) ? Color.FromArgb(34, 38, 50) : Color.FromArgb(24, 27, 35);
            btnNavExtract.ForeColor = (tabIndex == 0) ? Color.FromArgb(220, 185, 65) : Color.FromArgb(170, 175, 190);

            btnNavBuilder.BackColor = (tabIndex == 1) ? Color.FromArgb(34, 38, 50) : Color.FromArgb(24, 27, 35);
            btnNavBuilder.ForeColor = (tabIndex == 1) ? Color.FromArgb(220, 185, 65) : Color.FromArgb(170, 175, 190);

            btnNavOverlay.BackColor = (tabIndex == 2) ? Color.FromArgb(34, 38, 50) : Color.FromArgb(24, 27, 35);
            btnNavOverlay.ForeColor = (tabIndex == 2) ? Color.FromArgb(220, 185, 65) : Color.FromArgb(170, 175, 190);

            pnlExtractView.Visible = (tabIndex == 0);
            pnlBuilderView.Visible = (tabIndex == 1);
            pnlOverlayView.Visible = (tabIndex == 2);

            if (tabIndex == 0)
            {
                pnlExtractView.BringToFront();
            }
            else if (tabIndex == 1)
            {
                pnlBuilderView.BringToFront();
                LoadSelectedVesselData();
            }
            else if (tabIndex == 2)
            {
                pnlOverlayView.BringToFront();
                RefreshMainPresets();
            }
        }

        public void ShowTabPublic(bool showExtract)
        {
            ShowTab(showExtract ? 0 : 2);
        }

        private void InitializeExtractView()
        {
            pnlExtractView = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 22, 28),
                Padding = new Padding(18, 8, 18, 10)
            };

            btnExtract = new Button
            {
                Text = "Extract Relics",
                Location = new Point(18, 8),
                Width = 906,
                Height = 38,
                BackColor = Color.FromArgb(200, 160, 45),
                ForeColor = Color.FromArgb(15, 15, 20),
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            btnExtract.FlatAppearance.BorderColor = Color.FromArgb(235, 195, 80);
            btnExtract.Click += BtnExtract_Click;
            pnlExtractView.Controls.Add(btnExtract);

            lblStatus = new Label
            {
                Text = "Ready. Select a save file and click 'Extract Relics'.",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(150, 220, 150),
                AutoSize = true,
                Location = new Point(18, 52)
            };
            pnlExtractView.Controls.Add(lblStatus);

            txtResult = new TextBox
            {
                Location = new Point(18, 74),
                Width = 906,
                Height = 360,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(14, 16, 21),
                ForeColor = Color.FromArgb(225, 230, 240),
                Font = new Font("Consolas", 9.5f),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlExtractView.Controls.Add(txtResult);

            btnOpenCsv = new Button
            {
                Text = "Open CSV",
                Location = new Point(18, 444),
                Width = 110,
                Height = 34,
                BackColor = Color.FromArgb(40, 45, 55),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseMnemonic = false,
                Enabled = false,
                Cursor = Cursors.Hand
            };
            btnOpenCsv.FlatAppearance.BorderColor = Color.FromArgb(70, 75, 90);
            btnOpenCsv.Click += (s, e) => { if (lastResult != null && File.Exists(lastResult.CsvPath)) Process.Start(lastResult.CsvPath); };
            pnlExtractView.Controls.Add(btnOpenCsv);

            btnOpenExcel = new Button
            {
                Text = "Open Excel",
                Location = new Point(136, 444),
                Width = 115,
                Height = 34,
                BackColor = Color.FromArgb(40, 45, 55),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseMnemonic = false,
                Enabled = false,
                Cursor = Cursors.Hand
            };
            btnOpenExcel.FlatAppearance.BorderColor = Color.FromArgb(70, 75, 90);
            btnOpenExcel.Click += (s, e) => { if (lastResult != null && File.Exists(lastResult.XlsxPath)) Process.Start(lastResult.XlsxPath); };
            pnlExtractView.Controls.Add(btnOpenExcel);

            btnOpenFolder = new Button
            {
                Text = "Open Folder",
                Location = new Point(259, 444),
                Width = 120,
                Height = 34,
                BackColor = Color.FromArgb(40, 45, 55),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseMnemonic = false,
                Enabled = false,
                Cursor = Cursors.Hand
            };
            btnOpenFolder.FlatAppearance.BorderColor = Color.FromArgb(70, 75, 90);
            btnOpenFolder.Click += (s, e) => { if (lastResult != null && Directory.Exists(lastResult.SaveDirectory)) Process.Start("explorer.exe", lastResult.SaveDirectory); };
            pnlExtractView.Controls.Add(btnOpenFolder);

            btnGoToBuilder = new Button
            {
                Text = "⚱️ Open Vessel & Relic Builder ➔",
                Location = new Point(387, 444),
                Width = 537,
                Height = 34,
                BackColor = Color.FromArgb(40, 65, 95),
                ForeColor = Color.FromArgb(190, 225, 255),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            btnGoToBuilder.FlatAppearance.BorderColor = Color.FromArgb(70, 110, 160);
            btnGoToBuilder.Click += (s, e) => ShowTab(1);
            pnlExtractView.Controls.Add(btnGoToBuilder);
        }

        private void InitializeBuilderView()
        {
            pnlBuilderView = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 22, 28),
                Padding = new Padding(14, 6, 14, 10),
                Visible = false,
                AutoScroll = true
            };

            // Top selection bar
            Panel pnlTopBar = new Panel
            {
                Location = new Point(14, 4),
                Width = 906,
                Height = 40,
                BackColor = Color.FromArgb(26, 30, 40)
            };

            Label lblChar = new Label
            {
                Text = "Character:",
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 185, 65),
                UseMnemonic = false
            };
            pnlTopBar.Controls.Add(lblChar);

            cmbBuilderChar = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(85, 7),
                Width = 125,
                BackColor = Color.FromArgb(16, 18, 24),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f)
            };
            foreach (var ch in SaveRelicWriter.CHARACTERS) cmbBuilderChar.Items.Add(ch);
            cmbBuilderChar.SelectedIndex = 0;
            cmbBuilderChar.SelectedIndexChanged += (s, e) => LoadSelectedVesselData();
            pnlTopBar.Controls.Add(cmbBuilderChar);

            Label lblVessel = new Label
            {
                Text = "Vessel / Cup:",
                Location = new Point(222, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 185, 65),
                UseMnemonic = false
            };
            pnlTopBar.Controls.Add(lblVessel);

            cmbBuilderVessel = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(310, 7),
                Width = 150,
                BackColor = Color.FromArgb(16, 18, 24),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f)
            };
            foreach (var vn in SaveRelicWriter.VESSEL_NAMES) cmbBuilderVessel.Items.Add(vn);
            cmbBuilderVessel.SelectedIndex = 0;
            cmbBuilderVessel.SelectedIndexChanged += (s, e) => LoadSelectedVesselData();
            pnlTopBar.Controls.Add(cmbBuilderVessel);

            lblBuilderActiveStatus = new Label
            {
                Text = "🟢 Active In-Game Vessel",
                Location = new Point(475, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(90, 220, 120),
                UseMnemonic = false
            };
            pnlTopBar.Controls.Add(lblBuilderActiveStatus);

            btnBuilderReload = new Button
            {
                Text = "🔄 Reload",
                Location = new Point(815, 6),
                Width = 82,
                Height = 27,
                BackColor = Color.FromArgb(40, 48, 62),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            btnBuilderReload.FlatAppearance.BorderColor = Color.FromArgb(65, 80, 105);
            btnBuilderReload.Click += (s, e) => LoadSelectedVesselData();
            pnlTopBar.Controls.Add(btnBuilderReload);

            pnlBuilderView.Controls.Add(pnlTopBar);

            // Sub-bar
            Panel pnlSubBar = new Panel
            {
                Location = new Point(14, 48),
                Width = 906,
                Height = 26,
                BackColor = Color.FromArgb(16, 18, 24)
            };

            chkBuilderSetActive = new CheckBox
            {
                Text = "Set as Active In-Game Vessel when saving",
                Checked = true,
                Location = new Point(10, 3),
                Width = 340,
                ForeColor = Color.FromArgb(120, 220, 160),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            pnlSubBar.Controls.Add(chkBuilderSetActive);

            Label lblSubHint = new Label
            {
                Text = "Click 'Change...' on any slot to select from your save relics (hover to view full effect details)",
                Location = new Point(360, 5),
                AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.FromArgb(160, 170, 185)
            };
            pnlSubBar.Controls.Add(lblSubHint);

            pnlBuilderView.Controls.Add(pnlSubBar);

            // Slots Container
            pnlBuilderSlots = new Panel
            {
                Location = new Point(14, 76),
                Width = 906,
                Height = 345,
                BackColor = Color.FromArgb(20, 22, 28)
            };

            Label lblNormalSec = new Label
            {
                Text = "NORMAL RELIC SLOTS (Slots 1 - 3)",
                Location = new Point(0, 2),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 185, 65)
            };
            pnlBuilderSlots.Controls.Add(lblNormalSec);

            Label lblDeepSec = new Label
            {
                Text = "DEEP RELIC SLOTS (Slots 4 - 6)",
                Location = new Point(0, 172),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(150, 180, 255)
            };
            pnlBuilderSlots.Controls.Add(lblDeepSec);

            int cardWidth = 294;
            int cardHeight = 148;

            for (int i = 0; i < 6; i++)
            {
                int row = (i < 3) ? 0 : 1;
                int col = i % 3;
                int x = (col == 0) ? 0 : (col == 1 ? 306 : 612);
                int y = (row == 0) ? 20 : 192;
                int slotIdx = i;

                Panel card = new Panel
                {
                    Location = new Point(x, y),
                    Width = cardWidth,
                    Height = cardHeight,
                    BackColor = Color.FromArgb(26, 29, 38)
                };

                Label lblHeader = new Label
                {
                    Text = string.Format("Slot {0} • {1}", i + 1, (i < 3) ? "Normal" : "Deep"),
                    Location = new Point(10, 6),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(180, 190, 210),
                    UseMnemonic = false
                };
                card.Controls.Add(lblHeader);
                lblSlotHeaders[i] = lblHeader;

                Label lblReq = new Label
                {
                    Text = "⚪ ANY",
                    Location = new Point(175, 6),
                    Width = 108,
                    TextAlign = ContentAlignment.TopRight,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = Color.White,
                    UseMnemonic = false
                };
                card.Controls.Add(lblReq);
                lblSlotReqColors[i] = lblReq;

                Label lblName = new Label
                {
                    Text = "[ Empty Slot ]",
                    Location = new Point(10, 25),
                    Width = 274,
                    Height = 20,
                    AutoEllipsis = true,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(140, 145, 160),
                    UseMnemonic = false
                };
                card.Controls.Add(lblName);
                lblSlotRelicNames[i] = lblName;

                Label lblEff = new Label
                {
                    Text = "Click 'Change...' to equip a relic",
                    Location = new Point(10, 47),
                    Width = 274,
                    Height = 64,
                    AutoEllipsis = true,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(186, 230, 253),
                    UseMnemonic = false
                };
                card.Controls.Add(lblEff);
                lblSlotRelicEffects[i] = lblEff;

                Button btnChange = new Button
                {
                    Text = "🔍 Change...",
                    Location = new Point(10, 114),
                    Width = 186,
                    Height = 26,
                    BackColor = Color.FromArgb(38, 45, 60),
                    ForeColor = Color.FromArgb(210, 225, 250),
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    UseMnemonic = false,
                    Cursor = Cursors.Hand
                };
                btnChange.FlatAppearance.BorderColor = Color.FromArgb(65, 75, 95);
                btnChange.Click += (s, e) => OpenRelicPicker(slotIdx);
                card.Controls.Add(btnChange);
                btnSlotChanges[i] = btnChange;

                Button btnClear = new Button
                {
                    Text = "✕ Clear",
                    Location = new Point(202, 114),
                    Width = 82,
                    Height = 26,
                    BackColor = Color.FromArgb(50, 32, 36),
                    ForeColor = Color.FromArgb(240, 140, 140),
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    UseMnemonic = false,
                    Cursor = Cursors.Hand
                };
                btnClear.FlatAppearance.BorderColor = Color.FromArgb(85, 45, 50);
                btnClear.Click += (s, e) => ClearSlot(slotIdx);
                card.Controls.Add(btnClear);
                btnSlotClears[i] = btnClear;

                card.Paint += (s, e) =>
                {
                    Color bCol = GetSlotBorderColor(slotIdx);
                    using (var pen = new Pen(bCol, 1))
                    {
                        e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                    }
                };

                pnlBuilderSlots.Controls.Add(card);
                slotPanels[i] = card;
            }

            pnlBuilderView.Controls.Add(pnlBuilderSlots);

            // Bottom Action Bar
            Panel pnlBottomBar = new Panel
            {
                Location = new Point(14, 430),
                Width = 906,
                Height = 75,
                BackColor = Color.FromArgb(20, 22, 28)
            };

            btnBuilderSave = new Button
            {
                Text = "💾 Equip & Save to Save File",
                Location = new Point(0, 4),
                Width = 310,
                Height = 36,
                BackColor = Color.FromArgb(200, 160, 45),
                ForeColor = Color.FromArgb(15, 15, 20),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            btnBuilderSave.FlatAppearance.BorderColor = Color.FromArgb(235, 195, 80);
            btnBuilderSave.Click += BtnBuilderSave_Click;
            pnlBottomBar.Controls.Add(btnBuilderSave);

            btnBuilderPreset = new Button
            {
                Text = "⭐ Save as Preset",
                Location = new Point(325, 4),
                Width = 235,
                Height = 36,
                BackColor = Color.FromArgb(35, 55, 80),
                ForeColor = Color.FromArgb(180, 220, 255),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            btnBuilderPreset.FlatAppearance.BorderColor = Color.FromArgb(60, 95, 140);
            btnBuilderPreset.Click += BtnBuilderPreset_Click;
            pnlBottomBar.Controls.Add(btnBuilderPreset);

            btnBuilderClearAll = new Button
            {
                Text = "🧹 Clear All 6 Slots",
                Location = new Point(575, 4),
                Width = 331,
                Height = 36,
                BackColor = Color.FromArgb(42, 45, 55),
                ForeColor = Color.FromArgb(220, 225, 235),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            btnBuilderClearAll.FlatAppearance.BorderColor = Color.FromArgb(70, 75, 90);
            btnBuilderClearAll.Click += BtnBuilderClearAll_Click;
            pnlBottomBar.Controls.Add(btnBuilderClearAll);

            lblBuilderSafety = new Label
            {
                Text = "🔒 Automatic backup (.bak & timestamped) is always created before every save modification.",
                Location = new Point(2, 46),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(120, 210, 140)
            };
            pnlBottomBar.Controls.Add(lblBuilderSafety);

            pnlBuilderView.Controls.Add(pnlBottomBar);
        }

        private Color GetSlotBorderColor(int slotIndex)
        {
            if (currentVesselDetail != null && slotIndex >= 0 && slotIndex < currentVesselDetail.Slots.Count)
            {
                string req = currentVesselDetail.Slots[slotIndex].RequiredColor;
                if (string.Equals(req, "Red", StringComparison.OrdinalIgnoreCase)) return Color.FromArgb(180, 50, 50);
                if (string.Equals(req, "Blue", StringComparison.OrdinalIgnoreCase)) return Color.FromArgb(60, 110, 190);
                if (string.Equals(req, "Yellow", StringComparison.OrdinalIgnoreCase)) return Color.FromArgb(190, 150, 45);
                if (string.Equals(req, "Green", StringComparison.OrdinalIgnoreCase)) return Color.FromArgb(45, 160, 80);
                return Color.FromArgb(110, 90, 150); // Any
            }
            return Color.FromArgb(70, 75, 90);
        }

        private void LoadSelectedVesselData()
        {
            string path = txtFilePath != null ? txtFilePath.Text.Trim('"', '\'') : "";
            int charIdx = (cmbBuilderChar != null && cmbBuilderChar.SelectedIndex >= 0) ? cmbBuilderChar.SelectedIndex : 0;
            int vesselIdx = (cmbBuilderVessel != null && cmbBuilderVessel.SelectedIndex >= 0) ? cmbBuilderVessel.SelectedIndex : 0;

            currentVesselDetail = SaveRelicWriter.ReadCharacterVesselDetail(
                path,
                charIdx,
                vesselIdx,
                Program.ItemsDb,
                Program.EffectsDb);

            // Update Active Status
            if (lblBuilderActiveStatus != null)
            {
                if (currentVesselDetail.IsActiveVessel)
                {
                    lblBuilderActiveStatus.Text = "🟢 ACTIVE IN-GAME VESSEL";
                    lblBuilderActiveStatus.ForeColor = Color.FromArgb(90, 220, 120);
                    if (chkBuilderSetActive != null) chkBuilderSetActive.Checked = true;
                }
                else
                {
                    lblBuilderActiveStatus.Text = "⚪ Inactive (" + currentVesselDetail.ActiveVesselName + " Active)";
                    lblBuilderActiveStatus.ForeColor = Color.FromArgb(165, 175, 190);
                    if (chkBuilderSetActive != null) chkBuilderSetActive.Checked = false;
                }
            }

            // Update all 6 slots
            for (int s = 0; s < 6; s++)
            {
                UpdateSlotCardUI(s);
            }
        }

        private void UpdateSlotCardUI(int s)
        {
            if (currentVesselDetail == null || s < 0 || s >= currentVesselDetail.Slots.Count) return;
            var slot = currentVesselDetail.Slots[s];

            if (lblSlotHeaders[s] != null)
            {
                lblSlotHeaders[s].Text = string.Format("Slot {0} • {1}", s + 1, slot.IsDeepSlot ? "Deep" : "Normal");
            }

            if (lblSlotReqColors[s] != null)
            {
                string c = slot.RequiredColor;
                string icon = "⚪ ANY";
                Color fCol = Color.FromArgb(200, 205, 220);
                if (c == "Red") { icon = "🔴 RED"; fCol = Color.FromArgb(240, 90, 90); }
                else if (c == "Blue") { icon = "🔵 BLUE"; fCol = Color.FromArgb(90, 160, 240); }
                else if (c == "Yellow") { icon = "🟡 YELLOW"; fCol = Color.FromArgb(235, 195, 70); }
                else if (c == "Green") { icon = "🟢 GREEN"; fCol = Color.FromArgb(90, 215, 120); }

                lblSlotReqColors[s].Text = icon;
                lblSlotReqColors[s].ForeColor = fCol;
            }

            if (lblSlotRelicNames[s] != null)
            {
                if (slot.RelicId != 0)
                {
                    lblSlotRelicNames[s].Text = slot.RelicName;
                    if (slot.RelicColor == "Red") lblSlotRelicNames[s].ForeColor = Color.FromArgb(248, 113, 113);
                    else if (slot.RelicColor == "Blue") lblSlotRelicNames[s].ForeColor = Color.FromArgb(96, 165, 250);
                    else if (slot.RelicColor == "Yellow") lblSlotRelicNames[s].ForeColor = Color.FromArgb(251, 191, 36);
                    else if (slot.RelicColor == "Green") lblSlotRelicNames[s].ForeColor = Color.FromArgb(74, 222, 128);
                    else lblSlotRelicNames[s].ForeColor = Color.FromArgb(241, 245, 249);
                }
                else
                {
                    lblSlotRelicNames[s].Text = "[ Empty Slot ]";
                    lblSlotRelicNames[s].ForeColor = Color.FromArgb(130, 135, 150);
                }
            }

            var bullets = new List<string>();
            if (slot.RelicId != 0 && slot.EffectDescriptions != null)
            {
                foreach (var desc in slot.EffectDescriptions)
                {
                    bullets.Add("✦ " + desc);
                }
            }

            if (lblSlotRelicEffects[s] != null)
            {
                if (slot.RelicId != 0)
                {
                    if (bullets.Count > 0)
                    {
                        lblSlotRelicEffects[s].Text = string.Join("\n", bullets);
                        lblSlotRelicEffects[s].ForeColor = Color.FromArgb(186, 230, 253);
                    }
                    else
                    {
                        lblSlotRelicEffects[s].Text = string.Format("ID: 0x{0:X8} (Color: {1})", slot.RelicId, slot.RelicColor);
                        lblSlotRelicEffects[s].ForeColor = Color.FromArgb(160, 170, 185);
                    }
                }
                else
                {
                    lblSlotRelicEffects[s].Text = "Click 'Change...' to equip a relic";
                    lblSlotRelicEffects[s].ForeColor = Color.FromArgb(130, 135, 150);
                }
            }

            if (slotPanels[s] != null && slotTooltip != null)
            {
                if (slot.RelicId != 0)
                {
                    string tip = string.Format(
                        "{0}\nColor: {1} • Type: {2} • ID: 0x{3:X8}\n\nEffects:\n{4}\n\nSlot {5} Requirement: {6} ({7})",
                        slot.RelicName,
                        slot.RelicColor,
                        slot.RelicType,
                        slot.RelicId,
                        (bullets.Count > 0 ? string.Join("\n", bullets) : "No special effects"),
                        s + 1,
                        slot.RequiredColor,
                        slot.IsDeepSlot ? "Deep Relic" : "Normal Relic");

                    slotTooltip.SetToolTip(slotPanels[s], tip);
                    if (lblSlotRelicNames[s] != null) slotTooltip.SetToolTip(lblSlotRelicNames[s], tip);
                    if (lblSlotRelicEffects[s] != null) slotTooltip.SetToolTip(lblSlotRelicEffects[s], tip);
                }
                else
                {
                    string emptyTip = string.Format("Slot {0} is empty. Click 'Change...' to equip a {1} {2}.", s + 1, slot.RequiredColor, slot.IsDeepSlot ? "Deep Relic" : "Normal Relic");
                    slotTooltip.SetToolTip(slotPanels[s], emptyTip);
                    if (lblSlotRelicNames[s] != null) slotTooltip.SetToolTip(lblSlotRelicNames[s], emptyTip);
                    if (lblSlotRelicEffects[s] != null) slotTooltip.SetToolTip(lblSlotRelicEffects[s], emptyTip);
                }
                slotPanels[s].Invalidate();
            }
        }

        private void ClearSlot(int s)
        {
            if (currentVesselDetail != null && s >= 0 && s < currentVesselDetail.Slots.Count)
            {
                currentVesselDetail.Slots[s].RelicId = 0;
                currentVesselDetail.Slots[s].RelicName = "[ Empty Slot ]";
                currentVesselDetail.Slots[s].RelicColor = "None";
                currentVesselDetail.Slots[s].RelicType = "";
                currentVesselDetail.Slots[s].EffectDescriptions.Clear();
                UpdateSlotCardUI(s);
            }
        }

        private List<Program.RelicEntry> GetOrExtractPlayerRelics()
        {
            string path = txtFilePath != null ? txtFilePath.Text.Trim('"', '\'') : "";
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                TryAutoDetectFile(false);
                path = txtFilePath != null ? txtFilePath.Text.Trim('"', '\'') : "";
            }
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                MessageBox.Show(this, "Please select or auto-detect your save file (NR0000.co2) first.", "Save File Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return new List<Program.RelicEntry>();
            }

            try
            {
                byte[] cleanData = Program.DecryptSaveFileReadOnly(path);
                return Program.ExtractRelics(cleanData, Program.ItemsDb);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to read relics from save file:\n" + ex.Message, "Read Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new List<Program.RelicEntry>();
            }
        }

        private void OpenRelicPicker(int slotIndex)
        {
            if (currentVesselDetail == null || slotIndex < 0 || slotIndex >= currentVesselDetail.Slots.Count) return;

            var relics = GetOrExtractPlayerRelics();
            if (relics == null || relics.Count == 0) return;

            var slot = currentVesselDetail.Slots[slotIndex];

            using (var picker = new RelicPickerDialog(
                currentVesselDetail.CharacterName,
                currentVesselDetail.VesselName,
                slotIndex + 1,
                slot.IsDeepSlot,
                slot.RequiredColor,
                relics,
                Program.EffectsDb))
            {
                if (picker.ShowDialog(this) == DialogResult.OK)
                {
                    if (picker.ClearSelected)
                    {
                        ClearSlot(slotIndex);
                    }
                    else if (picker.SelectedRelic != null)
                    {
                        var r = picker.SelectedRelic;
                        slot.RelicId = r.RelicId;
                        slot.RelicName = (r.Item != null) ? r.Item.NameEn : string.Format("Relic 0x{0:X8}", r.RelicId);
                        slot.RelicColor = (r.Item != null) ? r.Item.Color : "Unknown";
                        slot.RelicType = (r.Item != null) ? r.Item.Type : "";
                        slot.EffectDescriptions.Clear();
                        slot.EffectIds.Clear();

                        if (r.EffectIds != null)
                        {
                            foreach (var effId in r.EffectIds)
                            {
                                slot.EffectIds.Add(effId);
                                slot.EffectDescriptions.Add(Program.GetFullEffectDisplay(effId));
                            }
                        }
                        UpdateSlotCardUI(slotIndex);
                    }
                }
            }
        }

        private void BtnBuilderSave_Click(object sender, EventArgs e)
        {
            string savePath = txtFilePath.Text.Trim('"', '\'');
            if (string.IsNullOrEmpty(savePath) || !File.Exists(savePath))
            {
                MessageBox.Show(this, "Please select or auto-detect a valid save file first.", "Save File Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (currentVesselDetail == null) return;

            var relicIds = new List<uint>();
            for (int s = 0; s < 6; s++)
            {
                relicIds.Add(currentVesselDetail.Slots[s].RelicId);
            }

            try
            {
                string backupPath;
                bool setActive = chkBuilderSetActive.Checked;
                SaveRelicWriter.SaveVesselLoadout(
                    savePath,
                    currentVesselDetail.CharacterIndex,
                    currentVesselDetail.VesselTypeIndex,
                    setActive,
                    relicIds,
                    out backupPath);

                string activeMsg = setActive ? "Yes (Vessel set as active in-game)" : "No (Vessel saved to character profile)";
                string msg = string.Format(
                    "Build successfully saved to your save file!\n\n" +
                    "Character: {0}\n" +
                    "Vessel: {1}\n" +
                    "Equipped As Active: {2}\n\n" +
                    "🔒 AUTOMATIC BACKUP CREATED:\n{3}\n\n" +
                    "How to activate in-game:\n" +
                    "1. Return to the Main Menu in Nightreign.\n" +
                    "2. Select 'Continue' or 'Load Game'.\n\n" +
                    "(Other multiplayer co-op players do NOT need this app installed!)",
                    currentVesselDetail.CharacterName,
                    currentVesselDetail.VesselName,
                    activeMsg,
                    backupPath);

                MessageBox.Show(this, msg, "Build Saved Successfully", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadSelectedVesselData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to save build to save file:\n" + ex.Message, "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnBuilderPreset_Click(object sender, EventArgs e)
        {
            if (currentVesselDetail == null) return;

            string defaultName = currentVesselDetail.CharacterName + " - " + currentVesselDetail.VesselName + " Build";

            using (var prompt = new Form())
            {
                prompt.Width = 380;
                prompt.Height = 170;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.Text = "Save as Loadout Preset";
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.BackColor = Color.FromArgb(24, 26, 34);
                prompt.ForeColor = Color.White;

                Label lblPrompt = new Label { Left = 20, Top = 16, Text = "Preset Name:", AutoSize = true };
                TextBox txtName = new TextBox { Left = 20, Top = 40, Width = 320, Text = defaultName, BackColor = Color.FromArgb(16, 18, 24), ForeColor = Color.White };
                Button btnOk = new Button { Text = "Save", Left = 170, Width = 80, Top = 80, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(200, 160, 45), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat };
                Button btnCancel = new Button { Text = "Cancel", Left = 260, Width = 80, Top = 80, DialogResult = DialogResult.Cancel, BackColor = Color.FromArgb(40, 45, 55), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

                prompt.Controls.Add(lblPrompt);
                prompt.Controls.Add(txtName);
                prompt.Controls.Add(btnOk);
                prompt.Controls.Add(btnCancel);
                prompt.AcceptButton = btnOk;
                prompt.CancelButton = btnCancel;

                if (prompt.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(txtName.Text.Trim()))
                {
                    var relicIds = new List<uint>();
                    var relicNames = new List<string>();
                    var relicColors = new List<string>();

                    for (int s = 0; s < 6; s++)
                    {
                        var slot = currentVesselDetail.Slots[s];
                        if (slot.RelicId != 0)
                        {
                            relicIds.Add(slot.RelicId);
                            relicNames.Add(slot.RelicName);
                            relicColors.Add(slot.RelicColor);
                        }
                    }

                    var preset = new LoadoutPreset
                    {
                        Name = txtName.Text.Trim(),
                        CharacterName = currentVesselDetail.CharacterName,
                        VesselId = currentVesselDetail.VesselId,
                        VesselName = currentVesselDetail.VesselName,
                        RelicIds = relicIds,
                        RelicNames = relicNames,
                        RelicColors = relicColors,
                        Description = "Created via Vessel Builder on " + DateTime.Now.ToString("g")
                    };

                    PresetManager.AddOrUpdatePreset(preset);
                    RefreshMainPresets();
                    MessageBox.Show(this, "Preset '" + preset.Name + "' saved!\n\nYou can now swap to this build anytime using the F10 in-game overlay.", "Preset Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void BtnBuilderClearAll_Click(object sender, EventArgs e)
        {
            if (currentVesselDetail == null) return;
            var res = MessageBox.Show(this, "Are you sure you want to clear all 6 slots for this vessel in the builder?", "Clear All Slots", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.Yes)
            {
                for (int s = 0; s < 6; s++)
                {
                    ClearSlot(s);
                }
            }
        }

        private void InitializeOverlayView()
        {
            pnlOverlayView = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 22, 28),
                Padding = new Padding(18, 6, 18, 10),
                Visible = false
            };

            // Instruction Banner
            Panel pnlInfo = new Panel
            {
                Location = new Point(18, 6),
                Width = 906,
                Height = 65,
                BackColor = Color.FromArgb(26, 32, 44),
                Padding = new Padding(12, 6, 12, 6)
            };

            Label lblInfoTitle = new Label
            {
                Text = "⚡ SAVE-FILE LOADOUT PRESETS & IN-GAME OVERLAY",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 185, 65),
                AutoSize = true,
                UseMnemonic = false,
                Location = new Point(10, 6)
            };
            pnlInfo.Controls.Add(lblInfoTitle);

            Label lblInfoBody = new Label
            {
                Text = "1. Press F10 in-game to toggle HUD overlay.  2. Click 'Apply' to swap relics in save.\n3. Quit to Main Menu and click 'Continue' to reload. (Other co-op players do NOT need this mod installed!)",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(195, 205, 225),
                AutoSize = true,
                UseMnemonic = false,
                Location = new Point(10, 26)
            };
            pnlInfo.Controls.Add(lblInfoBody);
            pnlOverlayView.Controls.Add(pnlInfo);

            // Controls Bar
            Panel pnlBar = new Panel
            {
                Location = new Point(18, 77),
                Width = 906,
                Height = 36,
                BackColor = Color.FromArgb(16, 18, 24)
            };

            chkEnableHotkey = new CheckBox
            {
                Text = "Enable F10 Hotkey",
                Checked = true,
                ForeColor = Color.FromArgb(100, 220, 140),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(10, 6),
                Width = 145,
                Cursor = Cursors.Hand
            };
            pnlBar.Controls.Add(chkEnableHotkey);

            Label lblFilter = new Label
            {
                Text = "Filter:",
                ForeColor = Color.FromArgb(170, 175, 190),
                Location = new Point(165, 8),
                AutoSize = true
            };
            pnlBar.Controls.Add(lblFilter);

            cmbMainCharFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(210, 5),
                Width = 140,
                BackColor = Color.FromArgb(34, 38, 50),
                ForeColor = Color.White
            };
            cmbMainCharFilter.Items.Add("All");
            foreach (var ch in SaveRelicWriter.CHARACTERS)
            {
                cmbMainCharFilter.Items.Add(ch);
            }
            cmbMainCharFilter.SelectedIndex = 0;
            cmbMainCharFilter.SelectedIndexChanged += (s, e) => RefreshMainPresets();
            pnlBar.Controls.Add(cmbMainCharFilter);

            btnMainSnapshot = new Button
            {
                Text = "💾 Snapshot Current Relics",
                Location = new Point(365, 4),
                Width = 220,
                Height = 28,
                BackColor = Color.FromArgb(35, 55, 80),
                ForeColor = Color.FromArgb(180, 220, 255),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            btnMainSnapshot.FlatAppearance.BorderColor = Color.FromArgb(60, 95, 140);
            btnMainSnapshot.Click += BtnMainSnapshot_Click;
            pnlBar.Controls.Add(btnMainSnapshot);

            btnMainOpenOverlay = new Button
            {
                Text = "🎮 Open Overlay (F10)",
                Location = new Point(600, 4),
                Width = 190,
                Height = 28,
                BackColor = Color.FromArgb(200, 160, 45),
                ForeColor = Color.FromArgb(15, 15, 20),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            btnMainOpenOverlay.FlatAppearance.BorderColor = Color.FromArgb(235, 195, 80);
            btnMainOpenOverlay.Click += (s, e) => ToggleOverlay();
            pnlBar.Controls.Add(btnMainOpenOverlay);

            pnlOverlayView.Controls.Add(pnlBar);

            // Presets Cards Container
            pnlMainPresetCards = new FlowLayoutPanel
            {
                Location = new Point(18, 120),
                Width = 906,
                Height = 350,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.FromArgb(14, 16, 21),
                Padding = new Padding(8)
            };
            pnlOverlayView.Controls.Add(pnlMainPresetCards);

            lblOverlayStatus = new Label
            {
                Text = "Global Hotkey F10 active. You can toggle the overlay anytime while playing!",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(150, 210, 150),
                Location = new Point(18, 478),
                AutoSize = true
            };
            pnlOverlayView.Controls.Add(lblOverlayStatus);
        }

        private void AutoSeedDefaultPresets()
        {
            try
            {
                var existing = PresetManager.LoadPresets();
                if (existing.Count > 0) return;

                string path = txtFilePath.Text.Trim('"', '\'');
                if (!File.Exists(path)) return;

                var loadouts = SaveRelicWriter.ReadAllCharacterLoadouts(path, Program.ItemsDb);
                var defaults = new List<LoadoutPreset>();

                foreach (var lo in loadouts)
                {
                    if (lo.EquippedRelicIds.Count > 0)
                    {
                        defaults.Add(new LoadoutPreset
                        {
                            Name = lo.CharacterName + " - Active " + lo.ActiveVesselName,
                            CharacterName = lo.CharacterName,
                            VesselId = lo.ActiveVesselId,
                            VesselName = lo.ActiveVesselName,
                            RelicIds = new List<uint>(lo.EquippedRelicIds),
                            RelicNames = new List<string>(lo.EquippedRelicNames),
                            RelicColors = new List<string>(lo.EquippedRelicColors),
                            Description = "Default loadout captured from save."
                        });
                    }
                }

                if (defaults.Count > 0)
                {
                    PresetManager.SavePresets(defaults);
                }
            }
            catch { }
        }

        public void RefreshMainPresets()
        {
            if (pnlMainPresetCards == null) return;
            pnlMainPresetCards.SuspendLayout();
            pnlMainPresetCards.Controls.Clear();

            string filter = cmbMainCharFilter != null && cmbMainCharFilter.SelectedIndex > 0
                ? cmbMainCharFilter.SelectedItem.ToString()
                : null;

            var presets = PresetManager.GetPresetsForCharacter(filter);
            if (presets.Count == 0)
            {
                Label lblEmpty = new Label
                {
                    Text = "No presets saved yet. Click '💾 Snapshot Current Relics' above to capture your current in-game vessel build as a preset!",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                    ForeColor = Color.FromArgb(150, 155, 170),
                    Width = 860,
                    Height = 80,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                pnlMainPresetCards.Controls.Add(lblEmpty);
            }
            else
            {
                foreach (var preset in presets)
                {
                    Panel card = new Panel
                    {
                        Width = 880,
                        Height = 72,
                        BackColor = Color.FromArgb(24, 27, 36),
                        Margin = new Padding(0, 0, 0, 6),
                        Padding = new Padding(8)
                    };

                    card.Paint += (s, e) =>
                    {
                        using (var pen = new Pen(Color.FromArgb(48, 54, 70), 1))
                            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                    };

                    Label lblName = new Label
                    {
                        Text = preset.Name,
                        Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                        ForeColor = Color.FromArgb(225, 195, 95),
                        AutoSize = true,
                        UseMnemonic = false,
                        Location = new Point(10, 8)
                    };
                    card.Controls.Add(lblName);

                    Label lblSub = new Label
                    {
                        Text = string.Format("{0} • {1} ({2} Relics)", preset.CharacterName, preset.VesselName ?? "Active Vessel", preset.RelicIds.Count),
                        Font = new Font("Segoe UI", 8.5f),
                        ForeColor = Color.FromArgb(160, 165, 180),
                        AutoSize = true,
                        UseMnemonic = false,
                        Location = new Point(12, 34)
                    };
                    card.Controls.Add(lblSub);

                    // Relic indicators with full effect tooltips
                    FlowLayoutPanel pnlDots = new FlowLayoutPanel
                    {
                        Location = new Point(280, 18),
                        Size = new Size(390, 36),
                        FlowDirection = FlowDirection.LeftToRight,
                        WrapContents = false
                    };
                    for (int i = 0; i < 6; i++)
                    {
                        Color dotColor = Color.FromArgb(50, 55, 70);
                        string tip = "Slot " + (i + 1) + ": Empty";
                        if (i < preset.RelicIds.Count)
                        {
                            string col = (i < preset.RelicColors.Count) ? preset.RelicColors[i] : "";
                            if (col == "Red") dotColor = Color.FromArgb(220, 80, 80);
                            else if (col == "Blue") dotColor = Color.FromArgb(80, 150, 240);
                            else if (col == "Yellow") dotColor = Color.FromArgb(240, 200, 70);
                            else if (col == "Green") dotColor = Color.FromArgb(80, 210, 110);
                            else dotColor = Color.FromArgb(180, 180, 180);

                            string rName = (i < preset.RelicNames.Count) ? preset.RelicNames[i] : ("Relic " + (i + 1));
                            string rEff = (preset.RelicEffects != null && i < preset.RelicEffects.Count) ? preset.RelicEffects[i] : "";
                            tip = string.Format("Slot {0}: {1} ({2})\nEffects:\n{3}", i + 1, rName, col, string.IsNullOrEmpty(rEff) ? "Effects preserved in save" : rEff);
                        }
                        Label dot = new Label
                        {
                            Width = 24,
                            Height = 24,
                            Margin = new Padding(0, 4, 8, 0),
                            BackColor = dotColor,
                            Text = string.Format("{0}", i + 1),
                            ForeColor = Color.Black,
                            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                            TextAlign = ContentAlignment.MiddleCenter
                        };
                        var tt = new ToolTip();
                        tt.SetToolTip(dot, tip);
                        pnlDots.Controls.Add(dot);
                    }
                    card.Controls.Add(pnlDots);

                    // Apply button
                    Button btnApply = new Button
                    {
                        Text = "⚡ Apply",
                        Location = new Point(690, 18),
                        Width = 90,
                        Height = 34,
                        BackColor = Color.FromArgb(200, 160, 45),
                        ForeColor = Color.Black,
                        Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                        FlatStyle = FlatStyle.Flat,
                        UseMnemonic = false,
                        Cursor = Cursors.Hand
                    };
                    btnApply.FlatAppearance.BorderColor = Color.FromArgb(235, 195, 80);
                    btnApply.Click += (s, e) =>
                    {
                        string savePath = txtFilePath.Text.Trim('"', '\'');
                        if (!File.Exists(savePath))
                        {
                            MessageBox.Show(this, "Save file not found: " + savePath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        try
                        {
                            string bak;
                            SaveRelicWriter.ApplyPreset(savePath, preset, out bak);
                            try { SystemSounds.Asterisk.Play(); } catch { }
                            lblOverlayStatus.Text = string.Format("✅ Preset '{0}' applied! Backup: {1}", preset.Name, Path.GetFileName(bak));
                            lblOverlayStatus.ForeColor = Color.FromArgb(100, 240, 130);
                            MessageBox.Show(this, string.Format("Loadout '{0}' applied to save file!\n\nBackup created at:\n{1}\n\n👉 Now exit to the Main Menu and click 'Continue' or 'Load Game' to activate in-game.", preset.Name, bak), "Loadout Applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(this, "Failed to apply preset: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    };
                    card.Controls.Add(btnApply);

                    // Delete button
                    Button btnDel = new Button
                    {
                        Text = "🗑️",
                        Location = new Point(790, 18),
                        Width = 70,
                        Height = 34,
                        BackColor = Color.FromArgb(45, 30, 35),
                        ForeColor = Color.FromArgb(240, 140, 140),
                        Font = new Font("Segoe UI", 10f),
                        FlatStyle = FlatStyle.Flat,
                        UseMnemonic = false,
                        Cursor = Cursors.Hand
                    };
                    btnDel.FlatAppearance.BorderColor = Color.FromArgb(80, 45, 50);
                    btnDel.Click += (s, e) =>
                    {
                        var res = MessageBox.Show(this, string.Format("Are you sure you want to delete preset '{0}'?", preset.Name), "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (res == DialogResult.Yes)
                        {
                            PresetManager.DeletePreset(preset.Id);
                            RefreshMainPresets();
                        }
                    };
                    card.Controls.Add(btnDel);

                    pnlMainPresetCards.Controls.Add(card);
                }
            }
            pnlMainPresetCards.ResumeLayout();
        }

        private void BtnMainSnapshot_Click(object sender, EventArgs e)
        {
            string savePath = txtFilePath.Text.Trim('"', '\'');
            if (!File.Exists(savePath))
            {
                MessageBox.Show(this, "Save file not found. Please locate NR0000.co2 first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string charName = cmbMainCharFilter.SelectedIndex > 0 ? cmbMainCharFilter.SelectedItem.ToString() : "Wylder";

            using (var prompt = new Form())
            {
                prompt.Width = 380;
                prompt.Height = 170;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.Text = "Snapshot Current Relics";
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.BackColor = Color.FromArgb(24, 26, 34);
                prompt.ForeColor = Color.White;

                Label lblPrompt = new Label { Left = 20, Top = 16, Text = "Preset Name for " + charName + ":", AutoSize = true };
                TextBox txtName = new TextBox { Left = 20, Top = 40, Width = 320, Text = charName + " - " + DateTime.Now.ToString("MMM d Build"), BackColor = Color.FromArgb(36, 40, 52), ForeColor = Color.White };
                Button btnOk = new Button { Text = "Save", Left = 150, Width = 90, Top = 80, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(200, 160, 45), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat };
                Button btnCancel = new Button { Text = "Cancel", Left = 250, Width = 90, Top = 80, DialogResult = DialogResult.Cancel, BackColor = Color.FromArgb(50, 55, 68), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

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
                        var snapshot = SaveRelicWriter.SnapshotActiveLoadout(savePath, charName, txtName.Text.Trim(), Program.ItemsDb);
                        PresetManager.AddOrUpdatePreset(snapshot);
                        RefreshMainPresets();
                        try { SystemSounds.Asterisk.Play(); } catch { }
                        lblOverlayStatus.Text = "💾 Saved preset '" + snapshot.Name + "'!";
                        lblOverlayStatus.ForeColor = Color.FromArgb(100, 240, 130);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, "Failed to snapshot loadout: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void Form_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void Form_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                txtFilePath.Text = files[0];
                AutoSeedDefaultPresets();
                RefreshMainPresets();
                DoExtraction();
            }
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Select Nightreign Save File";
                ofd.Filter = "Nightreign Save (*.co2;*.sl2)|*.co2;*.sl2|All Files (*.*)|*.*";
                ofd.CheckFileExists = true;

                if (!string.IsNullOrEmpty(txtFilePath.Text) && File.Exists(txtFilePath.Text))
                {
                    ofd.InitialDirectory = Path.GetDirectoryName(txtFilePath.Text);
                    ofd.FileName = Path.GetFileName(txtFilePath.Text);
                }

                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    txtFilePath.Text = ofd.FileName;
                    AutoSeedDefaultPresets();
                    RefreshMainPresets();
                }
            }
        }

        private void TryAutoDetectFile(bool userClicked = false)
        {
            if (File.Exists("NR0000.co2"))
            {
                txtFilePath.Text = Path.GetFullPath("NR0000.co2");
                if (userClicked) MessageBox.Show(this, "Found NR0000.co2 in current folder!", "Auto-Detect", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string nrRoaming = Path.Combine(appData, "Nightreign");
            if (Directory.Exists(nrRoaming))
            {
                var files = Directory.GetFiles(nrRoaming, "NR0000.co2", SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    txtFilePath.Text = Path.GetFullPath(files[0]);
                    if (userClicked) MessageBox.Show(this, "Found Nightreign save file:\n" + files[0], "Auto-Detect", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            if (Directory.Exists(nrRoaming))
            {
                var files = Directory.GetFiles(nrRoaming, "NR0000.sl2", SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    txtFilePath.Text = Path.GetFullPath(files[0]);
                    if (userClicked) MessageBox.Show(this, "Found Nightreign save file:\n" + files[0], "Auto-Detect", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            if (userClicked)
            {
                MessageBox.Show(this, "Could not automatically locate NR0000.co2 in AppData or current directory.\nPlease use 'Browse...' to select your file.", "Auto-Detect", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnExtract_Click(object sender, EventArgs e)
        {
            DoExtraction();
        }

        private void DoExtraction()
        {
            string path = txtFilePath.Text.Trim('"', '\'');
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                MessageBox.Show(this, "Please select a valid save file path first.", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnExtract.Enabled = false;
            lblStatus.Text = "Extracting relics in read-only mode...";
            lblStatus.ForeColor = Color.FromArgb(240, 200, 80);

            try
            {
                var result = Program.ProcessExtraction(path);
                lastResult = result;

                var sb = new StringBuilder();
                sb.AppendLine("Nightreign Relic Extractor");
                sb.AppendLine("Input: " + result.InputFileName);
                sb.AppendLine("Relics found: " + result.RelicsFound);
                sb.AppendLine("CSV: " + Path.GetFileName(result.CsvPath));
                sb.AppendLine("Excel: " + Path.GetFileName(result.XlsxPath));
                sb.AppendLine("Original save modified: NO");
                sb.AppendLine();
                sb.AppendLine("Output Folder: " + result.SaveDirectory);
                sb.AppendLine("Extraction completed successfully.");

                txtResult.Text = sb.ToString();
                lblStatus.Text = "Extraction completed: " + result.RelicsFound + " relics extracted!";
                lblStatus.ForeColor = Color.FromArgb(120, 230, 120);

                btnOpenCsv.Enabled = true;
                btnOpenExcel.Enabled = true;
                btnOpenFolder.Enabled = true;
            }
            catch (Exception ex)
            {
                txtResult.Text = "Error during extraction:\r\n" + ex.Message;
                lblStatus.Text = "Error: " + ex.Message;
                lblStatus.ForeColor = Color.FromArgb(240, 100, 100);
                MessageBox.Show(this, "Failed to extract relics: " + ex.Message, "Extraction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnExtract.Enabled = true;
            }
        }

        public void TriggerExtraction()
        {
            DoExtraction();
        }
    }
}
