// ReSharper disable InconsistentNaming
namespace IDisposableAnalyzers.Tests.Web;

using System;
using System.Collections.Immutable;
using System.Linq;

using Gu.Roslyn.AnalyzerExtensions;
using Gu.Roslyn.Asserts;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using Xunit;

public sealed class AllAnalyzersValid : IClassFixture<AllAnalyzersValid.Cache>
{
    private static readonly ImmutableArray<Type> AllAnalyzerTypes = typeof(KnownSymbols)
        .Assembly
        .GetTypes()
        .Where(t => !t.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(t))
        .ToImmutableArray();

    // ReSharper disable once InconsistentNaming
    private static readonly Solution ValidCode = CodeFactory.CreateSolution(
        ProjectFile.Find("ValidCode.Web.csproj"),
        settings: WebSettings.Exe);

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

    [Fact]
    public void NotEmpty()
    {
        Assert.NotEmpty(AllAnalyzerTypes);
    }

    [Theory]
    [MemberData(nameof(AnalyzerCases))]
    public void ValidCodeProject(Type analyzerType)
    {
        RoslynAssert.Valid((DiagnosticAnalyzer)Activator.CreateInstance(analyzerType)!, ValidCode);
    }

    public sealed class Cache : IDisposable
    {
        private readonly IDisposable transaction;

        public Cache()
        {
            // The cache will be enabled when running in VS.
            // It speeds up the tests and makes them more realistic.
            this.transaction = SyntaxTreeCache<SemanticModel>.Begin(null);
        }

        public void Dispose()
        {
            this.transaction.Dispose();
        }
    }
}
