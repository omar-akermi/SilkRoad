using UnityEngine;
using ScheduleOne.UI.Phone;
using AsmResolver.PE.DotNet.Cil;
using FluffyUnderware.Curvy.Generator;
using ScheduleOne.UI;
using MelonLoader;


namespace SilkRoad
{
    public class SilkRoadApp : App<SilkRoadApp>
    {
        private SilkRoadAppUI ui;

        public override void OnStartClient(bool IsOwner)
        {
            if (!IsOwner) return;

            AppName = "Silk Road";
            IconLabel = "Silk";
            Orientation = EOrientation.Horizontal;

            var icon = Plugin.LoadImage("SilkRoadIcon.png");
            if (icon != null)
            {
                AppIcon = icon;
                MelonLogger.Msg("✅ Silk Road icon loaded.");
            }

            base.OnStartClient(IsOwner);

            ui = gameObject.AddComponent<SilkRoadAppUI>();
            ui.BuildUI(appContainer);
        }
    }
}
