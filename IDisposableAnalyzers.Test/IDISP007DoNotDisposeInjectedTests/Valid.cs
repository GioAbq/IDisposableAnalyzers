namespace IDisposableAnalyzers.Test.IDISP007DoNotDisposeInjectedTests;

using System.Linq;
using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

public abstract class Valid
{
    protected abstract DiagnosticAnalyzer Analyzer { get; }

    private const string DisposableCode = @"
namespace N
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";

    [Fact]
    public void DisposingArrayItem()
    {
        var code = @"
#nullable disable
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable[] disposables = new IDisposable[1];

        public void M()
        {
            var disposable = disposables[0];
            disposable.Dispose();
        }

        public void Dispose()
        {
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void DisposingDictionaryItem()
    {
        var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;

    public sealed class C : IDisposable
    {
        private readonly Dictionary<int, IDisposable> map = new Dictionary<int, IDisposable>();

        public void M()
        {
            var disposable = map[0];
            disposable.Dispose();
        }

        public void Dispose()
        {
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void DisposingWithBaseClass()
    {
        var baseClass = @"
namespace N
{
    using System;
    using System.IO;

    public abstract class BaseClass : IDisposable
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed = false;

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;

            if (disposing)
            {
                this.stream.Dispose();
            }
        }
    }
}";

        var code = @"
namespace N
{
    using System.IO;

    public class C : BaseClass
    {
        private readonly Stream stream = File.OpenRead(string.Empty);
        private bool disposed;

        protected override void Dispose(bool disposing)
        {
            if (this.disposed)
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
        RoslynAssert.Valid(Analyzer, baseClass, code);
    }

    [Fact]
    public void DisposingInjectedPropertyInBaseClass()
    {
        var baseClass = @"
namespace N
{
    using System;

    public class BaseClass : IDisposable
    {
        private bool disposed = false;

        public BaseClass()
            : this(null)
        {
        }

        public BaseClass(object? p)
        {
            this.P = p;
        }

        public object? P { get; }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }
    }
}";

        var fooImplCode = @"
namespace N
{
    using System;
    using System.IO;

    public class C : BaseClass
    {
        public C(int no)
            : this(no.ToString())
        {
        }

        public C(string fileName)
            : base(File.OpenRead(fileName))
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                (this.P as IDisposable)?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
        RoslynAssert.Valid(Analyzer, baseClass, fooImplCode);
    }

    [Fact]
    public void DisposingInjectedPropertyInBaseClassFieldExpressionBody()
    {
        var baseClass = @"
namespace N
{
    using System;

    public class BaseClass : IDisposable
    {
        private readonly object? p;
        private bool disposed = false;

        public BaseClass()
            : this(null)
        {
        }

        public BaseClass(object? p)
        {
            this.p = p;
        }

        public object? P => this.p;

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }
    }
}";

        var fooImplCode = @"
namespace N
{
    using System;
    using System.IO;

    public class C : BaseClass
    {
        public C(int no)
            : this(no.ToString())
        {
        }

        public C(string fileName)
            : base(File.OpenRead(fileName))
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                (this.P as IDisposable)?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
        RoslynAssert.Valid(Analyzer, baseClass, fooImplCode);
    }

    [Fact]
    public void DisposingInjectedPropertyInBaseClassFieldExpressionBodyNotAssignedByChained()
    {
        var baseClass = @"
namespace N
{
    using System;

    public class BaseClass : IDisposable
    {
        private static IDisposable Empty = new Disposable();

        private readonly object p;
        private bool disposed = false;

        public BaseClass(string text)
        {
            this.p = Empty;
        }

        public BaseClass(object p)
        {
            this.p = p;
        }

        public object P => this.p;

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }
    }
}";

        var fooImplCode = @"
namespace N
{
    using System;
    using System.IO;

    public class C : BaseClass
    {
        public C(string fileName)
            : base(File.OpenRead(fileName))
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                (this.P as IDisposable)?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}";
        RoslynAssert.Valid(Analyzer, DisposableCode, baseClass, fooImplCode);
    }

    [Fact]
    public void InjectedInClassThatIsNotIDisposable()
    {
        var code = @"
namespace N
{
    using System;

    public sealed class C
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void InjectedInClassThatIsIDisposable()
    {
        var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void InjectedInClassThatIsIDisposableManyCtors()
    {
        var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
            : this(disposable, string.Empty)
        {
        }

        public C(IDisposable disposable1, IDisposable disposable2, int n)
            : this(disposable1, n)
        {
        }

        private C(IDisposable disposable, int n)
            : this(disposable, n.ToString())
        {
        }

        private C(IDisposable disposable, string n)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void InjectedObjectInClassThatIsIDisposableWhenTouchingInjectedInDisposeMethod()
    {
        var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private object? f;

        public C(object? f)
        {
            this.f = f;
        }

        public void Dispose()
        {
            f = null;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void NotDisposingFieldInVirtualDispose()
    {
        var code = @"
namespace N
{
    using System;

    public class C : IDisposable
    {
        private readonly IDisposable disposable;
        private bool disposed;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        protected void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void InjectingIntoPrivateCtor()
    {
        var disposableCode = @"
namespace N
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";

        var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private readonly IDisposable disposable;

        public C()
            : this(new Disposable())
        {
        }

        private C(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
            this.disposable.Dispose();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, disposableCode, code);
    }

    [Fact]
    public void BoolProperty()
    {
        var code = @"
namespace N
{
    using System;
    using System.ComponentModel;

    public sealed class C : IDisposable
    {
        private static readonly PropertyChangedEventArgs IsDirtyPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(IsDirty));
        private bool isDirty;

        public C()
        {
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsDirty
        {
            get
            {
                return this.isDirty;
            }

            private set
            {
                if (value == this.isDirty)
                {
                    return;
                }

                this.isDirty = value;
                this.PropertyChanged?.Invoke(this, IsDirtyPropertyChangedEventArgs);
            }
        }

        public void Dispose()
        {
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void InjectedInMethod()
    {
        var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        public C()
        {
        }

        public void M(IDisposable disposable)
        {
        }

        public void Dispose()
        {
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void IgnoreLambdaCreation()
    {
        var code = @"
namespace N
{
    using System;

    public class C
    {
        public C()
        {
            Action<IDisposable> action = x => x.Dispose();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }
    [Theory(Skip="Dunno about this.")]
    [InlineData("action(stream)")]
    [InlineData("action.Invoke(stream)")]
    public void IgnoreLambdaUsageOnLocal(string invokeCode)
    {
        var code = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        public C()
        {
            Action<IDisposable> action = x => x.Dispose();
            var stream = File.OpenRead(string.Empty);
            action(stream);
        }
    }
}".AssertReplace("action(stream)", invokeCode);
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void IgnoreInLambdaMethod()
    {
        var code = @"
namespace N
{
    using System;

    public class C
    {
        public C()
        {
            M(x => x?.Dispose());
        }

        public static void M(Action<IDisposable?> action)
        {
            action(null);
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void ReassignedParameter()
    {
        var code = @"
namespace N
{
    using System;
    using System.IO;

    internal class C
    {
        public static void ReassignParameter(IDisposable disposable)
        {
            disposable = File.OpenRead(string.Empty);
            disposable.Dispose();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void ReassignedParameterViaOut()
    {
        var code = @"
namespace N
{
    using System;
    using System.IO;

    internal class C
    {
        public static void ReassignParameter(IDisposable disposable)
        {
            if (TryReassign(disposable, out disposable))
            {
                disposable.Dispose();
            }
        }

        private static bool TryReassign(IDisposable old, out IDisposable result)
        {
            result = File.OpenRead(string.Empty);
            return true;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void WhenAssumingCreated()
    {
        var binaryReferencedCode = @"
namespace BinaryReferencedAssembly
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    public class Factory
    {
        public IDisposable Create() => new Disposable();
    }
}";
        var binaryReference = BinaryReference.Compile(binaryReferencedCode);

        var code = @"
namespace N
{
    using BinaryReferencedAssembly;

    public class C
    {
        private Factory factory;

        public C(Factory factory)
        {
            this.factory = factory;
        }

        public void M()
        {
            this.factory.Create().Dispose();
            this.factory.Create()?.Dispose();
        }
    }
}";

        var solution = CodeFactory.CreateSolution(
            code,
            Settings.Default
                    .WithCompilationOptions(x => x.WithMetadataImportOptions(MetadataImportOptions.Public))
                    .WithMetadataReferences(x => x.Append(binaryReference)));

        RoslynAssert.Valid(Analyzer, solution);
    }

    [Fact]
    public void FunctionPointerInvocation()
    {
        var code = @"
namespace N
{
    unsafe class C
    {
        public void M() => ((delegate* unmanaged<void>)null)();
    }
}";
        RoslynAssert.Valid(Analyzer, code, Settings.Default.WithCompilationOptions(x => x.WithAllowUnsafe(true)));
    }

    [Fact]
    public void FactoryChainedReturned()
    {
        var disposable = @"
namespace N
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";

        var factory = @"
namespace N
{
    using System;
    using Gu.Inject;

    public static class Factory
    {
        public static Kernel CreateKernel()
        {
            return Create().BindDisposable();
        }

        private static Kernel BindDisposable(this Kernel kernel1)
        {
            return kernel1.Bind<IDisposable, Disposable>();
        }

        private static Kernel Create()
        {
            var kernel2 = new Kernel();
            kernel2.Creating += OnCreating;
            kernel2.Created += OnCreated;
            return kernel2;
        }

        private static void OnCreating(object? sender, CreatingEventArgs e)
        {
        }

        private static void OnCreated(object? sender, CreatedEventArgs e)
        {
        }
    }
}";

        var code = @"
namespace N
{
    using Gu.Inject;
    using NUnit.Framework;

    [TestFixture]
    public class Tests
    {
        private Kernel? _container;

        [SetUp]
        public void SetUp()
        {
            _container = Factory.CreateKernel();
        }

        [TearDown]
        public void TearDown()
        {
            _container?.Dispose();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, disposable, factory, code);
    }

    [Fact]
    public void NewChainedReturned()
    {
        var factory = @"
namespace N
{
    using Gu.Inject;

    public static class Factory
    {
        public static Kernel CreateKernel()
        {
            return new Kernel().Id();
        }

        private static Kernel Id(this Kernel kernel) => kernel;
    }
}";

        var code = @"
namespace N
{
    using Gu.Inject;
    using NUnit.Framework;

    [TestFixture]
    public class Tests
    {
        private Kernel? _container;

        [SetUp]
        public void SetUp()
        {
            _container = Factory.CreateKernel();
        }

        [TearDown]
        public void TearDown()
        {
            _container?.Dispose();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, factory, code);
    }

    [Fact]
    public void NewChainedLocalReturned()
    {
        var factory = @"
namespace N
{
    using Gu.Inject;

    public static class Factory
    {
        public static Kernel CreateKernel()
        {
            var kernel = new Kernel().Id();
            return kernel;
        }

        private static Kernel Id(this Kernel k) => k;
    }
}";

        var code = @"
namespace N
{
    using Gu.Inject;
    using NUnit.Framework;

    [TestFixture]
    public class Tests
    {
        private Kernel? _container;

        [SetUp]
        public void SetUp()
        {
            _container = Factory.CreateKernel();
        }

        [TearDown]
        public void TearDown()
        {
            _container?.Dispose();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, factory, code);
    }

    [Fact]
    public void FactoryChainedLocalReturned()
    {
        var disposable = @"
namespace N
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";

        var factory = @"
namespace N
{
    using System;
    using Gu.Inject;

    public static class Factory
    {
        public static Kernel CreateKernel()
        {
            var kernel = Create().BindDisposable();
            return kernel;
        }

        private static Kernel BindDisposable(this Kernel kernel1)
        {
            kernel1.Bind<IDisposable, Disposable>();
            return kernel1;
        }

        private static Kernel Create()
        {
            var kernel2 = new Kernel();
            kernel2.Creating += OnCreating;
            kernel2.Created += OnCreated;
            return kernel2;
        }

        private static void OnCreating(object? sender, CreatingEventArgs e)
        {
        }

        private static void OnCreated(object? sender, CreatedEventArgs e)
        {
        }
    }
}";

        var code = @"
namespace N
{
    using Gu.Inject;
    using NUnit.Framework;

    [TestFixture]
    public class FixtureStackTests
    {
        private Kernel? _container;

        [SetUp]
        public void SetUp()
        {
            _container = Factory.CreateKernel();
        }

        [TearDown]
        public void TearDown()
        {
            _container?.Dispose();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, disposable, factory, code);
    }

    [Theory]
    [InlineData("new DefaultFalse(stream)")]
    [InlineData("new DefaultFalse(stream, false)")]
    [InlineData("new DefaultTrue(stream, false)")]
    public void LeaveOpenDispose(string expression)
    {
        var code = @"
namespace N
{
    using System;
    using System.IO;

    public class C
    {
        public static void M(string fileName)
        {
            var stream = File.OpenRead(fileName);
            using var reader = new DefaultFalse(stream);
        }

        private sealed class DefaultTrue : IDisposable
        {
            private readonly Stream stream;
            private readonly bool leaveOpen;

            public DefaultTrue(Stream stream, bool leaveOpen = true)
            {
                this.stream = stream;
                this.leaveOpen = leaveOpen;
            }

            public void Dispose()
            {
                if (!this.leaveOpen)
                {
                    this.stream.Dispose();
                }
            }
        }

        private sealed class DefaultFalse : IDisposable
        {
            private readonly Stream stream;
            private readonly bool leaveOpen;

            public DefaultFalse(Stream stream, bool leaveOpen = false)
            {
                this.stream = stream;
                this.leaveOpen = leaveOpen;
            }

            public void Dispose()
            {
                if (!this.leaveOpen)
                {
                    this.stream.Dispose();
                }
            }
        }
    }
}".AssertReplace("new DefaultFalse(stream)", expression);

        RoslynAssert.Valid(Analyzer, code);
    }

    [Theory]
    [InlineData("new DefaultFalse(stream)")]
    [InlineData("new DefaultFalse(stream, false)")]
    [InlineData("new DefaultTrue(stream, false)")]
    public void LeaveOpenWhenDisposeAsync(string expression)
    {
        var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class C
    {
        public static async Task M(string fileName)
        {
            var stream = File.OpenRead(fileName);
            await using var reader = new DefaultFalse(stream);
        }

        private sealed class DefaultTrue : IAsyncDisposable
        {
            private readonly Stream stream;
            private readonly bool leaveOpen;

            public DefaultTrue(Stream stream, bool leaveOpen = true)
            {
                this.stream = stream;
                this.leaveOpen = leaveOpen;
            }

            public ValueTask DisposeAsync()
            {
                if (!this.leaveOpen)
                {
                    return this.stream.DisposeAsync();
                }

                return ValueTask.CompletedTask;
            }
        }

        private sealed class DefaultFalse : IAsyncDisposable
        {
            private readonly Stream stream;
            private readonly bool leaveOpen;

            public DefaultFalse(Stream stream, bool leaveOpen = false)
            {
                this.stream = stream;
                this.leaveOpen = leaveOpen;
            }

            public ValueTask DisposeAsync()
            {
                if (!this.leaveOpen)
                {
                    return this.stream.DisposeAsync();
                }

                return ValueTask.CompletedTask;
            }
        }
    }
}
".AssertReplace("new DefaultFalse(stream)", expression);

        RoslynAssert.Valid(Analyzer, code);
    }

    [Theory]
    [InlineData("System.IO.File.OpenRead(fileName)")]
    [InlineData("System.Diagnostics.Process.Start(fileName)")]
    public void ReassignStatic(string expression)
    {
        var code = @"
namespace N;

using System;

public static class C
{
    private static IDisposable? disposable;

    public static void M(string fileName)
    {
        disposable?.Dispose();
        disposable = File.OpenRead(fileName);
    }
}".AssertReplace("File.OpenRead(fileName)", expression);
        RoslynAssert.Valid(Analyzer, code);
    }
}
