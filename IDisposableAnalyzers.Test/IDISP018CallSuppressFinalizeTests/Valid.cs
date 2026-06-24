namespace IDisposableAnalyzers.Test.IDISP018CallSuppressFinalizeTests;

using Gu.Roslyn.Asserts;
using Xunit;

public static class Valid
{
    private static readonly DisposeMethodAnalyzer Analyzer = new();

    [Fact]
    public static void SealedSimple()
    {
        var code = @"
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
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public static void SealedNoFinalizer()
    {
        var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private bool isDisposed = false;

        public void Dispose()
        {
            this.Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
            }
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public static void SealedWithFinalizer()
    {
        var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private bool isDisposed = false;

        ~C()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
            }
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }
}
