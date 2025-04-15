using System;
using ScheduleOne.Persistence.Datas;

namespace ScheduleOneEnhanced.Mods.BulkBuyer
{
    [Serializable]
    public class IntroData : SaveData
    {
        public int progress;

        public IntroData(int progress = 0)
        {
            this.progress = progress;
        }
    }
}