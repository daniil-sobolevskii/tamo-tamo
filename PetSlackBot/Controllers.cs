using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PetSlackBot;

[ApiController]
[Route("slack")]
public class SlackController : ControllerBase
{
    private readonly HttpClient _http = new();

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

        // 🔥 открыть DM
        var openResponse = await _http.PostAsJsonAsync(
            "https://slack.com/api/conversations.open",
            new { users = userId });

        var raw = await openResponse.Content.ReadAsStringAsync();
        Console.WriteLine("OPEN: " + raw);

        var openJson = JsonDocument.Parse(raw).RootElement;

        if (!openJson.GetProperty("ok").GetBoolean())
        {
            return Ok("error opening DM");
        }

        var channelId = openJson.GetProperty("channel").GetProperty("id").GetString();

        // 🔥 отправка сообщения
        await _http.PostAsJsonAsync(
            "https://slack.com/api/chat.postMessage",
            new
            {
                channel = channelId,
                text = "🐾 Тамагочи",
                blocks = new object[]
                {
                    new
                    {
                        type = "section",
                        text = new
                        {
                            type = "mrkdwn",
                            text = "*🐾 Тамагочи*\nЯ проснулся! Что будем делать?"
                        }
                    },
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
                            new {
                                type = "button",
                                text = new {
                                    type = "plain_text",
                                    text = "➕ Добавить задачу"
                                },
                                style = "primary",
                                action_id = "add_task"
                            }
                            }
                    }
                }
            });

        return Ok();
    }
    [HttpPost("actions")]
    public async Task<IActionResult> HandleActions()
    {
        var form = await Request.ReadFormAsync();
        var payload = form["payload"].ToString();

        var json = JsonDocument.Parse(payload).RootElement;

        var actionId = json.GetProperty("actions")[0].GetProperty("action_id").GetString();

        if (actionId == "add_task")
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
                        title = new {
                            type = "plain_text",
                            text = "Новая задача"
                        },
                        submit = new {
                            type = "plain_text",
                            text = "Сохранить"
                        },
                        close = new {
                            type = "plain_text",
                            text = "Отмена"
                        },
                        blocks = new object[]
                        {
                            new {
                                type = "input",
                                block_id = "task_input",
                                label = new {
                                    type = "plain_text",
                                    text = "Введите задачу"
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

        return Ok();
    }
}