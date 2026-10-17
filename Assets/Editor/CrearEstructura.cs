using System.IO;
using UnityEditor;
using UnityEngine;

// Menú "Trono > Crear carpetas del proyecto": crea las carpetas acordadas
// con un .gitkeep para que Git las suba aunque estén vacías.
public static class CrearEstructura
{
    static readonly string[] carpetas =
    {
        "Assets/Scripts/Core",
        "Assets/Scripts/UI",
        "Assets/Data",
        "Assets/Prefabs",
        "Assets/Scenes",
        "Assets/Art/Backgrounds",
        "Assets/Art/UI",
        "Assets/Art/Characters",
        "Assets/Art/Crowds",
        "Assets/Audio",
    };

    [MenuItem("Trono/Crear carpetas del proyecto")]
    public static void Crear()
    {
        foreach (string carpeta in carpetas)
        {
            Directory.CreateDirectory(carpeta);
            string gitkeep = Path.Combine(carpeta, ".gitkeep");
            if (!File.Exists(gitkeep)) File.WriteAllText(gitkeep, "");
        }

        AssetDatabase.Refresh();
        Debug.Log("[Trono] Carpetas del proyecto listas.");
    }
}
