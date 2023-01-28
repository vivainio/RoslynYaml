// See https://aka.ms/new-console-template for more information

using System.Security.AccessControl;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
MSBuildLocator.RegisterDefaults();


var solutions = new[]
{
    @"Src\Common\CommonTail.sln",
    @"Src\Common\CommonHead.sln",
    @"Src\Common\Gateway\Gateway.sln"
}.Select(p => Path.Combine(@"c:\r\1", p));

var collector = new StatCollector();

foreach (var s in solutions)
{

    await EmitSolution(s, collector);
}

PrjWalker.Emit(0, ".footer:");
collector.RenderStats();

async Task EmitSolution(string path, StatCollector statCollector)
{
    using var workspace = MSBuildWorkspace.Create();
    var p = await workspace.OpenSolutionAsync(path);

    foreach (var prj in p.Projects)
    {
        await EmitProject(prj, statCollector);
    }
}

async Task EmitProject(Project prj, StatCollector collector)
{
    var comp = await prj.GetCompilationAsync();
    PrjWalker.Emit(0, "---");
    PrjWalker.Emit(0, ".header:");
    PrjWalker.Emit(1, "project: " + prj.Name);

    foreach (var doc in prj.Documents)
    {
        var tree = await doc.GetSyntaxTreeAsync();
        if (tree == null)
        {
            continue;
        }
        var root = tree.GetCompilationUnitRoot();
        var mdl = comp.GetSemanticModel(tree);
        var walker = new PrjWalker(mdl, collector);
        walker.Visit(root);
        
    }
}