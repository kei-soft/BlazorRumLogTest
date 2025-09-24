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

        // Elastic APM 서비스 등록
        builder.Services.AddElasticApm();

        // Serilog 설정 - Elasticsearch로 직접 전송
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentUserName()
            .Enrich.WithElasticApmCorrelationInfo() // APM 상관관계 정보 추가
            .WriteTo.Console()
            .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:19200"))
            {
                IndexFormat = "blazor-logs-{0:yyyy.MM.dd}",
                AutoRegisterTemplate = true,
                NumberOfShards = 2,
                NumberOfReplicas = 1,
                EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog,
                ConnectionTimeout = TimeSpan.FromSeconds(5),
                //ModifyConnectionSettings = x => x.BasicAuthentication("username", "password") // 필요시
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


        // Serilog 요청 로깅
        app.UseSerilogRequestLogging();

        // Elastic APM 미들웨어 추가
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
