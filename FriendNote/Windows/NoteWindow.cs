using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using FriendNote.Localization;
using FriendNote.Service;

namespace FriendNote.Windows;

public class NoteWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly Configuration configuration;
    private ulong contentId;
    private string name = string.Empty;
    private string serverName = string.Empty;
    private string note = string.Empty;
    private bool isEditing;

    public NoteWindow(Plugin plugin)
        : base($"{Loc.AddNoteTitle}##AddNoteWindow", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        Size = new Vector2(360, 160);
        SizeCondition = ImGuiCond.FirstUseEver;

        this.plugin = plugin;
        configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public void Open(ulong targetContentId, string targetName, string targetServerName)
    {
        contentId = targetContentId;
        name = targetName;
        serverName = targetServerName;
        var savedNote = configuration.FriendNoteList.FirstOrDefault(x => x.ContentId == contentId);
        isEditing = savedNote != null;
        note = NoteService.FormatNote(savedNote?.Note, NoteService.NoteMaxLength);
        IsOpen = true;
    }

    public override void Draw()
    {
        var title = isEditing ? Loc.EditNoteTitle : Loc.AddNoteTitle;
        WindowName = $"{title}##AddNoteWindow";

        ImGui.Text(Loc.NoteLabel);
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextMultiline("##FriendNoteInput", ref note, NoteService.NoteMaxLength + 1, new Vector2(-1, 80));

        if (ImGui.Button(Loc.Save))
        {
            var savedNote = configuration.FriendNoteList.FirstOrDefault(x => x.ContentId == contentId);
            var trimmedNote = NoteService.FormatNote(note, NoteService.NoteMaxLength);

            if (string.IsNullOrWhiteSpace(trimmedNote))
            {
                if (savedNote != null)
                    configuration.FriendNoteList.Remove(savedNote);
            }
            else if (savedNote == null)
            {
                configuration.FriendNoteList.Add(new NoteList
                {
                    ContentId = contentId,
                    Note = trimmedNote,
                    FriendName = name,
                    ServerName = serverName,
                });
            }
            else
            {
                savedNote.Note = trimmedNote;
                savedNote.FriendName = name;
                savedNote.ServerName = serverName;
            }

            configuration.Save();
            plugin.ApplyNotes();
            IsOpen = false;
        }
    }
}
