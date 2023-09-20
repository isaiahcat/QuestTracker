using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace QuestTracker
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public bool ShowComplete { get; set; } = true;
        public bool ShowIncomplete { get; set; } = true;
        public bool ShowFraction { get; set; } = true;
        public bool ShowPercentage { get; set; } = false;

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
    }
}
