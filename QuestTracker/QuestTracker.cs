using System;
using System.IO;
using System.Linq;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Newtonsoft.Json;

namespace QuestTracker
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "Quest Tracker";

        private const string CommandName = "/qt";

        public static DalamudPluginInterface PluginInterface { get; private set; }
        public static ICommandManager CommandManager { get; private set; }

        private Configuration Configuration { get; init; }
        private QuestTrackerUI UI { get; init; }

        public readonly QuestData QuestData = null!;

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ICommandManager commandManager)
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

            DataConverter dc = new DataConverter(pluginInterface);

            try
            {
                PluginLog.Debug("Loading QuestData from data.json");

                var path = Path.Combine(PluginInterface.AssemblyLocation.Directory.Parent.Parent.FullName, "data.json");
                var jsonString = File.ReadAllText(path);
                QuestData = JsonConvert.DeserializeObject<QuestData>(jsonString);
                var start = DetermineStartArea();
                var gc = DetermineGrandCompany();
                UpdateQuestData(QuestData, start, gc);

                PluginLog.Debug("Load successful");
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
            this.UI.SettingsVisible = false;
            this.UI.CurrentCategory = null;
        }

        private void DrawUI()
        {
            this.UI.Draw();
        }

        private void DrawConfigUI()
        {
            this.UI.Visible = true;
            this.UI.SettingsVisible = true;
            this.UI.CurrentCategory = null;
        }

        public string DetermineStartArea()
        {
            return QuestManager.IsQuestComplete(66104) ? "Gridania" :
                   QuestManager.IsQuestComplete(66105) ? "Limsa Lominsa" :
                   QuestManager.IsQuestComplete(66106) ? "Ul'dah" : "";
        }

        public string DetermineGrandCompany()
        {
            return QuestManager.IsQuestComplete(66216) ? "Twin Adder" :
                   QuestManager.IsQuestComplete(66217) ? "Maelstrom" :
                   QuestManager.IsQuestComplete(66218) ? "Immortal Flames" : "";
        }

        public void UpdateQuestData(QuestData questData, string start, string gc)
        {
            if (questData.Categories.Count > 0)
            {
                foreach (var category in questData.Categories)
                {
                    UpdateQuestData(category, start, gc);
                    questData.NumComplete += category.NumComplete;
                    questData.Total += category.Total;
                }
            }
            else
            {
                foreach (var quest in questData.Quests.ToList())
                {
                    if (start != "" && quest.Start != "" && start != quest.Start)
                    {
                        if (QuestManager.IsQuestComplete(quest.Id))
                        {
                            PluginLog.Error($"Quest {quest.Id} is restricted but completed");
                        }

                        questData.Quests.Remove(quest);
                    }

                    if (gc != "" && quest.Gc != "" && gc != quest.Gc)
                    {
                        if (QuestManager.IsQuestComplete(quest.Id))
                        {
                            PluginLog.Error($"Quest {quest.Id} is restricted but completed");
                        }

                        questData.Quests.Remove(quest);
                    }

                    if (QuestManager.IsQuestComplete(quest.Id))
                    {
                        questData.NumComplete++;
                    }
                }

                questData.Total += questData.Quests.Count;
            }
        }
    }
}
