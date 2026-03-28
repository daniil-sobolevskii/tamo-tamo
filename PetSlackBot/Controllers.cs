using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PetSlackBot;

[ApiController]
[Route("slack")]
public class SlackController : ControllerBase
{
    private readonly HttpClient _http = new();
    private readonly MediaService mediaService;
    private readonly SlackActionHandler _handler;

    public SlackController()
    {
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "");
        _handler = new SlackActionHandler(_http);
        mediaService = new MediaService();
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
                        image_url = mediaService.GetGifPathByPetState(PetState.Greetings),
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
                                    text = @"
                                        I'm awake! What'll we do?
                                        Check Kiri tutorial here - https://docs.google.com/presentation/d/1Zlbq5dU6u8l9zGMuZN7qiHBQpH4S0bF64Q0ECrSX4SM/edit?slide=id.p1#slide=id.p1.
                                        Let's start!?"

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

        UsersStorage.UserSessions[userId] = new UserSession
        {
            SessionId = channelId,
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
        var userId = json.GetProperty("user").GetProperty("id").GetString();
        UsersStorage.UserSessions.TryGetValue(userId, out var session);
        
        var type = json.GetProperty("type").GetString();

        if (type == "block_actions")
        {
            var action = json.GetProperty("actions")[0]
                .GetProperty("action_id").GetString();

            await _handler.Handle(json, action, session);
        }

        else if (type == "view_submission")
        {

            var state = json.GetProperty("view")
                .GetProperty("state")
                .GetProperty("values");

            var task = state
                .GetProperty("task_input")
                .GetProperty("task_value")
                .GetProperty("value")
                .GetString();
            
            var newTask = WorkTask.Create(task);
            session.Tasks.Add(newTask);

            await _http.PostAsJsonAsync(
                "https://slack.com/api/chat.update",
                new
                {
                    channel = session.SessionId,
                    ts = session.MessageTs,
                    text = "Working...",
                    blocks = new object[]
                    {
                        new
                        {
                            type = "image",
                            image_url = mediaService.GetGifPathByPetState(PetState.Satiety),
                            alt_text = "pet",
                            
                        },
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

