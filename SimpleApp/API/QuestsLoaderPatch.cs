using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Persistence.Loaders;
using ScheduleOneEnhanced.API.Utils;
using UnityEngine;

namespace ScheduleOneEnhanced.API.Quests
{
    [HarmonyPatch]
    public class QuestsLoaderPatch
    {
        [HarmonyPatch(typeof(QuestsLoader), "Load")]
        [HarmonyPrefix]
        public static void QuestsLoaderLoad(QuestsLoader __instance, string mainPath)
        {
            MelonLogger.Msg("📥 QuestsLoader.Load called");

            string[] questDirectories = Directory.GetDirectories(mainPath)
                .Select(Path.GetFileName)
                .Where(directory => directory.StartsWith("Quest_"))
                .ToArray();

            foreach (string questDirectory in questDirectories)
            {
                string baseQuestPath = Path.Combine(mainPath, questDirectory);
                __instance.TryLoadFile(baseQuestPath, out string questDataText);
                if (questDataText == null)
                    continue;
                
                QuestData questData = JsonUtility.FromJson<QuestData>(questDataText);
                
                string questDirectoryPath = Path.Combine(mainPath, questDirectory);
                string soeQuestPath = Path.Combine(questDirectoryPath, "SOEQuest");
                if (!__instance.TryLoadFile(soeQuestPath, out string soeQuestText))
                    continue;

                SOEQuestData soeQuestData = JsonUtility.FromJson<SOEQuestData>(soeQuestText);
                if (soeQuestData?.questType == null)
                    continue;

                Type? questType = ReflectionUtils.GetTypeByName(soeQuestData.questType);
                if (questType == null || !typeof(SOEQuest).IsAssignableFrom(questType))
                    continue;
                
                GameObject questObject = new GameObject(questType.Name);
                questObject.SetActive(false);
                SOEQuest quest = (SOEQuest)questObject.AddComponent(questType);
                quest.StaticGUID = questData.GUID;
                
                FieldInfo[] fields = questType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (FieldInfo field in fields)
                {
                    SaveableField saveableFieldAttribute = field.GetCustomAttribute<SaveableField>();
                    if (saveableFieldAttribute == null)
                        continue;
                    
                    string saveablePath = Path.Combine(questDirectoryPath, saveableFieldAttribute.fileName);
                    if (!__instance.TryLoadFile(saveablePath, out string saveableText))
                        continue;
                    
                    field.SetValue(quest, JsonUtility.FromJson(saveableText, field.FieldType));
                }
                MelonLogger.Msg($"📥 Loading quest from save: {soeQuestData.questType}");

                questObject.SetActive(true);
            }
        }
    }
}