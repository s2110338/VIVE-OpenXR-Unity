// Copyright HTC Corporation All Rights Reserved.
using System.Runtime.InteropServices;
using System;
using UnityEngine;
using AOT;
using System.Collections.Generic;
using System.Text;

namespace VIVE.OpenXR
{
    /// <summary>
    /// This class is made for all features that need to intercept OpenXR API calls.
    /// Some APIs will be called by Unity internally, and we need to intercept them in c# to get some information.
    /// Append more interceptable functions for this class by adding a new partial class.
    /// The partial class can help the delegate name be nice to read and search.
    /// Please create per function in one partial class.
    /// 
    /// For all features want to use this class, please call <see cref="HookGetInstanceProcAddr" /> in your feature class.
    /// For example:
    ///     protected override IntPtr HookGetInstanceProcAddr(IntPtr func)
    ///     {
    ///         return ViveInterceptors.Instance.HookGetInstanceProcAddr(func);
    ///     }
    /// </summary>
	partial class ViveInterceptors
	{
		public const string TAG = "VIVE.OpenXR.ViveInterceptors";
		static StringBuilder m_sb = null;
		static StringBuilder sb {
			get {
				if (m_sb == null) { m_sb = new StringBuilder(); }
				return m_sb;
			}
		}
		static void DEBUG(StringBuilder msg) { Debug.LogFormat("{0} {1}", TAG, msg); }
		static void ERROR(StringBuilder msg) { Debug.LogErrorFormat("{0} {1}", TAG, msg); }

        public static ViveInterceptors instance = null;
        public static ViveInterceptors Instance
        {
            get
            {
                if (instance == null)
                    instance = new ViveInterceptors();
                return instance;
            }
        }

        public ViveInterceptors()
        {
            Debug.Log("ViveInterceptors");
        }

        public delegate XrResult DelegateXrGetInstanceProcAddr(XrInstance instance, string name, out IntPtr function);
        private static readonly DelegateXrGetInstanceProcAddr hookXrGetInstanceProcAddrHandle = new DelegateXrGetInstanceProcAddr(XrGetInstanceProcAddrInterceptor);
        private static readonly IntPtr hookGetInstanceProcAddrHandlePtr = Marshal.GetFunctionPointerForDelegate(hookXrGetInstanceProcAddrHandle);
        static DelegateXrGetInstanceProcAddr XrGetInstanceProcAddrOriginal = null;

