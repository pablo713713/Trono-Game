using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// GameState real (versión 2): lee Assets/Data/requests.json y aplica las reglas del juego.
//
// Reglas:
// - Cada petición define cuánto cambia el ánimo de CADA grupo con el sí y con el no.
//   El oro ya no cambia: las decisiones solo afectan el ánimo.
// - Las peticiones salen al azar de todo el banco y no se repiten en una misma partida.
// - Los días se diferencian por la cantidad de peticiones (Reglas.MensajerosPorDia) y el reloj.
// - El ánimo se conserva de un día a otro (no hay calma al final del día).
// - Si el reloj se acaba, la petición se aprueba sola.
// - Si el ánimo de un grupo llega a 0, ese grupo invade el castillo y la partida termina.
// - Tras el día 5 llega el Duque Ricardo: sí = cede la corona, no = victoria.
//
// Uso: componente en el GameObject "GameState" (en lugar de GameStateFalso) con requests.json
// asignado en "Archivo Peticiones". La interfaz sigue usando Juego.Estado.
// Para "Jugar de nuevo": recargar la escena.
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
    readonly int oro = Reglas.OroInicial;
    readonly int[] animo = { Reglas.AnimoInicial, Reglas.AnimoInicial, Reglas.AnimoInicial };

    readonly List<Peticion> banco = new List<Peticion>();     // peticiones que aún no salieron
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
        // El "|" separa las dos partes que muestra la interfaz.
        texto = "Sobrino, cinco días cargando con este reino... Te ves cansado.|" +
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

    // ---------- Carga de peticiones ----------

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
            banco.Add(p);
        }

        int necesarias = 0;
        for (int d = 1; d <= Reglas.Dias; d++) necesarias += Reglas.MensajerosPorDia(d);
        if (banco.Count < necesarias)
            Debug.LogWarning($"[GameState] Hay {banco.Count} peticiones y una partida usa {necesarias}. Algunos días tendrán menos mensajeros.");

        Log($"{banco.Count} peticiones cargadas");
    }

    // ---------- Días ----------

    void IniciarDia()
    {
        resumen = new ResumenDia { dia = dia };
        PrepararPeticionesDelDia();
        Log($"Empieza el día {dia} con {colaDelDia.Count} mensajeros");
        SiguientePeticion();
    }

    // Saca al azar las peticiones del día del banco (sin repetir en la partida).
    // Si alcanza, procura que venga al menos un mensajero de cada grupo.
    void PrepararPeticionesDelDia()
    {
        colaDelDia.Clear();
        int necesarias = Mathf.Min(Reglas.MensajerosPorDia(dia), banco.Count);

        Mezclar(banco);
        var elegidas = new List<Peticion>();
        foreach (Grupo g in Enum.GetValues(typeof(Grupo)))
        {
            if (elegidas.Count >= necesarias) break;
            Peticion p = banco.Find(x => x.GrupoEnum == g);
            if (p == null) continue;
            elegidas.Add(p);
            banco.Remove(p);
        }
        while (elegidas.Count < necesarias && banco.Count > 0)
        {
            elegidas.Add(banco[0]);
            banco.RemoveAt(0);
        }

        Mezclar(elegidas);
        foreach (Peticion p in elegidas) colaDelDia.Enqueue(p);
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

        resumen.comentarioTobias = ComentarioDeTobias();
        Log($"Fin del día {dia}: {resumen.sies} sí, {resumen.noes} no, {resumen.porReloj} por reloj | " +
            $"ánimo M{animo[0]} S{animo[1]} I{animo[2]}");
        FinDelDia?.Invoke(resumen);
    }

    string ComentarioDeTobias()
    {
        if (resumen.porReloj > 0) return "El silencio también es un sí, majestad.";

        int peor = 0;
        for (int g = 1; g < animo.Length; g++) if (animo[g] < animo[peor]) peor = g;
        if (animo[peor] < 25)
        {
            switch ((Grupo)peor)
            {
                case Grupo.Campesinos: return "El mercado está a punto de estallar, majestad. Mire por las gradas.";
                case Grupo.Soldados: return "Los soldados afilan algo más que sus espadas, majestad.";
                default: return "En la catedral ya no rezan por usted, majestad.";
            }
        }

        if (resumen.sies > resumen.noes * 2) return "Su padre habría dicho que sí a todo... y mire cómo terminó.";
        if (resumen.noes > resumen.sies * 2) return "Muchos noes hoy, majestad. Que no se le vuelva costumbre.";
        return "Un día difícil, majestad. Mañana habrá más fila.";
    }

    // ---------- Decisiones y derrotas ----------

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
        CambiarAnimo(Grupo.Campesinos, efectos.campesinos);
        CambiarAnimo(Grupo.Soldados, efectos.soldados);
        CambiarAnimo(Grupo.Iglesia, efectos.iglesia);

        Log($"{peticion.id}: {(porReloj ? "aprobada por el reloj" : si ? "SÍ" : "NO")} | " +
            $"ánimo M{animo[0]} S{animo[1]} I{animo[2]}");

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
