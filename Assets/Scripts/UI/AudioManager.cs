using UnityEngine;

// D2-09: ambiente del balcón que se mira, tic-tac que se acelera, sellos, pasos, campana
// y golpes en la puerta desde el trono si algún grupo está en estado 0.
// Los clips se buscan por nombre en el ArteCatalogo (amb_crowd_0..4, sfx_tick, sfx_stamp_yes,
// sfx_stamp_no, sfx_steps, sfx_bell, sfx_door_knock, music_loop y, opcional, music_duke).
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instancia { get; private set; }

    public AudioSource musica;
    public AudioSource ambiente;
    public AudioSource efectos;

    const float VolumenMusica = 0.5f;
    const float VolumenAmbiente = 0.8f;

    bool sonando;
    bool modoDuque;
    string ambienteActual = "";
    string ambienteDeseado = "";
    float proximoTic;
    float proximoGolpe;

    void Awake()
    {
        Instancia = this;
        musica.loop = true;
        ambiente.loop = true;
        musica.playOnAwake = ambiente.playOnAwake = efectos.playOnAwake = false;
        ambiente.volume = 0f;
    }

    void OnDestroy() { if (Instancia == this) Instancia = null; }

    // Se llama al pulsar Jugar (el navegador exige un clic antes de reproducir audio).
    public void Iniciar()
    {
        sonando = true;
        modoDuque = false;
        musica.pitch = 1f;
        musica.volume = VolumenMusica;
        AudioClip clip = ArteCatalogo.ObtenerSonido("music_loop");
        if (clip != null) { musica.clip = clip; musica.Play(); }
    }

    public void Detener()
    {
        sonando = false;
        musica.Stop();
        ambiente.Stop();
        ambienteActual = ambienteDeseado = "";
    }

    public void Efecto(string nombre, float volumen = 1f)
    {
        AudioClip clip = ArteCatalogo.ObtenerSonido(nombre);
        if (clip != null) efectos.PlayOneShot(clip, volumen);
    }

    public void MusicaDuque(bool activa)
    {
        if (modoDuque == activa) return;
        modoDuque = activa;

        AudioClip especial = activa ? ArteCatalogo.ObtenerSonido("music_duke") : null;
        AudioClip clip = especial != null ? especial : ArteCatalogo.ObtenerSonido("music_loop");
        if (clip != null && musica.clip != clip) { musica.clip = clip; musica.Play(); }

        // Sin música propia del Duque: la misma pista, más lenta y grave.
        musica.pitch = activa && especial == null ? 0.8f : 1f;
        musica.volume = activa ? VolumenMusica * 0.8f : VolumenMusica;
    }

    void Update()
    {
        IGameState e = Juego.Estado;
        if (!sonando || e == null) return;

        Tic(e);
        Golpes(e);
        Ambiente(e);
    }

    void Tic(IGameState e)
    {
        Peticion p = e.PeticionActual;
        if (p == null || p.EsDuque || e.TiempoTotal <= 0f || e.TiempoRestante <= 0f) return;
        if (Time.time < proximoTic) return;

        Efecto("sfx_tick", 0.6f);
        // 1 s entre tics hasta quedar ~9 s, y baja a 0,3 s al final.
        float intervalo = Mathf.Lerp(0.3f, 1f, Mathf.Clamp01((e.TiempoRestante - 1f) / 8f));
        proximoTic = Time.time + intervalo;
    }

    void Golpes(IGameState e)
    {
        Navegador nav = Navegador.Instancia;
        if (nav == null || nav.Bloqueado || nav.Actual != Vista.Trono) return;

        bool alguien = e.EstadoVisual(Grupo.Campesinos) == 0 || e.EstadoVisual(Grupo.Soldados) == 0 || e.EstadoVisual(Grupo.Iglesia) == 0;
        if (!alguien || Time.time < proximoGolpe) return;

        Efecto("sfx_door_knock", 0.9f);
        proximoGolpe = Time.time + Random.Range(2.2f, 3.8f);
    }

    void Ambiente(IGameState e)
    {
        Navegador nav = Navegador.Instancia;
        ambienteDeseado = "";
        if (nav != null && !nav.Bloqueado && !modoDuque)
        {
            switch (nav.Actual)
            {
                case Vista.Cultivos: ambienteDeseado = "amb_crowd_" + e.EstadoVisual(Grupo.Campesinos); break;
                case Vista.Armeria: ambienteDeseado = "amb_crowd_" + e.EstadoVisual(Grupo.Soldados); break;
                case Vista.Catedral: ambienteDeseado = "amb_crowd_" + e.EstadoVisual(Grupo.Iglesia); break;
            }
        }

        // Fundido: baja el volumen, cambia de clip y vuelve a subir.
        if (ambienteDeseado != ambienteActual)
        {
            ambiente.volume = Mathf.MoveTowards(ambiente.volume, 0f, Time.deltaTime * 4f);
            if (ambiente.volume <= 0.01f)
            {
                ambienteActual = ambienteDeseado;
                AudioClip clip = ArteCatalogo.ObtenerSonido(ambienteActual);
                if (clip != null) { ambiente.clip = clip; ambiente.Play(); }
                else ambiente.Stop();
            }
        }
        else
        {
            float objetivo = string.IsNullOrEmpty(ambienteActual) ? 0f : VolumenAmbiente;
            ambiente.volume = Mathf.MoveTowards(ambiente.volume, objetivo, Time.deltaTime * 4f);
        }
    }
}
