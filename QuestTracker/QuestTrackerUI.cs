using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;

namespace QuestTracker
{
    // It is good to have this be disposable in general, in case you ever need it to do any cleanup
    class QuestTrackerUI : IDisposable
    {
        private Plugin plugin;

        private Configuration configuration;

        private Vector2 iconButtonSize = new(16);

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
        
        public QuestData CurrentCategory;
        
        public QuestTrackerUI(Plugin plugin, Configuration configuration)
        {
            this.plugin = plugin;
            this.configuration = configuration;
        }

        public void Dispose() { }

        public void Draw()
        {
            // This is our only draw handler attached to UIBuilder, so it needs to be
            // able to draw any windows we might have open.
            // Each method checks its own visibility/state to ensure it only draws when
            // it actually makes sense.
            // There are other ways to do this, but it is generally best to keep the number of
            // draw delegates as low as possible.
            DrawMainWindow();
        }

        public void DrawMainWindow()
        {
            if (!Visible)
            {
                return;
            }

            var width = 170;
            if (configuration.ShowCount)
            {
                width += 30;
            }
            if (configuration.ShowPercentage)
            {
                width += 20;
            }
            ImGui.SetNextWindowSize(new Vector2(375, 330), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330), new Vector2(float.MaxValue, float.MaxValue));
            if (ImGui.Begin("Quest Tracker", ref this.visible))
            {
                ImGui.BeginGroup();
                if (ImGui.BeginChild("category_select",
                                     ImGuiHelpers.ScaledVector2(width, 0) - iconButtonSize with { X = 0 }, true))
                {
                    if (configuration.ShowOverall)
                    {
                        var text = "Overall";
                        if (configuration.ShowCount)
                        {
                            text += $" {plugin.QuestData.NumComplete}/{plugin.QuestData.Total}";
                        }
                        if (configuration.ShowPercentage)
                        {
                            text += $" {plugin.QuestData.NumComplete / plugin.QuestData.Total:P0}%";
                        }
                        ImGui.Text(text);
                        ImGui.Spacing();
                        ImGui.Separator();
                        ImGui.Spacing();
                    }
                    DrawSidePanel();
                }
                ImGui.EndChild();
                if (ImGui.Button("Settings"))
                {
                    CurrentCategory = null;
                    SettingsVisible = true;
                }

                iconButtonSize = ImGui.GetItemRectSize() + ImGui.GetStyle().ItemSpacing;
                ImGui.EndGroup();

                ImGui.SameLine();
                if (ImGui.BeginChild("category_view", ImGuiHelpers.ScaledVector2(0), true))
                {
                    if (CurrentCategory == null && SettingsVisible)
                    {
                        DrawSettings();
                    }
                    else
                    {
                        DrawDropdown();
                    }
                }

                ImGui.EndChild();
            }

            ImGui.End();
        }

        public void DrawDropdown()
        {
            foreach (var category in CurrentCategory.Categories)
            {
                var text = $"{category.Title}";
                if (configuration.ShowCount)
                {
                    text += $" {category.NumComplete}/{category.Total}";
                }
                if (configuration.ShowPercentage)
                {
                    text += $" {category.NumComplete/category.Total:P0}";
                }
                if (ImGui.CollapsingHeader(text))
                {
                    if (category.Categories.Count > 0)
                    {
                        foreach (var subcategory in category.Categories)
                        {
                            ImGui.TextDisabled($"{subcategory.Title}");
                            ImGui.Separator();
                            DrawQuestTable(subcategory.Quests);
                        }
                    }
                    else
                    {
                        DrawQuestTable(category.Quests);
                    }
                }
            }
        }

