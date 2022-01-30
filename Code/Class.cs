using System;
using System.Reflection;

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