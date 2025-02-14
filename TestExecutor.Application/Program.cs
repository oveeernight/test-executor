using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TestExecutor.Core;

class Program
{
    private static IHost _host;
    private static CancellationTokenSource cts = new CancellationTokenSource();

    public static void Main(string[] args)
    {
        var samples = args[1];
        // var asm = Assembly.LoadFrom(samples);

        var host = CreateHostBuilder().Build();

        host.RunAsync(cts.Token);
    }
    
    static IHostBuilder CreateHostBuilder() =>
        Host.CreateDefaultBuilder()
            .ConfigureServices(s => s.AddGrpc())
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel(options =>
                        options.ListenLocalhost(8980, listenOptions =>
                            listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2))
                    .Configure(app =>
                    {
                        app.UseRouting()
                            .UseEndpoints(endpoints =>
                            {
                                endpoints.MapGrpcService<ConcreteExecutorService>();
                            });
                    });
            });
}





