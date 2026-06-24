namespace IDisposableAnalyzers.Test.IDISP008DoNotMixInjectedAndCreatedForMemberTests;

using Gu.Roslyn.Asserts;
using Xunit;

public partial class Valid
{
    [Fact]
    public void SingleAssignmentDisposable()
    {
        var code = @"
namespace Gu.Reactive
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public abstract class C : IDisposable
    {
        private readonly SingleAssignmentDisposable subscription = new SingleAssignmentDisposable();

        protected C()
        {
            this.subscription.Disposable = File.OpenRead(string.Empty);
        }

        public void Dispose()
        {
            this.subscription.Dispose();
        }
     }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void SingleAssignmentDisposableAssignedWithObservableSubscribe()
    {
        var code = @"
namespace Gu.Reactive
{
    using System;
    using System.Reactive.Disposables;

    public abstract class C : IDisposable
    {
        private readonly SingleAssignmentDisposable subscription = new SingleAssignmentDisposable();

        protected C(IObservable<object> observable)
        {
            this.subscription.Disposable = observable.Subscribe(_ => { });
        }

        public void Dispose()
        {
            this.subscription.Dispose();
        }
     }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void SingleAssignmentDisposableAssignedInAction()
    {
        var code = @"
namespace Gu.Reactive
{
    using System;
    using System.IO;
    using System.Reactive.Disposables;

    public abstract class C : IDisposable
    {
        private readonly Lazy<int> lazy;
        private readonly SingleAssignmentDisposable subscription = new SingleAssignmentDisposable();

        protected C()
        {
            this.lazy = new Lazy<int>(
                () =>
                    {
                        this.subscription.Disposable = File.OpenRead(string.Empty);
                        return 1;
                    });
        }

        public void Dispose()
        {
            this.subscription.Dispose();
        }
     }
}";
        RoslynAssert.Valid(Analyzer, code);
    }
}
