using System;
using System.Reflection;

[Obsolete("System of class enums is outdated")]
/**<summary>This is enum that contains every class that is saved/loaded from the save file<para/>The whole point is to avoid saving full name in every save block</summary>*/
public enum Class
{
    /**<summary>If no class was provided</summary>*/
    Default,
    Customer,
    Waiter,
    Cook,
    Janitor,
    Fridge,
    Furnace,
    Table
}

/**<summary>Class containing function that allows searching for type while ignoring the namespace
 * Static class existing purely for one static function, thanks c# very cool</summary>*/
public static class TypeSearch
{
    /**<summary>llows searching for type while ignoring the namespace</summary>*/
    public static Type GetTypeByName(string name)
    {

        foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] assemblyTypes = a.GetTypes();
            for (int j = 0; j < assemblyTypes.Length; j++)
            {
                if (assemblyTypes[j].Name == name)
                {
                    return assemblyTypes[j];
                }
            }
        }
        
        return null;
    }
}