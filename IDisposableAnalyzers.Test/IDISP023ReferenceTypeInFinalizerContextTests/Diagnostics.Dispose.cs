namespace IDisposableAnalyzers.Test.IDISP023ReferenceTypeInFinalizerContextTests;

using Gu.Roslyn.Asserts;
using Xunit;

public static partial class Diagnostics
{
    public static class Dispose
    {
        private static readonly DisposeMethodAnalyzer Analyzer = new();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP023ReferenceTypeInFinalizerContext);

        [Theory]
        [InlineData("↓Builder.Append(1)")]
        [InlineData("_ = ↓Builder.Length")]
        public static void Static(string expression)
        {
            var code = @"
namespace N
{
    using System;
    using System.Text;

    public class C : IDisposable
    {
        private static readonly StringBuilder Builder = new StringBuilder();

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

        private void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
                if (disposing)
                {
                }

                ↓Builder.Append(1);
            }
        }
    }
}".AssertReplace("↓Builder.Append(1)", expression);

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Theory]
        [InlineData("this.↓builder.Append(1)")]
        [InlineData("↓builder.Append(1)")]
        [InlineData("_ = ↓builder.Length")]
        [InlineData("↓disposable.Dispose()")]
        [InlineData("↓disposable?.Dispose()")]
        [InlineData("this.↓disposable.Dispose()")]
        [InlineData("this.↓disposable?.Dispose()")]
        [InlineData("↓Disposable.Dispose()")]
        [InlineData("↓Disposable?.Dispose()")]
        [InlineData("this.↓Disposable.Dispose()")]
        [InlineData("this.↓Disposable?.Dispose()")]
        public static void InstanceOutsideIfDispose(string expression)
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Text;

    public class C : IDisposable
    {
        private readonly StringBuilder builder = new StringBuilder();
        private readonly IDisposable disposable = File.OpenRead(string.Empty);

        private bool isDisposed = false;

        private IDisposable Disposable { get; } = File.OpenRead(string.Empty);

        ~C()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
                if (disposing)
                {
                }

                this.↓builder.Append(1);
            }
        }
    }
}".AssertReplace("this.↓builder.Append(1)", expression);

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Theory]
        [InlineData("this.↓builder.Append(1)")]
        [InlineData("↓builder.Append(1)")]
        [InlineData("_ = ↓builder.Length")]
        [InlineData("↓disposable.Dispose()")]
        [InlineData("↓disposable?.Dispose()")]
        [InlineData("this.↓disposable.Dispose()")]
        [InlineData("this.↓disposable?.Dispose()")]
        [InlineData("↓Disposable.Dispose()")]
        [InlineData("↓Disposable?.Dispose()")]
        [InlineData("this.↓Disposable.Dispose()")]
        [InlineData("this.↓Disposable?.Dispose()")]
        public static void InstanceNoIfDispose(string expression)
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Text;

    public class C : IDisposable
    {
        private readonly StringBuilder builder = new StringBuilder();
        private readonly IDisposable disposable = File.OpenRead(string.Empty);

        private IDisposable Disposable { get; } = File.OpenRead(string.Empty);

        ~C()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            this.↓builder.Append(1);
        }
    }
}".AssertReplace("this.↓builder.Append(1)", expression);

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Fact]
        public static void CallingStatic()
        {
            var code = @"
namespace N
{
    using System;
    using System.Text;

    public class C : IDisposable
    {
        private static readonly StringBuilder Builder = new StringBuilder();

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

        protected virtual void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
                if (disposing)
                {
                }

                ↓M();
            }
        }

        private static void M() => Builder.Append(1);
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }
    }
}
