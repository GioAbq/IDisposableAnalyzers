namespace IDisposableAnalyzers.Tests.Web.Helpers;

using System.Threading;
using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

public static partial class DisposableTests
{
    public static class IsPotentiallyAssignableTo
    {
        [Theory]
        [InlineData("new string(' ', 1)", false)]
        [InlineData("new System.Text.StringBuilder()", false)]
        [InlineData("new System.IO.MemoryStream()", true)]
        [InlineData("(Microsoft.Extensions.Logging.ILoggerFactory)o", true)]
        public static void Expression(string code, bool expected)
        {
            var testCode = @"
namespace N
{
    internal class Foo
    {
        internal Foo(object o)
        {
            var value = PLACEHOLDER;
        }
    }
}".AssertReplace("PLACEHOLDER", code);
            var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(code).Value;
            Assert.Equal(expected, Disposable.IsPotentiallyAssignableFrom(value, semanticModel, CancellationToken.None));
        }
    }
}
