using UnityEngine;
using UnityEngine.UI;
using ScheduleOne.UI.Phone;
using ScheduleOne.UI.Phone.ProductManagerApp;
using MelonLoader;
using System.IO;
using ScheduleOne.DevUtilities;

namespace SilkRoad
{
    public static class AppCloner
    {
        public static GameObject CreateApp(string newName, out RectTransform container)
        {
            var appsCanvas = PlayerSingleton<AppsCanvas>.Instance.canvas.transform;
            var template = appsCanvas.Find("ProductManagerApp");

            if (template == null)
            {
                MelonLogger.Error("❌ ProductManagerApp template not found.");
                container = null;
                return null;
            }

            var clone = Object.Instantiate(template.gameObject, appsCanvas);
            clone.name = newName;
            clone.SetActive(false);

            var oldApp = clone.GetComponent<ProductManagerApp>();
            if (oldApp != null)
                Object.Destroy(oldApp);

            container = clone.transform.Find("Container")?.GetComponent<RectTransform>();
            if (container != null)
            {
                foreach (Transform child in container)
                    Object.Destroy(child.gameObject);
            }

            return clone;
        }

        public static void CreateAppIcon(string iconName, string labelText, string imagePath, GameObject appGO)
        {
            var iconGrid = GameObject.Find("Player_Local/CameraContainer/Camera/OverlayCamera/GameplayMenu/Phone/phone/HomeScreen/AppIcons");
            if (iconGrid == null)
            {
                MelonLogger.Error("❌ AppIcons grid not found.");
                return;
            }

            foreach (Transform icon in iconGrid.transform)
            {
                if (icon.name == iconName)
                    Object.Destroy(icon.gameObject);
            }

            var cloneSource = iconGrid.transform.GetChild(6);
            var newIcon = Object.Instantiate(cloneSource.gameObject, iconGrid.transform);
            newIcon.name = iconName;

            newIcon.transform.Find("Label").GetComponent<Text>().text = labelText;

            var iconSprite = LoadImage(imagePath);
            if (iconSprite != null)
            {
                newIcon.transform.Find("Mask/Image").GetComponent<Image>().sprite = iconSprite;
            }

            var btn = newIcon.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                RemoveDuplicateIcon(labelText, iconGrid);
                appGO.SetActive(true);
            });

            MelonLogger.Msg($"✅ {iconName} icon created.");
        }

        public static void DestroyAllWithName(string name, System.Type mustHaveType = null)
        {
            var parent = PlayerSingleton<AppsCanvas>.Instance.canvas.transform;
            foreach (Transform child in parent)
            {
                if (child.name == name && (mustHaveType == null || child.GetComponent(mustHaveType) == null))
                {
                    Object.Destroy(child.gameObject);
                }
            }
        }

        public static void RemoveIconWithLabel(string label)
        {
            var iconGrid = GameObject.Find("Player_Local/CameraContainer/Camera/OverlayCamera/GameplayMenu/Phone/phone/HomeScreen/AppIcons");
            if (iconGrid == null) return;

            foreach (Transform icon in iconGrid.transform)
            {
                if (icon.name == "AppIcon(Clone)")
                {
                    var text = icon.transform.Find("Label")?.GetComponent<Text>()?.text;
                    if (text == label)
                    {
                        Object.Destroy(icon.gameObject);
                        MelonLogger.Msg($"🗑️ Removed icon with label '{label}'");
                        break;
                    }
                }
            }
        }

        private static void RemoveDuplicateIcon(string labelText, GameObject iconGrid)
        {
            foreach (Transform icon in iconGrid.transform)
            {
                if (icon.name == "AppIcon(Clone)")
                {
                    var label = icon.transform.Find("Label")?.GetComponent<Text>()?.text;
                    if (label == labelText)
                    {
                        Object.Destroy(icon.gameObject);
                        MelonLogger.Msg($"🗑️ Removed duplicate icon '{labelText}'");
                        return;
                    }
                }
            }
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
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("❌ Failed to load sprite: " + ex);
            }

            return null;
        }
    }
}