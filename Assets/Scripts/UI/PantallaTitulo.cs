using UnityEngine;
using UnityEngine.UI;

public class PantallaTitulo : MonoBehaviour
{
    public Button jugar;

    void Start() => jugar.onClick.AddListener(() => Director.Instancia.Comenzar());

    void Update()
    {
        if (Teclado.Presionada(Tecla.Enter) || Teclado.Presionada(Tecla.Espacio)) Director.Instancia.Comenzar();
    }
}
