using System;
using System.Collections.Generic;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FriendNote.Windows;
using InteropGenerator.Runtime;

namespace FriendNote.Service;

public sealed unsafe class NoteService : IDisposable
{
    private const int MaxFriendListStringFieldsToScan = 1600;
    public const int NoteMaxLength = 60;

    private readonly IAddonLifecycle addonLifecycle;
    private readonly IClientState clientState;
    private readonly Configuration config;
    private readonly NoteWindow noteWindow;
    private readonly Func<uint, string> worldNameResolver;
    private readonly HashSet<ulong> lastAppliedNoteContentIds = new();
    private bool pendingInitialFriendInfoRefresh = true;

    public NoteService(
        IAddonLifecycle addonLifecycle,
        IClientState clientState,
        Configuration config,
        NoteWindow noteWindow,
        Func<uint, string> worldNameResolver)
    {
        this.addonLifecycle = addonLifecycle;
        this.clientState = clientState;
        this.config = config;
        this.noteWindow = noteWindow;
        this.worldNameResolver = worldNameResolver;

        this.addonLifecycle.RegisterListener(
            AddonEvent.PreRequestedUpdate,
            "FriendList",
            this.OnFriendListUpdate
        );
        this.clientState.Login += this.OnLogin;
    }

    public void Dispose()
    {
        this.addonLifecycle.UnregisterListener(this.OnFriendListUpdate);
        this.clientState.Login -= this.OnLogin;
    }

    private void OnLogin()
    {
        pendingInitialFriendInfoRefresh = true;
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
        var atkStage = AtkStage.Instance();
        if (atkStage == null)
            return;

        var stringArray = atkStage->GetStringArrayData(StringArrayType.FriendList);
        if (stringArray == null)
            return;

        ApplyNote(stringArray);
    }

    private void ApplyNote(StringArrayData* stringArray)
    {
        var proxy = InfoProxyFriendList.Instance();
        if (proxy == null)
            return;

        RefreshInitialFriendInfo(proxy);

        var friendDisplays = GetFriendDisplays(proxy);
        if (friendDisplays.Count == 0)
            return;

        var handledFriendIndexes = new bool[friendDisplays.Count];
        var handledCount = 0;
        var scanSize = Math.Min(stringArray->Size, MaxFriendListStringFieldsToScan);
        for (var i = 0; i < scanSize; i++)
        {
            var currentValue = ReadString(stringArray->StringArray[i]);
            if (string.IsNullOrEmpty(currentValue))
                continue;

            var friendIndex = FindFriendDisplayIndex(currentValue, friendDisplays, handledFriendIndexes);
            if (friendIndex < 0)
                continue;

            var friendDisplay = friendDisplays[friendIndex];
            var displayName = FormatDisplayName(currentValue, friendDisplay.RawName, friendDisplay.Note);
            if (!displayName.Equals(currentValue, StringComparison.Ordinal))
                stringArray->SetValue(i, displayName);

            handledFriendIndexes[friendIndex] = true;
            handledCount++;
            if (handledCount == friendDisplays.Count)
                break;
        }

        lastAppliedNoteContentIds.Clear();
        foreach (var friendDisplay in friendDisplays)
        {
            if (!string.IsNullOrEmpty(friendDisplay.Note))
                lastAppliedNoteContentIds.Add(friendDisplay.ContentId);
        }
    }

    private void RefreshInitialFriendInfo(InfoProxyFriendList* proxy)
    {
        if (!pendingInitialFriendInfoRefresh)
            return;

        if (proxy->EntryCount == 0)
            return;

        pendingInitialFriendInfoRefresh = false;

        if (config.FriendNoteList.Count == 0)
            return;

        var notesByContentId = new Dictionary<ulong, List<NoteList>>();
        foreach (var friendNote in config.FriendNoteList)
        {
            if (!notesByContentId.TryGetValue(friendNote.ContentId, out var notes))
            {
                notes = new List<NoteList>();
                notesByContentId.Add(friendNote.ContentId, notes);
            }

            notes.Add(friendNote);
        }

        var hasChanges = false;
        for (uint i = 0; i < proxy->EntryCount; i++)
        {
            var entry = proxy->GetEntry(i);
            if (entry == null || !notesByContentId.TryGetValue(entry->ContentId, out var notes))
                continue;

            var friendName = entry->NameString.ToString();
            if (string.IsNullOrWhiteSpace(friendName))
                continue;

            var serverName = worldNameResolver(entry->HomeWorld);

            foreach (var note in notes)
            {
                if (!note.FriendName.Equals(friendName, StringComparison.Ordinal))
                {
                    note.FriendName = friendName;
                    hasChanges = true;
                }

                if (!note.ServerName.Equals(serverName, StringComparison.Ordinal))
                {
                    note.ServerName = serverName;
                    hasChanges = true;
                }
            }
        }

        if (hasChanges)
            config.Save();
    }

