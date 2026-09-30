using BSMapper;
using Godot;
using System;
using System.Collections.Generic;

public partial class MapList : VBoxContainer
{
    private const int ItemsPerFrame = 1;

    [Signal]
    public delegate void MapSelectedEventHandler(string infoPath);

    public event Action NewMapRequested;

    private readonly Queue<(string FolderName, string MapFolder, string InfoPath)> _pendingItems = new();
    private ItemList _wipItems;
    private ItemList _customLevels;
    private TabContainer _levelTabs;

    private string _currentBeatMapLocation => _levelTabs.CurrentTab == (int)LevelTabs.WIPLevels
        ? Settings.WipBeatmapLocation
        : Settings.CustomLevelsLocation;

    private ItemList _currentItemList => _levelTabs.CurrentTab == (int)LevelTabs.WIPLevels ? _wipItems : _customLevels;

    public override void _Ready()
    {
        _levelTabs = GetNode<TabContainer>("%LevelTabs");
        _levelTabs.TabChanged += _ => Refresh();

        _wipItems = GetNode<ItemList>("%WIP Levels");
        _wipItems.ItemSelected += OnItemSelected;

        _customLevels = GetNode<ItemList>("%Custom Levels");
        _customLevels.ItemSelected += OnItemSelected;

        GetNode<Button>("%NewMap").Pressed += () => NewMapRequested?.Invoke();
    }

    public override void _Process(double delta)
    {
        for (var i = 0; i < ItemsPerFrame && _pendingItems.Count > 0; i++)
        {
            AddItem(_pendingItems.Dequeue());
        }
    }

    public void Refresh()
    {
        _pendingItems.Clear();
        _wipItems.Clear();

        if (!DirAccess.DirExistsAbsolute(_currentBeatMapLocation))
        {
            return;
        }

        using var directory = DirAccess.Open(_currentBeatMapLocation);
        if (directory is null)
        {
            return;
        }

        foreach (var folderName in directory.GetDirectories())
        {
            var mapFolder = _currentBeatMapLocation.PathJoin(folderName);
            var infoPath = FindInfoPath(mapFolder);
            if (!string.IsNullOrEmpty(infoPath))
            {
                _pendingItems.Enqueue((folderName, mapFolder, infoPath));
            }
        }
    }

    private void AddItem((string FolderName, string MapFolder, string InfoPath) item)
    {
        var songName = item.FolderName;
        var coverImageFileName = string.Empty;
        var json = new Json();
        if (json.Parse(FileAccess.GetFileAsString(item.InfoPath)) == Error.Ok)
        {
            var data = json.Data.AsGodotDictionary();
            if (data.TryGetValue("_songName", out var songNameValue)
                && !string.IsNullOrWhiteSpace(songNameValue.AsString()))
            {
                songName = songNameValue.AsString();
            }

            if (data.TryGetValue("_coverImageFilename", out var coverValue))
            {
                coverImageFileName = coverValue.AsString();
            }
        }

        const int maxTitleLength = 100;
        var title = songName.Length > maxTitleLength ? songName[..maxTitleLength] + "..." : songName;
        var index = _currentItemList.AddItem(title, LoadCoverImage(item.MapFolder, coverImageFileName));
        _currentItemList.SetItemMetadata(index, item.InfoPath);
    }

    private void OnItemSelected(long index)
    {
        EmitSignal(SignalName.MapSelected, _currentItemList.GetItemMetadata((int)index).AsString());
    }

    private static string FindInfoPath(string mapFolder)
    {
        var infoPath = mapFolder.PathJoin("info.dat");
        if (FileAccess.FileExists(infoPath))
        {
            return infoPath;
        }

        infoPath = mapFolder.PathJoin("Info.dat");
        return FileAccess.FileExists(infoPath) ? infoPath : string.Empty;
    }

    private static Texture2D LoadCoverImage(string mapFolder, string coverImageFileName)
    {
        var coverPath = string.IsNullOrWhiteSpace(coverImageFileName)
            ? "res://icon.svg"
            : mapFolder.PathJoin(coverImageFileName);
        var image = Image.LoadFromFile(coverPath) ?? Image.LoadFromFile("res://icon.svg");
        return image is null ? null : ImageTexture.CreateFromImage(image);
    }

    private enum LevelTabs
    {
        WIPLevels = 0,
        CustomLevels = 1,
    }
}
