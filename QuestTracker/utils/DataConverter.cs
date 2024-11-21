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
    public static IDalamudPluginInterface PluginInterface { get; private set; }
    public static IDataManager DataManager { get; private set; }
    public static IPluginLog PluginLog { get; private set; }

    private string DirPath;

    public DataConverter(IDalamudPluginInterface pluginInterface, IDataManager dataManager, IPluginLog pluginLog)
    {
        PluginInterface = pluginInterface;
        DataManager = dataManager;
        PluginLog = pluginLog;

        DirPath = PluginInterface.AssemblyLocation.Directory.Parent.Parent.FullName + "/utils";
        
        ConvertRawDataTxt();
    }

    private void ConvertRawDataTxt()
    {
        try
        {
            PluginLog.Debug("Loading data from rawdata.txt");
            var rawPath = Path.Combine(DirPath, "rawdata.txt");
            List<string> lines = File.ReadLines(rawPath).ToList();
            PluginLog.Debug("Finished reading");

            List<Quest> RawQuests = new List<Quest>();

            foreach (var line in lines)
            {
                string[] tokens = line.Split("\t");

                Quest q = new Quest();
                q.Title = tokens[0];
                q.Area = tokens[1];
                var questLookup = DataManager.GetExcelSheet<Lumina.Excel.Sheets.Quest>().Where(quest => quest.Name.ToString().Contains(q.Title));
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

    private void WriteResults(Object obj)
    {
        PluginLog.Debug("Wrting to results.json");

        var resultPath = Path.Combine(DirPath, "results.json");
        JsonSerializerSettings config = new JsonSerializerSettings
            { DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore };
        var json = JsonConvert.SerializeObject(obj, Formatting.Indented, config);
        File.WriteAllText(resultPath, json);

        PluginLog.Debug("Finishing writing to results.json");
    }
}
