#pragma warning disable GU0073 // Member of non-public type should be internal.
namespace IDisposableAnalyzers.Test.Helpers;

using System.Linq;
using System.Threading;
using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

public static class ReturnValueWalkerTests
{
    private static ReturnValueSearch Map(RvSearch search) => (ReturnValueSearch)(int)search;

    [Theory]
    [InlineData("this.CalculatedExpressionBody", RvSearch.Recursive, "1")]
    [InlineData("this.CalculatedExpressionBody", RvSearch.Member, "1")]
    [InlineData("this.CalculatedStatementBody", RvSearch.Recursive, "1")]
    [InlineData("this.CalculatedStatementBody", RvSearch.Member, "1")]
    [InlineData("this.ThisExpressionBody", RvSearch.Recursive, "this")]
    [InlineData("this.ThisExpressionBody", RvSearch.Member, "this")]
    [InlineData("this.CalculatedReturningFieldExpressionBody", RvSearch.Recursive, "this.value")]
    [InlineData("this.CalculatedReturningFieldExpressionBody", RvSearch.Member, "this.value")]
    [InlineData("this.CalculatedReturningFieldStatementBody", RvSearch.Recursive, "this.value")]
    [InlineData("this.CalculatedReturningFieldStatementBody", RvSearch.Member, "this.value")]
    public static void Property(string expression, RvSearch search, string expected)
    {
        var code = @"
namespace N
{
    internal class C
    {
        private readonly int value = 1;

        internal C()
        {
            var temp = CalculatedExpressionBody;
        }

        public int CalculatedExpressionBody => 1;

        public int CalculatedStatementBody
        {
            get
            {
                return 1;
            }
        }

        public C ThisExpressionBody => this;

        public int CalculatedReturningFieldExpressionBody => this.value;

        public int CalculatedReturningFieldStatementBody
        {
            get
            {
                return this.value;
            }
        }
    }
}".AssertReplace("var temp = CalculatedExpressionBody", $"var temp = {expression}");
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var value = syntaxTree.FindEqualsValueClause(expression).Value;
        using var walker = ReturnValueWalker.Borrow(value, Map(search), semanticModel, CancellationToken.None);
        Assert.Equal(expected, string.Join(", ", walker.Values));
    }

    [Theory]
    [InlineData("StaticRecursiveExpressionBody", RvSearch.Recursive, "")]
    [InlineData("StaticRecursiveExpressionBody", RvSearch.Member, "StaticRecursiveExpressionBody")]
    [InlineData("StaticRecursiveStatementBody", RvSearch.Recursive, "")]
    [InlineData("StaticRecursiveStatementBody", RvSearch.Member, "StaticRecursiveStatementBody")]
    [InlineData("RecursiveExpressionBody", RvSearch.Recursive, "")]
    [InlineData("RecursiveExpressionBody", RvSearch.Member, "this.RecursiveExpressionBody")]
    [InlineData("this.RecursiveExpressionBody", RvSearch.Recursive, "")]
    [InlineData("this.RecursiveExpressionBody", RvSearch.Member, "this.RecursiveExpressionBody")]
    [InlineData("this.RecursiveStatementBody", RvSearch.Recursive, "")]
    [InlineData("this.RecursiveStatementBody", RvSearch.Member, "this.RecursiveStatementBody")]
    [InlineData("RecursiveStatementBody", RvSearch.Recursive, "")]
    [InlineData("RecursiveStatementBody", RvSearch.Member, "this.RecursiveStatementBody")]
    public static void PropertyRecursive(string expression, RvSearch search, string expected)
    {
        var code = @"
namespace N
{
    internal class C
    {
        internal C()
        {
            var temp = StaticRecursiveExpressionBody;
        }

        public static int StaticRecursiveExpressionBody => StaticRecursiveExpressionBody;

        public static int StaticRecursiveStatementBody
        {
            get
            {
                return StaticRecursiveStatementBody;
            }
        }

        public int RecursiveExpressionBody => this.RecursiveExpressionBody;

        public int RecursiveStatementBody
        {
            get
            {
                return this.RecursiveStatementBody;
            }
        }
    }
}".AssertReplace("var temp = StaticRecursiveExpressionBody", $"var temp = {expression}");
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var value = syntaxTree.FindEqualsValueClause(expression).Value;
        using var walker = ReturnValueWalker.Borrow(value, Map(search), semanticModel, CancellationToken.None);
        Assert.Equal(expected, string.Join(", ", walker.Values));
    }

