using System.Text.Json;

namespace PetSlackBot;


public class SlackActionHandler
{
    private readonly HttpClient _http;

    public SlackActionHandler(HttpClient http)
    {
        _http = http;
    }

    public async Task Handle(JsonElement json, string action)
    {
        if (action == Actions.AddTask)
        {
            var triggerId = json.GetProperty("trigger_id").GetString();

            await _http.PostAsJsonAsync(
                "https://slack.com/api/views.open",
                new
                {
                    trigger_id = triggerId,
                    view = new
                    {
                        type = "modal",
                        title = new { type = "plain_text", text = "New task" },
                        submit = new { type = "plain_text", text = "Start!" },
                        close = new { type = "plain_text", text = "Not now" },
                        blocks = new object[]
                        {
                            new {
                                type = "input",
                                block_id = "task_input",
                                label = new {
                                    type = "plain_text",
                                    text = "Add task name"
                                },
                                element = new {
                                    type = "plain_text_input",
                                    action_id = "task_value"
                                }
                            }
                        }
                    }
                });
        }

        if (action == Actions.Break)
        {
            await UpdateMessage(json, "☕ Перерыв", "https://media.giphy.com/media/l0HlBO7eyXzSZkJri/giphy.gif");
        }

        if (action == Actions.Cancel)
        {
            await UpdateMessage(json, "❌ Отменено", null);
        }

        if (action == Actions.Finish)
        {
            await UpdateMessage(json, "✅ Завершено", "https://media.giphy.com/media/111ebonMs90YLu/giphy.gif");
        }
    }

    private async Task UpdateMessage(JsonElement json, string text, string? imageUrl)
    {
        var blocks = new List<object>
        {
            new
            {
                type = "section",
                text = new
                {
                    type = "mrkdwn",
                    text = text
                }
            }
        };

        if (!string.IsNullOrEmpty(imageUrl))
        {
            blocks.Add(new
            {
                type = "image",
                image_url = imageUrl,
                alt_text = "image"
            });
        }

        await _http.PostAsJsonAsync(
            "https://slack.com/api/chat.update",
            new
            {
                channel = json.GetProperty("channel").GetProperty("id").GetString(),
                ts = json.GetProperty("message").GetProperty("ts").GetString(),
                text = text,
                blocks = blocks
            });
    }
}