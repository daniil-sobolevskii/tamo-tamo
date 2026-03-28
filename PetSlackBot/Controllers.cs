using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PetSlackBot;

[ApiController]
[Route("slack")]
public class SlackController : ControllerBase
{
    private readonly HttpClient _http = new();

    // 🔥 простое хранилище (в памяти)
    private static Dictionary<string, UserSession> _sessions = new();

    public SlackController()
    {
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "");
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
                                action_id = Action.AddTask
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
            ChannelId = channelId,
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

            await HandleBlockActions(json, action);
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

            session.Task = task;

            await _http.PostAsJsonAsync(
                "https://slack.com/api/chat.update",
                new
                {
                    channel = session.ChannelId,
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
                                    action_id = Action.Break
                                },
                                new
                                {
                                    type = "button",
                                    text = new
                                    {
                                        type = "plain_text",
                                        text = "Cancel"
                                    },
                                    action_id = Action.Cancel,
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
                                    action_id = Action.Finish
                                }
                            }
                        }
                    }
                });

            // 🔥 таймер
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(1));

                await _http.PostAsJsonAsync(
                    "https://slack.com/api/chat.update",
                    new
                    {
                        channel = session.ChannelId,
                        ts = session.MessageTs,
                        text = "Время",
                        blocks = new object[]
                        {
                            new
                            {
                                type = "section",
                                text = new
                                {
                                    type = "mrkdwn",
                                    text = "⏰ *Время вышло!*\nПора отдохнуть ☕"
                                }
                            },
                            new
                            {
                                type = "image",
                                image_url = "https://media.giphy.com/media/3o7btPCcdNniyf0ArS/giphy.gif",
                                alt_text = "break"
                            }
                        }
                    });
            });
        }

        return Ok();
    }

    
      private async Task HandleBlockActions(JsonElement json, string action)
    {
        if (action == Action.AddTask)
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

        if (action == Action.Break)
        {
            await _http.PostAsJsonAsync(
                "https://slack.com/api/chat.update",
                new
                {
                    channel = json.GetProperty("channel").GetProperty("id").GetString(),
                    ts = json.GetProperty("message").GetProperty("ts").GetString(),
                    text = "Break",
                    blocks = new object[]
                    {
                        new {
                            type = "section",
                            text = new {
                                type = "mrkdwn",
                                text = "☕ *Перерыв*\nОтдохни немного"
                            }
                        },
                        new {
                            type = "image",
                            image_url = "https://media.giphy.com/media/l0HlBO7eyXzSZkJri/giphy.gif",
                            alt_text = "break"
                        }
                    }
                });
        }
        if (action == Action.Cancel)
        {
            await _http.PostAsJsonAsync(
                "https://slack.com/api/chat.update",
                new
                {
                    channel = json.GetProperty("channel").GetProperty("id").GetString(),
                    ts = json.GetProperty("message").GetProperty("ts").GetString(),
                    text = "Break",
                    blocks = new object[]
                    {
                        new {
                            type = "section",
                            text = new {
                                type = "mrkdwn",
                                text = "☕ *Перерыв*\nОтдохни немного"
                            }
                        },
                        new {
                            type = "image",
                            image_url = "https://media.giphy.com/media/l0HlBO7eyXzSZkJri/giphy.gif",
                            alt_text = "break"
                        }
                    }
                });
        }
        if (action == Action.Finish)
        {
            await _http.PostAsJsonAsync(
                "https://slack.com/api/chat.update",
                new
                {
                    channel = json.GetProperty("channel").GetProperty("id").GetString(),
                    ts = json.GetProperty("message").GetProperty("ts").GetString(),
                    text = "Done",
                    blocks = new object[]
                    {
                        new {
                            type = "section",
                            text = new {
                                type = "mrkdwn",
                                text = "✅ *Задача завершена!*"
                            }
                        },
                        new {
                            type = "image",
                            image_url = "https://media.giphy.com/media/111ebonMs90YLu/giphy.gif",
                            alt_text = "done"
                        }
                    }
                });
        }
    }

    class UserSession
    {
        public string Task { get; set; }
        public DateTime EndTime { get; set; }
        public string ChannelId { get; set; }
        public string MessageTs { get; set; }
    }

}