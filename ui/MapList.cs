using Godot;
using System.Collections.Generic;

public partial class MapList : VBoxContainer
{
    private const int ItemsPerFrame = 50;

    [Signal]
    public delegate void MapSelectedEventHandler(string infoPath);

    private readonly Queue<(string FolderName, string MapFolder, string InfoPath)> _pendingItems = new();
    private ItemList _items;

    public override void _Ready()
    {
        _items = GetNode<ItemList>("%Items");
        _items.ItemSelected += OnItemSelected;
    }

    public override void _Process(double delta)
    {
        for (var i = 0; i < ItemsPerFrame && _pendingItems.Count > 0; i++)
        {
            AddItem(_pendingItems.Dequeue());
        }
    }

    public void Refresh(string mapsLocation)
    {
        _pendingItems.Clear();
        _items.Clear();

        if (!DirAccess.DirExistsAbsolute(mapsLocation))
        {
            return;
        }

        using var directory = DirAccess.Open(mapsLocation);
        if (directory is null)
        {
            return;
        }

        foreach (var folderName in directory.GetDirectories())
        {
            var mapFolder = mapsLocation.PathJoin(folderName);
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
        var index = _items.AddItem(title, LoadCoverImage(item.MapFolder, coverImageFileName));
        _items.SetItemMetadata(index, item.InfoPath);
    }

    private void OnItemSelected(long index)
    {
        EmitSignal(SignalName.MapSelected, _items.GetItemMetadata((int)index).AsString());
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
}
