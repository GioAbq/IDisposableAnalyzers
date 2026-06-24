namespace IDisposableAnalyzers.Test.IDISP003DisposeBeforeReassigningTests;

using Gu.Roslyn.Asserts;
using Xunit;

public abstract partial class Valid
{
    [Theory]
    [InlineData("this.stream == null")]
    [InlineData("this.stream == null && file != null")]
    [InlineData("file != null && this.stream == null")]
    [InlineData("this.stream is null")]
    [InlineData("ReferenceEquals(this.stream, null)")]
    [InlineData("Equals(this.stream, null)")]
    [InlineData("object.ReferenceEquals(this.stream, null)")]
    [InlineData("object.Equals(this.stream, null)")]
    public void WhenNullCheckBefore(string nullCheck)
    {
        var code = @"
namespace N
{
    using System.IO;

    public class C
    {
        private Stream? stream;

        public void Update(string file)
        {
            if (this.stream == null)
            {
                this.stream = File.OpenRead(file);
            }
        }
    }
}".AssertReplace("this.stream == null", nullCheck);
        RoslynAssert.Valid(Analyzer, DisposableCode, code);
    }
}
