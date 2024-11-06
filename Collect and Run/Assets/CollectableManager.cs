using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CollectableManager : MonoBehaviour
{
    public int collectableCount;
    public TextMeshPro collectableText;
    // Start is called before the first frame update
    void Start()
    {
        collectableCount = 0;
    }

    // Update is called once per frame
    void Update()
    {
        collectableText.text = "Fruit count : " + collectableCount.ToString();
    }
}
