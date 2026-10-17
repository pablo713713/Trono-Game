using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Revisa que cada archivo de la lista exista con el nombre y tamaño acordados.
// Lee el ancho y el alto directo de la cabecera del PNG (no depende de cómo lo importe Unity).
public static class VerificadorArte
{
    [MenuItem("Trono/Dev2/4 Verificar nombres y tamaños de arte")]
    public static void Verificar()
    {
        var faltan = new StringBuilder();
        var tamanos = new StringBuilder();
        int ok = 0;

        foreach (EntradaArte e in ListaArte.Imagenes())
        {
            string ruta = Path.Combine(Application.dataPath, e.carpeta, e.nombre + ".png");
            if (!File.Exists(ruta)) { faltan.AppendLine("  FALTA   " + e.carpeta + "/" + e.nombre + ".png"); continue; }

            if (!LeerTamano(ruta, out int w, out int h)) { tamanos.AppendLine("  ILEGIBLE " + e.nombre + ".png"); continue; }
            if (w != e.ancho || h != e.alto)
                tamanos.AppendLine("  TAMAÑO  " + e.nombre + ".png es " + w + "×" + h + ", debe ser " + e.ancho + "×" + e.alto);
            else ok++;
        }

        foreach (string s in ListaArte.Sonidos)
        {
            bool existe = false;
            foreach (string ext in new[] { ".wav", ".ogg", ".mp3" })
                existe |= File.Exists(Path.Combine(Application.dataPath, "Audio", s + ext));
            if (existe) ok++; else faltan.AppendLine("  FALTA   Audio/" + s + " (.ogg o .wav)");
        }

        bool fuente = File.Exists(Path.Combine(Application.dataPath, ListaArte.Interfaz, "font_pixel.ttf"));
        if (fuente) ok++; else faltan.AppendLine("  FALTA   " + ListaArte.Interfaz + "/font_pixel.ttf");

        string informe = "Archivos correctos: " + ok + "\n" +
            (faltan.Length > 0 ? "\nFaltan:\n" + faltan : "") +
            (tamanos.Length > 0 ? "\nTamaño incorrecto:\n" + tamanos : "");
        if (faltan.Length == 0 && tamanos.Length == 0) informe += "\nTodo en orden.";

        Debug.Log("[Trono] Verificación de arte\n" + informe);
        EditorUtility.DisplayDialog("Verificar arte", informe, "OK");
    }

    static bool LeerTamano(string ruta, out int ancho, out int alto)
    {
        ancho = alto = 0;
        try
        {
            using (var fs = new FileStream(ruta, FileMode.Open, FileAccess.Read))
            {
                var b = new byte[24];
                if (fs.Read(b, 0, 24) < 24 || b[1] != 'P' || b[2] != 'N' || b[3] != 'G') return false;
                ancho = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
                alto = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
                return true;
            }
        }
        catch { return false; }
    }
}
