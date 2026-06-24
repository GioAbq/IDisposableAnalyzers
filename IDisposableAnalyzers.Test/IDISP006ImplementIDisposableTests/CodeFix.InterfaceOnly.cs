namespace IDisposableAnalyzers.Test.IDISP006ImplementIDisposableTests;

using Gu.Roslyn.Asserts;
using Xunit;

public static partial class CodeFix
{
    public static class InterfaceOnly
    {
        //// ReSharper disable once InconsistentNaming
        private static readonly ExpectedDiagnostic CS0535 = ExpectedDiagnostic.Create(nameof(CS0535));

        [Fact]
        public static void Struct()
        {
            var before = @"
namespace N
{
    using System;

    public struct C : ↓IDisposable
    {
    }
}";

            var after = @"
namespace N
{
    using System;

    public struct C : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
            RoslynAssert.CodeFix(Fix, CS0535, before, after);
            RoslynAssert.FixAll(Fix, CS0535, before, after);
        }

        [Fact]
        public static void NestedStruct()
        {
            var before = @"
namespace N
{
    using System;

    internal static class Cache<TKey, TValue>
    {
        internal struct Transaction : ↓IDisposable
        {
        }
    }
}";

            var after = @"
namespace N
{
    using System;

    internal static class Cache<TKey, TValue>
    {
        internal struct Transaction : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}";
            RoslynAssert.CodeFix(Fix, CS0535, before, after);
            RoslynAssert.FixAll(Fix, CS0535, before, after);
        }
    }
}
