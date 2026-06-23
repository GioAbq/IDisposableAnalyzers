namespace IDisposableAnalyzers.Test.IDISP004DoNotIgnoreCreatedTests;

using Gu.Roslyn.Asserts;
using NUnit.Framework;

public static partial class Valid
{
    [Test]
    public static void IgnoringTcpClientGetStream()
    {
        var code = @"
namespace N
{
    using System.Net.Sockets;

    public class C
    {
        public void M(TcpClient client)
        {
            client.GetStream();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }
}
