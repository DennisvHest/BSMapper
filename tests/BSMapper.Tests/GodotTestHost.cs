using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

/// <summary>
/// Runs assertions in Godot: scene objects and Godot collections require the engine's native runtime.
/// Each case gets a fresh process; the copied project keeps the editor and its build output untouched.
/// </summary>
public sealed class GodotTestHost : IDisposable
{
    private readonly string _project;
    private readonly string _godot;
    private Task _import;

    public GodotTestHost()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "BSMapper.csproj")))
            root = root.Parent;
        if (root == null)
            throw new InvalidOperationException("Cannot find the BSMapper source directory.");

        _godot = Environment.GetEnvironmentVariable("GODOT_BIN") ?? "godot";
        _project = Path.Combine(AppContext.BaseDirectory, "GodotHost", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_project);
        CopyResources(root.FullName, _project);

        var settings = Path.Combine(_project, "project.godot");
        File.WriteAllText(settings, File.ReadAllText(settings)
            .Replace("project/assembly_name=\"BSMapper\"", "project/assembly_name=\"BSMapper.Tests\"")
            .Replace("[autoload]", "[autoload]\nTestBootstrap=\"*res://tests/BSMapper.Tests/TestBootstrap.cs\"")
            .Replace("config/name=\"BSMapper\"", "config/name=\"BSMapper Tests\""));

        var assemblies = Path.Combine(_project, ".godot", "mono", "temp", "bin", "Debug");
        Directory.CreateDirectory(assemblies);
        foreach (var file in Directory.EnumerateFiles(AppContext.BaseDirectory))
            File.Copy(file, Path.Combine(assemblies, Path.GetFileName(file)), true);
    }

    public async Task Run(string suite, string scenario, ITestOutputHelper output)
    {
        // Import once to resolve scene UIDs, GDScript classes, fonts, textures and other assets.
        await (_import ??= Import(output));
        var scene = suite == "BulkSelectionChecks" ? "bulk_selection_checks" : "copy_paste_checks";
        var result = await Execute("--headless", "--xr-mode", "off", "--path", _project,
            $"res://tests/BSMapper.Tests/{scene}.tscn", "--", scenario);
        output.WriteLine(result.Log);
        Assert.True(result.ExitCode == 0, $"Godot exited with {result.ExitCode}.\n{result.Log}");
        Assert.Contains($"BSMAPPER_TEST_PASSED: {scenario} (", result.Log);
    }

    private async Task Import(ITestOutputHelper output)
    {
        var result = await Execute("--headless", "--xr-mode", "off", "--path", _project, "--import");
        output.WriteLine(result.Log);
        Assert.True(result.ExitCode == 0, $"Godot resource import failed.\n{result.Log}");
    }

    private async Task<(int ExitCode, string Log)> Execute(params string[] arguments)
    {
        var start = new ProcessStartInfo(_godot)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("Could not launch Godot. Set GODOT_BIN to the Godot .NET executable.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            throw new TimeoutException($"Godot timed out.\n{await stdout}\n{await stderr}");
        }
        return (process.ExitCode, $"{await stdout}\n{await stderr}");
    }

    private static void CopyResources(string source, string destination)
    {
        foreach (var file in Directory.EnumerateFiles(source))
        {
            var extension = Path.GetExtension(file);
            if (extension is ".csproj" or ".slnx" or ".sln")
                continue;
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
        }
        foreach (var directory in Directory.EnumerateDirectories(source))
        {
            var name = Path.GetFileName(directory);
            if (name.StartsWith('.') || name is "bin" or "obj" or "test_beatmaps")
                continue;
            var target = Path.Combine(destination, name);
            Directory.CreateDirectory(target);
            CopyResources(directory, target);
        }
    }

    public void Dispose() => Directory.Delete(_project, recursive: true);
}
