using ScheduleOne.Product;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;
using ScheduleOne.DevUtilities;
using ScheduleOne.Economy;
using ScheduleOne.ItemFramework;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Quests;
using UnityEngine.UI;
using System;
using MelonLoader;

namespace SilkRoad
{
    public class QuestDelivery : Contract
    {
        private DeadDrop deliverDrop;
        private DeadDrop rewardDrop;
        private QuestEntry deliveryEntry;
        private QuestEntry rewardEntry;

        private ProductDefinition product;
        private int amount;
        private int reward;
        private bool deliveryCompleted = false; // ✅ flag to track delivery status

        public QuestDelivery()
        {
            onActiveState = new UnityEvent();
            onComplete = new UnityEvent();
            onInitialComplete = new UnityEvent();
            onQuestBegin = new UnityEvent();
            onQuestEnd = new UnityEvent<EQuestState>();
            onTrackChange = new UnityEvent<bool>();

            TrackOnBegin = true;
            AutoCompleteOnAllEntriesComplete = true;
        }

        public void Init(ProductDefinition productDef, int amount, int reward)
        {

            this.product = productDef;
            this.amount = amount;
            this.reward = reward;

            title = $"{productDef.Name} Delivery";
            Description = $"Deliver {amount}x {productDef.Name} to the stash.";

            deliverDrop = DeadDrop.DeadDrops[5]; // Or random like in BulkOrder
            rewardDrop = DeadDrop.DeadDrops[5];

            deliverDrop.Storage.onClosed.AddListener(HandleDelivery);
            rewardDrop.Storage.onOpened.AddListener(HandleReward);

            GameObject deliverGO = new GameObject("DeliveryEntry");
            deliverGO.transform.SetParent(transform);
            deliveryEntry = deliverGO.AddComponent<QuestEntry>();
            deliveryEntry.SetEntryTitle($"Deliver {amount}x {productDef.Name}");
            deliveryEntry.PoILocation = deliverDrop.transform;

            GameObject rewardGO = new GameObject("RewardEntry");
            rewardGO.transform.SetParent(transform);
            rewardEntry = rewardGO.AddComponent<QuestEntry>();
            rewardEntry.SetEntryTitle($"Collect ${reward} reward");
            rewardEntry.PoILocation = rewardDrop.transform;
            // ✅ Add to ActiveQuests
            if (!Quest.ActiveQuests.Contains(this))
            {
                Quest.ActiveQuests.Add(this);
                MelonLogger.Msg("📝 Added to Quest.ActiveQuests");
            }
            Entries.Add(deliveryEntry);
            Entries.Add(rewardEntry);
            
        }

        private void HandleDelivery()
        {
            MelonLogger.Msg("📥 HandleDelivery() triggered!");

            if (deliverDrop == null || product == null) return;

            List<ItemSlot> matchingSlots = deliverDrop.Storage.ItemSlots
                .Where(slot => slot.ItemInstance != null && slot.ItemInstance.Definition.name == product.name)
                .ToList();

            int totalAmount = matchingSlots.Sum(slot => slot.Quantity);
            if (totalAmount < amount)
            {
                MelonLogger.Msg($"❌ Not enough {product.name}. Found {totalAmount}, need {amount}");
                return;
            }

            int toRemove = amount;
            foreach (var slot in matchingSlots)
            {
                int removeQty = Mathf.Min(toRemove, slot.Quantity);
                slot.ChangeQuantity(-removeQty);
                toRemove -= removeQty;
                if (toRemove <= 0) break;
            }

            MelonLogger.Msg($"✅ Delivered {amount}x {product.name}.");
            deliveryCompleted = true; // ✅ mark delivery as complete

            deliveryEntry?.Complete();
            rewardEntry?.SetState(EQuestState.Active);
        }


        private void HandleReward()
        {
            if (!deliveryCompleted)
            {
                MelonLogger.Warning("⛔ Tried to collect reward before completing delivery.");
                return;
            }

            if (rewardDrop == null) return;

            CashInstance cash = (CashInstance)PlayerSingleton<PlayerInventory>.Instance.cashInstance.GetCopy();
            cash.SetBalance(reward);
            if (rewardDrop.Storage.CanItemFit(cash))
            {
                rewardDrop.Storage.InsertItem(cash);
                rewardEntry?.Complete();

                MelonLogger.Msg("💰 Inserted $" + reward + " into reward stash.");
                MelonLogger.Msg("🏁 Reward collected. Quest complete!");
                Complete(); // ✅ mark quest as complete
            }
        }


        public override void Start()
        {
            base.Start();
            Begin(); // Must call Begin() here like in BulkOrder
        }
    }
}
