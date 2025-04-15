using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ScheduleOneEnhanced.API.Utils
{
    public static class ReflectionUtils
    {
        public static List<Type> GetDerivedClasses<TBaseClass>()
        {
            List<Type> derivedClasses = new List<Type>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
                derivedClasses.AddRange(assembly.GetTypes()
                    .Where(type => typeof(TBaseClass).IsAssignableFrom(type) 
                       && type != typeof(TBaseClass) 
                       && !type.IsAbstract)
                );
            
            return derivedClasses;
        }

        public static Type? GetTypeByName(string typeName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                Type? foundType = assembly.GetTypes().FirstOrDefault(type => type.Name == typeName);
                if (foundType == null)
                    continue;
                
                return foundType;
            }

            return null;
        }
    }
}