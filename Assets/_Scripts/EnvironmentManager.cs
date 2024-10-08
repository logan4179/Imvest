using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentManager : MonoBehaviour
{
    //[Header("[---- REFERENCE (INTERNAL) ----]")]
    public static EnvironmentManager Instance;

    [Header("[---- REFERENCE (EXTERNAL) ----]")]
    [SerializeField] private Light[] fireLights;

    private float cachedIntensity_fireLights;

    [SerializeField] private float[] intensityGoals_fireLights;
    private float[] intensityLerpSpeeds_fireLights;
    private Color[] colorGoals_fireLights;

    [Header("[---- OTHER ----]")]
    [SerializeField] private Color[] fireColors;
    [SerializeField] private float baseLerpSpeed = 5f;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        intensityGoals_fireLights = new float[fireLights.Length];
        intensityLerpSpeeds_fireLights = new float[fireLights.Length];
        colorGoals_fireLights = new Color[fireLights.Length];
        cachedIntensity_fireLights = fireLights[0].intensity;

        for ( int i = 0; i < fireLights.Length; i++ )
        {
            CalculateFireLightGoals( i );
        }
    }

    void Update()
    {
        for ( int i = 0; i < fireLights.Length; i++ )
        {
            fireLights[i].intensity = Mathf.Lerp( 
                fireLights[i].intensity, intensityGoals_fireLights[i], intensityLerpSpeeds_fireLights[i] * Time.deltaTime 
                );

            fireLights[i].color = Color.Lerp(
                fireLights[i].color, colorGoals_fireLights[i], intensityLerpSpeeds_fireLights[i] * Time.deltaTime 
                );

            if( fireLights[i].intensity >= intensityGoals_fireLights[i] * 0.98f )
            {
                CalculateFireLightGoals( i );
            }
        }
    }

    private void CalculateFireLightGoals(int index)
    {
        intensityGoals_fireLights[index] = Random.Range(0.2f * cachedIntensity_fireLights, 1.8f * cachedIntensity_fireLights);
        intensityLerpSpeeds_fireLights[index] = Random.Range(baseLerpSpeed * 0.2f, baseLerpSpeed * 0.5f);

        colorGoals_fireLights[index] = fireColors[Random.Range(0, fireColors.Length)];
    }
}
