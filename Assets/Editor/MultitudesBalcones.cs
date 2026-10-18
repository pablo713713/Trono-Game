using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// Menú "Trono > Dev1 > Poner multitudes en los balcones".
// En cada prefab de balcón crea (o actualiza) un hijo "MultitudSprites" con el componente Multitud,
// sobre la franja de la multitud (x 0-320, y 0-90), con el sprite de su grupo:
//   Panel_Balcon_Catedral -> Art/Crowds/fiel_iglesia.png
//   Panel_Balcon_Cultivos -> Art/Crowds/campesino.png
//   Panel_Balcon_Armeria  -> Art/Crowds/soldado.png
// Si el sprite de un balcón todavía no existe, ese balcón se salta (se puede volver a correr después).
public static class MultitudesBalcones
{
    struct Balcon
    {
        public string prefab;
        public Grupo grupo;
        public string sprite;
        public string sprite2;   // segundo tipo de poblador (mitad de la multitud); opcional
        // Área de la multitud en píxeles de arte (320 x 180, origen abajo a la izquierda).
        // y = donde pisan los de adelante; profundidad = hasta dónde llegan los de atrás.
        public float x, y, ancho, alto, profundidad;
    }

    static readonly Balcon[] balcones =
    {
        new Balcon { prefab = "Panel_Balcon_Catedral", grupo = Grupo.Iglesia, sprite = "fiel_iglesia", sprite2 = "fiel_iglesia2", x = 0, y = 0, ancho = 320, alto = 90, profundidad = 48 },
        new Balcon { prefab = "Panel_Balcon_Cultivos", grupo = Grupo.Campesinos, sprite = "campesino", sprite2 = "campesino2", x = 0, y = 0, ancho = 320, alto = 90, profundidad = 48 },
        new Balcon { prefab = "Panel_Balcon_Armeria", grupo = Grupo.Soldados, sprite = "soldado", sprite2 = "soldado2", x = 36, y = 70, ancho = 248, alto = 80, profundidad = 44 },   // sobre el pasto, entre las columnas
    };

    [MenuItem("Trono/Dev1/Poner multitudes en los balcones")]
    public static void Poner()
    {
        var hechos = new List<string>();
        var faltan = new List<string>();

        foreach (Balcon b in balcones)
        {
            string rutaPrefab = "Assets/Prefabs/" + b.prefab + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab) == null)
            {
                faltan.Add(b.prefab + ": no existe el prefab (corre Trono > Dev2 > 0 Todo)");
                continue;
            }

