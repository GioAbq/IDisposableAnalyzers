namespace IDisposableAnalyzers.Tests.Web.IDISP002DisposeMemberTests;

using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

public static class Valid
{
    private static readonly DiagnosticAnalyzer Analyzer = new FieldAndPropertyDeclarationAnalyzer();

    [Fact]
    public static void FieldDisposeAsyncInDisposeAsync()
    {
        var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class C : IAsyncDisposable
    {
        private readonly IAsyncDisposable disposable = File.OpenRead(string.Empty);

        public async ValueTask DisposeAsync()
        {
            await this.disposable.DisposeAsync();
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public static void IHostedService()
    {
        var code = @"
namespace N
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;

    class C : IHostedService
    {
        private IDisposable? disposable;

        public Task StartAsync(CancellationToken token)
        {
            this.disposable = File.OpenRead(string.Empty);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken token)
        {
            this.disposable?.Dispose();
            return Task.CompletedTask;
        }
    }
}";
        RoslynAssert.Valid(Analyzer, code);
    }
}
