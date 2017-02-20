using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class GyroManager : MonoBehaviour {
    
    [Header("Player Movement")]
    private Rigidbody2D body;
    public float speed = 0.1f;
    [Header("Circles")]
    public GameObject[] circles;
    private bool inCircle;
    [Header("Play Area")]
    public Vector2 bounds;
    [Header("UI")]
    public Text scoreUI;
    public Text timerUI;
    private int _score;
    private int score
    {
        get { return _score; }
        set {
            _score = Mathf.Clamp(value, 0, 9999999);
            scoreUI.text = _score.ToString();
        }
    }
    private float _timer = 60;
    private float timer
    {
        get { return _timer; }
        set {
            _timer = Mathf.Clamp(value, 0, 60);
            timerUI.text = _timer.ToString("F");
        }
    }

    private void Start()
    {
        body = GetComponent<Rigidbody2D>();
        SpawnNewCircle();
        StartCoroutine(SpawnTimer());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene("Title");
        }
        timer -= Time.deltaTime;
        if (inCircle)
        {
            score += 1;
        }
        body.AddForce(new Vector2(Input.acceleration.x, Input.acceleration.y) * speed);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<SpriteRenderer>() != null)
        {
            other.GetComponent<SpriteRenderer>().color = Color.green;
            inCircle = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<SpriteRenderer>() != null)
        {
            other.GetComponent<SpriteRenderer>().color = Color.red;
            inCircle = false;
        }
    }

    private int FindAvailableCircle()
    {
        for (int i = 0; i < circles.Length; i++)
        {
            if (!circles[i].activeSelf)
            {
                return i;
            }
        }
        return -1;
    }

    public void SpawnNewCircle()
    {
        if (timer == 0) return;
        int circleIndex = FindAvailableCircle();
        if (circleIndex == -1) return;
        float x = Random.Range(-bounds.x, bounds.x);
        float y = Random.Range(-bounds.y, bounds.y);
        circles[circleIndex].SetActive(true);
        circles[circleIndex].transform.position = new Vector2(x, y);
    }

    private IEnumerator SpawnTimer()
    {
        float time = Random.Range(0, 3);
        yield return new WaitForSeconds(time);
        SpawnNewCircle();
        StartCoroutine(SpawnTimer());
    }
}
