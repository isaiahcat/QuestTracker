using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;

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

        public QuestData SidePanelSelection;
        public QuestData DropdownSelection;

        public QuestTrackerUI(Plugin plugin, Configuration configuration)
        {
            this.plugin = plugin;
            this.configuration = configuration;
        }

        public void Reset()
        {
            SidePanelSelection = null;
            DropdownSelection = null;
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

        private void DrawMainWindow()
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

            ImGui.SetNextWindowSize(new Vector2(375, 440), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(375, 440), new Vector2(float.MaxValue, float.MaxValue));
            if (ImGui.Begin("Quest Tracker", ref this.visible))
            {
                ImGui.BeginGroup();
                if (ImGui.BeginChild("##category_select",
                                     ImGuiHelpers.ScaledVector2(width, 0) - iconButtonSize with { X = 0 }, true))
                {
                    if (configuration.ShowOverall)
                    {
                        ImGui.Text("Overall" + GetDisplayText(plugin.QuestData, true));
                        ImGui.Spacing();
                        ImGui.Separator();
                        ImGui.Spacing();
                    }

                    DrawSidePanel();
                }

                ImGui.EndChild();
                if (ImGui.Button("Settings"))
                {
                    SidePanelSelection = null;
                    SettingsVisible = true;
                }

                iconButtonSize = ImGui.GetItemRectSize() + ImGui.GetStyle().ItemSpacing;
                ImGui.EndGroup();

                ImGui.SameLine();
                if (ImGui.BeginChild("##category_view", ImGuiHelpers.ScaledVector2(0), true))
                {
                    if (SidePanelSelection == null && SettingsVisible)
                    {
                        DrawSettings();
                    }
                    else
                    {
                        switch (this.configuration.LayoutOption)
                        {
                            case 0:
                                DrawLayout1();
                                break;
                            case 1:
                                DrawLayout2();
                                break;
                            case 2:
                                DrawLayout3();
                                break;
                        }
                    }
                }

                ImGui.EndChild();
            }

            ImGui.End();
        }

        private void DrawLayout1()
        {
            if (SidePanelSelection == null) return;
            foreach (var category in SidePanelSelection.Categories)
            {
                if (!category.Hide)
                {
                    if (ImGui.CollapsingHeader(GetDisplayText(category, false)))
                    {
                        DrawQuestList(category);
                    }
                }
            }
        }

        private void DrawLayout2()
        {
            DrawDropdown();
            DrawQuestList(DropdownSelection);
        }

        private void DrawLayout3()
        {
            DrawDropdown();
            if (DropdownSelection.Categories.Count > 0)
            {
                foreach (var subcategory in DropdownSelection.Categories)
                {
                    if (!subcategory.Hide)
                    {
                        if (ImGui.CollapsingHeader(GetDisplayText(subcategory, false)))
                        {
                            DrawQuestTable(subcategory.Quests);
                        }
                    }
                }
            }
            else
            {
                DrawQuestTable(DropdownSelection.Quests);
            }
        }

        private void DrawDropdown()
        {
            if (DropdownSelection == null) return;
            if (ImGui.BeginCombo("##subcategory_select", GetDisplayText(DropdownSelection, false)))
            {
                foreach (var category in SidePanelSelection.Categories)
                {
                    if (!category.Hide)
                    {
                        if (ImGui.Selectable(GetDisplayText(category, false), DropdownSelection == category))
                        {
                            DropdownSelection = category;
                        }

                        if (DropdownSelection == category)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.Spacing();
        }

        private void DrawQuestList(QuestData category)
        {
            if (category == null) return;
            if (category.Categories.Count > 0)
            {
                foreach (var subcategory in category.Categories)
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
                DrawQuestTable(category.Quests);
            }
        }

        private void DrawQuestTable(List<Quest> quests)
        {
            if (ImGui.BeginTable("##quest_table", 4))
            {
                ImGui.TableSetupColumn("##icon", ImGuiTableColumnFlags.None, 0.10f);
                ImGui.TableSetupColumn("Title");
                ImGui.TableSetupColumn("Area", ImGuiTableColumnFlags.None, 0.90f);
                ImGui.TableSetupColumn("Level", ImGuiTableColumnFlags.None, 0.20f);
                ImGui.TableHeadersRow();
                foreach (var quest in quests)
                {
                    if (!quest.Hide)
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

        private void DrawSidePanel()
        {
            foreach (var category in plugin.QuestData.Categories)
            {
                if (ImGui.Selectable(GetDisplayText(category, false), SidePanelSelection == category))
                {
                    SidePanelSelection = category;
                    SettingsVisible = false;
                    DropdownSelection = SidePanelSelection.Categories.Find(c => !c.Hide);
                }
            }
        }

        private void DrawSettings()
        {
            ImGui.SetNextItemWidth(90);
            var layoutOption = this.configuration.LayoutOption;
            string[] layoutList = { "Layout 1", "Layout 2", "Layout 3" };
            if (ImGui.BeginCombo("##layout_option", layoutList[layoutOption]))
            {
                for (int i = 0; i < layoutList.Length; i++)
                {
                    if (ImGui.Selectable(layoutList[i]))
                    {
                        this.configuration.LayoutOption = i;
                        this.configuration.Save();
                    }

                    if (layoutOption == i)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.Spacing();
            
            ImGui.SetNextItemWidth(130);
            var displayOption = this.configuration.DisplayOption;
            string[] displayList = { "Show All", "Show Complete", "Show Incomplete" };
            if (ImGui.BeginCombo("##display_option", displayList[displayOption]))
            {
                for (int i = 0; i < displayList.Length; i++)
                {
                    if (ImGui.Selectable(displayList[i]))
                    {
                        this.configuration.DisplayOption = i;
                        this.configuration.Save();
                        this.plugin.UpdateQuestData(plugin.QuestData);
                        Reset();
                    }

                    if (displayOption == i)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }

                ImGui.EndCombo();
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
        }

        private string GetDisplayText(QuestData questData, bool addSymbol)
        {
            var text = $"{questData.Title}";
            if (configuration.ShowCount)
            {
                text += $" {questData.NumComplete}/{questData.Total}";
            }

            if (configuration.ShowPercentage)
            {
                text += addSymbol
                            ? $" {questData.NumComplete / questData.Total:P0}%"
                            : $" {questData.NumComplete / questData.Total:P0}";
            }

            return text;
        }
    }
}