    [Theory]
    [InlineData("StaticCreateIntStatementBody()", RvSearch.Recursive, "1")]
    [InlineData("StaticCreateIntStatementBody()", RvSearch.Member, "1")]
    [InlineData("StaticCreateIntExpressionBody()", RvSearch.Recursive, "2")]
    [InlineData("StaticCreateIntExpressionBody()", RvSearch.Member, "2")]
    [InlineData("IdStatementBody(1)", RvSearch.Recursive, "1")]
    [InlineData("IdStatementBody(1)", RvSearch.Member, "1")]
    [InlineData("IdExpressionBody(1)", RvSearch.Recursive, "1")]
    [InlineData("IdExpressionBody(1)", RvSearch.Member, "1")]
    [InlineData("OptionalIdExpressionBody()", RvSearch.Recursive, "1")]
    [InlineData("OptionalIdExpressionBody()", RvSearch.Member, "1")]
    [InlineData("OptionalIdExpressionBody(1)", RvSearch.Recursive, "1")]
    [InlineData("OptionalIdExpressionBody(1)", RvSearch.Member, "1")]
    [InlineData("AssigningToParameter(1)", RvSearch.Recursive, "1, 2, 3, 4")]
    [InlineData("AssigningToParameter(1)", RvSearch.Member, "1, 4")]
    [InlineData("CallingIdExpressionBody(1)", RvSearch.Recursive, "1")]
    [InlineData("CallingIdExpressionBody(1)", RvSearch.RecursiveInside, "")]
    [InlineData("CallingIdExpressionBody(1)", RvSearch.Member, "IdExpressionBody(arg1)")]
    [InlineData("ReturnLocal()", RvSearch.Recursive, "1")]
    [InlineData("ReturnLocal()", RvSearch.Member, "local")]
    [InlineData("ReturnLocalAssignedTwice(true)", RvSearch.Recursive, "1, 2, 3")]
    [InlineData("ReturnLocalAssignedTwice(true)", RvSearch.Member, "local, 3")]
    [InlineData("System.Threading.Tasks.Task.Run(() => 1)", RvSearch.Recursive, "System.Threading.Tasks.Task.Run(() => 1)")]
    [InlineData("System.Threading.Tasks.Task.Run(() => 1)", RvSearch.Member, "System.Threading.Tasks.Task.Run(() => 1)")]
    [InlineData("Missing()", RvSearch.Recursive, "")]
    [InlineData("Missing()", RvSearch.Member, "")]
    [InlineData("this.ThisExpressionBody()", RvSearch.Recursive, "this")]
    [InlineData("this.ThisExpressionBody()", RvSearch.Member, "this")]
    [InlineData("ReturningFileOpenRead()", RvSearch.Recursive, "System.IO.File.OpenRead(string.Empty)")]
    [InlineData("ReturningFileOpenRead()", RvSearch.Member, "System.IO.File.OpenRead(string.Empty)")]
    [InlineData("ReturningLocalFileOpenRead()", RvSearch.Recursive, "System.IO.File.OpenRead(string.Empty)")]
    [InlineData("ReturningLocalFileOpenRead()", RvSearch.Member, "stream")]
    public static void Call(string expression, RvSearch search, string expected)
    {
        var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal class C
    {
        internal C()
        {
            var temp = StaticCreateIntStatementBody();
        }

        internal static int StaticCreateIntStatementBody()
        {
            return 1;
        }

        internal static int StaticCreateIntExpressionBody() => 2;

        internal static int IdStatementBody(int arg)
        {
            return arg;
        }

        internal static int IdExpressionBody(int arg) => arg;

        internal static int OptionalIdExpressionBody(int arg = 1) => arg;

        internal static int CallingIdExpressionBody(int arg1) => IdExpressionBody(arg1);

        public static int AssigningToParameter(int arg)
        {
            if (true)
            {
                return arg;
            }
            else
            {
                if (true)
                {
                    arg = 2;
                }
                else
                {
                    arg = 3;
                }

                return arg;
            }

            return 4;
        }

        public static int ConditionalId(int arg)
        {
            if (true)
            {
                return arg;
            }

            return arg;
        }

        public static int ReturnLocal()
        {
            var local = 1;
            return local;
        }

        public static int ReturnLocalAssignedTwice(bool flag)
        {
            var local = 1;
            local = 2;
            if (flag)
            {
                return local;
            }

            local = 5;
            return 3;
        }

        public C ThisExpressionBody() => this;

        public static Stream ReturningFileOpenRead()
        {
            return System.IO.File.OpenRead(string.Empty);
        }

        public static Stream ReturningLocalFileOpenRead()
        {
            var stream = System.IO.File.OpenRead(string.Empty);
            return stream;
        }
    }
}".AssertReplace("var temp = StaticCreateIntStatementBody()", $"var temp = {expression}");
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var value = syntaxTree.FindEqualsValueClause(expression).Value;
        using var walker = ReturnValueWalker.Borrow(value, Map(search), semanticModel, CancellationToken.None);
        Assert.Equal(expected, string.Join(", ", walker.Values));
    }

    [Theory]
    [InlineData("Recursive()", RvSearch.Recursive, "")]
    [InlineData("Recursive()", RvSearch.Member, "Recursive()")]
    [InlineData("Recursive(1)", RvSearch.Recursive, "")]
    [InlineData("Recursive(1)", RvSearch.Member, "Recursive(arg)")]
    [InlineData("Recursive1(1)", RvSearch.Recursive, "")]
    [InlineData("Recursive1(1)", RvSearch.Member, "Recursive2(value)")]
    [InlineData("Recursive2(1)", RvSearch.Recursive, "")]
    [InlineData("Recursive2(1)", RvSearch.Member, "Recursive1(value)")]
    [InlineData("Recursive(true)", RvSearch.Recursive, "!flag, true")]
    [InlineData("Recursive(true)", RvSearch.Member, "Recursive(!flag), true")]
    [InlineData("RecursiveWithOptional(1)", RvSearch.Recursive, "1")]
    [InlineData("RecursiveWithOptional(1)", RvSearch.Member, "RecursiveWithOptional(arg, new[] { arg }), 1")]
    [InlineData("RecursiveWithOptional(1, null)", RvSearch.Recursive, "1")]
    [InlineData("RecursiveWithOptional(1, null)", RvSearch.Member, "RecursiveWithOptional(arg, new[] { arg }), 1")]
    [InlineData("RecursiveWithOptional(1, new[] { 1, 2 })", RvSearch.Recursive, "1")]
    [InlineData("RecursiveWithOptional(1, new[] { 1, 2 })", RvSearch.Member, "RecursiveWithOptional(arg, new[] { arg }), 1")]
    [InlineData("Flatten(null, null)", RvSearch.Member, "null")]
    [InlineData("Flatten(null, null)", RvSearch.Recursive, "null, new List<IDisposable>()")]
    public static void CallRecursive(string expression, RvSearch search, string expected)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal class C
    {
        internal C()
        {
            var temp = Recursive();
        }

        public static int Recursive() => Recursive();

        public static int Recursive(int arg) => Recursive(arg);

        public static bool Recursive(bool flag)
        {
            if (flag)
            {
                return Recursive(!flag);
            }

            return flag;
        }

        private static int RecursiveWithOptional(int arg, IEnumerable<int> args = null)
        {
            if (arg == null)
            {
                return RecursiveWithOptional(arg, new[] { arg });
            }

            return arg;
        }

        private static int Recursive1(int value)
        {
            return Recursive2(value);
        }

        private static int Recursive2(int value)
        {
            return Recursive1(value);
        }

        private static IReadOnlyList<IDisposable> Flatten(IReadOnlyList<IDisposable> source, List<IDisposable> result = null)
        {
            result = result ?? new List<IDisposable>();
            result.AddRange(source);
            foreach (var condition in source)
            {
                Flatten(new[] { condition }, result);
            }

            return result;
        }
    }
}".AssertReplace("var temp = Recursive()", $"var temp = {expression}"));
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var value = syntaxTree.FindEqualsValueClause(expression).Value;
        using var walker = ReturnValueWalker.Borrow(value, Map(search), semanticModel, CancellationToken.None);
        Assert.Equal(expected, string.Join(", ", walker.Values));
    }

    [Fact]
    public static void RecursiveWithOptionalParameter()
    {
        var code = @"
namespace N
{
    using System;
    using System.Collections.Generic;

    public abstract class C
    {
        public C(IDisposable disposable)
        {
            var local = disposable;
            local = WithOptionalParameter(local);
        }

        private static IDisposable WithOptionalParameter(IDisposable parameter, IEnumerable<IDisposable> values = null)
        {
            if (values == null)
            {
                return WithOptionalParameter(parameter, new[] { parameter });
            }

            return parameter;
        }
    }
}";
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var value = syntaxTree.FindInvocation("WithOptionalParameter(local)");
        using var walker = ReturnValueWalker.Borrow(value, ReturnValueSearch.Recursive, semanticModel, CancellationToken.None);
        Assert.Equal("disposable", string.Join(", ", walker.Values));
    }

    [Fact]
    public static void RecursiveSwitch()
    {
        var code = @"
namespace N
{
    public class C
    {
        public static object M(object value)
        {
            return value switch
            {
                int _ => M(1),
                double _ => M(2),
                string _ => 3,
                _ => value,
            };
        }
    }
}";
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var methodDeclaration = syntaxTree.FindMethodDeclaration("M");
        using var walker = ReturnValueWalker.Borrow(methodDeclaration, ReturnValueSearch.Recursive, semanticModel, CancellationToken.None);
        Assert.Equal("3, 2, 1, value", string.Join(", ", walker.Values));
    }

    [Theory]
    [InlineData("Func<int> temp = () => 1", RvSearch.Recursive, "1")]
    [InlineData("Func<int> temp = () => 1", RvSearch.Member, "1")]
    [InlineData("Func<int, int> temp = x => 1", RvSearch.Recursive, "1")]
    [InlineData("Func<int, int> temp = x => 1", RvSearch.Member, "1")]
    [InlineData("Func<int, int> temp = x => x", RvSearch.Recursive, "x")]
    [InlineData("Func<int, int> temp = x => x", RvSearch.Member, "x")]
    [InlineData("Func<int> temp = () => { return 1; }", RvSearch.Recursive, "1")]
    [InlineData("Func<int> temp = () => { return 1; }", RvSearch.Member, "1")]
    [InlineData("Func<int> temp = () => { if (true) return 1; return 2; }", RvSearch.Recursive, "1, 2")]
    [InlineData("Func<int> temp = () => { if (true) return 1; return 2; }", RvSearch.Member, "1, 2")]
    [InlineData("Func<int,int> temp = x => { if (true) return x; return 1; }", RvSearch.Recursive, "x, 1")]
    [InlineData("Func<int,int> temp = x => { if (true) return x; return 1; }", RvSearch.Member, "x, 1")]
    [InlineData("Func<int,int> temp = x => { if (true) return 1; return x; }", RvSearch.Recursive, "1, x")]
    [InlineData("Func<int,int> temp = x => { if (true) return 1; return x; }", RvSearch.Member, "1, x")]
    [InlineData("Func<int,int> temp = x => { if (true) return 1; return 2; }", RvSearch.Recursive, "1, 2")]
    [InlineData("Func<int,int> temp = x => { if (true) return 1; return 2; }", RvSearch.Member, "1, 2")]
    public static void Lambda(string expression, RvSearch search, string expected)
    {
        var code = @"
namespace N
{
    using System;

    internal class C
    {
        internal C()
        {
            Func<int> temp = () => 1;
        }

        internal static int StaticCreateIntStatementBody()
        {
            return 1;
        }
    }
}".AssertReplace("Func<int> temp = () => 1", expression);
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var value = syntaxTree.FindEqualsValueClause(expression).Value;
        using var walker = ReturnValueWalker.Borrow(value, Map(search), semanticModel, CancellationToken.None);
        Assert.Equal(expected, string.Join(", ", walker.Values));
    }

    [Theory]
    [InlineData("await CreateAsync(0)", RvSearch.Recursive, "1, 0, 2, 3")]
    [InlineData("await CreateAsync(0)", RvSearch.Member, "1, 0, 2, 3")]
    [InlineData("await CreateAsync(0).ConfigureAwait(false)", RvSearch.Recursive, "1, 0, 2, 3")]
    [InlineData("await CreateAsync(0).ConfigureAwait(false)", RvSearch.Member, "1, 0, 2, 3")]
    [InlineData("await CreateStringAsync()", RvSearch.Recursive, "new string(' ', 1)")]
    [InlineData("await CreateStringAsync()", RvSearch.Member, "new string(' ', 1)")]
    [InlineData("await CreateIntAsync()", RvSearch.Recursive, "1")]
    [InlineData("await CreateIntAsync()", RvSearch.Member, "1")]
    [InlineData("await CreateIntAsync().ConfigureAwait(false)", RvSearch.Recursive, "1")]
    [InlineData("await CreateIntAsync().ConfigureAwait(false)", RvSearch.Member, "1")]
    [InlineData("await ReturnTaskFromResultAsync()", RvSearch.Recursive, "1")]
    [InlineData("await ReturnTaskFromResultAsync()", RvSearch.Member, "1")]
    [InlineData("await ReturnTaskFromResultAsync().ConfigureAwait(false)", RvSearch.Recursive, "1")]
    [InlineData("await ReturnTaskFromResultAsync().ConfigureAwait(false)", RvSearch.Member, "1")]
    [InlineData("await ReturnTaskFromResultAsync(1)", RvSearch.Recursive, "1")]
    [InlineData("await ReturnTaskFromResultAsync(1)", RvSearch.Member, "1")]
    [InlineData("await ReturnTaskFromResultAsync(1).ConfigureAwait(false)", RvSearch.Recursive, "1")]
    [InlineData("await ReturnTaskFromResultAsync(1).ConfigureAwait(false)", RvSearch.Member, "1")]
    [InlineData("await ReturnAwaitTaskRunAsync()", RvSearch.Recursive, "new string(' ', 1)")]
    [InlineData("await ReturnAwaitTaskRunAsync()", RvSearch.Member, "await Task.Run(() => new string(\' \', 1))")]
    [InlineData("await ReturnAwaitTaskRunAsync().ConfigureAwait(false)", RvSearch.Recursive, "new string(' ', 1)")]
    [InlineData("await ReturnAwaitTaskRunAsync().ConfigureAwait(false)", RvSearch.Member, "await Task.Run(() => new string(' ', 1))")]
    [InlineData("await ReturnAwaitTaskRunConfigureAwaitAsync()", RvSearch.Recursive, "new string(' ', 1)")]
    [InlineData("await ReturnAwaitTaskRunConfigureAwaitAsync()", RvSearch.Member, "await Task.Run(() => new string(' ', 1)).ConfigureAwait(false)")]
    [InlineData("await ReturnAwaitTaskRunConfigureAwaitAsync().ConfigureAwait(false)", RvSearch.Recursive, "new string(' ', 1)")]
    [InlineData("await ReturnAwaitTaskRunConfigureAwaitAsync().ConfigureAwait(false)", RvSearch.Member, "await Task.Run(() => new string(' ', 1)).ConfigureAwait(false)")]
    [InlineData("await Task.Run(() => 1)", RvSearch.Recursive, "1")]
    [InlineData("await Task.Run(() => 1)", RvSearch.Member, "1")]
    [InlineData("await Task.Run(() => 1).ConfigureAwait(false)", RvSearch.Recursive, "1")]
    [InlineData("await Task.Run(() => 1).ConfigureAwait(false)", RvSearch.Member, "1")]
    [InlineData("await Task.Run(() => new Disposable())", RvSearch.Recursive, "new Disposable()")]
    [InlineData("await Task.Run(() => new Disposable())", RvSearch.Member, "new Disposable()")]
    [InlineData("await Task.Run(() => new Disposable()).ConfigureAwait(false)", RvSearch.Recursive, "new Disposable()")]
    [InlineData("await Task.Run(() => new Disposable()).ConfigureAwait(false)", RvSearch.Member, "new Disposable()")]
    [InlineData("await Task.Run(() => new string(' ', 1))", RvSearch.Recursive, "new string(' ', 1)")]
    [InlineData("await Task.Run(() => new string(' ', 1))", RvSearch.Member, "new string(' ', 1)")]
    [InlineData("await Task.Run(() => new string(' ', 1)).ConfigureAwait(false)", RvSearch.Recursive, "new string(' ', 1)")]
    [InlineData("await Task.Run(() => new string(' ', 1)).ConfigureAwait(false)", RvSearch.Member, "new string(' ', 1)")]
    [InlineData("await Task.Run(() => CreateInt())", RvSearch.Recursive, "1")]
    [InlineData("await Task.Run(() => CreateInt())", RvSearch.Member, "CreateInt()")]
    [InlineData("await Task.Run(() => CreateInt()).ConfigureAwait(false)", RvSearch.Recursive, "1")]
    [InlineData("await Task.Run(() => CreateInt()).ConfigureAwait(false)", RvSearch.Member, "CreateInt()")]
    [InlineData("await Task.FromResult(new string(' ', 1))", RvSearch.Recursive, "new string(' ', 1)")]
    [InlineData("await Task.FromResult(new string(' ', 1))", RvSearch.Member, "new string(' ', 1)")]
    [InlineData("await Task.FromResult(new string(' ', 1)).ConfigureAwait(false)", RvSearch.Recursive, "new string(' ', 1)")]
    [InlineData("await Task.FromResult(new string(' ', 1)).ConfigureAwait(false)", RvSearch.Member, "new string(' ', 1)")]
    [InlineData("await Task.FromResult(CreateInt())", RvSearch.Recursive, "1")]
    [InlineData("await Task.FromResult(CreateInt())", RvSearch.Member, "CreateInt()")]
    [InlineData("await Task.FromResult(CreateInt()).ConfigureAwait(false)", RvSearch.Recursive, "1")]
    [InlineData("await Task.FromResult(CreateInt()).ConfigureAwait(false)", RvSearch.Member, "CreateInt()")]
    public static void AsyncAwait(string expression, RvSearch search, string expected)
    {
        var code = @"
namespace N
{
    using System;
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
            var value = await CreateStringAsync();
        }

        internal static int CreateInt() => 1;

        internal static async Task<string> CreateStringAsync()
        {
            await Task.Delay(0);
            return new string(' ', 1);
        }

        internal static async Task<int> CreateIntAsync()
        {
            await Task.Delay(0);
            return 1;
        }

        internal static Task<int> ReturnTaskFromResultAsync() => Task.FromResult(1);

        internal static Task<int> ReturnTaskFromResultAsync(int arg) => Task.FromResult(arg);

        internal static async Task<string> ReturnAwaitTaskRunAsync() => await Task.Run(() => new string(' ', 1));

        internal static async Task<string> ReturnAwaitTaskRunConfigureAwaitAsync() => await Task.Run(() => new string(' ', 1)).ConfigureAwait(false);

        internal static Task<int> CreateAsync(int arg)
        {
            switch (arg)
            {
                case 0:
                    return Task.FromResult(1);
                case 1:
                    return Task.FromResult(arg);
                case 2:
                    return Task.Run(() => 2);
                case 3:
                    return Task.Run(() => arg);
                case 4:
                    return Task.Run(() => { return 3; });
                default:
                    return Task.Run(() => { return arg; });
            }
        }
    }
}".AssertReplace("var value = await CreateStringAsync()", $"var value = {expression}");

        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var value = syntaxTree.FindEqualsValueClause(expression).Value;
        using var walker = ReturnValueWalker.Borrow(value, Map(search), semanticModel, CancellationToken.None);
        Assert.Equal(expected, string.Join(", ", walker.Values));
    }

    [Theory]
    [InlineData(RvSearch.Recursive, "")]
    [InlineData(RvSearch.Member,    "await Task.SyntaxError(() => new string(' ', 1)).ConfigureAwait(false)")]
    public static void AwaitSyntaxError(RvSearch search, string expected)
    {
        var code = @"
using System.Threading.Tasks;

internal class C
{
    internal static async Task M()
    {
        var text = await CreateAsync().ConfigureAwait(false);
    }

    internal static async Task<string> CreateAsync()
    {
        await Task.Delay(0);
        return await Task.SyntaxError(() => new string(' ', 1)).ConfigureAwait(false);
    }
}";
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var value = syntaxTree.FindExpression("await CreateAsync().ConfigureAwait(false)");
        using var walker = ReturnValueWalker.Borrow(value, Map(search), semanticModel, CancellationToken.None);
        Assert.Equal(expected, string.Join(", ", walker.Values));
    }

    [Theory]
    [InlineData("await RecursiveAsync()", RvSearch.Recursive, "")]
    [InlineData("await RecursiveAsync()", RvSearch.Member, "")]
    [InlineData("await RecursiveAsync(1)", RvSearch.Recursive, "")]
    [InlineData("await RecursiveAsync(1)", RvSearch.Member, "")]
    [InlineData("await RecursiveAsync1(1)", RvSearch.Recursive, "")]
    [InlineData("await RecursiveAsync1(1)", RvSearch.Member, "await RecursiveAsync2(value)")]
    [InlineData("await RecursiveAsync3(1)", RvSearch.Recursive, "")]
    [InlineData("await RecursiveAsync3(1)", RvSearch.Member, "await RecursiveAsync3(value)")]
    public static void AsyncAwaitRecursive(string expression, RvSearch search, string expected)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System;
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
            var value = await RecursiveAsync();
        }

        internal static Task<int> RecursiveAsync() => RecursiveAsync();

        internal static Task<int> RecursiveAsync(int arg) => RecursiveAsync(arg);

        private static async Task<int> RecursiveAsync1(int value)
        {
            return await RecursiveAsync2(value);
        }

        private static async Task<int> RecursiveAsync2(int value)
        {
            return await RecursiveAsync1(value);
        }

        private static Task<int> RecursiveAsync3(int value)
        {
            return RecursiveAsync4(value);
        }

        private static async Task<int> RecursiveAsync4(int value)
        {
            return await RecursiveAsync3(value);
        }
    }
}".AssertReplace("var value = await RecursiveAsync()", $"var value = {expression}"));
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var value = syntaxTree.FindEqualsValueClause(expression).Value;
        using var walker = ReturnValueWalker.Borrow(value, Map(search), semanticModel, CancellationToken.None);
        Assert.Equal(expected, string.Join(", ", walker.Values));
    }

    [Fact]
    public static void ChainedExtensionMethod()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    using System;

    public class C
    {
        public void M(int i)
        {
            var value = i.AsDisposable().AsDisposable();
        }
    }

    public static class Ext
    {
        public static IDisposable AsDisposable(this int i) => new Disposable();

        public static IDisposable AsDisposable(this IDisposable d) => new WrappingDisposable(d);
    }

    public sealed class WrappingDisposable : IDisposable
    {
        private readonly IDisposable inner;

        public WrappingDisposable(IDisposable inner)
        {
            this.inner = inner;
        }

        public void Dispose()
        {
            this.inner.Dispose();
        }
    }
}");
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var methodDeclaration = syntaxTree.FindEqualsValueClause("var value = i.AsDisposable().AsDisposable()").Value;
        using var walker = ReturnValueWalker.Borrow(methodDeclaration, ReturnValueSearch.Recursive, semanticModel, CancellationToken.None);
        Assert.Equal("new WrappingDisposable(d)", walker.Values.Single().ToString());
    }

    [Fact]
    public static void LocalFunctionStatementBody()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    public class C
    {
        public void M()
        {
            Local();

            int Local()
            {
                return 1;
            }
        }
    }
}");
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var invocation = syntaxTree.FindInvocation("Local()");
        using var walker = ReturnValueWalker.Borrow(invocation, ReturnValueSearch.Recursive, semanticModel, CancellationToken.None);
        Assert.Equal("1", walker.Values.Single().ToString());
    }

    [Fact]
    public static void LocalFunctionExpressionBody()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    public class C
    {
        public void M()
        {
            Local();

            int Local() => 1;
        }
    }
}");
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var invocation = syntaxTree.FindInvocation("Local()");
        using var walker = ReturnValueWalker.Borrow(invocation, ReturnValueSearch.Recursive, semanticModel, CancellationToken.None);
        Assert.Equal("1", walker.Values.Single().ToString());
    }

    [Fact]
    public static void ConditionalExpression()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    public class C
    {
        public C(bool arg)
        {
            M(arg);
        }

        private static int M(bool b)
        {
            return b ? 1 : 2;
        }
    }
}");
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var methodDeclaration = syntaxTree.FindInvocation("M(arg)");
        using var walker = ReturnValueWalker.Borrow(methodDeclaration, ReturnValueSearch.Recursive, semanticModel, CancellationToken.None);
        Assert.Equal("1, 2", string.Join(", ", walker.Values));
    }

    [Fact]
    public static void NullCoalesce()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    public class C
    {
        public C()
        {
            M(null);
        }

        private static string M(string text)
        {
            return text ?? string.Empty;
        }
    }
}");
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var methodDeclaration = syntaxTree.FindInvocation("M(null)");
        using var walker = ReturnValueWalker.Borrow(methodDeclaration, ReturnValueSearch.Recursive, semanticModel, CancellationToken.None);
        Assert.Equal("null, string.Empty", string.Join(", ", walker.Values));
    }

    [Theory]
    [InlineData("(IDisposable)o")]
    [InlineData("o as IDisposable")]
    public static void Cast(string cast)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    public class C
    {
        public C()
        {
            M(null);
        }

        private static IDisposable M(object o)
        {
            return (IDisposable)o;
        }
    }
}".AssertReplace("(IDisposable)o", cast));
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var methodDeclaration = syntaxTree.FindInvocation("M(null)");
        using var walker = ReturnValueWalker.Borrow(methodDeclaration, ReturnValueSearch.Recursive, semanticModel, CancellationToken.None);
        Assert.Equal("null", string.Join(", ", walker.Values));
    }

    [Fact]
    public static void SwitchExpression()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
