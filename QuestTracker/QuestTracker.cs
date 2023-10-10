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

            try
            {
                PluginLog.Debug("Loading QuestData from data.json");

                var path = Path.Combine(PluginInterface.AssemblyLocation.Directory.Parent.Parent.FullName, "data.json");
                var jsonString = File.ReadAllText(path);
                QuestData = JsonConvert.DeserializeObject<QuestData>(jsonString);
                UpdateQuestData(QuestData);

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
            UpdateQuestData(QuestData);
            this.UI.Visible = true;
            this.UI.SettingsVisible = false;
            this.UI.Reset();
        }

        private void DrawUI()
        {
            this.UI.Draw();
        }

        private void DrawConfigUI()
        {
            this.UI.Visible = true;
            this.UI.SettingsVisible = true;
            this.UI.Reset();
        }

        private void DetermineStartArea()
        {
            Configuration.StartArea = QuestManager.IsQuestComplete(66104) ? "Gridania" :
                                      QuestManager.IsQuestComplete(66105) ? "Limsa Lominsa" :
                                      QuestManager.IsQuestComplete(66106) ? "Ul'dah" : "";
        }

        private void DetermineGrandCompany()
        {
            Configuration.GrandCompany = QuestManager.IsQuestComplete(66216) ? "Twin Adder" :
                                         QuestManager.IsQuestComplete(66217) ? "Maelstrom" :
                                         QuestManager.IsQuestComplete(66218) ? "Immortal Flames" : "";
        }

        private void DetermineStartClass()
        {
            Configuration.StartClass =
                // Gladiator
                QuestManager.IsQuestComplete(65792) && !QuestManager.IsQuestComplete(65822) ? 65822 :
                // Pugilist
                QuestManager.IsQuestComplete(66090) && !QuestManager.IsQuestComplete(66089) ? 66089 :
                // Marauder
                QuestManager.IsQuestComplete(65849) && !QuestManager.IsQuestComplete(65848) ? 65848 :
                // Lancer
                QuestManager.IsQuestComplete(65583) && !QuestManager.IsQuestComplete(65754) ? 65754 :
                // Archer
                QuestManager.IsQuestComplete(65582) && !QuestManager.IsQuestComplete(65755) ? 65755 :
                // Rogue
                QuestManager.IsQuestComplete(65640) && !QuestManager.IsQuestComplete(65638) ? 65638 :
                // Conjurer
                QuestManager.IsQuestComplete(65584) && !QuestManager.IsQuestComplete(65747) ? 65747 :
                // Thaumaturge
                QuestManager.IsQuestComplete(65883) && !QuestManager.IsQuestComplete(65882) ? 65882 :
                // Arcanist
                QuestManager.IsQuestComplete(65991) && !QuestManager.IsQuestComplete(65990) ? 65990 : 0;
        }

        public void UpdateQuestData(QuestData questData)
        {
            questData.NumComplete = questData.Total = 0;
            if (Configuration.StartArea == "") DetermineStartArea();
            if (Configuration.GrandCompany == "") DetermineGrandCompany();
            if (Configuration.StartClass == 0) DetermineStartClass();

            if (questData.Categories.Count > 0)
            {
                questData.Hide = true;
                foreach (var category in questData.Categories)
                {
                    UpdateQuestData(category);
                    questData.NumComplete += category.NumComplete;
                    questData.Total += category.Total;
                    if (!category.Hide) questData.Hide = false;
                }
            }
            else
            {
                questData.Hide = true;
                foreach (var quest in questData.Quests.ToList())
                {
                    if (Configuration.StartArea != "" && quest.Start != "" && Configuration.StartArea != quest.Start)
                    {
                        if (QuestManager.IsQuestComplete(quest.Id))
                        {
                            PluginLog.Error($"Quest {quest.Id} is restricted but completed");
                        }

                        questData.Quests.Remove(quest);
                        continue;
                    }

                    if (Configuration.GrandCompany != "" && quest.Gc != "" && Configuration.GrandCompany != quest.Gc)
                    {
                        if (QuestManager.IsQuestComplete(quest.Id))
                        {
                            PluginLog.Error($"Quest {quest.Id} is restricted but completed");
                        }

                        questData.Quests.Remove(quest);
                        continue;
                    }

                    if (Configuration.StartClass != 0 && Configuration.StartClass == quest.Id)
                    {
                        questData.Quests.Remove(quest);
                        continue;
                    }

                    if (QuestManager.IsQuestComplete(quest.Id))
                    {
                        questData.NumComplete++;
                    }

                    quest.Hide = (Configuration.DisplayOption == 1 && !QuestManager.IsQuestComplete(quest.Id)) ||
                                 (Configuration.DisplayOption == 2 && QuestManager.IsQuestComplete(quest.Id));
                    if (!quest.Hide) questData.Hide = false;
                }

                questData.Total += questData.Quests.Count;
            }
        }
    }
}
