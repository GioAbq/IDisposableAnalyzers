namespace IDisposableAnalyzers.Tests.Web.IDISP001DisposeCreatedTests;

using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

public static class CodeFix
{
    private static readonly DiagnosticAnalyzer Analyzer = new LocalDeclarationAnalyzer();
    private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP001DisposeCreated);
    private static readonly CodeFixProvider Fix = new AddUsingFix();

    [Theory]
    [InlineData("await task")]
    [InlineData("await task.ConfigureAwait(true)")]
    [InlineData("task.Result")]
    [InlineData("task.GetAwaiter().GetResult()")]
    public static void HttpClientIssue242(string expression)
    {
        var before = @"
namespace N
{
    using System.Net.Http;
    using System.Threading.Tasks;

    public static  class C
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public static async Task M()
        {
            await Task.Delay(10);
            var task = HttpClient.GetAsync(""http://example.com"");
            ↓var response = await task;
        }
    }
}".AssertReplace("await task", expression);

        var after = @"
namespace N
{
    using System.Net.Http;
    using System.Threading.Tasks;

    public static  class C
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public static async Task M()
        {
            await Task.Delay(10);
            var task = HttpClient.GetAsync(""http://example.com"");
            using var response = await task;
        }
    }
}".AssertReplace("await task", expression);
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "using");
        RoslynAssert.FixAll(Analyzer, Fix, ExpectedDiagnostic, before, after, fixTitle: "using");
    }
}
