using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.XR;
//Uses the base class of Arm Model
public class XRArm : XRBaseArmModel
{
    //Track if the user is left handed
    public static bool IsLeftHanded;

    //Used to quickly reference the correct hand
    public static XRNode GetDominantHand()
    {
        return IsLeftHanded ? XRNode.LeftHand : XRNode.RightHand;
    }

    //Used to store the state of the tracked devices
    private List<XRNodeState> nodeStates = new List<XRNodeState>();

    [SerializeField]
    private bool debugStatus;

    /// Position of the elbow joint relative to the head before the arm model is applied.
    public Vector3 elbowRestPosition = DEFAULT_ELBOW_REST_POSITION;

    /// Position of the wrist joint relative to the elbow before the arm model is applied.
    public Vector3 wristRestPosition = DEFAULT_WRIST_REST_POSITION;

    /// Position of the controller joint relative to the wrist before the arm model is applied.
    public Vector3 controllerRestPosition = DEFAULT_CONTROLLER_REST_POSITION;

    /// Offset applied to the elbow position as the controller is rotated upwards.
    public Vector3 armExtensionOffset = DEFAULT_ARM_EXTENSION_OFFSET;

    /// pivot elbow with torso and hold wrist stiff, for throwing
    [Tooltip("Pivot elbow with torso and hold wrist stiff, used for throwing")]
    public bool ShoulderPivot = false;

    /// Ratio of the controller's rotation to apply to the rotation of the elbow.
    /// The remaining rotation is applied to the wrist's rotation.
    [Range(0.0f, 1.0f)]
    public float elbowBendRatio = DEFAULT_ELBOW_BEND_RATIO;

    /// Represents the controller's position relative to the user's head.
    public Vector3 ControllerPositionFromHead
    {
        get
        {
            return controllerPosition;
        }
    }

    /// Represent the controller's rotation relative to the user's head.
    public Quaternion ControllerRotationFromHead
    {
        get
        {
            return controllerRotation;
        }
    }

    /// Represent the neck's position relative to the user's head.
    /// If isLockedToNeck is true, this will be the InputTracking position of the Head node modified
    /// by an inverse neck model to approximate the neck position.
    /// Otherwise, it is always zero.
    public Vector3 NeckPosition
    {
        get
        {
            return neckPosition;
        }
    }

    /// Represent the shoulder's position relative to the user's head.
    /// This is not actually used as part of the arm model calculations, and exists for debugging.
    public Vector3 ShoulderPosition
    {
        get
        {
            Vector3 shoulderPosition = neckPosition + torsoRotation * Vector3.Scale(SHOULDER_POSITION, handedMultiplier);
            return shoulderPosition;
        }
    }

    /// Represent the shoulder's rotation relative to the user's head.
    /// This is not actually used as part of the arm model calculations, and exists for debugging.
    public Quaternion ShoulderRotation
    {
        get
        {
            return torsoRotation;
        }
    }

    /// Represent the elbow's position relative to the user's head.
    public Vector3 ElbowPosition
    {
        get
        {
            return elbowPosition;
        }
    }

    /// Represent the elbow's rotation relative to the user's head.
    public Quaternion ElbowRotation
    {
        get
        {
            return elbowRotation;
        }
    }

    /// Represent the wrist's position relative to the user's head.
    public Vector3 WristPosition
    {
        get
        {
            return wristPosition;
        }
    }

    /// Represent the wrist's rotation relative to the user's head.
    public Quaternion WristRotation
    {
        get
        {
            return wristRotation;
        }
    }

    //calculated values, duplicated from properties above.
    private Vector3 neckPosition;
    private Vector3 elbowPosition;
    private Quaternion elbowRotation;
    private Vector3 wristPosition;
    private Quaternion wristRotation;
    private Vector3 controllerPosition;
    private Quaternion controllerRotation;


    /// Multiplier for handedness such that 1 = Right, 0 = Center, -1 = left.
    private Vector3 handedMultiplier;

    /// Forward direction of user's torso.
    private Vector3 torsoDirection;

    /// Orientation of the user's torso.
    private Quaternion torsoRotation;

    // The rotation of the tracked controller
    private Quaternion localControllerRotation;

    //Used to offset the shoulder position from the neck
    private Vector3 shoulderOffset = Vector3.zero;

    // Default values for tuning variables.
    public static readonly Vector3 DEFAULT_ELBOW_REST_POSITION = new Vector3(0.195f, -0.5f, 0.005f);
    public static readonly Vector3 DEFAULT_WRIST_REST_POSITION = new Vector3(0.0f, 0.0f, 0.25f);
    public static readonly Vector3 DEFAULT_CONTROLLER_REST_POSITION = new Vector3(0.0f, 0.0f, 0.05f);
    public static readonly Vector3 DEFAULT_ARM_EXTENSION_OFFSET = new Vector3(-0.13f, 0.14f, 0.08f);
    public const float DEFAULT_ELBOW_BEND_RATIO = 0.6f;

