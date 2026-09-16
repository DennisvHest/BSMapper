using System;
using Godot;

[GlobalClass]
public partial class ObjectTypeSelector : Node3D
{
    private ObjectEditPlane _objectEditPlane;
    private ObjectTypeSelectorUI _selectorUi;

    public override void _Ready()
    {
        _objectEditPlane = GetParent<ObjectEditPlane>()
            ?? throw new InvalidOperationException("ObjectTypeSelector must have an ObjectEditPlane parent");
        var viewportPanel = GetNode<Node>("ViewportPanel");
        _selectorUi = viewportPanel.Call("get_scene_instance").AsGodotObject() as ObjectTypeSelectorUI
            ?? throw new InvalidOperationException("ObjectTypeSelector viewport scene failed to initialize");
        _selectorUi.PlaceableSelected += OnPlaceableSelected;
        _selectorUi.BulkSelectionToggled += OnBulkSelectionToggled;
        _objectEditPlane.SelectedObjectTypeChanged += OnSelectedObjectTypeChanged;
        _objectEditPlane.BulkSelectionModeChanged += OnBulkSelectionModeChanged;
        UpdateSelectionVisuals(_objectEditPlane.SelectedObjectType);
        OnBulkSelectionModeChanged(_objectEditPlane.BulkSelectionModeEnabled);
    }

    public override void _ExitTree()
    {
        _selectorUi.PlaceableSelected -= OnPlaceableSelected;
        _selectorUi.BulkSelectionToggled -= OnBulkSelectionToggled;
        _objectEditPlane.SelectedObjectTypeChanged -= OnSelectedObjectTypeChanged;
        _objectEditPlane.BulkSelectionModeChanged -= OnBulkSelectionModeChanged;
    }

    private void OnBulkSelectionToggled()
    {
        _objectEditPlane.SetBulkSelectionModeEnabled(!_objectEditPlane.BulkSelectionModeEnabled);
    }

    private void OnBulkSelectionModeChanged(bool enabled)
    {
        _selectorUi?.SetBulkSelectionEnabled(enabled);
    }

    private void OnPlaceableSelected(ObjectEditPlane.PlaceableObjectType selectedObjectType)
    {
        _objectEditPlane.SetSelectedObjectType(selectedObjectType);
    }

    private void OnSelectedObjectTypeChanged(ObjectEditPlane.PlaceableObjectType selectedObjectType)
    {
        UpdateSelectionVisuals(selectedObjectType);
    }

    private void UpdateSelectionVisuals(ObjectEditPlane.PlaceableObjectType selectedObjectType)
    {
        _selectorUi?.SetSelectedObjectType(selectedObjectType);
    }
}