using Futulabs.HoloFramework.LeapMotion;
using UniRx;
using UnityEngine;
using Zenject;

public class LeapFrameTransformer : ILeapFrameTransformer
{
    private Matrix4x4               _leapToLocatableCamera;

    private Subject<LeapFrameData>  _transformedFrameStream;

    LeapFrameTransformer(
        [Inject(Id ="Frame stream")]                Subject<LeapFrameData>  frameStream,
        [Inject(Id = "Transformed frame stream")]   Subject<LeapFrameData>  transformedFrameStream,
        [Inject(Id = "Leap to locatable camera")]   Subject<Matrix4x4>      leapToLocatableCamera)
    {
        _transformedFrameStream = transformedFrameStream;

        leapToLocatableCamera.Subscribe(trans =>
        {
            _leapToLocatableCamera = trans;
        });

        frameStream.ObserveOn(Scheduler.MainThread).SubscribeOn(Scheduler.MainThread).Subscribe(frame =>
        {
            if (frame != null)
            {
                LeapArmData leftArm = frame.left_arm;
                LeapArmData rightArm = frame.right_arm;

                // Left arm
                if (leftArm != null)
                {
                    TransformArmData(ref leftArm);
                }

                // Right arm
                if (rightArm != null)
                {
                    TransformArmData(ref rightArm);
                }

                _transformedFrameStream.OnNext(frame);
            }
        });
    }

    private void TransformArmData(ref LeapArmData arm)
    {
        LeapForearmData forearm = arm.forearm;
        LeapHandData hand = arm.hand;
        LeapFingerData[] fingers = hand.fingers;

        // Fingers
        for (int i = 0; i < fingers.Length; ++i)
        {
            LeapFingerData finger = fingers[i];
            finger.TipPosition              = TransformVector(finger.TipPosition);
            finger.StabilizedTipPosition    = TransformVector(finger.StabilizedTipPosition);
            finger.FingerDirection          = TransformVector(finger.FingerDirection);
            finger.TipVelocity              = TransformVector(finger.TipVelocity);
        }
        
        // Hand
        hand.PalmPosition           = TransformVector(hand.PalmPosition);
        hand.StabilizedPalmPosition = TransformVector(hand.StabilizedPalmPosition);
        hand.PalmNormal             = TransformVector(hand.PalmNormal);
        hand.PalmVelocity           = TransformVector(hand.PalmVelocity);
        hand.PalmToFingersDirection = TransformVector(hand.PalmToFingersDirection);

        // Forearm
        forearm.WristPosition       = TransformVector(forearm.WristPosition);
        forearm.ForearmDirection    = TransformVector(forearm.ForearmDirection);
        forearm.ElbowPosition       = TransformVector(forearm.ElbowPosition);
    }

    private Vector3 TransformVector(Vector3 vec)
    {
        // Transform to homogenous coordinates
        Vector4 hom_vec = vec;
        hom_vec.w = 1.0f;
        // Transform to camera's coordinate system
        hom_vec = _leapToLocatableCamera * hom_vec;
        // Flip y-axis, since camera is modeled as a pinhole camera and as such the y-axis points down
        hom_vec.y *= -1.0f;
        hom_vec = hom_vec / hom_vec.w;
        return new Vector3(hom_vec.x, hom_vec.y, hom_vec.z);
    }
}
