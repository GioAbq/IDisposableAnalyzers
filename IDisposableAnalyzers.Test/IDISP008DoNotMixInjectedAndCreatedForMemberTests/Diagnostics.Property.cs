namespace IDisposableAnalyzers.Test.IDISP008DoNotMixInjectedAndCreatedForMemberTests;

using Gu.Roslyn.Asserts;
using Xunit;

public static partial class Diagnostics
{
    public static class Property
    {
        private static readonly FieldAndPropertyDeclarationAnalyzer Analyzer = new();

        [Theory]
        [InlineData("arg ?? File.OpenRead(string.Empty)")]
        [InlineData("File.OpenRead(string.Empty) ?? arg")]
        [InlineData("true ? arg : File.OpenRead(string.Empty)")]
        [InlineData("true ? File.OpenRead(string.Empty) : arg")]
        public static void InjectedAndCreated(string expression)
        {
            var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        private static readonly Stream StaticStream = File.OpenRead(string.Empty);

        public C(Stream arg)
        {
            this.Stream = arg ?? File.OpenRead(string.Empty);
        }

        ↓public Stream Stream { get; }
    }
}".AssertReplace("arg ?? File.OpenRead(string.Empty)", expression);
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Fact]
        public static void InjectedAndCreatedCtorAndInitializer()
        {
            var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public C(Stream stream)
        {
            this.Stream = stream;
        }

        ↓public Stream Stream { get; } = File.OpenRead(string.Empty);
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Fact]
        public static void InjectedAndCreatedTwoCtors()
        {
            var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public C()
        {
            this.Stream = File.OpenRead(string.Empty);
        }

        public C(Stream stream)
        {
            this.Stream = stream;
        }

        ↓public Stream Stream { get; }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Theory]
        [InlineData("public Stream Stream { get; protected set; }")]
        [InlineData("public Stream Stream { get; set; }")]
        [InlineData("protected Stream Stream { get; set; }")]
        public static void Mutable(string property)
        {
            var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        ↓public Stream Stream { get; set; } = File.OpenRead(string.Empty);
    }
}".AssertReplace("public Stream Stream { get; set; }", property);
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Theory]
        [InlineData("internal Stream Stream { get; set; }")]
        [InlineData("public Stream Stream { get; set; }")]
        [InlineData("public Stream Stream { get; internal set; }")]
        public static void MutablePropertyInSealed(string property)
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class C : IDisposable
    {
        ↓public Stream Stream { get; set; } = File.OpenRead(string.Empty);

        public void Dispose()
        {
            this.Stream?.Dispose();
        }
    }
}".AssertReplace("public Stream Stream { get; set; }", property);
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Fact]
        public static void InjectedAndCreatedInFactory()
        {
            var code = @"
namespace N
{
    using System;

    public sealed class C
    {
        public C(IDisposable p)
        {
            this.P = p;
        }

        ↓public IDisposable P { get; }

        public static C Create() => new C(new Disposable());
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, Disposable, code);
        }
    }
}