            string rutaSprite = "Assets/Art/Crowds/" + b.sprite + ".png";
            var hoja = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaSprite);
            if (hoja == null)
            {
                faltan.Add(b.prefab + ": falta " + rutaSprite);
                continue;
            }

            Sprite[] cuadros = Cuadros(rutaSprite, out string problema);
            if (cuadros == null)
            {
                faltan.Add(b.prefab + ": " + problema);
                continue;
            }

            GameObject raiz = PrefabUtility.LoadPrefabContents(rutaPrefab);
            try
            {
                Transform hijo = raiz.transform.Find("MultitudSprites");
                if (hijo == null)
                {
                    var go = new GameObject("MultitudSprites", typeof(RectTransform));
                    go.transform.SetParent(raiz.transform, false);
                    hijo = go.transform;
                }

                // Justo después de la imagen "Multitud" del panel: encima del fondo, debajo del título y el botón.
                Transform imagenPanel = raiz.transform.Find("Multitud");
                if (imagenPanel != null) hijo.SetSiblingIndex(imagenPanel.GetSiblingIndex() + 1);

                var r = (RectTransform)hijo;
                r.anchorMin = r.anchorMax = Vector2.zero;
                r.pivot = Vector2.zero;
                r.anchoredPosition = new Vector2(b.x, b.y);
                r.sizeDelta = new Vector2(b.ancho, b.alto);

                Multitud m = hijo.GetComponent<Multitud>();
                if (m == null) m = hijo.gameObject.AddComponent<Multitud>();

                var so = new SerializedObject(m);
                so.FindProperty("grupo").enumValueIndex = (int)b.grupo;
                so.FindProperty("hojaTranquilo").objectReferenceValue = hoja;
                so.FindProperty("usarEstadoDePrueba").boolValue = false;
                so.FindProperty("ancho").floatValue = b.ancho;
                so.FindProperty("profundidad").floatValue = b.profundidad;

                SerializedProperty lista = so.FindProperty("cuadrosTranquilo");
                lista.arraySize = cuadros.Length;
                for (int i = 0; i < cuadros.Length; i++)
                    lista.GetArrayElementAtIndex(i).objectReferenceValue = cuadros[i];

                // Segundo tipo de poblador: si su imagen existe, la mitad de la multitud lo usa.
                string nota2 = "";
                string ruta2 = "Assets/Art/Crowds/" + b.sprite2 + ".png";
                var hoja2 = string.IsNullOrEmpty(b.sprite2) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(ruta2);
                SerializedProperty lista2 = so.FindProperty("cuadrosTipo2");
                Sprite[] cuadros2 = hoja2 != null ? Cuadros(ruta2, out string problema2) : null;
                so.FindProperty("hojaTipo2").objectReferenceValue = cuadros2 != null ? hoja2 : null;
                lista2.arraySize = cuadros2 != null ? cuadros2.Length : 0;
                if (cuadros2 != null)
                {
                    for (int i = 0; i < cuadros2.Length; i++) lista2.GetArrayElementAtIndex(i).objectReferenceValue = cuadros2[i];
                    nota2 = " + " + cuadros2.Length + " de " + b.sprite2;
                }
                else if (!string.IsNullOrEmpty(b.sprite2)) nota2 = " (sin " + b.sprite2 + ": un solo tipo)";

                if (imagenPanel != null)
                    so.FindProperty("imagenAOcultar").objectReferenceValue = imagenPanel.GetComponent<UnityEngine.UI.Graphic>();

                so.ApplyModifiedPropertiesWithoutUndo();

                // El título del balcón se escribió al construir el prefab: actualizarlo (ej. CULTIVOS -> MERCADO).
                Transform titulo = raiz.transform.Find("Titulo");
                var textoTitulo = titulo != null ? titulo.GetComponent<UnityEngine.UI.Text>() : null;
                if (textoTitulo != null) textoTitulo.text = Textos.TituloBalcon(b.grupo);

                // Fondo de rebelión: con 80 % de enojo o más cambia a [fondo]_riot.
                FondoRebelion fr = raiz.GetComponent<FondoRebelion>();
                if (fr == null) fr = raiz.AddComponent<FondoRebelion>();
                fr.grupo = b.grupo;
                fr.fondoNormal = Textos.FondoBalcon(b.grupo);
                fr.fondoRebelion = fr.fondoNormal + "_riot";
                Transform fondo = raiz.transform.Find("Fondo");
                fr.fondo = fondo != null ? fondo.GetComponent<UnityEngine.UI.Image>() : null;

                PrefabUtility.SaveAsPrefabAsset(raiz, rutaPrefab);
                hechos.Add(b.prefab + " (" + cuadros.Length + " cuadros de " + b.sprite + nota2 + ")");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        foreach (string h in hechos) Debug.Log("[Trono] Multitud lista en " + h);
        foreach (string f in faltan) Debug.LogWarning("[Trono] Multitud pendiente en " + f);

        EditorUtility.DisplayDialog("Multitudes en los balcones",
            (hechos.Count > 0 ? "Listas:\n- " + string.Join("\n- ", hechos) : "Ninguna lista todavía.") +
            (faltan.Count > 0 ? "\n\nPendientes:\n- " + string.Join("\n- ", faltan) : ""),
            "OK");
    }

    // Cuadros de la tira, de izquierda a derecha. Si la imagen no está cortada, la corta sola
    // detectando las figuras separadas por columnas transparentes.
    static Sprite[] Cuadros(string ruta, out string problema)
    {
        problema = null;
        var importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importador == null) { problema = "no se pudo leer " + ruta; return null; }

        if (importador.textureType != TextureImporterType.Sprite || importador.spriteImportMode != SpriteImportMode.Multiple)
        {
            if (!CortarAutomatico(ruta, importador, out problema)) return null;
        }

        var lista = new List<Sprite>();
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ruta))
            if (o is Sprite s) lista.Add(s);

        if (lista.Count == 0) { problema = "la imagen no tiene cuadros; córtala en el Sprite Editor (Slice > Apply)"; return null; }
        lista.Sort((a, b) => a.rect.x.CompareTo(b.rect.x));
        return lista.ToArray();
    }

    // Corta una tira horizontal: cada grupo de columnas con píxeles visibles es un cuadro.
    static bool CortarAutomatico(string ruta, TextureImporter importador, out string problema)
    {
        problema = null;
        importador.textureType = TextureImporterType.Sprite;
        importador.isReadable = true;
        importador.SaveAndReimport();

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        Color32[] px = tex.GetPixels32();
        int w = tex.width, h = tex.height;

        var columnas = new bool[w];
        int yMin = h, yMax = -1;
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                if (px[y * w + x].a > 10)
                {
                    columnas[x] = true;
                    if (y < yMin) yMin = y;
                    if (y > yMax) yMax = y;
                }

        var rects = new List<SpriteRect>();
        int inicio = -1;
        for (int x = 0; x <= w; x++)
        {
            bool lleno = x < w && columnas[x];
            if (lleno && inicio < 0) inicio = x;
            if (!lleno && inicio >= 0)
            {
                rects.Add(new SpriteRect
                {
                    name = tex.name + "_" + rects.Count,
                    rect = new Rect(inicio, yMin, x - inicio, yMax - yMin + 1),
                    alignment = SpriteAlignment.BottomCenter,
                    pivot = new Vector2(0.5f, 0f),
                    spriteID = GUID.Generate(),
                });
                inicio = -1;
            }
        }

        if (rects.Count == 0) { problema = "la imagen está vacía o no tiene fondo transparente"; return false; }

        importador.spriteImportMode = SpriteImportMode.Multiple;
        importador.isReadable = false;
        importador.SaveAndReimport();

        var fabrica = new SpriteDataProviderFactories();
        fabrica.Init();
        ISpriteEditorDataProvider datos = fabrica.GetSpriteEditorDataProviderFromObject(importador);
        datos.InitSpriteEditorDataProvider();
        datos.SetSpriteRects(rects.ToArray());
        datos.Apply();
        importador.SaveAndReimport();
        Debug.Log("[Trono] " + tex.name + " cortada en " + rects.Count + " cuadros.");
        return true;
    }
}
