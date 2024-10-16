using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NamruSessionManagementSystem;

public class ImvestRing : NSMS_Object
{
    [SerializeField] private MultisystemEffectHandler multisystemEffectHandler;
    [SerializeField] private MeshRenderer meshRenderer;
    Animator animator;
    [SerializeField] private BoxCollider _boxCollider;

    private void Awake()
    {
        LogInc("Awake");
        meshRenderer = GetComponent<MeshRenderer>();

        animator = GetComponent<Animator>();

        NamruLogManager.DecrementTabLevel();
    }

    void Start()
    {
        
    }

    public void ResetMe()
    {
        multisystemEffectHandler.StopAll();

        meshRenderer.enabled = true;

        animator.SetBool("b_pulse", false);

        if ( _boxCollider != null )
        {
            _boxCollider.enabled = true;
        }
    }

    public void Explode()
    {
        multisystemEffectHandler.TriggerMe();

        meshRenderer.enabled = false;

        if ( _boxCollider != null )
        {
            _boxCollider.enabled = false;
        }

        animator.SetBool( "b_pulse", false );
    }
}
