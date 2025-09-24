using Elastic.Apm;
using Elastic.Apm.Api;

namespace BlazorRumLogTest
{
    public class ElasticApmExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ElasticApmExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Exception을 Elastic APM에 자동으로 보고
                if (Agent.IsConfigured)
                {
                    Agent.Tracer.CaptureException(ex, labels: new Dictionary<string, Label>
                {
                    { "http_method", context.Request.Method },
                    { "http_path", context.Request.Path.ToString() },
                    { "http_query", context.Request.QueryString.ToString() },
                    { "user_agent", context.Request.Headers.UserAgent.ToString() }
                });
                }

                throw;
            }
        }
    }

}
