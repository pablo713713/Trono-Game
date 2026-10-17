using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Construye la escena principal (Assets/Scenes/Main.unity) y los prefabs de cada pantalla
// (Assets/Prefabs/*.prefab). Todo se dibuja en una cuadrícula de 320 × 180 (1 unidad = 1 pixel de arte),
// con el origen abajo a la izquierda.
//
// Úsalo UNA vez para generar la base. Después los prefabs y la escena se editan a mano
// en Unity (posiciones, tamaños, textos) y NO se vuelve a ejecutar, porque reemplaza ambos.
public static class ConstructorEscena
{
    const string RutaEscena = "Assets/Scenes/Main.unity";
    const string CarpetaPrefabs = "Assets/Prefabs/";

    static Font fuente;
    static readonly Color Dorado = new Color(1f, 0.85f, 0.35f);
    static readonly Color Pergamino = new Color(0.17f, 0.09f, 0.04f);

    [MenuItem("Trono/Dev2/0 Todo: placeholders + escena + prefabs")]
    public static void Todo()
    {
        GeneradorPlaceholders.Generar();
        Construir();
    }

    [MenuItem("Trono/Dev2/2 Construir escena y prefabs")]
    public static void Construir()
    {
        if (File.Exists(RutaEscena) &&
            !EditorUtility.DisplayDialog("Construir escena",
                "Esto REEMPLAZA Assets/Scenes/Main.unity y los prefabs de Assets/Prefabs.\n" +
                "Si ya los ajustaste a mano, mejor cancela. ¿Continuar?", "Reemplazar", "Cancelar"))
            return;

        ArteTools.ReconstruirCatalogo();
        fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Directory.CreateDirectory("Assets/Scenes");

        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---- cámara: fondo negro (las barras al ajustar a 16:9) ----
        var camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGO.tag = "MainCamera";
        var cam = camGO.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;

        // ---- EventSystem ----
        var esGO = new GameObject("EventSystem", typeof(EventSystem));
        esGO.GetComponent<EventSystem>().sendNavigationEvents = false;   // Espacio/Enter no deben "pulsar" botones seleccionados
#if ENABLE_INPUT_SYSTEM
        esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
#else
        esGO.AddComponent<StandaloneInputModule>();
#endif

        // ---- Canvas 320 × 180 que se ajusta sin deformar ----
        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var escalador = canvasGO.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(320f, 180f);
        escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        var marcoGO = new GameObject("Marco", typeof(RectTransform), typeof(RectMask2D));
        marcoGO.transform.SetParent(canvasGO.transform, false);
        var marco = marcoGO.GetComponent<RectTransform>();
        marco.anchorMin = marco.anchorMax = marco.pivot = new Vector2(0.5f, 0.5f);
        marco.sizeDelta = new Vector2(320f, 180f);
        marco.anchoredPosition = Vector2.zero;

        // ---- pantallas (el orden es el orden de dibujo: lo último queda arriba) ----
        PanelTrono trono = CrearTrono(marco, out CanvasGroup grupoTrono);
        PanelBalcon cultivos = CrearBalcon(marco, Grupo.Campesinos, "Panel_Balcon_Cultivos", out CanvasGroup grupoCultivos);
        PanelBalcon armeria = CrearBalcon(marco, Grupo.Soldados, "Panel_Balcon_Armeria", out CanvasGroup grupoArmeria);
        PanelBalcon catedral = CrearBalcon(marco, Grupo.Iglesia, "Panel_Balcon_Catedral", out CanvasGroup grupoCatedral);
        PanelBoveda boveda = CrearBoveda(marco, out CanvasGroup grupoBoveda);
        CrearHud(marco, out CuadroDialogo dialogo);
        PantallaFinDia finDia = CrearFinDia(marco);
        PantallaFinal final = CrearFinal(marco);
        PantallaTitulo titulo = CrearTitulo(marco);

        // ---- estado del juego: APAGADO hasta pulsar Jugar. Dev 1 reemplazará el componente. ----
        var estadoGO = new GameObject("GameState");
        estadoGO.AddComponent<GameStateFalso>();
        estadoGO.SetActive(false);

        // ---- sistemas ----
        var sis = new GameObject("Sistemas");
        var nav = sis.AddComponent<Navegador>();
        nav.paneles = new[] { grupoTrono, grupoCultivos, grupoArmeria, grupoCatedral, grupoBoveda };

        var audio = sis.AddComponent<AudioManager>();
        audio.musica = sis.AddComponent<AudioSource>();
        audio.ambiente = sis.AddComponent<AudioSource>();
        audio.efectos = sis.AddComponent<AudioSource>();

        var tutorial = sis.AddComponent<TutorialTobias>();
        tutorial.dialogo = dialogo;

        var director = sis.AddComponent<Director>();
        director.estadoJuego = estadoGO;
        director.navegador = nav;
        director.trono = trono;
        director.balcones = new[] { cultivos, armeria, catedral };
        director.boveda = boveda;
        director.titulo = titulo;
        director.finDia = finDia;
        director.final = final;
        director.dialogo = dialogo;
        director.tutorial = tutorial;
        director.sonido = audio;

        EditorSceneManager.SaveScene(escena, RutaEscena);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(RutaEscena, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("[Trono] Escena y prefabs listos. Dale Play: Enter o clic en JUGAR. Debug: F1-F11 (ver README).");
    }

    // =====================================================================
    //  Pantallas
    // =====================================================================

    static PanelTrono CrearTrono(RectTransform marco, out CanvasGroup grupo)
    {
        RectTransform raiz = Panel("Panel_Trono", marco, out grupo);
        Imagen("Fondo", raiz, "bg_throne", 0, 0, 320, 180);

        Image mensajero = Imagen("Mensajero", raiz, "msg_peasant", 206, 26, 64, 96, catalogo: false);

        Image perg = Imagen("Pergamino", raiz, "ui_scroll", 40, 26, 160, 100, raycast: true);
        Button botonPerg = perg.gameObject.AddComponent<Button>();
        botonPerg.transition = Selectable.Transition.None;
        botonPerg.targetGraphic = perg;

        Text texto = Texto("Texto", perg.rectTransform, "", 14, 14, 132, 72, 8, TextAnchor.UpperLeft, Pergamino, sombra: false);
        Text continuar = Texto("Continuar", perg.rectTransform, "[Espacio] >>", 14, 3, 132, 10, 8, TextAnchor.MiddleRight, Pergamino, sombra: false);
        Image puesto = Imagen("SelloPuesto", perg.rectTransform, "ui_stamp_yes", 56, 26, 48, 48, catalogo: false);
        puesto.rectTransform.localRotation = Quaternion.Euler(0, 0, -12f);

        Button si = Boton("SelloSi", raiz, "ui_stamp_yes", 272, 76, 48, 48, "S");
        Button no = Boton("SelloNo", raiz, "ui_stamp_no", 272, 26, 48, 48, "N");

        BotonNav(raiz, "BotonCultivos", "ui_btn_farm", 2, 140, Vista.Cultivos, "1");
        BotonNav(raiz, "BotonArmeria", "ui_btn_armory", 2, 104, Vista.Armeria, "2");
        BotonNav(raiz, "BotonCatedral", "ui_btn_church", 2, 68, Vista.Catedral, "3");
        BotonNav(raiz, "BotonBoveda", "ui_btn_vault", 2, 32, Vista.Boveda, "4");

        var panel = raiz.gameObject.AddComponent<PanelTrono>();
        panel.mensajero = mensajero;
        panel.texto = texto;
        panel.indicadorContinuar = continuar;
        panel.pergamino = botonPerg;
        panel.selloSi = si;
        panel.selloNo = no;
        panel.selloPuesto = puesto;
        Guardar(raiz);
        return panel;
    }

    static PanelBalcon CrearBalcon(RectTransform marco, Grupo grupo, string nombre, out CanvasGroup cg)
    {
        RectTransform raiz = Panel(nombre, marco, out cg);
        Imagen("Fondo", raiz, Textos.FondoBalcon(grupo), 0, 0, 320, 180);
        Image multitud = Imagen("Multitud", raiz, "crowd_" + Textos.PrefijoMultitud(grupo) + "_2", 0, 0, 320, 90, catalogo: false);
        Text titulo = Texto("Titulo", raiz, Textos.TituloBalcon(grupo), 40, 160, 240, 14, 8, TextAnchor.MiddleCenter, Color.white);
        BotonNav(raiz, "BotonVolver", "ui_btn_back", 2, 2, Vista.Trono, "Esp");

        var panel = raiz.gameObject.AddComponent<PanelBalcon>();
        panel.grupo = grupo;
        panel.multitud = multitud;
        panel.titulo = titulo;
        Guardar(raiz);
        return panel;
    }

    static PanelBoveda CrearBoveda(RectTransform marco, out CanvasGroup cg)
    {
        RectTransform raiz = Panel("Panel_Boveda", marco, out cg);
        Imagen("Fondo", raiz, "bg_vault", 0, 0, 320, 180);
        Image oro = Imagen("Oro", raiz, "gold_2", 80, 8, 160, 80, catalogo: false);
        Imagen("RetratoInes", raiz, "portrait_ines", 20, 84, 48, 48);
        Imagen("GlobosInes", raiz, null, 74, 84, 226, 44, new Color(0f, 0f, 0f, 0.65f));
        Text frase = Texto("FraseInes", raiz, "", 82, 88, 210, 36, 8, TextAnchor.MiddleLeft, Color.white);
        BotonNav(raiz, "BotonVolver", "ui_btn_back", 2, 2, Vista.Trono, "Esp");

        var panel = raiz.gameObject.AddComponent<PanelBoveda>();
        panel.oro = oro;
        panel.fraseInes = frase;
        Guardar(raiz);
        return panel;
    }

    static void CrearHud(RectTransform marco, out CuadroDialogo dialogo)
    {
        RectTransform raiz = Zona("HUD", marco, 0, 0, 320, 180);

        Image reloj = Imagen("Reloj", raiz, null, 292, 144, 24, 32, catalogo: false);
        reloj.enabled = false;
        var relojComp = raiz.gameObject.AddComponent<RelojArena>();
        relojComp.imagen = reloj;

        RectTransform caja = Zona("Dialogo", raiz, 38, 128, 250, 50);
        Image fondo = caja.gameObject.AddComponent<Image>();
        fondo.color = new Color(0f, 0f, 0f, 0.78f);
        var boton = caja.gameObject.AddComponent<Button>();
        boton.transition = Selectable.Transition.None;
        boton.targetGraphic = fondo;
        Imagen("RetratoTobias", caja, "portrait_tobias", 1, 1, 48, 48);
        Text texto = Texto("Texto", caja, "", 54, 2, 192, 46, 8, TextAnchor.MiddleLeft, Color.white);

        dialogo = raiz.gameObject.AddComponent<CuadroDialogo>();
        dialogo.contenedor = caja.gameObject;
        dialogo.texto = texto;
        dialogo.boton = boton;
        caja.gameObject.SetActive(false);
        Guardar(raiz);
    }

    static PantallaFinDia CrearFinDia(RectTransform marco)
    {
        RectTransform raiz = Zona("Pantalla_FinDia", marco, 0, 0, 320, 180);
        Imagen("Fondo", raiz, "screen_dayend", 0, 0, 320, 180, raycast: true);
        Text encabezado = Texto("Encabezado", raiz, "Fin del día", 20, 148, 280, 20, 14, TextAnchor.MiddleCenter, Dorado);
        Text resumen = Texto("Resumen", raiz, "", 40, 74, 240, 66, 9, TextAnchor.UpperLeft, Color.white);
        Imagen("RetratoTobias", raiz, "portrait_tobias", 40, 14, 48, 48);
        Text comentario = Texto("ComentarioTobias", raiz, "", 96, 14, 134, 48, 8, TextAnchor.MiddleLeft, Color.white);
        Button continuar = BotonTexto("BotonContinuar", raiz, "CONTINUAR", 236, 26, 70, 24);

        var p = raiz.gameObject.AddComponent<PantallaFinDia>();
        p.encabezado = encabezado;
        p.resumen = resumen;
        p.comentarioTobias = comentario;
        p.continuar = continuar;
        Guardar(raiz);
        return p;
    }

    static PantallaFinal CrearFinal(RectTransform marco)
    {
        RectTransform raiz = Zona("Pantalla_Final", marco, 0, 0, 320, 180);
        Imagen("Fondo", raiz, "screen_ending", 0, 0, 320, 180, raycast: true);
        Text titulo = Texto("Titulo", raiz, "Final", 10, 120, 300, 30, 16, TextAnchor.MiddleCenter, Dorado);
        Text mensaje = Texto("Mensaje", raiz, "", 20, 96, 280, 20, 10, TextAnchor.MiddleCenter, Color.white);
        Text conteo = Texto("Conteo", raiz, "", 50, 42, 220, 48, 9, TextAnchor.MiddleCenter, Color.white);
        Button reiniciar = BotonTexto("BotonReiniciar", raiz, "VOLVER A JUGAR", 100, 10, 120, 22);

        var p = raiz.gameObject.AddComponent<PantallaFinal>();
        p.titulo = titulo;
        p.mensaje = mensaje;
        p.conteo = conteo;
        p.reiniciar = reiniciar;
        Guardar(raiz);
        return p;
    }

    static PantallaTitulo CrearTitulo(RectTransform marco)
    {
        RectTransform raiz = Zona("Pantalla_Titulo", marco, 0, 0, 320, 180);
        Imagen("Fondo", raiz, "screen_title", 0, 0, 320, 180, raycast: true);
        Texto("Titulo", raiz, "EL TRONO DEL NO", 10, 102, 300, 36, 24, TextAnchor.MiddleCenter, Dorado);
        Texto("Subtitulo", raiz, "Aprende a decir que no", 10, 82, 300, 16, 10, TextAnchor.MiddleCenter, Color.white);
        Button jugar = BotonTexto("BotonJugar", raiz, "JUGAR", 110, 34, 100, 26);
        Texto("Ayuda", raiz, "Enter o clic para empezar", 10, 14, 300, 12, 8, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.6f));

        var p = raiz.gameObject.AddComponent<PantallaTitulo>();
        p.jugar = jugar;
        Guardar(raiz);
        return p;
    }

