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
        public bool ShowPercentage { get; set; } = false;
        public int DisplayOption { get; set; } = 0;

        public string StartArea { get; set; } = "";
        public string GrandCompany { get; set; } = "";
        public uint StartClass { get; set; } = 0;
        public QuestData CategorySelection { get; set; } = null;
        public QuestData SubcategorySelection { get; set; } = null;

        [NonSerialized]
        private DalamudPluginInterface pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface) => this.pluginInterface = pluginInterface;

        public void Save() => this.pluginInterface.SavePluginConfig(this);

        public void ResetFilters()
        {
            StartArea = "";
            GrandCompany = "";
            StartClass = 0;
            Save();
        }
    }
}
