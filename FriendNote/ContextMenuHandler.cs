using System.Linq;
using Dalamud.Game.Gui.ContextMenu;
using FriendNote.Localization;

namespace FriendNote.Handler;

public class ContextMenuHandler
{
    private const char PrefixChar = 'F';
    private static Plugin? plugin;

    public static void Start(Plugin pluginInstance)
    {
        plugin = pluginInstance;
        Plugin.ContextMenu.OnMenuOpened += OnFriendListOpen;
        Plugin.ContextMenu.OnMenuOpened += OnNameRightClick;
    }

    private static void AddNote(IMenuItemClickedArgs args)
    {
        if (plugin == null || args.Target is not MenuTargetDefault target || target.TargetContentId == 0)
            return;

        plugin.AddNote(target.TargetContentId, target.TargetName, target.TargetHomeWorld.RowId);
    }

    private static void OnFriendListOpen(IMenuOpenedArgs args)
    {

        if (plugin == null || args.AddonName != "FriendList")
            return;

        if (args.Target is not MenuTargetDefault target || target.TargetContentId == 0)
            return;

        var hasNote = plugin.Configuration.FriendNoteList.Any(x =>
                                                                  x.ContentId == target.TargetContentId &&
                                                                  !string.IsNullOrWhiteSpace(x.Note));

        args.AddMenuItem(new MenuItem
        {
            Name = hasNote ? Loc.EditFriendNote : Loc.AddFriendNote,
            PrefixChar = PrefixChar,
            OnClicked = AddNote
        });
    }
    
    private static void OnNameRightClick(IMenuOpenedArgs args)
    {
        if (plugin == null || args.AddonName is not null)
            return;

        if (args.Target is not MenuTargetDefault target || target.TargetContentId == 0)
            return;
        
        var note = plugin.Configuration.FriendNoteList
                         .FirstOrDefault(x => x.ContentId == target.TargetContentId)
                         ?.Note;
        
        if (string.IsNullOrWhiteSpace(note))
            return;

        args.AddMenuItem(new MenuItem
        {
            Name = $"{Loc.NotePrefix}{note}",
            PrefixChar = PrefixChar,
            OnClicked = AddNote
        });
    }

    public static void Dispose()
    {
        Plugin.ContextMenu.OnMenuOpened -= OnFriendListOpen;
        Plugin.ContextMenu.OnMenuOpened -= OnNameRightClick;
        plugin = null;
    }
}
