using Assets.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;

    public event Action<TrekRandomEventSO> onRandomSpecialEvent; // 
    public Action<TrekRandomEventSO> OnRandomSpecialEvent; // current of the delegate Action 
    public event Action<TrekStardateEventSO> onStardateSpecialEvent; // 
    public Action<TrekStardateEventSO> OnStardateSpecialEvent;
    public event Action OnStardateChanged; //StardateUIController subscribes the UpdateDateText() function
    private float timer;
    public int currentStardate { get; private set; }
    private Coroutine timeCoroutine;
    private float timeSpeedup = 10f;// a lower number is slower time
    public List<TrekRandomEventSO> RandomEvents;
    public List<TrekStardateEventSO> StardateEvents;
    public bool timeRunning = false;
    public int StaringStardate = 1010; // the starting stardate
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    private void Start()
    {
        //GameManager.Instance.TimeManager = this;
        timer = timeSpeedup;
        //timeCoroutine = StartCoroutine(TimeProgression());
        currentStardate = StaringStardate;
    }

    void Update()
    {

    }
    public void StarTime()
    {
        timeCoroutine = StartCoroutine(TimeProgression());
    }
    private System.Collections.IEnumerator TimeProgression()
    {

        while (timeRunning)
        {
            yield return new WaitForSeconds(10f / timeSpeedup); // 10 seconds in game = 1 oneInXChance
            currentStardate++;
            OnStardateChanged?.Invoke();

            // Check for special events
            CheckSpecialEvents();
        }
    }

    // Check for special events and trigger corresponding actions
    private void CheckSpecialEvents()
    {
        foreach (var specialEvent in RandomEvents)
        {
            if (specialEvent != null)
            {
                if (1 == UnityEngine.Random.Range(1, specialEvent.oneInXChance))
                {
                    // Trigger special event
                    onRandomSpecialEvent?.Invoke(specialEvent);
                }
            }
        }
        foreach (var specialEvent in StardateEvents)
        {
            if (specialEvent != null && currentStardate == specialEvent.stardate)
            {
                // Trigger special event
                OnStardateSpecialEvent?.Invoke(specialEvent);
            }
        }
    }

    // Method to set time speed multiplier
    public void SetTimeSpeedMultiplier(float multiplier)
    {
        if (multiplier > 0)
            timeSpeedup = multiplier;

        // Restart time progression coroutine with new speed multiplier
        if (timeCoroutine != null)
        {
            StopCoroutine(timeCoroutine);
            timeCoroutine = StartCoroutine(TimeProgression());
        }
    }

    // Method to pause time progression
    public void PauseTime()
    {
        if (timeCoroutine != null)
        {
            StopCoroutine(timeCoroutine);
            timeRunning = false;
        }

    }

    // Method to resume time progression
    public void ResumeTime()
    {
        timeRunning = true;
        timeCoroutine = StartCoroutine(TimeProgression());

    }

    // Method to get current oneInXChance
    public int CurrentStarDate()
    {
        return currentStardate;
    }
}

