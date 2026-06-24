namespace IDisposableAnalyzers.Test.Helpers;

using System.Threading;
using Gu.Roslyn.AnalyzerExtensions;
using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

public static partial class DisposableTests
{
    public static class DisposedByReturnValue
    {
        [Fact]
        public static void FactoryMethod()
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class Disposer : IDisposable
    {
        private readonly Stream stream;

        private Disposer(Stream stream)
        {
            this.stream = stream;
        }

        public static Disposer M() => new Disposer(File.OpenRead(string.Empty));

        public void Dispose()
        {
            this.stream.Dispose();
        }
    }
}";
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindArgument("File.OpenRead(string.Empty)");
            Assert.Equal(true, Disposable.DisposedByReturnValue(value, semanticModel, CancellationToken.None, out _));
        }

        [Theory]
        [InlineData("new BinaryReader(stream)", true)]
        [InlineData("new BinaryReader(stream, new UTF8Encoding(), true)", false)]
        [InlineData("new BinaryReader(stream, new UTF8Encoding(), leaveOpen: true)", false)]
        [InlineData("new BinaryReader(stream, encoding: new UTF8Encoding(), leaveOpen: true)", false)]
        [InlineData("new BinaryReader(stream, leaveOpen: true, encoding: new UTF8Encoding())", false)]
        [InlineData("new BinaryReader(stream, new UTF8Encoding(), false)", true)]
        [InlineData("new BinaryReader(stream, leaveOpen: false, encoding: new UTF8Encoding())", true)]
        [InlineData("new BinaryWriter(stream, new UTF8Encoding(), leaveOpen: false)", true)]
        [InlineData("new BinaryWriter(stream, new UTF8Encoding(), leaveOpen: true)", false)]
        [InlineData("new StreamReader(stream, new UTF8Encoding(), true, 1024, leaveOpen: false)", true)]
        [InlineData("new StreamReader(stream, new UTF8Encoding(), true, 1024, leaveOpen: true)", false)]
        [InlineData("new StreamWriter(stream, new UTF8Encoding(), 1024, leaveOpen: false)", true)]
        [InlineData("new StreamWriter(stream, new UTF8Encoding(), 1024, leaveOpen: true)", false)]
        [InlineData("new CryptoStream(stream, new FromBase64Transform(), CryptoStreamMode.Read, leaveOpen: true)", false)]
        [InlineData("new CryptoStream(stream, new FromBase64Transform(), CryptoStreamMode.Read, leaveOpen: false)", true)]
        [InlineData("new DeflateStream(stream, CompressionLevel.Fastest)", true)]
        [InlineData("new DeflateStream(stream, CompressionLevel.Fastest, leaveOpen: true)", false)]
        [InlineData("new DeflateStream(stream, CompressionLevel.Fastest, leaveOpen: false)", true)]
        [InlineData("new GZipStream(stream, CompressionLevel.Fastest)", true)]
        [InlineData("new GZipStream(stream, CompressionLevel.Fastest, leaveOpen: true)", false)]
        [InlineData("new GZipStream(stream, CompressionLevel.Fastest, leaveOpen: false)", true)]
        [InlineData("new System.Net.Mail.Attachment(stream, string.Empty)", true)]
        public static void InLeaveOpen(string expression, bool stores)
        {
            var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Security.Cryptography;
    using System.Text;

    public class C
    {
        private readonly IDisposable disposable;

        public C(Stream stream)
        {
            this.disposable = new BinaryReader(stream);
        }
    }
}".AssertReplace("new BinaryReader(stream)", expression);
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindParameter("stream");
            Assert.Equal(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
            Assert.Equal(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
            Assert.Equal(stores, Disposable.Stores(localOrParameter, semanticModel, CancellationToken.None, out var container));
            Assert.Equal(stores, Disposable.DisposedByReturnValue(syntaxTree.FindArgument("stream"), semanticModel, CancellationToken.None, out _));
            if (stores)
            {
                Assert.Equal("N.C.disposable", container.ToString());
            }
        }

        [Theory]
        [InlineData("new HttpClient(handler)", true)]
        [InlineData("new HttpClient(handler, disposeHandler: true)", true)]
        [InlineData("new HttpClient(handler, disposeHandler: false)", false)]
        public static void InHttpClient(string expression, bool stores)
        {
            var code = @"
namespace N
{
    using System.Net.Http;

    public class C
    {
        private readonly IDisposable disposable;

        public C(HttpClientHandler handler)
        {
            this.disposable = new HttpClient(handler);
        }
    }
}".AssertReplace("new HttpClient(handler)", expression);
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindParameter("handler");
            Assert.Equal(true, semanticModel.TryGetSymbol(value, CancellationToken.None, out var symbol));
            Assert.Equal(true, LocalOrParameter.TryCreate(symbol, out var localOrParameter));
            Assert.Equal(stores, Disposable.Stores(localOrParameter, semanticModel, CancellationToken.None, out var container));
            Assert.Equal(stores, Disposable.DisposedByReturnValue(syntaxTree.FindArgument("handler"), semanticModel, CancellationToken.None, out _));
            if (stores)
            {
                Assert.Equal("N.C.disposable", container.ToString());
            }
        }
    }
}
