using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DancerBehaviour : MonoBehaviour
{
    [Header("Choose dance, default Salsa")]
    public bool Samba;
    public bool Silly;

    private void Start()
    {
        var animator = GetComponent<Animator>();
        if (Samba)
        {
            Silly = false;
            animator.SetBool("Samba", true);
        }
        else if (Silly)
        {
            animator.SetBool("Silly", true);
        }
    }
}
