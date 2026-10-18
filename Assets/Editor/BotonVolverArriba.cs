using UnityEditor;
using UnityEngine;

// Menú "Trono > Dev1 > Botón volver arriba a la izquierda".
// Mueve el botón "BotonVolver" (ui_btn_back) de cada balcón de abajo a la izquierda
// a arriba a la izquierda. Coordenadas en píxeles de arte (320 x 180, origen abajo a la izquierda).
// Si alguien vuelve a correr "Trono > Dev2 > 2 Construir escena y prefabs", hay que correr esto de nuevo.
public static class BotonVolverArriba
{
    const float X = 2f, Y = 146f, Tam = 32f;   // 2 px del borde izquierdo y 2 px del borde superior

    static readonly string[] prefabs =
    {
        "Panel_Balcon_Catedral", "Panel_Balcon_Cultivos", "Panel_Balcon_Armeria", "Panel_Boveda",
    };

    [MenuItem("Trono/Dev1/Botón volver arriba a la izquierda")]
    public static void Mover()
    {
        int movidos = 0;
        foreach (string nombre in prefabs)
        {
            string ruta = "Assets/Prefabs/" + nombre + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ruta) == null) continue;

            GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                Transform boton = raiz.transform.Find("BotonVolver");
                if (boton == null)
                {
                    Debug.LogWarning("[Trono] " + nombre + " no tiene BotonVolver.");
                    continue;
                }

                var r = (RectTransform)boton;
                r.anchorMin = r.anchorMax = Vector2.zero;
                r.pivot = Vector2.zero;
                r.anchoredPosition = new Vector2(X, Y);
                r.sizeDelta = new Vector2(Tam, Tam);
                boton.SetAsLastSibling();   // por encima de la multitud y el fondo

                PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                movidos++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }
        Debug.Log("[Trono] Botón volver movido arriba a la izquierda en " + movidos + " pantallas.");
    }
}
