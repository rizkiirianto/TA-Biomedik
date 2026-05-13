using UnityEngine;
using UnityEditor;
using System.IO;

public class WaterStreamGenerator : MonoBehaviour
{
    // This creates a custom button at the very top of your Unity window!
    [MenuItem("Tools/Generate Water Stream Pixel Art")]
    public static void GenerateTexture()
    {
        int width = 16;
        int height = 32;
        
        // Create an empty texture
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point; 

        // Our Pixel Art Palette
        Color baseColor = new Color(0.2f, 0.6f, 0.9f, 0.8f); // Translucent light blue
        Color edgeColor = new Color(0.1f, 0.4f, 0.7f, 0.9f); // Darker blue border
        Color highlightColor = new Color(0.9f, 0.95f, 1f, 0.9f); // White/Cyan highlight

        // Paint the pixels mathematically
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixelColor = baseColor;

                // 1. Draw dark borders on the far left and right edges
                if (x == 0 || x == 15)
                {
                    pixelColor = edgeColor;
                }
                // 2. Draw a slight inner shadow for depth
                else if (x == 1 || x == 14)
                {
                    pixelColor = Color.Lerp(baseColor, edgeColor, 0.5f);
                }
                // 3. Draw the Left Highlight (Broken line, perfectly seamless loop of 8 pixels)
                else if (x == 4 || x == 5)
                {
                    if (y % 8 < 5) pixelColor = highlightColor;
                }
                // 4. Draw the Right Highlight (Offset broken line, perfectly seamless loop of 16 pixels)
                else if (x == 11)
                {
                    if ((y + 4) % 16 < 6) pixelColor = highlightColor;
                }

                tex.SetPixel(x, y, pixelColor);
            }
        }

        // Apply the painted pixels
        tex.Apply();

        // Convert to a PNG and save it directly to your Unity Assets folder
        byte[] bytes = tex.EncodeToPNG();
        string path = Application.dataPath + "/WaterStream_16x32.png";
        File.WriteAllBytes(path, bytes);

        // Tell Unity to refresh so the file shows up immediately
        AssetDatabase.Refresh();
        Debug.Log("Success! Saved perfect seamless water pixel art to: " + path);
    }
}