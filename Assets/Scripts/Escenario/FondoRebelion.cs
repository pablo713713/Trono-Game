using UnityEngine;
using UnityEngine.UI;

// Cambia el fondo de un balcón cuando su grupo está a punto de rebelarse.
// Con el ánimo en Reglas.UmbralRebelion o menos (80 % de enojo o más) muestra la versión
// destruida del fondo (por ejemplo bg_balcony_church_riot); cuando el grupo se calma, vuelve al normal.
//
// Va en la raíz del panel del balcón (junto a PanelBalcon). Lo agrega el menú
// "Trono > Dev1 > Poner multitudes en los balcones".
public class FondoRebelion : MonoBehaviour
{
    public Grupo grupo;
    public string fondoNormal = "bg_balcony_church";
    public string fondoRebelion = "bg_balcony_church_riot";

    [Tooltip("Si se deja vacío, usa el hijo 'Fondo' del panel.")]
    public Image fondo;

    bool? mostrandoRebelion;

    void Awake()
    {
        if (fondo == null)
        {
            Transform t = transform.Find("Fondo");
            if (t != null) fondo = t.GetComponent<Image>();
        }
    }

    // El GameState arranca apagado (se enciende al pulsar Jugar): se revisa en cada cuadro,
    // pero el sprite solo se cambia cuando el estado pasa de un lado al otro del umbral.
    void LateUpdate()
    {
        if (fondo == null) return;

        bool rebelion = Juego.Estado != null && Juego.Estado.Animo(grupo) <= Reglas.UmbralRebelion;
        if (mostrandoRebelion == rebelion) return;
        mostrandoRebelion = rebelion;

        Sprite s = ArteCatalogo.ObtenerSprite(rebelion ? fondoRebelion : fondoNormal);
        if (s != null) fondo.sprite = s;
        else if (rebelion) Debug.LogWarning($"[FondoRebelion] Falta {fondoRebelion}.png en Assets/Art/Backgrounds.", this);
    }
}
