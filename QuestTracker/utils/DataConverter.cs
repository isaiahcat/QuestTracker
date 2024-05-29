using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;

namespace QuestTracker;

public class DataConverter
{
    public static DalamudPluginInterface PluginInterface { get; private set; }
    public static IDataManager DataManager { get; private set; }
    public static IPluginLog PluginLog { get; private set; }

    private List<Quest> RefQuests;
    private List<Quest> RawQuests;

    private QuestData QD;

    public DataConverter(DalamudPluginInterface pluginInterface, IDataManager dataManager, IPluginLog pluginLog)
    {
        PluginInterface = pluginInterface;
        DataManager = dataManager;
        PluginLog = pluginLog;
        
        //RefQuests = LoadFromFile("ref.json").Quests;
        //QD = LoadFromFile("data.json");
        //CompareDataJson(QD);
        //WriteResults(QD);

        ConvertRawDataTxt();
    }

    private void CompareDataJson(QuestData questData)
    {
        if (questData.Categories.Count > 0)
        {
            foreach (var category in questData.Categories)
            {
                CompareDataJson(category);
            }
        }
        else
        {
            foreach (var quest in questData.Quests)
            {
                foreach (var refQuest in RefQuests.FindAll(q => quest.Title == q.Title))
                {
                    foreach (var id in refQuest.Id)
                    {
                        if (!quest.Id.Contains(id))
                        {
                            quest.Id.Add(id);
                        }   
                    }
                }
            }
        }
    }

    private void ConvertRawDataTxt()
    {
        try
        {
            PluginLog.Debug("Loading data from rawdata.txt");
            var rawPath = Path.Combine(PluginInterface.AssemblyLocation.Directory.Parent.Parent.FullName+"/utils",
                                       "rawdata.txt");
            List<string> lines = File.ReadLines(rawPath).ToList();
            PluginLog.Debug("Finished reading");

            RawQuests = new List<Quest>();

            foreach (var line in lines)
            {
                string[] tokens = line.Split("\t");

                Quest q = new Quest();
                q.Title = tokens[0];
                q.Area = tokens[1];
                var questLookup = DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Quest>().Where(quest => quest.Name.RawString.Contains(q.Title));
                if (questLookup.Any())
                {
                    q.Id = new List<uint>();
                    foreach (var quest in questLookup)
                    {
                        q.Id.Add(quest.RowId);
                    }
                }
                else
                {
                    PluginLog.Error($"Match not found for {q.Title}");
                }
            //     if (RefQuests.Find(quest => quest.Title == q.Title) != null)
            //     {
            //         q.Id = RefQuests.Find(quest => quest.Title == q.Title).Id;
            //     }

                q.Level = short.Parse(tokens[2]);

                RawQuests.Add(q);
            }

            WriteResults(RawQuests);
        }
        catch (Exception e)
        {
            PluginLog.Error("Error loading from rawdata.txt");
            PluginLog.Error(e.Message);
        }
    }

    private QuestData LoadFromFile(string filename)
    {
        try
        {
            PluginLog.Debug($"Loading from {filename}");
            var path = Path.Combine(PluginInterface.AssemblyLocation.Directory.Parent.Parent.FullName, filename);
            var jsonString = File.ReadAllText(path);
            var result = JsonConvert.DeserializeObject<QuestData>(jsonString);
            PluginLog.Debug($"Load from {filename} successful");
            return result;
        }
        catch (Exception e)
        {
            PluginLog.Error($"Error loading from {filename}");
            PluginLog.Error(e.Message);
            return null;
        }
    }

    private void WriteResults(Object obj)
    {
        PluginLog.Debug("Wrting to resultdata.json");

        var resultPath = Path.Combine(PluginInterface.AssemblyLocation.Directory.Parent.Parent.FullName+"/utils",
                                      "resultdata.json");
        var json = JsonConvert.SerializeObject(obj, Formatting.Indented);
        File.WriteAllText(resultPath, json);

        PluginLog.Debug("Finishing writing to resultdata.json");
    }
}

[Serializable]
public class IdLookupData
{
    public List<Quest> Quests { get; set; } = new();
}
