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
    [Header("Colors")]
    public Color inActive;
    public Color active;
    [Header("UI")]
    public GameObject panelGameStart;
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
    private float _timer;
    public float Timer;
    private float timer
    {
        get { return _timer; }
        set {
            _timer = Mathf.Clamp(value, 0, Mathf.Infinity);
            timerUI.text = _timer.ToString("F");
        }
    }

    private bool isPlaying;
    private bool onTitleScreen;

    private void Start()
    {
        timer = Timer;
        body = GetComponent<Rigidbody2D>();
        onTitleScreen = true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene("Title");
        }
        else if(Input.GetMouseButton(0) && !isPlaying && onTitleScreen)
        {
            StartCoroutine(StartPlaying());
            onTitleScreen = false;
        }

        if (isPlaying)
        {
            timer -= Time.deltaTime;
            if (inCircle) score += 1;
            body.AddForce(new Vector2(Input.acceleration.x, Input.acceleration.y) * speed);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<SpriteRenderer>() != null)
        {
            SpriteRenderer sr = other.GetComponent<SpriteRenderer>();
            sr.sortingOrder++;
            sr.color = active;
            inCircle = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<SpriteRenderer>() != null)
        {
            SpriteRenderer sr = other.GetComponent<SpriteRenderer>();
            sr.sortingOrder--;
            sr.color = inActive;
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

    private IEnumerator StartPlaying()
    {
        Animator anim = panelGameStart.GetComponent<Animator>();
        anim.enabled = true;
        yield return new WaitForSeconds(2);
        isPlaying = true;
        panelGameStart.SetActive(false);
        panelGameStart.transform.rotation = Quaternion.identity;
        SpawnNewCircle();
        StartCoroutine(SpawnTimer());
        yield return null;
    }
}
