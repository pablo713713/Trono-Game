using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Menú "Trono > Dev1 > Aplicar disposición del trono".
// Cambia el prefab Panel_Trono a la nueva disposición:
//   puerta izquierda -> catedral, gradas -> mercado (cultivos), puerta derecha -> armería,
//   mensajero en la puerta central, pergamino y sellos sobre la mesa (franja inferior).
// Oculta los 4 botones de la izquierda; las teclas 1-4 siguen funcionando.
// Si alguien vuelve a correr "Trono > Dev2 > 2 Construir escena y prefabs", hay que volver a correr esto.
// Coordenadas en píxeles de arte (320 x 180), origen abajo a la izquierda.
public static class DisposicionTrono
{
    const string RutaPrefab = "Assets/Prefabs/Panel_Trono.prefab";

    [MenuItem("Trono/Dev1/Aplicar disposición del trono")]
    public static void Aplicar()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) == null)
        {
            EditorUtility.DisplayDialog("Disposición del trono",
                "No encuentro " + RutaPrefab + ".\nPrimero corre Trono > Dev2 > 0 Todo.", "OK");
            return;
        }

        GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
        try
        {
            // 1. Ocultar los botones de navegación de la izquierda.
            foreach (string nombre in new[] { "BotonCultivos", "BotonArmeria", "BotonCatedral", "BotonBoveda" })
            {
                Transform b = raiz.transform.Find(nombre);
                if (b != null) b.gameObject.SetActive(false);
            }

            // 2. Zonas de clic, justo encima del fondo (el resto de la interfaz queda por encima).
            int indice = IndiceDespuesDe(raiz.transform, "Fondo");
            indice = Zona(raiz.transform, indice, "ZonaCatedral", Vista.Catedral, "Balcón de la catedral (3)", 40, 36, 32, 78);
            indice = Zona(raiz.transform, indice, "ZonaMercado", Vista.Cultivos, "Balcón del mercado (1)", 80, 44, 64, 92);
            Zona(raiz.transform, indice, "ZonaArmeria", Vista.Armeria, "Balcón de la armería (2)", 248, 69, 28, 41);

            // Versión 2: sin bóveda. El cofre queda como decoración del fondo.
            Transform cofre = raiz.transform.Find("ZonaBoveda");
            if (cofre != null) Object.DestroyImmediate(cofre.gameObject);

            // 3. Mensajero en la puerta central.
            Mover(raiz.transform, "Mensajero", 137, 30, 64, 96);

            // 4. Pergamino sobre la mesa, más ancho y bajo.
            Mover(raiz.transform, "Pergamino", 75, 2, 170, 58);
            Mover(raiz.transform, "Pergamino/Texto", 8, 8, 154, 44);
            Mover(raiz.transform, "Pergamino/Continuar", 8, 1, 154, 9);
            Mover(raiz.transform, "Pergamino/SelloPuesto", 118, 8, 40, 40);

            // 5. Sellos sobre la mesa, a la derecha.
            Mover(raiz.transform, "SelloSi", 248, 2, 34, 34);
            Mover(raiz.transform, "SelloNo", 284, 2, 34, 34);

            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            Debug.Log("[Trono] Disposición del trono aplicada: puertas, gradas y cofre son clicables; pergamino y sellos sobre la mesa.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    static int IndiceDespuesDe(Transform raiz, string nombre)
    {
        Transform t = raiz.Find(nombre);
        return t != null ? t.GetSiblingIndex() + 1 : 0;
    }

    static int Zona(Transform raiz, int indice, string nombre, Vista destino, string letrero,
                    float x, float y, float w, float h)
    {
        Transform existente = raiz.Find(nombre);
        if (existente != null) Object.DestroyImmediate(existente.gameObject);

        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(raiz, false);
        go.transform.SetSiblingIndex(indice);
        Colocar((RectTransform)go.transform, x, y, w, h);

        var img = go.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0f);
        img.raycastTarget = true;

        var zona = go.AddComponent<ZonaClic>();
        zona.destino = destino;
        zona.letrero = letrero;
        return indice + 1;
    }

    static void Mover(Transform raiz, string ruta, float x, float y, float w, float h)
    {
        Transform t = raiz.Find(ruta);
        if (t == null)
        {
            Debug.LogWarning("[Trono] No encontré '" + ruta + "' en Panel_Trono; no lo moví.");
            return;
        }
        Colocar((RectTransform)t, x, y, w, h);
    }

    static void Colocar(RectTransform r, float x, float y, float w, float h)
    {
        r.anchorMin = r.anchorMax = Vector2.zero;
        r.pivot = Vector2.zero;
        r.anchoredPosition = new Vector2(x, y);
        r.sizeDelta = new Vector2(w, h);
    }
}
