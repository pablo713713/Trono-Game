using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// D1-10. Multitud de un balcón armada con sprites sueltos, por cantidad:
//   4 contentos   -> casi nadie en el balcón
//   3 tranquilos  -> poca gente
//   2 murmullos   -> gente media
//   1 carteles    -> mucha gente, más agitada
//   0 antorchas   -> balcón lleno, todos al frente y saltando
//
// Funciona de dos formas, y elige sola según dónde esté:
//   - Dentro de un Canvas (los balcones del Desarrollador 2): crea imágenes de interfaz,
//     que se ocultan junto con el panel. Va como hijo del panel del balcón, con su
//     RectTransform cubriendo la franja de la multitud (x 0-320, y 0-90).
//     Apaga la imagen "Multitud" del panel (la de crowd_[grupo]_[estado].png).
//   - Fuera de un Canvas (escena de prueba): crea sprites del mundo.
//
// Uso: agregar el componente, elegir el grupo y poner la imagen completa del sprite en
// "Hoja Tranquilo" (los cuadros se cargan solos). Sin GameState funciona "Estado De Prueba".
public class Multitud : MonoBehaviour
{
    [Header("Grupo")]
    [SerializeField] Grupo grupo = Grupo.Iglesia;

    [Header("Imágenes completas de cada animación (los cuadros se sacan solos)")]
    [SerializeField] Texture2D hojaTranquilo;
    [SerializeField] Texture2D hojaEnojado;
    [SerializeField] Texture2D hojaFestejo;

    [Header("Cuadros (se llenan solos desde las imágenes de arriba)")]
    [SerializeField] Sprite[] cuadrosTranquilo;
    [SerializeField] Sprite[] cuadrosEnojado;
    [SerializeField] Sprite[] cuadrosFestejo;

    [Header("Área de la multitud (en píxeles de arte)")]
    [SerializeField] float ancho = 320f;
    [SerializeField] float profundidad = 48f;   // filas de atrás hacia adelante
    [SerializeField] int maximoDePersonas = 30;

    [Header("Cantidad de personas por estado (0 = antorchas ... 4 = contentos)")]
    [SerializeField] int[] personasPorEstado = { 30, 20, 12, 6, 2 };

    [Header("Interfaz: imagen de multitud del panel que se debe apagar")]
    [Tooltip("Si se deja vacío, busca en el panel un hermano llamado 'Multitud' con Image.")]
    [SerializeField] Graphic imagenAOcultar;

    [Header("Escena de prueba (sprites del mundo)")]
    [SerializeField] string sortingLayer = "Default";
    [SerializeField] int ordenBase = 10;

    [Header("Prueba sin GameState")]
    [SerializeField] bool usarEstadoDePrueba;
    [SerializeField, Range(0, 4)] int estadoDePrueba = 2;

    const float PixelesPorUnidad = 16f;

    // Velocidad de animación (cuadros por segundo) y salto en píxeles por estado.
    static readonly float[] fpsPorEstado = { 14f, 10f, 7f, 5f, 4f };
    static readonly float[] saltoPorEstado = { 3f, 1f, 0f, 0f, 0f };

    class Persona
    {
        public GameObject objeto;
        public SpriteRenderer render;     // modo mundo
        public Image imagen;              // modo interfaz
        public RectTransform rect;
        public Vector2 posicionBase;      // en píxeles de arte
        public float desfase;
        public float velocidad;
    }

    readonly List<Persona> personas = new List<Persona>();
    bool modoInterfaz;
    int estadoActual = -1;
    bool suscrito;
    bool avisoSinCuadros;

    void Awake()
    {
        modoInterfaz = GetComponentInParent<Canvas>(true) != null;
        if (modoInterfaz && imagenAOcultar == null) BuscarImagenDelPanel();
        CrearPersonas();
    }

    void Start()
    {
        Suscribir();
        Refrescar();
    }

    void OnEnable()
    {
        if (personas.Count > 0) { estadoActual = -1; Refrescar(); }
    }

    void OnDestroy()
    {
        if (suscrito && Juego.Estado != null) Juego.Estado.EstadoCambio -= Refrescar;
    }

    void BuscarImagenDelPanel()
    {
        if (transform.parent == null) return;
        Transform hermano = transform.parent.Find("Multitud");
        if (hermano != null && hermano != transform) imagenAOcultar = hermano.GetComponent<Graphic>();
    }

