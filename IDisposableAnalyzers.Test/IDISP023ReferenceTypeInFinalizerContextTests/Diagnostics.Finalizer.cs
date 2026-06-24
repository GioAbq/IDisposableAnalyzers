namespace IDisposableAnalyzers.Test.IDISP023ReferenceTypeInFinalizerContextTests;

using Gu.Roslyn.Asserts;
using Xunit;

public static partial class Diagnostics
{
    public static class Finalizer
    {
        private static readonly FinalizerAnalyzer Analyzer = new();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP023ReferenceTypeInFinalizerContext);

        [Theory]
        [InlineData("↓Builder.Append(1)")]
        [InlineData("_ = ↓Builder.Length")]
        public static void Static(string expression)
        {
            var code = @"
namespace N
{
    using System.Text;

    public class C
    {
        private static readonly StringBuilder Builder = new StringBuilder();

        ~C()
        {
            ↓Builder.Append(1);
        }
    }
}".AssertReplace("↓Builder.Append(1)", expression);

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Theory]
        [InlineData("this.↓Builder.Append(1)")]
        [InlineData("↓Builder.Append(1)")]
        [InlineData("_ = ↓Builder.Length")]
        public static void Instance(string expression)
        {
            var code = @"
namespace N
{
    using System.Text;

    public class C
    {
        private readonly StringBuilder Builder = new StringBuilder();

        ~C()
        {
            this.↓Builder.Append(1);
        }
    }
}".AssertReplace("this.↓Builder.Append(1)", expression);

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Fact]
        public static void CallingStatic()
        {
            var code = @"
namespace N
{
    using System.Text;

    public class C
    {
        private static readonly StringBuilder Builder = new StringBuilder();

        ~C()
        {
            ↓M();
        }

        private static void M() => Builder.Append(1);
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }
    }
}
