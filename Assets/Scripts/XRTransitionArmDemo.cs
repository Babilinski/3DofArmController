using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XRTransitionArmDemo : MonoBehaviour
{

    public bool IsThrowing;
    [SerializeField]
    private XRTransitionArm transitionArm;
    [SerializeField]
    private XRArm pointArm;
    [SerializeField]
    private XRArm throwArm;

    private bool _isThrowing;

	// Update is called once per frame
	void Update () {
	    if (_isThrowing != IsThrowing)
	    {
	        if (IsThrowing)
	        {
	            transitionArm.TransitionToArmModel(throwArm);
	        }
	        else
	        {
	            transitionArm.TransitionToArmModel(pointArm);
            }

	        _isThrowing = IsThrowing;
	    }
	}
}
