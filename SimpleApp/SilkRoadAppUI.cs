using MelonLoader;
using MelonLoader.Utils;
using ScheduleOne.DevUtilities;
using ScheduleOne.Money;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Product;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace SilkRoad
{
    public class SilkRoadAppUI : MonoBehaviour
    {
        private List<QuestData> quests = new List<QuestData>();
        private QuestData selectedQuest;
        private QuestData activeQuest;
        private Coroutine deliveryCoroutine;

        public RectTransform questListContainer;
        public RectTransform questDetailPanel;
        public Button acceptButton;
        public Button mapButton;

        public Text questTitle;
        public Text questTask;
        public Text questReward;
        public Text questHeat;

        public void BuildUI(Transform root)
        {
            MelonLogger.Msg("📱 Building Silk Road UI...");

            GameObject bg = UIFactory.Panel("SilkRoad_Background", root, Color.black, fullAnchor: true);
            MelonLogger.Msg("✅ Main panel created");

            // Top bar
            GameObject topBar = UIFactory.Panel("TopBar", bg.transform, new Color(0.15f, 0.15f, 0.15f), new Vector2(0f, 0.93f), new Vector2(1f, 1f));
            UIFactory.Text("AppTitle", "Silk Road", topBar.transform, 26, TextAnchor.MiddleCenter, FontStyle.Bold);

            // LEFT: Scrollable quest list
            GameObject leftPanel = UIFactory.Panel("QuestListPanel", bg.transform, new Color(0.1f, 0.1f, 0.1f), new Vector2(0f, 0f), new Vector2(0.5f, 0.93f));

            // ScrollView
            GameObject scrollViewGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollViewGO.transform.SetParent(leftPanel.transform, false);
            RectTransform scrollRectRT = scrollViewGO.GetComponent<RectTransform>();
            scrollRectRT.anchorMin = Vector2.zero;
            scrollRectRT.anchorMax = Vector2.one;
            scrollRectRT.offsetMin = Vector2.zero;
            scrollRectRT.offsetMax = Vector2.zero;
            scrollRectRT.anchorMin = Vector2.zero;
            scrollViewGO.GetComponent<RectTransform>().SetAsLastSibling(); // ✅ force it to render on top

            ScrollRect scrollRect = scrollViewGO.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;

            // Viewport
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollViewGO.transform, false);
            RectTransform viewportRT = viewport.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = Vector2.zero;
            viewportRT.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.02f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            scrollRect.viewport = viewportRT;

            // Content container
            GameObject content = new GameObject("QuestListContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRT;
            questListContainer = contentRT;

            // RIGHT: Quest details
            GameObject rightPanel = UIFactory.Panel("QuestDetailPanel", bg.transform, new Color(0.12f, 0.12f, 0.12f), new Vector2(0.5f, 0f), new Vector2(1f, 0.93f));
            VerticalLayoutGroup rightLayout = rightPanel.AddComponent<VerticalLayoutGroup>();
            rightLayout.spacing = 12;
            rightLayout.padding = new RectOffset(10, 10, 10, 10);

            questTitle = UIFactory.Text("QuestTitle", "Select a quest", rightPanel.transform, 22, TextAnchor.UpperLeft, FontStyle.Bold);
            questTask = UIFactory.Text("QuestTask", "Task: --", rightPanel.transform, 18);
            questReward = UIFactory.Text("QuestReward", "Reward: --", rightPanel.transform, 18);
            questHeat = UIFactory.Text("QuestHeat", "Heat: --", rightPanel.transform, 18);

            GameObject acceptGO = UIFactory.Button("AcceptButton", "Accept Delivery", rightPanel.transform, new Color(0.2f, 0.6f, 0.2f));
            acceptButton = acceptGO.GetComponent<Button>();

            GameObject mapGO = UIFactory.Button("MapButton", "Show on Map", rightPanel.transform, new Color(0.2f, 0.5f, 0.8f));
            mapButton = mapGO.GetComponent<Button>();

            MelonLogger.Msg("✅ Silk Road UI finished.");

            LoadQuests();
        }


        public void LoadQuests()
        {
            MelonLogger.Msg("🧠 LoadQuests() called.");

            quests.Clear();

            quests.Add(new QuestData
            {
                Title = "OGKush Delivery",
                Task = "Bring 10x OGKush to the green tent",
                Reward = 1000,
                ProductID = "OGKUSH",
                AmountRequired = 10,
                TargetObjectName = "HardwareStoreBack"
            });

            MelonLogger.Msg($"📦 Loaded {quests.Count} quest(s).");

            RefreshQuestList();
        }

        private void RefreshQuestList()
        {
            MelonLogger.Msg("🔁 Refreshing quest list...");

            // Clear old entries
            foreach (Transform child in questListContainer)
                GameObject.Destroy(child.gameObject);

            // Load icon from disk
            Texture2D iconTex = LoadCustomImage("WeaponsIcon.png");
            Sprite iconSprite = null;

            if (iconTex != null)
            {
                iconSprite = Sprite.Create(iconTex, new Rect(0f, 0f, iconTex.width, iconTex.height), new Vector2(0.5f, 0.5f));
                MelonLogger.Msg("🖼️ Loaded icon from file: WeaponsIcon.png");
            }
            else
            {
                MelonLogger.Warning("⚠️ Could not load WeaponsIcon.png from disk.");
            }

            foreach (var quest in quests)
            {
                MelonLogger.Msg($"🆕 Creating icon for quest: {quest.Title}");

                // Icon container
                GameObject iconGO = new GameObject("QuestIcon_" + quest.Title);
                iconGO.transform.SetParent(questListContainer, false);
                RectTransform rt = iconGO.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(90f, 90f);

                // Background
                Image bg = iconGO.AddComponent<Image>();
                bg.color = new Color(0.15f, 0.15f, 0.15f);
                bg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
                bg.type = Image.Type.Sliced;

                // Add LayoutElement to help vertical layout
                LayoutElement le = iconGO.AddComponent<LayoutElement>();
                le.preferredHeight = 90f;
                le.minHeight = 90f;

                // Actual icon image
                GameObject icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                icon.transform.SetParent(iconGO.transform, false);
                RectTransform iconRT = icon.GetComponent<RectTransform>();
                iconRT.anchorMin = new Vector2(0.15f, 0.15f);
                iconRT.anchorMax = new Vector2(0.85f, 0.85f);
                iconRT.offsetMin = Vector2.zero;
                iconRT.offsetMax = Vector2.zero;

                Image iconImage = icon.GetComponent<Image>();
                iconImage.sprite = iconSprite;
                iconImage.preserveAspect = true;
                iconImage.color = Color.white; // Change to Color.green for debugging if needed

                // Add soft white outline around icon
                Outline outline = icon.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 1f, 1f, 0.15f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);

                // Optional: Add green border to the button container itself for debug
                Outline border = iconGO.AddComponent<Outline>();
                border.effectColor = new Color(0f, 1f, 0f, 0.5f);
                border.effectDistance = new Vector2(2f, -2f);

                // Clickable button
                Button btn = iconGO.AddComponent<Button>();
                btn.targetGraphic = bg; // FIXED: must be the background, not the icon!
                btn.onClick.AddListener(() => OnSelectQuest(quest));
            }

            MelonLogger.Msg($"✅ Displayed {quests.Count} quests using custom shipment icon.");
        }


        private Texture2D LoadCustomImage(string fileName)
        {
            string path = Path.Combine(MelonEnvironment.UserDataDirectory, fileName);
            if (!File.Exists(path))
                return null;

            byte[] arr = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            ImageConversion.LoadImage(tex, arr);
            return tex;
        }




        private void OnSelectQuest(QuestData quest)
        {
            selectedQuest = quest;
            MelonLogger.Msg($"🎯 Selected quest: {quest.Title}");

            questTitle.text = quest.Title;
            questTask.text = "Task: " + quest.Task;
            questReward.text = $"Reward: ${quest.Reward}";
            questHeat.text = "Heat: Low";

            acceptButton.onClick.RemoveAllListeners();
            acceptButton.onClick.AddListener(() => AcceptQuest(quest));

            mapButton.onClick.RemoveAllListeners();
            mapButton.onClick.AddListener(() => ShowQuestTarget(quest));
        }


        private void AcceptQuest(QuestData quest)
        {
            if (activeQuest != null)
            {
                MelonLogger.Warning("⚠️ You already have an active delivery.");
                return;
            }

            activeQuest = quest;
            MelonLogger.Msg($"📦 Accepted quest: {quest.Title}");

            if (deliveryCoroutine != null)
                StopCoroutine(deliveryCoroutine);

            deliveryCoroutine = StartCoroutine(CheckDelivery(quest));
        }

        private void ShowQuestTarget(QuestData quest)
        {
            var target = GameObject.Find(quest.TargetObjectName);
            if (target != null)
            {
                MelonLogger.Msg($"📍 Target location for {quest.Title}: {target.transform.position}");
                // You can add blinking effect or map marker here later
            }
            else
            {
                MelonLogger.Warning("⚠️ Target not found: " + quest.TargetObjectName);
            }
        }

        private Vector3 dropOffLocation = new Vector3(-18.9826f, -4.035f, 173.6407f);

        private IEnumerator CheckDelivery(QuestData quest)
        {
            while (activeQuest != null)
            {
                float distance = Vector3.Distance(PlayerSingleton<PlayerMovement>.Instance.transform.position, dropOffLocation);
                if (distance < 4f)
                {
                    uint count = PlayerSingleton<PlayerInventory>.Instance.GetAmountOfItem(quest.ProductID);
                    if (count >= quest.AmountRequired)
                    {
                        PlayerSingleton<PlayerInventory>.Instance.RemoveAmountOfItem(quest.ProductID, quest.AmountRequired);
                        NetworkSingleton<MoneyManager>.Instance.ChangeCashBalance(quest.Reward, true, true);
                        MelonLogger.Msg($"✅ Delivered {quest.AmountRequired}x {quest.ProductID} — earned ${quest.Reward}!");

                        activeQuest = null;
                        yield break;
                    }
                }

                yield return new WaitForSeconds(1.5f);
            }
        }

    }
}
