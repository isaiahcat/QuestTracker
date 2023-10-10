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
    private List<Quest> refQuests = new List<Quest>();
    private List<Quest> rawQuests = new List<Quest>();

    public static IPluginLog PluginLog { get; private set; }

    public DataConverter(DalamudPluginInterface pluginInterface, IPluginLog pluginLog)
    {
        PluginLog = pluginLog;

        try
        {
            PluginLog.Debug("Loading data from ref.json");

            var refPath = Path.Combine(pluginInterface.AssemblyLocation.Directory.Parent.Parent.FullName, "ref.json");
            var jsonString = File.ReadAllText(refPath);
            refQuests = JsonConvert.DeserializeObject<IdLookupData>(jsonString).Results;

            PluginLog.Debug("Load successful from ref.json");
        }
        catch (Exception e)
        {
            PluginLog.Error("Error loading QuestData from ref.jason");
            PluginLog.Error(e.Message);
        }

        try
        {
            PluginLog.Debug("Loading data from rawdata.txt");
            var rawPath = Path.Combine(pluginInterface.AssemblyLocation.Directory.Parent.Parent.FullName,
                                       "rawdata.txt");
            List<string> lines = File.ReadLines(rawPath).ToList();

            PluginLog.Debug("Finished reading");

            foreach (var line in lines)
            {
                string[] tokens = line.Split("\t");

                Quest q = new Quest();
                q.Title = tokens[0];
                q.Area = tokens[1];
                if (refQuests.Find(quest => quest.Title == q.Title) != null)
                {
                    q.Id = refQuests.Find(quest => quest.Title == q.Title).Id;
                }
                else
                {
                    PluginLog.Error($"Match not found for {q.Title}");
                }

                q.Level = short.Parse(tokens[2]);

                rawQuests.Add(q);
            }

            PluginLog.Debug("Wrting to resultdata.json");

            var resultPath = Path.Combine(pluginInterface.AssemblyLocation.Directory.Parent.Parent.FullName,
                                          "resultdata.json");
            var json = JsonConvert.SerializeObject(rawQuests, Formatting.Indented);
            File.WriteAllText(resultPath, json);

            PluginLog.Debug("Finishing writing");
        }
        catch (Exception e)
        {
            PluginLog.Error("Error loading data from rawdata.txt");
            PluginLog.Error(e.Message);
        }
    }
}

[Serializable]
public class IdLookupData
{
    public List<Quest> Results { get; set; } = new();
}
