using UnityEngine;

// Números del juego en un solo lugar, para que el GameState real y el falso
// usen exactamente las mismas reglas. Ajustar aquí durante el balance.
public static class Reglas
{
    public const int AnimoInicial = 50;
    public const int OroInicial = 100;
    public const int Dias = 5;

    public const int CalmaFinDelDia = 5;      // ánimo que recupera un grupo bajo 50
    public const int LimiteCalma = 50;
    public const int ImpuestosFinDelDia = 20;
    public const float ExigenciaPorSi = 0.2f; // costo en oro × (1 + 0,2 × exigencia)

    static readonly float[] tiempoPorDia = { 25f, 20f, 18f, 15f, 12f };
    static readonly int[] mensajerosPorDia = { 5, 6, 7, 8, 8 };

    public static float TiempoPorDia(int dia) =>
        tiempoPorDia[Mathf.Clamp(dia - 1, 0, tiempoPorDia.Length - 1)];

    public static int MensajerosPorDia(int dia) =>
        mensajerosPorDia[Mathf.Clamp(dia - 1, 0, mensajerosPorDia.Length - 1)];

    // 0 = antorchas (1-19), 1 = carteles, 2 = murmullos, 3 = tranquilos, 4 = celebrando (80-100)
    public static int EstadoVisual(int animo) => Mathf.Clamp(animo / 20, 0, 4);

    // 0 = casi vacía ... 4 = llena
    public static int NivelOro(int oro)
    {
        if (oro < 30) return 0;
        if (oro < 60) return 1;
        if (oro < 100) return 2;
        if (oro < 150) return 3;
        return 4;
    }

    public static Final FinalPorRebelion(Grupo grupo)
    {
        switch (grupo)
        {
            case Grupo.Campesinos: return Final.RebelionCampesina;
            case Grupo.Soldados: return Final.GolpeMilitar;
            default: return Final.Excomunion;
        }
    }
}
