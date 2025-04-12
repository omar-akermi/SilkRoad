using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using ScheduleOne.Messaging;
using ScheduleOne.Product;
using ScheduleOne.Quests;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Variables;
using ScheduleOne.UI.Phone.ContactsApp;
using ScheduleOne.GameTime;
using ScheduleOne.DevUtilities;


namespace SilkRoad
{
    public class BlackmarketBuyer : ModdedNPC
    {
        private bool _pendingOrder = false;
        private ProductDefinition _pendingProduct;
        private int _pendingAmount;
        private int _pendingReward;

        private static readonly string[] RandomTexts = new string[]
        {
            "Yo, I need {amount} bricks of {product}. I'll pay {price} if you drop it at the stash.",
            "Plug went silent. Can you bring me {amount} bricks of {product}? Got ${price} for you.",
            "Need {amount}x {product} bricks ASAP. Drop it off and the cash is yours: ${price}."
        };

        private static readonly string[] AcceptReplies = new string[]
        {
            "Good. I’ll be waiting.",
            "Drop it fast. I need it bad.",
            "You're the plug now. Let’s go."
        };

        private static readonly string[] DenyReplies = new string[]
        {
            "Weak move, man.",
            "Guess I’ll look elsewhere.",
            "Shame. I thought you had it."
        };

        public BlackmarketBuyer()
        {
            this.FirstName = "Blackmarket";
            this.LastName = "Buyer";
            this.ID = "npc_blackmarket_buyer";
        }

        public override void Awake()
        {
            // Hook into day pass
            NetworkSingleton<TimeManager>.Instance.onDayPass += SendRandomRequest;

            base.Awake();
        }

        private void SendRandomRequest()
        {
            if (_pendingOrder || ProductManager.DiscoveredProducts.Count == 0)
                return;

            _pendingProduct = ProductManager.DiscoveredProducts[UnityEngine.Random.Range(0, ProductManager.DiscoveredProducts.Count)];
            _pendingAmount = UnityEngine.Random.Range(5, 15); // Bricks
            _pendingReward = Mathf.RoundToInt(_pendingProduct.Price * 20 * _pendingAmount);

            string formattedText = RandomTexts[UnityEngine.Random.Range(0, RandomTexts.Length)]
                .Replace("{product}", $"<color=#33FF99>{_pendingProduct.Name}</color>")
                .Replace("{amount}", _pendingAmount.ToString())
                .Replace("{price}", $"<color=#00FF00>${_pendingReward}</color>");

            base.SendTextMessage(formattedText);
            _pendingOrder = true;

            // Prepare response options
            MSGConversation.ClearResponses(false);
            MSGConversation.ShowResponses(new List<Response>
            {
                new Response
                {
                    label = "ACCEPT",
                    text = AcceptReplies[UnityEngine.Random.Range(0, AcceptReplies.Length)],
                    callback = AcceptOrder
                },
                new Response
                {
                    label = "DENY",
                    text = DenyReplies[UnityEngine.Random.Range(0, DenyReplies.Length)],
                    callback = DenyOrder
                }
            }, 1f, true);
        }

        private void DenyOrder()
        {
            _pendingOrder = false;
            _pendingProduct = null;
            _pendingAmount = 0;
            _pendingReward = 0;

            base.SendTextMessage("Aight, maybe next time.");
        }

        private void AcceptOrder()
        {
            if (_pendingProduct == null)
            {
                base.SendTextMessage("Something went wrong. No product assigned.");
                return;
            }

            base.SendTextMessage("Good. Watch the stash.");
            SpawnDeliveryQuest(_pendingProduct, _pendingAmount, _pendingReward);
            _pendingOrder = false;
        }

        private void SpawnDeliveryQuest(ProductDefinition product, int amount, int reward)
        {
            GameObject questGO = new GameObject("SilkRoad_Quest_" + product.name);
            var quest = questGO.AddComponent<QuestDelivery>();
            quest.Init(product, amount, reward);

            // Hook onComplete to auto-destroy and log
            quest.onComplete.AddListener(() =>
            {
                MelonLoader.MelonLogger.Msg($"✅ Quest for {product.name} complete. Cleaning up.");
                GameObject.Destroy(questGO);
            });
        }
    }
}
