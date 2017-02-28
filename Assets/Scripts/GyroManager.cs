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
    private int numberOfCirclesCurrentlyIn;
    [Header("Play Area")]
    public Vector2 bounds;
    public float uiPadding;
    [Header("Colors")]
    public Color inActive;
    public Color active;
    [Header("UI")]
    public GameObject panelGameStart;
    public Text scoreUI;
    public Text timerUI;
    private int _thisScore;
    private int thisScore
    {
        get { return _thisScore; }
        set
        {
            _thisScore = Mathf.Clamp(value, 0, 9999999);
            //scoreUI.text = _score.ToString();
        }
    }
    private int _score;
    private int score
    {
        get { return _score; }
        set {
            _score = Mathf.Clamp(value, 0, 9999999);
            scoreUI.text = _score.ToString();
        }
    }
    private int _combo;
    private int combo
    {
        get { return _combo; }
        set
        {
            _combo = Mathf.Clamp(value, 1, 4);
            //scoreUI.text = _score.ToString();
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

    private enum CurrentState
    {
        TitleScreen,
        StartingGame,
        PlayingGame,
        EndingGame
    }
    private CurrentState currentState;

    private void Start()
    {
        body = GetComponent<Rigidbody2D>();
        currentState = CurrentState.TitleScreen;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene("Title");
        }
        else if(currentState == CurrentState.TitleScreen)
        {
            if(Input.GetMouseButtonDown(0)) StartCoroutine(StartPlaying());
        }
        else if (currentState == CurrentState.PlayingGame)
        {
            timer -= Time.deltaTime;
            if (numberOfCirclesCurrentlyIn > 0) {
                thisScore += 1;
                combo = score % 100;
                score += 1 * combo;
                scoreUI.color = active;
            }
            else
            {
                scoreUI.color = inActive;
            }
            body.AddForce(new Vector2(Input.acceleration.x, Input.acceleration.y) * speed);

            if(timer == 0)
            {
                currentState = CurrentState.EndingGame;
            }
        }
        else if(currentState == CurrentState.EndingGame)
        {
            gameObject.transform.position = Vector3.MoveTowards(gameObject.transform.position, Vector3.zero, Time.deltaTime * 2.5f);
            if(gameObject.transform.position == Vector3.zero && AllCirclesInActive())
            {
                panelGameStart.SetActive(true);
                score = 0;
                currentState = CurrentState.TitleScreen;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<SpriteRenderer>() != null)
        {
            numberOfCirclesCurrentlyIn++;
            SpriteRenderer sr = other.GetComponent<SpriteRenderer>();
            sr.sortingOrder++;
            sr.color = active;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<SpriteRenderer>() != null)
        {
            numberOfCirclesCurrentlyIn--;
            SpriteRenderer sr = other.GetComponent<SpriteRenderer>();
            sr.sortingOrder--;
            sr.color = inActive;
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

    private bool AllCirclesInActive()
    {
        for (int i = 0; i < circles.Length; i++)
        {
            if (circles[i].activeSelf)
            {
                return false;
            }
        }
        return true;
    }

    public void SpawnNewCircle()
    {
        if (timer == 0) return;
        int circleIndex = FindAvailableCircle();
        if (circleIndex == -1) return;
        float x = Random.Range(-bounds.x, bounds.x);
        float y = Random.Range(-bounds.y, bounds.y - uiPadding);
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
        currentState = CurrentState.StartingGame;
        timer = Timer;
        Animator anim = panelGameStart.GetComponent<Animator>();
        anim.enabled = true;
        yield return new WaitForSeconds(2);
        currentState = CurrentState.PlayingGame;
        anim.enabled = false;
        panelGameStart.SetActive(false);
        panelGameStart.transform.rotation = Quaternion.identity;
        SpawnNewCircle();
        StartCoroutine(SpawnTimer());
        yield return null;
    }
}
