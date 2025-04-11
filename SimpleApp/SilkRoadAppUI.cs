using UnityEngine;
using UnityEngine.UI;

namespace SilkRoad
{
    public class SilkRoadAppUI : MonoBehaviour
    {
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
            MelonLoader.MelonLogger.Msg("📱 Building Silk Road UI...");

            // Main background (black fullscreen)
            GameObject bg = UIFactory.Panel("SilkRoad_Background", root, Color.black, fullAnchor: true);
            MelonLoader.MelonLogger.Msg("✅ Main panel created");

            // Top bar
            GameObject topBar = UIFactory.Panel("TopBar", bg.transform, new Color(0.15f, 0.15f, 0.15f), new Vector2(0f, 0.93f), new Vector2(1f, 1f));
            UIFactory.Text("AppTitle", "Silk Road", topBar.transform, 26, TextAnchor.MiddleCenter, FontStyle.Bold);

            // LEFT PANEL: Quest list
            GameObject leftPanel = UIFactory.Panel("QuestListPanel", bg.transform, new Color(0.1f, 0.1f, 0.1f), new Vector2(0f, 0f), new Vector2(0.5f, 0.93f));
            VerticalLayoutGroup leftLayout = leftPanel.AddComponent<VerticalLayoutGroup>();
            leftLayout.spacing = 10;
            leftLayout.padding = new RectOffset(10, 10, 10, 10);
            leftLayout.childControlHeight = true;
            leftLayout.childForceExpandHeight = false;

            UIFactory.Text("Placeholder1", "• No quests yet", leftPanel.transform, 18);

            // Save container reference for future scroll view
            questListContainer = leftPanel.GetComponent<RectTransform>();

            // RIGHT PANEL: Quest detail
            GameObject rightPanel = UIFactory.Panel("QuestDetailPanel", bg.transform, new Color(0.12f, 0.12f, 0.12f), new Vector2(0.5f, 0f), new Vector2(1f, 0.93f));
            VerticalLayoutGroup rightLayout = rightPanel.AddComponent<VerticalLayoutGroup>();
            rightLayout.spacing = 12;
            rightLayout.padding = new RectOffset(10, 10, 10, 10);

            questTitle = UIFactory.Text("QuestTitle", "Select a quest", rightPanel.transform, 22, TextAnchor.UpperLeft, FontStyle.Bold);
            questTask = UIFactory.Text("QuestTask", "Task: --", rightPanel.transform, 18);
            questReward = UIFactory.Text("QuestReward", "Reward: --", rightPanel.transform, 18);
            questHeat = UIFactory.Text("QuestHeat", "Heat: --", rightPanel.transform, 18);

            // Accept Button
            GameObject acceptGO = UIFactory.Button("AcceptButton", "Accept Delivery", rightPanel.transform, new Color(0.2f, 0.6f, 0.2f));
            acceptButton = acceptGO.GetComponent<Button>();

            // Show on Map Button
            GameObject mapGO = UIFactory.Button("MapButton", "Show on Map", rightPanel.transform, new Color(0.2f, 0.5f, 0.8f));
            mapButton = mapGO.GetComponent<Button>();

            MelonLoader.MelonLogger.Msg("✅ Silk Road UI finished.");
        }
    }
}
