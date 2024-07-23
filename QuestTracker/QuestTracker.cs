using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace QuestTracker
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Quest Tracker";

        private const string CommandName = "/qt";
        
        private const string CommandNameAlt = "/quest";

        public static IDalamudPluginInterface PluginInterface { get; private set; }
        public static ICommandManager CommandManager { get; private set; }
        public static IDataManager DataManager { get; private set; }
        public static IGameGui GameGui { get; private set; }
        public static IPluginLog PluginLog { get; private set; }
        private Configuration Configuration { get; init; }
        private QuestDataManager QuestDataManager { get; init; }
        private MainWindow MainWindow { get; init; }

        public QuestData QuestData = null!;

        public Plugin(
            IDalamudPluginInterface pluginInterface,
            ICommandManager commandManager,
            IDataManager dataManager,
            IGameGui gameGui,
            IPluginLog pluginLog)
        {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;
            DataManager = dataManager;
            GameGui = gameGui;
            PluginLog = pluginLog;

            //DataConverter dc = new DataConverter(pluginInterface, dataManager, pluginLog);

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            QuestDataManager = new QuestDataManager(pluginInterface, pluginLog, this, Configuration);
            MainWindow = new MainWindow(this, QuestDataManager,Configuration);

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the Quest Tracker"
            });
            
            CommandManager.AddHandler(CommandNameAlt, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the Quest Tracker"
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            PluginInterface.UiBuilder.OpenMainUi += DrawMainUI;
        }

        public void Dispose()
        {
            MainWindow.Dispose();
            Configuration.Reset();
            CommandManager.RemoveHandler(CommandName);
            CommandManager.RemoveHandler(CommandNameAlt);
        }

        private void OnCommand(string command, string args) => DrawMainUI();

        private void DrawUI()
        {
            MainWindow.Draw();
        }

        private void DrawConfigUI()
        {
            MainWindow.Visible = true;
            MainWindow.SettingsVisible = true;
        }

        private void DrawMainUI()
        {
            MainWindow.Visible = true;
            MainWindow.SettingsVisible = false;
        }
    }
}
