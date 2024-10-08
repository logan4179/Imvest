using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class MultisystemEffectHandler : MonoBehaviour
{
    [SerializeField] ParticleSystem[] prtclSystems;

    [SerializeField] private float duration = 0f;
    private float cd_stopSystem = 0f;


    void Start()
    {
        StopAll();

    }


    void Update()
    {
        if ( cd_stopSystem > 0f )
        {
            cd_stopSystem -= Time.deltaTime;

            if ( cd_stopSystem < 0f )
            {
                //StopAll();
            }
        }

        /*if ( Input.GetKeyDown(KeyCode.Space) )
        {
            TriggerMe();
        }
        else if ( Input.GetKeyDown(KeyCode.E) )
        {
            StopAll();
        }
        else if ( Input.GetKeyDown(KeyCode.C) )
        {
            ClearAll();
        }*/
    }

    [ContextMenu("z call TriggerMe()")]
    public void TriggerMe()
    {
        cd_stopSystem = duration;

        foreach (ParticleSystem system in prtclSystems)
        {
            system.Play();
        }

    }

    [ContextMenu("z call StopAll()")]
    public void StopAll()
    {
        foreach (ParticleSystem system in prtclSystems)
        {
            system.Stop();
        }

    }

    [ContextMenu("z call ClearAll()")]
    public void ClearAll()
    {
        foreach (ParticleSystem system in prtclSystems)
        {
            system.Clear();
        }

    }
}
