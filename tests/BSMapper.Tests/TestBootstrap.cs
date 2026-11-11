using Godot;
using Godot.Bridge;

/// <summary>Registers application scripts before Godot instantiates the application's autoloads.</summary>
public partial class TestBootstrap : Node
{
    public TestBootstrap() => ScriptManagerBridge.LookupScriptsInAssembly(typeof(Editor).Assembly);
}
