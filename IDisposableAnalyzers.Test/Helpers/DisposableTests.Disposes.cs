namespace IDisposableAnalyzers.Test.Helpers;

using System.Threading;
using Gu.Roslyn.AnalyzerExtensions;
using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

public static partial class DisposableTests
{
    public static class Disposes
    {
        [Fact]
        public static void WhenNotUsed()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    internal class C
    {
        internal C(string fileName)
        {
            var disposable = File.OpenRead(fileName);
        }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindVariableDeclaration("disposable = File.OpenRead(fileName)");
            Assert.Equal(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out ILocalSymbol symbol));
            Assert.Equal(false, Disposable.Disposes(symbol, semanticModel, CancellationToken.None));
        }

        [Theory]
        [InlineData("disposable.Dispose()")]
        [InlineData("disposable?.Dispose()")]
        [InlineData("(disposable as IDisposable)?.Dispose()")]
        [InlineData("((IDisposable)disposable)?.Dispose()")]
        public static void DisposeInvocation(string expression)
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    internal class C
    {
        internal C(string fileName)
        {
            var disposable = File.OpenRead(fileName);
            disposable.Dispose();
        }
    }
}".AssertReplace("disposable.Dispose()", expression);
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindVariableDeclaration("disposable = File.OpenRead(fileName)");
            Assert.Equal(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out ILocalSymbol symbol));
            Assert.Equal(true, Disposable.Disposes(symbol, semanticModel, CancellationToken.None));
        }

        [Fact]
        public static void Using()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    internal class C
    {
        internal C(string fileName)
        {
            using (var disposable = File.OpenRead(fileName))
            {
            }
        }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindVariableDeclaration("disposable");
            Assert.Equal(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out ILocalSymbol symbol));
            Assert.Equal(true, Disposable.Disposes(symbol, semanticModel, CancellationToken.None));
        }

        [Fact]
        public static void UsingDeclaration()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    internal class C
    {
        internal C(string fileName)
        {
            using var disposable = File.OpenRead(fileName);
        }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindVariableDeclaration("disposable");
            Assert.Equal(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out ILocalSymbol symbol));
            Assert.Equal(true, Disposable.Disposes(symbol, semanticModel, CancellationToken.None));
        }

        [Fact]
        public static void UsingAfterDeclaration()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    internal class C
    {
        internal C(string fileName)
        {
            var disposable = File.OpenRead(fileName);
            using (disposable)
            {
            }
        }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindVariableDeclaration("disposable = File.OpenRead(fileName)");
            Assert.Equal(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out ILocalSymbol symbol));
            Assert.Equal(true, Disposable.Disposes(symbol, semanticModel, CancellationToken.None));
        }

        [Fact]
        public static void WhenAddedToFormComponents()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System.IO;
    using System.Windows.Forms;

    public class Winform : Form
    {
        Winform()
        {
            var stream = File.OpenRead(string.Empty);
            // Since this is added to components, it is automatically disposed of with the form.
            this.components.Add(stream);
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindVariableDeclaration("stream = File.OpenRead(string.Empty)");
            Assert.Equal(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out ILocalSymbol symbol));
            Assert.Equal(true, Disposable.Disposes(symbol, semanticModel, CancellationToken.None));
        }

        [Fact]
        public static void IgnoreNewFormShow()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System.Windows.Forms;

    public class Winform : Form
    {
        public static void M()
        {
            var form = new Winform();
            form.Show();
        }
    }
}");
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindVariableDeclaration("var form = new Winform()");
            Assert.Equal(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out ILocalSymbol symbol));
            Assert.Equal(true, Disposable.Disposes(symbol, semanticModel, CancellationToken.None));
        }
    }
}
