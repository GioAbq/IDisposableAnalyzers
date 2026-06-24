namespace IDisposableAnalyzers.Test.IDISP001DisposeCreatedTests;

using Gu.Roslyn.Asserts;
using Xunit;

public static partial class Diagnostics
{
    public static class OutParameter
    {
        private static readonly ArgumentAnalyzer Analyzer = new();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP001DisposeCreated);

        private const string DisposableCode = @"
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

        [Theory]
        [InlineData("out _")]
        [InlineData("out var temp")]
        [InlineData("out var _")]
        [InlineData("out Disposable temp")]
        [InlineData("out Disposable _")]
        public static void DiscardedNewDisposableStatementBody(string expression)
        {
            var code = @"
namespace N
{
    public static class C
    {
        public static bool M()
        {
            return TryM(↓out _);
        }

        private static bool TryM(out Disposable disposable)
        {
            disposable = new Disposable();
            return true;
        }
    }
}".AssertReplace("out _", expression);

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, code);
        }

        [Theory]
        [InlineData("out _")]
        [InlineData("out var temp")]
        [InlineData("out var _")]
        [InlineData("out Disposable temp")]
        [InlineData("out Disposable _")]
        public static void DiscardedNewDisposableExpressionBody(string expression)
        {
            var code = @"
namespace N
{
    public static class C
    {
        public static bool M() => TryM(↓out _);

        private static bool TryM(out Disposable disposable)
        {
            disposable = new Disposable();
            return true;
        }
    }
}".AssertReplace("out _", expression);

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, DisposableCode, code);
        }

        [Theory]
        [InlineData("out _")]
        [InlineData("out var temp")]
        [InlineData("out var _")]
        [InlineData("out FileStream? temp")]
        [InlineData("out FileStream _")]
        public static void DiscardedFileOpenReadStatementBody(string expression)
        {
            var code = @"
namespace N
{
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    public static class C
    {
        public static bool M(string fileName)
        {
            return TryM(fileName, ↓out _);
        }

        private static bool TryM(string fileName, [NotNullWhen(true)] out FileStream? stream)
        {
            if (File.Exists(fileName))
            {
                stream = File.OpenRead(string.Empty);
                return true;
            }

            stream = null;
            return false;
        }
    }
}".AssertReplace("out _", expression);

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Theory]
        [InlineData("out _")]
        [InlineData("out var temp")]
        [InlineData("out var _")]
        [InlineData("out FileStream? temp")]
        [InlineData("out FileStream _")]
        public static void DiscardedFileOpenReadExpressionBody(string expression)
        {
            var code = @"
namespace N
{
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    public static class C
    {
        public static bool M(string fileName) => TryM(fileName, ↓out _);

        private static bool TryM(string fileName, [NotNullWhen(true)] out FileStream? stream)
        {
            if (File.Exists(fileName))
            {
                stream = File.OpenRead(string.Empty);
                return true;
            }

            stream = null;
            return false;
        }
    }
}".AssertReplace("out _", expression);

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Theory]
        [InlineData("out _")]
        [InlineData("out var temp")]
        [InlineData("out var _")]
        [InlineData("out FileStream temp")]
        [InlineData("out FileStream _")]
        public static void DiscardedOutAssignedWithArgumentStatementBody(string expression)
        {
            var code = @"
namespace N
{
    using System.IO;

    public static class C
    {
        public static bool M(string fileName)
        {
            return TryGet(File.OpenRead(fileName), ↓out _);
        }

        private static bool TryGet(FileStream arg, out FileStream result)
        {
            result = arg;
            return true;
        }
    }
}".AssertReplace("out _", expression);

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }

        [Theory]
        [InlineData("out _")]
        [InlineData("out var temp")]
        [InlineData("out var _")]
        [InlineData("out FileStream temp")]
        [InlineData("out FileStream _")]
        public static void DiscardedOutAssignedWithArgumentExpressionBody(string expression)
        {
            var code = @"
namespace N
{
    using System.IO;

    public static class C
    {
        public static bool M(string fileName) => TryGet(File.OpenRead(fileName), ↓out _);

        private static bool TryGet(FileStream arg, out FileStream result)
        {
            result = arg;
            return true;
        }
    }
}".AssertReplace("out _", expression);

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
        }
    }
}
