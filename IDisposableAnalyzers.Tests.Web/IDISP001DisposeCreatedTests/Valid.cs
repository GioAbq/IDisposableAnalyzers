namespace IDisposableAnalyzers.Tests.Web.IDISP001DisposeCreatedTests;

using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

public abstract class Valid
{
    protected abstract DiagnosticAnalyzer Analyzer { get; }

    private const string Disposable = @"
namespace N
{
    using System;
    using System.Threading.Tasks;

    public sealed class Disposable : IDisposable, IAsyncDisposable
    {
        public void Dispose()
        {
        }

        public ValueTask DisposeAsync()
        {
            throw new NotImplementedException();
        }
    }
}";

    [Fact]
    public void LocalDisposeAsync()
    {
        var code = @"
namespace N
{
    using System.IO;
    using System.Threading.Tasks;

    public class C
    {
        public async ValueTask M()
        {
            var x = File.OpenRead(string.Empty);
            await x.DisposeAsync();
        }
    }
}";

        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void LocalDisposeAsyncInFinally()
    {
        var code = @"
namespace N
{
    using System.IO;
    using System.Threading.Tasks;

    public class C
    {
        public async ValueTask M()
        {
            var x = File.OpenRead(string.Empty);
            try
            {

            }
            finally
            {
                await x.DisposeAsync();
            }
        }
    }
}";

        RoslynAssert.Valid(Analyzer, code);
    }

    [Fact]
    public void IServiceProviderGetRequiredService()
    {
        var code = @"
namespace N
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public class C
    {
        private readonly IServiceProvider serviceProvider;

        public C(IServiceProvider serviceProvider)
        {
            var disposable1 = serviceProvider.GetRequiredService<Disposable>();
            _ = serviceProvider.GetRequiredService<Disposable>();
            var loggerFactory1 = serviceProvider.GetRequiredService<ILoggerFactory>();
            _ = serviceProvider.GetRequiredService<ILoggerFactory>();

            this.serviceProvider = serviceProvider;
            var disposable2 = this.serviceProvider.GetRequiredService<Disposable>();
            _ = this.serviceProvider.GetRequiredService<Disposable>();
            var loggerFactory2 = this.serviceProvider.GetRequiredService<ILoggerFactory>();
            _ = this.serviceProvider.GetRequiredService<ILoggerFactory>();
        }

        public sealed class Disposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}";

        RoslynAssert.Valid(Analyzer, code);
    }

    [Theory]
    [InlineData("response.RegisterForDispose(new Disposable())")]
    [InlineData("response.RegisterForDisposeAsync(new Disposable())")]
    public void RegisterForDispose(string expression)
    {
        var code = @"
namespace N
{
    using Microsoft.AspNetCore.Http;

    public class C
    {
        public void M(HttpResponse response)
        {
            response.RegisterForDispose(new Disposable());
        }
    }
}".AssertReplace("response.RegisterForDispose(new Disposable())", expression);

        RoslynAssert.Valid(Analyzer, Disposable, code);
    }

    [Theory]
    [InlineData("serviceProvider.GetService<Disposable>()")]
    [InlineData("serviceProvider.GetRequiredService<Disposable>()")]
    public void Issue293(string expression)
    {
        var code = @"
namespace N
{
    using System;
    using Microsoft.Extensions.DependencyInjection;

    public static class Issue293
    {
        public static void M1(IServiceProvider serviceProvider)
        {
            var disposable = serviceProvider.GetService<Disposable>();
        }
    }
}".AssertReplace("serviceProvider.GetService<Disposable>()", expression);

        RoslynAssert.Valid(Analyzer, Disposable, code);
    }

    [Theory]
    [InlineData("app.Run()")]
    [InlineData("await app.RunAsync()")]
    [InlineData("await app.RunAsync().ConfigureAwait(false)")]
    public void AppRun(string expression)
    {
        var code = @"
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.Run();
".AssertReplace("app.Run()", expression);

        RoslynAssert.Valid(Analyzer, code, settings: WebSettings.Exe);
    }

    [Fact]
    public void Issue330()
    {
        var code = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(""/Error"");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
";

        RoslynAssert.Valid(Analyzer, code, settings: WebSettings.Exe);
    }
}
