using System;
using System.IO;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.IoC;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using Newtonsoft.Json;

namespace QuestTracker
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "Quest Tracker";

        private const string CommandName = "/qt";

        public static DalamudPluginInterface PluginInterface { get; private set; }
        public static CommandManager CommandManager { get; private set; }
        
        public static QuestManager QuestManager { get; set; }

        private Configuration Configuration { get; init; }
        private QuestTrackerUI UI { get; init; }
        
        public readonly QuestData QuestData = null!;

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;

            this.Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(PluginInterface);
            
            this.UI = new QuestTrackerUI(this, this.Configuration);
            
            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "/qt: Opens the Quest Tracker"
            });
            
            try
            {
                PluginLog.Debug("Loading Quests");

                var path = Path.Combine(PluginInterface.AssemblyLocation.Directory.Parent.Parent.Parent.FullName, "data.json");
                var jsonString = File.ReadAllText(path);
                QuestData = JsonConvert.DeserializeObject<QuestData>(jsonString);
                UpdateQuestData();
            }
            catch (Exception e)
            {
                PluginLog.Error("Error loading QuestData from data.jason");
                PluginLog.Error(e.Message);
            }

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

        public void UpdateQuestData()
        {
            foreach (var category in QuestData.Categories)
            {
                foreach (var subcategory in category.Subcategories)
                {
                    foreach (var quest in subcategory.Quests)
                    {
                        if (QuestManager.IsQuestComplete(quest.Id))
                        {
                            subcategory.NumComplete++;
                        }
                    }

                    category.NumComplete += subcategory.NumComplete;
                    category.Total += subcategory.Quests.Count;
                }

                QuestData.NumComplete += category.NumComplete;
                QuestData.Total += category.Total;
            }
        }
    }
}
