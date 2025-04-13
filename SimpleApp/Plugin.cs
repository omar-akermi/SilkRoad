using MelonLoader;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using ScheduleOne.UI.Phone;
using ScheduleOne.DevUtilities;
using ScheduleOne.UI.Phone.ProductManagerApp;
using ScheduleOne.Product;
using HarmonyLib;
[assembly: MelonInfo(typeof(SilkRoad.Plugin), "SilkRoad", "1.0.0", "Nourchene")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace SilkRoad
{
    public class Plugin : MelonMod
    {
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("🚚 Silk Road mod loaded!");
            HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("com.silkroad.npc");
            harmony.PatchAll(typeof(SilkRoad.BlackmarketNPCLoader).Assembly);
        }
        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName != "Main") return;
            MelonCoroutines.Start(InitSilkRoadApp());

            MelonLogger.Msg("📦 Spawning Blackmarket Buyer NPC...");

            GameObject npcGO = new GameObject("Blackmarket Buyer");
            GameObject.DontDestroyOnLoad(npcGO);
        }

        

        private IEnumerator InitSilkRoadApp()
        {
            while (PlayerSingleton<AppsCanvas>.Instance == null)
                yield return null;

            var appsCanvas = PlayerSingleton<AppsCanvas>.Instance;
            Transform parentCanvas = appsCanvas.canvas.transform;
            foreach (Transform child in PlayerSingleton<AppsCanvas>.Instance.canvas.transform)
            {
                if (child.name == "SilkRoadApp" && child.GetComponent<SilkRoadApp>() == null)
                {
                    GameObject.Destroy(child.gameObject); // Destroy old, non-cloned versions
                }
            }
            // Remove 8th icon (index 7) on phone open
            var iconGrid = GameObject.Find("Player_Local/CameraContainer/Camera/OverlayCamera/GameplayMenu/Phone/phone/HomeScreen/AppIcons");
            if (iconGrid != null && iconGrid.transform.childCount > 7)
            {
                var oldClone = iconGrid.transform.GetChild(7);
                if (oldClone != null && oldClone.name.Contains("Products"))
                {
                    GameObject.Destroy(oldClone.gameObject);
                    MelonLogger.Msg("🗑️ Removed 8th icon (index 7)");
                }
            }
            foreach (Transform child in parentCanvas)
            {
                if (child.name.Contains("ProductManagerApp") && child.GetComponent<ProductManagerApp>() == null)
                {
                    GameObject.Destroy(child.gameObject);
                    MelonLogger.Msg("🗑️ Removed dangling ProductManagerApp clone.");
                }
            }
            // 1. Clone an existing app (ProductManagerApp)
            Transform originalApp = parentCanvas.Find("ProductManagerApp");
            if (originalApp == null)
            {
                MelonLogger.Error("❌ ProductManagerApp not found. Cannot clone.");
                yield break;
            }

            // 1. Clone
            GameObject clonedApp = GameObject.Instantiate(originalApp.gameObject, parentCanvas);
            clonedApp.name = "SilkRoadApp";
            clonedApp.SetActive(false);

            // 2. REMOVE the original ProductManagerApp behavior
            var oldApp = clonedApp.GetComponent<ProductManagerApp>();
            if (oldApp != null)
                GameObject.Destroy(oldApp);

            // 2. Clear its UI and inject our own
            Transform container = clonedApp.transform.Find("Container");
            if (container != null)
            {
                foreach (Transform child in container)
                    GameObject.Destroy(child.gameObject);
            }

            // 3. Attach your custom logic and UI
            var silkApp = clonedApp.AddComponent<SilkRoadApp>();
            silkApp.appContainer = container.GetComponent<RectTransform>();

            MelonLogger.Msg("✅ SilkRoad app attached to AppsCanvas.");

            // 4. Add the icon to HomeScreen
            SetupSilkRoadIcon(clonedApp);
        }

        private void SetupSilkRoadIcon(GameObject appGO)
        {
            string iconPath = "SilkRoadIcon.png";
            Sprite iconSprite = LoadImage(iconPath);

            var iconGrid = GameObject.Find("Player_Local/CameraContainer/Camera/OverlayCamera/GameplayMenu/Phone/phone/HomeScreen/AppIcons");
            if (iconGrid == null)
            {
                MelonLogger.Error("❌ AppIcons grid not found.");
                return;
            }

            // Clean up ALL old Silk icons (name and label check)
            foreach (Transform icon in iconGrid.transform)
            {
                var label = icon.Find("Label")?.GetComponent<Text>()?.text;
                if (icon.name == "SilkRoadIcon" || label == "Silk")
                    GameObject.Destroy(icon.gameObject);
            }

            // Clean up Products clones (keep only the first one)
            bool foundFirstProducts = false;
            foreach (Transform icon in iconGrid.transform)
            {
                var label = icon.Find("Label")?.GetComponent<Text>()?.text;
                if (label == "Products")
                {
                    if (!foundFirstProducts)
                    {
                        foundFirstProducts = true;
                        continue;
                    }
                    GameObject.Destroy(icon.gameObject);
                    MelonLogger.Msg("🗑️ Removed extra Products icon.");
                }
            }

            // Clone icon (based on icon index 6 as template)
            Transform existingIcon = iconGrid.transform.GetChild(6);
            GameObject newIcon = GameObject.Instantiate(existingIcon.gameObject, iconGrid.transform);
            newIcon.name = "SilkRoadIcon";

            newIcon.transform.Find("Label").GetComponent<Text>().text = "Silk";
            if (iconSprite != null)
            {
                var img = newIcon.transform.Find("Mask/Image").GetComponent<Image>();
                img.sprite = iconSprite;
            }

            var btn = newIcon.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                // 🔥 Remove *this* button’s GameObject (self-destroy)
                GameObject.Destroy(btn.gameObject);
                MelonLogger.Msg("🗑️ Removed original broken SilkRoad icon (self)");

                // ✅ Open the app
                appGO.SetActive(true);
            });

            MelonLogger.Msg("✅ SilkRoad icon created.");
        }

        public static Sprite LoadImage(string fileName)
        {
            string fullPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), fileName);
            if (!File.Exists(fullPath))
            {
                MelonLogger.Error($"❌ Icon file not found: {fullPath}");
                return null;
            }

            try
            {
                byte[] data = File.ReadAllBytes(fullPath);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(data))
                {
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("❌ Failed to load sprite: " + ex);
            }

            return null;
        }
    }
}
