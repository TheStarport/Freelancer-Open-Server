using System;
using System.Collections.Generic;
using System.Linq;

namespace FLDataFile
{

    public class Section
    {
        public List<Setting> Settings = new List<Setting>();
        private readonly Dictionary<string, Setting> _setDictionary = new Dictionary<string, Setting>(); 
        public string Name { get; set; }

        public Section(string name)
        {
            Name = name;
        }

        public Section(string name, string bufBytes)
        {
            Name = name;

            var sets = bufBytes.Split(new []{'\n'},StringSplitOptions.RemoveEmptyEntries);

            foreach (var str in sets.Select(set => set.Split('=')).Where(str => str[0].Trim()[0] != ';'))
            {
                Settings.Add(new Setting(str[1],str[0].Trim()));
            }
        }


        /// <summary>
        /// Returns first setting with this name. Speeds up consequentive calls if used.
        /// </summary>
        /// <param name="name">Name of the setting.</param>
        /// <returns>Setting class.</returns>
        public Setting GetFirstOf(string name)
        {
            if (name == null) return null;
            if (!_setDictionary.ContainsKey(name))
                _setDictionary[name] = Settings.FirstOrDefault(a => a.Name == name);

            return _setDictionary[name];
        }


        /// <summary>
        /// Try to get first setting with the name defined and store it in setting value.
        /// </summary>
        /// <param name="name">Name of the setting</param>
        /// <param name="setting">Where to store the setting.</param>
        /// <returns>True if succeeds, otherwise false.</returns>
        public bool TryGetFirstOf(string name, out Setting setting)
        {
            setting = GetFirstOf(name);
            return setting != null;
        }

        /// <summary>
        /// Returns any setting found with the names provided. At least two names needed, obviously.
        /// </summary>
        /// <param name="settings">Setting names.</param>
        /// <returns>First setting found, or null if none found.</returns>
        public Setting GetAnySetting(params string[] settings)
        {
            return settings.Select(GetFirstOf).FirstOrDefault(set => set != null);
        }

        public bool ContainsAnyOf(params string[] settings)
        {
            return settings.Any(set => Settings.Any(w => w.Name == set));
        }

        /// <summary>
        /// Returns all settings matching the name specified.
        /// </summary>
        /// <param name="name">Setting name</param>
        /// <returns></returns>
        public IEnumerable<Setting> GetSettings(string name)
        {
            return Settings.Where(a => a.Name == name);
        } 
    }



}
