using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PetSlackBot;

[ApiController]
[Route("slack")]
public class SlackController : ControllerBase
{
    private readonly HttpClient _http = new();

    private readonly SlackActionHandler _handler;

    // 🔥 простое хранилище (в памяти)
    private static Dictionary<string, UserSession> _sessions = new();

    public SlackController()
    {
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "");
        _handler = new SlackActionHandler(_http);
    }


    [HttpPost("commands")]
    public async Task<IActionResult> HandleCommand()
    {
        var form = await Request.ReadFormAsync();
        var userId = form["user_id"].ToString();

        var openResponse = await _http.PostAsJsonAsync(
            "https://slack.com/api/conversations.open",
            new { users = userId });

        var openJson = await openResponse.Content.ReadFromJsonAsync<JsonElement>();
        var channelId = openJson.GetProperty("channel").GetProperty("id").GetString();

        var msgResponse = await _http.PostAsJsonAsync(
            "https://slack.com/api/chat.postMessage",
            new
            {
                channel = channelId,
                text = "🐾",
                blocks = new object[]
                {
                    new
                    {
                        type = "image",
                        image_url = "https://media.giphy.com/media/JIX9t2j0ZTN9S/giphy.gif",
                        alt_text = "pet"
                    },
                    new
                    {
                        type = "actions",
                        elements = new object[]
                        {
                            new
                            {
                                type = "button",
                                text = new
                                {
                                    type = "plain_text",
                                    text = "Let's start!?"
                                },
                                style = "primary",
                                action_id = Actions.AddTask
                            }
                        }
                    }
                }
            });

        var msgJson = await msgResponse.Content.ReadFromJsonAsync<JsonElement>();
        var ts = msgJson.GetProperty("ts").GetString();

        // 3. Сохраняем сессию
        _sessions[userId] = new UserSession
        {
            UserId = channelId,
            MessageTs = ts
        };

        return Ok();
    }


    [HttpPost("actions")]
    public async Task<IActionResult> HandleActions()
    {
        var form = await Request.ReadFormAsync();
        var payload = form["payload"].ToString();

        var json = JsonDocument.Parse(payload).RootElement;
        var type = json.GetProperty("type").GetString();

        if (type == "block_actions")
        {
            var action = json.GetProperty("actions")[0]
                .GetProperty("action_id").GetString();

            await _handler.Handle(json, action);
        }

        else if (type == "view_submission")
        {
            var userId = json.GetProperty("user").GetProperty("id").GetString();

            var state = json.GetProperty("view")
                .GetProperty("state")
                .GetProperty("values");

            var task = state
                .GetProperty("task_input")
                .GetProperty("task_value")
                .GetProperty("value")
                .GetString();

            var session = _sessions[userId];

            var newTask = WorkTask.Create(task);
            session.Tasks.Add(newTask);

            await _http.PostAsJsonAsync(
                "https://slack.com/api/chat.update",
                new
                {
                    channel = session.UserId,
                    ts = session.MessageTs,
                    text = "Working...",
                    blocks = new object[]
                    {
                        new
                        {
                            type = "section",
                            text = new
                            {
                                type = "mrkdwn",
                                text = $"*💻 Работаю*\nЗадача: *{task}*"
                            }
                        },
                        new
                        {
                            type = "actions",
                            elements = new object[]
                            {
                                new
                                {
                                    type = "button",
                                    text = new
                                    {
                                        type = "plain_text",
                                        text = "☕ Coffee break"
                                    },
                                    action_id = Actions.Break
                                },
                                new
                                {
                                    type = "button",
                                    text = new
                                    {
                                        type = "plain_text",
                                        text = "Cancel"
                                    },
                                    action_id = Actions.Cancel,
                                    style = "danger"
                                },
                                new
                                {
                                    type = "button",
                                    text = new
                                    {
                                        type = "plain_text",
                                        text = "✅ Finish"
                                    },
                                    style = "primary",
                                    action_id = Actions.Finish
                                }
                            }
                        }
                    }
                });
        }


        return Ok();
    }
}

