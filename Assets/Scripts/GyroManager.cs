using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GyroManager : MonoBehaviour {

    public Rigidbody2D circle;
    public float speed = 10;
    private SpriteRenderer sprite;

    private void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene("Title");
        }
        circle.AddForce(new Vector2(Input.acceleration.x, Input.acceleration.y) * speed);
        Debug.Log(Input.acceleration);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        sprite.color = Color.green;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        sprite.color = Color.red;
    }
}
