using UnityEngine;
using UnityEngine.UI;

// Pone en este Image el sprite del catálogo con el nombre indicado (sin extensión).
[RequireComponent(typeof(Image))]
public class SpriteDeCatalogo : MonoBehaviour
{
    public string nombre;

    void OnEnable() => Aplicar();

    public void Aplicar()
    {
        Sprite s = ArteCatalogo.ObtenerSprite(nombre);
        if (s != null) GetComponent<Image>().sprite = s;
    }
}
