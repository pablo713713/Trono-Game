using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Teclas del juego (1 a 4, Espacio, Enter, S, N). Funciona con el Input System nuevo y con el antiguo.
public enum Tecla { Uno, Dos, Tres, Cuatro, Espacio, Enter, S, N }

public static class Teclado
{
    public static bool Presionada(Tecla tecla)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        if (k == null) return false;
        switch (tecla)
        {
            case Tecla.Uno: return k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame;
            case Tecla.Dos: return k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame;
            case Tecla.Tres: return k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame;
            case Tecla.Cuatro: return k.digit4Key.wasPressedThisFrame || k.numpad4Key.wasPressedThisFrame;
            case Tecla.Espacio: return k.spaceKey.wasPressedThisFrame;
            case Tecla.Enter: return k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame;
            case Tecla.S: return k.sKey.wasPressedThisFrame;
            case Tecla.N: return k.nKey.wasPressedThisFrame;
            default: return false;
        }
#else
        switch (tecla)
        {
            case Tecla.Uno: return Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
            case Tecla.Dos: return Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
            case Tecla.Tres: return Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3);
            case Tecla.Cuatro: return Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4);
            case Tecla.Espacio: return Input.GetKeyDown(KeyCode.Space);
            case Tecla.Enter: return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
            case Tecla.S: return Input.GetKeyDown(KeyCode.S);
            case Tecla.N: return Input.GetKeyDown(KeyCode.N);
            default: return false;
        }
#endif
    }
}
