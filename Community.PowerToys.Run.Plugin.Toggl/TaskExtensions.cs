using System.Threading.Tasks;

namespace Community.PowerToys.Run.Plugin.Toggl;

public static class TaskExtensions
{
    public static TResult AsSync<TResult>(this Task<TResult> task)
    {
        return task.ConfigureAwait(false).GetAwaiter().GetResult();
    }
}