var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5207");

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.UseStaticFiles();

app.Run();