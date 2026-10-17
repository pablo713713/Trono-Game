# Desarrollador 2 — escenas, interfaz y build

Todo esto vive en `Assets/Scripts/UI/` (mi carpeta). Lee el estado **solo** con `Juego.Estado` (`IGameState`).

## Primera vez (≈2 minutos)

1. Copiar esta carpeta dentro del proyecto y abrirlo en Unity 6.3 LTS (esperar a que compile).
2. Menú **Trono > Dev2 > 0 Todo: placeholders + escena + prefabs**.
   - Crea los placeholders con los nombres y tamaños de la lista (no pisa arte que ya exista).
   - Crea `Assets/Scenes/Main.unity` y los prefabs en `Assets/Prefabs/` (5 paneles + HUD + 3 pantallas).
3. Play: Enter o clic en JUGAR. Teclas de depuración del falso: F1–F11.
4. Commit de: `Scripts/UI`, `Prefabs`, `Scenes` y `Art/` + `Audio/` (placeholders y sus `.meta`).
   Avisar al grupo antes: los diseñadores empiezan a reemplazar desde ese commit.

> No volver a ejecutar el paso 2 después de ajustar a mano: **reemplaza** la escena y los prefabs.

## Menú Trono > Dev2

| Opción | Para qué |
| --- | --- |
| 1 Generar placeholders | Crea solo los archivos que faltan |
| 2 Construir escena y prefabs | Genera la base (una vez) |
| 3 Reconstruir catálogo de arte | Normalmente es automático al importar |
| 4 Verificar nombres y tamaños | Informe de lo que falta o tiene mal tamaño (para cada hito) |
| 5 Build WebGL (itch.io) | Sale en `Builds/WebGL`; subir el **contenido** en un .zip |

## Cómo se encuentra el arte

El juego busca sprites y sonidos **por nombre de archivo** en `ArteCatalogo`
(`Assets/Prefabs/Resources/ArteCatalogo.asset`), que se actualiza solo cuando cambia algo en
`Assets/Art` o `Assets/Audio`. Por eso se puede reemplazar un `.wav` por un `.ogg`
y no se rompe nada. Si no hay archivo, la pantalla lo omite en vez de fallar.

## Dónde va cada cosa (cuadrícula 320 × 180, origen abajo-izquierda)

Para los diseñadores, para no tapar nada importante:

- **Trono:** pergamino x 40–200, y 26–126 · mensajero x 206–270, y 26–122 · sellos x 272–320
  · botones de navegación x 2–34 · reloj de arena x 292–316, y 144–176 · franja de Tobías (arriba) y 128–178.
- **Balcones:** multitud en y 0–90 (franja inferior libre) · título arriba · botón volver abajo a la izquierda.
- **Bóveda:** oro x 80–240, y 8–88 · retrato de Inés + globo de texto en y 84–128.

## Acuerdos que necesito del Desarrollador 1

1. **Orden de eventos:** `PeticionResuelta` antes que `PeticionNueva` (así se ve el sello antes de que entre el siguiente mensajero).
2. **El objeto `GameState` arranca apagado** en la escena y el título lo enciende al pulsar Jugar. Su `Awake` debe fijar `Juego.Estado` y su `Start` empezar el día 1. Al destruirse debe poner `Juego.Estado = null`. Para cambiar al real: en el objeto `GameState`, quitar `GameStateFalso` y poner el componente real.
3. **Duque en dos partes:** separar el texto con `|` (o salto de línea) en la petición del Duque. Si viene en un solo bloque, la UI lo parte por la frase más cercana a la mitad.
4. **Reloj acabado:** al disparar `PeticionResuelta(true)` por reloj, `TiempoRestante` debe valer 0 (así Tobías dice "El silencio también es un sí").
5. `ResumenDia.comentarioTobias` se muestra tal cual; si viene vacío, uso uno por defecto.

## Opcionales que la interfaz ya reconoce

- `music_duke` (Audio): música propia del Duque. Si no existe, se usa `music_loop` más lenta y grave.
- `crowd_[grupo]_[estado]_b.png`: segundo cuadro de la multitud (A2-06); alterna cada 0,6 s.
- `font_pixel.ttf`: al entrar a `Art/UI/` se usa sola en todos los textos.

## Decisiones de diseño que tomé

- El jugador **no ve números**: el resumen del día usa tendencias ("más enojados", "el tesoro menguó"). Los conteos de sí/no/reloj solo salen en los finales.
- Los avisos de Tobías del día 1 no pausan el juego (el contrato no tiene pausa); aparecen en el momento justo.
- Los paneles no se desactivan: se ocultan con `CanvasGroup`, así el sello y las animaciones siguen aunque el jugador esté mirando un balcón.
