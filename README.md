# El Trono del No — WGJ

Juego para la WGJ con la consigna "aprender a decir que no". Unity 6.3 LTS, pixel art, build web para itch.io.

## Abrir el proyecto

1. Instalar Unity Hub, Unity 6.3 LTS (con Web Build Support), Git y Git LFS.
2. `git lfs install` (una vez por computadora).
3. Clonar el repo y abrirlo desde Unity Hub con *Add → Add project from disk*.
4. Todos con la misma versión exacta del editor.

## Carpetas y dueños

| Carpeta | Dueño |
| --- | --- |
| Assets/Scripts/Core/, Assets/Data/ | Desarrollador 1 |
| Assets/Scripts/UI/, Assets/Prefabs/, Assets/Scenes/ | Desarrollador 2 |
| Assets/Art/Backgrounds/, Assets/Art/UI/, Assets/Audio/ | Diseñador 1 |
| Assets/Art/Characters/, Assets/Art/Crowds/ | Diseñador 2 |

Nadie edita una carpeta que no es suya. Al reemplazar un placeholder: mismo nombre, mismo tamaño y no borrar el `.meta`.

## Para el Desarrollador 2

- Lee y controla el juego solo con `Juego.Estado` (tipo `IGameState`).
- Mientras la lógica real no esté lista, poner el componente `GameStateFalso` en un GameObject vacío de la escena.
- Teclas de depuración del falso: F1-F6 bajan/suben el ánimo de cada grupo, F7/F8 el oro, F9 termina el día, F10 trae al Duque, F11 recorre los 6 finales.

## Pixel art

Todo PNG dentro de `Assets/Art/` se configura solo (Point, sin compresión, 16 PPU) gracias a `Assets/Editor/PixelArtImporter.cs`.
