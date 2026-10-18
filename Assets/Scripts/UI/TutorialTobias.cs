using UnityEngine;

// D2-10: Tobías guía el día 1 con 4 mensajes cortos que aparecen en el momento justo
// (sellos y reloj al llegar el primer mensajero, balcones y bóveda la primera vez que se visitan).
// No decide por el jugador y no frena el reloj, por eso son avisos y no pantallas bloqueantes.
public class TutorialTobias : MonoBehaviour
{
    public CuadroDialogo dialogo;

    bool sellos, balcones, boveda;

    bool EsDia1 => Juego.Estado != null && Juego.Estado.Dia == 1;

    public void AlLlegarMensajero()
    {
        if (sellos || !EsDia1) return;
        sellos = true;
        dialogo.Encolar("Majestad, lea el pergamino y responda con los sellos: SÍ (tecla S) o NO (tecla N).", 7f);
        dialogo.Encolar("Cuidado con el reloj de arena: si se acaba, la petición se aprueba sola.", 7f);
    }

    public void AlCambiarVista(Vista vista)
    {
        if (!EsDia1) return;
        if (!balcones && (vista == Vista.Cultivos || vista == Vista.Armeria || vista == Vista.Catedral))
        {
            balcones = true;
            dialogo.Encolar("Desde los balcones ve cómo reacciona el pueblo: entre más gente, más enojados. Espacio para volver al trono.", 7f);
        }
        else if (!boveda && vista == Vista.Boveda)
        {
            boveda = true;
            dialogo.Encolar("Aquí está la bóveda. Inés le dirá cuánto oro queda... y el reloj no se detiene, majestad.", 7f);
        }
    }
}
