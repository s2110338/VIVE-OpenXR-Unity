// Copyright HTC Corporation All Rights Reserved.

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using System.Text;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VIVE.OpenXR.Samples.OpenXRInput
{
	[RequireComponent(typeof(Text))]
	public class HandInteractionTracking : MonoBehaviour
	{
		const string LOG_TAG = "VIVE.OpenXR.Sample.OpenXRInput.HandInteractionTracking ";
		StringBuilder m_sb = null;
		StringBuilder sb {
			get {
				if (m_sb == null) { m_sb = new StringBuilder(); }
				return m_sb;
			}
		}
		void DEBUG(StringBuilder msg) { Debug.Log(msg); }

		[SerializeField]
		private bool m_IsLeft = false;
		public bool IsLeft { get { return m_IsLeft; } set { m_IsLeft = value; } }

#if ENABLE_INPUT_SYSTEM
		[SerializeField]
		private InputActionReference m_GripPose = null;
		public InputActionReference GripPose { get { return m_GripPose; } set { m_GripPose = value; } }

		[SerializeField]
		private InputActionReference m_Strength = null;
		public InputActionReference Strength { get { return m_Strength; } set { m_Strength = value; } }

		[SerializeField]
		private InputActionReference m_AimPose = null;
		public InputActionReference AimPose { get { return m_AimPose; } set { m_AimPose = value; } }

		[SerializeField]
		private InputActionReference m_SelectValue = null;
		public InputActionReference SelectValue { get { return m_SelectValue; } set { m_SelectValue = value; } }
#endif

		private Text m_Text = null;
		private void Start()
		{
			m_Text = GetComponent<Text>();
		}

		void Update()
		{
			if (m_Text == null) { return; }

			m_Text.text = (m_IsLeft ? "Left Grip: " : "Right Grip: ");
#if ENABLE_INPUT_SYSTEM
			m_Text.text += "\nisTracked: ";
			{
				if (CommonHelper.GetPoseIsTracked(m_GripPose, out bool value, out string msg))
				{
					m_Text.text += value;
				}
				else
				{
					m_Text.text += msg;
				}
			}
			m_Text.text += "\ntrackingState: ";
			{
				if (CommonHelper.GetPoseTrackingState(m_GripPose, out InputTrackingState value, out string msg))
				{
					m_Text.text += value;
				}
				else
				{
					m_Text.text += msg;
				}
			}
			m_Text.text += "\nposition (";
			{
				if (CommonHelper.GetPosePosition(m_GripPose, out Vector3 value, out string msg))
				{
					m_Text.text += value.x.ToString() + ", " + value.y.ToString() + ", " + value.z.ToString();
				}
				else
				{
					m_Text.text += msg;
				}
			}
			m_Text.text += ")\nrotation (";
			{
				if (CommonHelper.GetPoseRotation(m_GripPose, out Quaternion value, out string msg))
				{
					m_Text.text += value.x.ToString() + ", " + value.y.ToString() + ", " + value.z.ToString() + ", " + value.w.ToString();
				}
				else
				{
					m_Text.text += msg;
				}
			}
			m_Text.text += ")\nstrength: ";
			{
				if (CommonHelper.GetAnalog(m_Strength, out float value, out string msg))
				{
					m_Text.text += value.ToString();
				}
				else
				{
					m_Text.text += msg;
				}
			}
			m_Text.text += "\n";
#endif
			m_Text.text += (m_IsLeft ? "Left Aim: " : "Right Aim: ");
#if ENABLE_INPUT_SYSTEM
			m_Text.text += "\nisTracked: ";
			{
				if (CommonHelper.GetPoseIsTracked(m_AimPose, out bool value, out string msg))
				{
					m_Text.text += value;
				}
				else
				{
					m_Text.text += msg;
				}
			}
			m_Text.text += "\ntrackingState: ";
			{
				if (CommonHelper.GetPoseTrackingState(m_AimPose, out InputTrackingState value, out string msg))
				{
					m_Text.text += value;
				}
				else
				{
					m_Text.text += msg;
				}
			}
			m_Text.text += "\nposition (";
			{
				if (CommonHelper.GetPosePosition(m_AimPose, out Vector3 value, out string msg))
				{
					m_Text.text += value.x.ToString() + ", " + value.y.ToString() + ", " + value.z.ToString();
				}
				else
				{
					m_Text.text += msg;
				}
			}
			m_Text.text += ")\nrotation (";
			{
				if (CommonHelper.GetPoseRotation(m_AimPose, out Quaternion value, out string msg))
				{
					m_Text.text += value.x.ToString() + ", " + value.y.ToString() + ", " + value.z.ToString() + ", " + value.w.ToString();
				}
				else
				{
					m_Text.text += msg;
				}
			}
			m_Text.text += ")\n";
			m_Text.text += "select: ";
			{
				if (CommonHelper.GetAnalog(m_SelectValue, out float value, out string msg))
				{
					m_Text.text += value;
				}
				else
				{
					m_Text.text += msg;
				}
			}
#endif
		}
	}
}
