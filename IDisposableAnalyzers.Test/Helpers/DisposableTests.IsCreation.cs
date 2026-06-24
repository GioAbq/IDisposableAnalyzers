#pragma warning disable GU0073 // Member of non-public type should be internal.
namespace IDisposableAnalyzers.Test.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Gu.Roslyn.AnalyzerExtensions;
using Gu.Roslyn.Asserts;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Xunit;

public static partial class DisposableTests
{
    public static class IsCreation
    {
        [Theory]
        [InlineData("1",                                                   false)]
        [InlineData("new string(' ', 1)",                                  false)]
        [InlineData("new Disposable()",                                    true)]
        [InlineData("new Disposable() as object",                          true)]
        [InlineData("(object) new Disposable()",                           true)]
        [InlineData("typeof(IDisposable)",                                 false)]
        [InlineData("(IDisposable)null",                                   false)]
        [InlineData("System.IO.File.OpenRead(string.Empty) ?? null",       true)]
        [InlineData("null ?? System.IO.File.OpenRead(string.Empty)",       true)]
        [InlineData("true ? null : System.IO.File.OpenRead(string.Empty)", true)]
        [InlineData("true ? System.IO.File.OpenRead(string.Empty) : null", true)]
        public static void LanguageConstructs(string expression, bool expected)
        {
            var code = """
                namespace N
                {
                    using System;

                    public class Disposable : IDisposable
                    {
                        public void Dispose()
                        {
                        }
                    }

                    internal class C
                    {
                        internal C()
                        {
                            var value = new Disposable();
                        }
                    }
                }
                """.AssertReplace("new Disposable()", expression);
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation =
                CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(expression).Value;
            Assert.Equal(true, semanticModel.TryGetType(value, CancellationToken.None, out var type));
            Assert.False(type is IErrorTypeSymbol);
            Assert.Equal(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
        }

        [Theory]
        [InlineData("StaticCreateIntStatementBody()")]
        [InlineData("StaticCreateIntExpressionBody()")]
        [InlineData("StaticCreateIntWithArg()")]
        [InlineData("StaticCreateIntId()")]
        [InlineData("StaticCreateIntSquare()")]
        [InlineData("this.CreateIntStatementBody()")]
        [InlineData("CreateIntExpressionBody()")]
        [InlineData("CreateIntWithArg()")]
        [InlineData("CreateIntId()")]
        [InlineData("CreateIntSquare()")]
        [InlineData("Id<int>()")]
        public static void MethodReturningNotDisposable(string expression)
        {
            var code = """
                namespace N
                {
                    using System;
                    using System.IO;

                    public class Disposable : IDisposable
                    {
                        public void Dispose()
                        {
                        }
                    }

                    internal class C
                    {
                        internal C()
                        {
                            // M();
                        }

                        internal static int StaticCreateIntStatementBody()
                        {
                            return 1;
                        }

                        internal static int StaticCreateIntExpressionBody() => 2;

                        internal static int StaticCreateIntWithArg(int arg) => 3;

                        internal static int StaticCreateIntId(int arg) => arg;

                        internal static int StaticCreateIntSquare(int arg) => arg * arg;

                        internal int CreateIntStatementBody()
                        {
                            return 1;
                        }

                        internal int CreateIntExpressionBody() => 2;

                        internal int CreateIntWithArg(int arg) => 3;
                   
                        internal int CreateIntId(int arg) => arg;
                   
                        internal int CreateIntSquare(int arg) => arg * arg;

                        internal T Id<T>(T arg) => arg;
                    }
                }
                """.AssertReplace("// M()", expression);
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation =
                CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindInvocation(expression);
            Assert.Equal(false, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
        }

        [Theory]
        [InlineData("Id(disposable)",                                   false)]
        [InlineData("Id<IDisposable>(null)",                            false)]
        [InlineData("this.Id<IDisposable>(null)",                       false)]
        [InlineData("this.Id<IDisposable>(this.disposable)",            false)]
        [InlineData("this.Id<IDisposable>(new Disposable())",           false)]
        [InlineData("this.Id<object>(new Disposable())",                false)]
        [InlineData("CreateDisposableStatementBody()",                  true)]
        [InlineData("this.CreateDisposableStatementBody()",             true)]
        [InlineData("CreateDisposableExpressionBody()",                 true)]
        [InlineData("CreateDisposableExpressionBodyReturnTypeObject()", true)]
        [InlineData("CreateDisposableInIf(true)",                       true)]
        [InlineData("CreateDisposableInElse(true)",                     true)]
        [InlineData("ReturningLocal()",                                 true)]
        public static void Call(string expression, bool expected)
        {
            var code = """
                namespace N
                {
                    using System;
                    using System.IO;

                    public class Disposable : IDisposable
                    {
                        public void Dispose()
                        {
                        }
                    }

                    internal class C
                    {
                        private readonly IDisposable disposable = new Disposable();

                        internal C()
                        {
                            Id(disposable);
                        }

                        internal static IDisposable Id(IDisposable arg) => arg;

                        internal T Id<T>(T arg) => arg;

                        internal T ConstrainedFactory<T>(T arg) where T : IDisposable, new() => new T();

                        internal T ConstrainedStructFactory<T>(T arg) where T : struct, new() => new T();

                        internal IDisposable CreateDisposableStatementBody()
                        {
                            return new Disposable();
                        }

                        internal IDisposable CreateDisposableExpressionBody() => new Disposable();
                       
                        internal object CreateDisposableExpressionBodyReturnTypeObject() => new Disposable();

                        internal IDisposable CreateDisposableInIf(bool flag)
                        {
                            if (flag)
                            {
                                return new Disposable();
                            }
                            else
                            {
                                return null;
                            }

                            return null;
                        }

                        internal IDisposable CreateDisposableInElse(bool flag)
                        {
                            if (flag)
                            {
                                return null;
                            }
                            else
                            {
                                return new Disposable();
                            }

                            return null;
                        }

                        public static Stream ReturningLocal()
                        {
                            var stream = File.OpenRead(string.Empty);
                            return stream;
                        }
                    }
                }
                """.AssertReplace("Id(disposable)", expression);
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation =
                CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindInvocation(expression);
            Assert.Equal(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
        }

        [Theory]
        [InlineData("StaticRecursiveStatementBody()",  false)]
        [InlineData("StaticRecursiveExpressionBody()", false)]
        [InlineData("CallingRecursive()",              false)]
        [InlineData("RecursiveTernary(true)",          true)]
        [InlineData("this.RecursiveExpressionBody()",  false)]
        [InlineData("this.RecursiveStatementBody()",   false)]
        public static void CallRecursiveMethod(string expression, bool expected)
        {
            var code = """
                namespace N
                {
                    using System;
                    using System.IO;

                    public class Disposable : IDisposable
                    {
                        public void Dispose()
                        {
                        }
                    }

                    internal class C
                    {
                        internal C()
                        {
                            // M();
                        }

                        private static IDisposable StaticRecursiveStatementBody()
                        {
                            return StaticRecursiveStatementBody();
                        }

                        private static IDisposable StaticRecursiveExpressionBody() => StaticRecursiveExpressionBody();

                        private static IDisposable CallingRecursive() => StaticRecursiveStatementBody();

                        private static IDisposable RecursiveTernary(bool flag) => flag ? new Disposable() : RecursiveTernary(bool flag);

                        private IDisposable RecursiveStatementBody()
                        {
                            return this.RecursiveStatementBody();
                        }

                        private IDisposable RecursiveExpressionBody() => this.RecursiveExpressionBody();
                    }
                }
                """.AssertReplace("// M()", expression);
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation =
                CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindInvocation(expression);
            Assert.Equal(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
        }

        [Fact]
        public static void RecursiveWithOptionalParameter()
        {
            var code = """
                namespace N
                {
                    using System;
                    using System.Collections.Generic;

                    public abstract class C
                    {
                        public C(IDisposable disposable)
                        {
                            var value = disposable;
                            value = WithOptionalParameter(value);
                        }

                        private static IDisposable WithOptionalParameter(IDisposable value, IEnumerable<IDisposable> values = null)
                        {
                            if (values == null)
                            {
                                return WithOptionalParameter(value, new[] { value });
                            }

                            return value;
                        }
                    }
                }
                """;
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation =
                CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindInvocation("WithOptionalParameter(value)");
            Assert.Equal(false, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
        }

        [Theory]
        [InlineData("Task.Run(() => 1)",                         false)]
        [InlineData("Task.Run(() => new Disposable())",          false)]
        [InlineData("CreateStringAsync()",                       false)]
        [InlineData("await CreateStringAsync()",                 false)]
        [InlineData("await Task.Run(() => new string(' ', 1))",  false)]
        [InlineData("await Task.FromResult(new string(' ', 1))", false)]
        [InlineData("await Task.Run(() => new Disposable())",    true)]
        [InlineData("await Task.FromResult(new Disposable())",   true)]
        [InlineData("await CreateDisposableAsync()",             true)]
        public static void AsyncAwait(string expression, bool expected)
        {
            var code = """
                namespace N
                {
                    using System;
                    using System.IO;
                    using System.Threading.Tasks;

                    public class Disposable : IDisposable
                    {
                        public void Dispose()
                        {
                        }
                    }

                    internal class C
                    {
                        internal async Task M()
                        {
                            var value = // M();
                        }

                        internal static async Task<string> CreateStringAsync()
                        {
                            await Task.Delay(0);
                            return new string(' ', 1);
                        }

                        internal static async Task<IDisposable> CreateDisposableAsync()
                        {
                            await Task.Delay(0);
                            return new Disposable();
                        }
                    }
                }
                """.AssertReplace("// M()", expression);

            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation =
                CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(expression)
                                  .Value;
            Assert.Equal(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
        }

        [Theory]
        [InlineData("Control.FromHandle(IntPtr.Zero)",      false)]
        [InlineData("Control.FromChildHandle(IntPtr.Zero)", false)]
        [InlineData("form.FindForm()",                   false)]
        public static void Winforms(string expression, bool expected)
        {
            var code = """
                namespace ValidCode
                {
                    using System;
                    using System.Windows.Forms;

                    public class C
                    {
                        public void M(Form form)
                        {
                            var a = Control.FromHandle(IntPtr.Zero);
                        }
                    }
                }
                """.AssertReplace("Control.FromHandle(IntPtr.Zero)", expression);

            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation =
                CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(expression)
                                  .Value;
            Assert.Equal(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
        }

        [Fact]
        public static void CompositeDisposableExtAddAndReturn()
        {
            var code = """
                namespace N
                {
                    using System;
                    using System.IO;
                    using System.Reactive.Disposables;

                    public static class CompositeDisposableExt
                    {
                        public static T AddAndReturn<T>(this CompositeDisposable disposable, T item)
                            where T : IDisposable
                        {
                            if (item != null)
                            {
                                disposable.Add(item);
                            }

                            return item;
                        }
                    }

                    public sealed class C : IDisposable
                    {
                        private readonly CompositeDisposable disposable = new CompositeDisposable();

                        public C()
                        {
                            disposable.AddAndReturn(File.OpenRead(string.Empty));
                        }

                        public void Dispose()
                        {
                            this.disposable.Dispose();
                        }
                    }
                }
                """;
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation =
                CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindInvocation("disposable.AddAndReturn(File.OpenRead(string.Empty))");
            Assert.Equal(false, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
        }

        [Theory]
        [InlineData("disposable.AsCustom()")]
        [InlineData("disposable.AsCustom() ?? other")]
        [InlineData("other ?? disposable.AsCustom()")]
        public static void AssumeYesForExtensionMethodReturningDifferentTypeThanThisParameter(string expression)
        {
            var code = """
                namespace N
                {
                    using System;
                    using BinaryReference;

                    class C
                    {
                        public C(IDisposable disposable, ICustomDisposable other)
                        {
                            _ = disposable.AsCustom();
                        }
                    }
                }
                """.AssertReplace("disposable.AsCustom()", expression);
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var binary = BinaryReference.Compile("""
                namespace BinaryReference
                {
                    using System;

                    public static class Extensions
                    {
                        public static ICustomDisposable? AsCustom(this IDisposable disposable) => default(ICustomDisposable);
                    }

                    public interface ICustomDisposable : IDisposable
                    {
                    }
                }
                """);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences!.Append(binary));
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindExpression(expression);
            Assert.Equal(true, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
        }

        [Theory]
        [InlineData("disposable.Fluent()")]
        [InlineData("disposable.Fluent() ?? other")]
        [InlineData("other ?? disposable.Fluent()")]
        public static void AssumeNoForUnknownExtensionMethodReturningSameTypeAsThisParameter(string expression)
        {
            var binary = """
                namespace BinaryReference
                {
                    using System;

                    public static class Extensions
                    {
                        public static IDisposable Fluent(this IDisposable disposable) => disposable;
                    }
                }
                """;

            var code = """
                namespace N
                {
                    using System;
                    using BinaryReference;

                    class C
                    {
                        public C(IDisposable disposable, IDisposable other)
                        {
                            _ = disposable.Fluent();
                        }
                    }
                }
                """.AssertReplace("disposable.Fluent()", expression);
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var references = Settings.Default.MetadataReferences!
                                               .Append(BinaryReference.Compile(binary));
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, references);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindExpression(expression);
            Assert.Equal(false, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
        }

        [Theory]
        [InlineData("",                                                                                   "System.IO.File.OpenText(string.Empty)",                              true)]
        [InlineData("",                                                                                   "System.IO.File.OpenRead(string.Empty)",                              true)]
        [InlineData("",                                                                                   "System.IO.File.ReadAllLines(string.Empty)",                          false)]
        [InlineData("System.IO.FileInfo fileInfo",                                                        "fileInfo.Directory",                                                 false)]
        [InlineData("System.IO.FileInfo fileInfo",                                                        "fileInfo.OpenRead()",                                                true)]
        [InlineData("System.IO.FileInfo fileInfo",                                                        "fileInfo.ToString()",                                                false)]
        [InlineData("Microsoft.Win32.RegistryKey registryKey",                                            "registryKey.CreateSubKey(string.Empty)",                             true)]
        [InlineData("Microsoft.Win32.RegistryKey registryKey",                                            "registryKey.OpenSubKey(string.Empty)",                               true)]
        [InlineData("System.Collections.Generic.List<int> xs",                                            "xs.GetEnumerator()",                                                 true)]
        [InlineData("",                                                                                   "System.Diagnostics.Process.Start(string.Empty)",                     true)]
        [InlineData("",                                                                                   "new System.Collections.Generic.List<IDisposable>().Find(x => true)", false)]
        [InlineData("",                                                                                   "ImmutableList<IDisposable>.Empty.Find(x => true)",                   false)]
        [InlineData("",                                                                                   "new Queue<IDisposable>().Peek()",                                    false)]
        [InlineData("",                                                                                   "ImmutableQueue<IDisposable>.Empty.Peek()",                           false)]
        [InlineData("",                                                                                   "new List<IDisposable>()[0]",                                         false)]
        [InlineData("",                                                                                   "Moq.Mock.Of<IDisposable>()",                                         false)]
        [InlineData("",                                                                                   "ImmutableList<IDisposable>.Empty[0]",                                false)]
        [InlineData("System.Windows.Controls.PasswordBox passwordBox",                                    "passwordBox.SecurePassword",                                         true)]
        [InlineData("System.Data.Entity.Infrastructure.SqlConnectionFactory factory",                     "factory.CreateConnection(string.Empty)",                             true)]
        [InlineData("System.Collections.Generic.List<int> xs",                                            "((System.Collections.IList)xs).GetEnumerator()",                     false)]
        [InlineData("System.Collections.Generic.List<IDisposable> xs",                                    "xs.First()",                                                         false)]
        [InlineData("System.Collections.Generic.Dictionary<int, IDisposable> map",                        "map[0]",                                                             false)]
        [InlineData("System.Collections.Generic.IReadOnlyDictionary<int, IDisposable> map",               "map[0]",                                                             false)]
        [InlineData("System.Runtime.CompilerServices.ConditionalWeakTable<IDisposable, IDisposable> map", "map.GetOrCreateValue(this.disposable)",                              false)]
        [InlineData("System.Resources.ResourceManager manager",                                           "manager.GetStream(null)",                                            false)]
        [InlineData("System.Resources.ResourceManager manager",                                           "manager.GetStream(null, null)",                                      false)]
        [InlineData("System.Resources.ResourceManager manager",                                           "manager.GetResourceSet(null, true, true)",                           false)]
        [InlineData("System.Net.Http.HttpResponseMessage message",                                        "message.EnsureSuccessStatusCode()",                                  false)]
        public static void ThirdParty(string parameter, string expression, bool expected)
        {
            var code = """
                namespace N
                {
                    using System;
                    using System.Collections;
                    using System.Collections.Generic;
                    using System.Collections.Immutable;
                    using System.IO;
                    using System.Linq;

                    internal class C
                    {
                        internal C(int value)
                        {
                            _ = value;
                        }
                    }
                }
                """.AssertReplace("int value", parameter)
.AssertReplace("value", expression);

            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindExpression(expression);
            Assert.Equal(true, semanticModel.TryGetType(value, CancellationToken.None, out var type));
            Assert.False(type is IErrorTypeSymbol);
            Assert.Equal(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
        }

        [Theory]
        [InlineData("Activator.CreateInstance<Disposable>()",                                                 true)]
        [InlineData("(Disposable)Activator.CreateInstance(typeof(Disposable))",                               true)]
        [InlineData("Activator.CreateInstance<System.Text.StringBuilder>()",                                  false)]
        [InlineData("Activator.CreateInstance(typeof(System.Text.StringBuilder))",                            false)]
        [InlineData("(System.Text.StringBuilder)Activator.CreateInstance(typeof(System.Text.StringBuilder))", false)]
        public static void Reflection(string expression, bool expected)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText("""
                namespace N
                {
                    using System;
                    using System.Reflection;

                    public class Disposable : IDisposable
                    {
                        public void Dispose()
                        {
                        }
                    }

                    class C
                    {
                        static void M(ConstructorInfo constructorInfo)
                        {
                            var value = Activator.CreateInstance<Disposable>();
                        }
                    }
                }
                """.AssertReplace("Activator.CreateInstance<Disposable>()", expression));
            var compilation =
                CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindEqualsValueClause(expression).Value;
            Assert.Equal(true, semanticModel.TryGetType(value, CancellationToken.None, out var type));
            Assert.False(type is IErrorTypeSymbol);
            Assert.Equal(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
        }

        [Theory]
        [InlineData("",                      "Factory.StaticDisposableField",                  false)]
        [InlineData("",                      "Factory.StaticIDisposableProperty",              false)]
        [InlineData("",                      "Factory.StaticCreateIDisposable()",              true)]
        [InlineData("",                      "Factory.StaticCreateIDisposable().Id()",         true)]
        [InlineData("",                      "Factory.StaticCreateIDisposable().ReturnThis()", true)]
        [InlineData("",                      "Factory.StaticCreateIDisposable().IdGeneric()",  true)]
        [InlineData("",                      "Factory.StaticCreateObject()",                   false)]
        [InlineData("Factory factory",       "factory.IDisposableProperty",                    false)]
        [InlineData("Factory factory",       "factory.CreateIDisposable()",                    true)]
        [InlineData("Factory factory",       "factory.CreateIDisposable().Id()",               true)]
        [InlineData("Factory factory",       "factory.CreateIDisposable().Id().Id()",          true)]
        [InlineData("Factory factory",       "factory.CreateIDisposable().IdGeneric()",        true)]
        [InlineData("Factory factory",       "factory.CreateObject()",                         false)]
        [InlineData("Disposable disposable", "disposable.Id()",                                false)]
        [InlineData("Disposable disposable", "disposable.IdGeneric()",                         false)]
        public static void Assumptions(string parameter, string expression, bool expected)
        {
            var binaryReference = BinaryReference.Compile("""
                namespace BinaryReferencedAssembly
                {
                    using System;

                    public class Disposable : IDisposable
                    {
                        public void Dispose()
                        {
                        }

                        public Disposable ReturnThis() => this;
                    }

                    public static class Ext
                    {
                        public static IDisposable Id(this IDisposable disposable) => disposable;

                        public static T IdGeneric<T>(this T item) => item;
                    }

                    public class Factory
                    {
                        public static readonly Disposable StaticDisposableField = new Disposable();

                        public static IDisposable StaticIDisposableProperty => StaticDisposableField;

                        public IDisposable IDisposableProperty => StaticDisposableField;

                        public static Disposable StaticCreateIDisposable() => new Disposable();

                        public static object StaticCreateObject() => new object();

                        public Disposable CreateIDisposable() => new Disposable();

                        public object CreateObject() => new object();
                    }
                }
                """);

            var syntaxTree = CSharpSyntaxTree.ParseText("""
                namespace N
                {
                    using System;
                    using BinaryReferencedAssembly;

                    class C
                    {
                        static object M(Factory factory)
                        {
                            return factory.CreateIDisposable();
                        }
                    }
                }
                """.AssertReplace("Factory factory", parameter)
.AssertReplace("factory.CreateIDisposable()", expression));

            var compilation = CSharpCompilation.Create(
                "test",
                new[] { syntaxTree },
                Settings.Default.MetadataReferences!.Append(binaryReference),
                CodeFactory.DllCompilationOptions.WithMetadataImportOptions(MetadataImportOptions.Public));
            Assert.Empty(compilation.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error));
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindExpression(expression);
            Assert.Equal(true, semanticModel.TryGetType(value, CancellationToken.None, out var type));
            Assert.False(type is IErrorTypeSymbol);
            Assert.Equal(expected, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
        }

        [Theory]
        [InlineData("Interlocked.Exchange(ref _disposable, new MemoryStream())")]
        [InlineData("Interlocked.Exchange(ref _disposable, null)")]
        public static void InterlockedExchange(string expression)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText("""
                namespace N
                {
                    using System;
                    using System.IO;
                    using System.Threading;

                    sealed class C : IDisposable
                    {
                        private IDisposable _disposable = new MemoryStream();

                        public void Update()
                        {
                            var oldValue = Interlocked.Exchange(ref _disposable, new MemoryStream());
                        }
                    }
                }
                """.AssertReplace("Interlocked.Exchange(ref _disposable, new MemoryStream())", expression));

            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindInvocation(expression);
            Assert.Equal(true, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
        }

        [Theory]
        [InlineData("new S()")]
        [InlineData("new()")]
        public static void RefStruct(string expression)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText("""
                namespace N;

                public ref struct S
                {
                    public static S M() => new S();
            
                    public void Dispose()
                    {
                    }
                }
                """.AssertReplace("new S()", expression));

            var compilation = CSharpCompilation.Create("test", new[] { syntaxTree });
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var value = syntaxTree.FindExpression(expression);
            Assert.Equal(true, Disposable.IsCreation(value, semanticModel, CancellationToken.None));
        }
        [Fact(Skip="Script")]
        public static void Dump()
        {
            var set = new HashSet<string>();
            foreach (var method in typeof(Microsoft.Win32.RegistryKey).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly).OrderBy(x => x.Name))
            {
                if (!method.IsSpecialName &&
                    set.Add(method.Name))
                {
                    Console.WriteLine($"                    IMethodSymbol {{ ContainingType: {{ MetadataName: \"{method.DeclaringType.Name}\" }}, MetadataName: \"{method.Name}\" }} => Result.{(typeof(IDisposable).IsAssignableFrom(method.ReturnType) ? "Yes" : "No")},");
                }
            }
        }
        [Fact(Skip="Script")]
        public static void DumpEnumerable()
        {
            foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes().Where(t => t.IsPublic && t.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(t))).OrderBy(x => x.Name))
            {
                Console.WriteLine($"                    IMethodSymbol {{ ContainingType: {{ MetadataName: \"{type.Name}\" }} }} => false,");
            }
        }
    }
}
