using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using InControl;
using UnityEngine.SceneManagement;

public class MultiTouchManager : MonoBehaviour {

    public Text touchText;

	private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene("Title");
        }
        touchText.text = TouchManager.TouchCount.ToString();
    }
}
