using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace NightreignRelicExtractor
{
    public class LoadoutPreset
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string CharacterName { get; set; }
        public uint VesselId { get; set; }
        public string VesselName { get; set; }
        public List<uint> RelicIds { get; set; }
        public List<string> RelicNames { get; set; }
        public List<string> RelicColors { get; set; }
        public string Description { get; set; }

        public LoadoutPreset()
        {
            Id = Guid.NewGuid().ToString("N");
            RelicIds = new List<uint>();
            RelicNames = new List<string>();
            RelicColors = new List<string>();
            Description = "";
        }
    }

    public static class PresetManager
    {
        public static string GetPresetsFilePath()
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(exeDir, "presets.json");
        }

        public static List<LoadoutPreset> LoadPresets()
        {
            string path = GetPresetsFilePath();
            if (!File.Exists(path)) return new List<LoadoutPreset>();

            try
            {
                string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
                var serializer = new JavaScriptSerializer();
                return serializer.Deserialize<List<LoadoutPreset>>(json) ?? new List<LoadoutPreset>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading presets: " + ex.Message);
                return new List<LoadoutPreset>();
            }
        }

        public static void SavePresets(List<LoadoutPreset> presets)
        {
            string path = GetPresetsFilePath();
            try
            {
                var serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(presets);
                File.WriteAllText(path, json, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error saving presets: " + ex.Message);
            }
        }

        public static void AddOrUpdatePreset(LoadoutPreset preset)
        {
            var presets = LoadPresets();
            int idx = presets.FindIndex(p => p.Id == preset.Id);
            if (idx >= 0)
            {
                presets[idx] = preset;
            }
            else
            {
                presets.Add(preset);
            }
            SavePresets(presets);
        }

        public static void DeletePreset(string presetId)
        {
            var presets = LoadPresets();
            presets.RemoveAll(p => p.Id == presetId);
            SavePresets(presets);
        }

        public static List<LoadoutPreset> GetPresetsForCharacter(string charName)
        {
            var presets = LoadPresets();
            if (string.IsNullOrEmpty(charName) || charName == "All") return presets;
            return presets.FindAll(p => string.Equals(p.CharacterName, charName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
