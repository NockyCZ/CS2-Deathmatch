
using CounterStrikeSharp.API.Core;

namespace DeathmatchAPI;

public class Preferences
{
    public class Categorie
    {
        public string Name { get; }
        public string MenuTitle { get; }
        public string MenuOption { get; }
        public bool UseLocalizer { get; }

        private static readonly List<Categorie> _categories = new();

        private Categorie(string name, string menuTitle, string menuOption, bool useLocalizer)
        {
            Name = name;
            MenuTitle = menuTitle;
            MenuOption = menuOption;
            UseLocalizer = useLocalizer;
        }

        public static Categorie? AddCustomCategory(string name, string menuTitle, string menuOption, bool useLocalizer = false)
        {
            if (_categories.Any(c => c.Name == name))
                return null;

            var newCategory = new Categorie(name, menuTitle, menuOption, useLocalizer);
            _categories.Add(newCategory);
            return newCategory;
        }

        public static void RemoveCategory(string categoryName)
        {
            var categoryToRemove = _categories.FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
            if (categoryToRemove == null)
                return;

            Menu.RemoveOptionsByCategory(categoryToRemove);
            _categories.Remove(categoryToRemove);
        }

        public static void RemoveCategory(Categorie category)
        {
            if (!_categories.Contains(category))
                return;

            Menu.RemoveOptionsByCategory(category);
            _categories.Remove(category);
        }

        public static IReadOnlyList<Categorie> GetAllCategories() => _categories.AsReadOnly();

        public static Categorie? GetCategoryByName(string name)
        {
            return _categories.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public static void RemoveAllCategories()
        {
            _categories.Clear();
        }

        public override string ToString() => Name;
    }

    public class PreferencesBooleanData
    {
        public required bool DefaultValue { get; set; }
        public List<string> CommandShortcuts { get; set; } = new();
    }

    public class PreferencesData
    {
        public required string DefaultValue { get; set; }
        public required List<string> Options { get; set; } = new();
        public List<string> CommandShortcuts { get; set; } = new();
    }

    public class Preference
    {
        public string Name { get; }
        public bool VipOnly { get; }
        public PreferencesBooleanData? BooleanData { get; }
        public PreferencesData? Data { get; }
        private static readonly List<Preference> _preferences = new();

        private Preference(string name, PreferencesBooleanData data, bool vipOnly = false)
        {
            VipOnly = vipOnly;
            Name = name;
            BooleanData = data;
        }

        private Preference(string name, PreferencesData data, bool vipOnly = false)
        {
            VipOnly = vipOnly;
            Name = name;
            Data = data;
        }

        public static Preference? RegisterPreference(string name, PreferencesBooleanData data, bool vipOnly = false)
        {
            if (_preferences.Any(o => o.Name == name))
                return null;

            var preference = new Preference(name, data, vipOnly);
            _preferences.Add(preference);

            return preference;
        }

        public static Preference? RegisterPreference(string name, PreferencesData data, bool vipOnly = false)
        {
            if (_preferences.Any(o => o.Name == name))
                return null;

            var preference = new Preference(name, data, vipOnly);
            _preferences.Add(preference);

            return preference;
        }

        public static Preference? GetPreferenceByName(string name)
        {
            return _preferences.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public static List<Preference> GetAllPreferences()
        {
            return _preferences;
        }

        public static void RemoveAllPreferences()
        {
            _preferences.Clear();
        }
    }

    public class Menu
    {
        public string? Name { get; }
        public string? Permission { get; }
        public Categorie? Category { get; }
        public Preference Preference { get; }
        public Action<CCSPlayerController, Menu>? OnChoose { get; }

        private static readonly List<Menu> _options = new();

        private Menu(Categorie? category, Preference preference)
        {
            Category = category;
            Preference = preference;
            OnChoose = null!;
        }

        private Menu(string name, Categorie? category, Action<CCSPlayerController, Menu> onChoose, string? permission = null)
        {
            Name = name;
            Category = category;
            Preference = null!;
            OnChoose = onChoose;
            Permission = permission;
        }

        public static void AddPreferenceOption(Categorie? category, Preference preference)
        {
            if (_options.Any(menu =>
                (menu.Category == category || (menu.Category == null && category == null)) &&
                menu.Preference == preference))
                return;

            var menu = new Menu(category, preference);
            _options.Add(menu);
        }

        public static void AddOption(string name, Categorie? category, Action<CCSPlayerController, Menu> onChoose, string? permission = null)
        {
            if (_options.Any(menu =>
                menu.Name == name &&
                (menu.Category == category || (menu.Category == null && category == null))))
                return;

            var menu = new Menu(name, category, onChoose, permission);
            _options.Add(menu);
        }

        public static void RemoveOptionsByCategory(Categorie category)
        {
            var optionsToRemove = _options.Where(o => o.Category == category).ToList();
            foreach (var option in optionsToRemove)
            {
                _options.Remove(option);
            }
        }

        public static void RemoveAllOptions()
        {
            _options.Clear();
        }

        public static IReadOnlyList<Menu> GetAllOptions() => _options.AsReadOnly();
        public static IReadOnlyList<Menu> GetOptionsByCategory(Categorie category) =>
            _options.Where(o => o.Category == category).ToList().AsReadOnly();
    }
}