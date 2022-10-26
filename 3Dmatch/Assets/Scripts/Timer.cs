using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    public float maxTime = 10f;
    public float timeLeft;
    Image TimeBar;

    public GameObject gameoverUI;

    // Start is called before the first frame update
    void Start()
    {
        TimeBar = GetComponent<Image>();
        timeLeft = maxTime;
        gameoverUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;
            TimeBar.fillAmount = timeLeft / maxTime;
        }
        else
        {
            Time.timeScale = 0f;
            gameoverUI.SetActive(true);
        }
    }
}
