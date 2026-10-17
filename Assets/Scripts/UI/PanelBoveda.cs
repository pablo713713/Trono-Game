using UnityEngine;
using UnityEngine.UI;

// D2-05: la bóveda muestra gold_[NivelOro] y una frase de Inés según el nivel.
public class PanelBoveda : MonoBehaviour
{
    public Image oro;
    public Text fraseInes;

    int nivelMostrado = -1;

    public void Refrescar()
    {
        IGameState e = Juego.Estado;
        if (e == null) return;

        int nivel = Mathf.Clamp(e.NivelOro, 0, 4);
        if (nivel == nivelMostrado) return;
        nivelMostrado = nivel;

        Sprite s = ArteCatalogo.ObtenerSprite("gold_" + nivel);
        oro.enabled = s != null;
        if (s != null) oro.sprite = s;
        fraseInes.text = Textos.FraseInes(nivel);
    }
}
