using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Director de la interfaz: el único script que escucha a IGameState y reparte lo que pasa
// entre paneles, pantallas, audio y tutorial. Los demás scripts de UI no se suscriben a nada.
//
// Flujo: la escena arranca con el objeto "GameState" APAGADO y la pantalla de título encendida.
// Al pulsar Jugar se enciende el GameState (que empieza el día 1) y se conectan los eventos.
// Así el reloj no corre mientras se ve el título, sin tocar el contrato IGameState.
[DefaultExecutionOrder(-50)]
public class Director : MonoBehaviour
{
    public static Director Instancia { get; private set; }

    [Header("Estado (apagado en la escena hasta pulsar Jugar)")]
    public GameObject estadoJuego;

    [Header("Piezas")]
    public Navegador navegador;
    public PanelTrono trono;
    public PanelBalcon[] balcones;
    public PanelBoveda boveda;
    public PantallaTitulo titulo;
    public PantallaFinDia finDia;
    public PantallaFinal final;
    public CuadroDialogo dialogo;
    public TutorialTobias tutorial;
    public AudioManager sonido;

    IGameState estado;
    Peticion ultima;
    int diaVisto;
    int sies, noes, porReloj;
    bool conectado;

    void Awake()
    {
        Instancia = this;
        Application.targetFrameRate = 60;
        Application.runInBackground = true;
    }

    void Start()
    {
        titulo.gameObject.SetActive(true);
        finDia.gameObject.SetActive(false);
        final.gameObject.SetActive(false);
        navegador.Bloqueado = true;
        navegador.VistaCambio += tutorial.AlCambiarVista;
        navegador.VistaCambio += _ => RefrescarVistas();
    }

    void OnDestroy()
    {
        Desconectar();
        if (Instancia == this) Instancia = null;
    }

    // ---------- flujo ----------

    public void Comenzar()
    {
        if (conectado) return;
        titulo.gameObject.SetActive(false);
        estadoJuego.SetActive(true);        // Awake del estado: fija Juego.Estado; su Start empieza el día 1

        estado = Juego.Estado;
        if (estado == null)
        {
            Debug.LogError("[Director] El objeto GameState no fijó Juego.Estado. ¿Tiene un componente IGameState?");
            return;
        }

        Conectar();
        sonido.Iniciar();
        navegador.Bloqueado = false;
        navegador.IrForzado(Vista.Trono);

        // Si el estado ya arrancó el día antes de conectarnos, no nos lo perdemos.
        if (estado.PeticionActual != null) AlLlegarPeticion(estado.PeticionActual);
        RefrescarVistas();
    }

    public void ContinuarDia(PantallaFinDia pantalla)
    {
        pantalla.gameObject.SetActive(false);
        navegador.Bloqueado = false;
        navegador.IrForzado(Vista.Trono);
        estado.ContinuarSiguienteDia();
    }

    public void Reiniciar()
    {
        Desconectar();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ---------- eventos del juego ----------

    void Conectar()
    {
        estado.PeticionNueva += AlLlegarPeticion;
        estado.PeticionResuelta += AlResolverPeticion;
        estado.EstadoCambio += RefrescarVistas;
        estado.FinDelDia += AlTerminarElDia;
        estado.FinDelJuego += AlTerminarElJuego;
        conectado = true;
    }

    void Desconectar()
    {
        if (!conectado || estado == null) return;
        estado.PeticionNueva -= AlLlegarPeticion;
        estado.PeticionResuelta -= AlResolverPeticion;
        estado.EstadoCambio -= RefrescarVistas;
        estado.FinDelDia -= AlTerminarElDia;
        estado.FinDelJuego -= AlTerminarElJuego;
        conectado = false;
    }

    void AlLlegarPeticion(Peticion peticion)
    {
        if (peticion == null || peticion == ultima) return;
        ultima = peticion;

        if (estado.Dia != diaVisto)
        {
            diaVisto = estado.Dia;
            sonido.Efecto("sfx_bell");      // campana de inicio de día
        }

        trono.Presentar(peticion);
        sonido.Efecto("sfx_steps");
        sonido.MusicaDuque(peticion.EsDuque);
        if (!peticion.EsDuque) tutorial.AlLlegarMensajero();
        RefrescarVistas();
    }

    void AlResolverPeticion(bool si)
    {
        if (ultima == null) return;
        bool esDuque = ultima.EsDuque;
        bool alAcabarseElReloj = !esDuque && estado.TiempoRestante <= 0.05f;

        if (!esDuque)
        {
            if (alAcabarseElReloj) porReloj++;
            else if (si) sies++;
            else noes++;
        }

        trono.Resolver(si);
        sonido.Efecto(si ? "sfx_stamp_yes" : "sfx_stamp_no");
        sonido.Efecto("sfx_steps", 0.5f);
        if (alAcabarseElReloj) dialogo.MostrarYa(Textos.SilencioEsSi);
        ultima = null;
    }

    void AlTerminarElDia(ResumenDia resumen)
    {
        navegador.Bloqueado = true;
        StartCoroutine(MostrarCuandoTermineElSello(() =>
        {
            trono.Limpiar();
            dialogo.Ocultar();
            navegador.IrForzado(Vista.Trono);
            sonido.Efecto("sfx_bell");      // campana de fin de día
            finDia.Mostrar(resumen);
        }));
    }

    void AlTerminarElJuego(Final resultado)
    {
        navegador.Bloqueado = true;
        sonido.Detener();
        StartCoroutine(MostrarCuandoTermineElSello(() =>
        {
            trono.Limpiar();
            dialogo.Ocultar();
            finDia.gameObject.SetActive(false);
            final.Mostrar(resultado, sies, noes, porReloj);
        }));
    }

    // Deja que se vea el sello (y el "no" al Duque) antes de tapar todo con una pantalla.
    IEnumerator MostrarCuandoTermineElSello(System.Action mostrar)
    {
        yield return null;
        while (trono.EnTransicion) yield return null;
        yield return new WaitForSeconds(0.25f);
        mostrar();
    }

    void RefrescarVistas()
    {
        if (estado == null) return;
        foreach (PanelBalcon b in balcones) b.Refrescar();
        boveda.Refrescar();
    }
}
