namespace IDisposableAnalyzers.Test.IDISP024DoNotCallSuppressFinalizeTests;

using Gu.Roslyn.Asserts;
using Xunit;

public static class CodeFix
{
    private static readonly SuppressFinalizeAnalyzer Analyzer = new();
    private static readonly RemoveCallFix Fix = new();

    [Fact]
    public static void SealedSimple()
    {
        var before = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        public void Dispose()
        {
            ↓GC.SuppressFinalize(this);
        }
    }
}";

        var after = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, before, after);
    }
}
