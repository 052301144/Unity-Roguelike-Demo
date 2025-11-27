using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DeathTextBlink : MonoBehaviour
{
    public float blinkSpeed = 3f;
    private TextMeshProUGUI text;

    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        float alpha = Mathf.Sin(Time.time * blinkSpeed) * 0.5f + 0.5f;
        text.color = new Color(text.color.r, text.color.g, text.color.b, alpha);
    }
}
