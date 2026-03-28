namespace PetSlackBot;


public class WorkTask
{
    public string Title { get; set; }
    public WorkTaskType Status { get; set; }
    public TimeSpan CurrentExecutionTime { get; set; }
    public DateTime TimeFromLastStart { get; set; }

    public static WorkTask Create(string title)
    {
        return new WorkTask
        {
            Title = title,
            Status = WorkTaskType.InProgress,
            CurrentExecutionTime = TimeSpan.Zero,
            TimeFromLastStart = DateTime.Now
        };
    }

    public static WorkTask Stop(WorkTask task)
    {
        var time = DateTime.Now - task.TimeFromLastStart;

        return new WorkTask
        {
            Status = WorkTaskType.Stopped,
            CurrentExecutionTime = task.CurrentExecutionTime + time
        };
    }

    public static WorkTask Continue(WorkTask task)
    {
        return new WorkTask
        {
            Status = WorkTaskType.InProgress,
            TimeFromLastStart = DateTime.Now
        };
    }
}
public static class UsersStorage
{
    public static Dictionary<string, UserSession> UserSessions { get; set; } = new();
}

public class UserSession
{
    public string UserId { get; set; }
    public ICollection<WorkTask> Tasks { get; set; } = new List<WorkTask>();
    public DateTime EndTime { get; set; }

    // Why?
    public string MessageTs { get; set; }
}


public enum WorkTaskType
{
    Stopped,
    InProgress,
    Completed
}