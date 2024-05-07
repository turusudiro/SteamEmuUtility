using IniParser;
using PluginsCommon;
using System.Text;

namespace GoldbergCommon.Configs
{
    public static class ConfigsCommon
    {
        public static void SerializeConfigs(string value, string path, string section, string key)
        {
            var parser = new FileIniDataParser();
            parser.Parser.Configuration.CommentString = "#";
            parser.Parser.Configuration.AssigmentSpacer = "";
            if (!FileSystem.FileExists(path))
            {
                FileSystem.CreateFile(path, true);
            }
            var data = parser.ReadFile(path);
            if (!data.Sections.ContainsSection(section))
            {
                data.Sections.AddSection(section);
            }
            data.Sections[section][key] = value;
            try
            {
                parser.WriteFile(path, data, new UTF8Encoding());
            }
            catch { }
        }
        public static void SerializeConfigs(bool value, string path, string section, string key)
        {
            if (value)
            {
                SerializeConfigs("1", path, section, key);
            }
            else
            {
                SerializeConfigs("0", path, section, key);
            }
        }
        public static string GetValue(string path, string section, string key)
        {
            var parser = new FileIniDataParser();
            parser.Parser.Configuration.CommentString = "#";
            parser.Parser.Configuration.AssigmentSpacer = "";
            if (!FileSystem.FileExists(path))
            {
                return string.Empty;
            }
            var data = parser.ReadFile(path);
            if (!data.Sections.ContainsSection(section))
            {
                return string.Empty;
            }
            if (!data.Sections[section].ContainsKey(key))
            {
                return string.Empty;
            }
            return data.Sections[section][key];
        }
        public static object GetValue(string path, string section, string key, object defaultValue)
        {
            if (!FileSystem.FileExists(path))
            {
                return SetDefaultValue(path, section, key, defaultValue);
            }
            var parser = new FileIniDataParser();
            parser.Parser.Configuration.CommentString = "#";
            parser.Parser.Configuration.AssigmentSpacer = "";
            var data = parser.ReadFile(path);
            if (!data.Sections.ContainsSection(section) || !data.Sections[section].ContainsKey(key))
            {
                return SetDefaultValue(path, section, key, defaultValue);
            }
            if (defaultValue is int)
            {
                return data.Sections[section][key];
            }
            switch (data.Sections[section][key])
            {
                case "":
                    return string.Empty;
                case "0":
                    return false;
                case "1":
                    return true;
                default:
                    return data.Sections[section][key];
            }
        }
        private static object SetDefaultValue(string path, string section, string key, object defaultValue)
        {
            switch (defaultValue)
            {
                case bool value:
                    SerializeConfigs(value, path, section, key);
                    break;
                case string value:
                    SerializeConfigs(value, path, section, key);
                    break;
                default:
                    SerializeConfigs(defaultValue.ToString(), path, section, key);
                    break;
            }
            if (defaultValue is bool a)
            {
                if (a)
                {
                    return true;
                }
                else { return false; }
            }
            return defaultValue.ToString();
        }
    }
}
