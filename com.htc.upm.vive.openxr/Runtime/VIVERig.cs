// Copyright HTC Corporation All Rights Reserved.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using System.Text;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VIVE.OpenXR
{
	[DisallowMultipleComponent]
	public sealed class VIVERig : MonoBehaviour
	{
		private static VIVERig m_Instance;
		public static VIVERig Instance => m_Instance;

		#region Log
		const string LOG_TAG = "VIVE.OpenXR.VIVERig";
		StringBuilder m_sb = null;
		StringBuilder sb {
			get {
				if (m_sb == null) { m_sb = new StringBuilder(); }
				return m_sb;
			}
		}
		void DEBUG(StringBuilder msg) { Debug.LogFormat("{0} {1}", LOG_TAG, msg); }
		#endregion

		#region Inspector
		[SerializeField]
		private GameObject m_CameraOffset = null;
		public GameObject CameraOffset { get { return m_CameraOffset; } set { m_CameraOffset = value; } }

		private TrackingOriginModeFlags m_TrackingOriginEx = TrackingOriginModeFlags.Device;
		[SerializeField]
		private TrackingOriginModeFlags m_TrackingOrigin = TrackingOriginModeFlags.Device;
		public TrackingOriginModeFlags TrackingOrigin { get { return m_TrackingOrigin; } set { m_TrackingOrigin = value; } }

		private Vector3 cameraPosOffset = Vector3.zero;
		[SerializeField]
		private float m_CameraHeight = 1.5f;
		public float CameraHeight { get { return m_CameraHeight; } set { m_CameraHeight = value; } }

		[System.Obsolete("This variable is deprecated. Please use CameraHeight instead.")]
		[SerializeField]
		private float m_CameraYOffset = 1;
		[System.Obsolete("This variable is deprecated. Please use CameraHeight instead.")]
		public float CameraYOffset { get { return m_CameraYOffset; } set { m_CameraYOffset = value; } }

#if ENABLE_INPUT_SYSTEM
		[SerializeField]
		private InputActionAsset m_ActionAsset;
		public InputActionAsset actionAsset { get => m_ActionAsset; set => m_ActionAsset = value; }
#endif
		#endregion

		static List<XRInputSubsystem> s_InputSubsystems = new List<XRInputSubsystem>();
		private void OnEnable()
		{
			SubsystemManager.GetInstances(s_InputSubsystems);
			for (int i = 0; i < s_InputSubsystems.Count; i++)
			{
				s_InputSubsystems[i].trackingOriginUpdated += TrackingOriginUpdated;
			}

#if ENABLE_INPUT_SYSTEM
			if (m_ActionAsset != null)
			{
				m_ActionAsset.Enable();
			}
#endif
		}
		private void OnDisable()
		{
			SubsystemManager.GetInstances(s_InputSubsystems);
			for (int i = 0; i < s_InputSubsystems.Count; i++)
			{
				s_InputSubsystems[i].trackingOriginUpdated -= TrackingOriginUpdated;
			}
		}

		float m_LastRecenteredTime = 0.0f;
		private void TrackingOriginUpdated(XRInputSubsystem obj)
		{
			m_LastRecenteredTime = Time.time;
			sb.Clear().Append("TrackingOriginUpdated() m_LastRecenteredTime: ").Append(m_LastRecenteredTime); DEBUG(sb);
		}

		XRInputSubsystem m_InputSystem = null;
		void UpdateInputSystem()
		{
			SubsystemManager.GetInstances(s_InputSubsystems);
			if (s_InputSubsystems.Count > 0)
			{
				m_InputSystem = s_InputSubsystems[0];
			}
		}
		private void Awake()
		{
			if (m_Instance == null)
			{
				m_Instance = this;
			}
			else if (m_Instance != this)
			{
				Destroy(this);
			}

			UpdateInputSystem();
			if (m_InputSystem != null)
			{
				sb.Clear().Append("Awake() TrySetTrackingOriginMode ").Append(m_TrackingOrigin); DEBUG(sb);
				m_InputSystem.TrySetTrackingOriginMode(m_TrackingOrigin);

				TrackingOriginModeFlags mode = m_InputSystem.GetTrackingOriginMode();
				sb.Clear().Append("Awake() Tracking mode is set to ").Append(mode); DEBUG(sb);
			}
			else
			{
				sb.Clear().Append("Awake() no XRInputSubsystem."); DEBUG(sb);
			}
			m_TrackingOriginEx = m_TrackingOrigin;
		}

		private void Update()
		{
			UpdateInputSystem();
			if (m_InputSystem != null)
			{
				TrackingOriginModeFlags mode = m_InputSystem.GetTrackingOriginMode();
				if ((mode != m_TrackingOrigin || m_TrackingOriginEx != m_TrackingOrigin) && m_TrackingOrigin != TrackingOriginModeFlags.Unknown)
				{
					m_InputSystem.TrySetTrackingOriginMode(m_TrackingOrigin);

					mode = m_InputSystem.GetTrackingOriginMode();
					sb.Clear().Append("Update() Tracking mode is set to " + mode);
					m_TrackingOriginEx = m_TrackingOrigin;
				}
			}

			if (m_CameraOffset != null && m_TrackingOrigin == TrackingOriginModeFlags.Device)
			{
				cameraPosOffset.x = m_CameraOffset.transform.localPosition.x;
				cameraPosOffset.y = m_CameraHeight;
				cameraPosOffset.z = m_CameraOffset.transform.localPosition.z;

				m_CameraOffset.transform.localPosition = cameraPosOffset;
			}
		}
	}
}
