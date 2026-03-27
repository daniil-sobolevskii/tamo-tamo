var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseHttpsRedirection();

// Slack endpoint
app.MapPost("/slack/commands", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();

    var userId = form["user_id"].ToString();
    var command = form["command"].ToString();

    return Results.Ok($"Бот жив 🐾 user: {userId}, command: {command}");
});

app.Run();