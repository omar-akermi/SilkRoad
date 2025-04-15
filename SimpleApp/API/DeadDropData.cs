using System;
using System.Linq;
using ScheduleOne.Economy;
using ScheduleOne.Persistence.Datas;

namespace ScheduleOneEnhanced.Mods.BulkBuyer
{
    [Serializable]
    public class DeadDropData : SaveData
    {
        public string? deliverDeadDropGUID;
        public string? collectDeadDropGUID;
    
        public DeadDrop? DeliveryDeadDrop => DeadDrop.DeadDrops
            .FirstOrDefault(deadDrop => deadDrop.GUID.ToString() == deliverDeadDropGUID);
        
        public DeadDrop? CollectDeadDrop => DeadDrop.DeadDrops
            .FirstOrDefault(deadDrop => deadDrop.GUID.ToString() == collectDeadDropGUID);
    }
}