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

    public class VesselSlotDetail
    {
        public int SlotIndex;
        public bool IsDeepSlot;
        public string RequiredColor;
        public uint RelicId;
        public string RelicName;
        public string RelicColor;
        public string RelicType;
        public List<string> EffectDescriptions;

        public VesselSlotDetail()
        {
            EffectDescriptions = new List<string>();
            RelicName = "[ Empty Slot ]";
            RelicColor = "None";
            RelicType = "";
            RequiredColor = "Any";
        }
    }

    public class VesselDetailInfo
    {
        public int CharacterIndex;
        public string CharacterName;
        public int VesselTypeIndex;
        public string VesselName;
        public uint VesselId;
        public bool IsActiveVessel;
        public uint ActiveVesselId;
        public string ActiveVesselName;
        public List<VesselSlotDetail> Slots;

        public VesselDetailInfo()
        {
            Slots = new List<VesselSlotDetail>();
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

        public static readonly string[][][] VESSEL_SLOT_COLORS = new string[][][]
        {
            // Wylder
            new string[][]
            {
                new string[] { "Red", "Red", "Blue", "Red", "Red", "Blue" }, // [0] Urn
                new string[] { "Yellow", "Green", "Green", "Yellow", "Green", "Green" }, // [1] Goblet
                new string[] { "Red", "Yellow", "Any", "Red", "Blue", "Green" }, // [2] Chalice
                new string[] { "Blue", "Blue", "Yellow", "Blue", "Blue", "Yellow" }, // [3] Soot-Covered Urn
                new string[] { "Blue", "Red", "Red", "Green", "Yellow", "Yellow" }, // [4] Sealed Urn
                new string[] { "Blue", "Green", "Yellow", "Blue", "Green", "Yellow" }, // [5] Decrepit Goblet
                new string[] { "Green", "Green", "Yellow", "Red", "Green", "Any" }, // [6] Forgotten Goblet
            },
            // Guardian
            new string[][]
            {
                new string[] { "Red", "Yellow", "Yellow", "Red", "Yellow", "Yellow" }, // [0] Urn
                new string[] { "Blue", "Blue", "Green", "Blue", "Blue", "Green" }, // [1] Goblet
                new string[] { "Blue", "Yellow", "Any", "Red", "Blue", "Yellow" }, // [2] Chalice
                new string[] { "Red", "Green", "Green", "Red", "Green", "Green" }, // [3] Soot-Covered Urn
                new string[] { "Yellow", "Yellow", "Red", "Green", "Green", "Yellow" }, // [4] Sealed Urn
                new string[] { "Yellow", "Green", "Green", "Yellow", "Green", "Green" }, // [5] Decrepit Goblet
                new string[] { "Green", "Blue", "Blue", "Red", "Blue", "Any" }, // [6] Forgotten Goblet
            },
            // Iron Eye
            new string[][]
            {
                new string[] { "Yellow", "Green", "Green", "Yellow", "Green", "Green" }, // [0] Urn
                new string[] { "Red", "Blue", "Yellow", "Red", "Blue", "Yellow" }, // [1] Goblet
                new string[] { "Red", "Green", "Any", "Red", "Red", "Green" }, // [2] Chalice
                new string[] { "Blue", "Yellow", "Yellow", "Blue", "Yellow", "Yellow" }, // [3] Soot-Covered Urn
                new string[] { "Green", "Green", "Yellow", "Blue", "Blue", "Red" }, // [4] Sealed Urn
                new string[] { "Blue", "Blue", "Green", "Blue", "Blue", "Green" }, // [5] Decrepit Goblet
                new string[] { "Yellow", "Blue", "Red", "Yellow", "Green", "Any" }, // [6] Forgotten Goblet
            },
            // Duchess
            new string[][]
            {
                new string[] { "Red", "Blue", "Blue", "Red", "Blue", "Blue" }, // [0] Urn
                new string[] { "Yellow", "Yellow", "Green", "Yellow", "Yellow", "Green" }, // [1] Goblet
                new string[] { "Blue", "Yellow", "Any", "Red", "Blue", "Yellow" }, // [2] Chalice
                new string[] { "Red", "Red", "Green", "Red", "Red", "Green" }, // [3] Soot-Covered Urn
                new string[] { "Blue", "Blue", "Red", "Green", "Green", "Yellow" }, // [4] Sealed Urn
                new string[] { "Blue", "Green", "Green", "Blue", "Green", "Green" }, // [5] Decrepit Goblet
                new string[] { "Green", "Yellow", "Yellow", "Red", "Green", "Any" }, // [6] Forgotten Goblet
            },
            // Raider
            new string[][]
            {
                new string[] { "Red", "Green", "Green", "Red", "Green", "Green" }, // [0] Urn
                new string[] { "Red", "Blue", "Yellow", "Red", "Blue", "Yellow" }, // [1] Goblet
                new string[] { "Red", "Red", "Any", "Red", "Yellow", "Yellow" }, // [2] Chalice
                new string[] { "Blue", "Blue", "Green", "Blue", "Blue", "Green" }, // [3] Soot-Covered Urn
                new string[] { "Green", "Green", "Red", "Yellow", "Blue", "Blue" }, // [4] Sealed Urn
                new string[] { "Yellow", "Yellow", "Green", "Yellow", "Yellow", "Green" }, // [5] Decrepit Goblet
                new string[] { "Yellow", "Blue", "Red", "Red", "Green", "Any" }, // [6] Forgotten Goblet
            },
            // Revenant
            new string[][]
            {
                new string[] { "Blue", "Blue", "Yellow", "Blue", "Blue", "Yellow" }, // [0] Urn
                new string[] { "Red", "Red", "Green", "Red", "Red", "Green" }, // [1] Goblet
                new string[] { "Blue", "Green", "Any", "Blue", "Yellow", "Green" }, // [2] Chalice
                new string[] { "Red", "Yellow", "Yellow", "Red", "Yellow", "Yellow" }, // [3] Soot-Covered Urn
                new string[] { "Yellow", "Blue", "Blue", "Green", "Green", "Red" }, // [4] Sealed Urn
                new string[] { "Red", "Red", "Yellow", "Red", "Red", "Yellow" }, // [5] Decrepit Goblet
                new string[] { "Green", "Red", "Red", "Yellow", "Green", "Any" }, // [6] Forgotten Goblet
            },
            // Recluse
            new string[][]
            {
                new string[] { "Blue", "Blue", "Green", "Blue", "Blue", "Green" }, // [0] Urn
                new string[] { "Red", "Blue", "Yellow", "Red", "Blue", "Yellow" }, // [1] Goblet
                new string[] { "Yellow", "Green", "Any", "Blue", "Green", "Green" }, // [2] Chalice
                new string[] { "Red", "Red", "Yellow", "Red", "Red", "Yellow" }, // [3] Soot-Covered Urn
                new string[] { "Green", "Blue", "Blue", "Yellow", "Yellow", "Red" }, // [4] Sealed Urn
                new string[] { "Red", "Red", "Blue", "Red", "Red", "Blue" }, // [5] Decrepit Goblet
                new string[] { "Yellow", "Blue", "Red", "Blue", "Green", "Any" }, // [6] Forgotten Goblet
            },
            // Executor
            new string[][]
            {
                new string[] { "Red", "Yellow", "Yellow", "Red", "Yellow", "Yellow" }, // [0] Urn
                new string[] { "Red", "Blue", "Green", "Red", "Blue", "Green" }, // [1] Goblet
                new string[] { "Blue", "Yellow", "Any", "Yellow", "Yellow", "Green" }, // [2] Chalice
                new string[] { "Red", "Red", "Blue", "Red", "Red", "Blue" }, // [3] Soot-Covered Urn
                new string[] { "Yellow", "Yellow", "Red", "Green", "Green", "Blue" }, // [4] Sealed Urn
                new string[] { "Red", "Red", "Yellow", "Red", "Red", "Yellow" }, // [5] Decrepit Goblet
                new string[] { "Green", "Blue", "Red", "Yellow", "Green", "Any" }, // [6] Forgotten Goblet
            },
            // Scholar
            new string[][]
            {
                new string[] { "Red", "Red", "Yellow", "Red", "Red", "Yellow" }, // [0] Urn
                new string[] { "Blue", "Green", "Yellow", "Blue", "Green", "Yellow" }, // [1] Goblet
                new string[] { "Red", "Blue", "Any", "Red", "Yellow", "Yellow" }, // [2] Chalice
                new string[] { "Blue", "Green", "Green", "Blue", "Green", "Green" }, // [3] Soot-Covered Urn
                new string[] { "Yellow", "Red", "Red", "Green", "Blue", "Blue" }, // [4] Sealed Urn
                new string[] { "Blue", "Blue", "Green", "Blue", "Blue", "Green" }, // [5] Decrepit Goblet
                new string[] { "Yellow", "Green", "Blue", "Red", "Green", "Any" }, // [6] Forgotten Goblet
            },
            // Undertaker
            new string[][]
            {
                new string[] { "Blue", "Green", "Green", "Blue", "Green", "Green" }, // [0] Urn
                new string[] { "Red", "Yellow", "Yellow", "Red", "Yellow", "Yellow" }, // [1] Goblet
                new string[] { "Green", "Yellow", "Any", "Blue", "Green", "Yellow" }, // [2] Chalice
                new string[] { "Red", "Red", "Blue", "Red", "Red", "Blue" }, // [3] Soot-Covered Urn
                new string[] { "Green", "Green", "Blue", "Yellow", "Red", "Red" }, // [4] Sealed Urn
                new string[] { "Red", "Blue", "Blue", "Red", "Blue", "Blue" }, // [5] Decrepit Goblet
                new string[] { "Yellow", "Yellow", "Red", "Blue", "Yellow", "Any" }, // [6] Forgotten Goblet
            },
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

        public static VesselDetailInfo ReadCharacterVesselDetail(
            string saveFilePath,
            int charIndex,
            int vesselTypeIndex,
            Dictionary<int, Program.ItemInfo> itemsDb,
            Dictionary<uint, Program.EffectInfo> effectsDb)
        {
            var info = new VesselDetailInfo
            {
                CharacterIndex = charIndex,
                CharacterName = (charIndex >= 0 && charIndex < CHARACTERS.Length) ? CHARACTERS[charIndex] : "Unknown",
                VesselTypeIndex = vesselTypeIndex,
                VesselName = (vesselTypeIndex >= 0 && vesselTypeIndex < VESSEL_NAMES.Length) ? VESSEL_NAMES[vesselTypeIndex] : ("Vessel " + vesselTypeIndex),
                VesselId = (uint)((charIndex + 1) * 1000 + vesselTypeIndex)
            };

            for (int s = 0; s < 6; s++)
            {
                string reqCol = "Any";
                if (charIndex >= 0 && charIndex < VESSEL_SLOT_COLORS.Length &&
                    vesselTypeIndex >= 0 && vesselTypeIndex < VESSEL_SLOT_COLORS[charIndex].Length)
                {
                    reqCol = VESSEL_SLOT_COLORS[charIndex][vesselTypeIndex][s];
                }
                info.Slots.Add(new VesselSlotDetail
                {
                    SlotIndex = s,
                    IsDeepSlot = (s >= 3),
                    RequiredColor = reqCol
                });
            }

            if (!File.Exists(saveFilePath)) return info;

            try
            {
                byte[] raw = File.ReadAllBytes(saveFilePath);
                if (raw.Length < 64) return info;

                int pos = 64;
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

                var relicMap = new Dictionary<uint, Program.RelicEntry>();
                if (itemsDb != null)
                {
                    var allRelics = Program.ExtractRelics(cleanData, itemsDb);
                    foreach (var r in allRelics)
                    {
                        if (!relicMap.ContainsKey(r.RelicId))
                            relicMap[r.RelicId] = r;
                    }
                }

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

                if (markerPos != -1)
                {
                    info.ActiveVesselId = BitConverter.ToUInt32(cleanData, markerPos + 4);
                    info.ActiveVesselName = GetVesselName(info.ActiveVesselId);
                    info.IsActiveVessel = (vesselTypeIndex == (int)(info.ActiveVesselId % 1000));
                }

                int vesselOffset = VESSEL_TABLE_START + (charIndex * 7 + vesselTypeIndex) * VESSEL_STRIDE;
                for (int s = 0; s < 6; s++)
                {
                    uint rId = BitConverter.ToUInt32(cleanData, vesselOffset + 4 + s * 4);
                    info.Slots[s].RelicId = rId;

                    if (rId != 0)
                    {
                        Program.RelicEntry rEntry;
                        if (relicMap.TryGetValue(rId, out rEntry) && rEntry.Item != null)
                        {
                            info.Slots[s].RelicName = rEntry.Item.NameEn;
                            info.Slots[s].RelicColor = rEntry.Item.Color;
                            info.Slots[s].RelicType = rEntry.Item.Type;

                            if (effectsDb != null && rEntry.EffectIds != null)
                            {
                                foreach (var effId in rEntry.EffectIds)
                                {
                                    Program.EffectInfo eff;
                                    if (effectsDb.TryGetValue(effId, out eff))
                                    {
                                        info.Slots[s].EffectDescriptions.Add(eff.NameEn);
                                    }
                                }
                            }
                        }
                        else
                        {
                            info.Slots[s].RelicName = string.Format("Relic 0x{0:X8}", rId);
                        }
                    }
                }
            }
            catch { }

            return info;
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

        public static void ApplyPreset(string saveFilePath, LoadoutPreset preset, out string backupPath, bool setActiveVessel = true)
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
            else if (setActiveVessel)
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

        public static void SaveVesselLoadout(
            string saveFilePath,
            int charIndex,
            int vesselTypeIndex,
            bool setActive,
            List<uint> relicIds,
            out string backupPath)
        {
            if (charIndex < 0 || charIndex >= CHARACTERS.Length)
                throw new ArgumentException("Invalid character index");
            if (vesselTypeIndex < 0 || vesselTypeIndex >= VESSEL_NAMES.Length)
                throw new ArgumentException("Invalid vessel index");

            uint vesselId = (uint)((charIndex + 1) * 1000 + vesselTypeIndex);
            var preset = new LoadoutPreset
            {
                Name = CHARACTERS[charIndex] + " - " + VESSEL_NAMES[vesselTypeIndex],
                CharacterName = CHARACTERS[charIndex],
                VesselId = vesselId,
                VesselName = VESSEL_NAMES[vesselTypeIndex],
                RelicIds = relicIds ?? new List<uint>()
            };

            ApplyPreset(saveFilePath, preset, out backupPath, setActiveVessel: setActive);
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
