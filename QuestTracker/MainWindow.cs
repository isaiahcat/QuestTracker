using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;

namespace QuestTracker
{
    class MainWindow : Window, IDisposable
    {
        private Plugin plugin;

        private QuestDataManager questDataManager;

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

        private string searchText = "";

        public MainWindow(Plugin plugin, QuestDataManager questDataManager, Configuration configuration)
            : base("Quest Tracker##main_window", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.plugin = plugin;
            this.questDataManager = questDataManager;
            this.configuration = configuration;
        }

        public void Dispose() { }

        public override void Draw()
        {
            if (!Visible) return;

            questDataManager.UpdateQuestData();
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

                var otherQuestsComplete = plugin.QuestData.Categories.Last().NumComplete;
                var otherQuestsTotal = plugin.QuestData.Categories.Last().Total;

                var overallComplete = configuration.ExcludeOtherQuests
                                          ? plugin.QuestData.NumComplete - otherQuestsComplete
                                          : plugin.QuestData.NumComplete;

                var overallTotal = configuration.ExcludeOtherQuests
                                       ? plugin.QuestData.Total - otherQuestsTotal
                                       : plugin.QuestData.Total;

                ImGui.TableNextColumn();
                ImGui.Text("Overall");
                ImGui.Separator();
                ImGui.TableNextColumn();
                ImGui.Text($"{overallComplete}/{overallTotal}");
                ImGui.Separator();
                ImGui.TableNextColumn();
                ImGui.Text($"{overallComplete / overallTotal:P2}%");
                ImGui.Separator();
                ImGui.TableNextRow();

                foreach (var category in plugin.QuestData.Categories)
                {
                    if (category == plugin.QuestData.Categories.Last() && configuration.ExcludeOtherQuests)
                    {
                        ImGui.TableNextColumn();
                        ImGui.TextDisabled(category.Title);
                        ImGui.TableNextColumn();
                        ImGui.TextDisabled($"{category.NumComplete}/{category.Total}");
                        ImGui.TableNextColumn();
                        ImGui.TextDisabled($"{category.NumComplete / category.Total:P2}%");
                        ImGui.TableNextRow();
                    }
                    else
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text(category.Title);
                        ImGui.TableNextColumn();
                        ImGui.Text($"{category.NumComplete}/{category.Total}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{category.NumComplete / category.Total:P2}%");
                        ImGui.TableNextRow();
                    }
                }
            }

