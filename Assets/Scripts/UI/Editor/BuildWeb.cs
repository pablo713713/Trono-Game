using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// D2-11: build WebGL para itch.io.
// Resultado en Builds/WebGL. Para subir: comprimir el CONTENIDO de esa carpeta en un .zip
// (index.html debe quedar en la raíz del zip) y marcar "This file will be played in the browser".
public static class BuildWeb
{
    public const string Escena = "Assets/Scenes/Main.unity";
    public const string Salida = "Builds/WebGL";

    [MenuItem("Trono/Dev2/5 Build WebGL (itch.io)")]
    public static void Construir()
    {
        if (!File.Exists(Escena))
        {
            EditorUtility.DisplayDialog("Build WebGL", "Falta " + Escena + ". Primero: Trono > Dev2 > 2 Construir escena y prefabs.", "OK");
            return;
        }

        ArteTools.ReconstruirCatalogo();

        // Gzip con "decompression fallback": funciona en itch.io sin configurar cabeceras del servidor.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.defaultWebScreenWidth = 960;
        PlayerSettings.defaultWebScreenHeight = 540;
        PlayerSettings.runInBackground = true;

        Directory.CreateDirectory(Salida);
        var opciones = new BuildPlayerOptions
        {
            scenes = new[] { Escena },
            locationPathName = Salida,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };

        BuildReport reporte = BuildPipeline.BuildPlayer(opciones);
        BuildSummary resumen = reporte.summary;
        if (resumen.result == BuildResult.Succeeded)
        {
            Debug.Log("[Trono] Build WebGL listo: " + Salida + " (" + (resumen.totalSize / 1048576f).ToString("0.0") + " MB). " +
                      "Comprime el contenido de la carpeta en un .zip y súbelo a itch.io.");
            EditorUtility.RevealInFinder(Salida);
        }
        else
        {
            Debug.LogError("[Trono] El build WebGL falló (" + resumen.result + "). ¿Está instalado Web Build Support en Unity Hub?");
        }
    }
}
