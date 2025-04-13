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
using ScheduleOne.GameTime;
using Harmony;
using ScheduleOne.Map;
using ScheduleOne.UI;

namespace SilkRoad
{
    public class QuestDelivery : Quest
    {
        private DeadDrop deliverDrop;
        private DeadDrop rewardDrop;
        private QuestEntry deliveryEntry;
        private QuestEntry rewardEntry;

        private ProductDefinition product;
        private int amount;
        private int reward;
        private bool deliveryCompleted = false; // ✅ flag to track delivery status
        public UnityEvent onActiveState = new UnityEvent();
        public UnityEvent onComplete = new UnityEvent();
        public UnityEvent onInitialComplete = new UnityEvent();
        public UnityEvent onQuestBegin = new UnityEvent();
        public UnityEvent<EQuestState> onQuestEnd = new UnityEvent<EQuestState>();
        public UnityEvent<bool> onTrackChange = new UnityEvent<bool>();

        public bool TrackOnBegin = true;
        public bool AutoCompleteOnAllEntriesComplete = true;


        public void Init(ProductDefinition productDef, int amount, int reward)
        {
            MelonLogger.Msg("🚀 QuestDelivery.Init() called");
            MelonLogger.Msg($"📦 Product: {productDef?.Name ?? "NULL"}, Amount: {amount}, Reward: ${reward}");

            this.product = productDef;
            this.amount = amount;
            this.reward = reward;
            this.autoInitialize = false;
            this.IsTracked = true;
            this.title = $"{productDef.Name} Delivery";
            this.Description = $"Deliver {amount}x {productDef.Name} bricks to the stash.";
            this.Expires = true;

            MelonLogger.Msg("⏳ Setting expiry (2 in-game days)");
            GameDateTime expiry = NetworkSingleton<TimeManager>.Instance.GetDateTime().AddMins(2880);
            this.ConfigureExpiry(true, expiry);

            // Dead drop targets
            if (DeadDrop.DeadDrops == null || DeadDrop.DeadDrops.Count <= 5)
            {
                MelonLogger.Error("❌ DeadDrops list is missing or too short!");
                return;
            }

            deliverDrop = DeadDrop.DeadDrops[5];
            rewardDrop = DeadDrop.DeadDrops[5];
            MelonLogger.Msg($"📍 Using drop point: {deliverDrop?.name}");

            // Hook delivery events
            if (deliverDrop?.Storage == null || rewardDrop?.Storage == null)
            {
                MelonLogger.Error("❌ Drop storage is null!");
                return;
            }

            deliverDrop.Storage.onClosed.AddListener(HandleDelivery);
            rewardDrop.Storage.onOpened.AddListener(HandleReward);
            MelonLogger.Msg("✅ Subscribed to drop storage events");

            // Icon & POI
            MelonLogger.Msg("🎨 Creating IconPrefab & PoIPrefab");
            this.IconPrefab = CreateIconPrefab().GetComponent<RectTransform>();
            this.PoIPrefab = CreatePoIPrefab();

            // Delivery Entry
            MelonLogger.Msg("📄 Creating delivery quest entry");
            GameObject deliverGO = new GameObject("DeliveryEntry");
            deliverGO.transform.SetParent(transform);
            deliveryEntry = deliverGO.AddComponent<QuestEntry>();
            deliveryEntry.SetEntryTitle($"Deliver {amount}x {productDef.Name}");
            deliveryEntry.PoILocation = deliverDrop.transform;

            // Reward Entry
            MelonLogger.Msg("📄 Creating reward quest entry");
            GameObject rewardGO = new GameObject("RewardEntry");
            rewardGO.transform.SetParent(transform);
            rewardEntry = rewardGO.AddComponent<QuestEntry>();
            rewardEntry.SetEntryTitle($"Collect ${reward} reward");
            rewardEntry.PoILocation = rewardDrop.transform;

            // Entries list
            Entries.Add(deliveryEntry);
            Entries.Add(rewardEntry);
            MelonLogger.Msg("✅ Added entries to quest");

            // Register quest
            Quest.Quests.Add(this);
            Quest.ActiveQuests.Add(this);
            MelonLogger.Msg("📝 Registered quest in Quest lists");

            // Initialize data
            try
            {
                string guid = Guid.NewGuid().ToString();
                this.InitializeQuest(title, Description, Entries.Select(e => e.GetSaveData()).ToArray(), guid);
                MelonLogger.Msg($"🧠 Initialized quest with GUID: {guid}");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"❌ Failed to initialize quest: {ex.Message}");
            }

            // Start
            this.Begin(true);
            MelonLogger.Msg("🔥 Quest.Begin(true) called");
        }


        private GameObject CreateIconPrefab()
        {
            GameObject icon = new GameObject("IconPrefab", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            icon.transform.SetParent(transform);
            Image img = icon.GetComponent<Image>();
            var iconn = Plugin.LoadImage("SilkRoadIcon.png");
            img.sprite = iconn;
            return icon;
        }

        private GameObject CreatePoIPrefab()
        {
            GameObject poiGO = new GameObject("POIPrefab");
            poiGO.transform.SetParent(transform);

            POI poi = poiGO.AddComponent<POI>();
            poi.DefaultMainText = "Blackmarket Request";

            var field = HarmonyLib.AccessTools.Field(typeof(POI), "UIPrefab");
            field.SetValue(poi, CreatePoIUIPrefab());

            return poiGO;
        }

        private GameObject CreatePoIUIPrefab()
        {
            GameObject poiUI = new GameObject("PoIUIPrefab", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.EventSystems.EventTrigger), typeof(Button));
            poiUI.transform.SetParent(transform);

            GameObject label = new GameObject("MainLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.transform.SetParent(poiUI.transform);

            GameObject icon = new GameObject("IconContainer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            icon.transform.SetParent(poiUI.transform);
            icon.GetComponent<Image>().sprite = PlayerSingleton<ScheduleOne.UI.Phone.ContactsApp.ContactsApp>.Instance.AppIcon;

            return poiUI;
        }
        private void HandleDelivery()
        {
            if (deliveryCompleted) return;

            MelonLogger.Msg("📥 HandleDelivery() triggered!");

            if (deliverDrop == null || product == null)
                return;

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
            deliveryCompleted = true;

            // ✅ Just like QuestBulkOrder — mark first step as complete
            if (deliveryEntry != null)
                deliveryEntry.Complete();

            if (rewardEntry != null)
                rewardEntry.SetState(EQuestState.Active, true); // true = show toast popup or highlight

            // Optional: Notify NPC
            // BlackmarketBuyer.Instance?.NotifyDelivery(product.name);
        }


        private void HandleReward()
        {
            if (!deliveryCompleted) return;

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
                deliveryCompleted = true;

                rewardEntry?.Complete();
                deliveryEntry?.Complete(); // just in case
                onComplete?.Invoke();

                MelonLogger.Msg($"💰 Inserted ${reward} into reward stash.");
                MelonLogger.Msg("🏁 Reward collected. Quest complete!");

                Complete(); // ✅ properly ends quest
                Quest.ActiveQuests.Remove(this);
                Destroy(gameObject); // ✅ destroy to prevent reactivation
            }
        }


        public override void Start()
        {
            base.Start();
            Begin(); // Must call Begin() here like in BulkOrder
        }
    }
}
