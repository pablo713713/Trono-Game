using UnityEngine;
using UnityEngine.UI;

// Usa font_pixel.ttf (si ya existe) en este Text; si no, deja la fuente por defecto.
[RequireComponent(typeof(Text))]
public class TextoPixel : MonoBehaviour
{
    void Awake()
    {
        ArteCatalogo c = ArteCatalogo.Actual;
        if (c != null && c.fuente != null) GetComponent<Text>().font = c.fuente;
    }
}
