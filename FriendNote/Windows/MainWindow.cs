using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using FriendNote.Localization;

namespace FriendNote.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    private readonly Plugin plugin;

    public MainWindow(Plugin plugin)
        : base($"{Loc.NoteListTitle}##NoteList")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;
        this.configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        WindowName = $"{Loc.NoteListTitle}##NoteList";

        var notes = configuration.FriendNoteList;

        if (notes.Count == 0)
        {
            ImGui.TextDisabled(Loc.NoSavedNotes);
            return;
        }

        var tableFlags =
            ImGuiTableFlags.Borders |
            ImGuiTableFlags.RowBg |
            ImGuiTableFlags.Resizable |
            ImGuiTableFlags.ScrollY |
            ImGuiTableFlags.SizingStretchProp;

        var deleteIndex = -1;

        if (ImGui.BeginTable("##SavedNotesTable", 3, tableFlags, new Vector2(-1, -1)))
        {
            ImGui.TableSetupColumn(Loc.FriendNameColumn);
            ImGui.TableSetupColumn(Loc.NoteColumn);
            ImGui.TableSetupColumn(Loc.ActionColumn, ImGuiTableColumnFlags.WidthFixed, 120);
            ImGui.TableHeadersRow();

            for (var i = 0; i < notes.Count; i++)
            {
                var item = notes[i];

                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.TextUnformatted($"{item.FriendName}{item.ServerName}");

                ImGui.TableSetColumnIndex(1);
                ImGui.TextWrapped(string.IsNullOrWhiteSpace(item.Note) ? Loc.EmptyNote : item.Note);

                ImGui.TableSetColumnIndex(2);
                ImGui.PushID(i);
                if (ImGui.Button(Loc.Edit))
                    plugin.AddNote(item.ContentId, item.FriendName, item.ServerName);
                ImGui.SameLine();
                if (ImGui.Button(Loc.Delete))
                    deleteIndex = i;
                ImGui.PopID();
            }

            ImGui.EndTable();
        }

        if (deleteIndex >= 0)
        {
            notes.RemoveAt(deleteIndex);
            configuration.Save();
            plugin.ApplyNotes();
        }
    }
}