    private List<FriendDisplay> GetFriendDisplays(InfoProxyFriendList* proxy)
    {
        var notesByContentId = new Dictionary<ulong, string>();
        foreach (var friendNote in config.FriendNoteList)
        {
            if (notesByContentId.ContainsKey(friendNote.ContentId))
                continue;

            var note = FormatNote(friendNote.Note, NoteMaxLength);
            if (!string.IsNullOrEmpty(note))
                notesByContentId.Add(friendNote.ContentId, note);
        }

        if (notesByContentId.Count == 0 && lastAppliedNoteContentIds.Count == 0)
            return new List<FriendDisplay>();

        var friendDisplays = new List<FriendDisplay>(notesByContentId.Count + lastAppliedNoteContentIds.Count);

        for (uint i = 0; i < proxy->EntryCount; i++)
        {
            var entry = proxy->GetEntry(i);
            if (entry == null)
                continue;

            if (!notesByContentId.ContainsKey(entry->ContentId) &&
                !lastAppliedNoteContentIds.Contains(entry->ContentId))
                continue;

            var rawName = entry->NameString.ToString();
            if (string.IsNullOrWhiteSpace(rawName))
                continue;

            notesByContentId.TryGetValue(entry->ContentId, out var note);
            friendDisplays.Add(new FriendDisplay(entry->ContentId, rawName, note ?? string.Empty));
        }

        return friendDisplays;
    }

    private static int FindFriendDisplayIndex(
        string currentValue,
        IReadOnlyList<FriendDisplay> friendDisplays,
        bool[] handledIndexes)
    {
        for (var i = 0; i < friendDisplays.Count; i++)
        {
            if (handledIndexes[i])
                continue;

            var rawName = friendDisplays[i].RawName;
            if (currentValue.Length < rawName.Length)
                continue;

            if (currentValue.Equals(rawName, StringComparison.Ordinal) ||
                IsFormattedFriendName(currentValue, rawName) ||
                currentValue.Contains(rawName, StringComparison.Ordinal))
                return i;
        }

        return -1;
    }

    private static string FormatDisplayName(string currentValue, string rawName, string note)
    {
        var formattedName = string.IsNullOrWhiteSpace(note)
                                ? rawName
                                : $"{rawName} ({note})";

        if (currentValue.Equals(rawName, StringComparison.Ordinal) ||
            IsFormattedFriendName(currentValue, rawName))
            return formattedName;

        var nameStart = currentValue.IndexOf(rawName, StringComparison.Ordinal);
        if (nameStart < 0)
            return currentValue;

        var nameEnd = nameStart + rawName.Length;
        var segmentEnd = nameEnd;
        if (currentValue.Length > nameEnd + 2 &&
            currentValue[nameEnd] == ' ' &&
            currentValue[nameEnd + 1] == '(')
        {
            var noteEnd = currentValue.IndexOf(')', nameEnd + 2);
            if (noteEnd >= 0)
                segmentEnd = noteEnd + 1;
        }

        return string.Concat(
            currentValue.AsSpan(0, nameStart),
            formattedName,
            currentValue.AsSpan(segmentEnd));
    }

    private static bool IsFormattedFriendName(string value, string rawName)
    {
        return value.Length > rawName.Length + 3 &&
               value.StartsWith($"{rawName} (", StringComparison.Ordinal) &&
               value.EndsWith(')');
    }

    private static string ReadString(CStringPointer value)
    {
        return value.Value == null ? string.Empty : value.ToString();
    }

    private readonly record struct FriendDisplay(ulong ContentId, string RawName, string Note);

    public static string FormatNote(string? note, int maxLength = int.MaxValue)
    {
        if (string.IsNullOrWhiteSpace(note))
            return string.Empty;

        var formattedNote = string.Join(' ', note.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return formattedNote[..Math.Min(formattedNote.Length, maxLength)];
    }
}
