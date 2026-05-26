using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FriendNote.Handler;
using FriendNote.Localization;
using FriendNote.Service;
using FriendNote.Windows;
using Lumina.Excel.Sheets;
using Lumina.Extensions;

namespace FriendNote;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService]
    internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService]
    internal static ICommandManager CommandManager { get; private set; } = null!;

    [PluginService]
    internal static IPluginLog Log { get; private set; } = null!;

    [PluginService]
    public static IContextMenu ContextMenu { get; private set; } = null!;

    [PluginService]
    internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;

    [PluginService]
    internal static IDataManager DataManager { get; private set; } = null!;

    private const string CommandName = "/fnote";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("FriendNote");
    private MainWindow MainWindow { get; init; }
    private NoteWindow NoteWindow { get; init; }
    private NoteService NoteService { get; init; }

    public Plugin()
    {
        Loc.SetLanguage(PluginInterface.UiLanguage);

        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        MainWindow = new MainWindow(this);
        NoteWindow = new NoteWindow(this);
        NoteService = new NoteService(AddonLifecycle, Configuration, NoteWindow);

        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(NoteWindow);
        ContextMenuHandler.Start(this);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = Loc.CommandHelp
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;

        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        PluginInterface.LanguageChanged += LanguageChanged;
    }

    public void Dispose()
    {
        // Unregister all actions to not leak anything during disposal of plugin
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        PluginInterface.LanguageChanged -= LanguageChanged;

        WindowSystem.RemoveAllWindows();

        MainWindow.Dispose();
        NoteWindow.Dispose();
        NoteService.Dispose();
        ContextMenuHandler.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        // In response to the slash command, toggle the display status of our main ui
        MainWindow.Toggle();
    }

    private void LanguageChanged(string langCode)
    {
        Loc.SetLanguage(langCode);
    }

    public void ToggleMainUi() => MainWindow.Toggle();
    public void AddNote(ulong targetContentId, string targetName, uint targetHomeWorld)
    {
        NoteService.AddNote(targetContentId, targetName, GetWorldName(targetHomeWorld));
    }

    public void AddNote(ulong targetContentId, string targetName, string targetServerName)
    {
        NoteService.AddNote(targetContentId, targetName, targetServerName);
    }

    public void ApplyNotes() => NoteService.ApplyNote();

    private static string GetWorldName(uint worldId)
    {
        var world = DataManager.GetExcelSheet<World>().GetRowOrDefault(worldId);
        return world?.Name.ExtractText() ?? string.Empty;
    }
}
