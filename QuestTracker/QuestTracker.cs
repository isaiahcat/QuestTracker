using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.IoC;
using System.IO;
using System.Reflection;

namespace QuestTracker
{
    public class QuestTracker : IDalamudPlugin
    {
        public string Name => "Quest Tracker";

        private const string CommandName = "/qt";

        public static DalamudPluginInterface PluginInterface { get; private set; }
        public static CommandManager CommandManager { get; private set; }

        private Configuration Configuration { get; init; }
        private QuestTrackerUI UI { get; init; }

        public QuestTracker(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;

            this.Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(PluginInterface);
            
            this.UI = new QuestTrackerUI(this.Configuration);
            
            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "/qt: Opens the Quest Tracker"
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }

        public void Dispose()
        {
            this.UI.Dispose();

            CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            this.UI.Visible = true;
        }

        private void DrawUI()
        {
            this.UI.Draw();
        }

        private void DrawConfigUI()
        {
            this.UI.SettingsVisible = true;
        }
    }
}
