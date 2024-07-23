using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace QuestTracker
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public bool ShowCount { get; set; } = true;
        public bool ShowPercentage { get; set; }
        public bool ExcludeOtherQuests { get; set; }
        public int DisplayOption { get; set; } 
        public string StartArea { get; set; } = "";
        public string GrandCompany { get; set; } = "";
        public uint StartClass { get; set; }
        public QuestData CategorySelection { get; set; }
        public QuestData SubcategorySelection { get; set; }

        [NonSerialized]
        private IDalamudPluginInterface pluginInterface;

        public void Initialize(IDalamudPluginInterface pluginInterface) => this.pluginInterface = pluginInterface;

        public void Save() => this.pluginInterface.SavePluginConfig(this);

        public void Reset()
        {
            StartArea = "";
            GrandCompany = "";
            StartClass = 0;
            CategorySelection = null;
            SubcategorySelection = null;
            Save();
        }
    }
}
