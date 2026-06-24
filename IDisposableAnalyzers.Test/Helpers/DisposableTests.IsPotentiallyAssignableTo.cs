namespace IDisposableAnalyzers.Test.Helpers;

using System.Threading;
using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

public static partial class DisposableTests
{
    public static class IsPotentiallyAssignableTo
    {
        [Theory]
        [InlineData("1", false)]
        [InlineData("null", false)]
        [InlineData("\"abc\"", false)]
        public static void ShortCircuit(string expression, bool expected)
        {
            var code = @"
namespace N
{
    internal class C
    {
        internal C()
        {
            var value = PLACEHOLDER;
        }
    }
}".AssertReplace("PLACEHOLDER", expression);
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var value = syntaxTree.FindEqualsValueClause(expression).Value;
            Assert.Equal(expected, Disposable.IsPotentiallyAssignableFrom(value, null, CancellationToken.None));
        }

        [Theory]
        [InlineData("new string(' ', 1)", false)]
        [InlineData("new System.Text.StringBuilder()", false)]
        [InlineData("new System.IO.MemoryStream()", true)]
        public static void ObjectCreation(string expression, bool expected)
        {
            var code = @"
namespace N
{
    internal class C
    {
        internal C()
        {
            var value = PLACEHOLDER;
        }
    }
}".AssertReplace("PLACEHOLDER", expression);
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(expression).Value;
            Assert.Equal(expected, Disposable.IsPotentiallyAssignableFrom(value, semanticModel, CancellationToken.None));
        }
    }
}
