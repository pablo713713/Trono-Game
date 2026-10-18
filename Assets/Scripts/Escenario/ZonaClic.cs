using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Zona invisible sobre el fondo del trono que lleva a otra vista al hacer clic
// (puerta izquierda -> catedral, gradas -> mercado, puerta derecha -> armería, cofre -> bóveda).
// Al pasar el mouse se ilumina y muestra un letrero con su nombre y su tecla.
[RequireComponent(typeof(Image))]
public class ZonaClic : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Vista destino;
    public string letrero = "Balcón";

    [SerializeField] Color colorResaltado = new Color(1f, 0.95f, 0.7f, 0.18f);

    Image area;
    GameObject cartel;

    void Awake()
    {
        area = GetComponent<Image>();
        area.color = new Color(1f, 1f, 1f, 0f);   // invisible, pero recibe clics
        area.raycastTarget = true;
        CrearCartel();
        Resaltar(false);
    }

    void CrearCartel()
    {
        cartel = new GameObject("Letrero", typeof(RectTransform), typeof(Image));
        cartel.transform.SetParent(transform, false);
        var r = (RectTransform)cartel.transform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);   // centrado arriba de la zona
        r.pivot = new Vector2(0.5f, 0f);
        r.anchoredPosition = new Vector2(0f, 2f);
        r.sizeDelta = new Vector2(Mathf.Max(60f, letrero.Length * 5f + 8f), 12f);
        var fondo = cartel.GetComponent<Image>();
        fondo.color = new Color(0f, 0f, 0f, 0.75f);
        fondo.raycastTarget = false;

        var textoGO = new GameObject("Texto", typeof(RectTransform), typeof(Text));
        textoGO.transform.SetParent(cartel.transform, false);
        var tr = (RectTransform)textoGO.transform;
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        var texto = textoGO.GetComponent<Text>();
        texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.fontSize = 8;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = Color.white;
        texto.horizontalOverflow = HorizontalWrapMode.Overflow;
        texto.raycastTarget = false;
        texto.text = letrero;
        textoGO.AddComponent<TextoPixel>();   // usa font_pixel.ttf si existe (componente del Desarrollador 2)
    }

    void Resaltar(bool si)
    {
        if (area != null) area.color = si ? colorResaltado : new Color(1f, 1f, 1f, 0f);
        if (cartel != null) cartel.SetActive(si);
    }

    public void OnPointerEnter(PointerEventData e) => Resaltar(true);
    public void OnPointerExit(PointerEventData e) => Resaltar(false);

    public void OnPointerClick(PointerEventData e)
    {
        Resaltar(false);
        if (Navegador.Instancia != null) Navegador.Instancia.Ir(destino);
    }

    void OnDisable() => Resaltar(false);
}
