using System;
using ScheduleOne.Persistence.Datas;

namespace ScheduleOneEnhanced.API.Quests
{
    [Serializable]
    public class SOEQuestData : SaveData
    {
        public string? questType;
    }
}