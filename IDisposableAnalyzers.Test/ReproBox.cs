namespace IDisposableAnalyzers.Test;

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

public static class ReproBox
{
    private const string SkipReason = "For harvesting test cases only.";

    // ReSharper disable once UnusedMember.Local
    private static readonly ImmutableArray<Type> AllAnalyzerTypes =
        typeof(KnownSymbols)
            .Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(t))
            .ToImmutableArray();

    private static readonly Solution Solution = CodeFactory.CreateSolution(
        new FileInfo("C:\\Git\\_GuOrg\\Gu.Reactive\\Gu.Reactive.sln"));

    public static TheoryData<Type> AnalyzerCases
    {
        get
        {
            var data = new TheoryData<Type>();
            foreach (var type in AllAnalyzerTypes)
            {
                data.Add(type);
            }

            return data;
        }
    }

    [Theory(Skip = SkipReason)]
    [MemberData(nameof(AnalyzerCases))]
    public static void SolutionRepro(Type analyzerType)
    {
        RoslynAssert.Valid((DiagnosticAnalyzer)Activator.CreateInstance(analyzerType)!, Solution);
    }

    [Theory(Skip = SkipReason)]
    [MemberData(nameof(AnalyzerCases))]
    public static void Repro(Type analyzerType)
    {
        var code = @"
namespace N
{
    public sealed class C
    {
    }
}";
        RoslynAssert.Valid((DiagnosticAnalyzer)Activator.CreateInstance(analyzerType)!, code);
    }
}