    void Suscribir()
    {
        if (suscrito || Juego.Estado == null) return;
        Juego.Estado.EstadoCambio += Refrescar;
        suscrito = true;
    }

    bool ModoPrueba => usarEstadoDePrueba || Juego.Estado == null;

    int LeerEstado() => ModoPrueba ? estadoDePrueba : Juego.Estado.EstadoVisual(grupo);

    // Crea todas las personas una sola vez y luego solo se muestran u ocultan.
    // Posiciones con semilla fija por grupo: la multitud no "salta" de lugar.
    void CrearPersonas()
    {
        var azar = new System.Random(1000 + (int)grupo);
        for (int i = 0; i < maximoDePersonas; i++)
        {
            var p = new Persona
            {
                posicionBase = new Vector2((float)azar.NextDouble() * ancho, (float)azar.NextDouble() * profundidad),
                desfase = (float)azar.NextDouble() * 10f,
                velocidad = 0.85f + 0.3f * (float)azar.NextDouble(),
            };
            bool espejo = azar.NextDouble() < 0.5;
            float brillo = 0.85f + 0.15f * (float)azar.NextDouble();
            var color = new Color(brillo, brillo, brillo, 1f);

            if (modoInterfaz)
            {
                p.objeto = new GameObject($"Persona_{i:00}", typeof(RectTransform), typeof(Image));
                p.objeto.transform.SetParent(transform, false);
                p.rect = (RectTransform)p.objeto.transform;
                p.rect.anchorMin = p.rect.anchorMax = Vector2.zero;   // abajo a la izquierda
                p.rect.pivot = new Vector2(0.5f, 0f);                  // pies en el suelo
                p.rect.localScale = new Vector3(espejo ? -1f : 1f, 1f, 1f);
                p.imagen = p.objeto.GetComponent<Image>();
                p.imagen.raycastTarget = false;
                p.imagen.color = color;
            }
            else
            {
                p.objeto = new GameObject($"Persona_{i:00}");
                p.objeto.transform.SetParent(transform, false);
                p.render = p.objeto.AddComponent<SpriteRenderer>();
                p.render.sortingLayerName = sortingLayer;
                p.render.flipX = espejo;
                p.render.color = color;
            }

            p.objeto.SetActive(false);
            personas.Add(p);
        }

        // En interfaz el orden de dibujo es el orden de los hijos: primero los de atrás.
        if (modoInterfaz)
        {
            var porFila = new List<Persona>(personas);
            porFila.Sort((a, b) => b.posicionBase.y.CompareTo(a.posicionBase.y));
            for (int i = 0; i < porFila.Count; i++) porFila[i].objeto.transform.SetSiblingIndex(i);
        }
    }

    void Refrescar()
    {
        Suscribir();
        int estado = Mathf.Clamp(LeerEstado(), 0, 4);
        if (estado == estadoActual) return;
        estadoActual = estado;

        int visibles = Mathf.Min(personas.Count, personasPorEstado[Mathf.Clamp(estado, 0, personasPorEstado.Length - 1)]);
        for (int i = 0; i < personas.Count; i++)
            personas[i].objeto.SetActive(i < visibles);

        if (ModoPrueba) Debug.Log($"[Multitud] {name}: estado {estado}, {visibles} personas", this);
    }

    Sprite[] CuadrosDelEstado(int estado)
    {
        Sprite[] elegidos = Limpiar(estado <= 1 ? cuadrosEnojado : estado == 4 ? cuadrosFestejo : cuadrosTranquilo);
        if (elegidos.Length > 0) return elegidos;
        return Limpiar(cuadrosTranquilo);
    }

    static Sprite[] Limpiar(Sprite[] cuadros)
    {
        if (cuadros == null) return new Sprite[0];
        return System.Array.FindAll(cuadros, s => s != null);
    }

