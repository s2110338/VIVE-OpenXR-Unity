// Copyright HTC Corporation All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.OpenXR;
using VIVE.OpenXR.Hand;

#if UNITY_XR_HANDS
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.OpenXR;
#endif

namespace VIVE.OpenXR.Toolkits.Common
{
    public enum DeviceCategory
    {
        None = 0,
        HMD = 1,
        CenterEye = 2,
        LeftController = 3,
        RightController = 4,
        LeftHand = 5,
        RightHand = 6,
        Tracker0 = 7,
        Tracker1 = 8,
        Tracker2 = 9,
        Tracker3 = 10,
        Tracker4 = 11,
    }
    public enum PoseState
    {
        None = 0,
        IsTracked = 1,
        Position = 2,
        Rotation = 3,
        Velocity = 4,
        AngularVelocity = 5,
        Acceleration = 6,
        AngularAcceleration = 7,
    }
    public enum Handedness
    {
        None = -1,
        Right = 0,
        Left = 1,
    }
    public enum HandEvent
    {
        None = 0,
        PinchValue = 0x00000001,
        PinchPose = 0x00000002,
        GraspValue = 0x00000010,
        GraspPose = 0x00000020,
    }
    public enum ButtonEvent
    {
        None = 0,
        GripValue = 0x00000001,
        GripPress = 0x00000002,
        TriggerValue = 0x00000010,
        TriggerTouch = 0x00000020,
        TriggerPress = 0x00000040,
        Primary2DAxisValue = 0x00000100,
        Primary2DAxisTouch = 0x00000200,
        Primary2DAxisPress = 0x00000400,
        Secondary2DAxisValue = 0x00001000,
        Secondary2DAxisTouch = 0x00002000,
        Secondary2DAxisPress = 0x00004000,
        PrimaryButton = 0x00010000,
        SecondaryButton = 0x00020000,
        ParkingTouch = 0x00100000,
        Menu = 0x01000000,
    }
    public enum HandJointType : Int32
    {
        Palm = XrHandJointEXT.XR_HAND_JOINT_PALM_EXT,
        Wrist = XrHandJointEXT.XR_HAND_JOINT_WRIST_EXT,
        Thumb_Joint0 = XrHandJointEXT.XR_HAND_JOINT_THUMB_METACARPAL_EXT,
        Thumb_Joint1 = XrHandJointEXT.XR_HAND_JOINT_THUMB_PROXIMAL_EXT,
        Thumb_Joint2 = XrHandJointEXT.XR_HAND_JOINT_THUMB_DISTAL_EXT,
        Thumb_Tip = XrHandJointEXT.XR_HAND_JOINT_THUMB_TIP_EXT,
        Index_Joint0 = XrHandJointEXT.XR_HAND_JOINT_INDEX_METACARPAL_EXT,
        Index_Joint1 = XrHandJointEXT.XR_HAND_JOINT_INDEX_PROXIMAL_EXT,
        Index_Joint2 = XrHandJointEXT.XR_HAND_JOINT_INDEX_INTERMEDIATE_EXT,
        Index_Joint3 = XrHandJointEXT.XR_HAND_JOINT_INDEX_DISTAL_EXT,
        Index_Tip = XrHandJointEXT.XR_HAND_JOINT_INDEX_TIP_EXT,
        Middle_Joint0 = XrHandJointEXT.XR_HAND_JOINT_MIDDLE_METACARPAL_EXT,
        Middle_Joint1 = XrHandJointEXT.XR_HAND_JOINT_MIDDLE_PROXIMAL_EXT,
        Middle_Joint2 = XrHandJointEXT.XR_HAND_JOINT_MIDDLE_INTERMEDIATE_EXT,
        Middle_Joint3 = XrHandJointEXT.XR_HAND_JOINT_MIDDLE_DISTAL_EXT,
        Middle_Tip = XrHandJointEXT.XR_HAND_JOINT_MIDDLE_TIP_EXT,
        Ring_Joint0 = XrHandJointEXT.XR_HAND_JOINT_RING_METACARPAL_EXT,
        Ring_Joint1 = XrHandJointEXT.XR_HAND_JOINT_RING_PROXIMAL_EXT,
        Ring_Joint2 = XrHandJointEXT.XR_HAND_JOINT_RING_INTERMEDIATE_EXT,
        Ring_Joint3 = XrHandJointEXT.XR_HAND_JOINT_RING_DISTAL_EXT,
        Ring_Tip = XrHandJointEXT.XR_HAND_JOINT_RING_TIP_EXT,
        Pinky_Joint0 = XrHandJointEXT.XR_HAND_JOINT_LITTLE_METACARPAL_EXT,
        Pinky_Joint1 = XrHandJointEXT.XR_HAND_JOINT_LITTLE_PROXIMAL_EXT,
        Pinky_Joint2 = XrHandJointEXT.XR_HAND_JOINT_LITTLE_INTERMEDIATE_EXT,
        Pinky_Joint3 = XrHandJointEXT.XR_HAND_JOINT_LITTLE_DISTAL_EXT,
        Pinky_Tip = XrHandJointEXT.XR_HAND_JOINT_LITTLE_TIP_EXT,
        Count = XrHandJointEXT.XR_HAND_JOINT_MAX_ENUM_EXT,
    }

