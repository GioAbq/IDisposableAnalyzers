namespace IDisposableAnalyzers.Test.IDISP012PropertyShouldNotReturnCreatedTests;

using Gu.Roslyn.Asserts;
using Xunit;

public static class Diagnostics
{
    private static readonly ReturnValueAnalyzer Analyzer = new();
    private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP012PropertyShouldNotReturnCreated);

    [Fact]
    public static void ReturnFileOpenReadGetBody()
    {
        var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public Stream P
        {
            get
            {
                return ↓File.OpenRead(string.Empty);
            }
        }
    }
}";
        RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
    }

    [Fact]
    public static void ReturnFileOpenReadExpressionBody()
    {
        var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public Stream P => ↓File.OpenRead(string.Empty);
    }
}";
        RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
    }

    [Fact]
    public static void ReturnFileOpenReadGetExpressionBody()
    {
        var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public Stream P
        {
            get => ↓File.OpenRead(string.Empty);
        }
    }
}";
        RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
    }
}
