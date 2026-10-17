using UnityEngine;
using UnityEngine.UI;

// D2-04: cada balcón muestra crowd_[grupo]_[EstadoVisual] y se actualiza con EstadoCambio.
// Si existe la segunda versión (crowd_[grupo]_[estado]_b) alterna los 2 cuadros (A2-06).
public class PanelBalcon : MonoBehaviour
{
    public Grupo grupo;
    public Image multitud;
    public Text titulo;
    public float segundosPorCuadro = 0.6f;

    Sprite cuadroA, cuadroB;
    int estadoMostrado = -1;

    public void Refrescar()
    {
        IGameState e = Juego.Estado;
        if (e == null) return;

        int estado = Mathf.Clamp(e.EstadoVisual(grupo), 0, 4);
        if (estado == estadoMostrado && cuadroA != null) return;
        estadoMostrado = estado;

        string baseNombre = "crowd_" + Textos.PrefijoMultitud(grupo) + "_" + estado;
        cuadroA = ArteCatalogo.ObtenerSprite(baseNombre);
        cuadroB = ArteCatalogo.ObtenerSprite(baseNombre + "_b");
        multitud.enabled = cuadroA != null;
        if (cuadroA != null) multitud.sprite = cuadroA;
    }

    void Update()
    {
        if (cuadroA == null || cuadroB == null) return;
        bool segundo = Mathf.FloorToInt(Time.time / segundosPorCuadro) % 2 == 1;
        multitud.sprite = segundo ? cuadroB : cuadroA;
    }
}
