using Microsoft.CodeAnalysis;

public class StatCollector
{
    public Dictionary<string, MethodSymbolInfo> Calls { get; }

    public StatCollector()
    {
        Calls = new Dictionary<string, MethodSymbolInfo>();
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

        
        string methName = ms.Name;
        if (ms.TypeArguments.Length > 0)
        {
            var param = String.Join(",", ms.TypeArguments.Select(tp => tp.ToDisplayString()));
            methName = $"{methName}<{param}>";
        }

        var receiverType = PrjWalker.RenderType(ms.ReceiverType);
        var s = PrjWalker.YamlEscape($"{methName}() {receiverType}");

        var ai = ms.ContainingAssembly.Name;
        var skip = false || ai == "mscorlib" || ai == "System.Core";

        cached = new MethodSymbolInfo
        {
            Count = 1,
            Rendered = s,
            Type = receiverType,
            Name = methName,
            Symbol = ms,
            Skip = skip
        };
        Calls[cacheKey] = cached;
        return cached;
    }

    public void RenderStats()
    {
        PrjWalker.Emit(1, "callstats: |");
        var byCount = Calls.Values.OrderBy(it => it.Count);
        foreach (var msi in byCount)
        {
            if (msi.Skip) 
                continue;
            PrjWalker.Emit(2, $"{msi.Count}; {msi.Rendered}; {msi.Symbol.ContainingAssembly.Name}");
        }
    }
}