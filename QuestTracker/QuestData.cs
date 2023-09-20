using System;
using System.Collections.Generic;

namespace QuestTracker;

public class QuestData
{
    public List<QuestCategory> Categories { get; set; } = new();
}

[Serializable]
public class QuestCategory
{
    public string Title { get; set; }
    public List<QuestSubcategory> Subcategories { get; set; } = new();
}

[Serializable]
public class QuestSubcategory
{
    public string Title { get; set; }
    public List<Quest> Quests { get; set; } = new();
    public int NumComplete { get; set; } = 0;
}

[Serializable]
public class Quest
{
    public string Title { get; set; }
    public uint Id { get; set; }
    public bool IsQuestCompleted { get; set; } = false;
}
