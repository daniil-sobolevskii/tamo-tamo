using System.Text.Json;

namespace PetSlackBot;

public class SlackActionHandler
{
    private readonly HttpClient _http;
    private readonly MediaService mediaService;

    public SlackActionHandler(HttpClient http)
    {
        _http = http;
        mediaService = new MediaService();
    }

    public async Task Handle(JsonElement json, string action, UserSession session)
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
                            new
                            {
                                type = "input",
                                block_id = "task_input",
                                label = new
                                {
                                    type = "plain_text",
                                    text = "Add task name"
                                },
                                element = new
                                {
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
            var lastTask = session.Tasks.FirstOrDefault(x => x.Status == WorkTaskType.InProgress);
            WorkTask.Stop(lastTask);

            var endDay = CalculateTotalTime(session.Tasks) > TimeSpan.FromMinutes(5);
            var buttons = new List<object>
            {
                BuildButton("▶️ Continue", Actions.Resume, "primary"),
                BuildButton("❌ Cancel", Actions.Cancel, "danger")
            };

            if (endDay)
            {
                buttons.Add(BuildButton("🏁 End day", Actions.EndDay));
            }

            await UpdateMessage(
                session,
                "☕ *Перерыв*\nЗадача поставлена на паузу",
                mediaService.GetGifPathByPetState(PetState.Satiety),
                buttons.ToArray());
        }

        if (action == Actions.Cancel)
        {
            var task = session.Tasks.FirstOrDefault(x =>
                x.Status == WorkTaskType.InProgress || x.Status == WorkTaskType.Stopped);
            WorkTask.Finish(task);

            var time = Format(task.CurrentExecutionTime);

            await UpdateMessage(
                session,
                $"❌ *Cancelled*\n⏱ Time spent: *{time}*",
                mediaService.GetGifPathByPetState(PetState.Neutral),
                new[]
                {
                    BuildButton("➕ New task", Actions.AddTask),
                    BuildButton("Lunch", Actions.Lunch),
                });
        }

        if (action == Actions.Finish)
        {
            var task = session.Tasks.FirstOrDefault(x => x.Status == WorkTaskType.InProgress);
            WorkTask.Finish(task);
            var time = Format(task.CurrentExecutionTime);

            await UpdateMessage(
                session,
                $"✅ *Task completed!*\n⏱ Time spent: *{time}*",
                mediaService.GetGifPathByPetState(PetState.Happiness),
                new[]
                {
                    BuildButton("➕ New task", Actions.AddTask),
                    BuildButton("Lunch", Actions.Lunch),
                });
        }

        if (action == Actions.Lunch)
        {
            var task = session.Tasks.First();
            if (task.Status == WorkTaskType.InProgress)
            {
                WorkTask.Stop(task);
            }

            await UpdateMessage(
                session,
                "Eating",
                mediaService.GetGifPathByPetState(PetState.Satiety),
                new[]
                {
                    BuildButton("Stop Lunch", Actions.Resume),
                });
        }

        if (action == Actions.Resume)
        {
            var task = session.Tasks.First();
            if (task.Status == WorkTaskType.Stopped)
            {
                WorkTask.Continue(task);
                await UpdateMessage(
                    session,
                    "Resunme",
                    mediaService.GetGifPathByPetState(PetState.Working),
                    new[]
                    {
                        BuildButton("☕ Coffee break", Actions.Break, "primary"),
                        BuildButton("✅ Finish", Actions.Finish),
                        BuildButton("❌ Cancel", Actions.Cancel, "danger")
                    });
                return;
            }

            await UpdateMessage(
                session,
                "Resunme",
                mediaService.GetGifPathByPetState(PetState.Working),
                new[]
                {
                    BuildButton("➕ New task", Actions.AddTask),
                    BuildButton("☕ Coffee break", Actions.Break, "primary"),
                });
        }

        if (action == Actions.EndDay)
        {
            if (session.Tasks.Count == 0)
            {
                await UpdateMessage(
                    session,
                    "📭 *No tasks was today*",
                    mediaService.GetGifPathByPetState(PetState.Neutral),
                    new[]
                    {
                        BuildButton("➕ New task", Actions.AddTask)
                    });

                return;
            }

            var lines = new List<string>();
            TimeSpan total = TimeSpan.Zero;

            foreach (var task in session.Tasks)
            {
                var time = task.CurrentExecutionTime;
                total += time;

                lines.Add($"• *{task.Title}* — {Format(time)}");
            }

            var summary = string.Join("\n", lines);
            var totalTime = Format(total);

            await UpdateMessage(
                session,
                $"🏁 *End of day summary*\n\n{summary}\n\n⏱ *Total: {totalTime}*",
                mediaService.GetGifPathByPetState(PetState.Happiness),
                new[]
                {
                    BuildButton("➕ Start new day", Actions.AddTask, "primary")
                });

            // опционально: очистить день
            session.Tasks.Clear();
        }
    }

    private async Task UpdateMessage(
        UserSession session,
        string text,
        string? imageUrl,
        object[] buttons)
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

        blocks.Add(new
        {
            type = "actions",
            elements = buttons
        });

        await _http.PostAsJsonAsync(
            "https://slack.com/api/chat.update",
            new
            {
                channel = session.SessionId,
                ts = session.MessageTs,
                text = text,
                blocks = blocks.ToArray()
            });
    }

    private object BuildButton(string text, string actionId, string? style = null)
    {
        var button = new Dictionary<string, object>
        {
            ["type"] = "button",
            ["text"] = new
            {
                type = "plain_text",
                text = text
            },
            ["action_id"] = actionId
        };

        if (!string.IsNullOrEmpty(style))
        {
            button["style"] = style;
        }

        return button;
    }

    private string Format(TimeSpan time)
    {
        return $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}";
    }

    private TimeSpan CalculateTotalTime(ICollection<WorkTask> tasks)
    {
        var totalTime = TimeSpan.Zero;

        foreach (var task in tasks)
        {
            if (task.Status == WorkTaskType.InProgress)
            {
                var time = DateTime.UtcNow - task.TimeFromLastStart;
                totalTime += time;
            }
            else
            {
                totalTime += task.CurrentExecutionTime;
            }
        }

        return totalTime;
    }
}