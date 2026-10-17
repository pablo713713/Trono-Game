using UnityEngine;
using UnityEngine.UI;

// Botón de navegación: lleva a una vista (trono, balcones, bóveda).
[RequireComponent(typeof(Button))]
public class BotonNavegacion : MonoBehaviour
{
    public Vista destino;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            if (Navegador.Instancia != null) Navegador.Instancia.Ir(destino);
        });
    }
}
