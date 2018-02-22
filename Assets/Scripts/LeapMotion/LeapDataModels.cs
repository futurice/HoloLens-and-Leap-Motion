using System;
using UnityEngine;

[Serializable]
public class LeapFingerData
{
    public int              type;
    public float            direction_x;
    public float            direction_y;
    public float            direction_z;
    public bool             is_extended;
    public float            tip_x;
    public float            tip_y;
    public float            tip_z;
    public float            stabilized_tip_x;
    public float            stabilized_tip_y;
    public float            stabilized_tip_z;
    public float            tip_velocity_x;
    public float            tip_velocity_y;
    public float            tip_velocity_z;

    public Vector3 TipPosition
    {
        get
        {
            return new Vector3(tip_x, tip_y, tip_z);
        }

        set
        {
            tip_x = value.x;
            tip_y = value.y;
            tip_z = value.z;
        }
    }

    public Vector3 StabilizedTipPosition
    {
        get
        {
            return new Vector3(stabilized_tip_x, stabilized_tip_y, stabilized_tip_z);
        }

        set
        {
            stabilized_tip_x = value.x;
            stabilized_tip_y = value.y;
            stabilized_tip_z = value.z;
        }
    }

    public Vector3 FingerDirection
    {
        get
        {
            return new Vector3(direction_x, direction_y, direction_z).normalized;
        }

        set
        {
            direction_x = value.x;
            direction_y = value.y;
            direction_z = value.z;
        }
    }

    public Vector3 TipVelocity
    {
        get
        {
            return new Vector3(tip_velocity_x, tip_velocity_y, tip_velocity_z);
        }

        set
        {
            tip_velocity_x = value.x;
            tip_velocity_y = value.y;
            tip_velocity_z = value.z;
        }
    }

    public bool IsExtended
    {
        get
        {
            return is_extended;
        }
    }

}

[Serializable]
public class LeapHandData
{
    public float            palm_x;
    public float            palm_y;
    public float            palm_z;
    public float            stabilized_palm_x;
    public float            stabilized_palm_y;
    public float            stabilized_palm_z;
    public float            palm_normal_x;
    public float            palm_normal_y;
    public float            palm_normal_z;
    public float            palm_velocity_x;
    public float            palm_velocity_y;
    public float            palm_velocity_z;
    public float            palm_to_fingers_x;
    public float            palm_to_fingers_y;
    public float            palm_to_fingers_z;
    public LeapFingerData[] fingers;
    public float            grab_angle;
    public float            pinch_distance;

    public Vector3 PalmPosition
    {
        get
        {
            return new Vector3(palm_x, palm_y, palm_z);
        }

        set
        {
            palm_x = value.x;
            palm_y = value.y;
            palm_z = value.z;
        }
    }

    public Vector3 StabilizedPalmPosition
    {
        get
        {
            return new Vector3(stabilized_palm_x, stabilized_palm_y, stabilized_palm_z);
        }

        set
        {
            stabilized_palm_x = value.x;
            stabilized_palm_y = value.y;
            stabilized_palm_z = value.z;
        }
    }

    public Vector3 PalmNormal
    {
        get
        {
            return new Vector3(palm_normal_x, palm_normal_y, palm_normal_z).normalized;
        }

        set
        {
            palm_normal_x = value.x;
            palm_normal_y = value.y;
            palm_normal_z = value.z;
        }
    }

    public Vector3 PalmVelocity
    {
        get
        {
            return new Vector3(palm_velocity_x, palm_velocity_y, palm_velocity_z);
        }

        set
        {
            palm_velocity_x = value.x;
            palm_velocity_y = value.y;
            palm_velocity_z = value.z;
        }
    }

    public Vector3 PalmToFingersDirection
    {
        get
        {
            return new Vector3(palm_to_fingers_x, palm_to_fingers_y, palm_to_fingers_z).normalized;
        }

        set
        {
            palm_to_fingers_x = value.x;
            palm_to_fingers_y = value.y;
            palm_to_fingers_z = value.z;
        }
    }

    public LeapFingerData Thumb
    {
        get
        {
            if (fingers != null)
            {
                for (int i = 0; i < fingers.Length; ++i)
                {
                    if (fingers[i].type == 0) return fingers[i];
                }
            }
            return null;
        }
    }

    public LeapFingerData IndexFinger
    {
        get
        {
            if (fingers != null)
            {
                for (int i = 0; i < fingers.Length; ++i)
                {
                    if (fingers[i].type == 1) return fingers[i];
                }
            }
            return null;
        }
    }

    public LeapFingerData MiddleFinger
    {
        get
        {
            if (fingers != null)
            {
                for (int i = 0; i < fingers.Length; ++i)
                {
                    if (fingers[i].type == 2) return fingers[i];
                }
            }
            return null;
        }
    }

    public LeapFingerData RingFinger
    {
        get
        {
            if (fingers != null)
            {
                for (int i = 0; i < fingers.Length; ++i)
                {
                    if (fingers[i].type == 3) return fingers[i];
                }
            }
            return null;
        }
    }

    public LeapFingerData Pinky
    {
        get
        {
            if (fingers != null)
            {
                for (int i = 0; i < fingers.Length; ++i)
                {
                    if (fingers[i].type == 4) return fingers[i];
                }
            }
            return null;
        }
    }

    public float GrabAngle
    {
        get
        {
            return grab_angle;
        }
    }

    public float PinchDistance
    {
        get
        {
            return pinch_distance;
        }
    }
}

[Serializable]
public class LeapForearmData
{
    public float            wrist_x;
    public float            wrist_y;
    public float            wrist_z;
    public float            direction_x;
    public float            direction_y;
    public float            direction_z;
    public float            elbow_x;
    public float            elbow_y;
    public float            elbow_z;

    public Vector3 WristPosition
    {
        get
        {
            return new Vector3(wrist_x, wrist_y, wrist_z);
        }
        set
        {
            wrist_x = value.x;
            wrist_y = value.y;
            wrist_z = value.z;
        }
    }

    public Vector3 ForearmDirection
    {
        get
        {
            return new Vector3(direction_x, direction_y, direction_z).normalized;
        }

        set
        {
            direction_x = value.x;
            direction_y = value.y;
            direction_z = value.z;
        }
    }

    public Vector3 ElbowPosition
    {
        get
        {
            return new Vector3(elbow_x, elbow_y, elbow_z);
        }

        set
        {
            elbow_x = value.x;
            elbow_y = value.y;
            elbow_z = value.z;
        }
    }
}

[Serializable]
public class LeapArmData
{
    public LeapForearmData  forearm;
    public LeapHandData     hand;
}

[Serializable]
public class LeapFrameData
{
    public LeapArmData     left_arm;
    public LeapArmData     right_arm;
}