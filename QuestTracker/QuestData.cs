using System;
using System.Collections.Generic;

namespace QuestTracker;

public class QuestData
{
    public string Title { get; set; }
    public List<QuestData> Categories { get; set; } = new();
    public List<Quest> Quests { get; set; } = new();
    public float NumComplete { get; set; }
    public float Total { get; set; }
    public bool Hide { get; set; }
}

[Serializable]
public class Quest
{
    public string Title { get; set; }
    public List<uint> Id { get; set; }
    public string Area { get; set; } = "";    
    public string Start { get; set; }
    public string Gc { get; set; }
    public int Level { get; set; }
    public bool Hide { get; set; }
}
