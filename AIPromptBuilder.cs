using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace NightreignRelicExtractor
{
    public static class AIPromptBuilder
    {
        public static string BuildPrompt(
            string characterName,
            string vesselName,
            List<string> requiredSlotColors,
            List<Program.RelicEntry> inventoryRelics)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== ELDEN RING: NIGHTREIGN RELIC BUILD REQUEST ===");
            sb.AppendLine();
            sb.AppendLine("You are an expert Elden Ring: Nightreign theorycrafter and build optimizer.");
            sb.AppendLine(string.Format("Create 3 to 5 optimized, creative relic builds for {0} using their active vessel '{1}'.", characterName, vesselName));
            sb.AppendLine();
            sb.AppendLine("### RULES & CONSTRAINTS:");
            sb.AppendLine(string.Format("1. Character: {0}", characterName));
            sb.AppendLine(string.Format("2. Vessel: {0}", vesselName));
            sb.AppendLine("3. Each vessel has 3 Normal relic slots (Slots 1, 2, and 3). You MUST match the socket color requirement for each slot (or use Any if applicable):");
            if (requiredSlotColors != null)
            {
                for (int i = 0; i < Math.Min(3, requiredSlotColors.Count); i++)
                {
                    sb.AppendLine(string.Format("   - Slot {0} Socket Color: {1}", i + 1, requiredSlotColors[i]));
                }
            }
            sb.AppendLine("4. You must ONLY select relics from my unlocked inventory listed below. Do not invent non-existent relic IDs.");
            sb.AppendLine();
            sb.AppendLine("### REQUIRED BUILD CATEGORIES TO GENERATE:");
            sb.AppendLine("1. **meta_first**: Maximum DPS, ultimate scaling, optimal boss melting, top-tier synergy for high-difficulty runs.");
            sb.AppendLine("2. **fun_first**: Unique playstyle, elemental chaos, infinite stamina/arts, high-risk high-reward, glass cannon, or meme/thematic build.");
            sb.AppendLine("3. **support_utility**: Team survivability, damage negation, bolus/healing sharing, aggro control, fast revives.");
            sb.AppendLine();
            sb.AppendLine("Give each build an awesome, memorable name (e.g. 'Wylder - Infernal Firestarter', 'Guardian - Immortal Bastion', 'Duchess - Frostbite Weaver').");
            sb.AppendLine();
            sb.AppendLine("### MY UNLOCKED INVENTORY RELICS (ID, Name, Color, Effects):");

            if (inventoryRelics != null && inventoryRelics.Count > 0)
            {
                var seen = new HashSet<uint>();
                foreach (var r in inventoryRelics)
                {
                    if (r == null || r.Item == null || seen.Contains(r.RelicId)) continue;
                    seen.Add(r.RelicId);

                    string effs = "";
                    if (r.EffectIds != null && r.EffectIds.Count > 0)
                    {
                        var effList = new List<string>();
                        foreach (var eid in r.EffectIds) effList.Add(Program.GetFullEffectDisplay(eid));
                        effs = " [" + string.Join("; ", effList.ToArray()) + "]";
                    }

                    sb.AppendLine(string.Format("- ID: {0} | {1} ({2}){3}", r.RelicId, r.Item.NameEn, r.Item.Color, effs));
                }
            }
            else
            {
                sb.AppendLine("(Inventory relics not loaded — please extract from save file first.)");
            }

            sb.AppendLine();
            sb.AppendLine("### REQUIRED OUTPUT FORMAT:");
            sb.AppendLine("You MUST respond ONLY with a raw JSON array (no conversational filler). The app will parse your response directly:");
            sb.AppendLine("```json");
            sb.AppendLine("[");
            sb.AppendLine("  {");
            sb.AppendLine(string.Format("    \"name\": \"{0} - Infernal Firestorm (Meta)\",", characterName));
            sb.AppendLine(string.Format("    \"characterName\": \"{0}\",", characterName));
            sb.AppendLine(string.Format("    \"vesselName\": \"{0}\",", vesselName));
            sb.AppendLine("    \"category\": \"meta_first\",");
            sb.AppendLine("    \"description\": \"Maximizes fiery follow-ups with high attack scaling for rapid boss staggers.\",");
            sb.AppendLine("    \"relicIds\": [ 12345678, 23456789, 34567890 ]");
            sb.AppendLine("  }");
            sb.AppendLine("]");
            sb.AppendLine("```");

            return sb.ToString();
        }

        public static List<LoadoutPreset> ParseAIResponse(string rawText, out string errorMessage)
        {
            errorMessage = null;
            if (string.IsNullOrEmpty(rawText))
            {
                errorMessage = "Input text is empty.";
                return null;
            }

            try
            {
                string clean = rawText.Trim();
                var match = Regex.Match(clean, @"```(?:json)?\s*([\s\S]*?)\s*```", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    clean = match.Groups[1].Value.Trim();
                }

                var serializer = new JavaScriptSerializer();

                if (clean.StartsWith("["))
                {
                    var list = serializer.Deserialize<List<Dictionary<string, object>>>(clean);
                    if (list == null || list.Count == 0)
                    {
                        errorMessage = "Parsed JSON array is empty.";
                        return null;
                    }

                    var presets = new List<LoadoutPreset>();
                    foreach (var dict in list)
                    {
                        var p = ConvertDictToPreset(dict);
                        if (p != null) presets.Add(p);
                    }
                    return presets;
                }
                else if (clean.StartsWith("{"))
                {
                    var dict = serializer.Deserialize<Dictionary<string, object>>(clean);
                    var p = ConvertDictToPreset(dict);
                    if (p != null) return new List<LoadoutPreset> { p };
                }

                errorMessage = "Could not find valid JSON array or object in response.";
                return null;
            }
            catch (Exception ex)
            {
                errorMessage = "JSON Parse Error: " + ex.Message;
                return null;
            }
        }

        private static LoadoutPreset ConvertDictToPreset(Dictionary<string, object> dict)
        {
            if (dict == null) return null;

            var p = new LoadoutPreset();
            if (dict.ContainsKey("name") && dict["name"] != null) p.Name = dict["name"].ToString();
            else p.Name = "AI Build";

            if (dict.ContainsKey("characterName") && dict["characterName"] != null) p.CharacterName = dict["characterName"].ToString();
            else p.CharacterName = "Wylder";

            if (dict.ContainsKey("vesselName") && dict["vesselName"] != null) p.VesselName = dict["vesselName"].ToString();
            else p.VesselName = "Urn";

            if (dict.ContainsKey("description") && dict["description"] != null) p.Description = dict["description"].ToString();
            else p.Description = "";

            p.RelicIds = new List<uint>();
            if (dict.ContainsKey("relicIds"))
            {
                var arr = dict["relicIds"] as System.Collections.ArrayList;
                if (arr != null)
                {
                    foreach (var item in arr)
                    {
                        if (item != null)
                        {
                            uint rid;
                            if (uint.TryParse(item.ToString(), out rid))
                                p.RelicIds.Add(rid);
                        }
                    }
                }
            }

            return p;
        }
    }
}
