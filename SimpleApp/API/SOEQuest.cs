using System;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using ScheduleOne.DevUtilities;
using ScheduleOne.Map;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Quests;
using ScheduleOne.UI.Phone.ContactsApp;
using SilkRoad;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScheduleOneEnhanced.API.Quests
{
    public class SOEQuest : Quest
    {
        [SaveableField("SOEQuest")]
        public SOEQuestData soeQuestData;
        
        private void InitializeIcon() => IconPrefab = CreateIconPrefab().GetComponent<RectTransform>();
        private void InitializePoI() => PoIPrefab = CreatePoIPrefab();

        public SOEQuest()
        {
            onActiveState = new UnityEvent();
            onComplete = new UnityEvent();
            onInitialComplete = new UnityEvent();
            onQuestBegin = new UnityEvent();
            onQuestEnd = new UnityEvent<EQuestState>();
            onTrackChange = new UnityEvent<bool>();
            TrackOnBegin = true;
            AutoCompleteOnAllEntriesComplete = true;
            autoInitialize = false;
            this.Expires = true;

        }

        private GameObject CreateIconPrefab()
        {
             GameObject icon = new GameObject("IconPrefab", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
             icon.transform.SetParent(transform);
             Image img = icon.GetComponent<Image>();
             var iconn = Plugin.LoadImage("SilkRoadIcon.png");
             img.sprite = iconn;
             return icon;
        }

        private GameObject CreatePoIPrefab()
        {
            GameObject poiPrefabObject = new GameObject("POIPrefab");
            poiPrefabObject.SetActive(false);
            poiPrefabObject.transform.SetParent(transform);
            POI poi = poiPrefabObject.AddComponent<POI>();
            poi.DefaultMainText = "Did it work?";
            FieldInfo uiPrefabField = AccessTools.Field(typeof(POI), "UIPrefab");
            uiPrefabField.SetValue(poi, CreatePoIUIPrefab());
            return poiPrefabObject;
        }

        private GameObject CreatePoIUIPrefab()
        {
            GameObject uiPrefabObject = new GameObject("PoIUIPrefab", 
                typeof(RectTransform), 
                typeof(CanvasRenderer), 
                typeof(EventTrigger),
                typeof(Button)
            );
            uiPrefabObject.transform.SetParent(transform);
            
            GameObject labelObject = new GameObject("MainLabel", 
                typeof(RectTransform), 
                typeof(CanvasRenderer), 
                typeof(Text)
            );
            labelObject.transform.SetParent(uiPrefabObject.transform);
            
            GameObject iconContainerObject = new GameObject("IconContainer", 
                typeof(RectTransform), 
                typeof(CanvasRenderer), 
                typeof(Image)
            );
            iconContainerObject.transform.SetParent(uiPrefabObject.transform);
            Image iconImage = iconContainerObject.GetComponent<Image>();
            var iconn = Plugin.LoadImage("Stash.png");
            iconImage.sprite = iconn;
            RectTransform iconRectTransform = iconImage.GetComponent<RectTransform>();
            iconRectTransform.sizeDelta = new Vector2(30, 30);
            
            return uiPrefabObject;
        }

        public void AddEntry()
        {
            GameObject questEntryObject = new GameObject("QuestEntry");
            questEntryObject.SetActive(false);
            questEntryObject.transform.SetParent(transform);
            QuestEntry questEntry = questEntryObject.AddComponent<QuestEntry>();
            questEntry.PoILocation = questEntryObject.transform;
            questEntryObject.SetActive(true);
            Entries.Add(questEntry);
        }

        public override void Awake()
        {
            soeQuestData = new SOEQuestData()
            {
                questType = GetType().Name
            };
            
            InitializeIcon();
            InitializePoI();
            
            base.Awake();
            InitializeQuest(Title, Description, Array.Empty<QuestEntryData>(), StaticGUID);
        }

        public override void Start()
        {
            base.Start();
            Begin();
        }
        
        public override void End()
        {
            base.End();
            Quests.Remove(this);
        }
    }
}