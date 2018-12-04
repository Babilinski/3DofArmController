using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Use if you want to Lerp between two arm configurations, Such as pointing and throwing, use this script as the
//Main reference for position and rotation. Transition to diferent arms by using the TransitionToArmModel function.
public class XRTransitionArm : XRBaseArmModel
{
    // Stores information about a transition from one arm model to the next.
    private struct ArmTransitionInfo
    {
        public XRArm armModel;
        public float transitionValue;

        public ArmTransitionInfo(XRArm transitionArmModel)
        {
            armModel = transitionArmModel;
            transitionValue = 0.0f;
        }
    }

    [Tooltip("The transition speed between Arm Models")]
    public float TransitionSpeed = 5f;

    [Tooltip("Current Arm Model")]
    [SerializeField]
    private XRArm currentArmModel;

    /// List of transitions to other arm models. If TransitionToArmModel is called during a transition,
    /// then we need to keep information about the previous transition to make sure that the transition to
    /// the new arm model remains smooth.
    private List<ArmTransitionInfo> transitionsList = new List<ArmTransitionInfo>();

    /// Max number of active transitions that can be going on at one time.
    /// Transitions are only completed when the controller rotates, so if TransitionToArmModel
    /// is called several times without the controller moving, the number of active transitions can
    /// add up.
    private const int MAX_ACTIVE_TRANSITIONS = 10;

    /// When transitioning to a new arm model, drop any old transitions that have barely begun.
    private const float DROP_TRANSITION_THRESHOLD = 0.035f;


    public XRArm CurrentArmModel
    {
        get
        {
            return currentArmModel;
        }
    }

    public Vector3 ControllerPositionFromHead
    {
        get
        {
            if(currentArmModel == null)
            {
                return Vector3.zero;
            }

            Vector3 result = currentArmModel.ControllerPositionFromHead;

            for(int i = 0; i < transitionsList.Count; i++)
            {
                result = Vector3.Slerp(result, transitionsList[i].armModel.ControllerPositionFromHead, transitionsList[i].transitionValue);
            }

            return result;
        }
    }

    public Quaternion ControllerRotationFromHead
    {
        get
        {
            if(currentArmModel == null)
            {
                return Quaternion.identity;
            }

            Quaternion result = currentArmModel.ControllerRotationFromHead;

            for(int i = 0; i < transitionsList.Count; i++)
            {
                result = Quaternion.Slerp(result, transitionsList[i].armModel.ControllerRotationFromHead, transitionsList[i].transitionValue);
            }

            return result;
        }
    }


    public Vector3 ShoulderPosition
    {
        get
        {
            if(currentArmModel == null)
            {
                return Vector3.zero;
            }

            Vector3 result = currentArmModel.ShoulderPosition;

            for(int i = 0; i < transitionsList.Count; i++)
            {
                result = Vector3.Slerp(result, transitionsList[i].armModel.ShoulderPosition, transitionsList[i].transitionValue);
            }

            return result;
        }
    }

    public Quaternion ShoulderRotation
    {
        get
        {
            if(currentArmModel == null)
            {
                return Quaternion.identity;
            }

            Quaternion result = currentArmModel.ShoulderRotation;

            for(int i = 0; i < transitionsList.Count; i++)
            {
                result = Quaternion.Slerp(result, transitionsList[i].armModel.ShoulderRotation, transitionsList[i].transitionValue);
            }

            return result;
        }
    }

    public Vector3 ElbowPosition
    {
        get
        {
            if(currentArmModel == null)
            {
                return Vector3.zero;
            }

            Vector3 result = currentArmModel.ElbowPosition;

            for(int i = 0; i < transitionsList.Count; i++)
            {
                result = Vector3.Slerp(result, transitionsList[i].armModel.ElbowPosition, transitionsList[i].transitionValue);
            }

            return result;
        }
    }

    public Quaternion ElbowRotation
    {
        get
        {
            if(currentArmModel == null)
            {
                return Quaternion.identity;
            }

            Quaternion result = currentArmModel.ElbowRotation;

            for(int i = 0; i < transitionsList.Count; i++)
            {
                result = Quaternion.Slerp(result, transitionsList[i].armModel.ElbowRotation, transitionsList[i].transitionValue);
            }

            return result;
        }
    }

    public Vector3 WristPosition
    {
        get
        {
            if(currentArmModel == null)
            {
                return Vector3.zero;
            }

            Vector3 result = currentArmModel.WristPosition;

            for(int i = 0; i < transitionsList.Count; i++)
            {
                result = Vector3.Slerp(result, transitionsList[i].armModel.WristPosition, transitionsList[i].transitionValue);
            }

            return result;
        }
    }

    public Quaternion WristRotation
    {
        get
        {
            if(currentArmModel == null)
            {
                return Quaternion.identity;
            }

            Quaternion result = currentArmModel.WristRotation;

            for(int i = 0; i < transitionsList.Count; i++)
            {
                result = Quaternion.Slerp(result, transitionsList[i].armModel.WristRotation, transitionsList[i].transitionValue);
            }

            return result;
        }
    }

    public void TransitionToArmModel(XRArm armModel)
    {
        if(armModel == null)
        {
            return;
        }

        if(currentArmModel == null)
        {
            currentArmModel = armModel;
            return;
        }

        // Drop any old transitions that have only just begun transitioning,
        // since they won't impact how smooth the transition feels anyways.
        for(int i = transitionsList.Count - 1; i >= 0; i--)
        {
            if(transitionsList[i].transitionValue < DROP_TRANSITION_THRESHOLD)
            {
                transitionsList.RemoveAt(i);
            }
        }

        // If the max number of transitions is already active, drop the oldest.
        if(transitionsList.Count >= MAX_ACTIVE_TRANSITIONS)
        {
            transitionsList.RemoveAt(0);
        }

        transitionsList.Add(new ArmTransitionInfo(armModel));
    }

    private void Update()
    {
        if(transitionsList.Count == 0)
        {
            // Just return early if there are no transitions right now.
            return;
        }

        //GVR uses the angular velocity of the hand controller. however, we just use a fixed time. This is because
        //devices like the Oculus Go don't allow developers to access the angular velocity
        float lerpValue = Time.deltaTime * TransitionSpeed;

        // Update each transition and detect if a transition has finished.
        for(int i = transitionsList.Count - 1; i >= 0; i--)
        {
            ArmTransitionInfo transitionInfo = transitionsList[i];

            transitionInfo.transitionValue = Mathf.Lerp(transitionInfo.transitionValue, 1.0f, lerpValue);

            if(transitionInfo.transitionValue >= 0.95f)
            {
                transitionInfo.transitionValue = 1.0f;
            }


            transitionsList[i] = transitionInfo;

            // Transition is finished, so it should be removed.
            if(transitionInfo.transitionValue >= 1.0f)
            {
                // Set the current arm model to the arm model we just finished transitioning to.
                currentArmModel = transitionInfo.armModel;

                // All transitions prior to the one that finished can be removed.
                // That means we can mutate the list to remove all prior transitions and then break out of
                // the loop early.
                transitionsList.RemoveRange(0, i + 1);
                break;
            }
        }
    }
}
