using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Versión falsa de IGameState para que el Desarrollador 2 construya las pantallas
// sin esperar la lógica real. Usa 3 peticiones de prueba por día y el reloj real.
//
// Uso: poner este componente en un GameObject vacío de la escena (ej. "GameState").
// El Desarrollador 2 accede siempre por Juego.Estado.
//
// Teclas de depuración (no chocan con 1-4, Espacio, S y N del juego):
//   F1 / F2  campesinos -10 / +10
//   F3 / F4  soldados   -10 / +10
//   F5 / F6  iglesia    -10 / +10
//   F7 / F8  oro        -20 / +20
//   F9       terminar el día ahora
//   F10      traer al Duque Ricardo
//   F11      disparar el siguiente final (recorre los 6)
[DefaultExecutionOrder(-100)]
public class GameStateFalso : MonoBehaviour, IGameState
{
    [SerializeField] int peticionesPorDia = 3;
    [SerializeField] bool registrarEnConsola = true;

    int dia = 1;
    int oro = Reglas.OroInicial;
    readonly int[] animo = { Reglas.AnimoInicial, Reglas.AnimoInicial, Reglas.AnimoInicial };

    Peticion peticionActual;
    float tiempoRestante;
    float tiempoTotal;
    int atendidasHoy;
    bool terminado;
    int siguienteFinal;
    ResumenDia resumen = new ResumenDia();
    readonly ResumenDia total = new ResumenDia();
    int indicePrueba;

    public ResumenDia ResumenTotal { get { total.dia = dia; return total; } }

    public int Dia => dia;
    public int Oro => oro;
    public int Animo(Grupo grupo) => animo[(int)grupo];
    public int EstadoVisual(Grupo grupo) => Reglas.EstadoVisual(animo[(int)grupo]);
    public int NivelOro => Reglas.NivelOro(oro);
    public Peticion PeticionActual => peticionActual;
    public float TiempoRestante => tiempoRestante;
    public float TiempoTotal => tiempoTotal;

    public event Action<Peticion> PeticionNueva;
    public event Action<bool> PeticionResuelta;
    public event Action EstadoCambio;
    public event Action<ResumenDia> FinDelDia;
    public event Action<Final> FinDelJuego;

    static readonly Peticion[] peticionesDePrueba =
    {
        new Peticion { id = "prueba_campesinos", grupo = "campesinos", mensajero = "peasant",
            texto = "Majestad, la helada arruinó las semillas. ¿Nos dejará morir de hambre?",
            si = new Efectos { campesinos = 12, oro = -15 }, no = new Efectos { campesinos = -10 } },
        new Peticion { id = "prueba_soldados", grupo = "soldados", mensajero = "soldier",
            texto = "¡Los bárbaros se acercan! Necesitamos espadas nuevas hoy mismo.",
            si = new Efectos { soldados = 10, oro = -30 }, no = new Efectos { soldados = -8 } },
        new Peticion { id = "prueba_iglesia", grupo = "iglesia", mensajero = "monk",
            texto = "Su padre prometió un campanario de oro. Un rey sabio no rompería esa promesa.",
            si = new Efectos { iglesia = 15, oro = -40 }, no = new Efectos { iglesia = -12 } },
    };

    static readonly Peticion peticionDuque = new Peticion
    {
        id = "duque", grupo = "especial", mensajero = "duke",
        texto = "Sobrino, has hecho tanto... Déjame cargar con la corona por ti.",
    };

    void Awake()
    {
        Juego.Estado = this;
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
        LeerTeclasDeDepuracion();

        if (terminado || peticionActual == null || peticionActual.EsDuque) return;

        tiempoRestante -= Time.deltaTime;
        if (tiempoRestante <= 0f)
        {
            tiempoRestante = 0f;
            Resolver(true, porReloj: true);
        }
    }

    public void Decidir(bool si)
    {
        if (terminado || peticionActual == null) return;
        Resolver(si, porReloj: false);
    }

    public void ContinuarSiguienteDia()
    {
        if (terminado || peticionActual != null) return;

        if (dia >= Reglas.Dias)
        {
            TraerDuque();
            return;
        }

        dia++;
        IniciarDia();
    }

    void IniciarDia()
    {
        atendidasHoy = 0;
        resumen = new ResumenDia { dia = dia };
        Log($"Empieza el día {dia}");
        SiguientePeticion();
    }

    void SiguientePeticion()
    {
        peticionActual = peticionesDePrueba[indicePrueba % peticionesDePrueba.Length];
        indicePrueba++;
        tiempoTotal = Reglas.TiempoPorDia(dia);
        tiempoRestante = tiempoTotal;
        Log($"Petición nueva: {peticionActual.id} ({tiempoTotal} s)");
        PeticionNueva?.Invoke(peticionActual);
    }

    void TraerDuque()
    {
        peticionActual = peticionDuque;
        tiempoTotal = 0f;
        tiempoRestante = 0f;
        Log("Llega el Duque Ricardo");
        PeticionNueva?.Invoke(peticionActual);
    }