    public static class VIVEInput
    {
        private struct InputActionMapping
        {
            public DeviceCategory device;
            public PoseState poseState;
            public ButtonEvent buttonEvent;
            public HandEvent handEvent;
            public InputAction inputAction { get; private set; }

            public InputActionMapping(string bindingPath, DeviceCategory device,
                                      PoseState poseState = PoseState.None, ButtonEvent buttonEvent = ButtonEvent.None, HandEvent handEvent = HandEvent.None)
            {
                inputAction = new InputAction(binding: bindingPath);
                inputAction.Enable();
                this.device = device;
                this.poseState = poseState;
                this.buttonEvent = buttonEvent;
                this.handEvent = handEvent;
            }

            public static InputActionMapping Identify => new InputActionMapping("", DeviceCategory.None);

            public override bool Equals(object obj)
            {
                return obj is InputActionMapping inputActionMapping &&
                       device == inputActionMapping.device &&
                       poseState == inputActionMapping.poseState &&
                       buttonEvent == inputActionMapping.buttonEvent &&
                       handEvent == inputActionMapping.handEvent &&
                       inputAction == inputActionMapping.inputAction;
            }
            public override int GetHashCode()
            {
                return device.GetHashCode() ^ poseState.GetHashCode() ^ buttonEvent.GetHashCode() ^ handEvent.GetHashCode() ^ inputAction.GetHashCode();
            }
            public static bool operator ==(InputActionMapping source, InputActionMapping target) => source.Equals(target);
            public static bool operator !=(InputActionMapping source, InputActionMapping target) => !(source == (target));
        }
        private struct JointData
        {
            public bool isValid { get; private set; }
            public Vector3 position { get; private set; }
            public Quaternion rotation { get; private set; }

            public JointData(bool isValid, Vector3 position, Quaternion rotation)
            {
                this.isValid = isValid;
                this.position = position;
                this.rotation = rotation;
            }

            public static JointData Identify => new JointData(false, Vector3.zero, Quaternion.identity);
        }
        private struct HandData
        {
            public bool isTracked { get; private set; }
            public int updateTime { get; private set; }
            public JointData[] joints { get; private set; }

            public HandData(JointData[] joints)
            {
                this.joints = joints;
                isTracked = !this.joints.Any(x => x.isValid == false);
                updateTime = Time.frameCount;
            }

            public void Update(JointData[] joints)
            {
                this.joints = joints;
                isTracked = !this.joints.Any(x => x.isValid == false);
                updateTime = Time.frameCount;
            }

            public static HandData Identify
            {
                get
                {
                    JointData[] newJoints = new JointData[(int)HandJointType.Count];
                    for (int i = 0; i < newJoints.Length; i++)
                    {
                        newJoints[i] = JointData.Identify;
                    }
                    return new HandData(newJoints);
                }
            }
        }

        private static bool isInitInputActions = false;
        private static List<InputActionMapping> inputActions = new List<InputActionMapping>();
        private static HandData leftHand = HandData.Identify;
        private static HandData rightHand = HandData.Identify;
#if UNITY_XR_HANDS
        private static XRHandSubsystem handSubsystem = null;
#endif

        #region Public Interface

