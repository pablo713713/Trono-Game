using System.IO;
using UnityEditor;
using UnityEngine;

// D2-01: crea los placeholders (rectángulos de color) con los nombres y tamaños exactos de la lista.
// NUNCA pisa un archivo que ya exista: si un diseñador ya subió el arte final, se respeta.
public static class GeneradorPlaceholders
{
    [MenuItem("Trono/Dev2/1 Generar placeholders (no pisa arte existente)")]
    public static void Generar()
    {
        int creados = 0;

        foreach (EntradaArte e in ListaArte.Imagenes())
        {
            string carpeta = Path.Combine(Application.dataPath, e.carpeta);
            string ruta = Path.Combine(carpeta, e.nombre + ".png");
            if (File.Exists(ruta)) continue;

            Directory.CreateDirectory(carpeta);
            Texture2D t = Dibujar(e);
            File.WriteAllBytes(ruta, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            creados++;
        }

        string audio = Path.Combine(Application.dataPath, "Audio");
        Directory.CreateDirectory(audio);
        foreach (string nombre in ListaArte.Sonidos)
        {
            if (ExisteSonido(audio, nombre)) continue;
            EscribirWav(Path.Combine(audio, nombre + ".wav"), SonidoDe(nombre));
            creados++;
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ArteTools.ReconstruirCatalogo();
        Debug.Log("[Trono] Placeholders creados: " + creados + ". Faltan font_pixel.ttf (la entrega el Diseñador 1).");
    }

    static bool ExisteSonido(string carpeta, string nombre)
    {
        foreach (string ext in new[] { ".wav", ".ogg", ".mp3" })
            if (File.Exists(Path.Combine(carpeta, nombre + ext))) return true;
        return false;
    }

    // ---------------- imágenes ----------------

    static Texture2D Dibujar(EntradaArte e)
    {
        var t = new Texture2D(e.ancho, e.alto, TextureFormat.RGBA32, false);
        Rellenar(t, 0, 0, e.ancho, e.alto, Color.clear);
        string n = e.nombre;

        if (n == "bg_throne")
        {
            Rellenar(t, 0, 0, 320, 180, C(0.16f, 0.10f, 0.13f));
            Rellenar(t, 0, 0, 320, 50, C(0.22f, 0.15f, 0.16f));        // suelo
            Rellenar(t, 140, 70, 40, 80, C(0.07f, 0.05f, 0.06f));      // puerta al fondo
            Rellenar(t, 40, 60, 12, 100, C(0.28f, 0.2f, 0.2f));        // columnas
            Rellenar(t, 268, 60, 12, 100, C(0.28f, 0.2f, 0.2f));
            Borde(t, C(0.4f, 0.3f, 0.2f));
        }
        else if (n.StartsWith("bg_balcony"))
        {
            Color cielo = n.EndsWith("farm") ? C(0.35f, 0.5f, 0.3f) : n.EndsWith("armory") ? C(0.3f, 0.35f, 0.45f) : C(0.4f, 0.3f, 0.5f);
            Rellenar(t, 0, 0, 320, 180, cielo);
            Rellenar(t, 0, 0, 320, 90, Oscuro(cielo, 0.7f));            // franja inferior libre para la multitud
            Rellenar(t, 0, 88, 320, 4, C(0.2f, 0.15f, 0.1f));           // baranda
            Borde(t, Color.white);
        }
        else if (n == "bg_vault")
        {
            Rellenar(t, 0, 0, 320, 180, C(0.14f, 0.1f, 0.07f));
            Rellenar(t, 0, 0, 320, 30, C(0.2f, 0.14f, 0.09f));
            Borde(t, C(0.5f, 0.4f, 0.1f));
        }
        else if (n.StartsWith("gold_"))
        {
            int nivel = n[n.Length - 1] - '0';
            for (int p = 0; p <= nivel; p++)                            // más pilas y más altas a mayor nivel
                Rellenar(t, 8 + p * 30, 2, 24, 8 + (nivel + 1) * 10 - (p % 2) * 6, C(0.95f, 0.78f, 0.15f));
            Rellenar(t, 0, 0, 160, 2, C(0.5f, 0.35f, 0.1f));
        }
        else if (n == "ui_scroll")
        {
            Rellenar(t, 0, 0, 160, 100, C(0.86f, 0.75f, 0.52f));
            Borde(t, C(0.45f, 0.3f, 0.15f));
            Rellenar(t, 0, 0, 160, 4, C(0.6f, 0.42f, 0.2f));
            Rellenar(t, 0, 96, 160, 4, C(0.6f, 0.42f, 0.2f));
        }
        else if (n == "ui_stamp_yes" || n == "ui_stamp_no")
        {
            Color c = n.EndsWith("yes") ? C(0.2f, 0.65f, 0.3f) : C(0.75f, 0.2f, 0.2f);
            Rellenar(t, 2, 2, 44, 44, c);
            Borde(t, Oscuro(c, 0.5f), 2);
            if (n.EndsWith("yes")) Rellenar(t, 14, 20, 20, 8, Color.white);       // "+"
            if (n.EndsWith("yes")) Rellenar(t, 20, 14, 8, 20, Color.white);
            else Rellenar(t, 12, 20, 24, 8, Color.white);                          // "-"
        }
        else if (n == "ui_hourglass")
        {
            for (int f = 0; f < 8; f++)
            {
                int x0 = f * 24;
                Rellenar(t, x0 + 2, 1, 20, 2, C(0.5f, 0.35f, 0.15f));
                Rellenar(t, x0 + 2, 29, 20, 2, C(0.5f, 0.35f, 0.15f));
                int arriba = Mathf.RoundToInt((7 - f) / 7f * 12f);
                int abajo = Mathf.RoundToInt(f / 7f * 12f);
                Rellenar(t, x0 + 6, 17, 12, arriba, C(0.95f, 0.8f, 0.4f));
                Rellenar(t, x0 + 6, 3, 12, abajo, C(0.95f, 0.8f, 0.4f));
                Rellenar(t, x0 + 11, 15, 2, 2, C(0.95f, 0.8f, 0.4f));
            }
        }
        else if (n.StartsWith("ui_btn_"))
        {
            Color c = n.EndsWith("farm") ? C(0.35f, 0.6f, 0.3f)
                    : n.EndsWith("armory") ? C(0.4f, 0.45f, 0.6f)
                    : n.EndsWith("church") ? C(0.6f, 0.4f, 0.65f)
                    : n.EndsWith("vault") ? C(0.85f, 0.7f, 0.2f)
                    : C(0.6f, 0.6f, 0.6f);
            Rellenar(t, 1, 1, 30, 30, c);
            Borde(t, Oscuro(c, 0.45f), 1);
            Rellenar(t, 10, 10, 12, 12, Oscuro(c, 0.7f));
        }
        else if (n.StartsWith("screen_"))
        {
            Color c = n.EndsWith("title") ? C(0.15f, 0.08f, 0.1f) : n.EndsWith("dayend") ? C(0.08f, 0.1f, 0.16f) : C(0.1f, 0.1f, 0.1f);
            Rellenar(t, 0, 0, 320, 180, c);
            Borde(t, C(0.6f, 0.45f, 0.15f), 3);
        }
        else if (n.StartsWith("msg_"))
        {
            Color c = n.EndsWith("peasant") ? C(0.5f, 0.4f, 0.2f)
                    : n.EndsWith("soldier") ? C(0.45f, 0.5f, 0.6f)
                    : n.EndsWith("monk") ? C(0.4f, 0.28f, 0.18f)
                    : C(0.5f, 0.2f, 0.55f);
            Rellenar(t, 12, 0, 40, 60, c);
            Rellenar(t, 22, 60, 20, 22, C(0.9f, 0.72f, 0.58f));
            Borde(t, Color.clear);
        }
        else if (n.StartsWith("portrait_"))
        {
            Color c = n.EndsWith("tobias") ? C(0.35f, 0.4f, 0.5f) : C(0.5f, 0.35f, 0.4f);
            Rellenar(t, 0, 0, 48, 48, c);
            Rellenar(t, 14, 10, 20, 22, C(0.9f, 0.72f, 0.58f));
            Borde(t, Color.white);
        }
        else if (n.StartsWith("crowd_"))
        {
            DibujarMultitud(t, n);
        }
        else Rellenar(t, 0, 0, e.ancho, e.alto, Color.magenta);

        t.Apply();
        return t;
    }

    // Multitud: figuras del color del grupo, más brillantes cuanto mejor el estado; cuadritos blancos = número de estado + 1.
    static void DibujarMultitud(Texture2D t, string n)
    {
        string[] p = n.Split('_');
        int estado = int.Parse(p[2]);
        Color baseColor = p[1] == "farm" ? C(0.45f, 0.65f, 0.3f) : p[1] == "army" ? C(0.45f, 0.5f, 0.7f) : C(0.6f, 0.45f, 0.7f);
        Color c = Oscuro(baseColor, 0.5f + 0.125f * estado);

        for (int i = 0; i < 26; i++)
        {
            int x = 6 + i * 12;
            int altura = 26 + ((i * 7) % 5) * 5;
            Rellenar(t, x, 0, 9, altura, c);
            Rellenar(t, x + 1, altura, 7, 7, C(0.9f, 0.72f, 0.58f));
            if (estado == 0 && i % 3 == 0) Rellenar(t, x + 3, altura + 8, 3, 8, C(1f, 0.55f, 0.1f));   // antorchas
        }
        for (int i = 0; i <= estado; i++) Rellenar(t, 4 + i * 8, 80, 6, 6, Color.white);
    }

    static Color C(float r, float g, float b) => new Color(r, g, b, 1f);
    static Color Oscuro(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);

    static void Rellenar(Texture2D t, int x, int y, int w, int h, Color c)
    {
        for (int j = Mathf.Max(0, y); j < Mathf.Min(t.height, y + h); j++)
            for (int i = Mathf.Max(0, x); i < Mathf.Min(t.width, x + w); i++)
                t.SetPixel(i, j, c);
    }

    static void Borde(Texture2D t, Color c, int grosor = 1)
    {
        if (c.a <= 0f) return;
        for (int k = 0; k < grosor; k++)
        {
            for (int i = 0; i < t.width; i++) { t.SetPixel(i, k, c); t.SetPixel(i, t.height - 1 - k, c); }
            for (int j = 0; j < t.height; j++) { t.SetPixel(k, j, c); t.SetPixel(t.width - 1 - k, j, c); }
        }
    }

    // ---------------- sonidos (suaves, solo para probar) ----------------

    const int Hz = 22050;

    static float[] SonidoDe(string n)
    {
        switch (n)
        {
            case "sfx_tick": return Tono(1800f, 0.03f, 0.35f, 60f);
            case "sfx_stamp_yes": return Mezclar(Tono(150f, 0.18f, 0.6f, 14f), Ruido(0.05f, 0.3f, 40f));
            case "sfx_stamp_no": return Mezclar(Tono(95f, 0.24f, 0.6f, 10f), Ruido(0.05f, 0.3f, 40f));
            case "sfx_steps": return Concatenar(Ruido(0.06f, 0.3f, 30f), Silencio(0.14f), Ruido(0.06f, 0.3f, 30f), Silencio(0.14f), Ruido(0.06f, 0.3f, 30f), Silencio(0.14f));
            case "sfx_bell": return Mezclar(Tono(880f, 0.9f, 0.4f, 4f), Tono(1320f, 0.9f, 0.2f, 5f));
            case "sfx_door_knock": return Concatenar(Tono(90f, 0.1f, 0.7f, 18f), Silencio(0.15f), Tono(90f, 0.1f, 0.7f, 18f), Silencio(0.1f));
            case "music_loop": return Mezclar(Tono(110f, 4f, 0.06f, 0f), Tono(165f, 4f, 0.04f, 0f));
        }
        if (n.StartsWith("amb_crowd_"))
        {
            int estado = n[n.Length - 1] - '0';
            return RuidoModulado(2f, 0.06f, estado + 1);
        }
        return Silencio(0.2f);
    }

    static float[] Silencio(float seg) => new float[(int)(Hz * seg)];

    static float[] Tono(float freq, float seg, float vol, float decaimiento)
    {
        var m = new float[(int)(Hz * seg)];
        for (int i = 0; i < m.Length; i++)
        {
            float t = i / (float)Hz;
            m[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * vol * Mathf.Exp(-decaimiento * t);
        }
        return m;
    }

    static float[] Ruido(float seg, float vol, float decaimiento)
    {
        var rng = new System.Random(7);
        var m = new float[(int)(Hz * seg)];
        for (int i = 0; i < m.Length; i++)
            m[i] = ((float)rng.NextDouble() * 2f - 1f) * vol * Mathf.Exp(-decaimiento * i / Hz);
        return m;
    }

    static float[] RuidoModulado(float seg, float vol, float frecuencia)
    {
        var rng = new System.Random(3);
        var m = new float[(int)(Hz * seg)];
        for (int i = 0; i < m.Length; i++)
        {
            float t = i / (float)Hz;
            float mod = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * frecuencia * t / seg * Mathf.Round(seg));
            m[i] = ((float)rng.NextDouble() * 2f - 1f) * vol * mod;
        }
        return m;
    }

    static float[] Mezclar(float[] a, float[] b)
    {
        var m = new float[Mathf.Max(a.Length, b.Length)];
        for (int i = 0; i < m.Length; i++) m[i] = (i < a.Length ? a[i] : 0f) + (i < b.Length ? b[i] : 0f);
        return m;
    }

    static float[] Concatenar(params float[][] partes)
    {
        int total = 0;
        foreach (float[] p in partes) total += p.Length;
        var m = new float[total];
        int pos = 0;
        foreach (float[] p in partes) { System.Array.Copy(p, 0, m, pos, p.Length); pos += p.Length; }
        return m;
    }

    static void EscribirWav(string ruta, float[] muestras)
    {
        using (var fs = new FileStream(ruta, FileMode.Create))
        using (var w = new BinaryWriter(fs))
        {
            int datos = muestras.Length * 2;
            w.Write(new[] { 'R', 'I', 'F', 'F' });
            w.Write(36 + datos);
            w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(Hz); w.Write(Hz * 2); w.Write((short)2); w.Write((short)16);
            w.Write(new[] { 'd', 'a', 't', 'a' });
            w.Write(datos);
            foreach (float s in muestras) w.Write((short)(Mathf.Clamp(s, -1f, 1f) * short.MaxValue));
        }
    }
}
