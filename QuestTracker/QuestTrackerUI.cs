using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Lumina.Excel.GeneratedSheets;

namespace QuestTracker
{
    class QuestTrackerUI : IDisposable
    {
        private Plugin plugin;

        private Configuration configuration;

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
            this.plugin = plugin;
            this.configuration = configuration;
        }

        public void Dispose() { }

        public void Draw()
        {
            if (!Visible) return;

            plugin.UpdateQuestData();
            ImGui.SetNextWindowSize(new Vector2(375, 440), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(375, 240), new Vector2(float.MaxValue, float.MaxValue));
            if (ImGui.Begin(plugin.Name, ref visible))
            {
                if (ImGui.BeginTabBar("##tab_bar", ImGuiTabBarFlags.None))
                {
                    if (ImGui.BeginTabItem("Overview##overview_tab"))
                    {
                        DrawOverviewTab();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem($"Quests##quest_tab"))
                    {
                        DrawQuestsTab();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem($"Settings##settings_tab"))
                    {
                        DrawSettingsTab();
                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }
            }

            ImGui.End();
        }

        private void DrawOverviewTab()
        {
            ImGui.BeginChild("##overview_tab", ImGuiHelpers.ScaledVector2(0), true);
            if (ImGui.BeginTable("##overview_table", 3))
            {
                ImGui.TableSetupColumn("##title");
                ImGui.TableSetupColumn("##count", ImGuiTableColumnFlags.None, 0.70f);
                ImGui.TableSetupColumn("##percentage", ImGuiTableColumnFlags.None, 0.30f);

                ImGui.TableNextColumn();
                ImGui.Text("Overall");
                ImGui.Separator();
                ImGui.TableNextColumn();
                ImGui.Text($"{plugin.QuestData.NumComplete}/{plugin.QuestData.Total}");
                ImGui.Separator();
                ImGui.TableNextColumn();
                ImGui.Text($"{plugin.QuestData.NumComplete / plugin.QuestData.Total:P0}%");
                ImGui.Separator();
                ImGui.TableNextRow();

                foreach (var category in plugin.QuestData.Categories)
                {
                    ImGui.TableNextColumn();
                    ImGui.Text(category.Title);
                    ImGui.TableNextColumn();
                    ImGui.Text($"{category.NumComplete}/{category.Total}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{category.NumComplete / category.Total:P0}%");
                    ImGui.TableNextRow();
                }
            }

            ImGui.EndTable();
            ImGui.EndChild();
        }

        private void DrawQuestsTab()
        {
            ImGui.BeginChild("##quests_tab", ImGuiHelpers.ScaledVector2(0), true);
            if (configuration.CategorySelection == null) ResetSelections();

            ImGui.SetNextItemWidth(330);
            if (ImGui.BeginCombo("##category_dropdown", GetDisplayText(configuration.CategorySelection)))
            {
                foreach (var category in plugin.QuestData.Categories)
                {
                    if (!category.Hide)
                    {
                        if (ImGui.Selectable(GetDisplayText(category),
                                             configuration.CategorySelection == category))
                        {
                            configuration.CategorySelection = category;
                            configuration.SubcategorySelection =
                                configuration.CategorySelection.Categories.Find(c => !c.Hide);
                            configuration.Save();
                        }
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.Spacing();
            ImGui.SetNextItemWidth(330);
            if (ImGui.BeginCombo("##subcategory_dropdown", GetDisplayText(configuration.SubcategorySelection)))
            {
                foreach (var category in configuration.CategorySelection.Categories)
                {
                    if (!category.Hide)
                    {
                        if (ImGui.Selectable(GetDisplayText(category),
                                             configuration.SubcategorySelection == category))
                            configuration.SubcategorySelection = category;

                        if (configuration.SubcategorySelection == category) ImGui.SetItemDefaultFocus();
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.Spacing();
            if (configuration.SubcategorySelection.Categories.Count > 0)
            {
                foreach (var subcategory in configuration.SubcategorySelection.Categories)
                {
                    if (!subcategory.Hide)
                    {
                        ImGui.TextDisabled($"{subcategory.Title}");
                        ImGui.Separator();
                        DrawQuestTable(subcategory.Quests);
                    }
                }
            }
            else
            {
                DrawQuestTable(configuration.SubcategorySelection.Quests);
            }

            ImGui.EndChild();
        }

        private void DrawQuestTable(List<Quest> quests)
        {
            if (ImGui.BeginTable("##quest_table", 4))
            {
                ImGui.TableSetupColumn("##check", ImGuiTableColumnFlags.None, 0.10f);
                ImGui.TableSetupColumn("Title");
                ImGui.TableSetupColumn("Area", ImGuiTableColumnFlags.None, 0.70f);
                ImGui.TableSetupColumn("Level", ImGuiTableColumnFlags.None, 0.30f);
                ImGui.TableHeadersRow();
                foreach (var quest in quests)
                {
                    if (!quest.Hide)
                    {
                        ImGui.TableNextColumn();
                        if (Plugin.IsQuestComplete(quest))
                        {
                            ImGui.PushFont(UiBuilder.IconFont);
                            ImGui.TextUnformatted(FontAwesomeIcon.Check.ToIconString());
                            ImGui.PopFont();
                        }

                        ImGui.TableNextColumn();
                        ImGui.Text(quest.Title);
                        ImGui.TableNextColumn();
                        if (ImGui.Selectable($"{quest.Area}##{quest.Id[0]}")) OpenAreaMap(quest);
                        ImGui.TableNextColumn();
                        ImGui.Text($"{quest.Level}");
                        ImGui.TableNextRow();
                    }
                }
            }

            ImGui.EndTable();
        }

        private void DrawSettingsTab()
        {
            ImGui.BeginChild("##settings_tab", ImGuiHelpers.ScaledVector2(0), true);
            ImGui.SetNextItemWidth(130);
            var displayOption = configuration.DisplayOption;
            string[] displayList = { "Show All", "Show Complete", "Show Incomplete" };
            if (ImGui.BeginCombo("##display_option", displayList[displayOption]))
            {
                for (int i = 0; i < displayList.Length; i++)
                {
                    if (ImGui.Selectable(displayList[i]))
                    {
                        configuration.DisplayOption = i;
                        configuration.Save();
                        plugin.UpdateQuestData();
                        ResetSelections();
                    }

                    if (displayOption == i) ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            ImGui.Spacing();

            var showCount = configuration.ShowCount;
            if (ImGui.Checkbox("Show count \"Main Scenario 502/843\"", ref showCount))
            {
                configuration.ShowCount = showCount;
                configuration.Save();
            }

            ImGui.Spacing();

            var showPercentage = configuration.ShowPercentage;
            if (ImGui.Checkbox("Show percentage \"Tribal Quests 54%\"", ref showPercentage))
            {
                configuration.ShowPercentage = showPercentage;
                configuration.Save();
            }

            ImGui.EndChild();
        }

        private void ResetSelections()
        {
            if (configuration.CategorySelection.Hide)
            {
                configuration.CategorySelection = plugin.QuestData.Categories.Find(c => !c.Hide);
                configuration.SubcategorySelection = configuration.CategorySelection.Categories.Find(c => !c.Hide);
            }

            if (configuration.SubcategorySelection.Hide)
            {
                configuration.SubcategorySelection = configuration.CategorySelection.Categories.Find(c => !c.Hide);
            }

            configuration.Save();
        }

        private string GetDisplayText(QuestData questData)
        {
            var text = $"{questData.Title}";
            if (configuration.ShowCount) text += $" {questData.NumComplete}/{questData.Total}";
            if (configuration.ShowPercentage) text += $" {questData.NumComplete / questData.Total:P0}";
            return text;
        }

        private static void OpenAreaMap(Quest quest)
        {
            var questEnumerable = Plugin.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Quest>()
                                        .Where(q => quest.Id.Contains(q.RowId) && q.IssuerLocation.Value != null);
            Level level = questEnumerable.First().IssuerLocation.Value;
            var mapLink = new MapLinkPayload(level.Territory.Row,
                                             level.Map.Row,
                                             (int)(level.X * 1_000f),
                                             (int)(level.Z * 1_000f));
            Plugin.GameGui.OpenMapWithMapLink(mapLink);
        }
    }
}
