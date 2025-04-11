using MelonLoader;
using UnityEngine;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Money;
using ScheduleOne.DevUtilities;

namespace SilkRoad
{
    public class DeliveryQuest : MelonMod
    {
        private readonly Vector3 dropOffLocation = new Vector3(-18.9826f, -4.035f, 173.6407f);
        private bool questCompleted = false;

        private string requiredItemId = "OGKushPackaged"; // Replace with actual item ID
        private uint requiredAmount = 1;
        private int rewardAmount = 500;

        public override void OnUpdate()
        {
            if (questCompleted || PlayerSingleton<PlayerMovement>.Instance == null)
                return;

            Vector3 playerPos = PlayerSingleton<PlayerMovement>.Instance.transform.position;
            if (Vector3.Distance(playerPos, dropOffLocation) < 5f)
            {
                TryCompleteQuest();
            }
        }

        private void TryCompleteQuest()
        {
            uint count = PlayerInventory.Instance.GetAmountOfItem(requiredItemId);
            if (count >= requiredAmount)
            {
                PlayerInventory.Instance.RemoveAmountOfItem(requiredItemId, requiredAmount);
                NetworkSingleton<MoneyManager>.Instance.ChangeCashBalance(rewardAmount, true, true);
                questCompleted = true;
                MelonLogger.Msg($"✅ Delivered {requiredAmount}x {requiredItemId}. +${rewardAmount} awarded!");
            }
            else
            {
                MelonLogger.Warning($"❌ You need {requiredAmount}x {requiredItemId}, but only have {count}.");
            }
        }
    }
}
