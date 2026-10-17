using UnityEngine;
using UnityEngine.UI;

// D2-03: reloj de arena según TiempoRestante. Visible en todas las vistas (vive en el HUD).
// Se oculta en la escena del Duque (sin reloj).
public class RelojArena : MonoBehaviour
{
    public Image imagen;
    public int cuadros = 8;

    Sprite[] frames;

    void Update()
    {
        IGameState e = Juego.Estado;
        bool activo = e != null && e.PeticionActual != null && !e.PeticionActual.EsDuque && e.TiempoTotal > 0f;
        imagen.enabled = activo;
        if (!activo) return;

        if (frames == null) frames = ArteCatalogo.Cortar(ArteCatalogo.ObtenerSprite("ui_hourglass"), cuadros);
        if (frames == null) { imagen.enabled = false; return; }

        float restante = Mathf.Clamp01(e.TiempoRestante / e.TiempoTotal);
        int indice = Mathf.Clamp(Mathf.FloorToInt((1f - restante) * cuadros), 0, cuadros - 1);
        imagen.sprite = frames[indice];

        // Parpadeo rojizo al final para meter presión.
        if (e.TiempoRestante <= 5f)
        {
            float pulso = Mathf.PingPong(Time.time * 4f, 1f);
            imagen.color = Color.Lerp(Color.white, new Color(1f, 0.45f, 0.4f), pulso);
        }
        else imagen.color = Color.white;
    }
}
