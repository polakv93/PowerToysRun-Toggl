using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Humanizer;
using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.Toggl;

/// <summary>
///     Main class of this plugin that implement all used interfaces.
/// </summary>
public class Main : IPlugin, IContextMenu, IDisposable, ISettingProvider
{
    /// <summary>
    ///     ID of the plugin.
    /// </summary>
    public static string PluginID => "ABDA11571F3D40A58F187F038F62DBEA";

    private PluginInitContext Context { get; set; }

    private string _offIconPath;
    private string _workingIconPath;
    private string _startNewIconPath;

    private bool Disposed { get; set; }

    public string TogglApiKey { get; set; }
    private TogglClient _togglClient { get; set; }

    /// <summary>
    ///     Return a list context menu entries for a given <see cref="Result" /> (shown at the right side of the result).
    /// </summary>
    /// <param name="selectedResult">The <see cref="Result" /> for the list with context menu entries.</param>
    /// <returns>A list context menu entries.</returns>
    public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
    {
        return [];
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Name of the plugin.
    /// </summary>
    public string Name => "Toggl";

    /// <summary>
    ///     Description of the plugin.
    /// </summary>
    public string Description => "Manage toggl from powertoys run";

    /// <summary>
    ///     Return a filtered list, based on the given query.
    /// </summary>
    /// <param name="query">The query to filter the list.</param>
    /// <returns>A filtered list, can be empty when nothing was found.</returns>
    public List<Result> Query(Query query)
    {
        if (string.IsNullOrWhiteSpace(TogglApiKey))
            return
            [
                new Result
                {
                    Title = $"Error{nameof(TogglApiKey)} is not set in PowerToys Run settings"
                }
            ];

        var search = query.Search;

        List<Result> results = [];

        if (!string.IsNullOrWhiteSpace(search))
            AddStartNewEntry(results, search);
        
        AddEntries(results, search);

        return results;
    }

    private void AddEntries(List<Result> results, string search)
    {
        var isAnySearchPhrase = !string.IsNullOrWhiteSpace(search);
        var entires = _togglClient.GetEntries();
        foreach (var entry in entires)
        {
            if (entry.StopAt.HasValue is false)
            {
                results.Add(new Result
                {
                    IcoPath = _workingIconPath,
                    Title = $"{entry.Title}",
                    SubTitle = $"Currently running for {(DateTime.Now - entry.StartsAt).Humanize(2)}, enter to stop",
                    Action = _ =>
                    {
                        _togglClient.Stop(entry);
                        Context.API.ShowNotification($"Stopped {entry.Title}");
                        return true;
                    }
                });
                continue;
            }

            if (!isAnySearchPhrase || entry.Title.Contains(search, StringComparison.InvariantCultureIgnoreCase))
            {
                results.Add(new Result
                {
                    IcoPath = _workingIconPath,
                    Title = $"{entry.Title} - {(entry.StopAt.Value - entry.StartsAt).Humanize(2)}",
                    SubTitle = "Enter to start again",
                    QueryTextDisplay = entry.Title,
                    Action = _ =>
                    {
                        _togglClient.Start(entry);
                        Context.API.ShowNotification($"Started {entry.Title}");
                        return true;
                    }
                });
            }
        }
    }

    /// <summary>
    ///     Initialize the plugin with the given <see cref="PluginInitContext" />.
    /// </summary>
    /// <param name="context">The <see cref="PluginInitContext" /> for this plugin.</param>
    public void Init(PluginInitContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Context.API.ThemeChanged += OnThemeChanged;
        UpdateIconPath(Context.API.GetCurrentTheme());
    }

    public Control CreateSettingPanel()
    {
        throw new NotImplementedException();
    }

    public void UpdateSettings(PowerLauncherPluginSettings settings)
    {
        TogglApiKey = settings.AdditionalOptions.SingleOrDefault(x => x.Key == nameof(TogglApiKey))?.TextValue;
        _togglClient = new TogglClient(TogglApiKey);
    }

    public IEnumerable<PluginAdditionalOption> AdditionalOptions =>
    [
        new()
        {
            Key = nameof(TogglApiKey),
            DisplayLabel = "Toggl API Key",
            DisplayDescription = "Can be found under https://track.toggl.com/profile > API Token section",
            PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Textbox,
            TextValue = TogglApiKey
        }
    ];

    private void AddStartNewEntry(List<Result> results, string search)
    {
        results.Add(
            new Result
            {
                QueryTextDisplay = search,
                IcoPath = _startNewIconPath,
                Title = "Start: " + search,
                SubTitle = "Select to start",
                Action = _ =>
                {
                    _togglClient.Start(search);
                    Context.API.ShowNotification($"Started {search}");
                    return true;
                },
                ContextData = search
            });
    }

    /// <summary>
    ///     Wrapper method for <see cref="Dispose()" /> that dispose additional objects and events form the plugin itself.
    /// </summary>
    /// <param name="disposing">Indicate that the plugin is disposed.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (Disposed || !disposing) return;

        if (Context?.API != null) Context.API.ThemeChanged -= OnThemeChanged;

        Disposed = true;
    }

    private void UpdateIconPath(Theme theme)
    {
        var variant = theme is Theme.Light or Theme.HighContrastWhite ? "light" : "dark";
        
        _startNewIconPath = $"Images/hourglass-high.{variant}.png";
        _workingIconPath = $"Images/hourglass.{variant}.png";
        _offIconPath = $"Images/hourglass-off.{variant}.png";
    }


    private void OnThemeChanged(Theme currentTheme, Theme newTheme)
    {
        UpdateIconPath(newTheme);
    }
}