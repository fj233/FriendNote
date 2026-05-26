namespace FriendNote.Localization;

public static class Loc
{
    private enum PluginLanguage
    {
        English,
        Chinese,
        Japanese,
    }

    private static PluginLanguage Language { get; set; } = GetLanguage(Plugin.PluginInterface.UiLanguage);

    public static void SetLanguage(string langCode)
    {
        Language = GetLanguage(langCode);
    }

    private static PluginLanguage GetLanguage(string langCode)
    {
        var normalized = langCode.ToLowerInvariant();

        if (normalized.StartsWith("zh"))
            return PluginLanguage.Chinese;

        if (normalized.StartsWith("ja"))
            return PluginLanguage.Japanese;

        return PluginLanguage.English;
    }

    public static string CommandHelp => Language switch
    {
        PluginLanguage.Japanese => "フレンドメモ一覧を開く",
        PluginLanguage.Chinese => "打开备注列表",
        _ => "Open friend note list",
    };

    public static string AddFriendNote => Language switch
    {
        PluginLanguage.Japanese => "フレンドメモを追加",
        PluginLanguage.Chinese => "添加好友备注",
        _ => "Add friend note",
    };

    public static string EditFriendNote => Language switch
    {
        PluginLanguage.Japanese => "フレンドメモを編集",
        PluginLanguage.Chinese => "修改好友备注",
        _ => "Edit friend note",
    };

    public static string NotePrefix => Language switch
    {
        PluginLanguage.Japanese => "メモ: ",
        PluginLanguage.Chinese => "备注：",
        _ => "Note: ",
    };

    public static string NoteListTitle => Language switch
    {
        PluginLanguage.Japanese => "メモ一覧",
        PluginLanguage.Chinese => "备注列表",
        _ => "Note List",
    };

    public static string AddNoteTitle => Language switch
    {
        PluginLanguage.Japanese => "メモを追加",
        PluginLanguage.Chinese => "添加备注",
        _ => "Add Note",
    };

    public static string EditNoteTitle => Language switch
    {
        PluginLanguage.Japanese => "ノートを編集",
        PluginLanguage.Chinese => "修改备注",
        _ => "Edit Note",
    };

    public static string NoteLabel => Language switch
    {
        PluginLanguage.Japanese => "メモ",
        PluginLanguage.Chinese => "备注",
        _ => "Note",
    };

    public static string NoSavedNotes => Language switch
    {
        PluginLanguage.Japanese => "保存済みのメモはありません",
        PluginLanguage.Chinese => "暂无已保存备注",
        _ => "No saved notes",
    };

    public static string FriendNameColumn => Language switch
    {
        PluginLanguage.Japanese => "フレンド名",
        PluginLanguage.Chinese => "好友名称",
        _ => "Friend Name",
    };

    public static string NoteColumn => Language switch
    {
        PluginLanguage.Japanese => "メモ",
        PluginLanguage.Chinese => "备注",
        _ => "Note",
    };

    public static string ActionColumn => Language switch
    {
        PluginLanguage.Japanese => "操作",
        PluginLanguage.Chinese => "操作",
        _ => "Action",
    };

    public static string UnknownFriend => Language switch
    {
        PluginLanguage.Japanese => "(不明なフレンド)",
        PluginLanguage.Chinese => "(未知好友)",
        _ => "(Unknown friend)",
    };

    public static string EmptyNote => Language switch
    {
        PluginLanguage.Japanese => "(空のメモ)",
        PluginLanguage.Chinese => "(空备注)",
        _ => "(Empty note)",
    };

    public static string Edit => Language switch
    {
        PluginLanguage.Japanese => "編集",
        PluginLanguage.Chinese => "修改",
        _ => "Edit",
    };

    public static string Delete => Language switch
    {
        PluginLanguage.Japanese => "削除",
        PluginLanguage.Chinese => "删除",
        _ => "Delete",
    };

    public static string Save => Language switch
    {
        PluginLanguage.Japanese => "保存",
        PluginLanguage.Chinese => "保存",
        _ => "Save",
    };
}
