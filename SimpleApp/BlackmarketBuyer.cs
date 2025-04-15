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
using SilkRoad.Quests;


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




    }
}
