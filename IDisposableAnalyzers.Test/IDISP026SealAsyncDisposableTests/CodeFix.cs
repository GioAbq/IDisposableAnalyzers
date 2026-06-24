namespace IDisposableAnalyzers.Test.IDISP026SealAsyncDisposableTests;

using Gu.Roslyn.Asserts;
using Xunit;

public static class CodeFix
{
    private static readonly ClassDeclarationAnalyzer Analyzer = new();
    private static readonly SealFix Fix = new();
    private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP026SealAsyncDisposable);

    [Fact]
    public static void IAsyncDisposable()
    {
        var before = """
            namespace N;

            using System;
            using System.Threading.Tasks;
        
            public class ↓C : IAsyncDisposable
            {
                public ValueTask DisposeAsync()
                {
                    return ValueTask.CompletedTask;
                }
            }
            """;

        var after = """
            namespace N;

            using System;
            using System.Threading.Tasks;
        
            public sealed class C : IAsyncDisposable
            {
                public ValueTask DisposeAsync()
                {
                    return ValueTask.CompletedTask;
                }
            }
            """;
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }
}
