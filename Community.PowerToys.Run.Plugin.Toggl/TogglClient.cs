using System;
using System.Linq;
using Flurl.Http;

namespace Community.PowerToys.Run.Plugin.Toggl;

public class TogglClient
{
    private const string BaseUrl = "https://api.track.toggl.com/api/v9";
    private readonly string _togglApiKey;
    private long? _workspaceId;

    public TogglClient(string togglApiKey)
    {
        _togglApiKey = togglApiKey;
    }

    public void Start(string title)
    {
        var workspaceId = GetWorkspaceId();

        Post($"/workspaces/{workspaceId}/time_entries", new TogglStart
        {
            created_with = "PowerToysRun-Toggl",
            workspace_id = workspaceId,
            description = title,
            duration = -1,
            start = DateTime.UtcNow
        });
    }
    
    public void Stop(CurrentEntry currentEntry)
    {
        var workspaceId = GetWorkspaceId();
        Patch($"/workspaces/{workspaceId}/time_entries/{currentEntry.Id}/stop");
    }

    private void Post(string endpoint, object body)
    {
        $"{BaseUrl}{endpoint}"
            .WithBasicAuth(_togglApiKey, "api_token")
            .PostJsonAsync(body)
            .AsSync();
    }
    
    
    private void Patch(string endpoint)
    {
        $"{BaseUrl}{endpoint}"
            .WithBasicAuth(_togglApiKey, "api_token")
            .PatchAsync()
            .AsSync();
    }

    private TResult Get<TResult>(string endpoint)
    {
        return $"{BaseUrl}{endpoint}"
            .WithBasicAuth(_togglApiKey, "api_token")
            .GetAsync()
            .ReceiveJson<TResult>()
            .AsSync();
    }


    private long GetWorkspaceId()
    {
        if (_workspaceId.HasValue)
            return _workspaceId.Value;

        var workspaces = Get<TogglWorkspace[]>("/me/workspaces");

        //its simplified
        _workspaceId = workspaces.First().Id;
        return _workspaceId.Value;
    }

    //todo cache?
    public CurrentEntry GetCurrentEntry()
    {
        var entry = Get<TogglCurrentEntry>("/me/time_entries/current");
        if (entry is null)
            return null;

        return new CurrentEntry
        {
            Id = entry.id,
            Title = entry.description,
            StartsAt = entry.start
        };
    }

    private record TogglWorkspace
    {
        public long Id { get; init; }
        public string Name { get; init; }
    }

    private record TogglStart
    {
        public string created_with { get; set; }
        public string description { get; init; }
        public long workspace_id { get; init; }
        public int duration { get; init; }
        public DateTime start { get; init; }
    }

    private record TogglCurrentEntry
    {
        public long id { get; init; }
        public long workspace_id { get; init; }
        public long? project_id { get; init; }
        public long? task_id { get; init; }
        public bool billable { get; init; }
        public DateTime start { get; init; }
        public DateTime? stop { get; init; }
        public int duration { get; init; }
        public string description { get; init; }
        public string[] tags { get; init; }
        public long[] tag_ids { get; init; }
        public bool duronly { get; init; }
        public DateTime at { get; init; }
        public DateTime? server_deleted_at { get; init; }
        public long user_id { get; init; }
        public long uid { get; init; }
        public long wid { get; init; }
    }
}

public record CurrentEntry
{
    public required long Id { get; init; }
    public required string Title { get; init; }
    public required DateTime StartsAt { get; init; }
}