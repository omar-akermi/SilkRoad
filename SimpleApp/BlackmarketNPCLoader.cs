using System;
using HarmonyLib;
using UnityEngine;
using ScheduleOne.Persistence.Loaders;
using SilkRoad;

namespace SilkRoad
{
    [HarmonyPatch]
    public class BlackmarketNPCLoader
    {
        [HarmonyPatch(typeof(NPCsLoader), "Load")]
        [HarmonyPostfix]
        public static void SpawnBlackmarketBuyer()
        {
            MelonLoader.MelonLogger.Msg("📦 Hooked into NPCsLoader.Load");

            if (GameObject.Find("Blackmarket Buyer") != null)
            {
                MelonLoader.MelonLogger.Msg("⚠️ Blackmarket Buyer already exists");
                return;
            }

            GameObject npcGO = new GameObject("Blackmarket Buyer");
            GameObject.DontDestroyOnLoad(npcGO);
            npcGO.AddComponent<BlackmarketBuyer>();

            MelonLoader.MelonLogger.Msg("✅ Blackmarket Buyer NPC spawned and hooked");
        }
    }
}
