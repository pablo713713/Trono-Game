using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Cuadro de diálogo de Tobías en la franja superior (vive en el HUD, visible en todas las vistas).
// No pausa el juego: el reloj sigue corriendo. Clic para saltar al siguiente mensaje.
public class CuadroDialogo : MonoBehaviour
{
    public GameObject contenedor;
    public Text texto;
    public Button boton;

    struct Mensaje { public string texto; public float segundos; }
    readonly Queue<Mensaje> cola = new Queue<Mensaje>();
    float restante;
    bool mostrando;

    void Start()
    {
        boton.onClick.AddListener(Siguiente);
        contenedor.SetActive(false);
    }

    public void Encolar(string mensaje, float segundos = 6f)
    {
        cola.Enqueue(new Mensaje { texto = mensaje, segundos = segundos });
        if (!mostrando) Siguiente();
    }

    // Reemplaza lo que se esté mostrando (para avisos urgentes).
    public void MostrarYa(string mensaje, float segundos = 4f)
    {
        cola.Clear();
        Poner(new Mensaje { texto = mensaje, segundos = segundos });
    }

    public void Ocultar()
    {
        cola.Clear();
        mostrando = false;
        contenedor.SetActive(false);
    }

    void Update()
    {
        if (!mostrando) return;
        restante -= Time.deltaTime;
        if (restante <= 0f) Siguiente();
    }

    void Siguiente()
    {
        if (cola.Count == 0)
        {
            mostrando = false;
            contenedor.SetActive(false);
            return;
        }
        Poner(cola.Dequeue());
    }

    void Poner(Mensaje m)
    {
        mostrando = true;
        restante = m.segundos;
        texto.text = m.texto;
        contenedor.SetActive(true);
    }
}
