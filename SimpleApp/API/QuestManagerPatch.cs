using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Quests;

namespace ScheduleOneEnhanced.API.Quests
{
    [HarmonyPatch]
    public class QuestManagerPatch
    {
        private static readonly List<string> _writtenQuests = new List<string>();
        
        [HarmonyPatch(typeof(QuestManager), "WriteData")]
        [HarmonyPostfix]
        public static void WriteData(QuestManager __instance, string parentFolderPath, ref List<string> __result)
        {
            string questFolder = Path.Combine(parentFolderPath, "Quests");
            _writtenQuests.Clear();
            foreach (Quest quest in Quest.Quests)
            {
                _writtenQuests.Add(quest.SaveFolderName);

                if (!(quest is SOEQuest soeQuest))
                    continue;
                
                string path = Path.Combine(questFolder, soeQuest.SaveFolderName);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                WriteData(path, "SOEQuest", soeQuest.soeQuestData.GetJson());
                
                var derivedType = soeQuest.GetType();
                FieldInfo[] fields = derivedType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (FieldInfo field in fields)
                {
                    SaveableField saveableFieldAttribute = field.GetCustomAttribute<SaveableField>();
                    if (saveableFieldAttribute == null)
                        continue;
                    
                    WriteData(path, saveableFieldAttribute.fileName, ((SaveData)field.GetValue(soeQuest)).GetJson());
                }
            }
        }
        
        public static void WriteData(string path, string fileName, string json)
        {
            string saveFileName = fileName.EndsWith(".json")
                ? fileName
                : $"{fileName}.json";
                
            try
            {
                string filePath = Path.Combine(path, saveFileName);
                File.WriteAllText(filePath, json);
            }
            catch (Exception e)
            {
                MelonLogger.Msg(e);
            }
        }

        [HarmonyPatch(typeof(QuestManager), "DeleteUnapprovedFiles")]
        [HarmonyPostfix]
        public static void DeleteUnapprovedFiles(QuestManager __instance, string parentFolderPath)
        {
            string questFolder = Path.Combine(parentFolderPath, "Quests");
            
            string[] unapprovedQuestDirectories = Directory.GetDirectories(questFolder)
                .Where(directory => directory.StartsWith("Quest_") && !_writtenQuests.Contains(directory))
                .ToArray();
            
            foreach (string unapprovedQuestDirectory in unapprovedQuestDirectories)
                Directory.Delete(unapprovedQuestDirectory, true);
        }
    }
}