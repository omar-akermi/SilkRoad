using MelonLoader;
using MelonLoader.Utils;
using ScheduleOne.DevUtilities;
using ScheduleOne.Money;
using ScheduleOne.NPCs;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Product;
using ScheduleOne.Quests;
using ScheduleOne.UI.Phone;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public Text questTitle;
        public Text questTask;
        public Text questReward;
        private static QuestData lastActiveQuest;
        public Text deliveryStatus; // declare at top

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
            deliveryStatus = UIFactory.Text("DeliveryStatus", "", rightPanel.transform, 16, TextAnchor.UpperLeft);

            GameObject acceptGO = UIFactory.Button("AcceptButton", "Accept Delivery", rightPanel.transform, new Color(0.2f, 0.6f, 0.2f));
            acceptButton = acceptGO.GetComponent<Button>();



            MelonLogger.Msg("✅ Silk Road UI finished.");

            LoadQuests();
        }


        public void LoadQuests()
        {
            MelonLogger.Msg("🧠 LoadQuests() with 4 random products");

            quests.Clear();
            System.Random rng = new System.Random();

            // Shuffle and pick only 4 valid products
            var shuffled = ProductManager.Instance.AllProducts
                .Where(def => def != null && !string.IsNullOrWhiteSpace(def.name) && def.Price > 0f)
                .OrderBy(_ => rng.Next())
                .Take(4)
                .ToList();

            foreach (var def in shuffled)
            {
                int bricks = rng.Next(1, 10); // Between 1 and 9
                int reward = Mathf.RoundToInt(def.Price * 25f * bricks) + 10000;

                quests.Add(new QuestData
                {
                    Title = $"{def.Name} Delivery",
                    Task = $"Deliver {bricks}x {def.Name} Bricks to the stash.",
                    Reward = reward,
                    ProductID = def.name,
                    AmountRequired = (uint)bricks,
                    TargetObjectName = "GreenTent"
                });
            }

            MelonLogger.Msg($"📦 Generated {quests.Count} randomized quests.");
            RefreshQuestList();
        }


        public Sprite GetProductIcon(string productName)
        {
            foreach (var product in ProductManager.Instance.AllProducts)
            {
                if (product == null || !product.name.Equals(productName, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Try to dynamically generate icons
                Sprite generated = ProductIconManager.Instance.GenerateIcons(product.name);
                if (generated != null)
                {
                    MelonLogger.Msg($"🛠️ Generated icon for {product.name}.");
                    return generated;
                }

                // Fallback: Try ValidPackaging
                if (product.ValidPackaging != null)
                {
                    foreach (var packaging in product.ValidPackaging)
                    {
                        if (packaging == null) continue;

                        string packagingID = packaging.name;

                        Sprite icon = ProductIconManager.Instance.GetIcon(product.name, packagingID, true);
                        if (icon != null)
                        {
                            MelonLogger.Msg($"✅ Found icon for {product.name} with packaging '{packagingID}'");
                            return icon;
                        }
                        else
                        {
                            MelonLogger.Warning($"⚠️ No icon for {product.name} with packaging '{packagingID}'");
                        }
                    }
                }

                MelonLogger.Warning($"⚠️ Product '{product.name}' has no valid icon or packaging.");
            }

            MelonLogger.Error($"❌ Product not found or no valid icon for: {productName}");
            return null;
        }


        private void RefreshQuestList()
        {
            MelonLogger.Msg("🔁 Refreshing quest list...");

            foreach (Transform child in questListContainer)
                GameObject.Destroy(child.gameObject);

            foreach (var quest in quests)
            {
                MelonLogger.Msg($"🆕 Creating icon for quest: {quest.Title}");

                ProductDefinition product = null;

                foreach (var def in ProductManager.Instance.AllProducts)
                {
                    if (def != null && def.name.Equals(quest.ProductID, StringComparison.OrdinalIgnoreCase))
                    {
                        product = def;
                        break;
                    }
                }

                if (product == null)
                {
                    MelonLogger.Warning($"❌ Product not found: {quest.ProductID}");
                    continue;
                }

                Sprite iconSprite = product.Icon;

                if (iconSprite == null)
                {
                    MelonLogger.Warning($"⚠️ No icon found for product {product.name}");
                    continue;
                }

                // Create outer container with horizontal layout
                GameObject row = new GameObject("QuestRow_" + quest.Title, typeof(RectTransform));
                row.transform.SetParent(questListContainer, false);
                RectTransform rowRT = row.GetComponent<RectTransform>();
                rowRT.sizeDelta = new Vector2(400f, 90f);

                HorizontalLayoutGroup hLayout = row.AddComponent<HorizontalLayoutGroup>();
                hLayout.spacing = 10;
                hLayout.padding = new RectOffset(50, 10, 5, 5); // ⬅️ 25px left padding
                hLayout.childAlignment = TextAnchor.MiddleLeft;
                hLayout.childForceExpandHeight = false;
                hLayout.childForceExpandWidth = false;

                LayoutElement rowLE = row.AddComponent<LayoutElement>();
                rowLE.preferredHeight = 90f;
                rowLE.minHeight = 90f;

                // ICON (your original code unchanged)
                GameObject iconGO = new GameObject("QuestIcon_" + quest.Title);
                iconGO.transform.SetParent(row.transform, false);
                RectTransform rt = iconGO.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(90f, 90f);

                Image bg = iconGO.AddComponent<Image>();
                bg.color = new Color(0.15f, 0.15f, 0.15f);
                bg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
                bg.type = Image.Type.Sliced;

                LayoutElement le = iconGO.AddComponent<LayoutElement>();
                le.preferredHeight = 90f;
                le.minHeight = 90f;
                le.preferredWidth = 90f;

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
                iconImage.color = Color.white;

                Outline outline = icon.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 1f, 1f, 0.15f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);

                Button btn = iconGO.AddComponent<Button>();
                btn.targetGraphic = bg;
                btn.onClick.AddListener(() => OnSelectQuest(quest));

                // TEXT panel
                GameObject textPanel = new GameObject("QuestText", typeof(RectTransform));
                textPanel.transform.SetParent(row.transform, false);
                RectTransform textRT = textPanel.GetComponent<RectTransform>();
                textRT.sizeDelta = new Vector2(280f, 90f);

                VerticalLayoutGroup vLayout = textPanel.AddComponent<VerticalLayoutGroup>();
                vLayout.spacing = 4;
                vLayout.childAlignment = TextAnchor.MiddleLeft;
                vLayout.childControlHeight = true;
                vLayout.childControlWidth = true;
                vLayout.childForceExpandWidth = false;

                LayoutElement textLE = textPanel.AddComponent<LayoutElement>();
                textLE.minWidth = 200f;
                textLE.flexibleWidth = 1;

                // Quest Title
                Text titleText = UIFactory.Text("QuestTitle", quest.Title, textPanel.transform, 16, TextAnchor.MiddleLeft, FontStyle.Bold);

                // Mafia Label
                string mafiaLabel = "Client: Unknown";
                if (product is WeedDefinition) mafiaLabel = "Client: German Mafia";
                else if (product is CocaineDefinition) mafiaLabel = "Client: Canadian Mafia";
                else if (product is MethDefinition) mafiaLabel = "Client: Russian Mafia";

                Text clientText = UIFactory.Text("QuestClient", mafiaLabel, textPanel.transform, 14, TextAnchor.UpperLeft);
            }
            if (lastActiveQuest != null)
            {
                OnSelectQuest(lastActiveQuest);
                UpdateUIStateForAcceptedQuest(); // this will also disable the button
            }

            MelonLogger.Msg($"✅ Displayed {quests.Count} quests using in-game product icons.");
        }


        private void UpdateUIStateForAcceptedQuest()
        {
            if (deliveryStatus != null)
                deliveryStatus.text = "🚚 Delivery Active";

            if (acceptButton != null)
                acceptButton.interactable = false;
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

            // Update Accept button text
            acceptButton.GetComponentInChildren<Text>().text = "Accept Delivery";

            acceptButton.onClick.RemoveAllListeners();

            // ⛔ Prevent accepting new delivery if one is active
            if (activeQuest != null)
            {
                acceptButton.interactable = false;
                MelonLogger.Msg("⛔ Accept disabled - active quest already running");
            }
            else
            {
                acceptButton.interactable = true;
                acceptButton.onClick.AddListener(() => AcceptQuest(quest));
            }

        }





        private void AcceptQuest(QuestData quest)
        {
            MelonLogger.Msg($"🚀 AcceptQuest called for: {quest.Title}");

            if (activeQuest != null)
            {
                MelonLogger.Warning("⚠️ You already have an active delivery.");
                return;
            }

            ProductDefinition product = ProductManager.Instance.AllProducts
                .FirstOrDefault(def => def != null && def.name.Equals(quest.ProductID, StringComparison.OrdinalIgnoreCase));

            if (product == null)
            {
                MelonLogger.Warning($"❌ Cannot start quest — product not found: {quest.ProductID}");
                return;
            }

            // Spawn quest GameObject
            GameObject questGO = new GameObject("DeliveryQuest_" + quest.ProductID);
            QuestDelivery questDelivery = questGO.AddComponent<QuestDelivery>();
            questDelivery.Init(product, (int)quest.AmountRequired, quest.Reward);

            activeQuest = quest;
            lastActiveQuest = quest; // 💾 persist across UI reloads

            // ✅ Update UI to reflect active quest
            acceptButton.interactable = false;
            acceptButton.GetComponentInChildren<Text>().text = "Delivery Active";


            // ✅ Hook on completion
            questDelivery.onComplete.AddListener(() =>
            {
                MelonLogger.Msg("✅ Quest marked as complete. Resetting activeQuest.");
                activeQuest = null;
                GameObject.Destroy(questGO);

                // ✅ Reset UI
                acceptButton.interactable = true;
                acceptButton.GetComponentInChildren<Text>().text = "Accept Delivery";
            });
            acceptButton.interactable = false;

            // ✅ Send message from Blackmarket Buyer NPC
            var npc = NPCManager.NPCRegistry.Find(n => n.ID == "npc_blackmarket_buyer");
            if (npc != null)
            {
                MelonLogger.Msg("📱 Found Blackmarket Buyer NPC, sending quest text...");
                npc.SendTextMessage($"Yo, I’m expecting {quest.AmountRequired}x bricks of {product.Name}. Drop it off at the usual stash. Reward: ${quest.Reward}.");
            }
            else
            {
                MelonLogger.Warning("⚠️ Blackmarket Buyer NPC not found. No message sent.");
            }

            MelonLogger.Msg($"📦 Delivery quest started: {quest.Title}");
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
