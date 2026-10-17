using UnityEngine;
using UnityEngine.UI;

public class PantallaFinal : MonoBehaviour
{
    public Text titulo;
    public Text mensaje;
    public Text conteo;
    public Button reiniciar;

    float desde;

    void Start() => reiniciar.onClick.AddListener(() => Director.Instancia.Reiniciar());

    public void Mostrar(Final final, int sies, int noes, int porReloj)
    {
        gameObject.SetActive(true);
        desde = Time.unscaledTime;
        Textos.DeFinal(final, out string t, out string m, out bool victoria);
        titulo.text = t;
        titulo.color = victoria ? new Color(1f, 0.85f, 0.35f) : new Color(0.9f, 0.3f, 0.25f);
        mensaje.text = m;
        conteo.text = "Dijiste SÍ " + sies + (sies == 1 ? " vez" : " veces") +
                      "\nDijiste NO " + noes + (noes == 1 ? " vez" : " veces") +
                      "\nDejaste que el reloj decidiera " + porReloj + (porReloj == 1 ? " vez" : " veces");
    }

    void Update()
    {
        if (Teclado.Presionada(Tecla.Enter) && Time.unscaledTime - desde > 0.6f) Director.Instancia.Reiniciar();
    }
}
