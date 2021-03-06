﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleCircle : MonoBehaviour {
    [Range(0, 0.1f)]
    public float shrinkSpeed;
    private Vector3 startingScale;
    private GyroManager manager;
    private Collider2D collider;
    private SpriteRenderer sprite;
    private bool settingUp;

    private void Awake()
    {
        manager = GameObject.FindGameObjectWithTag("Player").GetComponent<GyroManager>();
        collider = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();
        startingScale = transform.localScale;
        gameObject.SetActive(false);
    }

	private void OnEnable()
    {
        transform.localScale = startingScale;
        StartCoroutine(ScaleDown());
    }
	
	private IEnumerator ScaleDown()
    {
        collider.enabled = false;
        yield return new WaitForSeconds(1);
        collider.enabled = true;
        while (true)
        {
            transform.localScale = new Vector2(transform.localScale.x - shrinkSpeed, transform.localScale.y - shrinkSpeed);
            if(transform.localScale.x <= 0)
            {
                manager.SpawnNewCircle();
                gameObject.SetActive(false);
            }
            yield return new WaitForFixedUpdate();
        }
    }
}
