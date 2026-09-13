using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace NightreignRelicExtractor
{
    public class CharacterLoadoutInfo
    {
        public int CharacterIndex;
        public string CharacterName;
        public uint ActiveVesselId;
        public string ActiveVesselName;
        public List<uint> EquippedRelicIds;
        public List<string> EquippedRelicNames;
        public List<string> EquippedRelicColors;

        public CharacterLoadoutInfo()
        {
            EquippedRelicIds = new List<uint>();
            EquippedRelicNames = new List<string>();
            EquippedRelicColors = new List<string>();
        }
    }

    public static class SaveRelicWriter
    {
        public static readonly string[] CHARACTERS = new string[]
        {
            "Wylder", "Guardian", "Iron Eye", "Duchess", "Raider",
            "Revenant", "Recluse", "Executor", "Scholar", "Undertaker"
        };

        public static readonly string[] VESSEL_NAMES = new string[]
        {
            "Urn", "Goblet", "Chalice", "Soot-Covered Urn", "Sealed Urn", "Decrepit Goblet", "Forgotten Goblet"
        };

        public const int VESSEL_TABLE_START = 0x1BD3C;
        public const int VESSEL_STRIDE = 28;

        public static int GetCharacterIndex(string charName)
        {
            for (int i = 0; i < CHARACTERS.Length; i++)
            {
                if (string.Equals(CHARACTERS[i], charName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        public static string GetVesselName(uint vesselId)
        {
            int v = (int)(vesselId % 1000);
            if (v >= 0 && v < VESSEL_NAMES.Length)
                return VESSEL_NAMES[v];
            return "Vessel " + vesselId;
        }

        public static List<CharacterLoadoutInfo> ReadAllCharacterLoadouts(string saveFilePath, Dictionary<int, Program.ItemInfo> itemsDb)
        {
            var list = new List<CharacterLoadoutInfo>();
            if (!File.Exists(saveFilePath)) return list;

            byte[] raw = File.ReadAllBytes(saveFilePath);
            if (raw.Length < 64) return list;

            int pos = 64; // Entry 0
            int entrySize = BitConverter.ToInt32(raw, pos + 8);
            int entryOffset = BitConverter.ToInt32(raw, pos + 16);

            byte[] iv = new byte[16];
            Array.Copy(raw, entryOffset, iv, 0, 16);

            byte[] dec;
            using (var aes = new RijndaelManaged())
            {
                aes.KeySize = 128;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                aes.Key = Program.DS2_KEY;
                aes.IV = iv;
                using (var d = aes.CreateDecryptor())
                    dec = d.TransformFinalBlock(raw, entryOffset + 16, entrySize - 16);
            }

            byte[] cleanData = new byte[dec.Length - 4];
            Array.Copy(dec, 4, cleanData, 0, cleanData.Length);

            // Build map of relic instances from save
            var relicMap = new Dictionary<uint, Program.RelicEntry>();
            if (itemsDb != null)
            {
                try
                {
                    var allRelics = Program.ExtractRelics(cleanData, itemsDb);
                    foreach (var r in allRelics)
                    {
                        if (!relicMap.ContainsKey(r.RelicId))
                            relicMap[r.RelicId] = r;
                    }
                }
                catch { }
            }

            // Scan all 10 characters
            for (int c = 0; c < CHARACTERS.Length; c++)
            {
                uint marker = 0x0000FF01 + (uint)c;
                int markerPos = -1;
                for (int p = 0x1B800; p < 0x1BD30; p += 4)
                {
                    if (BitConverter.ToUInt32(cleanData, p) == marker)
                    {
                        markerPos = p;
                        break;
                    }
                }

                if (markerPos != -1)
                {
                    uint activeVid = BitConverter.ToUInt32(cleanData, markerPos + 4);
                    int v = (int)(activeVid % 1000);
                    int vesselOffset = VESSEL_TABLE_START + (c * 7 + v) * VESSEL_STRIDE;

                    var info = new CharacterLoadoutInfo
                    {
                        CharacterIndex = c,
                        CharacterName = CHARACTERS[c],
                        ActiveVesselId = activeVid,
                        ActiveVesselName = GetVesselName(activeVid)
                    };

                    for (int s = 0; s < 6; s++)
                    {
                        uint rId = BitConverter.ToUInt32(cleanData, vesselOffset + 4 + s * 4);
                        if (rId != 0)
                        {
                            info.EquippedRelicIds.Add(rId);

                            string itemName = string.Format("Relic 0x{0:X8}", rId);
                            string itemColor = "Unknown";

                            Program.RelicEntry rEntry;
                            if (relicMap.TryGetValue(rId, out rEntry) && rEntry.Item != null)
                            {
                                itemName = rEntry.Item.NameEn;
                                itemColor = rEntry.Item.Color;
                            }

                            info.EquippedRelicNames.Add(itemName);
                            info.EquippedRelicColors.Add(itemColor);
                        }
                    }

                    list.Add(info);
                }
            }

            return list;
        }

        public static void ApplyPreset(string saveFilePath, LoadoutPreset preset, out string backupPath)
        {
            if (!File.Exists(saveFilePath))
                throw new FileNotFoundException("Save file not found: " + saveFilePath);

            // 1. Create timestamped backup
            string dir = Path.GetDirectoryName(saveFilePath);
            string baseName = Path.GetFileName(saveFilePath);
            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            backupPath = Path.Combine(dir, string.Format("{0}.bak_{1}", baseName, timeStamp));
            File.Copy(saveFilePath, backupPath, true);

            // Also keep standard .bak
            string latestBak = Path.Combine(dir, baseName + ".bak");
            File.Copy(saveFilePath, latestBak, true);

            // 2. Read and decrypt Entry 0
            byte[] raw = File.ReadAllBytes(saveFilePath);
            if (raw.Length < 64)
                throw new InvalidDataException("Save file is invalid or too small.");

            int pos = 64; // Entry 0
            int entrySize = BitConverter.ToInt32(raw, pos + 8);
            int entryOffset = BitConverter.ToInt32(raw, pos + 16);

            byte[] iv = new byte[16];
            Array.Copy(raw, entryOffset, iv, 0, 16);

            byte[] dec;
            using (var aes = new RijndaelManaged())
            {
                aes.KeySize = 128;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                aes.Key = Program.DS2_KEY;
                aes.IV = iv;
                using (var d = aes.CreateDecryptor())
                    dec = d.TransformFinalBlock(raw, entryOffset + 16, entrySize - 16);
            }

            byte[] cleanData = new byte[dec.Length - 4];
            Array.Copy(dec, 4, cleanData, 0, cleanData.Length);

            // 3. Find character index
            int charIndex = GetCharacterIndex(preset.CharacterName);
            if (charIndex < 0)
                throw new ArgumentException("Unknown character name: " + preset.CharacterName);

            uint marker = 0x0000FF01 + (uint)charIndex;
            int markerPos = -1;
            for (int p = 0x1B800; p < 0x1BD30; p += 4)
            {
                if (BitConverter.ToUInt32(cleanData, p) == marker)
                {
                    markerPos = p;
                    break;
                }
            }

            if (markerPos == -1)
                throw new InvalidDataException("Character profile not found in save for " + preset.CharacterName);

            uint targetVesselId = preset.VesselId;
            if (targetVesselId == 0)
            {
                // Use character's current active vessel
                targetVesselId = BitConverter.ToUInt32(cleanData, markerPos + 4);
            }
            else
            {
                // Update active vessel pointer to this vessel
                BitConverter.GetBytes(targetVesselId).CopyTo(cleanData, markerPos + 4);
            }

            int vesselTypeIndex = (int)(targetVesselId % 1000);
            int vesselOffset = VESSEL_TABLE_START + (charIndex * 7 + vesselTypeIndex) * VESSEL_STRIDE;

            // 4. Write up to 6 relic slots
            for (int s = 0; s < 6; s++)
            {
                uint rId = 0;
                if (preset.RelicIds != null && s < preset.RelicIds.Count)
                {
                    rId = preset.RelicIds[s];
                }
                BitConverter.GetBytes(rId).CopyTo(cleanData, vesselOffset + 4 + s * 4);
            }

            // 5. Copy cleanData back to dec
            cleanData.CopyTo(dec, 4);

            // 6. Re-encrypt with AES-128-CBC
            byte[] reEnc;
            using (var aes = new RijndaelManaged())
            {
                aes.KeySize = 128;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                aes.Key = Program.DS2_KEY;
                aes.IV = iv;
                using (var enc = aes.CreateEncryptor())
                    reEnc = enc.TransformFinalBlock(dec, 0, dec.Length);
            }

            Array.Copy(reEnc, 0, raw, entryOffset + 16, reEnc.Length);

            // 7. Write safely to file
            string tempFile = saveFilePath + ".tmp";
            File.WriteAllBytes(tempFile, raw);
            File.Delete(saveFilePath);
            File.Move(tempFile, saveFilePath);
        }

        public static LoadoutPreset SnapshotActiveLoadout(string saveFilePath, string charName, string presetName, Dictionary<int, Program.ItemInfo> itemsDb)
        {
            var loadouts = ReadAllCharacterLoadouts(saveFilePath, itemsDb);
            var match = loadouts.Find(l => string.Equals(l.CharacterName, charName, StringComparison.OrdinalIgnoreCase));
            if (match == null)
                throw new Exception("Character loadout not found for " + charName);

            var preset = new LoadoutPreset
            {
                Name = string.IsNullOrEmpty(presetName) ? (charName + " - Saved Build") : presetName,
                CharacterName = match.CharacterName,
                VesselId = match.ActiveVesselId,
                VesselName = match.ActiveVesselName,
                RelicIds = new List<uint>(match.EquippedRelicIds),
                RelicNames = new List<string>(match.EquippedRelicNames),
                RelicColors = new List<string>(match.EquippedRelicColors),
                Description = "Captured from in-game equipped vessel on " + DateTime.Now.ToString("g")
            };

            return preset;
        }
    }
}