    // =====================================================================
    //  Ayudantes de construcción (origen abajo a la izquierda, unidades = pixels de arte)
    // =====================================================================

    static RectTransform Zona(string nombre, Transform padre, float x, float y, float w, float h)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = Vector2.zero;
        r.pivot = Vector2.zero;
        r.anchoredPosition = new Vector2(x, y);
        r.sizeDelta = new Vector2(w, h);
        return r;
    }

    static RectTransform Panel(string nombre, Transform padre, out CanvasGroup grupo)
    {
        RectTransform r = Zona(nombre, padre, 0, 0, 320, 180);
        grupo = r.gameObject.AddComponent<CanvasGroup>();
        return r;
    }

    // catalogo = true: el sprite se vuelve a buscar por nombre al jugar (sobrevive a cambios de archivo).
    static Image Imagen(string nombre, Transform padre, string sprite, float x, float y, float w, float h,
                        Color? color = null, bool raycast = false, bool catalogo = true)
    {
        RectTransform r = Zona(nombre, padre, x, y, w, h);
        var img = r.gameObject.AddComponent<Image>();
        img.raycastTarget = raycast;
        img.color = color ?? Color.white;
        if (sprite != null)
        {
            img.sprite = BuscarSprite(sprite);
            if (catalogo) r.gameObject.AddComponent<SpriteDeCatalogo>().nombre = sprite;
        }
        return img;
    }

    static Text Texto(string nombre, Transform padre, string contenido, float x, float y, float w, float h,
                      int tamano, TextAnchor ancla, Color color, bool sombra = true)
    {
        RectTransform r = Zona(nombre, padre, x, y, w, h);
        var t = r.gameObject.AddComponent<Text>();
        t.font = fuente;
        t.text = contenido;
        t.fontSize = tamano;
        t.alignment = ancla;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        t.raycastTarget = false;
        t.supportRichText = false;
        r.gameObject.AddComponent<TextoPixel>();
        if (sombra)
        {
            var s = r.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0, 0, 0, 0.85f);
            s.effectDistance = new Vector2(1, -1);
        }
        return t;
    }

    // Botón con arte (sellos y botones de navegación). "etiqueta" es la tecla, chiquita en la esquina.
    static Button Boton(string nombre, Transform padre, string sprite, float x, float y, float w, float h, string etiqueta)
    {
        Image img = Imagen(nombre, padre, sprite, x, y, w, h, raycast: true);
        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.transition = Selectable.Transition.ColorTint;
        var colores = b.colors;
        colores.highlightedColor = new Color(1f, 1f, 0.85f);
        colores.pressedColor = new Color(0.75f, 0.75f, 0.75f);
        colores.disabledColor = new Color(1f, 1f, 1f, 0.45f);
        b.colors = colores;
        if (!string.IsNullOrEmpty(etiqueta))
            Texto("Tecla", img.rectTransform, etiqueta, 1, 1, w - 2, 10, 8, TextAnchor.LowerRight, Color.white);
        return b;
    }

    static void BotonNav(Transform padre, string nombre, string sprite, float x, float y, Vista destino, string etiqueta)
    {
        Button b = Boton(nombre, padre, sprite, x, y, 32, 32, etiqueta);
        b.gameObject.AddComponent<BotonNavegacion>().destino = destino;
    }

    // Botón sin arte (título, continuar, volver a jugar): rectángulo con texto.
    static Button BotonTexto(string nombre, Transform padre, string etiqueta, float x, float y, float w, float h)
    {
        Image img = Imagen(nombre, padre, null, x, y, w, h, new Color(0.45f, 0.12f, 0.1f), raycast: true);
        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var colores = b.colors;
        colores.highlightedColor = new Color(1f, 0.85f, 0.7f);
        colores.pressedColor = new Color(0.7f, 0.6f, 0.55f);
        b.colors = colores;
        Texto("Etiqueta", img.rectTransform, etiqueta, 0, 0, w, h, 10, TextAnchor.MiddleCenter, Color.white);
        return b;
    }

    static Sprite BuscarSprite(string nombre)
    {
        foreach (string guid in AssetDatabase.FindAssets(nombre + " t:Sprite", new[] { "Assets/Art" }))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(ruta) == nombre) return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        }
        return null;
    }

    static void Guardar(RectTransform raiz)
    {
        PrefabUtility.SaveAsPrefabAssetAndConnect(raiz.gameObject, CarpetaPrefabs + raiz.name + ".prefab", InteractionMode.AutomatedAction);
    }
}
