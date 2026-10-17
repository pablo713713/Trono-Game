using System.Collections.Generic;
using UnityEngine;

// D1-10. Multitud de un balcón armada con sprites sueltos.
// Lee Juego.Estado.EstadoVisual(grupo) y cambia cuánta gente hay y cómo se mueve:
//   4 celebrando  -> gente moderada, animación de festejo, saltitos
//   3 tranquilos  -> poca gente, animación lenta
//   2 murmullos   -> gente media, animación normal
//   1 carteles    -> mucha gente, animación enojada
//   0 antorchas   -> balcón lleno, gente enojada y agitada, todos al frente
//
// Uso:
// 1. Crear un GameObject vacío dentro del balcón y agregarle este componente.
// 2. Elegir el grupo y poner la imagen completa de cada animación en "Hoja Tranquilo",
//    "Hoja Enojado" y "Hoja Festejo". Las listas de cuadros se llenan solas.
//    Si falta "Festejo" o "Enojado", se usa "Tranquilo".
// 3. Colocar el objeto donde va el borde inferior izquierdo de la franja de la multitud.
//    El rectángulo amarillo en la vista Scene muestra el área.
// Sin juego corriendo se puede probar con "Usar estado de prueba".
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

    [Header("Área de la multitud (en unidades; 16 píxeles = 1 unidad)")]
    [SerializeField] float ancho = 20f;        // 320 px
    [SerializeField] float profundidad = 3f;   // filas de atrás hacia adelante
    [SerializeField] int maximoDePersonas = 30;

    [Header("Cantidad de personas por estado (0 = antorchas ... 4 = celebrando)")]
    [SerializeField] int[] personasPorEstado = { 30, 20, 12, 6, 10 };

    [Header("Orden de dibujo")]
    [SerializeField] string sortingLayer = "Default";
    [SerializeField] int ordenBase = 10;

    [Header("Prueba sin GameState")]
    [SerializeField] bool usarEstadoDePrueba;
    [SerializeField, Range(0, 4)] int estadoDePrueba = 2;

    // Velocidad de animación (cuadros por segundo) y salto por estado.
    static readonly float[] fpsPorEstado = { 14f, 10f, 7f, 4f, 9f };
    static readonly float[] saltoPorEstado = { 0.18f, 0.06f, 0f, 0f, 0.12f };

    class Persona
    {
        public SpriteRenderer render;
        public Vector3 posicionBase;
        public float desfase;
        public float velocidad;
    }

    readonly List<Persona> personas = new List<Persona>();
    int estadoActual = -1;
    bool suscrito;
    bool avisoSinCuadros;

    void Awake()
    {
        CrearPersonas();
    }

    void Start()
    {
        Suscribir();
        Refrescar();
    }

    void OnEnable()
    {
        // Los balcones se activan y desactivan al navegar: al volver, mostrar el estado actual.
        if (personas.Count > 0) Refrescar();
    }

    void OnDestroy()
    {
        if (suscrito && Juego.Estado != null) Juego.Estado.EstadoCambio -= Refrescar;
    }

    void Suscribir()
    {
        if (suscrito || Juego.Estado == null) return;
        Juego.Estado.EstadoCambio += Refrescar;
        suscrito = true;
    }

    int LeerEstado()
    {
        if (usarEstadoDePrueba || Juego.Estado == null) return estadoDePrueba;
        return Juego.Estado.EstadoVisual(grupo);
    }

    // Crea todas las personas una sola vez y luego solo se muestran u ocultan.
    // Las posiciones usan una semilla fija por grupo: la multitud no "salta" de lugar.
    void CrearPersonas()
    {
        var azar = new System.Random(1000 + (int)grupo);
        for (int i = 0; i < maximoDePersonas; i++)
        {
            var objeto = new GameObject($"Persona_{i:00}");
            objeto.transform.SetParent(transform, false);

            var render = objeto.AddComponent<SpriteRenderer>();
            render.sortingLayerName = sortingLayer;
            render.flipX = azar.NextDouble() < 0.5;
            float brillo = 0.85f + 0.15f * (float)azar.NextDouble();
            render.color = new Color(brillo, brillo, brillo, 1f);

            personas.Add(new Persona
            {
                render = render,
                posicionBase = new Vector3((float)azar.NextDouble() * ancho, (float)azar.NextDouble() * profundidad, 0f),
                desfase = (float)azar.NextDouble() * 10f,
                velocidad = 0.85f + 0.3f * (float)azar.NextDouble(),
            });
            objeto.SetActive(false);
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
            personas[i].render.gameObject.SetActive(i < visibles);

        Debug.Log($"[Multitud] {name}: estado {estado}, {visibles} personas", this);
    }

    Sprite[] CuadrosDelEstado(int estado)
    {
        Sprite[] elegidos = Limpiar(estado <= 1 ? cuadrosEnojado : estado == 4 ? cuadrosFestejo : cuadrosTranquilo);
        if (elegidos.Length > 0) return elegidos;
        return Limpiar(cuadrosTranquilo);
    }

    // Quita las casillas vacías (None) de una lista de cuadros.
    static Sprite[] Limpiar(Sprite[] cuadros)
    {
        if (cuadros == null) return new Sprite[0];
        return System.Array.FindAll(cuadros, s => s != null);
    }

    void Update()
    {
        // Sin GameState en la escena, el control "Estado De Prueba" funciona aunque la casilla esté desmarcada.
        bool modoPrueba = usarEstadoDePrueba || Juego.Estado == null;
        if (modoPrueba && estadoDePrueba != estadoActual) Refrescar();
        if (estadoActual < 0) return;

        Sprite[] cuadros = CuadrosDelEstado(estadoActual);
        if (cuadros.Length == 0)
        {
            if (!avisoSinCuadros)
            {
                Debug.LogWarning($"[Multitud] {name}: la lista 'Cuadros Tranquilo' está vacía o solo tiene casillas en None. " +
                                 "Arrastra los cuadros cortados del sprite a esa lista (con el juego detenido).", this);
                avisoSinCuadros = true;
            }
            return;
        }

        float fps = fpsPorEstado[estadoActual];
        float salto = saltoPorEstado[estadoActual];
        bool alFrente = estadoActual == 0;

        foreach (Persona p in personas)
        {
            if (!p.render.gameObject.activeSelf) continue;

            float t = Time.time * p.velocidad + p.desfase;
            int cuadro = Mathf.FloorToInt(t * fps) % cuadros.Length;
            p.render.sprite = cuadros[cuadro];

            Vector3 pos = p.posicionBase;
            if (alFrente) pos.y *= 0.4f;                       // en antorchas todos se apiñan al frente
            pos.y += Mathf.Abs(Mathf.Sin(t * 6f)) * salto;     // saltitos de enojo o de festejo
            p.render.transform.localPosition = SnapAPixel(pos);

            // Los de adelante (y más baja) se dibujan encima de los de atrás.
            p.render.sortingOrder = ordenBase + Mathf.RoundToInt((profundidad - p.posicionBase.y) * 100f);
        }
    }

    // Mantiene el pixel art nítido: posiciones en múltiplos exactos de 1 píxel.
    static Vector3 SnapAPixel(Vector3 v)
    {
        const float pixel = 1f / 16f;
        return new Vector3(Mathf.Round(v.x / pixel) * pixel, Mathf.Round(v.y / pixel) * pixel, 0f);
    }

#if UNITY_EDITOR
    // Cada vez que se cambia algo en el Inspector, llena las listas de cuadros
    // a partir de las imágenes completas. Solo corre en el editor; en el juego
    // las listas ya quedan guardadas.
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

        // Tira horizontal: de izquierda a derecha.
        cuadros.Sort((a, b) => a.rect.x.CompareTo(b.rect.x));

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
        Gizmos.color = Color.yellow;
        Vector3 centro = transform.position + new Vector3(ancho / 2f, profundidad / 2f, 0f);
        Gizmos.DrawWireCube(centro, new Vector3(ancho, profundidad, 0f));
    }
}
