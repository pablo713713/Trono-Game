using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// GameState real: lee Assets/Data/requests.json y aplica todas las reglas del juego.
// Historias D1-03 a D1-07: efectos, reloj, 5 días, derrotas, Duque y exigencia.
//
// Uso: poner este componente en el GameObject "GameState" en lugar de GameStateFalso
// (nunca los dos a la vez) y arrastrar requests.json al campo "Archivo Peticiones".
// El Desarrollador 2 no cambia nada: sigue usando Juego.Estado.
//
// Para "Jugar de nuevo": recargar la escena (SceneManager.LoadScene).
//
// Teclas de depuración (se pueden apagar en el Inspector):
//   F9   terminar el día ahora
//   F10  traer al Duque Ricardo
[DefaultExecutionOrder(-100)]
public class GameState : MonoBehaviour, IGameState
{
    [SerializeField] TextAsset archivoPeticiones;
    [SerializeField] bool teclasDeDepuracion = true;
    [SerializeField] bool registrarEnConsola = true;

    int dia = 1;
    int oro = Reglas.OroInicial;
    readonly int[] animo = { Reglas.AnimoInicial, Reglas.AnimoInicial, Reglas.AnimoInicial };
    readonly int[] exigencia = new int[3];

    readonly List<Peticion> todas = new List<Peticion>();
    readonly HashSet<string> usadas = new HashSet<string>();
    readonly Queue<Peticion> colaDelDia = new Queue<Peticion>();

    Peticion peticionActual;
    float tiempoRestante;
    float tiempoTotal;
    bool terminado;
    bool diaCerrado;
    ResumenDia resumen = new ResumenDia();
    readonly ResumenDia total = new ResumenDia();

    static readonly Peticion peticionDuque = new Peticion
    {
        id = "duque", grupo = "especial", mensajero = "duke",
        texto = "Sobrino, cinco días cargando con este reino... Te ves cansado. " +
                "Déjame aliviarte la carga: dame la corona y yo me encargo de todo.",
    };

    // ---------- IGameState ----------

    public int Dia => dia;
    public int Oro => oro;
    public int Animo(Grupo grupo) => animo[(int)grupo];
    public int EstadoVisual(Grupo grupo) => Reglas.EstadoVisual(animo[(int)grupo]);
    public int NivelOro => Reglas.NivelOro(oro);
    public Peticion PeticionActual => peticionActual;
    public float TiempoRestante => tiempoRestante;
    public float TiempoTotal => tiempoTotal;
    public ResumenDia ResumenTotal { get { total.dia = dia; return total; } }

    public event Action<Peticion> PeticionNueva;
    public event Action<bool> PeticionResuelta;
    public event Action EstadoCambio;
    public event Action<ResumenDia> FinDelDia;
    public event Action<Final> FinDelJuego;

    public void Decidir(bool si)
    {
        if (terminado || peticionActual == null) return;
        Resolver(si, porReloj: false);
    }

    public void ContinuarSiguienteDia()
    {
        if (terminado || !diaCerrado) return;
        diaCerrado = false;

        if (dia >= Reglas.Dias)
        {
            TraerDuque();
            return;
        }

        dia++;
        IniciarDia();
    }

    // ---------- Ciclo de Unity ----------

    void Awake()
    {
        if (FindAnyObjectByType<GameStateFalso>() != null)
            Debug.LogError("[GameState] Hay un GameStateFalso en la escena. Deja solo uno de los dos.");

        Juego.Estado = this;
        CargarPeticiones();
    }

    void OnDestroy()
    {
        if (ReferenceEquals(Juego.Estado, this)) Juego.Estado = null;
    }

    void Start()
    {
        IniciarDia();
    }

    void Update()
    {
        if (teclasDeDepuracion) LeerTeclasDeDepuracion();

        if (terminado || peticionActual == null || peticionActual.EsDuque) return;

        tiempoRestante -= Time.deltaTime;
        if (tiempoRestante <= 0f)
        {
            tiempoRestante = 0f;
            Resolver(true, porReloj: true);   // el silencio también es un sí
        }
    }

    // ---------- Carga de peticiones (D1-03) ----------

    void CargarPeticiones()
    {
        if (archivoPeticiones == null)
        {
            Debug.LogError("[GameState] Falta asignar requests.json en el campo 'Archivo Peticiones'.");
            return;
        }

        ListaPeticiones lista;
        try
        {
            lista = JsonUtility.FromJson<ListaPeticiones>(archivoPeticiones.text);
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameState] requests.json tiene un error de formato: {e.Message}");
            return;
        }

