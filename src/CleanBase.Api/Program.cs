using CleanBase.Api;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseSentry(options =>
{
    options.Dsn = builder.Configuration["Sentry:Dsn"];
    options.Environment = builder.Environment.EnvironmentName;
    // Adjust the trace sample rate based on the environment.
    options.TracesSampler = context => builder.Environment.IsProduction() ? 1.0f : 0.0f;
});

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
);

builder.Services.AddApi(builder.Configuration);

var app = builder.Build();

await app.ConfigureApi();

app.Run();
