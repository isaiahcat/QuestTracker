using ImGuiNET;
using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace QuestTracker
{
    
    // It is good to have this be disposable in general, in case you ever need it to do any cleanup
    class QuestTrackerUI : IDisposable
    {
        private Plugin Plugin;
        
        private Configuration configuration;
        
        // this extra bool exists for ImGui, since you can't ref a property
        private bool visible = false;
        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }
        
        public QuestTrackerUI(Plugin plugin, Configuration configuration)
        {
            this.Plugin = plugin;
            this.configuration = configuration;
        }

        public void Dispose()
        {
            
        }

        public void Draw()
        {
            // This is our only draw handler attached to UIBuilder, so it needs to be
            // able to draw any windows we might have open.
            // Each method checks its own visibility/state to ensure it only draws when
            // it actually makes sense.
            // There are other ways to do this, but it is generally best to keep the number of
            // draw delegates as low as possible.
            DrawMainWindow();
            DrawSettingsWindow();
        }

        public void DrawMainWindow()
        {
            if (!Visible)
            {
                return;
            }
            
            ImGui.SetNextWindowSize(new Vector2(375, 330), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330), new Vector2(float.MaxValue, float.MaxValue));
            if (ImGui.Begin("Quest Tracker", ref this.visible))
            {
                if (ImGui.Button("Settings"))
                {
                    SettingsVisible = true;
                }

                ImGui.Text($"Overall {Plugin.QuestData.NumComplete}/{Plugin.QuestData.Total}");
                
                foreach (var category in Plugin.QuestData.Categories)
                {
                    if (ImGui.CollapsingHeader($"{category.Title} {category.NumComplete}/{category.Total}"))
                    {
                        foreach (var subcategory in category.Subcategories)
                        {
                            if (ImGui.CollapsingHeader($"{subcategory.Title} {subcategory.NumComplete}/{subcategory.Quests.Count}"))
                            {
                                foreach (var quest in subcategory.Quests)
                                {
                                    if (QuestManager.IsQuestComplete(quest.Id))
                                    {
                                        ImGui.Text(quest.Title);
                                    }
                                    else
                                    {
                                        ImGui.TextDisabled(quest.Title);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            ImGui.End();
        }

        public void DrawSettingsWindow()
        {
            if (!SettingsVisible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(232, 170), ImGuiCond.Always);
            if (ImGui.Begin("Settings", ref this.settingsVisible,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                // can't ref a property, so use a local copy
                var showComplete = this.configuration.ShowComplete;
                if (ImGui.Checkbox("Show complete", ref showComplete))
                {
                    this.configuration.ShowComplete = showComplete;
                    // can save immediately on change, if you don't want to provide a "Save and Close" button
                    this.configuration.Save();
                }
                // can't ref a property, so use a local copy
                var showIncomplete = this.configuration.ShowIncomplete;
                if (ImGui.Checkbox("Show incomplete", ref showIncomplete))
                {
                    this.configuration.ShowIncomplete = showIncomplete;
                    // can save immediately on change, if you don't want to provide a "Save and Close" button
                    this.configuration.Save();
                }
                // can't ref a property, so use a local copy
                var showFraction = this.configuration.ShowFraction;
                if (ImGui.Checkbox("Show fraction", ref showFraction))
                {
                    this.configuration.ShowFraction = showFraction;
                    // can save immediately on change, if you don't want to provide a "Save and Close" button
                    this.configuration.Save();
                }
                // can't ref a property, so use a local copy
                var showPercentage = this.configuration.ShowPercentage;
                if (ImGui.Checkbox("Show percentage", ref showPercentage))
                {
                    this.configuration.ShowPercentage = showPercentage;
                    // can save immediately on change, if you don't want to provide a "Save and Close" button
                    this.configuration.Save();
                }
            }
            ImGui.End();
        }
    }
}
