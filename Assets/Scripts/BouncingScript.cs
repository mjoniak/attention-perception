using System;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class BouncingScript : MonoBehaviour
{
    public float PeriodSec = 2.0f;
    public float SoundPhaseShiftSec = 0.5f;
    public float ColorChangeTimeSec = 0.0f;
    public float DelaySec = 0.0f;
    public float G = 10.0f;
    public AudioClip BounceSound;
    public Material FinalMaterial;

    private float topY;
    private int bounceCount = 0;
    private AudioSource[] audioSources;
    private int flip = 0;
    private float timeStart;

    private Color initialColor;
    private Color? finalColor = null;

    // Start is called before the first frame update
    void Start()
    {
        topY = transform.position.y + 0.125f * G * (PeriodSec * PeriodSec);

        if (BounceSound != null) {
            audioSources = new AudioSource[2] { gameObject.AddComponent<AudioSource>(), gameObject.AddComponent<AudioSource>() };
            audioSources[0].clip = BounceSound;
            audioSources[1].clip = BounceSound;
        }

        timeStart = Time.time;
        initialColor = GetComponent<Renderer>().material.color;
        if (FinalMaterial != null) finalColor = FinalMaterial.color;
    }

    // Update is called once per frame
    void Update()
    {
        float time = Time.time - timeStart;
        float t = ((time + DelaySec) % PeriodSec) - 0.5f * PeriodSec;
        transform.position = new Vector3(transform.position.x, topY - 0.5f * G * t * t, transform.position.z);

        if (BounceSound != null) {
            double cycleStart = bounceCount * PeriodSec + SoundPhaseShiftSec;
            if (time > cycleStart) {
                double dspTime = AudioSettings.dspTime;
                double nextBounceDspTime = dspTime - time + cycleStart;
                if (nextBounceDspTime > .0) {
                    audioSources[flip].PlayScheduled(nextBounceDspTime);
                    flip = 1 - flip;
                    // Debug.Log("Ball will bounce at " + nextBounceDspTime + " current time " + dspTime + " bounces " + bounceCount);
                    bounceCount++;
                }
            }
        }

        if (ColorChangeTimeSec > 0.0f && finalColor != null) {
            float colorChangeRatio = Math.Min(1.0f, time / ColorChangeTimeSec);
            var newColor = (1.0f - colorChangeRatio) * initialColor + colorChangeRatio * finalColor.Value;
            Debug.Log(newColor);
            GetComponent<Renderer>().material.color = newColor;
        }
    }
}
