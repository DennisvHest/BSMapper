using Godot;

namespace BSMapper;

public static class Settings
{
    private const string SettingsFilePath = "user://settings.cfg";

    private static ConfigFile _configFile;

    public static ConfigFile LoadSettings()
    {
        _configFile = new ConfigFile();
        _configFile.Load(SettingsFilePath);

        return _configFile;
    }

    public static ConfigFile GetSettings()
    {
        return _configFile;
    }

    public static void SaveSettings()
    {
        _configFile.Save(SettingsFilePath);
    }
}
