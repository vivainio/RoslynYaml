
using Microsoft.Build.Locator;
using Spectre.Console.Cli;

MSBuildLocator.RegisterDefaults();



var app = new CommandApp();
app.Configure(config =>
{
    config.AddCommand<EmitCommand>("emit");
    config.PropagateExceptions();
});
app.Run(args);

public class EmitSettings : CommandSettings
{
    [CommandArgument(0, "<Path>")]
    public string[] Path { get; set; }

    [CommandOption("--projectpattern")] public string ProjectPattern { get; set; }
    [CommandOption("--quiet")] public bool Quiet { get; set; } = false;
}

public class EmitCommand : AsyncCommand<EmitSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, EmitSettings settings)
    {
        var collector = new StatCollector();

        var solutions = new List<string>();

        foreach (var path in settings.Path)
        {
            if (File.Exists(path) && path.EndsWith(".sln"))
            {
                solutions.Add(path);
            }
            if (Directory.Exists(path))
            {
                var fnames = Directory.GetFiles(path, "*.sln", SearchOption.AllDirectories);
                solutions.AddRange(fnames);
            }
            
        }
        Util.Emit(0, ".workplan:");
        Util.Emit(1, "solutions:");
        foreach (var sln in solutions)
        {
            Util.Emit(2, "- " + sln);
        }

        foreach (var s in solutions)
        {
            await Emitters.EmitSolution(s, collector, settings);
        }

        Util.Emit(0, ".footer:");
        collector.RenderUnused();
        //collector.RenderDeclared();
        //collector.RenderCallStats();
        return 0;
    }
}