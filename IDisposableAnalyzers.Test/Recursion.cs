namespace IDisposableAnalyzers.Test;

using System;
using System.Collections.Immutable;
using System.Linq;
using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

public static class Recursion
{
    private static readonly ImmutableArray<Type> AllAnalyzerTypes =
        typeof(AnalyzerCategory)
            .Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(t))
            .ToImmutableArray();

    public static TheoryData<Type> AnalyzerCases
    {
        get
        {
            var data = new TheoryData<Type>();
            foreach (var type in AllAnalyzerTypes)
            {
                data.Add(type);
            }

            return data;
        }
    }

    [Fact]
    public static void NotEmpty()
    {
        Assert.NotEmpty(AllAnalyzerTypes);
    }

    [Theory]
    [MemberData(nameof(AnalyzerCases))]
    public static void ConstructorCallingSelf(Type analyzerType)
    {
        var analyzer = (DiagnosticAnalyzer)Activator.CreateInstance(analyzerType)!;
        var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private IDisposable disposable;

        public C()
            : this()
        {
        }

        public C(IDisposable disposable)
            : this(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public C(int i, IDisposable disposable)
            : this(disposable, i)
        {
            this.disposable = disposable;
        }

        public C(IDisposable disposable, int i)
            : this(i, disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
        }

        public static C Create(string fileName) => new C(File.OpenRead(fileName));
    }
}";
        RoslynAssert.NoAnalyzerDiagnostics(analyzer, code);
    }

    [Theory]
    [MemberData(nameof(AnalyzerCases))]
    public static void ConstructorCycle(Type analyzerType)
    {
        var analyzer = (DiagnosticAnalyzer)Activator.CreateInstance(analyzerType)!;
        var code = @"
namespace N
{
    using System;

    public sealed class C : IDisposable
    {
        private IDisposable disposable;

        public C(int i, IDisposable disposable)
            : this(disposable, i)
        {
            this.disposable = disposable;
        }

        public C(IDisposable disposable, int i)
            : this(i, disposable)
        {
            this.disposable = disposable;
        }

        public void Dispose()
        {
        }
    }
}";
        RoslynAssert.NoAnalyzerDiagnostics(analyzer, code);
    }
}
