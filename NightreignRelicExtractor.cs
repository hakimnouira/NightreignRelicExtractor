using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace NightreignRelicExtractor
{
    static class Program
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

        private static byte[] DecryptSaveFileReadOnly(string path)
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

        private static List<RelicEntry> ExtractRelics(byte[] cleanData, Dictionary<int, ItemInfo> itemsDb)
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

        private static string FormatEffectValue(EffectInfo info, uint rawEffId)
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
        private TextBox txtFilePath;
        private Button btnBrowse;
        private Button btnAutoDetect;
        private Button btnExtract;
        private TextBox txtResult;
        private Button btnOpenCsv;
        private Button btnOpenExcel;
        private Button btnOpenFolder;
        private Label lblStatus;

        private Program.ExtractionResult lastResult = null;

        public MainForm()
        {
            InitializeComponent();
            Program.EnsureDatabasesLoaded();
            TryAutoDetectFile();
        }

        private void InitializeComponent()
        {
            this.Text = "Nightreign Relic Extractor";
            this.Size = new Size(680, 520);
            this.MinimumSize = new Size(680, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(24, 26, 32);
            this.ForeColor = Color.FromArgb(235, 238, 245);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            this.AllowDrop = true;

            // Drag and drop anywhere on the form
            this.DragEnter += Form_DragEnter;
            this.DragDrop += Form_DragDrop;

            // Header Panel
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(18, 20, 26),
                Padding = new Padding(20, 12, 20, 10)
            };

            Label lblTitle = new Label
            {
                Text = "Nightreign Relic Extractor",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.FromArgb(212, 175, 55), // Golden Elden accent
                AutoSize = true,
                Location = new Point(18, 10)
            };

            Label lblSub = new Label
            {
                Text = "Extract complete relic inventory from .co2 save file to CSV and Excel (Strict Read-Only)",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(160, 165, 180),
                AutoSize = true,
                Location = new Point(20, 38)
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);
            this.Controls.Add(pnlHeader);

            // File selection group
            Label lblFilePrompt = new Label
            {
                Text = "Select Nightreign Save File (NR0000.co2 / NR0000.sl2):",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 225, 235),
                AutoSize = true,
                Location = new Point(20, 85)
            };
            this.Controls.Add(lblFilePrompt);
            lblFilePrompt.BringToFront();

            txtFilePath = new TextBox
            {
                Location = new Point(20, 110),
                Width = 425,
                Height = 28,
                BackColor = Color.FromArgb(34, 37, 46),
                ForeColor = Color.FromArgb(240, 240, 240),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };
            this.Controls.Add(txtFilePath);
            txtFilePath.BringToFront();

            btnBrowse = new Button
            {
                Text = "Browse...",
                Location = new Point(455, 109),
                Width = 85,
                Height = 29,
                BackColor = Color.FromArgb(45, 50, 62),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnBrowse.FlatAppearance.BorderColor = Color.FromArgb(70, 75, 90);
            btnBrowse.Click += BtnBrowse_Click;
            this.Controls.Add(btnBrowse);
            btnBrowse.BringToFront();

            btnAutoDetect = new Button
            {
                Text = "Auto-Detect",
                Location = new Point(547, 109),
                Width = 100,
                Height = 29,
                BackColor = Color.FromArgb(45, 50, 62),
                ForeColor = Color.FromArgb(175, 195, 255),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAutoDetect.FlatAppearance.BorderColor = Color.FromArgb(70, 75, 90);
            btnAutoDetect.Click += (s, e) => { TryAutoDetectFile(true); };
            this.Controls.Add(btnAutoDetect);
            btnAutoDetect.BringToFront();

            Label lblDragHint = new Label
            {
                Text = "Tip: You can also drag and drop your NR0000.co2 file directly onto this window.",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(140, 145, 160),
                AutoSize = true,
                Location = new Point(20, 143)
            };
            this.Controls.Add(lblDragHint);
            lblDragHint.BringToFront();

            // Extract Action Button
            btnExtract = new Button
            {
                Text = "Extract Relics",
                Location = new Point(20, 172),
                Width = 627,
                Height = 42,
                BackColor = Color.FromArgb(200, 160, 45), // Golden Elden Ring button
                ForeColor = Color.FromArgb(15, 15, 20),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnExtract.FlatAppearance.BorderColor = Color.FromArgb(235, 195, 80);
            btnExtract.Click += BtnExtract_Click;
            this.Controls.Add(btnExtract);
            btnExtract.BringToFront();

            // Status label
            lblStatus = new Label
            {
                Text = "Ready. Select a save file and click 'Extract Relics'.",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(150, 220, 150),
                AutoSize = true,
                Location = new Point(20, 222)
            };
            this.Controls.Add(lblStatus);
            lblStatus.BringToFront();

            // Result Log Box
            txtResult = new TextBox
            {
                Location = new Point(20, 245),
                Width = 627,
                Height = 160,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(15, 17, 22),
                ForeColor = Color.FromArgb(225, 230, 240),
                Font = new Font("Consolas", 10f),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(txtResult);
            txtResult.BringToFront();

            // Action Buttons Panel
            btnOpenCsv = new Button
            {
                Text = "Open CSV",
                Location = new Point(20, 418),
                Width = 120,
                Height = 34,
                BackColor = Color.FromArgb(40, 45, 55),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false,
                Cursor = Cursors.Hand
            };
            btnOpenCsv.FlatAppearance.BorderColor = Color.FromArgb(70, 75, 90);
            btnOpenCsv.Click += (s, e) => { if (lastResult != null && File.Exists(lastResult.CsvPath)) Process.Start(lastResult.CsvPath); };
            this.Controls.Add(btnOpenCsv);
            btnOpenCsv.BringToFront();

            btnOpenExcel = new Button
            {
                Text = "Open Excel",
                Location = new Point(150, 418),
                Width = 120,
                Height = 34,
                BackColor = Color.FromArgb(40, 45, 55),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false,
                Cursor = Cursors.Hand
            };
            btnOpenExcel.FlatAppearance.BorderColor = Color.FromArgb(70, 75, 90);
            btnOpenExcel.Click += (s, e) => { if (lastResult != null && File.Exists(lastResult.XlsxPath)) Process.Start(lastResult.XlsxPath); };
            this.Controls.Add(btnOpenExcel);
            btnOpenExcel.BringToFront();

            btnOpenFolder = new Button
            {
                Text = "Open Output Folder",
                Location = new Point(280, 418),
                Width = 160,
                Height = 34,
                BackColor = Color.FromArgb(40, 45, 55),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false,
                Cursor = Cursors.Hand
            };
            btnOpenFolder.FlatAppearance.BorderColor = Color.FromArgb(70, 75, 90);
            btnOpenFolder.Click += (s, e) => { if (lastResult != null && Directory.Exists(lastResult.SaveDirectory)) Process.Start("explorer.exe", lastResult.SaveDirectory); };
            this.Controls.Add(btnOpenFolder);
            btnOpenFolder.BringToFront();
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
                }
            }
        }

        private void TryAutoDetectFile(bool userClicked = false)
        {
            // 1. Current directory
            if (File.Exists("NR0000.co2"))
            {
                txtFilePath.Text = Path.GetFullPath("NR0000.co2");
                if (userClicked) MessageBox.Show(this, "Found NR0000.co2 in current folder!", "Auto-Detect", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 2. Nightreign AppData directory
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

            // 3. Fallback check for .sl2
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
