using System;
using System.Collections.Generic;

// Estructuras que se leen de Assets/Data/requests.json con JsonUtility.
// JsonUtility no lee un arreglo suelto: el JSON va envuelto en { "peticiones": [ ... ] }.

[Serializable]
public class Efectos
{
    public int campesinos;
    public int soldados;
    public int iglesia;
    public int oro;

    public int Valor(Grupo grupo)
    {
        switch (grupo)
        {
            case Grupo.Campesinos: return campesinos;
            case Grupo.Soldados: return soldados;
            default: return iglesia;
        }
    }
}

[Serializable]
public class Peticion
{
    public string id;
    public int dia;             // día en que puede aparecer (1 a 5)
    public string grupo;        // "campesinos", "soldados", "iglesia" o "especial"
    public string mensajero;    // nombre del sprite sin prefijo: "peasant" -> msg_peasant.png
    public string texto;
    public Efectos si = new Efectos();
    public Efectos no = new Efectos();
    public bool trampa;         // solo para balance; el jugador no lo ve

    public bool EsDuque => id == "duque";

    // Grupo al que pertenece la petición; null si es "especial".
    public Grupo? GrupoEnum
    {
        get
        {
            switch (grupo)
            {
                case "campesinos": return Grupo.Campesinos;
                case "soldados": return Grupo.Soldados;
                case "iglesia": return Grupo.Iglesia;
                default: return null;
            }
        }
    }
}

[Serializable]
public class ListaPeticiones
{
    public List<Peticion> peticiones = new List<Peticion>();
}

[Serializable]
public class ResumenDia
{
    public int dia;
    public int sies;
    public int noes;
    public int porReloj;          // peticiones aprobadas porque se acabó el tiempo
    public int cambioCampesinos;
    public int cambioSoldados;
    public int cambioIglesia;
    public int cambioOro;
    public string comentarioTobias;
}
