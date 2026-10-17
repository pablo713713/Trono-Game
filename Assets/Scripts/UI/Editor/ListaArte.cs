using System.Collections.Generic;

// Lista oficial de archivos de arte y sonido (nombre, carpeta y tamaño) del documento
// "Historias de usuario". Si cambia un nombre o un tamaño, se avisa al grupo ANTES y se cambia aquí.
public struct EntradaArte
{
    public string nombre;
    public string carpeta;   // relativa a Assets/
    public int ancho, alto;
}

public static class ListaArte
{
    public const string Fondos = "Art/Backgrounds";
    public const string Interfaz = "Art/UI";
    public const string Personajes = "Art/Characters";
    public const string Multitudes = "Art/Crowds";

    public static readonly string[] Sonidos =
    {
        "amb_crowd_0", "amb_crowd_1", "amb_crowd_2", "amb_crowd_3", "amb_crowd_4",
        "sfx_tick", "sfx_stamp_yes", "sfx_stamp_no", "sfx_steps", "sfx_bell", "sfx_door_knock",
        "music_loop",
    };

    public static List<EntradaArte> Imagenes()
    {
        var l = new List<EntradaArte>();
        void Add(string n, string c, int w, int h) => l.Add(new EntradaArte { nombre = n, carpeta = c, ancho = w, alto = h });

        Add("bg_throne", Fondos, 320, 180);
        Add("bg_balcony_farm", Fondos, 320, 180);
        Add("bg_balcony_armory", Fondos, 320, 180);
        Add("bg_balcony_church", Fondos, 320, 180);
        Add("bg_vault", Fondos, 320, 180);

        for (int i = 0; i < 5; i++) Add("gold_" + i, Interfaz, 160, 80);
        Add("ui_scroll", Interfaz, 160, 100);
        Add("ui_stamp_yes", Interfaz, 48, 48);
        Add("ui_stamp_no", Interfaz, 48, 48);
        Add("ui_hourglass", Interfaz, 192, 32);
        foreach (string b in new[] { "farm", "armory", "church", "vault", "back" }) Add("ui_btn_" + b, Interfaz, 32, 32);
        foreach (string s in new[] { "title", "dayend", "ending" }) Add("screen_" + s, Interfaz, 320, 180);

        foreach (string g in new[] { "farm", "army", "church" })
            for (int i = 0; i < 5; i++) Add("crowd_" + g + "_" + i, Multitudes, 320, 90);

        foreach (string m in new[] { "peasant", "soldier", "monk", "duke" }) Add("msg_" + m, Personajes, 64, 96);
        Add("portrait_tobias", Personajes, 48, 48);
        Add("portrait_ines", Personajes, 48, 48);
        return l;
    }
}
