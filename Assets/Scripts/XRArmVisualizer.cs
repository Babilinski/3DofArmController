using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XRArmVisualizer : MonoBehaviour {



    [SerializeField] private Transform shoulderJoint;

    [SerializeField] private Transform bicepLimb;

    [SerializeField] private Transform elbowJoint;

    [SerializeField] private Transform forearmLimb;

    [SerializeField] private Transform wristJoint;


    [SerializeField] private float wristOffset = -0.05f;

    [Tooltip("Use the XRTransitionArm as the target if you want to visualize the lerping between two arm models.")]
    public XRBaseArmModel armModel;

    private const float BICEP_SCALE_FACTOR = 4.4f;
    private const float FOREARM_SCALE_FACTOR = 3.6f;


    void LateUpdate()
    {
        Vector3 shoulderPos;
        Vector3 elbowPos;
        Vector3 wristPos;
        Quaternion shoulderRotation;
        Quaternion elbowRotation;
        Quaternion wristRotation;

        XRArm xrArm = armModel as XRArm;
        XRTransitionArm armModelVisual = armModel as XRTransitionArm;

        if(xrArm != null)
        {
            shoulderPos = xrArm.ShoulderPosition;
            elbowPos = xrArm.ElbowPosition;
            wristPos = xrArm.WristPosition;
            shoulderRotation = xrArm.ShoulderRotation;
            elbowRotation = xrArm.ElbowRotation;
            wristRotation = xrArm.WristRotation;
        }
        else if(armModelVisual != null)
        {
            shoulderPos = armModelVisual.ShoulderPosition;
            elbowPos = armModelVisual.ElbowPosition;
            wristPos = armModelVisual.WristPosition;
            shoulderRotation = armModelVisual.ShoulderRotation;
            elbowRotation = armModelVisual.ElbowRotation;
            wristRotation = armModelVisual.WristRotation;
        }
        else
        {
            return;
        }

        // Shoulder Joint.
        shoulderJoint.localPosition = shoulderPos;
        shoulderJoint.localRotation = shoulderRotation;
  

        // Elbow Joint.
        elbowJoint.localPosition = elbowPos;
        elbowJoint.localRotation = elbowRotation;



        // Bicep Limb.
        Vector3 elbowShoulderDiff = elbowJoint.localPosition - shoulderJoint.localPosition;
        Vector3 bicepPosition = shoulderJoint.localPosition + (elbowShoulderDiff * 0.5f);
        bicepLimb.localPosition = bicepPosition;
        bicepLimb.LookAt(shoulderJoint, elbowJoint.forward);
        bicepLimb.localScale = new Vector3(1.0f, 1.0f, elbowShoulderDiff.magnitude * BICEP_SCALE_FACTOR);


        // Wrist Joint.
        wristJoint.localPosition = wristPos;
        wristJoint.localRotation = wristRotation;
        Vector3 wristDir = wristRotation * Vector3.forward;
        wristJoint.localPosition = wristJoint.localPosition + (wristDir * wristOffset);


        // Forearm Limb.
        Vector3 wristElbowDiff = wristJoint.localPosition - elbowJoint.localPosition;
        Vector3 forearmPosition = elbowJoint.localPosition + (wristElbowDiff * 0.5f);
        forearmLimb.localPosition = forearmPosition;
        forearmLimb.LookAt(elbowJoint, wristJoint.up);
        forearmLimb.localScale = new Vector3(1.0f, 1.0f, wristElbowDiff.magnitude * FOREARM_SCALE_FACTOR);
    }

}