    /// Increases elbow bending as the controller moves up (unitless).
    private const float EXTENSION_WEIGHT = 0.4f;

    /// Rest position for shoulder joint.
    private static readonly Vector3 SHOULDER_POSITION = new Vector3(0.17f, -0.2f, -0.03f);

    /// Angle ranges the for arm extension offset to start and end (degrees).
    private const float MIN_EXTENSION_ANGLE = 7.0f;
    private const float MAX_EXTENSION_ANGLE = 60.0f;

    //Called once per frame
    void Update()
    {

        //Assign current node states to the list
        InputTracking.GetNodeStates(nodeStates);

        //The default value for the angular velocity is zero. This value stays zero if 
        //The current device doesn't support getting the angular velocity using the XRNode.
        Vector3 angularVelocity = Vector3.zero;

        //Iterate through the list and find the XRNode that is the "Head"
        for(int i = 0; i < nodeStates.Count; i++)
        {
            if(nodeStates[i].nodeType == XRNode.Head)
            {
                //Once we find the headset, we try to get the angularVelocity. This generates garbage.
                nodeStates[i].TryGetAngularVelocity(out angularVelocity);
                break;
            }
        }

        Quaternion handRotation = InputTracking.GetLocalRotation(GetDominantHand());
        Vector3 gazeDirection = InputTracking.GetLocalRotation(XRNode.Head) * Vector3.forward;
        Vector3 headPosition = InputTracking.GetLocalPosition(XRNode.Head);

        if(debugStatus)
        {
            Debug.Log("The player is using their left hand = " + IsLeftHanded);
            Debug.Log("Angular Velocity = " + angularVelocity);
            Debug.Log("Hand Rotation = " + handRotation);
            Debug.Log("Gaze Direction = " + gazeDirection);
            Debug.Log("Head Position = " + headPosition);
        }


        //Update our arm calculation
        UpdateHandData(IsLeftHanded, handRotation, gazeDirection, angularVelocity, headPosition);
    }


    public void UpdateHandData(bool isLeftHanded, Quaternion controllerRotation,
        Vector3 gazeDirection,
        Vector3 angularVelocity,
        Vector3 headPosition)
    {
        //Handedness//
        //Assign Left VS Right hand multiplier
        UpdateHandedness(isLeftHanded);
        //Store local controller rotation
        UpdateControllerReferenceRotation(controllerRotation);


        //Torso Direction//
        //We calculate the direction of the torso
        UpdateTorsoDirection(gazeDirection, angularVelocity);

        //Neck Position//
        neckPosition = headPosition;

        //Finish calculation
        ApplyArmModel();

    }

    //Set the handedMultiplier parameter
    private void UpdateHandedness(bool isLeftHanded)
    {
        // Determine handedness multiplier.
        handedMultiplier.Set(0, 1, 1);
        if(isLeftHanded)
        {
            handedMultiplier.x = -1.0f;
        }
        else
        {
            handedMultiplier.x = 1.0f;
        }
    }

    private void UpdateControllerReferenceRotation(Quaternion localRotation)
    {
        localControllerRotation = localRotation;
    }

    private void UpdateTorsoDirection(Vector3 gazeDirection, Vector3 angularVelocity)
    {
        Vector3 leveledGazeDirection = gazeDirection;
        leveledGazeDirection.y = 0.0f;
        leveledGazeDirection.Normalize();

        float angularMagnitude = angularVelocity.magnitude;
        float gazeFilterStrength = Mathf.Clamp((angularMagnitude - 0.2f) / 45.0f, 0.0f, 0.1f);

        //Set the torso Direction
        torsoDirection = Vector3.Slerp(torsoDirection, leveledGazeDirection, gazeFilterStrength);

        // Calculate the torso rotation.
        torsoRotation = Quaternion.FromToRotation(Vector3.forward, torsoDirection);
    }

    private void ApplyArmModel()
    {
        // Set the starting positions of the joints before they are transformed by the arm model.
        SetUntransformedJointPositions();

        Quaternion controllerOrientation;
        Quaternion xyRotation;
        float xAngle;
        GetControllerRotation(out controllerOrientation, out xyRotation, out xAngle);


        // Offset the elbow by the extension offset.
        float extensionRatio = CalculateExtensionRatio(xAngle);
        ApplyExtensionOffset(extensionRatio);



        // Calculate the lerp rotation, which is used to control how much the rotation of the
        // controller impacts each joint.
        Quaternion lerpRotation = CalculateLerpRotation(xyRotation, extensionRatio);

        CalculateFinalJointRotations(controllerOrientation, xyRotation, lerpRotation);
        ApplyRotationToJoints();
    }

