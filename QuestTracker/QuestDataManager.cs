using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using Newtonsoft.Json;

namespace QuestTracker
{
    class QuestDataManager
    {
        public IPluginLog pluginLog { get; private set; }

        private readonly Plugin plugin;
        
        private readonly Configuration configuration;

        public QuestDataManager(
            IDalamudPluginInterface pluginInterface, 
            IPluginLog pluginLog,
            Plugin plugin, 
            Configuration configuration)
        {
            this.pluginLog = pluginLog;
            this.plugin = plugin;
            this.configuration = configuration;
            
            try
            {
                pluginLog.Debug("Loading QuestData from data.json");
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("QuestTracker.data.json");
                using var stringStream = new StreamReader(stream);
                var jsonString = stringStream.ReadToEnd();
                plugin.QuestData = JsonConvert.DeserializeObject<QuestData>(jsonString);

                pluginLog.Debug("Load successful");
            }
            catch (Exception e)
            {
                pluginLog.Error("Error loading QuestData from data.jason");
                pluginLog.Error(e.Message);
            }
        }
        
        private void DetermineStartArea()
        {
            configuration.StartArea = QuestManager.IsQuestComplete(65575) ? "Gridania" :
                                      QuestManager.IsQuestComplete(65643) ? "Limsa Lominsa" :
                                      QuestManager.IsQuestComplete(66130) ? "Ul'dah" : "";
            
            pluginLog.Debug($"Start Area {configuration.StartArea}");
        }

        private void DetermineGrandCompany()
        {
            configuration.GrandCompany = QuestManager.IsQuestComplete(66216) ? "Twin Adder" :
                                         QuestManager.IsQuestComplete(66217) ? "Maelstrom" :
                                         QuestManager.IsQuestComplete(66218) ? "Immortal Flames" : "";
            
            pluginLog.Debug($"Grand Company {configuration.GrandCompany}");
        }

        private void DetermineStartClass()
        {
            configuration.StartClass = (uint) (
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
                QuestManager.IsQuestComplete(65991) && !QuestManager.IsQuestComplete(65990) ? 65990 : 0);
            
            pluginLog.Debug($"Start Class {configuration.StartClass}");
        }

        public void UpdateQuestData()
        {
            UpdateQuestData(plugin.QuestData);
        }
        
        private void UpdateQuestData(QuestData questData)
        {
            questData.NumComplete = questData.Total = 0;
            if (configuration.StartArea == "") DetermineStartArea();
            if (configuration.GrandCompany == "") DetermineGrandCompany();
            if (configuration.StartClass == 0) DetermineStartClass();

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
                    if (!configuration.StartArea.IsNullOrEmpty() && !quest.Start.IsNullOrEmpty() && configuration.StartArea != quest.Start)
                    {
                        if (IsQuestComplete(quest))
                        {
                            pluginLog.Error($"Quest {quest.Title} {string.Join(" ", quest.Id)} is restricted but completed");
                        }

                        questData.Quests.Remove(quest);
                        continue;
                    }

                    if (!configuration.GrandCompany.IsNullOrEmpty() && !quest.Gc.IsNullOrEmpty() && configuration.GrandCompany != quest.Gc)
                    {
                        if (IsQuestComplete(quest))
                        {
                            pluginLog.Error($"Quest {quest.Title} {string.Join(" ", quest.Id)} is restricted but completed");
                        }

                        questData.Quests.Remove(quest);
                        continue;
                    }

                    if (configuration.StartClass != 0 && quest.Id.Contains(configuration.StartClass))
                    {
                        questData.Quests.Remove(quest);
                        continue;
                    }
                    
                    // ARR "Call of the Wild" Tribal Alliance Quests
                    if ((QuestManager.IsQuestComplete(67001) && (quest.Id.Contains(67002) || quest.Id.Contains(67003))) ||
                        (QuestManager.IsQuestComplete(67002) && (quest.Id.Contains(67001) || quest.Id.Contains(67003))) ||
                        (QuestManager.IsQuestComplete(67003) && (quest.Id.Contains(67001) || quest.Id.Contains(67002))) ||
                    // YorHa "Heads or Tails"
                        (QuestManager.IsQuestComplete(69256) && quest.Id.Contains(69257)) || 
                        (QuestManager.IsQuestComplete(69257) && quest.Id.Contains(69256)) || 
                    // Qitari "The First Stela"
                        (QuestManager.IsQuestComplete(69336) && quest.Id.Contains(69337)) || 
                        (QuestManager.IsQuestComplete(69337) && quest.Id.Contains(69336)) || 
                    // Qitari "The Second Stela"
                        (QuestManager.IsQuestComplete(69338) && quest.Id.Contains(69339)) || 
                        (QuestManager.IsQuestComplete(69339) && quest.Id.Contains(69338)) || 
                    // Qitari "The Third Stela"
                        (QuestManager.IsQuestComplete(69340) && quest.Id.Contains(69341)) || 
                        (QuestManager.IsQuestComplete(69341) && quest.Id.Contains(69340))) 
                    {
                        questData.Quests.Remove(quest);
                    }

                    if (IsQuestComplete(quest)) questData.NumComplete++;

                    quest.Hide = (configuration.DisplayOption == 1 && !IsQuestComplete(quest)) ||
                                 (configuration.DisplayOption == 2 && IsQuestComplete(quest));
                    if (!quest.Hide) questData.Hide = false;
                }

                questData.Total += questData.Quests.Count;
            }
        }

        public static bool IsQuestComplete(Quest quest)
        {
            foreach (var id in quest.Id)
            {
                if (QuestManager.IsQuestComplete(id)) return true;   
            }
            return false;
        }
    }
}
