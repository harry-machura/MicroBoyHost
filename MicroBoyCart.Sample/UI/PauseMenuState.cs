using System.Collections.Generic;

namespace MicroBoyCart.Sample.UI;

public sealed class PauseMenuState
{
    private const string SaveEntryId = "save";
    private const string SettingsEntryId = "settings";
    private const string ResumeEntryId = "resume";

    private readonly List<MenuEntry> entries = new()
    {
        new MenuEntry(SaveEntryId, "SAVE GAME", "WRITE PROGRESS"),
        new MenuEntry(SettingsEntryId, "SETTINGS", "COMING SOON"),
        new MenuEntry(ResumeEntryId, "RESUME", "RETURN TO PLAY"),
    };

    public bool IsOpen { get; private set; }
    public int SelectedIndex { get; private set; }
    public IReadOnlyList<MenuEntry> Entries => entries;
    public MenuEntry SelectedEntry => entries[SelectedIndex];

    public void Open()
    {
        if (IsOpen)
        {
            return;
        }

        IsOpen = true;
        SelectedIndex = 0;
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        SelectedIndex = 0;
    }

    public void MoveNext()
    {
        if (!IsOpen || entries.Count == 0)
        {
            return;
        }

        SelectedIndex = (SelectedIndex + 1) % entries.Count;
    }

    public void MovePrevious()
    {
        if (!IsOpen || entries.Count == 0)
        {
            return;
        }

        SelectedIndex = (SelectedIndex - 1 + entries.Count) % entries.Count;
    }

    public bool IsSaveSelected() => SelectedEntry.Id == SaveEntryId;
    public bool IsSettingsSelected() => SelectedEntry.Id == SettingsEntryId;
    public bool IsResumeSelected() => SelectedEntry.Id == ResumeEntryId;

    public readonly record struct MenuEntry(string Id, string Title, string? Subtext);
}
