namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests;

using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

public static partial class Valid<T>
    where T : DiagnosticAnalyzer, new()
{
    [Test]
    public static void ReassignFieldWithNewAfterUsingCapture()
    {
        var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private Disposable? disposable;

        public void M()
        {
            using var _ = this.disposable;
            this.disposable = new Disposable();
        }

        public void Dispose() => this.disposable?.Dispose();

        private sealed class Disposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Test]
    public static void ReassignFieldWithNullAfterUsingCapture()
    {
        var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private Disposable? disposable;

        public void M()
        {
            using var _ = this.disposable;
            this.disposable = null;
        }

        public void Dispose() => this.disposable?.Dispose();

        private sealed class Disposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }
}
