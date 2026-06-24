namespace IDisposableAnalyzers.Test.IDISP017PreferUsingTests;

using Gu.Roslyn.Asserts;
using Xunit;

public static class CodeFix
{
    private static readonly DisposeCallAnalyzer Analyzer = new();
    private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.IDISP017PreferUsing);
    private static readonly AddUsingFix Fix = new();

    [Fact]
    public static void Local()
    {
        var before = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            var stream = File.OpenRead(string.Empty);
            var b = stream.ReadByte();
            ↓stream.Dispose();
        }
    }
}";

        var after = @"
namespace N
{
    using System.IO;

    public sealed class C
    {
        public void M()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                var b = stream.ReadByte();
            }
        }
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }

    [Fact]
    public static void InitializedLocalDisposeInFinally()
    {
        var before = @"
namespace N
{
    using System.IO;

    public class C
    {
        public void M()
        {
            var stream = File.OpenRead(string.Empty);
            try
            {
                var b = stream.ReadByte();
            }
            finally
            {
                ↓stream.Dispose();
            }
        }
    }
}";

        var after = @"
namespace N
{
    using System.IO;

    public class C
    {
        public void M()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                var b = stream.ReadByte();
            }
        }
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }

    [Fact]
    public static void AssignedInTryDisposeInFinally()
    {
        var before = @"
namespace N
{
    using System.IO;

    public class C
    {
        public void M()
        {
            FileStream stream = null;
            try
            {
                stream = File.OpenRead(string.Empty);
                var b = stream.ReadByte();
            }
            finally
            {
                ↓stream.Dispose();
            }
        }
    }
}";

        var after = @"
namespace N
{
    using System.IO;

    public class C
    {
        public void M()
        {
            using (var stream = File.OpenRead(string.Empty))
            {
                var b = stream.ReadByte();
            }
        }
    }
}";
        RoslynAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, before, after);
    }
}
