using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace QuestTracker
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public bool ShowOverall { get; set; } = true;
        public bool ShowCount { get; set; } = true;
        public bool ShowPercentage { get; set; } = false;
        public bool ShowSidePanel { get; set; } = true;
        public int LayoutOption { get; set; } = 0;
        public int DisplayOption { get; set; } = 0;
        public string StartArea { get; set; } = "";
        public string GrandCompany { get; set; } = "";
        public uint StartClass { get; set; } = 0;
        public QuestData SidePanelSelection { get; set; } = null;
        public QuestData DropdownSelection { get; set; } = null;

        // the below exist just to make saving less cumbersome

        [NonSerialized]
        private DalamudPluginInterface pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface.SavePluginConfig(this);
        }

        public void ResetFilters()
        {
            StartArea = "";
            GrandCompany = "";
            StartClass = 0;
            Save();
        }

        public void ResetSelections()
        {
            SidePanelSelection = null;
            DropdownSelection = null;
            Save();
        }
    }
}
