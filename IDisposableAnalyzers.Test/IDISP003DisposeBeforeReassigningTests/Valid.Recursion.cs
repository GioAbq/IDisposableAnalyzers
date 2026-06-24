namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests;

using Gu.Roslyn.Asserts;
using Xunit;

public abstract partial class Valid
{
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

    [Fact]
    public void RecursiveOut()
    {
        var code = @"
namespace N
{
    using System;

    public abstract class C
    {
        public static bool RecursiveOut(double foo, out IDisposable? value)
        {
            value = null;
            return RecursiveOut(3.0, out value);
        }

        public void M()
        {
            IDisposable? value;
            RecursiveOut(1.0, out value);
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }
}
