using UnityEngine;

// Textos y nombres de archivo del lado de la interfaz, en un solo lugar.
public static class Textos
{
    public const string SilencioEsSi = "El silencio también es un sí, majestad.";

    // ---- nombres de archivo por grupo ----
    public static string PrefijoMultitud(Grupo g)
    {
        switch (g) { case Grupo.Campesinos: return "farm"; case Grupo.Soldados: return "army"; default: return "church"; }
    }

    public static string FondoBalcon(Grupo g)
    {
        switch (g) { case Grupo.Campesinos: return "bg_balcony_farm"; case Grupo.Soldados: return "bg_balcony_armory"; default: return "bg_balcony_church"; }
    }

    public static string TituloBalcon(Grupo g)
    {
        switch (g)
        {
            case Grupo.Campesinos: return "CULTIVOS - Marta, la cosechera";
            case Grupo.Soldados: return "ARMERÍA - Capitán Bruno";
            default: return "CATEDRAL - Obispo Anselmo";
        }
    }

    public static string NombreGrupo(Grupo g)
    {
        switch (g) { case Grupo.Campesinos: return "Los campesinos"; case Grupo.Soldados: return "Los soldados"; default: return "La iglesia"; }
    }

    // ---- Inés, según NivelOro (0 = casi vacía ... 4 = llena) ----
    static readonly string[] frasesInes =
    {
        "¡Majestad, no queda casi nada! Ni para las velas.",
        "Esto no alcanza para el invierno.",
        "Vamos justos. Cada moneda cuenta.",
        "Aún hay margen, pero no se confíe.",
        "La bóveda está llena... por ahora.",
    };

    public static string FraseInes(int nivel) => frasesInes[Mathf.Clamp(nivel, 0, frasesInes.Length - 1)];

    // ---- resumen del día: el jugador nunca ve números, solo tendencias ----
    public static string Tendencia(Grupo g, int cambio)
    {
        string nombre = NombreGrupo(g);
        if (cambio > 0) return nombre + ": más contentos";
        if (cambio < 0) return nombre + ": más enojados";
        return nombre + ": sin cambios";
    }

    public static string TendenciaOro(int cambio)
    {
        if (cambio > 0) return "El tesoro: creció";
        if (cambio < 0) return "El tesoro: menguó";
        return "El tesoro: igual";
    }

    public const string ComentarioTobiasPorDefecto = "Otro día más en el trono, majestad.";

    // ---- finales ----
    public static void DeFinal(Final f, out string titulo, out string mensaje, out bool victoria)
    {
        victoria = f == Final.Victoria;
        switch (f)
        {
            case Final.RebelionCampesina: titulo = "Rebelión campesina"; mensaje = "Ignoraste a quien te alimenta."; break;
            case Final.GolpeMilitar: titulo = "Golpe militar"; mensaje = "Ignoraste a quien te protege."; break;
            case Final.Excomunion: titulo = "Excomunión"; mensaje = "Ignoraste la fe de tu pueblo."; break;
            case Final.Bancarrota: titulo = "Bancarrota"; mensaje = "Dijiste que sí demasiadas veces."; break;
            case Final.CedioLaCorona: titulo = "El rey que no supo decir que no"; mensaje = "Sobreviviste, pero cediste lo único que importaba."; break;
            default: titulo = "Victoria"; mensaje = "Aprendiste a decir que no."; break;
        }
    }

    // ---- Duque: el texto va en dos partes (separadas por | o por salto de línea en requests/código) ----
    public const string DuqueParte2PorDefecto = "Solo firma aquí, sobrino. La corona estará en buenas manos.";

    public static string[] PartesDuque(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return new[] { "...", DuqueParte2PorDefecto };

        string[] explicitas = texto.Split(new[] { '|', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (explicitas.Length >= 2) return new[] { explicitas[0].Trim(), explicitas[1].Trim() };

        // Un solo bloque: partir por la frase que cae más cerca de la mitad.
        int mejor = -1;
        float distancia = float.MaxValue;
        for (int i = 0; i < texto.Length - 1; i++)
        {
            bool finDeFrase = (texto[i] == '.' || texto[i] == '!' || texto[i] == '?') && texto[i + 1] == ' ';
            if (!finDeFrase) continue;
            float d = Mathf.Abs(i - texto.Length / 2f);
            if (d < distancia) { distancia = d; mejor = i; }
        }
        if (mejor > 0) return new[] { texto.Substring(0, mejor + 1).Trim(), texto.Substring(mejor + 1).Trim() };

        return new[] { texto, DuqueParte2PorDefecto };
    }
}
