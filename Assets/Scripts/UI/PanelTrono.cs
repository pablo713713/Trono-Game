using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// D2-03 y D2-08: el mensajero, el pergamino y los sellos SÍ / NO.
// Solo se decide desde aquí (el Navegador oculta este panel en las otras vistas).
public class PanelTrono : MonoBehaviour
{
    public Image mensajero;
    public Text texto;
    public Text indicadorContinuar;        // "▼ Espacio" cuando el Duque tiene más texto
    public Button pergamino;               // clic en el pergamino = seguir leyendo (Duque)
    public Button selloSi;
    public Button selloNo;
    public Image selloPuesto;              // sello estampado sobre el pergamino

    public Peticion Mostrada { get; private set; }
    public bool EnTransicion => salida != null;

    bool puedeDecidir;
    string[] partes;
    int parte;
    Coroutine salida;
    Coroutine entrada;
    Peticion pendiente;
    float mensajeroX;
    float tiempoInicio;

    void Awake()
    {
        mensajeroX = mensajero.rectTransform.anchoredPosition.x;
    }

    void Start()
    {
        selloSi.onClick.AddListener(() => Pulsar(true));
        selloNo.onClick.AddListener(() => Pulsar(false));
        pergamino.onClick.AddListener(AvanzarTexto);
        Limpiar();
    }

    void Update()
    {
        if (Navegador.Instancia == null || Navegador.Instancia.Bloqueado || Navegador.Instancia.Actual != Vista.Trono) return;

        if (Teclado.Presionada(Tecla.S)) Pulsar(true);
        else if (Teclado.Presionada(Tecla.N)) Pulsar(false);
        else if (Teclado.Presionada(Tecla.Espacio) || Teclado.Presionada(Tecla.Enter)) AvanzarTexto();
    }

    // ---------- lo llama el Director ----------

    public void Presentar(Peticion peticion)
    {
        if (salida != null) { pendiente = peticion; return; }   // espera a que se vaya el anterior
        Comenzar(peticion);
    }

    public void Resolver(bool si)
    {
        if (Mostrada == null) return;
        puedeDecidir = false;
        ActualizarSellos();
        if (salida != null) StopCoroutine(salida);
        salida = StartCoroutine(Salida(si));
    }

    public void Limpiar()
    {
        if (entrada != null) StopCoroutine(entrada);
        if (salida != null) StopCoroutine(salida);
        entrada = salida = null;
        pendiente = null;
        Mostrada = null;
        puedeDecidir = false;
        texto.text = "";
        indicadorContinuar.enabled = false;
        mensajero.enabled = false;
        selloPuesto.enabled = false;
        ActualizarSellos();
    }

    // ---------- interno ----------

    void Comenzar(Peticion peticion)
    {
        Mostrada = peticion;
        tiempoInicio = Time.unscaledTime;
        selloPuesto.enabled = false;

        partes = peticion.EsDuque ? Textos.PartesDuque(peticion.texto) : new[] { peticion.texto };
        parte = 0;
        texto.text = partes[0];
        puedeDecidir = partes.Length == 1;
        indicadorContinuar.enabled = !puedeDecidir;
        ActualizarSellos();

        Sprite s = ArteCatalogo.ObtenerSprite("msg_" + peticion.mensajero);
        mensajero.sprite = s != null ? s : mensajero.sprite;
        mensajero.enabled = s != null;     // sin arte todavía (mercader, niño...) no se dibuja

        if (entrada != null) StopCoroutine(entrada);
        entrada = StartCoroutine(Entrada());
    }

    void AvanzarTexto()
    {
        if (Mostrada == null || partes == null || puedeDecidir || salida != null) return;
        if (Time.unscaledTime - tiempoInicio < 0.3f) return;   // evita saltarse el texto con la misma tecla que cerró la pantalla anterior
        if (parte < partes.Length - 1)
        {
            parte++;
            texto.text = partes[parte];
        }
        if (parte >= partes.Length - 1)
        {
            puedeDecidir = true;
            indicadorContinuar.enabled = false;
            ActualizarSellos();
        }
    }

    void Pulsar(bool si)
    {
        IGameState e = Juego.Estado;
        if (!puedeDecidir || e == null || Mostrada == null) return;
        if (e.PeticionActual != Mostrada) return;     // ya se resolvió (por reloj, por ejemplo)
        puedeDecidir = false;
        e.Decidir(si);
    }

    void ActualizarSellos()
    {
        selloSi.interactable = puedeDecidir;
        selloNo.interactable = puedeDecidir;
    }

    IEnumerator Entrada()
    {
        RectTransform r = mensajero.rectTransform;
        const float duracion = 0.35f;
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            float k = 1f - Mathf.Pow(1f - t / duracion, 2f);
            r.anchoredPosition = new Vector2(Mathf.Lerp(mensajeroX + 40f, mensajeroX, k), r.anchoredPosition.y);
            mensajero.color = new Color(1f, 1f, 1f, k);
            yield return null;
        }
        r.anchoredPosition = new Vector2(mensajeroX, r.anchoredPosition.y);
        mensajero.color = Color.white;
        entrada = null;
    }

    IEnumerator Salida(bool si)
    {
        if (entrada != null) { StopCoroutine(entrada); entrada = null; }

        Sprite sello = ArteCatalogo.ObtenerSprite(si ? "ui_stamp_yes" : "ui_stamp_no");
        selloPuesto.sprite = sello != null ? sello : selloPuesto.sprite;
        selloPuesto.enabled = true;

        RectTransform sr = selloPuesto.rectTransform;
        for (float t = 0f; t < 0.12f; t += Time.deltaTime)     // golpe del sello
        {
            sr.localScale = Vector3.one * Mathf.Lerp(1.8f, 1f, t / 0.12f);
            yield return null;
        }
        sr.localScale = Vector3.one;
        yield return new WaitForSeconds(0.35f);

        RectTransform r = mensajero.rectTransform;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)      // se va el mensajero
        {
            float k = t / 0.3f;
            r.anchoredPosition = new Vector2(Mathf.Lerp(mensajeroX, mensajeroX + 40f, k), r.anchoredPosition.y);
            mensajero.color = new Color(1f, 1f, 1f, 1f - k);
            yield return null;
        }

        selloPuesto.enabled = false;
        texto.text = "";
        mensajero.enabled = false;
        Mostrada = null;
        salida = null;

        if (pendiente != null)
        {
            Peticion siguiente = pendiente;
            pendiente = null;
            Comenzar(siguiente);
        }
    }
}