    void Resolver(bool si, bool porReloj)
    {
        Peticion peticion = peticionActual;
        peticionActual = null;

        if (peticion.EsDuque)
        {
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
        CambiarOro(efectos.oro);

        Log($"{peticion.id}: {(porReloj ? "aprobada por el reloj" : si ? "SÍ" : "NO")}");
        PeticionResuelta?.Invoke(si);
        EstadoCambio?.Invoke();

        if (RevisarDerrota()) return;

        atendidasHoy++;
        if (atendidasHoy >= peticionesPorDia) CerrarDia();
        else SiguientePeticion();
    }

    void CerrarDia()
    {
        peticionActual = null;
        tiempoRestante = 0f;

        for (int g = 0; g < animo.Length; g++)
            if (animo[g] < Reglas.LimiteCalma) CambiarAnimo((Grupo)g, Reglas.CalmaFinDelDia);
        CambiarOro(Reglas.ImpuestosFinDelDia);

        resumen.comentarioTobias = "Otro día más en el trono, majestad.";
        Log($"Fin del día {dia}: {resumen.sies} sí, {resumen.noes} no, {resumen.porReloj} por reloj");
        EstadoCambio?.Invoke();
        FinDelDia?.Invoke(resumen);
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

    void Terminar(Final final)
    {
        terminado = true;
        peticionActual = null;
        Log($"Fin del juego: {final}");
        FinDelJuego?.Invoke(final);
    }

    void CambiarAnimo(Grupo grupo, int cambio)
    {
        if (cambio == 0) return;
        animo[(int)grupo] = Mathf.Clamp(animo[(int)grupo] + cambio, 0, 100);
        switch (grupo)
        {
            case Grupo.Campesinos: resumen.cambioCampesinos += cambio; break;
            case Grupo.Soldados: resumen.cambioSoldados += cambio; break;
            default: resumen.cambioIglesia += cambio; break;
        }
    }

    void CambiarOro(int cambio)
    {
        if (cambio == 0) return;
        oro = Mathf.Max(0, oro + cambio);
        resumen.cambioOro += cambio;
    }

    void CambioManual(Grupo grupo, int cambio)
    {
        CambiarAnimo(grupo, cambio);
        Log($"[F] {grupo}: {animo[(int)grupo]} (estado visual {EstadoVisual(grupo)})");
        EstadoCambio?.Invoke();
    }

    void OroManual(int cambio)
    {
        CambiarOro(cambio);
        Log($"[F] Oro: {oro} (nivel {NivelOro})");
        EstadoCambio?.Invoke();
    }

    void LeerTeclasDeDepuracion()
    {
        if (Presionada(1)) CambioManual(Grupo.Campesinos, -10);
        if (Presionada(2)) CambioManual(Grupo.Campesinos, 10);
        if (Presionada(3)) CambioManual(Grupo.Soldados, -10);
        if (Presionada(4)) CambioManual(Grupo.Soldados, 10);
        if (Presionada(5)) CambioManual(Grupo.Iglesia, -10);
        if (Presionada(6)) CambioManual(Grupo.Iglesia, 10);
        if (Presionada(7)) OroManual(-20);
        if (Presionada(8)) OroManual(20);
        if (Presionada(9) && !terminado && peticionActual != null && !peticionActual.EsDuque) CerrarDia();
        if (Presionada(10) && !terminado) TraerDuque();
        if (Presionada(11))
        {
            Final final = (Final)(siguienteFinal % 6);
            siguienteFinal++;
            terminado = false;
            Terminar(final);
        }
    }

    // Tecla F1 a F11, compatible con el Input System nuevo y con el antiguo.
    static bool Presionada(int teclaF)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard teclado = Keyboard.current;
        if (teclado == null) return false;
        switch (teclaF)
        {
            case 1: return teclado.f1Key.wasPressedThisFrame;
            case 2: return teclado.f2Key.wasPressedThisFrame;
            case 3: return teclado.f3Key.wasPressedThisFrame;
            case 4: return teclado.f4Key.wasPressedThisFrame;
            case 5: return teclado.f5Key.wasPressedThisFrame;
            case 6: return teclado.f6Key.wasPressedThisFrame;
            case 7: return teclado.f7Key.wasPressedThisFrame;
            case 8: return teclado.f8Key.wasPressedThisFrame;
            case 9: return teclado.f9Key.wasPressedThisFrame;
            case 10: return teclado.f10Key.wasPressedThisFrame;
            case 11: return teclado.f11Key.wasPressedThisFrame;
            default: return false;
        }
#else
        return Input.GetKeyDown(KeyCode.F1 + (teclaF - 1));
#endif
    }

    void Log(string mensaje)
    {
        if (registrarEnConsola) Debug.Log($"[GameStateFalso] {mensaje}");
    }
}
