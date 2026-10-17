using System.Collections.Generic;
using UnityEngine;

// Catálogo de sprites, sonidos y fuente buscados POR NOMBRE de archivo (sin extensión).
// Así, cuando un diseñador reemplaza un placeholder, el juego lo encuentra solo
// aunque cambie la extensión (por ejemplo de .wav a .ogg).
// Lo llena el editor (Assets/Scripts/UI/Editor) y vive en Assets/Prefabs/Resources/.
public class ArteCatalogo : ScriptableObject
{
    public List<Sprite> sprites = new List<Sprite>();
    public List<AudioClip> sonidos = new List<AudioClip>();
    public Font fuente;

    static ArteCatalogo actual;
    Dictionary<string, Sprite> porNombreSprite;
    Dictionary<string, AudioClip> porNombreSonido;
    static readonly Dictionary<string, Sprite[]> cortes = new Dictionary<string, Sprite[]>();

    public static ArteCatalogo Actual
    {
        get
        {
            if (actual == null) actual = Resources.Load<ArteCatalogo>("ArteCatalogo");
            return actual;
        }
    }

    public void Invalidar()
    {
        porNombreSprite = null;
        porNombreSonido = null;
        cortes.Clear();
    }

    // Devuelve null si el archivo todavía no existe: quien llama debe tolerarlo.
    public static Sprite ObtenerSprite(string nombre)
    {
        ArteCatalogo c = Actual;
        if (c == null || string.IsNullOrEmpty(nombre)) return null;
        if (c.porNombreSprite == null)
        {
            c.porNombreSprite = new Dictionary<string, Sprite>();
            foreach (Sprite s in c.sprites) if (s != null) c.porNombreSprite[s.name] = s;
        }
        c.porNombreSprite.TryGetValue(nombre, out Sprite sprite);
        return sprite;
    }

    public static AudioClip ObtenerSonido(string nombre)
    {
        ArteCatalogo c = Actual;
        if (c == null || string.IsNullOrEmpty(nombre)) return null;
        if (c.porNombreSonido == null)
        {
            c.porNombreSonido = new Dictionary<string, AudioClip>();
            foreach (AudioClip a in c.sonidos) if (a != null) c.porNombreSonido[a.name] = a;
        }
        c.porNombreSonido.TryGetValue(nombre, out AudioClip clip);
        return clip;
    }

    // Corta una hoja horizontal en n cuadros iguales (ui_hourglass.png = 8 cuadros de 24 x 32).
    public static Sprite[] Cortar(Sprite hoja, int cuadros)
    {
        if (hoja == null || cuadros <= 0) return null;
        string clave = hoja.name + "#" + cuadros;
        if (cortes.TryGetValue(clave, out Sprite[] ya) && ya != null && ya.Length > 0 && ya[0] != null) return ya;

        Texture2D textura = hoja.texture;
        int ancho = textura.width / cuadros;
        Sprite[] resultado = new Sprite[cuadros];
        for (int i = 0; i < cuadros; i++)
        {
            resultado[i] = Sprite.Create(textura, new Rect(i * ancho, 0, ancho, textura.height),
                new Vector2(0.5f, 0.5f), hoja.pixelsPerUnit);
            resultado[i].name = hoja.name + "_" + i;
        }
        cortes[clave] = resultado;
        return resultado;
    }
}
