using UnityEngine;
using System.IO;

public static class ModuleSaveManager
{
    private static readonly string ModulesFolderPath = Path.Combine(Application.persistentDataPath, "SavedModules");

    public static void SaveModule(string moduleName, string json)
    {
        if (!Directory.Exists(ModulesFolderPath))
            Directory.CreateDirectory(ModulesFolderPath);

        string filePath = Path.Combine(ModulesFolderPath, moduleName + ".json");
        File.WriteAllText(filePath, json);

        Debug.Log($"Module '{moduleName}' saved at: {filePath}");
    }

    public static string LoadModule(string filePath)
    {
        if (File.Exists(filePath))
            return File.ReadAllText(filePath);

        Debug.LogError("Module file not found: " + filePath);
        return null;
    }

    public static string[] GetAllModulePaths()
    {
        if (!Directory.Exists(ModulesFolderPath))
            Directory.CreateDirectory(ModulesFolderPath);

        return Directory.GetFiles(ModulesFolderPath, "*.json");
    }

    public static string GetModulePath(string moduleName)
    {
        if (!Directory.Exists(ModulesFolderPath))
            Directory.CreateDirectory(ModulesFolderPath);

        return Path.Combine(ModulesFolderPath, moduleName + ".json");
    }

    public static bool ModuleExists(string moduleName)
    {
        return File.Exists(GetModulePath(moduleName));
    }
}