            ImGui.EndTable();
            ImGui.EndChild();
        }

        private void DrawQuestsTab()
        {
            ImGui.BeginChild("##quests_tab", ImGuiHelpers.ScaledVector2(0), true);
            if (configuration.CategorySelection == null) ResetSelections();

            // If there's search text, show search results (this is global, not category-specific)
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                ImGui.SetNextItemWidth(-1);
                ImGui.InputTextWithHint("##search_input", "Search all quests...", ref searchText, 256);
                ImGui.Spacing();

                DrawSearchResults();
            }
            else
            {
                float availableWidth = ImGui.GetContentRegionAvail().X;
                float searchBoxWidth = 400;
                float spacing = ImGui.GetStyle().ItemSpacing.X;
                float comboWidth = 400;

                ImGui.SetNextItemWidth(comboWidth);
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

                // Right-align the search box on the same line
                ImGui.SameLine(availableWidth - searchBoxWidth);
                ImGui.SetNextItemWidth(searchBoxWidth);
                ImGui.InputTextWithHint("##search_input", "Search all quests...", ref searchText, 256);

                ImGui.Spacing();
                ImGui.SetNextItemWidth(comboWidth);
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
            }

            ImGui.EndChild();
        }

        private void DrawSearchResults()
        {
            var allQuests = GetQuests();
            var filteredQuests = allQuests.Where(questWithCategory =>
                questWithCategory.quest.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                questWithCategory.quest.Area.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();

            ImGui.Text($"Search Results ({filteredQuests.Count} found)");
            ImGui.Separator();

            var availableSize = ImGui.GetContentRegionAvail();
            if (ImGui.BeginChild("##search_results_scroll", new Vector2(availableSize.X, availableSize.Y), false, ImGuiWindowFlags.HorizontalScrollbar))
            {
                if (ImGui.BeginTable("##global_quest_table", 5, 
                    ImGuiTableFlags.Resizable | 
                    ImGuiTableFlags.BordersOuter | 
                    ImGuiTableFlags.BordersV | 
                    ImGuiTableFlags.ScrollX |
                    ImGuiTableFlags.SizingFixedFit))
                {
                    ImGui.TableSetupColumn("##check", ImGuiTableColumnFlags.WidthFixed, 30.0f);
                    ImGui.TableSetupColumn("Title", ImGuiTableColumnFlags.WidthFixed, 200.0f);
                    ImGui.TableSetupColumn("Category", ImGuiTableColumnFlags.WidthFixed, 250.0f);
                    ImGui.TableSetupColumn("Area", ImGuiTableColumnFlags.WidthFixed, 180.0f);
                    ImGui.TableSetupColumn("Level", ImGuiTableColumnFlags.WidthFixed, 60.0f);
                    ImGui.TableHeadersRow();

                    foreach (var questWithCategory in filteredQuests)
                    {
                        if (!questWithCategory.quest.Hide)
                        {
                            ImGui.TableNextColumn();
                            if (QuestDataManager.IsQuestComplete(questWithCategory.quest))
                            {
                                ImGui.PushFont(UiBuilder.IconFont);
                                ImGui.TextUnformatted(FontAwesomeIcon.Check.ToIconString());
                                ImGui.PopFont();
                                questWithCategory.quest.Hide = configuration.DisplayOption == 2;
                            }

                            ImGui.TableNextColumn();
                            ImGui.Text(questWithCategory.quest.Title);

                            ImGui.TableNextColumn();

                            // Use the direct parent category title instead of the full path
                            if (ImGui.Selectable($"{questWithCategory.directParentCategory.Title}##{questWithCategory.quest.Id[0]}_category"))
                            {
                                NavigateToCategory(questWithCategory.topLevelCategory, questWithCategory.directParentCategory);
                            }

                            ImGui.TableNextColumn();
                            if (ImGui.Selectable($"{questWithCategory.quest.Area}##{questWithCategory.quest.Id[0]}"))
                                OpenAreaMap(questWithCategory.quest);
                            ImGui.TableNextColumn();
                            ImGui.Text($"{questWithCategory.quest.Level}");
                            ImGui.TableNextRow();
                        }
                    }
                }

                ImGui.EndTable();
            }
            ImGui.EndChild();
        }

        private List<(Quest quest, string categoryPath, QuestData topLevelCategory, QuestData directParentCategory)> GetQuests()
        {
            var allQuests = new List<(Quest quest, string categoryPath, QuestData topLevelCategory, QuestData directParentCategory)>();

            foreach (var category in plugin.QuestData.Categories)
            {
                if (!category.Hide)
                {
                    GetQuestsData(category, category, category.Title, allQuests);
                }
            }

            return allQuests;
        }

        private void GetQuestsData(QuestData currentCategory, QuestData topLevelCategory, string categoryPath, List<(Quest quest, string categoryPath, QuestData topLevelCategory, QuestData directParentCategory)> allQuests)
        {
            foreach (var quest in currentCategory.Quests)
            {
                allQuests.Add((quest, categoryPath, topLevelCategory, currentCategory));
            }

            foreach (var subCategory in currentCategory.Categories)
            {
                if (!subCategory.Hide)
                {
                    var newPath = $"{categoryPath} > {subCategory.Title}";
                    GetQuestsData(subCategory, topLevelCategory, newPath, allQuests);
                }
            }
        }

        private void NavigateToCategory(QuestData topLevelCategory, QuestData directParentCategory)
        {
            searchText = "";

            configuration.CategorySelection = topLevelCategory;

            if (directParentCategory == topLevelCategory)
            {
                configuration.SubcategorySelection = topLevelCategory.Categories.Find(c => !c.Hide);
            }
            else
            {
                configuration.SubcategorySelection = directParentCategory;
            }

            configuration.Save();
        }

        private void GetQuests(QuestData questData, string categoryPath, List<(Quest quest, string categoryPath)> allQuests)
        {
            foreach (var quest in questData.Quests)
            {
                allQuests.Add((quest, categoryPath));
            }

            foreach (var subCategory in questData.Categories)
            {
                if (!subCategory.Hide)
                {
                    var newPath = $"{categoryPath} > {subCategory.Title}";
                    GetQuests(subCategory, newPath, allQuests);
                }
            }
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
                        if (QuestDataManager.IsQuestComplete(quest))
                        {
                            ImGui.PushFont(UiBuilder.IconFont);
                            ImGui.TextUnformatted(FontAwesomeIcon.Check.ToIconString());
                            ImGui.PopFont();
                            quest.Hide = configuration.DisplayOption == 2;
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
                        questDataManager.UpdateQuestData();
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
            if (ImGui.Checkbox("Show percentage \"Tribal Quests 32.13%\"", ref showPercentage))
            {
                configuration.ShowPercentage = showPercentage;
                configuration.Save();
            }

            ImGui.Spacing();

            var excludeOtherQuests = configuration.ExcludeOtherQuests;
            if (ImGui.Checkbox("Exclude \'Other Quests\' from Overall", ref excludeOtherQuests))
            {
                configuration.ExcludeOtherQuests = excludeOtherQuests;
                configuration.Save();
            }

            ImGui.EndChild();
        }

        private void ResetSelections()
        {
            if (configuration.CategorySelection == null || configuration.CategorySelection.Hide)
            {
                configuration.CategorySelection = plugin.QuestData.Categories.Find(c => !c.Hide);
                configuration.SubcategorySelection = configuration.CategorySelection.Categories.Find(c => !c.Hide);
            }

            if (configuration.SubcategorySelection == null || configuration.SubcategorySelection.Hide)
            {
                configuration.SubcategorySelection = configuration.CategorySelection.Categories.Find(c => !c.Hide);
            }

            configuration.Save();
        }

        private string GetDisplayText(QuestData questData)
        {
            var text = $"{questData.Title}";
            if (configuration.ShowCount) text += $" {questData.NumComplete}/{questData.Total}";
            if (configuration.ShowPercentage) text += $" {questData.NumComplete / questData.Total:P2}";
            return text;
        }

        private static void OpenAreaMap(Quest quest)
        {
            var questEnumerable = Plugin.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Quest>()
                                        .Where(q => quest.Id.Contains(q.RowId) && q.IssuerLocation.Value.RowId != null);
            Level level = questEnumerable.First().IssuerLocation.Value;
            var mapLink = new MapLinkPayload(level.Territory.RowId,
                                             level.Map.RowId,
                                             (int)(level.X * 1_000f),
                                             (int)(level.Z * 1_000f));
            Plugin.GameGui.OpenMapWithMapLink(mapLink);
        }
    }
}
