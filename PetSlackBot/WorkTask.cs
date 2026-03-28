using System.Collections.Concurrent;

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

    public static bool Stop(WorkTask task)
    {
        var time = DateTime.Now - task.TimeFromLastStart;

        task.Status = WorkTaskType.Stopped;
        task.CurrentExecutionTime += time;

        return true;
    }
    public static bool Finish(WorkTask task)
    {
        var time = DateTime.Now - task.TimeFromLastStart;

        task.Status = WorkTaskType.Completed;
        task.CurrentExecutionTime += time;

        return true;
    }


    public static bool Continue(WorkTask task)
    {
        
        var time = DateTime.Now - task.TimeFromLastStart;

        task.Status = WorkTaskType.InProgress;
        task.TimeFromLastStart = DateTime.Now;

        return true;
   
    }
}
public static class UsersStorage
{
    public static ConcurrentDictionary<string, UserSession> UserSessions { get; set; } = new();
}

public class UserSession
{
    public string SessionId { get; set; }
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