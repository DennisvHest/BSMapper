using Godot;

namespace BSMapper;

public static class Settings
{
    private const string SettingsFilePath = "user://settings.cfg";

    public const string CustomLevelsFolder = "Beat Saber_Data/CustomLevels";
    public const string CustomWipLevelsFolder = "Beat Saber_Data/CustomWIPLevels";

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

    public static string WipBeatmapLocation => 
        _configFile.GetValue(SettingSections.Settings, SettingsKeys.BeatSaberInstallLocation, string.Empty)
            .ToString()
            .PathJoin(CustomWipLevelsFolder);
}

public static class SettingSections
{
    public const string Settings = "settings";
}

public static class SettingsKeys
{
    public const string BeatSaberInstallLocation = "install_location";
}
