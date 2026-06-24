namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests;

using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

public abstract partial class Valid
{
    protected abstract DiagnosticAnalyzer Analyzer { get; }
    private static readonly DiagnosticDescriptor Descriptor = Descriptors.IDISP001DisposeCreated;

    private const string Disposable = @"
namespace N
{
    using System;

    public class Disposable : IDisposable
    {
        public Disposable(string _)
            : this()
        {
        }

        public Disposable()
        {
        }

        public void Dispose()
        {
        }
    }
}";

    [Theory]
    [InlineData("1")]
    [InlineData("new string(' ', 1)")]
    [InlineData("typeof(IDisposable)")]
    [InlineData("(IDisposable?)null")]
    [InlineData("await Task.FromResult(1)")]
    [InlineData("await Task.Run(() => 1)")]
    [InlineData("await Task.Run(() => new object())")]
    [InlineData("await Task.Run(() => Type.GetType(string.Empty))")]
    [InlineData("await Task.Run(() => this.GetType())")]
    public void LanguageConstructs(string expression)
    {
        var code = @"
#pragma warning disable CS0219
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    internal class C
    {
        internal async void M(Object o, FileInfo f)
        {
            await Task.Delay(1);
            var value = new string(' ', 1);
        }
    }
}".AssertReplace("new string(' ', 1)", expression);
        RoslynAssert.Valid(Analyzer, Disposable, code);
    }

    [Fact]
    public void WhenDisposingVariable()
    {
        var code = @"
namespace N
{
    public class C
    {
        public void M()
        {
            var item = new Disposable();
            item.Dispose();
        }
    }
}";

        RoslynAssert.Valid(Analyzer, Disposable, code);
    }

    [Fact]
    public void UsingFileStream()
    {
        var code = @"
namespace N
{
    using System.IO;

    public static class C
    {
        public static long M()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                return stream.Length;
            }
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void UsingFileStreamCSharp8()
    {
        var code = @"
namespace N
{
    using System.IO;

    public static class C
    {
        public static long M()
        {
            using var stream = File.OpenRead(string.Empty);
            return stream.Length;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void UsingNewDisposable()
    {
        var disposableCode = @"
namespace N
{
    using System;

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";

        var code = @"
namespace N
{
    public static class C
    {
        public static long M()
        {
            using (var disposable = new Disposable())
            {
                return 1;
            }
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code, disposableCode);
    }

    [Fact]
    public void Awaiting()
    {
        var code = @"
namespace N
{
    using System.IO;
    using System.Threading.Tasks;
  
    internal static class C
    {
        internal static async Task M()
        {
            using (var stream = await ReadAsync(string.Empty))
            {
            }
        }

        internal static async Task<Stream> ReadAsync(string file)
        {
            var stream = new MemoryStream();
            using (var fileStream = File.OpenRead(file))
            {
                await fileStream.CopyToAsync(stream)
                                ;
            }

            stream.Position = 0;
            return stream;
        }
    }
}";

        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void AwaitingMethodReturningString()
    {
        var code = @"
namespace N
{
    using System.Threading.Tasks;
  
    internal static class C
    {
        internal static async Task M()
        {
            var text = await ReadAsync(string.Empty);
        }

        internal static async Task<string> ReadAsync(string text)
        {
            await Task.Delay(10);
            return text;
        }
    }
}";

        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void AwaitDownloadDataTask()
    {
        var code = @"
#pragma warning disable SYSLIB0014
namespace N
{
    using System.Net;
    using System.Threading.Tasks;

    public class C
    {
        public async Task M()
        {
            using (var client = new WebClient())
            {
                var bytes = await client.DownloadDataTaskAsync(string.Empty);
            }
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void FactoryMethod()
    {
        var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class Disposer : IDisposable
    {
        private readonly Stream stream;

        public Disposer()
            : this(File.OpenRead(string.Empty))
        {
        }

        private Disposer(Stream stream)
        {
            this.stream = stream;
        }

        public static Disposer Create()
        {
            Stream stream = File.OpenRead(string.Empty);
            return new Disposer(stream);
        }

        public void Dispose()
        {
            this.stream.Dispose();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void FactoryMethodExpressionBody()
    {
        var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class Disposal : IDisposable
    {
        private readonly Stream stream;

        public Disposal()
            : this(File.OpenRead(string.Empty))
        {
        }

        private Disposal(Stream stream)
        {
            this.stream = stream;
        }

        public static Disposal Create() => new Disposal(File.OpenRead(string.Empty));

        public void Dispose()
        {
            this.stream.Dispose();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void InjectedDbConnectionCreateCommand()
    {
        var code = @"
namespace N
{
    using System.Data.Common;

    public class C
    {
        public static void M(DbConnection conn)
        {
            using(var command = conn.CreateCommand())
            {
            }
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void InjectedMemberDbConnectionCreateCommand()
    {
        var code = @"
namespace N
{
    using System.Data.Common;

    public class C
    {
        private readonly DbConnection connection;

        public C(DbConnection connection)
        {
            this.connection = connection;
        }

        public void M()
        {
            using(var command = this.connection.CreateCommand())
            {
            }
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void DisposedInEventLambda()
    {
        var code = @"
namespace N
{
    using System.Diagnostics;
    using System.Threading.Tasks;

    public class C
    {
        static Task RunProcessAsync(string fileName)
        {
            // there is no non-generic TaskCompletionSource
            var tcs = new TaskCompletionSource<bool>();

            var process = new Process
                          {
                              StartInfo = { FileName = fileName },
                              EnableRaisingEvents = true
                          };

            process.Exited += (sender, args) =>
            {
                tcs.SetResult(true);
                process.Dispose();
            };

            process.Start();

            return tcs.Task;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void UsingOutParameter()
    {
        var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        public C()
        {
            Stream stream;
            if (TryGetStream(out stream))
            {
                using (stream)
                {
                }
            }

            stream?.Dispose();
        }

        private static bool TryGetStream(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void UsingOutVar()
    {
        var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        public C()
        {
            if (TryGetStream(out var stream))
            {
                using (stream)
                {
                }
            }
            else
            {
                stream?.Dispose();
            }
        }

        private static bool TryGetStream(out Stream stream)
        {
            stream = File.OpenRead(string.Empty);
            return true;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void NewDisposableSplitDeclarationAndAssignment()
    {
        var code = @"
namespace N
{
    using System;

    public class C
    {
        public C()
        {
            IDisposable disposable;
            using (disposable = new Disposable())
            {
            }
        }
    }
}";
        RoslynAssert.Valid(Analyzer, Disposable, code);
    }

    [Fact]
    public void DisposeInFinally()
    {
        var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        public void M()
        {
            FileStream? currentStream = null;
            try
            {
                currentStream = File.OpenRead(string.Empty);
            }
            finally
            {
                currentStream?.Dispose();
            }
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void LocalAssignedToLocalThatIsDisposed()
    {
        var code = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public C(string file)
        {
            var stream = File.OpenRead(file);
            var temp = stream;
            temp.Dispose();
        }
    }
}";

        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void PairOfFileStreams()
    {
        var code = @"
namespace N
{
    using System;
    using System.IO;

    public sealed class PairOfFileStreams : IDisposable
    {
        private readonly Pair<FileStream> pair;

        public PairOfFileStreams(string file1, string file2)
        {
            var stream1 = File.OpenRead(file1);
            var stream2 = File.OpenRead(file2);
            this.pair = Pair.Create(stream1, stream2);
        }

        public void Dispose()
        {
            this.pair.Item1.Dispose();
            this.pair.Item2.Dispose();
        }

        public static class Pair
        {
            public static Pair<T> Create<T>(T item1, T item2) => new Pair<T>(item1, item2);
        }

        public class Pair<T>
        {
            public Pair(T item1, T item2)
            {
                this.Item1 = item1;
                this.Item2 = item2;
            }

            public T Item1 { get; }

            public T Item2 { get; }
        }
    }
}";

        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void StaticFactory()
    {
        var staticFactory = @"
namespace N
{
    using System;

    public static class StaticFactory
    {
        public static IDisposable Create() => new Disposable();
    }
}";

        var c = @"
namespace N
{
    public class C
    {
        public void M()
        {
            using (StaticFactory.Create())
            {
            }
        }
    }
}";

        RoslynAssert.Valid(Analyzer, Disposable, staticFactory, c);
    }

    [Fact]
    public void Factory()
    {
        var factory = @"
namespace N
{
    using System;

    public class Factory
    {
        public IDisposable Create() => new Disposable();
    }
}";

        var c = @"
namespace N
{
    public class C
    {
        public void M(Factory factory)
        {
            using (factory.Create())
            {
            }
        }
    }
}";
        RoslynAssert.Valid(Analyzer, Disposable, factory, c);
    }

    [Theory]
    [InlineData("System.Activator.CreateInstance<StringBuilder>()")]
    [InlineData("(StringBuilder)System.Activator.CreateInstance(typeof(StringBuilder))!")]
    [InlineData("(StringBuilder?)System.Activator.CreateInstance(typeof(StringBuilder))")]
    [InlineData("System.Activator.CreateInstance(typeof(StringBuilder))")]
    [InlineData("(StringBuilder)constructorInfo.Invoke(null)")]
    public void Reflection(string expression)
    {
        var code = @"
namespace N
{
    using System.Reflection;
    using System.Text;

    public class C
    {
        public static void M(ConstructorInfo constructorInfo)
        {
            var disposable = Activator.CreateInstance<StringBuilder>();
        }
    }
}".AssertReplace("Activator.CreateInstance<StringBuilder>()", expression);

        RoslynAssert.Valid(Analyzer, Disposable, code);
    }

    [Fact]
    public void ReturningIfTrueItemReturnNullAfter()
    {
        var code = @"
namespace N
{
    using System.IO;

    sealed class C
    {
        MemoryStream? M(bool condition)
        {
            var item = new MemoryStream();
            if (condition)
            {
                return item;
            }

            item.Dispose();
            return null;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void ReturningIfTrueItemElseNull()
    {
        var code = @"
namespace N
{
    using System.IO;

    sealed class C
    {
        MemoryStream? M(bool condition)
        {
            var item = new MemoryStream();
            if (condition)
            {
                return item;
            }
            else
            {
                item.Dispose();
                return null;
            }
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void ReturningIfTrueReturnNullReturnItemAfter()
    {
        var code = @"
namespace N
{
    using System.IO;

    sealed class C
    {
        MemoryStream? M(bool condition)
        {
            var item = new MemoryStream();
            if (condition)
            {
                item.Dispose();
                return null;                
            }

            return item;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void ReturningIfTrueReturnNullElseReturnItem()
    {
        var code = @"
namespace N
{
    using System.IO;

    sealed class C
    {
        MemoryStream? M(bool condition)
        {
            var item = new MemoryStream();
            if (condition)
            {
                item.Dispose();
                return null;
            }
            else
            {
                return item;
            }
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void ReturnWrappingCngKey()
    {
        var code = @"
namespace N
{
    using System.Security.Cryptography;

    public static class Issue286
    {
        public static ECDsaCng M(CngAlgorithm algorithm, string keyId, CngKeyCreationParameters creationParameters)
        {
            var key = CngKey.Create(algorithm, keyId, creationParameters);
            return new ECDsaCng(key);
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }
}
