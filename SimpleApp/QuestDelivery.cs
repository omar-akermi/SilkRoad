using System;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using ScheduleOne.DevUtilities;
using ScheduleOne.Economy;
using ScheduleOne.GameTime;
using ScheduleOne.ItemFramework;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Product;
using ScheduleOne.Quests;
using ScheduleOneEnhanced.API;
using ScheduleOneEnhanced.API.Quests;
using ScheduleOneEnhanced.Mods.BulkBuyer;
using UnityEngine;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;
using Random = UnityEngine.Random;


namespace SilkRoad.Quests
{
    public class QuestDelivery : SOEQuest
    {

        [Serializable]
        public class DeliverySaveData : SaveData
        {
            public string productID;
            public int quantity;
            public int reward;
            public GameDateTime expiryDateTime; // ✅ NEW

        }


        public static int LoadCount = 0;
        public static event Action OnAnyComplete;
        [SaveableField("DeliveryData")]
        public DeliverySaveData deliveryData;

        [SaveableField("DeadDrop")]
        public DeadDropData deadDropData;

        private QuestEntry DeliveryEntry => Entries[0];
        private QuestEntry RewardEntry => Entries[1];

        public override void Awake()
        {
            LoadCount++;
            MelonLogger.Msg($"📦 QuestDelivery loaded (instance #{LoadCount})");

            soeQuestData = new SOEQuestData
            {
                questType = GetType().FullName
            };

            deliveryData ??= new DeliverySaveData();
            deadDropData ??= new DeadDropData();

            AddEntry();
            AddEntry();

            base.Awake();
            InitializeQuest(title, Description, Array.Empty<QuestEntryData>(), StaticGUID);
        }
        public QuestDelivery()
        {
            title = "Silkroad Delivery";
        }
        public override void Start()
        {
            Description = $"Deliver {deliveryData.quantity}x {deliveryData.productID} to the stash shown in the map.";
            if (deliveryData == null || deadDropData == null)
                throw new Exception("Quest data missing!");

            DeliveryEntry.SetEntryTitle($"Deliver {deliveryData.quantity}x {deliveryData.productID} to the stash.");
            RewardEntry.SetEntryTitle("Pick up your reward.");
            
            if (deadDropData.DeliveryDeadDrop != null)
            {
                DeliveryEntry.SetPoILocation(deadDropData.DeliveryDeadDrop.transform.position);
                deadDropData.DeliveryDeadDrop.Storage.onClosed.AddListener(CheckDelivery);
            }

            if (deadDropData.CollectDeadDrop != null)
            {
                RewardEntry.SetPoILocation(deadDropData.CollectDeadDrop.transform.position);
                deadDropData.CollectDeadDrop.Storage.onOpened.AddListener(GiveReward);
            }

            base.Start();
        }

        public void InitializeDelivery(string productID, int quantity, int reward)
        {

            int minsToComplete = 1440; // 1 in-game day = 1440 mins

            GameDateTime expiresAt = NetworkSingleton<TimeManager>.Instance.GetDateTime().AddMins(2880);

            deliveryData = new DeliverySaveData
            {
                productID = productID,
                quantity = quantity,
                reward = reward,
                expiryDateTime = expiresAt

            };
            ConfigureExpiry(true, deliveryData.expiryDateTime);

            DeadDrop delivery = DeadDrop.DeadDrops[Random.Range(0, DeadDrop.DeadDrops.Count)];
            DeadDrop rewardDrop = DeadDrop.DeadDrops[Random.Range(0, DeadDrop.DeadDrops.Count)];

            deadDropData = new DeadDropData
            {
                deliverDeadDropGUID = delivery.GUID.ToString(),
                collectDeadDropGUID = rewardDrop.GUID.ToString()
            };
        }
        private void CheckDelivery()
        {

            if (deliveryData == null || deadDropData?.DeliveryDeadDrop == null)
                return;

            MelonLogger.Msg("📥 CheckDelivery() triggered");
            MelonLogger.Msg($"🔍 Expecting: {deliveryData.quantity} bricks of productID: {deliveryData.productID}");

            var storage = deadDropData.DeliveryDeadDrop.Storage;

            List<ItemSlot> matchingSlots = storage.ItemSlots
                .Where(slot => slot.ItemInstance is ProductItemInstance item &&
                               item.PackagingID == "brick" &&
                               item.Definition.name == deliveryData.productID)
                .ToList();

            foreach (var slot in matchingSlots)
            {
                if (slot.ItemInstance is ProductItemInstance item)
                {
                    MelonLogger.Msg($"🧱 Slot with {slot.Quantity}x {item.Definition.ID} [{item.PackagingID}] found");
                }
            }

            int total = matchingSlots.Sum(slot => slot.Quantity);
            MelonLogger.Msg($"📦 Total matching bricks in stash: {total}");

            if (total < deliveryData.quantity)
            {
                MelonLogger.Msg($"❌ Not enough bricks: found {total}, need {deliveryData.quantity}");
                return;
            }

            int toRemove = deliveryData.quantity;
            foreach (ItemSlot slot in matchingSlots)
            {
                int removeQty = Mathf.Min(toRemove, slot.Quantity);
                slot.ChangeQuantity(-removeQty);
                toRemove -= removeQty;
                if (toRemove <= 0) break;
            }

            MelonLogger.Msg($"✅ Delivered {deliveryData.quantity} bricks of {deliveryData.productID}");

            DeliveryEntry.Complete();
            RewardEntry.SetState(EQuestState.Active, true);
            if (DeliveryEntry != null)
                DeliveryEntry.Complete();
            this.Description = $"\n<color=#AAAAAA><size=12>Reward ready: Collect it from the drop point.</size></color>";
        }

        private void GiveReward()
        {
            if (deliveryData == null || deadDropData?.CollectDeadDrop == null)
                return;

            if (DeliveryEntry.State != EQuestState.Completed)
            {
                MelonLogger.Warning("⛔ Tried to collect reward before completing delivery.");
                return;
            }

            var cash = (CashInstance)PlayerSingleton<PlayerInventory>.Instance.cashInstance.GetCopy();
            cash.SetBalance(deliveryData.reward);

            if (!deadDropData.CollectDeadDrop.Storage.CanItemFit(cash))
                return;

            deadDropData.CollectDeadDrop.Storage.InsertItem(cash);
            RewardEntry.Complete();
            
            MelonLogger.Msg($"💰 Reward of ${deliveryData.reward} inserted into reward stash.");
            MelonLogger.Msg("🏁 Quest complete!");
            
            MelonLogger.Msg("🏁 Triggering onComplete event");
            onComplete?.Invoke(); // trigger event (used in UI for NPCs, messages, etc.)
            OnAnyComplete?.Invoke(); // ✅ NEW
            Complete(); // mark quest fully done
            Quest.Quests.Remove(this);
            Quest.ActiveQuests.Remove(this);

            // Optional: destroy HUD UI
            var huds = GameObject.FindObjectsOfType<ScheduleOne.UI.QuestHUDUI>();
            foreach (var hud in huds)
            {
                if (hud != null && hud.Quest == this)
                {
                    GameObject.Destroy(hud.gameObject);
                    MelonLogger.Msg("🗑️ Destroyed QuestHUD UI for completed quest.");
                    break;
                }
            }

            GameObject.Destroy(this.gameObject); // ✅ clean up quest instance*/
        }
    }

}
