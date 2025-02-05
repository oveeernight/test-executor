using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TestExecutor.Core;

class Program
{
    private static IHost _host;

    public static void Main(string[] args)
    {
        var samples = args[1];
        var asm = Assembly.LoadFrom(samples);

        var host = CreateHostBuilder().Build();

        host.Run();
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
                                endpoints.MapGet("/shutdown", async context =>
                                    await _host.StopAsync());
                                endpoints.MapGrpcService<ConcreteExecutorService>();
                            });
                    });
            });
}





