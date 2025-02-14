﻿using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TestExecutor.Core;

class Program
{
    private static IHost _host;
    private static CancellationTokenSource cts = new CancellationTokenSource();

    public static async Task Main(string[] args)
    {
        Debug.Assert(args[0] == "--asm");
        var samples = Assembly.LoadFrom(args[1]);

        var host = CreateHostBuilder().Build();

        // var service = host.Services.GetService<ConcreteExecutorService>();
        // service.SamplesAssembly = samples;

        await host.RunAsync(cts.Token);
    }
    
    static IHostBuilder CreateHostBuilder() =>
        Host.CreateDefaultBuilder()
            .ConfigureServices(s =>
            {
                s.AddGrpc();
                // s.AddSingleton<ConcreteExecutorService>();
            })
            .ConfigureLogging(builder => builder.AddConsole(opt =>
            {
                opt.IncludeScopes = true;
                opt.TimestampFormat = "hh:mm:ss ";
            }))
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





