using System;
using System.Linq;
using ScheduleOne.GameTime;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Product;

namespace ScheduleOneEnhanced.Mods.BulkBuyer
{
    [Serializable]
    public class OrderData : SaveData
    {
        public string? productID;
        public int amount;
        public int price;
        public GameDateTime expiryDateTime;
        
        public ProductDefinition? Product => ProductManager.DiscoveredProducts
            .FirstOrDefault(product => product.ID == productID);
    }
}