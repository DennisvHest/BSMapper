using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

public class EditorTests : IClassFixture<GodotTestHost>
{
    private readonly GodotTestHost _host;
    private readonly ITestOutputHelper _output;

    public EditorTests(GodotTestHost host, ITestOutputHelper output)
    {
        _host = host;
        _output = output;
    }

    [Theory]
    [InlineData("BulkSelectionChecks", "CheckRanges")]
    [InlineData("BulkSelectionChecks", "CheckAdditiveSelection")]
    [InlineData("BulkSelectionChecks", "CheckSelector")]
    [InlineData("BulkSelectionChecks", "CheckSelectorClicks")]
    [InlineData("BulkSelectionChecks", "CheckSettingsToggles")]
    [InlineData("CopyPasteChecks", "CheckClipboardData")]
    [InlineData("CopyPasteChecks", "CheckPanelControls")]
    [InlineData("CopyPasteChecks", "CheckEditorIntegration")]
    [InlineData("CopyPasteChecks", "CheckAutosave")]
    public Task EditorBehavior(string suite, string scenario) => _host.Run(suite, scenario, _output);
}