    void Update()
    {
        bool prueba = ModoPrueba;
        if (prueba && estadoDePrueba != estadoActual) Refrescar();

        // El GameState arranca apagado y se enciende al pulsar Jugar.
        if (!prueba && !suscrito)
        {
            Suscribir();
            estadoActual = -1;
            Refrescar();
        }
        if (estadoActual < 0) return;

        Sprite[] cuadros = CuadrosDelEstado(estadoActual);
        if (cuadros.Length == 0)
        {
            if (!avisoSinCuadros)
            {
                Debug.LogWarning($"[Multitud] {name}: no hay cuadros. Pon la imagen del sprite en 'Hoja Tranquilo' (con el juego detenido).", this);
                avisoSinCuadros = true;
            }
            return;
        }

        float fps = fpsPorEstado[estadoActual];
        float salto = saltoPorEstado[estadoActual];
        bool alFrente = estadoActual == 0;

        foreach (Persona p in personas)
        {
            if (!p.objeto.activeSelf) continue;

            float t = Time.time * p.velocidad + p.desfase;
            Sprite cuadro = cuadros[Mathf.FloorToInt(t * fps) % cuadros.Length];

            Vector2 pos = p.posicionBase;
            if (alFrente) pos.y *= 0.4f;                       // en antorchas todos se apiñan al frente
            pos.y += Mathf.Abs(Mathf.Sin(t * 6f)) * salto;     // saltitos de enojo
            pos = new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.y));   // pixel art nítido

            if (modoInterfaz)
            {
                p.imagen.sprite = cuadro;
                p.rect.sizeDelta = cuadro.rect.size;           // 1 unidad del Canvas = 1 píxel de arte
                p.rect.anchoredPosition = pos;
            }
            else
            {
                p.render.sprite = cuadro;
                p.objeto.transform.localPosition = pos / PixelesPorUnidad;
                p.render.sortingOrder = ordenBase + Mathf.RoundToInt(profundidad - p.posicionBase.y);
            }
        }
    }

    void LateUpdate()
    {
        // El panel del balcón vuelve a encender su imagen de multitud al refrescar: mantenerla apagada.
        if (imagenAOcultar != null && imagenAOcultar.enabled) imagenAOcultar.enabled = false;
    }

#if UNITY_EDITOR
    // Llena las listas de cuadros a partir de las imágenes completas (solo en el editor).
    void OnValidate()
    {
        LlenarDesdeHoja(hojaTranquilo, ref cuadrosTranquilo, "Tranquilo");
        LlenarDesdeHoja(hojaEnojado, ref cuadrosEnojado, "Enojado");
        LlenarDesdeHoja(hojaFestejo, ref cuadrosFestejo, "Festejo");
    }

    void LlenarDesdeHoja(Texture2D hoja, ref Sprite[] destino, string nombre)
    {
        if (hoja == null) return;

        string ruta = UnityEditor.AssetDatabase.GetAssetPath(hoja);
        var importador = UnityEditor.AssetImporter.GetAtPath(ruta) as UnityEditor.TextureImporter;
        if (importador != null)
        {
            if (importador.textureType != UnityEditor.TextureImporterType.Sprite)
            {
                Debug.LogWarning($"[Multitud] '{hoja.name}': en su Inspector cambia Texture Type a 'Sprite (2D and UI)' y presiona Apply.", hoja);
                return;
            }
            if (importador.spriteImportMode != UnityEditor.SpriteImportMode.Multiple)
            {
                Debug.LogWarning($"[Multitud] '{hoja.name}': falta cortarla. Sprite Mode: Multiple → Apply → Sprite Editor → Slice → Apply.", hoja);
                return;
            }
        }

        var cuadros = new List<Sprite>();
        foreach (Object o in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(ruta))
            if (o is Sprite s) cuadros.Add(s);

        if (cuadros.Count == 0)
        {
            Debug.LogWarning($"[Multitud] '{hoja.name}' no tiene cuadros cortados. Ábrela en el Sprite Editor → Slice → Apply.", hoja);
            return;
        }

        cuadros.Sort((a, b) => a.rect.x.CompareTo(b.rect.x));   // tira horizontal: de izquierda a derecha

        if (destino != null && destino.Length == cuadros.Count)
        {
            bool iguales = true;
            for (int i = 0; i < destino.Length && iguales; i++) iguales = destino[i] == cuadros[i];
            if (iguales) return;
        }

        destino = cuadros.ToArray();
        avisoSinCuadros = false;
        Debug.Log($"[Multitud] {name}: {destino.Length} cuadros de '{hoja.name}' cargados en {nombre}.", this);
    }
#endif

    void OnDrawGizmos()
    {
        if (GetComponentInParent<Canvas>() != null) return;   // en interfaz el área es el RectTransform
        Gizmos.color = Color.yellow;
        Vector3 tam = new Vector3(ancho, profundidad, 0f) / PixelesPorUnidad;
        Gizmos.DrawWireCube(transform.position + tam / 2f, tam);
    }
}