    /// Set the starting positions of the joints before they are transformed by the arm model.
    protected virtual void SetUntransformedJointPositions()
    {
        elbowPosition = Vector3.Scale(elbowRestPosition, handedMultiplier);
        wristPosition = Vector3.Scale(wristRestPosition, handedMultiplier);
        controllerPosition = Vector3.Scale(controllerRestPosition, handedMultiplier);
    }

    /// Calculate the extension ratio based on the angle of the controller along the x axis.
    protected virtual float CalculateExtensionRatio(float xAngle)
    {
        float normalizedAngle = (xAngle - MIN_EXTENSION_ANGLE) / (MAX_EXTENSION_ANGLE - MIN_EXTENSION_ANGLE);
        float extensionRatio = Mathf.Clamp(normalizedAngle, 0.0f, 1.0f);
        return extensionRatio;
    }

    /// Offset the elbow by the extension offset.
    protected virtual void ApplyExtensionOffset(float extensionRatio)
    {
        Vector3 extensionOffset = Vector3.Scale(armExtensionOffset, handedMultiplier);
        elbowPosition += extensionOffset * extensionRatio;
    }

    /// Calculate the lerp rotation, which is used to control how much the rotation of the
    /// controller impacts each joint.
    protected virtual Quaternion CalculateLerpRotation(Quaternion xyRotation, float extensionRatio)
    {
        float totalAngle = Quaternion.Angle(xyRotation, Quaternion.identity);
        float lerpSuppresion = 1.0f - Mathf.Pow(totalAngle / 180.0f, 6.0f);
        float inverseElbowBendRatio = 1.0f - elbowBendRatio;
        float lerpValue = inverseElbowBendRatio + elbowBendRatio * extensionRatio * EXTENSION_WEIGHT;
        lerpValue *= lerpSuppresion;
        return Quaternion.Lerp(Quaternion.identity, xyRotation, lerpValue);
    }

    /// Determine the final joint rotations relative to the head.
    protected virtual void CalculateFinalJointRotations(Quaternion controllerOrientation, Quaternion xyRotation, Quaternion lerpRotation)
    {
        elbowRotation = torsoRotation * Quaternion.Inverse(lerpRotation) * xyRotation;
        wristRotation = localControllerRotation * lerpRotation;
        controllerRotation = torsoRotation * controllerOrientation;
    }

    /// Apply the joint rotations to the positions of the joints to determine the final pose.
    protected virtual void ApplyRotationToJoints()
    {
        if(ShoulderPivot)
        {
            Vector3 shoulderPosition = neckPosition + torsoRotation * shoulderOffset;
            elbowPosition = shoulderPosition + elbowRotation * elbowPosition;
            wristPosition = elbowPosition + wristRotation * wristPosition;
            controllerPosition = wristPosition + controllerPosition;
        }
        else
        {
            elbowPosition = neckPosition + torsoRotation * elbowPosition;
            wristPosition = elbowPosition + elbowRotation * wristPosition;
            controllerPosition = wristPosition + wristRotation * controllerPosition;
        }
    }

    /// Get the controller's orientation.
    protected void GetControllerRotation(out Quaternion rotation, out Quaternion xyRotation, out float xAngle)
    {
        // Find the controller's orientation relative to the player.
        rotation = localControllerRotation;
        rotation = Quaternion.Inverse(torsoRotation) * rotation;

        // Extract just the x rotation angle.
        Vector3 controllerForward = rotation * Vector3.forward;
        xAngle = 90.0f - Vector3.Angle(controllerForward, Vector3.up);

        // Remove the z rotation from the controller.
        xyRotation = Quaternion.FromToRotation(Vector3.forward, controllerForward);
    }

#if UNITY_EDITOR
    protected virtual void OnDrawGizmosSelected()
    {
        if(!enabled)
        {
            return;
        }

        if(transform.parent == null)
        {
            return;
        }

        Vector3 worldShoulder = transform.parent.TransformPoint(ShoulderPosition);
        Vector3 worldElbow = transform.parent.TransformPoint(elbowPosition);
        Vector3 worldwrist = transform.parent.TransformPoint(wristPosition);
        Vector3 worldcontroller = transform.parent.TransformPoint(controllerPosition);


        Gizmos.color = Color.red;
        Gizmos.DrawSphere(worldShoulder, 0.02f);
        Gizmos.DrawLine(worldShoulder, worldElbow);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(worldElbow, 0.02f);
        Gizmos.DrawLine(worldElbow, worldwrist);

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(worldwrist, 0.02f);

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(worldcontroller, 0.02f);
    }
#endif // UNITY_EDITOR

}
