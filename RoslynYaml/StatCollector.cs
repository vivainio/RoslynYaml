using Microsoft.CodeAnalysis;

public class StatCollector
{
    public Dictionary<string, MethodSymbolInfo> Calls { get; }
    public List<MethodSymbolInfo> Declarations { get; set; }

    public StatCollector()
    {
        Calls = new Dictionary<string, MethodSymbolInfo>();
        Declarations = new List<MethodSymbolInfo>();
    }


    public void AddMethodDeclaration(IMethodSymbol methodSymbol)
    {
        var msi = CreateMethodSymbolInfo(methodSymbol);
        Declarations.Add(msi);
    }
    public MethodSymbolInfo AddInvocation(IMethodSymbol ms)
    {
        var cacheKey = ms.ToString();
        var cached = Calls.GetValueOrDefault(cacheKey);
        if (cached != null)
        {
            cached.Count++;
            return cached;
        }
        
        var msi = CreateMethodSymbolInfo(ms);
        Calls[cacheKey] = msi;
        return msi;

    }

    private MethodSymbolInfo CreateMethodSymbolInfo(IMethodSymbol ms)
    {
        MethodSymbolInfo cached;
        string methName = ms.Name;
        if (ms.TypeArguments.Length > 0)
        {
            var param = String.Join(",", ms.TypeArguments.Select(tp => tp.ToDisplayString()));
            methName = $"{methName}<{param}>";
        }

        var receiverType = PrjWalker.RenderType(ms.ReceiverType);
        var s = Util.YamlEscape($"{methName}() {receiverType}");

        var ai = ms.ContainingAssembly.Name;
        var skip = false || ai == "mscorlib" || ai == "System.Core";
        var attrs = string.Join(",", ms.GetAttributes().Select(a => a.AttributeClass.Name).Distinct()
            .Select(n => $"[{n}]"));
        
        
        cached = new MethodSymbolInfo
        {
            Count = 1,
            Rendered = s,
            Type = receiverType,
            Name = methName,
            Symbol = ms,
            Skip = skip,
            Attrs = attrs,
        };
        return cached;
    }
    
    public void RenderUnused()
    {
        Util.Emit(1, "unusedinterfacemethods:");

        var calledClasses = Calls.Values
            .Where(msi => msi.Symbol.IsAbstract)
            .Select(msi => (msi.Type, msi.Name)).ToHashSet();
        var declaredClasses = Declarations
            .Where(msi => msi.Symbol.IsAbstract)
            .Select(msi => (msi.Type, msi.Name)).ToHashSet();
        var unusedclasses = declaredClasses.Except(calledClasses).ToList();
        foreach (var g in unusedclasses.GroupBy(e => e.Type))
        {
            
            Util.Emit(2, g.Key+ ":");
            foreach (var it in g)
            {
                Util.Emit(3, "- " + it.Name);
            }
        }
    }
    
    public void RenderCallStats()
    {
        Util.Emit(1, "callstats: |");
        var byCount = Calls.Values.OrderBy(it => it.Count);
        foreach (var msi in byCount)
        {
            if (msi.Skip) 
                continue;
            Util.Emit(2, $"{msi.Count}\t{msi.Rendered}\t{msi.Symbol.ContainingAssembly.Name}\t{msi.Attrs}");
        }
    }
}