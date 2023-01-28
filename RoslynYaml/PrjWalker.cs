﻿using System.Security;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class MethodSymbolInfo
{
    public string Rendered { get; set; }
    public int Count { get; set; }
    public string Assembly { get; set; }
    public string Type { get; set; }
    public string Name { get; set; }
    public IMethodSymbol Symbol { get; set; }
    public bool Skip { get; set; }
}

class PrjWalker : CSharpSyntaxWalker
{
    private readonly SemanticModel _mdl;
    public StatCollector _coll { get; set; }

    public static void Emit(int nest, string text)
    {
        var spaces = new string(' ', nest*2);
        Console.WriteLine(spaces + text);
    }

    public static string YamlEscape(string s)
    {

        if (s.Contains('"'))
        {
            return "'" + s + "'";
        }
        if (s.Contains(": "))
        {
            return "\"" + s + "\"";
        }

        return s;

    }
    private void EmitDict(int nest, string mainKey, Dictionary<string, string[]> d)
    {
        Emit(nest, $"- {mainKey}: {YamlEscape(d[mainKey].Single())}");
        d.Remove(mainKey);
        
        foreach (var k in d.Keys)
        {
            var values = d[k];
            if (values.Length == 1)
            {
                Emit(nest, $"  {k}: {YamlEscape(values[0])}");
            }
                 
            if (values.Length > 1)
            {
                Emit(nest, $"  {k}:");
                foreach (var ent in d[k])
                {
                    Emit(nest, "    - "+YamlEscape(ent));
                }
            }
        }
        
        
    }
    public PrjWalker(SemanticModel mdl, StatCollector coll) : base()
    {
        _mdl = mdl;
        _coll = coll;
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var sym = _mdl.GetDeclaredSymbol(node);

        var d = new Dictionary<string, string[]>
        {
            ["m"] = new[] { sym.Name },
            ["param"] = sym.Parameters.Select(p => RenderType(p.Type)).ToArray(),
            ["attr"] = sym.GetAttributes().Select(a => a.AttributeClass.Name).ToArray(),

        };
        EmitDict(1, "m", d);
        Emit(2, "ret: " + RenderType(sym.ReturnType)); 
        base.VisitMethodDeclaration(node);
    }

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        var sym = _mdl.GetSymbolInfo(node);

        if (sym.Symbol is IMethodSymbol ms)
        {
            var msi = _coll.AddInvocation(ms);
            if (!msi.Skip)
            {
                var s = "- call: " + msi.Rendered;
                Emit(1, s);
            }
            
        }
        
        base.VisitInvocationExpression(node);
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var sym = _mdl.GetDeclaredSymbol(node);
        Emit(0, $"{sym.OriginalDefinition}:");

        var d = new Dictionary<string, string[]>
        {
            ["attr"] = sym.GetAttributes().Select(a => a.AttributeClass.Name).ToArray(),
            ["interfaces"] = sym.OriginalDefinition.Interfaces.Select(i => i.ToDisplayString()).ToArray(),
            ["acc"] = new[] { sym.DeclaredAccessibility.ToString() }
            
        };
        var bas = sym.BaseType.ToDisplayString();
        if (bas != "object")
        {
            d["base"] = new[] { bas };
        }

        
        EmitDict(1, "acc", d);
        
        base.VisitClassDeclaration(node);
    }

    public static string RenderType(ITypeSymbol typeSym)
    {
        if (typeSym is INamedTypeSymbol nts)
        {
            var targs = nts.TypeArguments;
            if (targs.Length == 0)
            {
                return typeSym.ToString();
            }
            
            var joined = string.Join(" ", targs.Select(t => RenderType(t)));
            return joined + " " + nts.Name;
        }

        if (typeSym is IArrayTypeSymbol ats)
        {
            return RenderType(ats.ElementType) + " arr";

        }
        return typeSym.ToString();

    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        var sym = _mdl.GetDeclaredSymbol(node);
        var d = new Dictionary<string, string[]>
        {
            ["ctor"] = new[] { sym.ReceiverType.MetadataName },
            ["param"] = sym.Parameters.Select(p => RenderType(p.Type)).ToArray(),
            ["attr"] = sym.GetAttributes().Select(a => a.AttributeClass.Name).ToArray()

        };
        EmitDict(1, "ctor", d);

        base.VisitConstructorDeclaration(node);
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        var sym = _mdl.GetDeclaredSymbol(node);
        var d = new Dictionary<string, string[]>
        {
            ["p"] = new[] { $"{sym.Name} {RenderType(sym.Type)}" },
            ["attr"] = sym.GetAttributes().Select(a => a.AttributeClass.Name).ToArray()

        };
        EmitDict(1, "p", d);
    }

    public override void VisitVariableDeclaration(VariableDeclarationSyntax node)
    {
        foreach (var v in node.Variables)
        {
            var sym = _mdl.GetDeclaredSymbol(v);

            if (sym is IFieldSymbol fs)
            {
                var d = new Dictionary<string, string[]>
                {
                    ["f"] = new[] {$"{fs.Name} {RenderType(fs.Type)}"},
                    ["attr"] = fs.GetAttributes().Select(a => a.AttributeClass.Name).ToArray()
                };
                EmitDict(1, "f", d);
                //Emit(1, $"- f: {fs.Name} {RenderType(fs.Type)}");
                continue;
            }

            if (sym is ILocalSymbol ls)
            {
                Emit(1 , "- v: "+ YamlEscape($"{ls.Name} {RenderType(ls.Type)}"));
                continue;
            }
        }

        base.VisitVariableDeclaration(node);
    }
}