namespace N
{
    public class C
    {
        public C(object o)
        {
            M(o);
        }

        private static int M(object obj)
        {
            return obj switch
            {
                int _ => 1,
                double _ => 2,
                _ => 3,
            };
        }
    }
}");
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var methodDeclaration = syntaxTree.FindInvocation("M(o)");
        using var walker = ReturnValueWalker.Borrow(methodDeclaration, ReturnValueSearch.Recursive, semanticModel, CancellationToken.None);
        Assert.Equal("1, 2, 3", string.Join(", ", walker.Values));
    }

    [Fact]
    public static void ValidationErrorToStringConverter()
    {
        var code = @"
namespace N
{
    using System;
    using System.Globalization;
    using System.Windows.Controls;
    using System.Windows.Data;

    public class ValidationErrorToStringConverter : IValueConverter
    {
        /// <summary> Gets the default instance </summary>
        public static readonly ValidationErrorToStringConverter Default = new ValidationErrorToStringConverter();

        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                return text;
            }

            if (value is ValidationResult result)
            {
                return this.Convert(result.ErrorContent, targetType, parameter, culture);
            }

            if (value is ValidationError error)
            {
                return this.Convert(error.ErrorContent, targetType, parameter, culture);
            }

            return value;
        }

        /// <inheritdoc />
        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException($""{this.GetType().Name} only supports one-way conversion."");
        }
    }
}";
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, Settings.Default.MetadataReferences);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var methodDeclaration = syntaxTree.FindMethodDeclaration("Convert");
        using var walker = ReturnValueWalker.Borrow(methodDeclaration, ReturnValueSearch.Recursive, semanticModel, CancellationToken.None);
        Assert.Equal("error.ErrorContent, result.ErrorContent, value", string.Join(", ", walker.Values));
    }
}
