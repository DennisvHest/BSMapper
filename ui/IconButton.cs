using Godot;
using System;

[Tool]
public partial class IconButton : Button
{
    private string _labelText = string.Empty;
    private string _iconName = "circle-question";
    private int _iconSize = 16;
    private int _horizontalPadding = 12;
    private int _verticalPadding = 6;
    private int _separation = 8;

    private HBoxContainer _content;
    private Label _icon;
    private Label _label;

    [Export]
    public string LabelText
    {
        get => _labelText;
        set
        {
            _labelText = value;
            ApplyConfiguration();
        }
    }

    [Export]
    public string IconName
    {
        get => _iconName;
        set
        {
            _iconName = value;
            ApplyConfiguration();
        }
    }

    [Export(PropertyHint.Range, "1,128,1")]
    public int IconSize
    {
        get => _iconSize;
        set
        {
            _iconSize = value;
            ApplyConfiguration();
        }
    }

    [Export(PropertyHint.Range, "0,64,1")]
    public int HorizontalPadding
    {
        get => _horizontalPadding;
        set
        {
            _horizontalPadding = value;
            ApplyConfiguration();
        }
    }

    [Export(PropertyHint.Range, "0,64,1")]
    public int VerticalPadding
    {
        get => _verticalPadding;
        set
        {
            _verticalPadding = value;
            ApplyConfiguration();
        }
    }

    [Export(PropertyHint.Range, "0,64,1")]
    public int Separation
    {
        get => _separation;
        set
        {
            _separation = value;
            ApplyConfiguration();
        }
    }

    public override void _Ready()
    {
        _content = GetNode<HBoxContainer>("%Content");
        _icon = GetNode<Label>("%Icon");
        _label = GetNode<Label>("%Label");

        _content.MinimumSizeChanged += UpdateMinimumSize;
        ApplyConfiguration();
    }

    private void ApplyConfiguration()
    {
        if (_content is null)
        {
            return;
        }

        _label.Text = _labelText;
        _icon.Set("icon_name", _iconName);
        _icon.Set("icon_size", _iconSize);
        _content.AddThemeConstantOverride("separation", _separation);

        var margin = GetNode<MarginContainer>("%Margin");
        margin.AddThemeConstantOverride("margin_left", _horizontalPadding);
        margin.AddThemeConstantOverride("margin_right", _horizontalPadding);
        margin.AddThemeConstantOverride("margin_top", _verticalPadding);
        margin.AddThemeConstantOverride("margin_bottom", _verticalPadding);

        UpdateMinimumSize();
    }

    private void UpdateMinimumSize()
    {
        var contentSize = _content.GetCombinedMinimumSize();
        var styleMinimumSize = GetThemeStylebox("normal").GetMinimumSize();

        CustomMinimumSize = new Vector2(
            Math.Max(contentSize.X + (2 * _horizontalPadding), styleMinimumSize.X),
            Math.Max(contentSize.Y + (2 * _verticalPadding), styleMinimumSize.Y));
    }
}
