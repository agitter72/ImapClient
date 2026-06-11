using ImapClient.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();
    });
});
builder.Services.AddScoped<IImapMailService, ImapMailService>();

var app = builder.Build();

app.UseCors("frontend");
app.MapControllers();

app.Run();