        public void DrawQuestTable(List<Quest> quests)
        {
            if (ImGui.BeginTable("quest_table", 4))
            {
                ImGui.TableSetupColumn("##icon", ImGuiTableColumnFlags.None, 0.10f);
                ImGui.TableSetupColumn("Title");
                ImGui.TableSetupColumn("Area", ImGuiTableColumnFlags.None, 0.90f);
                ImGui.TableSetupColumn("Level", ImGuiTableColumnFlags.None, 0.20f);
                ImGui.TableHeadersRow();
                foreach (var quest in quests)
                {
                    var hide = (configuration.HideComplete && QuestManager.IsQuestComplete(quest.Id)) ||
                               (configuration.HideIncomplete && !QuestManager.IsQuestComplete(quest.Id));
                    if (!hide)
                    {
                        ImGui.TableNextColumn();
                        if (QuestManager.IsQuestComplete(quest.Id))
                        {
                            ImGui.PushFont(UiBuilder.IconFont);
                            ImGui.TextUnformatted(FontAwesomeIcon.Check.ToIconString());
                            ImGui.PopFont();
                        }

                        if (quest.Id != 0)
                        {
                            ImGui.TableNextColumn();
                            ImGui.Text(quest.Title);
                            ImGui.TableNextColumn();
                            ImGui.Text(quest.Area);
                            ImGui.TableNextColumn();
                            ImGui.Text($"{quest.Level}");
                        }
                        else
                        {
                            ImGui.TableNextColumn();
                            ImGui.TextDisabled(quest.Title);
                            ImGui.TableNextColumn();
                            ImGui.TextDisabled(quest.Area);
                            ImGui.TableNextColumn();
                            ImGui.TextDisabled($"{quest.Level}");
                        }

                        ImGui.TableNextRow();   
                    }
                }
            }

            ImGui.EndTable();
        }

        public void DrawSidePanel()
        {
            foreach (var category in plugin.QuestData.Categories)
            {
                var text = $"{category.Title}";
                if (configuration.ShowCount)
                {
                    text += $" {category.NumComplete}/{category.Total}";
                }
                if (configuration.ShowPercentage)
                {
                    text += $" {category.NumComplete/category.Total:P0}";
                }
                if (ImGui.Selectable(text, CurrentCategory == category))
                {
                    CurrentCategory = category;
                    SettingsVisible = false;
                }
            }
        }

        public void DrawSettings()
        {
            // can't ref a property, so use a local copy
            var hideComplete = this.configuration.HideComplete;
            if (ImGui.Checkbox("Hide complete", ref hideComplete))
            {
                this.configuration.HideComplete = hideComplete;
                // can save immediately on change, if you don't want to provide a "Save and Close" button
                this.configuration.Save();
            }
            ImGui.Spacing();

            // can't ref a property, so use a local copy
            var hideIncomplete = this.configuration.HideIncomplete;
            if (ImGui.Checkbox("Hide incomplete", ref hideIncomplete))
            {
                this.configuration.HideIncomplete = hideIncomplete;
                // can save immediately on change, if you don't want to provide a "Save and Close" button
                this.configuration.Save();
            }
            ImGui.Spacing();

            // can't ref a property, so use a local copy
            var showCount = this.configuration.ShowCount;
            if (ImGui.Checkbox("Show count \"Main Scenario 502/843\"", ref showCount))
            {
                this.configuration.ShowCount = showCount;
                // can save immediately on change, if you don't want to provide a "Save and Close" button
                this.configuration.Save();
            }
            ImGui.Spacing();

            // can't ref a property, so use a local copy
            var showPercentage = this.configuration.ShowPercentage;
            if (ImGui.Checkbox("Show percentage \"Tribal Quests 54%\"", ref showPercentage))
            {
                this.configuration.ShowPercentage = showPercentage;
                // can save immediately on change, if you don't want to provide a "Save and Close" button
                this.configuration.Save();
            }
            ImGui.Spacing();
            
            // can't ref a property, so use a local copy
            var showOverall = this.configuration.ShowOverall;
            if (ImGui.Checkbox("Show overall", ref showOverall))
            {
                this.configuration.ShowOverall = showOverall;
                // can save immediately on change, if you don't want to provide a "Save and Close" button
                this.configuration.Save();
            }
            ImGui.Spacing();
            
            // can't ref a property, so use a local copy
            var excludeOther = this.configuration.ExcludeOther;
            if (ImGui.Checkbox("Exclude Other Quests from overall", ref excludeOther))
            {
                this.configuration.ExcludeOther = excludeOther;
                // can save immediately on change, if you don't want to provide a "Save and Close" button
                this.configuration.Save();
            }
            ImGui.Spacing();

            var layoutOption = this.configuration.LayoutOption;
            string[] layoutList = {"Option 1", "Option 2", "Option 3"};
            ImGui.Combo("Layout", ref layoutOption, layoutList, layoutList.Length);
        }
    }
}
