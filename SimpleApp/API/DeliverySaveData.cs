using ScheduleOne.GameTime;
using ScheduleOne.Persistence.Datas;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilkRoad.API
{
    [Serializable]
    public class DeliverySaveData : SaveData
    {
        public bool deliveryCompleted;
        public string lastDropID;
        public GameDateTime expiryDateTime; // ✅ add this

    }
}
