using System;
using System.Collections.Generic;

namespace NightreignRelicExtractor
{
    public static class Localization
    {
        public enum Language
        {
            English,
            French,
            Arabic
        }

        public static Language CurrentLanguage = Language.English;

        private static Dictionary<string, Dictionary<Language, string>> Strings = null;

        private static void Add(string key, string en, string fr, string ar)
        {
            Strings[key] = new Dictionary<Language, string>
            {
                { Language.English, en },
                { Language.French, fr },
                { Language.Arabic, ar }
            };
        }

        private static void EnsureInitialized()
        {
            if (Strings != null) return;
            Strings = new Dictionary<string, Dictionary<Language, string>>();

            // Header
            Add("AppTitle", 
                "Nightreign Relic Extractor & Loadout Switcher", 
                "Extracteur de Reliques & Gestionnaire Nightreign", 
                "مستخرج الآثار ومبدل العتاد لـ Nightreign");

            Add("AppSubtitle", 
                "Build & switch relic loadouts for your vessel • F10 to toggle overlay in-game", 
                "Créez et changez de calice/reliques • F10 pour ouvrir l'overlay en jeu", 
                "اصنع وبدل عتاد آثار الوعاء • اضغط F10 لفتح الواجهة داخل اللعبة");

            Add("QuickOverlay", 
                "In-Game Overlay (F10)", 
                "Overlay en Jeu (F10)", 
                "واجهة اللعبة (F10)");

            // Tabs
            Add("TabExtract", 
                "Relic Extractor", 
                "Extracteur de Reliques", 
                "مستخرج الآثار");

            Add("TabBuilder", 
                "Vessel Builder", 
                "Constructeur de Calice", 
                "صانع الأواني");

            Add("TabOverlay", 
                "Loadouts & Overlay (F10)", 
                "Builds & Overlay (F10)", 
                "العتاد والواجهة (F10)");

            // File Bar
            Add("SaveFilePrompt", 
                "Nightreign Save File (NR0000.co2):", 
                "Fichier de Sauvegarde Nightreign (NR0000.co2) :", 
                "ملف حفظ Nightreign (NR0000.co2):");

            Add("Browse", 
                "Browse...", 
                "Parcourir...", 
                "استعراض...");

            Add("AutoDetect", 
                "Auto-Detect", 
                "Auto-Détection", 
                "كشف تلقائي");

            Add("WrongPathWarning", 
                "Wrong file? The game only reads from the Steam-ID subfolder. Click Auto-Detect.", 
                "Mauvais fichier ? Le jeu lit uniquement le sous-dossier Steam-ID. Cliquez sur Auto-Détection.", 
                "ملف خاطئ؟ اللعبة تقرأ فقط من المجلد الفرعي لمعرف Steam. اضغط كشف تلقائي.");

            // Vessel Builder Controls
            Add("Character", 
                "Character:", 
                "Personnage :", 
                "الشخصية:");

            Add("VesselCup", 
                "Vessel / Cup:", 
                "Calice / Coupe :", 
                "الوعاء / الكأس:");

            Add("Reload", 
                "Reload", 
                "Recharger", 
                "إعادة تحميل");

            Add("SetActiveCheckbox", 
                "Set as Active In-Game Vessel when saving", 
                "Définir comme Calice Actif lors de la sauvegarde", 
                "تعيين كوعاء نشط داخل اللعبة عند الحفظ");

            Add("EquipAndSave", 
                "Equip & Save to Save File", 
                "Équiper & Sauvegarder dans le Fichier", 
                "تجهيز وحفظ في ملف الحفظ");

            Add("SaveAsPreset", 
                "Save as Preset", 
                "Sauvegarder comme Préréglage", 
                "حفظ كقالب");

            Add("BrowsePresets", 
                "Browse Presets", 
                "Parcourir les Préréglages", 
                "تصفح القوالب");

            Add("CopyAIPrompt", 
                "Copy AI Prompt", 
                "Copier Prompt IA", 
                "نسخ مطالبة الذكاء الاصطناعي");

            Add("ImportAIBuild", 
                "Import AI Build", 
                "Importer Build IA", 
                "استيراد بناء الذكاء الاصطناعي");

            Add("ClearAllSlots", 
                "Clear All 6 Slots", 
                "Vider les 6 Emplacements", 
                "إفراغ جميع الخانات الـ 6");

            Add("BackupNotice", 
                "Automatic backup (.bak timestamped, max 4 kept) is always created before every save modification.", 
                "Une sauvegarde automatique (.bak horodatée, max 4 conservées) est toujours créée avant chaque modification.", 
                "يتم دائمًا إنشاء نسخة احتياطية تلقائية (.bak مؤرخة، والاحتفاظ بـ 4 كحد أقصى) قبل أي تعديل.");

            // Slots
            Add("NormalSlotsTitle", 
                "NORMAL RELIC SLOTS (Slots 1 - 3)", 
                "EMPLACEMENTS DE RELIQUES NORMAUX (1 - 3)", 
                "خانات الآثار العادية (الخانات 1 - 3)");

            Add("DeepSlotsTitle", 
                "DEEP RELIC SLOTS (Slots 4 - 6)", 
                "EMPLACEMENTS DE RELIQUES PROFONDS (4 - 6)", 
                "خانات الآثار العميقة (الخانات 4 - 6)");

            Add("Change", 
                "Change...", 
                "Changer...", 
                "تغيير...");

            Add("Clear", 
                "Clear", 
                "Effacer", 
                "مسح");

            Add("EmptySlot", 
                "[ Empty Slot ]", 
                "[ Emplacement Vide ]", 
                "[ خانة فارغة ]");

            Add("ClickToEquip", 
                "Click 'Change...' to equip a relic", 
                "Cliquez sur 'Changer...' pour équiper une relique", 
                "اضغط 'تغيير...' لتجهيز أثر");

            // Overlay / Presets View
            Add("Filter", 
                "Filter:", 
                "Filtrer :", 
                "تصفية:");

            Add("EnableHotkey", 
                "Enable F10 Hotkey", 
                "Activer la touche F10", 
                "تفعيل المفتاح F10");

            Add("SnapshotCurrent", 
                "Snapshot Current Relics", 
                "Capturer les Reliques Actuelles", 
                "التقاط عتاد الآثار الحالي");

            Add("Apply", 
                "Apply", 
                "Appliquer", 
                "تطبيق");

            Add("Delete", 
                "Delete", 
                "Supprimer", 
                "حذف");

            Add("QuitMenuAlert", 
                "Quit to the Main Menu and click 'Continue' to reload with your new relics.", 
                "Retournez au Menu Principal et cliquez sur 'Continuer' pour charger vos nouvelles reliques.", 
                "اخرج إلى القائمة الرئيسية واضغط 'متابعة' لتحميل آثارك الجديدة.");
        }

        public static string Get(string key)
        {
            EnsureInitialized();
            Dictionary<Language, string> langDict;
            if (Strings.TryGetValue(key, out langDict))
            {
                string val;
                if (langDict.TryGetValue(CurrentLanguage, out val))
                    return val;
                string fallback;
                if (langDict.TryGetValue(Language.English, out fallback))
                    return fallback;
            }
            return key;
        }

        public static void SetLanguage(Language lang)
        {
            CurrentLanguage = lang;
        }
    }
}
