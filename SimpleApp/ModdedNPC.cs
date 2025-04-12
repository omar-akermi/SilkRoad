using System;
using System.Collections.Generic;
using System.Reflection;
using FishNet.Object;
using HarmonyLib;
using MelonLoader;
using ScheduleOne.DevUtilities;
using ScheduleOne.Interaction;
using ScheduleOne.Messaging;
using ScheduleOne.Noise;
using ScheduleOne.NPCs;
using ScheduleOne.NPCs.Relation;
using ScheduleOne.NPCs.Responses;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI.Phone.ContactsApp;
using ScheduleOne.UI.WorldspacePopup;
using ScheduleOne.Variables;
using ScheduleOne.Vehicles;
using ScheduleOne.Vision;
using UnityEngine;
using UnityEngine.Events;

namespace SilkRoad
{
    public class ModdedNPC : ScheduleOne.NPCs.NPC
    {
        public ModdedNPC()
        {
            this.FirstName = "Custom";
            this.LastName = "NPC";
            this.ID = "custom_npc";
            this.BakedGUID = Guid.NewGuid().ToString();
            this.MugshotSprite = PlayerSingleton<ContactsApp>.Instance.AppIcon;
            this.ConversationCategories = new List<EConversationCategory> { EConversationCategory.Customer };
        }

        public override void Awake()
        {
            base.gameObject.SetActive(false);

            // Message system
            this.CreateMessageConversation();

            // Awareness setup
            this.Health = gameObject.GetComponent<NPCHealth>() ?? gameObject.AddComponent<NPCHealth>();
            this.Health.onDie = new UnityEvent();
            this.Health.onKnockedOut = new UnityEvent();

            var awarenessGO = new GameObject("NPCAwareness");
            awarenessGO.transform.SetParent(transform);
            this.awareness = awarenessGO.AddComponent<NPCAwareness>();
            this.awareness.Listener = gameObject.AddComponent<Listener>();
            this.awareness.onExplosionHeard = new UnityEvent<ScheduleOne.Noise.NoiseEvent>();
            this.awareness.onGunshotHeard = new UnityEvent<ScheduleOne.Noise.NoiseEvent>();
            this.awareness.onHitByCar = new UnityEvent<LandVehicle>();
            this.awareness.onNoticedDrugDealing = new UnityEvent<Player>();
            this.awareness.onNoticedGeneralCrime = new UnityEvent<Player>();
            this.awareness.onNoticedPettyCrime = new UnityEvent<Player>();
            this.awareness.onNoticedPlayerViolatingCurfew = new UnityEvent<Player>();
            this.awareness.onNoticedSuspiciousPlayer = new UnityEvent<Player>();

            var responsesGO = new GameObject("NPCResponses");
            responsesGO.transform.SetParent(transform);
            this.awareness.Responses = responsesGO.AddComponent<NPCResponses_Civilian>();

            var visionGO = new GameObject("VisionCone");
            visionGO.transform.SetParent(transform);
            this.awareness.VisionCone = visionGO.AddComponent<VisionCone>();
            this.awareness.VisionCone.QuestionMarkPopup = gameObject.AddComponent<WorldspacePopup>();

            this.intObj = gameObject.AddComponent<InteractableObject>();
            this.RelationData = new NPCRelationData();

            // Mark unlocked
            this.RelationData.onUnlocked += (unlockType, notify) =>
            {
                if (!string.IsNullOrEmpty(this.NPCUnlockedVariable))
                {
                    NetworkSingleton<VariableDatabase>.Instance.SetVariableValue(this.NPCUnlockedVariable, true.ToString(), true);
                }
            };

            // Inventory
            var inventory = gameObject.AddComponent<NPCInventory>();
            inventory.PickpocketIntObj = gameObject.AddComponent<InteractableObject>();

            // Avatar and registry
            this.Avatar = Player.Local.Avatar;
            NPCManager.NPCRegistry.Add(this);

            // Attach network identity
            var netObj = gameObject.AddComponent<NetworkObject>();
            PropertyInfo netBehavioursProp = AccessTools.Property(typeof(NetworkObject), "NetworkBehaviours");
            netBehavioursProp.SetValue(netObj, new ModdedNPC[] { this });

            gameObject.SetActive(true);
            awarenessGO.SetActive(true);
            responsesGO.SetActive(true);
            visionGO.SetActive(true);

            MelonLogger.Msg("🧑‍💻 ModdedNPC initialized.");
            base.Awake();
        }

        protected virtual void Start()
        {
            base.Start();
        }
    }
}
