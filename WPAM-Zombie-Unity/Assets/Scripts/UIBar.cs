using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIBar : MonoBehaviour
{
    public Text Text;
    public Transform Fill;

    public void ReceiveUpdate(string text, float fillPercentage)
    {
        Text.text = text;
        Fill.localScale = new Vector3(fillPercentage, 1, 1);
    }
}
