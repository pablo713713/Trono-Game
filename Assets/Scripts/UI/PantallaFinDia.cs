using UnityEngine;
using UnityEngine.UI;

public class PantallaFinDia : MonoBehaviour
{
    public Text encabezado;
    public Text resumen;
    public Text comentarioTobias;
    public Button continuar;

    float desde;   // evita saltarse la pantalla con una tecla que el jugador ya venía pulsando

    void Start() => continuar.onClick.AddListener(Continuar);

    public void Mostrar(ResumenDia r)
    {
        gameObject.SetActive(true);
        desde = Time.unscaledTime;
        encabezado.text = "Fin del día " + r.dia;
        resumen.text =
            Textos.Tendencia(Grupo.Campesinos, r.cambioCampesinos) + "\n" +
            Textos.Tendencia(Grupo.Soldados, r.cambioSoldados) + "\n" +
            Textos.Tendencia(Grupo.Iglesia, r.cambioIglesia);   // versión 2: el oro ya no cambia
        comentarioTobias.text = "Tobías: \"" +
            (string.IsNullOrEmpty(r.comentarioTobias) ? Textos.ComentarioTobiasPorDefecto : r.comentarioTobias) + "\"";
    }

    void Update()
    {
        if (Teclado.Presionada(Tecla.Enter) || Teclado.Presionada(Tecla.Espacio)) Continuar();
    }

    void Continuar()
    {
        if (!gameObject.activeSelf || Time.unscaledTime - desde < 0.6f) return;
        Director.Instancia.ContinuarDia(this);
    }
}
