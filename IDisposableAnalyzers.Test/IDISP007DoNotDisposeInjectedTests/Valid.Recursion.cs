namespace IDisposableAnalyzers.Test.IDISP007DoNotDisposeInjectedTests;

using Gu.Roslyn.Asserts;

using Microsoft.CodeAnalysis.Diagnostics;

using Xunit;

public abstract class ValidRecursion
{
    protected abstract DiagnosticAnalyzer Analyzer { get; }

    [Fact]
    public void IgnoresWhenDisposingRecursiveProperty()
    {
        var code = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
            this.RecursiveProperty.Dispose();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void IgnoresWhenNotDisposingRecursiveProperty()
    {
        var code = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void IgnoresWhenDisposingFieldAssignedWithRecursiveProperty()
    {
        var code = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        private IDisposable disposable;

        public C()
        {
            this.disposable = this.RecursiveProperty;
        }

        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void IgnoresWhenNotDisposingFieldAssignedWithRecursiveProperty()
    {
        var code = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        private IDisposable disposable;

        public C()
        {
            this.disposable = this.RecursiveProperty;
        }

        public IDisposable RecursiveProperty => RecursiveProperty;

        public void Dispose()
        {
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void IgnoresWhenDisposingRecursiveMethod()
    {
        var code = @"
namespace N
{
    using System;

    public class C
    {
        public IDisposable RecursiveMethod() => RecursiveMethod();

        public void Dispose()
        {
            this.RecursiveMethod().Dispose();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }
}
