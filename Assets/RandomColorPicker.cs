using RuniOS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RandomColorPicker : MonoBehaviour
{
    public Image image;
    public TMP_Text text;
    
    public void ColorUpdate()
    {
        Color color = new Color32((byte)Random.Range(0, 255), (byte)Random.Range(0, 255), (byte)Random.Range(0, 255), 255);
        image.color = color;
        text.text = ((HexColor)color).ToString();
    }
}