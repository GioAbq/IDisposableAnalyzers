namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests;

using Gu.Roslyn.Asserts;
using Xunit;

public abstract partial class Valid
{
    [Theory]
    [InlineData("disposables.First();")]
    [InlineData("disposables.First(x => x != null);")]
    [InlineData("disposables.Where(x => x != null);")]
    [InlineData("disposables.Single();")]
    [InlineData("Enumerable.Empty<IDisposable>();")]
    public void Linq(string linq)
    {
        var code = @"
namespace N
{
    using System;
    using System.Linq;

    public sealed class C
    {
        public C(IDisposable[] disposables)
        {
            var first = disposables.First();
        }
    }
}".AssertReplace("disposables.First();", linq);
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void MockOf()
    {
        var code = @"
namespace N
{
    using System;
    using Moq;
    using NUnit.Framework;

    public sealed class C
    {
        [Test]
        public void Test()
        {
            var mocked = Mock.Of<IDisposable>();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void Ninject()
    {
        var code = @"
namespace N
{
    using System;
    using Ninject;

    public sealed class C
    {
        public C(IKernel kernel)
        {
            var mocked = kernel.Get<IDisposable>();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void ReactiveUiDisposeWith()
    {
        const string Code = """
            namespace N
            {
                using System;
                using System.Reactive.Disposables;
                using ReactiveUI;
                
                public sealed class C : IDisposable
                {
                    private readonly CompositeDisposable _disposables = new();
                    
                    public C()
                    {
                        var command = ReactiveCommand.Create(() => { Console.WriteLine("Hi"); }).DisposeWith(_disposables);
                    }
                    
                    public void Dispose()
                    {
                        _disposables.Dispose();
                    }
                }
            }
            """;

        RoslynAssert.Valid(Analyzer, Code);
    }
}
