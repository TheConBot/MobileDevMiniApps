using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using InControl;

public class MultiTouchManager : MonoBehaviour {

    public Text touchText;

	private void Update()
    {
        touchText.text = TouchManager.TouchCount.ToString();
    }
}
