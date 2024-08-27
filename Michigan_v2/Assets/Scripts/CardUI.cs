using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CardUI : MonoBehaviour
{
    // todo: create a custom layout group that will add/order suit icons using value (like how standard cards are laid out)

    [SerializeField]
    TextMeshProUGUI valueText;
    [SerializeField]
    Image suitImage;

    public void LoadCard(Sprite icon, string value, Color color)
    {
        suitImage.sprite = icon;
        suitImage.color = color;

        valueText.text = value;
        valueText.color = color;
    }
}
