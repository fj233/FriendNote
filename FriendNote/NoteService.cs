using System;
using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FriendNote.Windows;

namespace FriendNote.Service;

public sealed unsafe class NoteService : IDisposable
{
    private const int FriendListStringArrayFieldCount = 5;
    private const int FriendNameFieldOffset = 0;
    public const int NoteMaxLength = 60;

    private readonly IAddonLifecycle addonLifecycle;
    private readonly Configuration config;
    private readonly NoteWindow noteWindow;

    public NoteService(IAddonLifecycle addonLifecycle, Configuration config, NoteWindow noteWindow)
    {
        this.addonLifecycle = addonLifecycle;
        this.config = config;
        this.noteWindow = noteWindow;

        this.addonLifecycle.RegisterListener(
            AddonEvent.PreRequestedUpdate,
            "FriendList",
            this.OnFriendListUpdate
        );

        this.addonLifecycle.RegisterListener(
            AddonEvent.PostRefresh,
            "FriendList",
            this.OnFriendListUpdate
        );
    }

    public void Dispose()
    {
        this.addonLifecycle.UnregisterListener(this.OnFriendListUpdate);
    }

    private void OnFriendListUpdate(AddonEvent type, AddonArgs args)
    {
        ApplyNote();
    }

    public void AddNote(ulong targetContentId, string targetName, string targetServerName)
    {
        noteWindow.Open(targetContentId, targetName, targetServerName);
    }

    public void ApplyNote()
    {
        var proxy = InfoProxyFriendList.Instance();
        if (proxy == null)
            return;

        var atkStage = AtkStage.Instance();
        if (atkStage == null)
            return;

        var stringArray = atkStage->GetStringArrayData(StringArrayType.FriendList);
        if (stringArray == null)
            return;

        var notesByContentId = config.FriendNoteList
                                     .GroupBy(x => x.ContentId)
                                     .ToDictionary(x => x.Key, x => x.First());

        for (uint i = 0; i < proxy->EntryCount; i++)
        {
            var entry = proxy->GetEntry(i);
            if (entry == null)
                continue;

            notesByContentId.TryGetValue(entry->ContentId, out var friendNote);

            var note = FormatNote(friendNote?.Note, NoteMaxLength);
            var displayName = string.IsNullOrWhiteSpace(note)
                                  ? entry->NameString.ToString()
                                  : $"{entry->NameString} ({note})";

            var nameFieldIndex = ((int)i * FriendListStringArrayFieldCount) + FriendNameFieldOffset;
            stringArray->SetValue(nameFieldIndex, displayName);
        }
    }

    public static string FormatNote(string? note, int maxLength = int.MaxValue)
    {
        if (string.IsNullOrWhiteSpace(note))
            return string.Empty;

        var formattedNote = string.Join(' ', note.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return formattedNote[..Math.Min(formattedNote.Length, maxLength)];
    }
}
