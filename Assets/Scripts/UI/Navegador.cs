using System;
using UnityEngine;

public enum Vista { Trono = 0, Cultivos = 1, Armeria = 2, Catedral = 3, Boveda = 4 }

// D2-02: moverse entre el trono, los 3 balcones y la bóveda.
// Los paneles no se desactivan (así sus animaciones y datos siguen vivos):
// solo se ponen invisibles y sin clics con un CanvasGroup.
public class Navegador : MonoBehaviour
{
    public static Navegador Instancia { get; private set; }

    public CanvasGroup[] paneles = new CanvasGroup[5];   // en el orden del enum Vista
    public bool Bloqueado { get; set; } = true;          // el Director lo abre al pulsar Jugar
    public Vista Actual { get; private set; } = Vista.Trono;

    public event Action<Vista> VistaCambio;

    void Awake() => Instancia = this;
    void OnDestroy() { if (Instancia == this) Instancia = null; }
    void Start() => Mostrar(Vista.Trono);

    public void Ir(Vista vista)
    {
        if (Bloqueado) return;
        Mostrar(vista);
    }

    public void IrForzado(Vista vista) => Mostrar(vista);

    void Mostrar(Vista vista)
    {
        for (int i = 0; i < paneles.Length; i++)
        {
            if (paneles[i] == null) continue;
            bool activo = i == (int)vista;
            paneles[i].alpha = activo ? 1f : 0f;
            paneles[i].interactable = activo;
            paneles[i].blocksRaycasts = activo;
        }
        Actual = vista;
        VistaCambio?.Invoke(vista);
    }

    void Update()
    {
        if (Bloqueado) return;
        if (Teclado.Presionada(Tecla.Uno)) Ir(Vista.Cultivos);
        else if (Teclado.Presionada(Tecla.Dos)) Ir(Vista.Armeria);
        else if (Teclado.Presionada(Tecla.Tres)) Ir(Vista.Catedral);
        else if (Teclado.Presionada(Tecla.Cuatro)) Ir(Vista.Boveda);
        else if (Teclado.Presionada(Tecla.Espacio)) Ir(Vista.Trono);
    }
}
