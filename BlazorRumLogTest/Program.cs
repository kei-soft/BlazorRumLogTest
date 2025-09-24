using BlazorRumLogTest.Components;

using Elastic.Apm.SerilogEnricher;

using Serilog;
using Serilog.Sinks.Elasticsearch;

namespace BlazorRumLogTest;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // Elastic APM ���� ���
        builder.Services.AddElasticApm();

        // Serilog ���� - Elasticsearch�� ���� ����
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentUserName()
            .Enrich.WithElasticApmCorrelationInfo() // APM ������� ���� �߰�
            .WriteTo.Console()
            .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:19200"))
            {
                IndexFormat = "blazor-logs-{0:yyyy.MM.dd}",
                AutoRegisterTemplate = true,
                NumberOfShards = 2,
                NumberOfReplicas = 1,
                EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog,
                ConnectionTimeout = TimeSpan.FromSeconds(5),
                //ModifyConnectionSettings = x => x.BasicAuthentication("username", "password") // �ʿ��
            })
            .CreateLogger();

        builder.Host.UseSerilog();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }


        // Serilog ��û �α�
        app.UseSerilogRequestLogging();

        // Elastic APM �̵���� �߰�
        //app.UseElasticApm();

        app.UseMiddleware<ElasticApmExceptionMiddleware>();

        app.UseHttpsRedirection();

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}
