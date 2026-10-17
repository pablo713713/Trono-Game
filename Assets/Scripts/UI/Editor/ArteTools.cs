using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Mantiene el ArteCatalogo (Assets/Prefabs/Resources/ArteCatalogo.asset) al día.
// Se reconstruye solo cada vez que cambia algo en Assets/Art o Assets/Audio,
// así los diseñadores reemplazan archivos sin que nadie tenga que reenlazar nada.
public static class ArteTools
{
    const string RutaCatalogo = "Assets/Prefabs/Resources/ArteCatalogo.asset";

    [MenuItem("Trono/Dev2/3 Reconstruir catálogo de arte")]
    public static void ReconstruirCatalogo()
    {
        if (!Directory.Exists("Assets/Prefabs/Resources")) Directory.CreateDirectory("Assets/Prefabs/Resources");

        ArteCatalogo c = AssetDatabase.LoadAssetAtPath<ArteCatalogo>(RutaCatalogo);
        bool nuevo = c == null;
        if (nuevo)
        {
            c = ScriptableObject.CreateInstance<ArteCatalogo>();
            AssetDatabase.CreateAsset(c, RutaCatalogo);
        }

        var sprites = new List<Sprite>();
        if (Directory.Exists("Assets/Art"))
            foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Art" }))
                sprites.AddRange(AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)).OfType<Sprite>()
                    .Where(s => !s.name.Contains("_frame")));

        var sonidos = new List<AudioClip>();
        if (Directory.Exists("Assets/Audio"))
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" }))
                sonidos.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid)));

        Font fuente = null;
        if (Directory.Exists("Assets/Art"))
            foreach (string guid in AssetDatabase.FindAssets("font_pixel t:Font", new[] { "Assets/Art" }))
                fuente = AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(guid));

        sprites = sprites.Where(s => s != null).OrderBy(s => s.name).ToList();
        sonidos = sonidos.Where(s => s != null).OrderBy(s => s.name).ToList();

        // Solo se escribe si algo cambió, para no ensuciar el commit.
        bool igual = !nuevo && c.fuente == fuente
            && c.sprites.SequenceEqual(sprites) && c.sonidos.SequenceEqual(sonidos);
        if (igual) return;

        c.sprites = sprites;
        c.sonidos = sonidos;
        c.fuente = fuente;
        c.Invalidar();
        EditorUtility.SetDirty(c);
        AssetDatabase.SaveAssets();
        Debug.Log("[Trono] Catálogo de arte actualizado: " + sprites.Count + " sprites, " + sonidos.Count + " sonidos" +
                  (fuente != null ? ", font_pixel lista." : ", sin font_pixel todavía."));
    }
}

public class CatalogoAutomatico : AssetPostprocessor
{
    static bool pendiente;

    static void OnPostprocessAllAssets(string[] importados, string[] borrados, string[] movidos, string[] movidosDesde)
    {
        if (!Relevante(importados) && !Relevante(borrados) && !Relevante(movidos) && !Relevante(movidosDesde)) return;
        if (pendiente) return;
        pendiente = true;
        EditorApplication.delayCall += () =>
        {
            pendiente = false;
            if (File.Exists("Assets/Prefabs/Resources/ArteCatalogo.asset")) ArteTools.ReconstruirCatalogo();
        };
    }

    static bool Relevante(string[] rutas)
    {
        foreach (string r in rutas)
            if (r.StartsWith("Assets/Art/") || r.StartsWith("Assets/Audio/")) return true;
        return false;
    }
}
