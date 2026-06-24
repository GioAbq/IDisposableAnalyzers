// ReSharper disable InconsistentNaming
namespace IDisposableAnalyzers.Test;

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
    private static readonly ImmutableArray<Type> AllAnalyzerTypes =
        typeof(KnownSymbols)
        .Assembly
        .GetTypes()
        .Where(t => !t.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(t))
        .ToImmutableArray();

    private static readonly Solution AnalyzersCode = CodeFactory.CreateSolution(
        ProjectFile.Find("IDisposableAnalyzers.csproj"),
        settings: Settings.Default.WithCompilationOptions(x => x.WithSuppressedDiagnostics("CS1701")));

    // ReSharper disable once InconsistentNaming
    private static readonly Solution ValidCode = CodeFactory.CreateSolution(
        ProjectFile.Find("ValidCode.csproj"),
        settings: Settings.Default.WithCompilationOptions(x => x.WithSuppressedDiagnostics("CS1701")));

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
    public void AnalyzersProject(Type analyzerType)
    {
        RoslynAssert.Valid(CreateAnalyzer(analyzerType), AnalyzersCode);
    }

    [Theory]
    [MemberData(nameof(AnalyzerCases))]
    public void ValidCodeProject(Type analyzerType)
    {
        RoslynAssert.Valid(CreateAnalyzer(analyzerType), ValidCode);
    }

    [Theory]
    [MemberData(nameof(AnalyzerCases))]
    public void WithSyntaxErrors(Type analyzerType)
    {
        var code = @"
namespace N
{
    using System;
    using System.IO;

    public class C : SyntaxError
    {
        private readonly Stream stream = File.SyntaxError(string.Empty);
        private bool disposed;

        protected override void Dispose(bool disposing)
        {
            if (this.syntaxError)
            {
                return;
            }

            this.disposed = true;
            if (disposing)
            {
                this.stream.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
        RoslynAssert.NoAnalyzerDiagnostics(CreateAnalyzer(analyzerType), code);
    }

    private static DiagnosticAnalyzer CreateAnalyzer(Type type) => (DiagnosticAnalyzer)Activator.CreateInstance(type)!;

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