        /// <summary>
        /// Get the pose state of the specified device.
        /// </summary>
        /// <param name="device">The device category.</param>
        /// <param name="poseState">The pose state to be retrieved.</param>
        /// <param name="eventResult">The result of the event.</param>
        /// <returns>True if the pose state was successfully retrieved; otherwise, false.</returns>
        public static bool GetPoseState(DeviceCategory device, PoseState poseState, out bool eventResult)
        {
            CheckInitialize();
            eventResult = false;
            if ((device == DeviceCategory.LeftHand || device == DeviceCategory.RightHand) && poseState == PoseState.IsTracked)
            {
                eventResult = IsHandTracked(GetHandedness(device));
                return true;
            }
            else
            {
                InputActionMapping inputActionMapping = inputActions.FirstOrDefault(x => x.device == device && x.poseState == poseState);
				if (inputActionMapping == null) { return false; }

				try
                {
                    eventResult = inputActionMapping.inputAction.ReadValue<float>() > 0;
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Get the pose state of the specified device.
        /// </summary>
        /// <param name="device">The device category.</param>
        /// <param name="poseState">The pose state to be retrieved.</param>
        /// <param name="eventResult">The result of the event.</param>
        /// <returns>True if the pose state was successfully retrieved; otherwise, false.</returns>
        public static bool GetPoseState(DeviceCategory device, PoseState poseState, out Vector3 eventResult)
        {
            CheckInitialize();
            eventResult = Vector3.zero;
            if ((device == DeviceCategory.LeftHand || device == DeviceCategory.RightHand) && poseState == PoseState.Position)
            {
                GetJointPose(GetHandedness(device), HandJointType.Wrist, out Pose jointPose);
                eventResult = jointPose.position;
                return true;
            }
            else
            {
                InputActionMapping inputActionMapping = inputActions.FirstOrDefault(x => x.device == device && x.poseState == poseState);
				if (inputActionMapping == null) { return false; }

				try
                {
                    eventResult = inputActionMapping.inputAction.ReadValue<Vector3>();
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Get the pose state of the specified device.
        /// </summary>
        /// <param name="device">The device category.</param>
        /// <param name="poseState">The pose state to be retrieved.</param>
        /// <param name="eventResult">The result of the event.</param>
        /// <returns>True if the pose state was successfully retrieved; otherwise, false.</returns>
        public static bool GetPoseState(DeviceCategory device, PoseState poseState, out Quaternion eventResult)
        {
            CheckInitialize();
            eventResult = Quaternion.identity;
            if ((device == DeviceCategory.LeftHand || device == DeviceCategory.RightHand) && poseState == PoseState.Rotation)
            {
                GetJointPose(GetHandedness(device), HandJointType.Wrist, out Pose jointPose);
                eventResult = jointPose.rotation;
                return true;
            }
            else
            {
                InputActionMapping inputActionMapping = inputActions.FirstOrDefault(x => x.device == device && x.poseState == poseState);
                if (inputActionMapping == null) { return false; }

                try
                {
                    eventResult = inputActionMapping.inputAction.ReadValue<Quaternion>();
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Check if a specified button event has toggled at this frame and return the result.
        /// </summary>
        /// <param name="handedness">The handedness (left or right hand) to check the button event for.</param>
        /// <param name="buttonEvent">The specified button event to check.</param>
        /// <param name="eventResult">Output whether the button event has toggled.</param>
        /// <returns>Returns true if the button event was successfully retrieved, otherwise false.</returns>
        public static bool GetButtonDown(Handedness handedness, ButtonEvent buttonEvent, out bool eventResult)
        {
            CheckInitialize();
            eventResult = false;
            InputActionMapping inputActionMapping = inputActions.FirstOrDefault(x => x.device == GetController(handedness) && x.buttonEvent == buttonEvent);
            if (inputActionMapping != null)
            {
                eventResult = inputActionMapping.inputAction.WasPressedThisFrame();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check if a specified button event has toggled at this frame and return the result.
        /// </summary>
        /// <param name="handedness">The handedness (left or right hand) to check the button event for.</param>
        /// <param name="buttonEvent">The specified button event to check.</param>
        /// <param name="eventResult">Output whether the button event has toggled.</param>
        /// <returns>Returns true if the button event was successfully retrieved, otherwise false.</returns>
        public static bool GetButtonUp(Handedness handedness, ButtonEvent buttonEvent, out bool eventResult)
        {
            CheckInitialize();
            eventResult = false;
            InputActionMapping inputActionMapping = inputActions.FirstOrDefault(x => x.device == GetController(handedness) && x.buttonEvent == buttonEvent);
            if (inputActionMapping != null)
            {
                eventResult = inputActionMapping.inputAction.WasReleasedThisFrame();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check if a specified button event has toggled and return the result.
        /// </summary>
        /// <param name="handedness">The handedness (left or right hand) to check the button event for.</param>
        /// <param name="buttonEvent">The specified button event to check.</param>
        /// <param name="eventResult">Output for the button event.</param>
        /// <returns>Returns true if the button event was successfully retrieved, otherwise false.</returns>
        public static bool GetButtonValue(Handedness handedness, ButtonEvent buttonEvent, out bool eventResult)
        {
            CheckInitialize();
            eventResult = false;
            InputActionMapping inputActionMapping = inputActions.FirstOrDefault(x => x.device == GetController(handedness) && x.buttonEvent == buttonEvent);
            if (inputActionMapping != null)
            {
                try
                {
                    eventResult = inputActionMapping.inputAction.ReadValue<float>() == 1;
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
            return false;

        }

        /// <summary>
        /// Check if a specified button event has toggled and return the result.
        /// </summary>
        /// <param name="handedness">The handedness (left or right hand) to check the button event for.</param>
        /// <param name="buttonEvent">The specified button event to check.</param>
        /// <param name="eventResult">Output for the button event.</param>
        /// <returns>Returns true if the button event was successfully retrieved, otherwise false.</returns>
        public static bool GetButtonValue(Handedness handedness, ButtonEvent buttonEvent, out float eventResult)
        {
            CheckInitialize();
            eventResult = 0f;
            InputActionMapping inputActionMapping = inputActions.FirstOrDefault(x => x.device == GetController(handedness) && x.buttonEvent == buttonEvent);
            if (inputActionMapping != null)
            {
                try
                {
                    eventResult = inputActionMapping.inputAction.ReadValue<float>();
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
            return false;

        }

        /// <summary>
        /// Check if a specified button event has toggled and return the result.
        /// </summary>
        /// <param name="handedness">The handedness (left or right hand) to check the button event for.</param>
        /// <param name="buttonEvent">The specified button event to check.</param>
        /// <param name="eventResult">Output for the button event.</param>
        /// <returns>Returns true if the button event was successfully retrieved, otherwise false.</returns>
        public static bool GetButtonValue(Handedness handedness, ButtonEvent buttonEvent, out Vector2 eventResult)
        {
            CheckInitialize();
            eventResult = Vector2.zero;
            InputActionMapping inputActionMapping = inputActions.FirstOrDefault(x => x.device == GetController(handedness) && x.buttonEvent == buttonEvent);
            if (inputActionMapping != null)
            {
                try
                {
                    eventResult = inputActionMapping.inputAction.ReadValue<Vector2>();
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if a specified hand event has toggled and return the result.
        /// </summary>
        /// <param name="handedness">The handedness (left or right hand) to check the hand event for.</param>
        /// <param name="handEvent">The specified hand event to check.</param>
        /// <param name="eventResult">Output for the hand event.</param>
        /// <returns>Returns true if the hand event was successfully retrieved, otherwise false.</returns>
        public static bool GetHandValue(Handedness handedness, HandEvent handEvent, out float eventResult)
        {
            CheckInitialize();
            eventResult = 0;
            InputActionMapping inputActionMapping = inputActions.FirstOrDefault(x => x.device == GetHand(handedness) && x.handEvent == handEvent);
            if (inputActionMapping != null)
            {
                try
                {
                    eventResult = inputActionMapping.inputAction.ReadValue<float>();
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if a specified hand event has toggled and return the result.
        /// </summary>
        /// <param name="handedness">The handedness (left or right hand) to check the hand event for.</param>
        /// <param name="handEvent">The specified hand event to check.</param>
        /// <param name="eventResult">Output for the hand event.</param>
        /// <returns>Returns true if the hand event was successfully retrieved, otherwise false.</returns>
        public static bool GetHandValue(Handedness handedness, HandEvent handEvent, out Pose eventResult)
        {
            CheckInitialize();
            eventResult = Pose.identity;
            InputActionMapping inputActionMapping = inputActions.FirstOrDefault(x => x.device == GetHand(handedness) && x.handEvent == handEvent);
            if (inputActionMapping != null)
            {
                try
                {
                    UnityEngine.XR.OpenXR.Input.Pose pose = inputActionMapping.inputAction.ReadValue<UnityEngine.XR.OpenXR.Input.Pose>();
                    eventResult = new Pose(pose.position, pose.rotation);
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Retrieves the pose of a specified hand joint for the given handedness.
        /// </summary>
        /// <param name="handedness">The handedness (left or right hand) to get the joint pose for.</param>
        /// <param name="joint">The specific hand joint to retrieve the pose of.</param>
        /// <param name="jointPose">Outputs the pose of the specified hand joint.</param>
        /// <returns>Returns true if the joint pose was successfully retrieved, otherwise false.</returns>
        public static bool GetJointPose(Handedness handedness, HandJointType joint, out Pose jointPose)
        {
            CheckHandUpdated();
            jointPose = Pose.identity;
            if (handedness == Handedness.Left)
            {
                jointPose = new Pose(leftHand.joints[(int)joint].position, leftHand.joints[(int)joint].rotation);
                return leftHand.joints[(int)joint].isValid;
            }
            else
            {
                jointPose = new Pose(rightHand.joints[(int)joint].position, rightHand.joints[(int)joint].rotation);
                return rightHand.joints[(int)joint].isValid;
            }
        }

        /// <summary>
        /// Determines if the specified hand is currently being tracked.
        /// </summary>
        /// <param name="handedness">The handedness (left or right hand) to check for tracking.</param>
        /// <returns>Returns true if the specified hand is being tracked, otherwise false.</returns>
        public static bool IsHandTracked(Handedness handedness)
        {
            CheckHandUpdated();
            return handedness == Handedness.Left ? leftHand.isTracked : rightHand.isTracked;
        }

        public static bool IsHandValidate()
        {
            ViveHandTracking viveHand = OpenXRSettings.Instance.GetFeature<ViveHandTracking>();
            if (viveHand)
            {
                return true;
            }
#if UNITY_XR_HANDS
            HandTracking xrHand = OpenXRSettings.Instance.GetFeature<HandTracking>();
            if (xrHand)
            {
                return true;
            }
#endif
            return false;
        }

        #endregion

        [RuntimeInitializeOnLoadMethod]
        private static bool CheckInitialize()
        {
            if (!isInitInputActions)
            {
                Initialized();
                isInitInputActions = true;
            }
            return isInitInputActions;
        }

        private static void Initialized()
        {
            #region Head
            inputActions.Add(new InputActionMapping("<XRHMD>/isTracked", DeviceCategory.HMD, poseState: PoseState.IsTracked));
            inputActions.Add(new InputActionMapping("<XRHMD>/centerEyePosition", DeviceCategory.HMD, poseState: PoseState.Position));
            inputActions.Add(new InputActionMapping("<XRHMD>/centerEyeRotation", DeviceCategory.HMD, poseState: PoseState.Rotation));
            inputActions.Add(new InputActionMapping("<XRHMD>/centerEyeVelocity", DeviceCategory.HMD, poseState: PoseState.Velocity));
            inputActions.Add(new InputActionMapping("<XRHMD>/centerEyeAngularVelocity", DeviceCategory.HMD, poseState: PoseState.AngularVelocity));
            inputActions.Add(new InputActionMapping("<XRHMD>/centerEyeAcceleration", DeviceCategory.HMD, poseState: PoseState.Acceleration));
            inputActions.Add(new InputActionMapping("<XRHMD>/centerEyeAngularAcceleration", DeviceCategory.HMD, poseState: PoseState.AngularAcceleration));
            #endregion
            #region Eye
            inputActions.Add(new InputActionMapping("<EyeGaze>/pose/isTracked", DeviceCategory.CenterEye, poseState: PoseState.IsTracked));
            inputActions.Add(new InputActionMapping("<EyeGaze>/pose/position", DeviceCategory.CenterEye, poseState: PoseState.Position));
            inputActions.Add(new InputActionMapping("<EyeGaze>/pose/rotation", DeviceCategory.CenterEye, poseState: PoseState.Rotation));
            inputActions.Add(new InputActionMapping("<EyeGaze>/pose/velocity", DeviceCategory.CenterEye, poseState: PoseState.Velocity));
            inputActions.Add(new InputActionMapping("<EyeGaze>/pose/angularVelocity", DeviceCategory.CenterEye, poseState: PoseState.AngularVelocity));
            #endregion
            #region Controller
            inputActions.Add(new InputActionMapping("<XRController>{LeftHand}/isTracked", DeviceCategory.LeftController, poseState: PoseState.IsTracked));
            inputActions.Add(new InputActionMapping("<XRController>{LeftHand}/pointerPosition", DeviceCategory.LeftController, poseState: PoseState.Position));
            inputActions.Add(new InputActionMapping("<XRController>{LeftHand}/pointerRotation", DeviceCategory.LeftController, poseState: PoseState.Rotation));
            inputActions.Add(new InputActionMapping("<XRController>{LeftHand}/deviceVelocity", DeviceCategory.LeftController, poseState: PoseState.Velocity));
            inputActions.Add(new InputActionMapping("<XRController>{LeftHand}/deviceAngularVelocity", DeviceCategory.LeftController, poseState: PoseState.AngularVelocity));
            inputActions.Add(new InputActionMapping("<XRController>{LeftHand}/deviceAcceleration", DeviceCategory.LeftController, poseState: PoseState.Acceleration));
            inputActions.Add(new InputActionMapping("<XRController>{LeftHand}/deviceAngularAcceleration", DeviceCategory.LeftController, poseState: PoseState.AngularAcceleration));
            inputActions.Add(new InputActionMapping("<XRController>{LeftHand}/{grip}", DeviceCategory.LeftController, buttonEvent: ButtonEvent.GripValue));
            inputActions.Add(new InputActionMapping("<XRController>{LeftHand}/{gripButton}", DeviceCategory.LeftController, buttonEvent: ButtonEvent.GripPress));
            inputActions.Add(new InputActionMapping("<XRController>{LeftHand}/{trigger}", DeviceCategory.LeftController, buttonEvent: ButtonEvent.TriggerValue));
            inputActions.Add(new InputActionMapping("<XRController>{LeftHand}/triggerTouched", DeviceCategory.LeftController, buttonEvent: ButtonEvent.TriggerTouch));
            inputActions.Add(new InputActionMapping("<XRController>{LeftHand}/{triggerButton}", DeviceCategory.LeftController, buttonEvent: ButtonEvent.TriggerPress));
            inputActions.Add(new InputActionMapping("<XRController>{LeftHand}/{primary2DAxis}", DeviceCategory.LeftController, buttonEvent: ButtonEvent.Primary2DAxisValue));
            inputActions.Add(new InputActionMapping("<XRController>{LeftHand}/{primary2DAxisTouch}", DeviceCategory.LeftController, buttonEvent: ButtonEvent.Primary2DAxisTouch));
            inputActions.Add(new InputActionMapping("<XRController>{LeftHand}/{primary2DAxisClick}", DeviceCategory.LeftController, buttonEvent: ButtonEvent.Primary2DAxisPress));
            inputActions.Add(new InputActionMapping("<XRController>{LeftHand}/{secondary2DAxis}", DeviceCategory.LeftController, buttonEvent: ButtonEvent.Secondary2DAxisValue));
            inputActions.Add(new InputActionMapping("<XRController>{LeftHand}/{secondary2DAxisTouch}", DeviceCategory.LeftController, buttonEvent: ButtonEvent.Secondary2DAxisTouch));
            inputActions.Add(new InputActionMapping("<XRController>{LeftHand}/{secondary2DAxisClick}", DeviceCategory.LeftController, buttonEvent: ButtonEvent.Secondary2DAxisPress));
            inputActions.Add(new InputActionMapping("<XRController>{LeftHand}/{primaryButton}", DeviceCategory.LeftController, buttonEvent: ButtonEvent.PrimaryButton));
            inputActions.Add(new InputActionMapping("<XRController>{LeftHand}/{secondaryButton}", DeviceCategory.LeftController, buttonEvent: ButtonEvent.SecondaryButton));
            inputActions.Add(new InputActionMapping("<XRController>{LeftHand}/parkingTouched", DeviceCategory.LeftController, buttonEvent: ButtonEvent.ParkingTouch));
            inputActions.Add(new InputActionMapping("<XRController>{LeftHand}/menu", DeviceCategory.LeftController, buttonEvent: ButtonEvent.Menu));

            inputActions.Add(new InputActionMapping("<XRController>{RightHand}/isTracked", DeviceCategory.RightController, poseState: PoseState.IsTracked));
            inputActions.Add(new InputActionMapping("<XRController>{RightHand}/pointerPosition", DeviceCategory.RightController, poseState: PoseState.Position));
            inputActions.Add(new InputActionMapping("<XRController>{RightHand}/pointerRotation", DeviceCategory.RightController, poseState: PoseState.Rotation));
            inputActions.Add(new InputActionMapping("<XRController>{RightHand}/deviceVelocity", DeviceCategory.RightController, poseState: PoseState.Velocity));
            inputActions.Add(new InputActionMapping("<XRController>{RightHand}/deviceAngularVelocity", DeviceCategory.RightController, poseState: PoseState.AngularVelocity));
            inputActions.Add(new InputActionMapping("<XRController>{RightHand}/deviceAcceleration", DeviceCategory.RightController, poseState: PoseState.Acceleration));
            inputActions.Add(new InputActionMapping("<XRController>{RightHand}/deviceAngularAcceleration", DeviceCategory.RightController, poseState: PoseState.AngularAcceleration));
            inputActions.Add(new InputActionMapping("<XRController>{RightHand}/{grip}", DeviceCategory.RightController, buttonEvent: ButtonEvent.GripValue));
            inputActions.Add(new InputActionMapping("<XRController>{RightHand}/{gripButton}", DeviceCategory.RightController, buttonEvent: ButtonEvent.GripPress));
            inputActions.Add(new InputActionMapping("<XRController>{RightHand}/{trigger}", DeviceCategory.RightController, buttonEvent: ButtonEvent.TriggerValue));
            inputActions.Add(new InputActionMapping("<XRController>{RightHand}/triggerTouched", DeviceCategory.RightController, buttonEvent: ButtonEvent.TriggerTouch));
            inputActions.Add(new InputActionMapping("<XRController>{RightHand}/{triggerButton}", DeviceCategory.RightController, buttonEvent: ButtonEvent.TriggerPress));
            inputActions.Add(new InputActionMapping("<XRController>{RightHand}/{primary2DAxis}", DeviceCategory.RightController, buttonEvent: ButtonEvent.Primary2DAxisValue));
            inputActions.Add(new InputActionMapping("<XRController>{RightHand}/{primary2DAxisTouch}", DeviceCategory.RightController, buttonEvent: ButtonEvent.Primary2DAxisTouch));
            inputActions.Add(new InputActionMapping("<XRController>{RightHand}/{primary2DAxisClick}", DeviceCategory.RightController, buttonEvent: ButtonEvent.Primary2DAxisPress));
            inputActions.Add(new InputActionMapping("<XRController>{RightHand}/{secondary2DAxis}", DeviceCategory.RightController, buttonEvent: ButtonEvent.Secondary2DAxisValue));
            inputActions.Add(new InputActionMapping("<XRController>{RightHand}/{secondary2DAxisTouch}", DeviceCategory.RightController, buttonEvent: ButtonEvent.Secondary2DAxisTouch));
            inputActions.Add(new InputActionMapping("<XRController>{RightHand}/{secondary2DAxisClick}", DeviceCategory.RightController, buttonEvent: ButtonEvent.Secondary2DAxisPress));
            inputActions.Add(new InputActionMapping("<XRController>{RightHand}/{primaryButton}", DeviceCategory.RightController, buttonEvent: ButtonEvent.PrimaryButton));
            inputActions.Add(new InputActionMapping("<XRController>{RightHand}/{secondaryButton}", DeviceCategory.RightController, buttonEvent: ButtonEvent.SecondaryButton));
            inputActions.Add(new InputActionMapping("<XRController>{RightHand}/parkingTouched", DeviceCategory.RightController, buttonEvent: ButtonEvent.ParkingTouch));
            #endregion
            #region Hand
            inputActions.Add(new InputActionMapping("<ViveHandInteraction>{LeftHand}/selectValue", DeviceCategory.LeftHand, handEvent: HandEvent.PinchValue));
            inputActions.Add(new InputActionMapping("<ViveHandInteraction>{LeftHand}/pointerPose", DeviceCategory.LeftHand, handEvent: HandEvent.PinchPose));
            inputActions.Add(new InputActionMapping("<ViveHandInteraction>{LeftHand}/gripValue", DeviceCategory.LeftHand, handEvent: HandEvent.GraspValue));
            inputActions.Add(new InputActionMapping("<ViveHandInteraction>{LeftHand}/devicePose", DeviceCategory.LeftHand, handEvent: HandEvent.GraspPose));

            inputActions.Add(new InputActionMapping("<ViveHandInteraction>{RightHand}/selectValue", DeviceCategory.RightHand, handEvent: HandEvent.PinchValue));
            inputActions.Add(new InputActionMapping("<ViveHandInteraction>{RightHand}/pointerPose", DeviceCategory.RightHand, handEvent: HandEvent.PinchPose));
            inputActions.Add(new InputActionMapping("<ViveHandInteraction>{RightHand}/gripValue", DeviceCategory.RightHand, handEvent: HandEvent.GraspValue));
            inputActions.Add(new InputActionMapping("<ViveHandInteraction>{RightHand}/devicePose", DeviceCategory.RightHand, handEvent: HandEvent.GraspPose));
            #endregion
            #region Tracker
            inputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 0}/devicePose/isTracked", DeviceCategory.Tracker0, poseState: PoseState.IsTracked));
            inputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 0}/devicePosition", DeviceCategory.Tracker0, poseState: PoseState.Position));
            inputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 0}/deviceRotation", DeviceCategory.Tracker0, poseState: PoseState.Rotation));
            inputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 0}/devicePose/velocity", DeviceCategory.Tracker0, poseState: PoseState.Velocity));
            inputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 0}/devicePose/angularVelocity", DeviceCategory.Tracker0, poseState: PoseState.AngularVelocity));

            inputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 1}/devicePose/isTracked", DeviceCategory.Tracker1, poseState: PoseState.IsTracked));
            inputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 1}/devicePosition", DeviceCategory.Tracker1, poseState: PoseState.Position));
            inputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 1}/deviceRotation", DeviceCategory.Tracker1, poseState: PoseState.Rotation));
            inputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 1}/devicePose/velocity", DeviceCategory.Tracker1, poseState: PoseState.Velocity));
            inputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 1}/devicePose/angularVelocity", DeviceCategory.Tracker1, poseState: PoseState.AngularVelocity));

            inputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 2}/devicePose/isTracked", DeviceCategory.Tracker2, poseState: PoseState.IsTracked));
            inputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 2}/devicePosition", DeviceCategory.Tracker2, poseState: PoseState.Position));
            inputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 2}/deviceRotation", DeviceCategory.Tracker2, poseState: PoseState.Rotation));
            inputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 2}/devicePose/velocity", DeviceCategory.Tracker2, poseState: PoseState.Velocity));
            inputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 2}/devicePose/angularVelocity", DeviceCategory.Tracker2, poseState: PoseState.AngularVelocity));

            inputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 3}/devicePose/isTracked", DeviceCategory.Tracker3, poseState: PoseState.IsTracked));
            inputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 3}/devicePosition", DeviceCategory.Tracker3, poseState: PoseState.Position));
            inputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 3}/deviceRotation", DeviceCategory.Tracker3, poseState: PoseState.Rotation));
            inputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 3}/devicePose/velocity", DeviceCategory.Tracker3, poseState: PoseState.Velocity));
            inputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 3}/devicePose/angularVelocity", DeviceCategory.Tracker3, poseState: PoseState.AngularVelocity));

            inputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 4}/devicePose/isTracked", DeviceCategory.Tracker4, poseState: PoseState.IsTracked));
            inputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 4}/devicePosition", DeviceCategory.Tracker4, poseState: PoseState.Position));
            inputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 4}/deviceRotation", DeviceCategory.Tracker4, poseState: PoseState.Rotation));
            inputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 4}/devicePose/velocity", DeviceCategory.Tracker4, poseState: PoseState.Velocity));
            inputActions.Add(new InputActionMapping("<ViveXRTracker>{Ultimate Tracker 4}/devicePose/angularVelocity", DeviceCategory.Tracker4, poseState: PoseState.AngularVelocity));
            #endregion
        }

        private static void CheckHandUpdated()
        {
            if (Time.frameCount > leftHand.updateTime ||
                Time.frameCount > rightHand.updateTime)
            {
                ViveHandTracking viveHand = OpenXRSettings.Instance.GetFeature<ViveHandTracking>();
                if (viveHand)
                {
                    UpdateViveHand(true, viveHand);
                    UpdateViveHand(false, viveHand);
                }

#if UNITY_XR_HANDS
                HandTracking xrHand = OpenXRSettings.Instance.GetFeature<HandTracking>();
                if (xrHand)
                {
                    if (handSubsystem == null || !handSubsystem.running)
                    {
                        if (handSubsystem != null && !handSubsystem.running)
                        {
                            handSubsystem.updatedHands -= OnUpdatedHands;
                            handSubsystem = null;
                        }

                        var handSubsystems = new List<XRHandSubsystem>();
                        SubsystemManager.GetSubsystems(handSubsystems);
                        for (var i = 0; i < handSubsystems.Count; ++i)
                        {
                            var xrHnad = handSubsystems[i];
                            if (xrHnad.running)
                            {
                                handSubsystem = xrHnad;
                                break;
                            }
                        }
                        if (handSubsystem != null && handSubsystem.running)
                        {
                            handSubsystem.updatedHands += OnUpdatedHands;
                        }
                    }
                }
#endif
            }
        }

        private static void UpdateViveHand(bool isLeft, ViveHandTracking viveHand)
        {
            bool isUpdated = viveHand.GetJointLocations(isLeft, out XrHandJointLocationEXT[] viveJoints);
            JointData[] joints = new JointData[viveJoints.Length];
            for (int i = 0; i < joints.Length; i++)
            {
                bool isValid = isUpdated &&
                               viveJoints[i].locationFlags.HasFlag(XrSpaceLocationFlags.XR_SPACE_LOCATION_POSITION_TRACKED_BIT) &&
                               viveJoints[i].locationFlags.HasFlag(XrSpaceLocationFlags.XR_SPACE_LOCATION_ORIENTATION_TRACKED_BIT);
                Vector3 position = viveJoints[i].pose.position.ToUnityVector();
                Quaternion rotation = viveJoints[i].pose.orientation.ToUnityQuaternion();
                joints[i] = new JointData(isValid, position, rotation);
            }
            if (isLeft)
            {
                leftHand.Update(joints);
            }
            else
            {
                rightHand.Update(joints);
            }
        }

#if UNITY_XR_HANDS
		private static void OnUpdatedHands(XRHandSubsystem xrHnad, XRHandSubsystem.UpdateSuccessFlags flags, XRHandSubsystem.UpdateType type)
        {
            if (xrHnad != null && xrHnad.running)
            {
                UpdateXRHand(true, xrHnad, flags.HasFlag(XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints));
                UpdateXRHand(false, xrHnad, flags.HasFlag(XRHandSubsystem.UpdateSuccessFlags.RightHandJoints));
            }
        }

        private static void UpdateXRHand(bool isLeft, XRHandSubsystem xrHnad, bool isUpdated)
        {
            JointData[] joints = new JointData[(int)HandJointType.Count];
            for (int i = 0; i < joints.Length; i++)
            {
                XRHandJointID jointId = JointTypeToXRId(i);
                XRHandJoint joint = (isLeft ? xrHnad.leftHand : xrHnad.rightHand).GetJoint(jointId);
				bool isValid = isUpdated && joint.trackingState.HasFlag(XRHandJointTrackingState.Pose);
				joint.TryGetPose(out Pose pose);
				joints[i] = new JointData(isValid, pose.position, pose.rotation);
			}
			if (isLeft)
			{
				leftHand.Update(joints);
			}
			else
			{
				rightHand.Update(joints);
			}
		}

		private static XRHandJointID JointTypeToXRId(int id)
		{
			switch (id)
			{
				case 0:
					return XRHandJointID.Palm;
				case 1:
					return XRHandJointID.Wrist;
				default:
					return (XRHandJointID)(id + 1);
			}
		}
#endif

		private static DeviceCategory GetController(Handedness handedness)
        {
            DeviceCategory device = DeviceCategory.None;
            switch (handedness)
            {
                case Handedness.Left:
                    device = DeviceCategory.LeftController;
                    break;
                case Handedness.Right:
                    device = DeviceCategory.RightController;
                    break;
            }
            return device;
        }

        private static DeviceCategory GetHand(Handedness handedness)
        {
            DeviceCategory device = DeviceCategory.None;
            switch (handedness)
            {
                case Handedness.Left:
                    device = DeviceCategory.LeftHand;
                    break;
                case Handedness.Right:
                    device = DeviceCategory.RightHand;
                    break;
            }
            return device;
        }

        private static Handedness GetHandedness(DeviceCategory device)
        {
            Handedness handedness = Handedness.None;
            switch (device)
            {
                case DeviceCategory.LeftHand:
                    handedness = Handedness.Left;
                    break;
                case DeviceCategory.RightHand:
                    handedness = Handedness.Right;
                    break;
            }
            return handedness;
        }
    }
}
