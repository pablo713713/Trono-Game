using UnityEngine;

// Números del juego en un solo lugar, para que el GameState real y el falso
// usen exactamente las mismas reglas. Balancear con Tools/simular_balance.py.
//
// Versión 2: las decisiones solo cambian el ánimo de los grupos (el oro ya no se usa),
// el ánimo se conserva de un día a otro y los días se diferencian por la cantidad de peticiones.
public static class Reglas
{
    public const int AnimoInicial = 50;
    public const int OroInicial = 100;   // se mantiene fijo; queda solo por compatibilidad con la interfaz
    public const int Dias = 5;

    static readonly float[] tiempoPorDia = { 25f, 20f, 18f, 15f, 12f };
    static readonly int[] mensajerosPorDia = { 4, 6, 8, 10, 12 };   // 40 de un banco de 50 peticiones

    public static float TiempoPorDia(int dia) =>
        tiempoPorDia[Mathf.Clamp(dia - 1, 0, tiempoPorDia.Length - 1)];

    public static int MensajerosPorDia(int dia) =>
        mensajerosPorDia[Mathf.Clamp(dia - 1, 0, mensajerosPorDia.Length - 1)];

    // 0 = a punto de invadir (1-19), 1 = carteles, 2 = murmullos, 3 = tranquilos, 4 = contentos (80-100)
    public static int EstadoVisual(int animo) => Mathf.Clamp(animo / 20, 0, 4);

    // Sin oro que cambie, la bóveda siempre se ve igual.
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
            case Grupo.Campesinos: return Final.RebelionCampesina;   // se muestra como "Rebelión del mercado"
            case Grupo.Soldados: return Final.GolpeMilitar;
            default: return Final.Excomunion;                        // se muestra como "Levantamiento de la iglesia"
        }
    }
}
