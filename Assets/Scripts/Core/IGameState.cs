using System;

// Contrato entre el Desarrollador 1 (lógica) y el Desarrollador 2 (escenas).
// Cambiar algo aquí se avisa en el grupo antes de hacerlo.

public enum Grupo { Campesinos, Soldados, Iglesia }

public enum Final { RebelionCampesina, GolpeMilitar, Excomunion, Bancarrota, CedioLaCorona, Victoria }

public interface IGameState
{
    int Dia { get; }                    // 1 a 5
    int Oro { get; }
    int Animo(Grupo grupo);             // 0 a 100
    int EstadoVisual(Grupo grupo);      // 0 = antorchas ... 4 = celebrando
    int NivelOro { get; }               // 0 = casi vacía ... 4 = llena
    Peticion PeticionActual { get; }    // id "duque" = escena final
    float TiempoRestante { get; }       // segundos
    float TiempoTotal { get; }
    ResumenDia ResumenTotal { get; }    // totales de toda la partida, para la pantalla final

    event Action<Peticion> PeticionNueva;
    event Action<bool> PeticionResuelta;   // true = sí
    event Action EstadoCambio;             // cambió ánimo u oro
    event Action<ResumenDia> FinDelDia;
    event Action<Final> FinDelJuego;

    void Decidir(bool si);
    void ContinuarSiguienteDia();
}

// Punto de acceso único. El Desarrollador 2 siempre usa Juego.Estado,
// sin importar si detrás está el GameState real o el falso.
public static class Juego
{
    public static IGameState Estado { get; set; }
}
