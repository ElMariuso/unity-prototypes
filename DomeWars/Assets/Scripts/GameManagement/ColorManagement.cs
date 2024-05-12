using System.Collections.Generic;
using UnityEngine;

public class ColorManagement : MonoBehaviour
{
    public List<Color> gangColors;

    public void GenerateRandomColors(int numberOfColors)
    {
        bool tooSimilar;
        float h, s, v;
        Color newColor;
        gangColors = new List<Color>();

        while (gangColors.Count < numberOfColors)
        {
            h = UnityEngine.Random.Range(0f, 1f);
            s = UnityEngine.Random.Range(0.5f, 1f);
            v = UnityEngine.Random.Range(0.5f, 1f);

            newColor = Color.HSVToRGB(h, s, v);
            tooSimilar = false;

            foreach (Color existingColor in gangColors)
            {
                if (ColorSimilarity(newColor, existingColor))
                {
                    tooSimilar = true;
                    break;
                }
            }

            if (!tooSimilar)
                gangColors.Add(newColor);
        }
    }

    private bool ColorSimilarity(Color c1, Color c2)
    {
        float threshold = 0.1f;
        Color.RGBToHSV(c1, out float h1, out float s1, out float v1);
        Color.RGBToHSV(c2, out float h2, out float s2, out float v2);
        return (Mathf.Abs(h1 - h2) < threshold && Mathf.Abs(s1 - s2) < threshold && Mathf.Abs(v1 - v2) < threshold);
    }

    public Color GetAColor()
    {
        if (gangColors.Count > 0)
        {
            int index = Random.Range(0, gangColors.Count);
            Color selectedColor = gangColors[index];
            gangColors.RemoveAt(index);
            return selectedColor;
        }
        else
            return Color.black;
    }
}
