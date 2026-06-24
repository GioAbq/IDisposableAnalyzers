namespace IDisposableAnalyzers.Test.Helpers;

using System.Threading;
using Gu.Roslyn.AnalyzerExtensions;
using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

public static partial class DisposableTests
{
    public static class Assigns
    {
        [Fact]
        public static void WhenNotUsed()
        {
            var code = @"
namespace N
{
    using System;

    internal class C
    {
        internal C(IDisposable disposable)
        {
        }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindParameter("IDisposable disposable");
            var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
            Assert.Equal(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
            Assert.Equal(false, Disposable.Assigns(localOrParameter, semanticModel, CancellationToken.None, out _));
        }

        [Fact]
        public static void AssigningLocal()
        {
            var code = @"
namespace N
{
    using System;

    internal class C
    {
        internal C(IDisposable disposable)
        {
            var temp = disposable;
        }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindParameter("IDisposable disposable");
            var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
            Assert.Equal(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
            Assert.Equal(false, Disposable.Assigns(localOrParameter, semanticModel, CancellationToken.None, out _));
        }

        [Fact]
        public static void FieldAssignedInCtor()
        {
            var code = @"
namespace N
{
    using System;

    internal class C
    {
        private IDisposable disposable;

        internal C(IDisposable disposable)
        {
            this.disposable = disposable;
        }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindParameter("IDisposable disposable");
            var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
            Assert.Equal(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
            Assert.Equal(true, Disposable.Assigns(localOrParameter, semanticModel, CancellationToken.None, out var field));
            Assert.Equal("N.C.disposable", field.Symbol.ToString());
        }

        [Fact]
        public static void FieldAssignedViaCalledMethodParameter()
        {
            var code = @"
namespace N
{
    using System;

    internal class C
    {
        private IDisposable disposable;

        internal C(IDisposable disposable)
        {
            this.M(disposable);
        }

        private void M(IDisposable disposable)
        {
            this.disposable = disposable;
        }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindParameter("IDisposable disposable");
            var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
            Assert.Equal(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
            Assert.Equal(true, Disposable.Assigns(localOrParameter, semanticModel, CancellationToken.None, out var field));
            Assert.Equal("N.C.disposable", field.Symbol.ToString());
        }

        [Fact]
        public static void FieldAssignedInCtorViaLocal()
        {
            var code = @"
namespace N
{
    using System;

    internal class C
    {
        private IDisposable disposable;

        internal C(IDisposable disposable)
        {
            var temp = disposable;
            this.disposable = temp;
        }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindParameter("IDisposable disposable");
            var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
            Assert.Equal(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
            Assert.Equal(true, Disposable.Assigns(localOrParameter, semanticModel, CancellationToken.None, out var field));
            Assert.Equal("N.C.disposable", field.Symbol.ToString());
        }

        [Fact]
        public static void PropertyAssignedInCtor()
        {
            var code = @"
namespace N
{
    using System;

    internal class C
    {
        internal C(IDisposable disposable)
        {
            this.Disposable = disposable;
        }

        public IDisposable Disposable { get; }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindParameter("IDisposable disposable");
            var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
            Assert.Equal(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
            Assert.Equal(true, Disposable.Assigns(localOrParameter, semanticModel, CancellationToken.None, out var field));
            Assert.Equal("N.C.Disposable", field.Symbol.ToString());
        }

        [Fact]
        public static void PropertyAssignedInCalledMethod()
        {
            var code = @"
namespace N
{
    using System;

    internal class C
    {
        internal C(IDisposable disposable)
        {
            this.M(disposable);
        }

        public IDisposable Disposable { get; private set; }

        private void M(IDisposable arg)
        {
            this.Disposable = arg;
        }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindParameter("IDisposable disposable");
            var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
            Assert.Equal(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
            Assert.Equal(true, Disposable.Assigns(localOrParameter, semanticModel, CancellationToken.None, out var field));
            Assert.Equal("N.C.Disposable", field.Symbol.ToString());
        }

        [Fact]
        public static void PropertyAssignedViaIdentity()
        {
            var code = @"
namespace N
{
    using System;

    internal class C
    {
        internal C(IDisposable disposable)
        {
            this.Disposable = this.M(disposable);
        }

        public IDisposable Disposable { get; private set; }

        private void M(IDisposable arg) => arg;
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindParameter("IDisposable disposable");
            var symbol = semanticModel.GetDeclaredSymbol(value, CancellationToken.None);
            Assert.Equal(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
            Assert.Equal(true, Disposable.Assigns(localOrParameter, semanticModel, CancellationToken.None, out var field));
            Assert.Equal("N.C.Disposable", field.Symbol.ToString());
        }

        [Theory]
        [InlineData("Task.FromResult(File.OpenRead(fileName))")]
        [InlineData("Task.FromResult(File.OpenRead(fileName)).ConfigureAwait(true)")]
        [InlineData("Task.Run(() => File.OpenRead(fileName))")]
        [InlineData("Task.Run(() => { return File.OpenRead(fileName); })")]
        [InlineData("Task.Run(() => File.OpenRead(fileName)).ConfigureAwait(true)")]
        public static void AssigningFieldAwait(string expression)
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public sealed class C : IDisposable
    {
        private IDisposable disposable;

        public async Task M(string fileName)
        {
            this.disposable?.Dispose();
            this.disposable = await Task.FromResult(File.OpenRead(fileName));
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
        }
    }
}".AssertReplace("Task.FromResult(File.OpenRead(fileName))", expression);
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
            Assert.Equal(true, Disposable.Assigns(value, semanticModel, CancellationToken.None, out var fieldOrProperty));
            Assert.Equal("disposable", fieldOrProperty.Name);
        }

        [Theory]
        [InlineData("Task.FromResult(File.OpenRead(fileName)).Result")]
        [InlineData("Task.FromResult(File.OpenRead(fileName)).GetAwaiter().GetResult()")]
        [InlineData("Task.Run(() => File.OpenRead(fileName)).Result")]
        [InlineData("Task.Run(() => File.OpenRead(fileName)).GetAwaiter().GetResult()")]
        [InlineData("Task.Run(() => { return File.OpenRead(fileName); }).Result")]
        [InlineData("Task.Run(() => { return File.OpenRead(fileName); }).GetAwaiter().GetResult()")]
        public static void AssigningFieldGetAwaiterGetResult(string expression)
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public sealed class C : IDisposable
    {
        private IDisposable disposable;

        public async Task M(string fileName)
        {
            this.disposable?.Dispose();
            this.disposable = Task.FromResult(File.OpenRead(fileName));
        }

        public void Dispose()
        {
            this.disposable?.Dispose();
        }
    }
}".AssertReplace("Task.FromResult(File.OpenRead(fileName))", expression);
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindExpression("File.OpenRead(fileName)");
            Assert.Equal(true, Disposable.Assigns(value, semanticModel, CancellationToken.None, out var fieldOrProperty));
            Assert.Equal("disposable", fieldOrProperty.Name);
        }
    }
}
