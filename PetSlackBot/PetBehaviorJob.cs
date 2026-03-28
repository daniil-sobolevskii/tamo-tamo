using System.Net.Http.Headers;
using System.Text;
using static System.Net.WebRequestMethods;

namespace PetSlackBot;

public class PetBehaviorJob
{
    private const int ProcessIntervalSeconds = 60;

    private readonly TimeSpan TaskWorkingTimeBound = TimeSpan.FromMinutes(150);

    private readonly HttpClient _httpClient = new HttpClient();
    private readonly MediaService _mediaService = new MediaService();

    private CancellationTokenSource _tokenSource;
    private Task _processing;

    public PetBehaviorJob()
    {
        Start();
    }

    private void Start()
    {
        if (_processing != null)
        {
            return;
        }

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "");

        _tokenSource = new CancellationTokenSource();
        _processing = Task.Run(ProcessAsync);
    }

    private async Task ProcessAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(ProcessIntervalSeconds));

        try
        {
            var stoppingToken = _tokenSource.Token;

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await CheckUserSessionsAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            //
        }
    }

    private async Task CheckUserSessionsAsync()
    {
        var sessions = UsersStorage.UserSessions;

        foreach (var session in sessions)
        {
            foreach (var task in session.Value.Tasks)
            {
                if (task.Status == WorkTaskType.InProgress)
                {
                    var timeFromLastStart = DateTime.Now - task.TimeFromLastStart;

                    if (timeFromLastStart > TaskWorkingTimeBound)
                    {
                        WorkTask.Stop(task);

                        await SendToSlack(session);
                    }
                }
            }
        }
    }

    private async Task SendToSlack(KeyValuePair<string, UserSession> session)
    {
        var channelId = session.Value.SessionId;

        var payload = new
        {
            channel = channelId,
            text = "Your task is stopped 😢",
            blocks = new object[]
            {
                new
                {
                    type = "section",
                    text = new
                    {
                        type = "mrkdwn",
                        text = "⏰ *Task stopped*\nYou have been active for too long"
                    }
                },
                new
                {
                    type = "image",
                    image_url = _mediaService.GetGifPathByPetState(PetState.Tired),
                    alt_text = "sad pet"
                }
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(payload);

        var request = new HttpRequestMessage(HttpMethod.Post, "https://slack.com/api/chat.postMessage");
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "");

        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        Console.WriteLine(responseBody);
    }
}

