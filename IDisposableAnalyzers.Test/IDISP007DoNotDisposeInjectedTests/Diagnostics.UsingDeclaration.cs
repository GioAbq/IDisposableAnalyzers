namespace IDisposableAnalyzers.Test.IDISP007DoNotDisposeInjectedTests;

using Gu.Roslyn.Asserts;
using Xunit;

public static partial class Diagnostics
{
    public static class UsingDeclaration
    {
        private static readonly LocalDeclarationAnalyzer Analyzer = new();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP007DoNotDisposeInjected);

        [Fact]
        public static void UsingField1()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
            using var temp = ↓this.disposable ;
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Fact]
        public static void UsingField2()
        {
            var code = @"
namespace N
{
    using System;

    public class C
    {
        private readonly IDisposable disposable;

        public C(IDisposable disposable)
        {
            this.disposable = disposable;
            using var temp = ↓disposable;
        }
    }
}";
            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }
    }
}
