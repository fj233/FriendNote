using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace FriendNote;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    
    public List<NoteList> FriendNoteList { get; set; } = new();

    // The below exists just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}

[Serializable]
public class NoteList
{
    public ulong ContentId { get; set; }
    public string Note { get; set; } = string.Empty;
    public string FriendName { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
}
