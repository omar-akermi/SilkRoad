using System;

namespace ScheduleOneEnhanced.API
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SaveableField : Attribute
    {
        public string fileName { get; }

        public SaveableField(string fileName)
        {
            this.fileName = fileName;
        }
    }
}