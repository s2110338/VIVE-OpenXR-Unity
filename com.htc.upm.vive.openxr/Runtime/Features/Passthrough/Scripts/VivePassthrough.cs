// Copyright HTC Corporation All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR;
using AOT;

#if UNITY_EDITOR
using UnityEditor.XR.OpenXR.Features;
#endif

namespace VIVE.OpenXR.Passthrough
{
#if UNITY_EDITOR
	[OpenXRFeature(UiName = "VIVE XR Passthrough",
		Desc = "Enable this feature to use the VIVE OpenXR Passthrough feature.",
		Company = "HTC",
		DocumentationLink = "..\\Documentation",
		OpenxrExtensionStrings = kOpenxrExtensionStrings,
		Version = "1.0.0",
		BuildTargetGroups = new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone },
		FeatureId = featureId
	)]
#endif
	public class VivePassthrough : OpenXRFeature
	{
		#region LOG
		const string LOG_TAG = "VIVE.OpenXR.Passthrough.VivePassthrough";
		StringBuilder m_sb = null;
		StringBuilder sb {
			get {
				if (m_sb == null) { m_sb = new StringBuilder(); }
				return m_sb;
			}
		}
		static void DEBUG(string msg) { Debug.Log(LOG_TAG + " " + msg); }
		static void DEBUG(StringBuilder msg) { Debug.LogFormat("{0} {1}", LOG_TAG, msg); }
		static void WARNING(string msg) { Debug.LogWarning(LOG_TAG + " " + msg); }
		static void ERROR(string msg) { Debug.LogError(LOG_TAG + " " + msg); }
		#endregion

		/// <summary>
		/// The feature id string. This is used to give the feature a well known id for reference.
		/// </summary>
		public const string featureId = "vive.openxr.feature.passthrough";

		/// <summary>
		/// The extension string.
		/// </summary>
		public const string kOpenxrExtensionStrings = "XR_HTC_passthrough XR_HTC_passthrough_configuration";

#if UNITY_STANDALONE
		private static IntPtr xrGetInstanceProcAddr_prev;
		private static IntPtr XrEndFrame_prev;
		private static IntPtr XrWaitFrame_prev;
		private static List<IntPtr> layerListOrigin = new List<IntPtr>();
		private static List<IntPtr> layerListModified = new List<IntPtr>();
		private static IntPtr layersModified = Marshal.AllocHGlobal((int)(Marshal.SizeOf(typeof(IntPtr)) * 30)); //Preallocate a layer buffer with sufficient size and reuse it for each frame.
		protected override IntPtr HookGetInstanceProcAddr(IntPtr func)
		{
			UnityEngine.Debug.Log("EXT: registering our own xrGetInstanceProcAddr");
			xrGetInstanceProcAddr_prev = func;
			return Marshal.GetFunctionPointerForDelegate(Intercept_xrGetInstanceProcAddr);
		}
		[MonoPInvokeCallback(typeof(OpenXRHelper.xrGetInstanceProcAddrDelegate))]
		private static XrResult InterceptXrEndFrame_xrGetInstanceProcAddr(XrInstance instance, string name, out IntPtr function)
		{
			if (xrGetInstanceProcAddr_prev == null || xrGetInstanceProcAddr_prev == IntPtr.Zero)
			{
				UnityEngine.Debug.LogError("xrGetInstanceProcAddr_prev is null");
				function = IntPtr.Zero;
				return XrResult.XR_ERROR_VALIDATION_FAILURE;
			}

			// Get delegate of old xrGetInstanceProcAddr.
			var xrGetProc = Marshal.GetDelegateForFunctionPointer<OpenXRHelper.xrGetInstanceProcAddrDelegate>(xrGetInstanceProcAddr_prev);
			XrResult result = xrGetProc(instance, name, out function);
			if (name == "xrEndFrame")
			{
				XrEndFrame_prev = function;
				m_intercept_xrEndFrame = intercepted_xrEndFrame;
				function = Marshal.GetFunctionPointerForDelegate(m_intercept_xrEndFrame); ;
				UnityEngine.Debug.Log("Getting xrEndFrame func");
			}
			if (name == "xrWaitFrame")
			{
				XrWaitFrame_prev = function;
				m_intercept_xrWaitFrame = intercepted_xrWaitFrame;
				function = Marshal.GetFunctionPointerForDelegate(m_intercept_xrWaitFrame); ;
				UnityEngine.Debug.Log("Getting xrWaitFrame func");
			}
			return result;
		}

		[MonoPInvokeCallback(typeof(OpenXRHelper.xrEndFrameDelegate))]
		private static XrResult intercepted_xrEndFrame(XrSession session, ref XrFrameEndInfo frameEndInfo)
		{
			XrResult res;
			// Get delegate of prev xrEndFrame.
			var xrEndFrame = Marshal.GetDelegateForFunctionPointer<OpenXRHelper.xrEndFrameDelegate>(XrEndFrame_prev);

			layerListOrigin.Clear();
			uint layerCount = frameEndInfo.layerCount;
			IntPtr layers = frameEndInfo.layers;
			for (int i = 0; i < layerCount; i++)
			{
				IntPtr ptr = Marshal.ReadIntPtr(layers, i * Marshal.SizeOf(typeof(IntPtr)));
				XrCompositionLayerBaseHeader header = (XrCompositionLayerBaseHeader)Marshal.PtrToStructure(ptr, typeof(XrCompositionLayerBaseHeader));
				layerListOrigin.Add(ptr);
			}
			List<IntPtr> layerListNew;
			if (layerListModified.Count != 0)
			{
				layerListNew = new List<IntPtr>(layerListModified);
			}
			else
			{
				layerListNew = new List<IntPtr>(layerListOrigin);
			}
			for (int i = 0; i < layerListNew.Count; i++)
			{
				Marshal.WriteIntPtr(layersModified, i * Marshal.SizeOf(typeof(IntPtr)), layerListNew[i]);
			}
			frameEndInfo.layers = layersModified;
			frameEndInfo.layerCount = (uint)layerListNew.Count;

			res = xrEndFrame(session, ref frameEndInfo);
			return res;
		}
		private static XrFrameWaitInfo m_frameWaitInfo;
		private static XrFrameState m_frameState;
		[MonoPInvokeCallback(typeof(OpenXRHelper.xrWaitFrameDelegate))]
		private static int intercepted_xrWaitFrame(ulong session, ref XrFrameWaitInfo frameWaitInfo, ref XrFrameState frameState)
		{
			var xrWaitFrame = Marshal.GetDelegateForFunctionPointer<OpenXRHelper.xrWaitFrameDelegate>(XrWaitFrame_prev);
			int res = xrWaitFrame(session, ref frameWaitInfo, ref frameState);
			m_frameWaitInfo = frameWaitInfo;
			m_frameState = frameState;
			return res;
		}
		public void GetOriginEndFrameLayerList(out List<IntPtr> layers)
		{
			layers = new List<IntPtr>(layerListOrigin);
		}

		public void SubmitLayers(List<IntPtr> layers)
		{
			layerListModified = new List<IntPtr>(layers);
			//UnityEngine.Debug.Log("####Update submit end " + layerListModified.Count);
		}
		public XrFrameState GetFrameState()
		{
			return m_frameState;
		}
#endif
#if UNITY_ANDROID
		protected override IntPtr HookGetInstanceProcAddr(IntPtr func)
		{
			ViveInterceptors.Instance.AddRequiredFunction("xrPollEvent");
			return ViveInterceptors.Instance.HookGetInstanceProcAddr(func);
		}
#endif

		#region OpenXR Life Cycle
		private bool m_XrInstanceCreated = false;
		/// <summary>
		/// The XR instance is created or not.
		/// </summary>
		public bool XrInstanceCreated
		{
			get { return m_XrInstanceCreated; }
		}
		private XrInstance m_XrInstance = 0;
		protected override bool OnInstanceCreate(ulong xrInstance)
		{
			foreach (string kOpenxrExtensionString in kOpenxrExtensionStrings.Split(' '))
			{
				if (!OpenXRRuntime.IsExtensionEnabled(kOpenxrExtensionString))
				{
					WARNING("OnInstanceCreate() " + kOpenxrExtensionString + " is NOT enabled.");
				}
				else
				{
					DEBUG("OnInstanceCreate() " + kOpenxrExtensionString + " is enabled.");
				}
			}

			m_XrInstanceCreated = true;
			m_XrInstance = xrInstance;
			DEBUG("OnInstanceCreate() " + m_XrInstance);

			return GetXrFunctionDelegates(m_XrInstance);
		}
		protected override void OnInstanceDestroy(ulong xrInstance)
		{
			m_XrInstanceCreated = false;
			DEBUG("OnInstanceDestroy() " + m_XrInstance);
		}

		private XrSystemId m_XrSystemId = 0;
		protected override void OnSystemChange(ulong xrSystem)
		{
			m_XrSystemId = xrSystem;
			DEBUG("OnSystemChange() " + m_XrSystemId);
		}

		private bool m_XrSessionCreated = false;
		/// <summary>
		/// The XR session is created or not.
		/// </summary>
		public bool XrSessionCreated
		{
			get { return m_XrSessionCreated; }
		}
		private XrSession m_XrSession = 0;
		protected override void OnSessionCreate(ulong xrSession)
		{
			m_XrSession = xrSession;
			m_XrSessionCreated = true;
			DEBUG("OnSessionCreate() " + m_XrSession);
			CheckConfigurationSupport();
		}

		private XrSpace m_WorldLockSpaceOriginOnHead = 0, m_WorldLockSpaceOriginOnFloor = 0, m_HeadLockSpace = 0;
		private XrSpace WorldLockSpaceOriginOnHead
		{
			get { return m_WorldLockSpaceOriginOnHead; }
		}
		private XrSpace WorldLockSpaceOriginOnFloor
		{
			get { return m_WorldLockSpaceOriginOnFloor; }
		}
		private XrSpace HeadLockSpace
		{
			get { return m_HeadLockSpace; }
		}

		private bool m_XrSessionEnding = false;
		/// <summary>
		/// The XR session is ending or not.
		/// </summary>
		public bool XrSessionEnding
		{
			get { return m_XrSessionEnding; }
		}
		protected override void OnSessionBegin(ulong xrSession)
		{
			m_XrSessionEnding = false;
			DEBUG("OnSessionBegin() " + m_XrSession);

			// Enumerate supported reference space types and create the XrSpace.
			XrReferenceSpaceType[] spaces = new XrReferenceSpaceType[Enum.GetNames(typeof(XrReferenceSpaceType)).Count()];
			UInt32 spaceCountOutput;
			if (EnumerateReferenceSpaces(
				spaceCapacityInput: 0,
				spaceCountOutput: out spaceCountOutput,
				spaces: out spaces[0]) == XrResult.XR_SUCCESS)
			{
				//DEBUG("spaceCountOutput: " + spaceCountOutput);

				Array.Resize(ref spaces, (int)spaceCountOutput);
				if (EnumerateReferenceSpaces(
					spaceCapacityInput: spaceCountOutput,
					spaceCountOutput: out spaceCountOutput,
					spaces: out spaces[0]) == XrResult.XR_SUCCESS)
				{
					if (spaces.Contains(XrReferenceSpaceType.XR_REFERENCE_SPACE_TYPE_LOCAL))
					{
						XrReferenceSpaceCreateInfo referenceSpaceCreateInfoWorldLock;
						referenceSpaceCreateInfoWorldLock.type = XrStructureType.XR_TYPE_REFERENCE_SPACE_CREATE_INFO;
						referenceSpaceCreateInfoWorldLock.next = IntPtr.Zero;
						referenceSpaceCreateInfoWorldLock.referenceSpaceType = XrReferenceSpaceType.XR_REFERENCE_SPACE_TYPE_LOCAL;
						referenceSpaceCreateInfoWorldLock.poseInReferenceSpace.orientation = new XrQuaternionf(0, 0, 0, 1);
						referenceSpaceCreateInfoWorldLock.poseInReferenceSpace.position = new XrVector3f(0, 0, 0);

						if (CreateReferenceSpace(
						createInfo: ref referenceSpaceCreateInfoWorldLock,
						space: out m_WorldLockSpaceOriginOnHead) == XrResult.XR_SUCCESS)
						{
							//DEBUG("CreateReferenceSpace: " + m_WorldLockSpaceOriginOnHead);
						}
						else
						{
							ERROR("CreateReferenceSpace for world lock layers on head failed.");
						}
					}
					else
					{
						ERROR("CreateReferenceSpace no space type for world lock on head layers.");
					}

					if (spaces.Contains(XrReferenceSpaceType.XR_REFERENCE_SPACE_TYPE_STAGE))
					{
						XrReferenceSpaceCreateInfo referenceSpaceCreateInfoWorldLock;
						referenceSpaceCreateInfoWorldLock.type = XrStructureType.XR_TYPE_REFERENCE_SPACE_CREATE_INFO;
						referenceSpaceCreateInfoWorldLock.next = IntPtr.Zero;
						referenceSpaceCreateInfoWorldLock.referenceSpaceType = XrReferenceSpaceType.XR_REFERENCE_SPACE_TYPE_STAGE;
						referenceSpaceCreateInfoWorldLock.poseInReferenceSpace.orientation = new XrQuaternionf(0, 0, 0, 1);
						referenceSpaceCreateInfoWorldLock.poseInReferenceSpace.position = new XrVector3f(0, 0, 0);

						if (CreateReferenceSpace(
						createInfo: ref referenceSpaceCreateInfoWorldLock,
						space: out m_WorldLockSpaceOriginOnFloor) == XrResult.XR_SUCCESS)
						{
							//DEBUG("CreateReferenceSpace: " + m_WorldLockSpaceOriginOnFloor);
						}
						else
						{
							ERROR("CreateReferenceSpace for world lock layers on floor failed.");
						}
					}
					else
					{
						ERROR("CreateReferenceSpace no space type for world lock on floor layers.");
					}

					if (spaces.Contains(XrReferenceSpaceType.XR_REFERENCE_SPACE_TYPE_VIEW))
					{
						XrReferenceSpaceCreateInfo referenceSpaceCreateInfoHeadLock;
						referenceSpaceCreateInfoHeadLock.type = XrStructureType.XR_TYPE_REFERENCE_SPACE_CREATE_INFO;
						referenceSpaceCreateInfoHeadLock.next = IntPtr.Zero;
						referenceSpaceCreateInfoHeadLock.referenceSpaceType = XrReferenceSpaceType.XR_REFERENCE_SPACE_TYPE_VIEW;
						referenceSpaceCreateInfoHeadLock.poseInReferenceSpace.orientation = new XrQuaternionf(0, 0, 0, 1);
						referenceSpaceCreateInfoHeadLock.poseInReferenceSpace.position = new XrVector3f(0, 0, 0);

						if (CreateReferenceSpace(
						createInfo: ref referenceSpaceCreateInfoHeadLock,
						space: out m_HeadLockSpace) == XrResult.XR_SUCCESS)
						{
							//DEBUG("CreateReferenceSpace: " + m_HeadLockSpace);
						}
						else
						{
							ERROR("CreateReferenceSpace for head lock layers failed.");
						}
					}
					else
					{
						ERROR("CreateReferenceSpace no space type for head lock layers.");
					}
				}
				else
				{
					ERROR("EnumerateReferenceSpaces(" + spaceCountOutput + ") failed.");
				}
			}
			else
			{
				ERROR("EnumerateReferenceSpaces(0) failed.");
			}
		}
		protected override void OnSessionEnd(ulong xrSession)
		{
			m_XrSessionEnding = true;
			DEBUG("OnSessionEnd() " + m_XrSession);
		}

		/// <summary>
		/// The delegate of Passthrough Session Destroy.
		/// </summary>
		public delegate void OnPassthroughSessionDestroyDelegate(XrPassthroughHTC passthroughID);
		private Dictionary<XrPassthroughHTC, OnPassthroughSessionDestroyDelegate> OnPassthroughSessionDestroyHandlerDictionary = new Dictionary<XrPassthroughHTC, OnPassthroughSessionDestroyDelegate>();
		protected override void OnSessionDestroy(ulong xrSession)
		{
			if (!m_XrSessionCreated || m_XrSession != xrSession) { return; }

			sb.Clear().Append("OnSessionDestroy() " + xrSession); DEBUG(sb);
			m_XrSessionCreated = false;
			m_XrSession = 0;

			List<XrPassthroughHTC> passthroughs = PassthroughList;
			for (int i = 0; i < passthroughs.Count; i++)
			{
				XrPassthroughHTC currentpassthrough = passthroughs[i];
				DestroyPassthroughHTC(currentpassthrough);
				if (OnPassthroughSessionDestroyHandlerDictionary.ContainsKey(currentpassthrough) && OnPassthroughSessionDestroyHandlerDictionary[currentpassthrough] != null)
				{
					sb.Clear().Append("OnSessionDestroy() Call back ").Append(passthroughs[i]); DEBUG(sb);
					OnPassthroughSessionDestroyHandlerDictionary[passthroughs[i]].Invoke(passthroughs[i]);
				}
			}

			if (m_HeadLockSpace != 0)
			{
				DestroySpace(m_HeadLockSpace);
				m_HeadLockSpace = 0;
			}
			if (m_WorldLockSpaceOriginOnFloor != 0)
			{
				DestroySpace(m_WorldLockSpaceOriginOnFloor);
				m_WorldLockSpaceOriginOnFloor = 0;
			}
			if (m_WorldLockSpaceOriginOnHead != 0)
			{
				DestroySpace(m_WorldLockSpaceOriginOnHead);
				m_WorldLockSpaceOriginOnHead = 0;
			}
		}

		/// <summary>
		/// The current XR session state.
		/// </summary>
		public XrSessionState XrSessionCurrentState
		{
			get { return m_XrSessionNewState; }
		}
		private XrSessionState m_XrSessionNewState = XrSessionState.XR_SESSION_STATE_UNKNOWN;
		private XrSessionState m_XrSessionOldState = XrSessionState.XR_SESSION_STATE_UNKNOWN;
		protected override void OnSessionStateChange(int oldState, int newState)
		{
			DEBUG("OnSessionStateChange() oldState: " + oldState + " newState:" + newState);

			if (Enum.IsDefined(typeof(XrSessionState), oldState))
			{
				m_XrSessionOldState = (XrSessionState)oldState;
			}
			else
			{
				DEBUG("OnSessionStateChange() oldState undefined");
			}

			if (Enum.IsDefined(typeof(XrSessionState), newState))
			{
				m_XrSessionNewState = (XrSessionState)newState;
			}
			else
			{
				DEBUG("OnSessionStateChange() newState undefined");
			}

		}
		#endregion

		#region OpenXR function delegates
		/// xrGetInstanceProcAddr
		OpenXRHelper.xrGetInstanceProcAddrDelegate XrGetInstanceProcAddr;

		/// xrGetSystemProperties
		OpenXRHelper.xrGetSystemPropertiesDelegate xrGetSystemProperties;
		/// <summary>
		/// Helper function to get this feature' properties.
		/// See <see href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#xrGetSystemProperties">xrGetSystemProperties</see>
		/// </summary>
		public XrResult GetSystemProperties(ref XrSystemProperties properties)
		{
			if (m_XrInstanceCreated)
			{
				return xrGetSystemProperties(m_XrInstance, m_XrSystemId, ref properties);
			}

			return XrResult.XR_ERROR_INSTANCE_LOST;
		}

#if UNITY_STANDALONE
		OpenXRHelper.xrGetInstanceProcAddrDelegate Intercept_xrGetInstanceProcAddr =
			new OpenXRHelper.xrGetInstanceProcAddrDelegate(InterceptXrEndFrame_xrGetInstanceProcAddr);

		private static OpenXRHelper.xrEndFrameDelegate m_intercept_xrEndFrame;
		private static OpenXRHelper.xrWaitFrameDelegate m_intercept_xrWaitFrame;

		VivePassthroughHelper.xrCreatePassthroughHTCDelegate xrCreatePassthroughHTC;
		VivePassthroughHelper.xrDestroyPassthroughHTCDelegate xrDestroyPassthroughHTC;
#endif

		/// xrEnumerateReferenceSpaces
		OpenXRHelper.xrEnumerateReferenceSpacesDelegate xrEnumerateReferenceSpaces;
		private XrResult EnumerateReferenceSpaces(UInt32 spaceCapacityInput, out UInt32 spaceCountOutput, out XrReferenceSpaceType spaces)
		{
			if (!m_XrSessionCreated)
			{
				spaceCountOutput = 0;
				spaces = XrReferenceSpaceType.XR_REFERENCE_SPACE_TYPE_UNBOUNDED_MSFT;
				return XrResult.XR_ERROR_SESSION_NOT_RUNNING;
			}

			return xrEnumerateReferenceSpaces(m_XrSession, spaceCapacityInput, out spaceCountOutput, out spaces);
		}

		/// xrCreateReferenceSpace
		OpenXRHelper.xrCreateReferenceSpaceDelegate xrCreateReferenceSpace;
		/// <summary>
		/// Creates a reference space
		/// See <see href="https://registry.khronos.org/OpenXR/specs/1.0/man/html/xrCreateReferenceSpace.html">xrCreateReferenceSpace</see>
		/// </summary>
		private XrResult CreateReferenceSpace(ref XrReferenceSpaceCreateInfo createInfo, out XrSpace space)
		{
			if (!m_XrSessionCreated)
			{
				space = 0;
				return XrResult.XR_ERROR_SESSION_NOT_RUNNING;
			}

			return xrCreateReferenceSpace(m_XrSession, ref createInfo, out space);
		}

		/// xrDestroySpace
		OpenXRHelper.xrDestroySpaceDelegate xrDestroySpace;
		private XrResult DestroySpace(XrSpace space)
		{
			if (space != 0)
			{
				return xrDestroySpace(space);
			}
			return XrResult.XR_ERROR_REFERENCE_SPACE_UNSUPPORTED;
		}


		VivePassthroughHelper.xrEnumeratePassthroughImageRatesHTCDelegate xrEnumeratePassthroughImageRatesHTC;
		VivePassthroughHelper.xrGetPassthroughConfigurationHTCDelegate xrGetPassthroughConfigurationHTC;
		VivePassthroughHelper.xrSetPassthroughConfigurationHTCDelegate xrSetPassthroughConfigurationHTC;

		private bool GetXrFunctionDelegates(XrInstance xrInstance)
		{
			/// xrGetInstanceProcAddr
			if (xrGetInstanceProcAddr != null && xrGetInstanceProcAddr != IntPtr.Zero)
			{
				DEBUG("Get function pointer of xrGetInstanceProcAddr.");
				XrGetInstanceProcAddr = Marshal.GetDelegateForFunctionPointer(
					xrGetInstanceProcAddr,
					typeof(OpenXRHelper.xrGetInstanceProcAddrDelegate)) as OpenXRHelper.xrGetInstanceProcAddrDelegate;
			}
			else
			{
				ERROR("xrGetInstanceProcAddr");
				return false;
			}

			IntPtr funcPtr = IntPtr.Zero;
			/// xrGetSystemProperties
			if (XrGetInstanceProcAddr(xrInstance, "xrGetSystemProperties", out funcPtr) == XrResult.XR_SUCCESS)
			{
				if (funcPtr != IntPtr.Zero)
				{
					DEBUG("Get function pointer of xrGetSystemProperties.");
					xrGetSystemProperties = Marshal.GetDelegateForFunctionPointer(
						funcPtr,
						typeof(OpenXRHelper.xrGetSystemPropertiesDelegate)) as OpenXRHelper.xrGetSystemPropertiesDelegate;
				}
			}
			else
			{
				ERROR("xrGetSystemProperties");
				return false;
			}
			/// xrEnumerateReferenceSpaces
			if (XrGetInstanceProcAddr(xrInstance, "xrEnumerateReferenceSpaces", out funcPtr) == XrResult.XR_SUCCESS)
			{
				if (funcPtr != IntPtr.Zero)
				{
					DEBUG("Get function pointer of xrEnumerateReferenceSpaces.");
					xrEnumerateReferenceSpaces = Marshal.GetDelegateForFunctionPointer(
						funcPtr,
						typeof(OpenXRHelper.xrEnumerateReferenceSpacesDelegate)) as OpenXRHelper.xrEnumerateReferenceSpacesDelegate;
				}
			}
			else
			{
				ERROR("xrEnumerateReferenceSpaces");
				return false;
			}
			/// xrCreateReferenceSpace
			if (XrGetInstanceProcAddr(xrInstance, "xrCreateReferenceSpace", out funcPtr) == XrResult.XR_SUCCESS)
			{
				if (funcPtr != IntPtr.Zero)
				{
					DEBUG("Get function pointer of xrCreateReferenceSpace.");
					xrCreateReferenceSpace = Marshal.GetDelegateForFunctionPointer(
						funcPtr,
						typeof(OpenXRHelper.xrCreateReferenceSpaceDelegate)) as OpenXRHelper.xrCreateReferenceSpaceDelegate;
				}
			}
			else
			{
				ERROR("xrCreateReferenceSpace");
				return false;
			}
			/// xrDestroySpace
			if (XrGetInstanceProcAddr(xrInstance, "xrDestroySpace", out funcPtr) == XrResult.XR_SUCCESS)
			{
				if (funcPtr != IntPtr.Zero)
				{
					DEBUG("Get function pointer of xrDestroySpace.");
					xrDestroySpace = Marshal.GetDelegateForFunctionPointer(
						funcPtr,
						typeof(OpenXRHelper.xrDestroySpaceDelegate)) as OpenXRHelper.xrDestroySpaceDelegate;
				}
			}
			else
			{
				ERROR("xrDestroySpace");
				return false;
			}
#if UNITY_ANDROID
			/// xrEnumeratePassthroughImageRatesHTC
			if (XrGetInstanceProcAddr(xrInstance, "xrEnumeratePassthroughImageRatesHTC", out funcPtr) == XrResult.XR_SUCCESS)
			{
				if (funcPtr != IntPtr.Zero)
				{
					DEBUG("Get function pointer of xrEnumeratePassthroughImageRatesHTC.");
					xrEnumeratePassthroughImageRatesHTC = Marshal.GetDelegateForFunctionPointer(
						funcPtr,
						typeof(VivePassthroughHelper.xrEnumeratePassthroughImageRatesHTCDelegate)) as VivePassthroughHelper.xrEnumeratePassthroughImageRatesHTCDelegate;
				}
			}
			else
			{
				ERROR("xrEnumeratePassthroughImageRatesHTC");
				//return false;
			}

			/// xrGetPassthroughConfigurationHTC
			if (XrGetInstanceProcAddr(xrInstance, "xrGetPassthroughConfigurationHTC", out funcPtr) == XrResult.XR_SUCCESS)
			{
				if (funcPtr != IntPtr.Zero)
				{
					DEBUG("Get function pointer of xrGetPassthroughConfigurationHTC.");
					xrGetPassthroughConfigurationHTC = Marshal.GetDelegateForFunctionPointer(
						funcPtr,
						typeof(VivePassthroughHelper.xrGetPassthroughConfigurationHTCDelegate)) as VivePassthroughHelper.xrGetPassthroughConfigurationHTCDelegate;
				}
			}
			else
			{
				ERROR("xrGetPassthroughConfigurationHTC");
				//return false;
			}

			/// xrSetPassthroughConfigurationHTC
			if (XrGetInstanceProcAddr(xrInstance, "xrSetPassthroughConfigurationHTC", out funcPtr) == XrResult.XR_SUCCESS)
			{
				if (funcPtr != IntPtr.Zero)
				{
					DEBUG("Get function pointer of xrSetPassthroughConfigurationHTC.");
					xrSetPassthroughConfigurationHTC = Marshal.GetDelegateForFunctionPointer(
						funcPtr,
						typeof(VivePassthroughHelper.xrSetPassthroughConfigurationHTCDelegate)) as VivePassthroughHelper.xrSetPassthroughConfigurationHTCDelegate;
				}
			}
			else
			{
				ERROR("xrSetPassthroughConfigurationHTC");
				//return false;
			}
#endif
#if UNITY_STANDALONE
			/// xrCreatePassthroughHTC
			if (XrGetInstanceProcAddr(xrInstance, "xrCreatePassthroughHTC", out funcPtr) == XrResult.XR_SUCCESS)
			{
				if (funcPtr != IntPtr.Zero)
				{
					DEBUG("Get function pointer of xrCreatePassthroughHTC.");
					xrCreatePassthroughHTC = Marshal.GetDelegateForFunctionPointer(
						funcPtr,
						typeof(VivePassthroughHelper.xrCreatePassthroughHTCDelegate)) as VivePassthroughHelper.xrCreatePassthroughHTCDelegate;
				}
			}
			else
			{
				ERROR("xrCreatePassthroughHTC");
				return false;
			}
			/// xrCreatePassthroughHTC
			if (XrGetInstanceProcAddr(xrInstance, "xrDestroyPassthroughHTC", out funcPtr) == XrResult.XR_SUCCESS)
			{
				if (funcPtr != IntPtr.Zero)
				{
					DEBUG("Get function pointer of xrDestroyPassthroughHTC.");
					xrDestroyPassthroughHTC = Marshal.GetDelegateForFunctionPointer(
						funcPtr,
						typeof(VivePassthroughHelper.xrDestroyPassthroughHTCDelegate)) as VivePassthroughHelper.xrDestroyPassthroughHTCDelegate;
				}
			}
			else
			{
				ERROR("xrDestroyPassthroughHTC");
				return false;
			}
#endif

#if UNITY_ANDROID
			if (GetFuncAddrs(xrInstance, xrGetInstanceProcAddr) == XrResult.XR_SUCCESS)
			{
				DEBUG("Get function pointers in native.");
			}
			else
			{
				ERROR("GetFuncAddrs");
				return false;
			}
#endif
			return true;
		}
#endregion

#if UNITY_ANDROID
#region Android Hook - Public
		private const string ExtLib = "viveopenxr";
		[DllImport(ExtLib, EntryPoint = "htcpassthrough_CreatePassthrough")]
		private static extern int ViveCreatePassthrough(XrSession session, CompositionLayer.LayerType layerType, PassthroughLayerForm layerForm, uint compositionDepth = 0);
		[DllImport(ExtLib, EntryPoint = "htcpassthrough_DestroyPassthrough")]
		private static extern bool ViveDestroyPassthrough(int passthroughID);

		[DllImport(ExtLib, EntryPoint = "htcpassthrough_SetAlpha")]
		private static extern bool ViveSetAlpha(int passthroughID, float alpha);
		/// <summary>
		/// Set Passthough Alpha.
		/// </summary>
		public bool SetAlpha(XrPassthroughHTC passthrough, float alpha)
		{
			if (!m_XrInstanceCreated)
			{
				ERROR("SetAlpha: " + kOpenxrExtensionStrings + " is NOT enabled.");
				return false;
			}
			if (passthrough == 0) { ERROR("SetAlpha() Invalid passthrough."); return false; }

			int passthroughID = (int)(passthrough & 0x00007FFF);
			sb.Clear().Append("SetAlpha() passthrough: ").Append(passthroughID).Append(", alpha: ").Append(alpha); DEBUG(sb);
			return ViveSetAlpha(passthroughID, alpha);
		}

		[DllImport(ExtLib, EntryPoint = "htcpassthrough_SetLayerType")]
		private static extern bool ViveSetLayerType(int passthroughID, CompositionLayer.LayerType layerType, uint compositionDepth = 0);
		/// <summary>
		/// Set Passthough Layer Type.
		/// </summary>
		public bool SetLayerType(XrPassthroughHTC passthrough, CompositionLayer.LayerType layerType, uint compositionDepth = 0)
		{
			if (!m_XrInstanceCreated)
			{
				ERROR("SetLayerType: " + kOpenxrExtensionStrings + " is NOT enabled.");
				return false;
			}
			if (passthrough == 0) { ERROR("SetLayerType() Invalid passthrough."); return false; }

			int passthroughID = (int)(passthrough & 0x00007FFF);
			sb.Clear().Append("SetAlpha() passthrough: ").Append(passthroughID).Append(", layerType: ").Append(layerType).Append(", compositionDepth: ").Append(compositionDepth); DEBUG(sb);
			return ViveSetLayerType(passthroughID, layerType, compositionDepth);
		}

		[DllImport(ExtLib, EntryPoint = "htcpassthrough_SetMesh")]
		private static extern bool ViveSetMesh(int passthroughID, uint vertexCount, [In, Out] XrVector3f[] vertexBuffer, uint indexCount, [In, Out] uint[] indexBuffer);
		/// <summary>
		/// Set Passthough Mesh.
		/// </summary>
		public bool SetMesh(XrPassthroughHTC passthrough, uint vertexCount, [In, Out] XrVector3f[] vertexBuffer, uint indexCount, [In, Out] uint[] indexBuffer)
		{
			if (!m_XrInstanceCreated)
			{
				ERROR("SetMesh: " + kOpenxrExtensionStrings + " is NOT enabled.");
				return false;
			}
			if (passthrough == 0) { ERROR("SetMesh() Invalid passthrough."); return false; }

			int passthroughID = (int)(passthrough & 0x00007FFF);
			sb.Clear().Append("SetMesh() passthrough: ").Append(passthroughID).Append(", vertexCount: ").Append(vertexCount).Append(", indexCount: ").Append(indexCount); DEBUG(sb);
			return ViveSetMesh(passthroughID, vertexCount, vertexBuffer, indexCount, indexBuffer);
		}

		[DllImport(ExtLib, EntryPoint = "htcpassthrough_SetMeshTransform")]
		private static extern bool ViveSetMeshTransform(int passthroughID, XrSpace meshSpace, XrPosef meshPose, XrVector3f meshScale);
		/// <summary>
		/// Set Passthough Mesh Transform.
		/// </summary>
		public bool SetMeshTransform(XrPassthroughHTC passthrough, XrSpace meshSpace, XrPosef meshPose, XrVector3f meshScale)
		{
			if (!m_XrInstanceCreated)
			{
				ERROR("SetMeshTransform: " + kOpenxrExtensionStrings + " is NOT enabled.");
				return false;
			}
			if (passthrough == 0) { ERROR("SetMeshTransform() Invalid passthrough."); return false; }

			int passthroughID = (int)(passthrough & 0x00007FFF);
			sb.Clear().Append("SetMeshTransform() passthrough: ").Append(passthroughID).Append(", meshSpace: ").Append(meshSpace); DEBUG(sb);
			return ViveSetMeshTransform(passthroughID, meshSpace, meshPose, meshScale);
		}

		[DllImport(ExtLib, EntryPoint = "htcpassthrough_SetMeshTransformSpace")]
		private static extern bool ViveSetMeshTransformSpace(int passthroughID, XrSpace meshSpace);
		/// <summary>
		/// Set Passthough Mesh Transform Space.
		/// </summary>
		public bool SetMeshTransformSpace(XrPassthroughHTC passthrough, XrSpace meshSpace)
		{
			if (!m_XrInstanceCreated)
			{
				ERROR("SetMeshTransformSpace: " + kOpenxrExtensionStrings + " is NOT enabled.");
				return false;
			}
			if (passthrough == 0) { ERROR("SetMeshTransformSpace() Invalid passthrough."); return false; }

			int passthroughID = (int)(passthrough & 0x00007FFF);
			sb.Clear().Append("SetMeshTransformSpace() passthrough: ").Append(passthroughID).Append(", meshSpace: ").Append(meshSpace); DEBUG(sb);
			return ViveSetMeshTransformSpace(passthroughID, meshSpace);
		}

		[DllImport(ExtLib, EntryPoint = "htcpassthrough_SetMeshTransformPosition")]
		private static extern bool ViveSetMeshTransformPosition(int passthroughID, XrVector3f meshPosition);
		/// <summary>
		/// Set Passthough Mesh Transform Position.
		/// </summary>
		public bool SetMeshTransformPosition(XrPassthroughHTC passthrough, XrVector3f meshPosition)
		{
			if (!m_XrInstanceCreated)
			{
				ERROR("SetMeshTransformPosition: " + kOpenxrExtensionStrings + " is NOT enabled.");
				return false;
			}
			if (passthrough == 0) { ERROR("SetMeshTransformPosition() Invalid passthrough."); return false; }

			int passthroughID = (int)(passthrough & 0x00007FFF);
			sb.Clear().Append("SetMeshTransformPosition() passthrough: ").Append(passthroughID); DEBUG(sb);
			return ViveSetMeshTransformPosition(passthroughID, meshPosition);
		}

		[DllImport(ExtLib, EntryPoint = "htcpassthrough_SetMeshTransformOrientation")]
		private static extern bool ViveSetMeshTransformOrientation(int passthroughID, XrQuaternionf meshOrientation);
		/// <summary>
		/// Set Passthough Mesh Transform orientation.
		/// </summary>
		public bool SetMeshTransformOrientation(XrPassthroughHTC passthrough, XrQuaternionf meshOrientation)
		{
			if (!m_XrInstanceCreated)
			{
				ERROR("SetMeshTransformOrientation: " + kOpenxrExtensionStrings + " is NOT enabled.");
				return false;
			}
			if (passthrough == 0) { ERROR("SetMeshTransformOrientation() Invalid passthrough."); return false; }

			int passthroughID = (int)(passthrough & 0x00007FFF);
			sb.Clear().Append("SetMeshTransformOrientation() passthrough: ").Append(passthroughID); DEBUG(sb);
			return ViveSetMeshTransformOrientation(passthroughID, meshOrientation);
		}

		[DllImport(ExtLib, EntryPoint = "htcpassthrough_SetMeshTransformScale")]
		private static extern bool ViveSetMeshTransformScale(int passthroughID, XrVector3f meshScale);
		/// <summary>
		/// Set Passthough Mesh Transform scale.
		/// </summary>
		public bool SetMeshTransformScale(XrPassthroughHTC passthrough, XrVector3f meshScale)
		{
			if (!m_XrInstanceCreated)
			{
				ERROR("SetMeshTransformScale: " + kOpenxrExtensionStrings + " is NOT enabled.");
				return false;
			}
			if (passthrough == 0) { ERROR("SetMeshTransformScale() Invalid passthrough."); return false; }

			int passthroughID = (int)(passthrough & 0x00007FFF);
			sb.Clear().Append("SetMeshTransformScale() passthrough: ").Append(passthroughID); DEBUG(sb);
			return ViveSetMeshTransformScale(passthroughID, meshScale);
		}
#endregion

#region Android Hook - Private
		[DllImport(ExtLib, EntryPoint = "htcpassthrough_GetFuncAddrs")]
		private static extern XrResult ViveGetFuncAddrs(XrInstance xrInstance, IntPtr xrGetInstanceProcAddrFuncPtr);
		private XrResult GetFuncAddrs(XrInstance xrInstance, IntPtr xrGetInstanceProcAddrFuncPtr)
		{
			if (!m_XrInstanceCreated)
			{
				ERROR("ViveGetFuncAddrs: " + kOpenxrExtensionStrings + " is NOT enabled.");
				return XrResult.XR_ERROR_FEATURE_UNSUPPORTED;
			}

			return ViveGetFuncAddrs(xrInstance, xrGetInstanceProcAddrFuncPtr);
		}
#endregion
#endif

		private List<XrPassthroughHTC> passthroughList = new List<XrPassthroughHTC>();
		public List<XrPassthroughHTC> PassthroughList {
			get {
				if (passthroughList == null) { passthroughList = new List<XrPassthroughHTC>(); }
				return passthroughList;
			}
		}

#region Public API
		public XrResult CreatePassthroughHTC(XrPassthroughCreateInfoHTC createInfo, out XrPassthroughHTC passthrough, CompositionLayer.LayerType layerType, uint compositionDepth, OnPassthroughSessionDestroyDelegate onDestroy)
		{
			passthrough = 0;

			if (!m_XrSessionCreated)
			{
				ERROR("CreatePassthroughHTC() XR_ERROR_SESSION_LOST.");
				return XrResult.XR_ERROR_SESSION_LOST;
			}
			if (!m_XrInstanceCreated)
			{
				ERROR("CreatePassthroughHTC() XR_ERROR_INSTANCE_LOST.");
				return XrResult.XR_ERROR_INSTANCE_LOST;
			}

			sb.Clear().Append("CreatePassthroughHTC() layerType: ").Append(layerType).Append(", compositionDepth: ").Append(compositionDepth); DEBUG(sb);

			XrResult result = XrResult.XR_ERROR_RUNTIME_FAILURE;

#if UNITY_STANDALONE
			result = xrCreatePassthroughHTC(m_XrSession, createInfo, out passthrough);
			DEBUG("CreatePassthroughHTC() xrCreatePassthroughHTC result: " + result);
			if (result == XrResult.XR_SUCCESS)
			{
				passthroughList.Add(passthrough);
				if (onDestroy != null) { OnPassthroughSessionDestroyHandlerDictionary.Add(passthrough, onDestroy); }
			}
#endif

#if UNITY_ANDROID
			int passthroughID = 0;

			if (createInfo.form == XrPassthroughFormHTC.XR_PASSTHROUGH_FORM_PLANAR_HTC)
				passthroughID = ViveCreatePassthrough(m_XrSession, layerType, PassthroughLayerForm.Planar, compositionDepth);
			else // createInfo.form == XrPassthroughFormHTC.XR_PASSTHROUGH_FORM_PROJECTED_HTC
				passthroughID = ViveCreatePassthrough(m_XrSession, layerType, PassthroughLayerForm.Projected, compositionDepth);

			sb.Clear().Append("CreatePassthroughHTC() CreatePassthrough passthroughID: ").Append(passthroughID); DEBUG(sb);
			if (passthroughID != 0)
			{
				passthrough = (UInt64)(passthroughID & 0x7FFFFFFF);
				passthroughList.Add(passthrough);
				if (onDestroy != null) { OnPassthroughSessionDestroyHandlerDictionary.Add(passthrough, onDestroy); }
				result = XrResult.XR_SUCCESS;
			}
#endif
			return result;
		}
		public XrResult CreatePassthroughHTC(XrPassthroughCreateInfoHTC createInfo, out XrPassthroughHTC passthrough, CompositionLayer.LayerType layerType, OnPassthroughSessionDestroyDelegate onDestroy = null)
		{
			return CreatePassthroughHTC(createInfo, out passthrough, layerType, 0, onDestroy);
		}
		public XrResult CreatePassthroughHTC(XrPassthroughCreateInfoHTC createInfo, out XrPassthroughHTC passthrough, CompositionLayer.LayerType layerType = CompositionLayer.LayerType.Overlay)
		{
			return CreatePassthroughHTC(createInfo, out passthrough, layerType, 0, null);
		}

		public XrResult DestroyPassthroughHTC(XrPassthroughHTC passthrough)
		{
			if (!passthroughList.Contains(passthrough))
			{
				ERROR("DestroyPassthroughHTC() Invalid passthrough: " + passthrough);
				return XrResult.XR_ERROR_VALIDATION_FAILURE;
			}

			sb.Clear().Append("DestroyPassthroughHTC() passthrough: ").Append(passthrough); DEBUG(sb);

			XrResult result = XrResult.XR_ERROR_RUNTIME_FAILURE;

			int passthroughID = (int)(passthrough & 0x00007FFF);
#if UNITY_STANDALONE
			result = xrDestroyPassthroughHTC(passthrough);
			sb.Clear().Append("DestroyPassthroughHTC() ").Append(passthrough).Append(", result: ").Append(result); DEBUG(sb);
			if (result == XrResult.XR_SUCCESS)
			{
				passthroughList.Remove(passthrough);
				if (OnPassthroughSessionDestroyHandlerDictionary.ContainsKey(passthrough))
					OnPassthroughSessionDestroyHandlerDictionary.Remove(passthrough);
			}
#endif

#if UNITY_ANDROID
			bool ret = ViveDestroyPassthrough(passthroughID);
			sb.Clear().Append("DestroyPassthroughHTC() ").Append(passthroughID).Append(", ret: ").Append(ret); DEBUG(sb);
			if (ret)
			{
				passthroughList.Remove(passthrough);
				if (OnPassthroughSessionDestroyHandlerDictionary.ContainsKey(passthrough))
					OnPassthroughSessionDestroyHandlerDictionary.Remove(passthrough);
				result = XrResult.XR_SUCCESS;
			}
#endif
			return result;
		}

		/// <summary>
		/// According to XRInputSubsystem's tracking origin mode, return the corresponding XrSpace.
		/// </summary>
		/// <returns><see cref="XrSpace"/> for tracking origin.</returns>
		public XrSpace GetTrackingSpace()
		{
			XrSpace space = GetCurrentAppSpace();
			sb.Clear().Append("GetTrackingSpace() ").Append(space); DEBUG(sb);
			return space;
		}

		private List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>();
		/// <summary>
		/// Helper function to get XrSpace from space type.
		/// </summary>
		public XrSpace GetXrSpaceFromSpaceType(ProjectedPassthroughSpaceType spaceType)
		{
			XrSpace meshSpace = 0;
			switch (spaceType)
			{
				case ProjectedPassthroughSpaceType.Headlock:
					meshSpace = HeadLockSpace;
					break;
				case ProjectedPassthroughSpaceType.Worldlock:
				default:
					XRInputSubsystem subsystem = null;
					SubsystemManager.GetInstances(inputSubsystems);
					if (inputSubsystems.Count > 0)
					{
						subsystem = inputSubsystems[0];
					}

					if (subsystem != null)
					{
						TrackingOriginModeFlags trackingOriginMode = subsystem.GetTrackingOriginMode();

						switch (trackingOriginMode)
						{
							default:
							case TrackingOriginModeFlags.Floor:
								meshSpace = WorldLockSpaceOriginOnFloor;
								break;
							case TrackingOriginModeFlags.Device:
								meshSpace = WorldLockSpaceOriginOnHead;
								break;
						}
					}
					else
					{
						meshSpace = WorldLockSpaceOriginOnFloor;
					}
					break;
			}

			return meshSpace;
		}

		public XrResult EnumeratePassthroughImageRatesHTC([In] UInt32 imageRateCapacityInput, ref UInt32 imageRateCountOutput,[In, Out] XrPassthroughConfigurationImageRateHTC[] imageRates)
		{
			if (!m_XrSessionCreated)
			{
				ERROR("EnumeratePassthroughImageRatesHTC() XR_ERROR_SESSION_LOST.");
				return XrResult.XR_ERROR_SESSION_LOST;
			}
			return xrEnumeratePassthroughImageRatesHTC(m_XrSession, imageRateCapacityInput, ref imageRateCountOutput, imageRates);
		}

		public XrResult GetPassthroughConfigurationHTC(IntPtr config)
		{
			if (!m_XrSessionCreated)
			{
				ERROR("GetPassthroughConfigurationHTC() XR_ERROR_SESSION_LOST.");
				return XrResult.XR_ERROR_SESSION_LOST;
			}
			return xrGetPassthroughConfigurationHTC(m_XrSession, config);
		}

		public XrResult SetPassthroughConfigurationHTC(IntPtr config)
		{
			if (!m_XrSessionCreated)
			{
				ERROR("SetPassthroughConfigurationHTC() XR_ERROR_SESSION_LOST.");
				return XrResult.XR_ERROR_SESSION_LOST;
			}
			return xrSetPassthroughConfigurationHTC(m_XrSession, config);
		}

		private XrBool32 m_SupportsImageRate;
		private XrBool32 m_SupportsImageQuality;
		XrSystemProperties systemProperties;
		XrSystemPassthroughConfigurationPropertiesHTC passthroughConfigurationProperties;
		private void CheckConfigurationSupport()
		{
			m_SupportsImageRate = false;
			m_SupportsImageQuality = false;
			if (!m_XrSessionCreated)
			{
				ERROR("CheckUserPresenceSupport() session is not created.");
				return;
			}

			passthroughConfigurationProperties.type = XrStructureType.XR_TYPE_SYSTEM_PASSTHROUGH_CONFIGURATION_PROPERTIES_HTC;
			passthroughConfigurationProperties.next = IntPtr.Zero;
			systemProperties.type = XrStructureType.XR_TYPE_SYSTEM_PROPERTIES;
			systemProperties.next = Marshal.AllocHGlobal(Marshal.SizeOf(passthroughConfigurationProperties));

			long offset = 0;
			if (IntPtr.Size == 4)
				offset = systemProperties.next.ToInt32();
			else
				offset = systemProperties.next.ToInt64();

			IntPtr passthroughConfigurationPtr = new IntPtr(offset);
			Marshal.StructureToPtr(passthroughConfigurationProperties, passthroughConfigurationPtr, false);

			if (GetSystemProperties(ref systemProperties) == XrResult.XR_SUCCESS)
			{
				if (IntPtr.Size == 4)
					offset = systemProperties.next.ToInt32();
				else
					offset = systemProperties.next.ToInt64();

				passthroughConfigurationPtr = new IntPtr(offset);
				passthroughConfigurationProperties = (XrSystemPassthroughConfigurationPropertiesHTC)Marshal.PtrToStructure(passthroughConfigurationPtr, typeof(XrSystemPassthroughConfigurationPropertiesHTC));

				sb.Clear().Append("CheckConfigurationSupport() supportsImageQuality: ").Append((UInt32)passthroughConfigurationProperties.supportsImageQuality); DEBUG(sb);
				sb.Clear().Append("CheckConfigurationSupport() supportsImageRate: ").Append((UInt32)passthroughConfigurationProperties.supportsImageRate); DEBUG(sb);
				m_SupportsImageQuality = passthroughConfigurationProperties.supportsImageQuality;
				m_SupportsImageRate = passthroughConfigurationProperties.supportsImageRate;
			}
			else
			{
				ERROR("CheckSupport() GetSystemProperties failed.");
			}

			Marshal.FreeHGlobal(systemProperties.next);
		}
		public bool SupportsImageRate() { return m_SupportsImageRate; }
		public bool SupportsImageQuality() { return m_SupportsImageQuality; }

#endregion
	}
}