        [MonoPInvokeCallback(typeof(DelegateXrGetInstanceProcAddr))]
        private static XrResult XrGetInstanceProcAddrInterceptor(XrInstance instance, string name, out IntPtr function)
        {
            // Used to check if the original function is already hooked.
            if (instance == 0 && name == "ViveInterceptorHooked")
            {
                function = IntPtr.Zero;
                return XrResult.XR_SUCCESS;
            }

            // Custom interceptors
            if (name == "xrWaitFrame" && requiredFunctions.Contains(name))
            {
                Debug.Log($"{TAG}: XrGetInstanceProcAddrInterceptor() {name} is intercepted.");
                var ret = XrGetInstanceProcAddrOriginal(instance, name, out function);
                if (ret == XrResult.XR_SUCCESS)
                {
                    XrWaitFrameOriginal = Marshal.GetDelegateForFunctionPointer<DelegateXrWaitFrame>(function);
                    function = xrWaitFrameInterceptorPtr;
                }
                return ret;
            }

            if (name == "xrEndFrame" && requiredFunctions.Contains(name))
            {
                Debug.Log($"{TAG}: XrGetInstanceProcAddrInterceptor() {name} is intercepted.");
                var ret = XrGetInstanceProcAddrOriginal(instance, name, out function);
                if (ret == XrResult.XR_SUCCESS)
                {
                    XrEndFrameOriginal = Marshal.GetDelegateForFunctionPointer<DelegateXrEndFrame>(function);
                    function = xrEndFrameInterceptorPtr;
                }
                return ret;
            }

#if PERFORMANCE_TEST
            if (name == "xrLocateSpace" && requiredFunctions.Contains(name))
            {
                Debug.Log($"{TAG}: XrGetInstanceProcAddrInterceptor() {name} is intercepted.");
                var ret = XrGetInstanceProcAddrOriginal(instance, name, out function);
                if (ret == XrResult.XR_SUCCESS)
                {
                    XrLocateSpaceOriginal = Marshal.GetDelegateForFunctionPointer<DelegateXrLocateSpace>(function);
                    function = xrLocateSpaceInterceptorPtr;
                }
                return ret;
            }
#endif
            if (name == "xrPollEvent" && requiredFunctions.Contains(name))
            {
                Debug.Log($"{TAG}: XrGetInstanceProcAddrInterceptor() {name} is intercepted.");
                var ret = XrGetInstanceProcAddrOriginal(instance, name, out function);
                if (ret == XrResult.XR_SUCCESS)
                {
                    xrPollEventOrigin = Marshal.GetDelegateForFunctionPointer < xrPollEventDelegate > (function);
                    function = xrPollEventPtr;
                }
                return ret;
            }
            if (name == "xrBeginSession" && requiredFunctions.Contains(name))
            {
                Debug.Log($"{TAG}: XrGetInstanceProcAddrInterceptor() {name} is intercepted.");
                var ret = XrGetInstanceProcAddrOriginal(instance, name, out function);
                if (ret == XrResult.XR_SUCCESS)
                {
                    xrBeginSessionOrigin = Marshal.GetDelegateForFunctionPointer<xrBeginSessionDelegate>(function);
                    function = xrBeginSessionPtr;
                }
                return ret;
            }

            return XrGetInstanceProcAddrOriginal(instance, name, out function);
        }

        public IntPtr HookGetInstanceProcAddr(IntPtr func)
        {
            Debug.Log($"{TAG}: HookGetInstanceProcAddr");
            if (XrGetInstanceProcAddrOriginal == null)
            {
                Debug.Log($"{TAG}: registering our own xrGetInstanceProcAddr");
                XrGetInstanceProcAddrOriginal = Marshal.GetDelegateForFunctionPointer<DelegateXrGetInstanceProcAddr>(func);

#if UNITY_EDITOR
                if (Application.isEditor) {
                    // This is a trick to check if the original function is already hooked by this class.  Sometimes, the static XrGetInstanceProcAddrOriginal didn't work as expected.
                    Debug.Log($"{TAG}: Check if duplicate hooked by this script with instance=0 and \"ViveInterceptorHooked\" name.  If following a loader error, ignore it.");
                    // E OpenXR-Loader: Error [SPEC | xrGetInstanceProcAddr | VUID-xrGetInstanceProcAddr-instance-parameter] : XR_NULL_HANDLE for instance but query for ViveInterceptorHooked requires a valid instance

                    // Call XrGetInstanceProcAddrOriginal to check if the original function is already hooked by this class
                    if (XrGetInstanceProcAddrOriginal(0, "ViveInterceptorHooked", out IntPtr function) == XrResult.XR_SUCCESS)
                    {
                        // If it is called successfully, it means the original function is already hooked.  So we should return the original function.
                        Debug.Log($"{TAG}: Already hooked");
                        return func;
                    }
                }
#endif

                return hookGetInstanceProcAddrHandlePtr;
            }
            else
            {
                // Dont return hookGetInstanceProcAddrHandlePtr again.
                // If this hook function is called by multiple features, it should only work at the first time.
                // If called by other features, it should return the original function.
                return func;
            }
        }

        static readonly List<string> requiredFunctions = new List<string>();

        /// <summary>
        /// Call before <see cref="HookGetInstanceProcAddr" /> to add required functions."/>
        /// </summary>
        /// <param name="name"></param>
        public void AddRequiredFunction(string name)
        {
            if (requiredFunctions.Contains(name)) return;
            Debug.Log($"{TAG}: AddRequiredFunction({name})");
            requiredFunctions.Add(name);
        }
    }
}

