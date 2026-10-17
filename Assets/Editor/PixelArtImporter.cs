using UnityEditor;
using UnityEngine;

// Configura automáticamente toda imagen dentro de Assets/Art/ para pixel art:
// sin filtro, sin compresión, sin mipmaps y 16 pixels por unidad.
// Así los diseñadores solo arrastran su PNG y se ve nítido.
public class PixelArtImporter : AssetPostprocessor
{
    const string CarpetaArte = "Assets/Art/";
    const float PixelsPorUnidad = 16f;

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(CarpetaArte)) return;

        TextureImporter importador = (TextureImporter)assetImporter;

        // Solo la primera vez: tipo Sprite. Después se respeta lo que cambie el
        // Desarrollador 2 (por ejemplo, Multiple para cortar ui_hourglass.png).
        if (importador.importSettingsMissing)
        {
            importador.textureType = TextureImporterType.Sprite;
            importador.spriteImportMode = SpriteImportMode.Single;
        }

        // Siempre: lo que hace que el pixel art se vea nítido.
        importador.filterMode = FilterMode.Point;
        importador.textureCompression = TextureImporterCompression.Uncompressed;
        importador.mipmapEnabled = false;
        importador.spritePixelsPerUnit = PixelsPorUnidad;
        importador.alphaIsTransparency = true;
    }
}