        var ids = new HashSet<string>();
        foreach (Peticion p in lista.peticiones)
        {
            if (string.IsNullOrEmpty(p.id) || string.IsNullOrEmpty(p.texto))
            {
                Debug.LogWarning("[GameState] Se ignoró una petición sin id o sin texto.");
                continue;
            }
            if (!ids.Add(p.id))
            {
                Debug.LogWarning($"[GameState] Id repetido en requests.json: {p.id}. Se usa solo el primero.");
                continue;
            }
            if (p.si == null) p.si = new Efectos();
            if (p.no == null) p.no = new Efectos();
            todas.Add(p);
        }

        Log($"{todas.Count} peticiones cargadas");
    }

    // ---------- Días (D1-05) ----------

    void IniciarDia()
    {
        resumen = new ResumenDia { dia = dia };
        PrepararPeticionesDelDia();
        Log($"Empieza el día {dia} con {colaDelDia.Count} mensajeros");
        SiguientePeticion();
    }

    // Elige las peticiones del día: primero una de cada grupo, luego relleno.
    // Prefiere las del día actual y, si faltan, usa las de días anteriores no usadas.
    void PrepararPeticionesDelDia()
    {
        colaDelDia.Clear();
        int necesarias = Reglas.MensajerosPorDia(dia);

        var disponibles = todas.FindAll(p => p.dia <= dia && !usadas.Contains(p.id));
        Mezclar(disponibles);
        disponibles.Sort((a, b) => (b.dia == dia).CompareTo(a.dia == dia));

        var elegidas = new List<Peticion>();
        foreach (Grupo g in Enum.GetValues(typeof(Grupo)))
        {
            Peticion p = disponibles.Find(x => x.GrupoEnum == g);
            if (p == null) continue;
            elegidas.Add(p);
            disponibles.Remove(p);
        }

        while (elegidas.Count < necesarias && disponibles.Count > 0)
        {
            elegidas.Add(disponibles[0]);
            disponibles.RemoveAt(0);
        }

        if (elegidas.Count > necesarias) elegidas.RemoveRange(necesarias, elegidas.Count - necesarias);
        if (elegidas.Count < necesarias)
            Debug.LogWarning($"[GameState] Día {dia}: hay {elegidas.Count} peticiones de {necesarias}. Agreguen más a requests.json.");

        Mezclar(elegidas);
        foreach (Peticion p in elegidas)
        {
            usadas.Add(p.id);
            colaDelDia.Enqueue(p);
        }
    }

    void SiguientePeticion()
    {
        if (colaDelDia.Count == 0)
        {
            CerrarDia();
            return;
        }

        peticionActual = colaDelDia.Dequeue();
        tiempoTotal = Reglas.TiempoPorDia(dia);
        tiempoRestante = tiempoTotal;
        Log($"Petición nueva: {peticionActual.id} ({tiempoTotal} s)");
        PeticionNueva?.Invoke(peticionActual);
    }

    void CerrarDia()
    {
        peticionActual = null;
        tiempoRestante = 0f;
        diaCerrado = true;

        for (int g = 0; g < animo.Length; g++)
            if (animo[g] < Reglas.LimiteCalma) CambiarAnimo((Grupo)g, Reglas.CalmaFinDelDia);
        CambiarOro(Reglas.ImpuestosFinDelDia);

        resumen.comentarioTobias = ComentarioDeTobias();
        Log($"Fin del día {dia}: {resumen.sies} sí, {resumen.noes} no, {resumen.porReloj} por reloj. Oro {oro}");
        EstadoCambio?.Invoke();
        FinDelDia?.Invoke(resumen);
    }

    string ComentarioDeTobias()
    {
        if (resumen.porReloj > 0) return "El silencio también es un sí, majestad.";
        if (oro < 30) return "Inés dice que la bóveda suena hueca, majestad.";
        if (resumen.sies > resumen.noes * 2) return "Su padre habría hecho lo mismo... y la bóveda lo sabe.";
        if (resumen.noes > resumen.sies * 2) return "El pueblo murmura, pero el reino sigue en pie.";
        return "Un día difícil, majestad. Mañana habrá más fila.";
    }

    // ---------- Decisiones, exigencia y derrotas (D1-03, D1-04, D1-06, D1-07) ----------

    void Resolver(bool si, bool porReloj)
    {
        Peticion peticion = peticionActual;
        peticionActual = null;
        tiempoRestante = 0f;

        if (peticion.EsDuque)
        {
            if (si) total.sies++; else total.noes++;
            Log($"El Duque: {(si ? "SÍ, le diste la corona" : "NO")}");
            PeticionResuelta?.Invoke(si);
            Terminar(si ? Final.CedioLaCorona : Final.Victoria);
            return;
        }

        if (porReloj) { resumen.porReloj++; total.porReloj++; }
        else if (si) { resumen.sies++; total.sies++; }
        else { resumen.noes++; total.noes++; }

        Efectos efectos = si ? peticion.si : peticion.no;
        Grupo? grupo = peticion.GrupoEnum;

        // Exigencia: cada sí anterior a este grupo encarece su costo en oro.
        int cambioOro = efectos.oro;
        if (grupo.HasValue && cambioOro < 0)
        {
            float multiplicador = 1f + Reglas.ExigenciaPorSi * exigencia[(int)grupo.Value];
            cambioOro = Mathf.RoundToInt(cambioOro * multiplicador);
        }

        CambiarAnimo(Grupo.Campesinos, efectos.campesinos);
        CambiarAnimo(Grupo.Soldados, efectos.soldados);
        CambiarAnimo(Grupo.Iglesia, efectos.iglesia);
        CambiarOro(cambioOro);

        if (grupo.HasValue)
        {
            int g = (int)grupo.Value;
            exigencia[g] = si ? exigencia[g] + 1 : Mathf.Max(0, exigencia[g] - 1);
        }

        Log($"{peticion.id}: {(porReloj ? "aprobada por el reloj" : si ? "SÍ" : "NO")} | " +
            $"oro {cambioOro:+#;-#;0} → {oro} | ánimo C{animo[0]} S{animo[1]} I{animo[2]} | " +
            $"exigencia C{exigencia[0]} S{exigencia[1]} I{exigencia[2]}");

        PeticionResuelta?.Invoke(si);
        EstadoCambio?.Invoke();

        if (RevisarDerrota()) return;
        SiguientePeticion();
    }

    bool RevisarDerrota()
    {
        for (int g = 0; g < animo.Length; g++)
        {
            if (animo[g] <= 0)
            {
                Terminar(Reglas.FinalPorRebelion((Grupo)g));
                return true;
            }
        }

        if (oro <= 0)
        {
            Terminar(Final.Bancarrota);
            return true;
        }

        return false;
    }

    void TraerDuque()
    {
        peticionActual = peticionDuque;
        tiempoTotal = 0f;
        tiempoRestante = 0f;
        Log("Llega el Duque Ricardo");
        PeticionNueva?.Invoke(peticionActual);
    }

    void Terminar(Final final)
    {
        terminado = true;
        peticionActual = null;
        colaDelDia.Clear();
        Log($"Fin del juego: {final} | total {total.sies} sí, {total.noes} no, {total.porReloj} por reloj");
        FinDelJuego?.Invoke(final);
    }

    // ---------- Utilidades ----------

    void CambiarAnimo(Grupo grupo, int cambio)
    {
        if (cambio == 0) return;
        int g = (int)grupo;
        int antes = animo[g];
        animo[g] = Mathf.Clamp(animo[g] + cambio, 0, 100);
        int real = animo[g] - antes;
        Sumar(resumen, grupo, real);
        Sumar(total, grupo, real);
    }

    static void Sumar(ResumenDia r, Grupo grupo, int cambio)
    {
        switch (grupo)
        {
            case Grupo.Campesinos: r.cambioCampesinos += cambio; break;
            case Grupo.Soldados: r.cambioSoldados += cambio; break;
            default: r.cambioIglesia += cambio; break;
        }
    }

    void CambiarOro(int cambio)
    {
        if (cambio == 0) return;
        int antes = oro;
        oro = Mathf.Max(0, oro + cambio);
        resumen.cambioOro += oro - antes;
        total.cambioOro += oro - antes;
    }

    static void Mezclar<T>(List<T> lista)
    {
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (lista[i], lista[j]) = (lista[j], lista[i]);
        }
    }

    void LeerTeclasDeDepuracion()
    {
        if (terminado) return;

        bool f9, f10;
#if ENABLE_INPUT_SYSTEM
        Keyboard teclado = Keyboard.current;
        if (teclado == null) return;
        f9 = teclado.f9Key.wasPressedThisFrame;
        f10 = teclado.f10Key.wasPressedThisFrame;
#else
        f9 = Input.GetKeyDown(KeyCode.F9);
        f10 = Input.GetKeyDown(KeyCode.F10);
#endif

        if (f9 && peticionActual != null && !peticionActual.EsDuque)
        {
            colaDelDia.Clear();
            CerrarDia();
        }
        if (f10 && !(peticionActual != null && peticionActual.EsDuque))
        {
            colaDelDia.Clear();
            diaCerrado = false;
            TraerDuque();
        }
    }

    void Log(string mensaje)
    {
        if (registrarEnConsola) Debug.Log($"[GameState] {mensaje}");
    }
}
