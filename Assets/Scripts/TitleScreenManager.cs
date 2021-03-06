﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreenManager : MonoBehaviour {

    private void Start()
    {
        Input.backButtonLeavesApp = false;
    }

	public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void ChangeBackgroundColor()
    {
        Camera.main.backgroundColor = Random.ColorHSV();
    }
}
