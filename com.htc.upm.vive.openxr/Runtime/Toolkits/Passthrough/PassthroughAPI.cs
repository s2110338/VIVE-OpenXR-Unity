// Copyright HTC Corporation All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using System.Threading.Tasks;
using VIVE.OpenXR;

namespace VIVE.OpenXR.Passthrough
{
	public static class PassthroughAPI
	{
		#region LOG
		const string LOG_TAG = "VIVE.OpenXR.Passthrough.PassthroughAPI";
		static void DEBUG(string msg) { Debug.LogFormat("{0} {1}", LOG_TAG, msg); }
		static void WARNING(string msg) { Debug.LogWarningFormat("{0} {1}", LOG_TAG, msg); }
		static void ERROR(string msg) { Debug.LogErrorFormat("{0} {1}", LOG_TAG, msg); }
		#endregion

		private static VivePassthrough passthroughFeature = null;
		private static bool AssertFeature()
		{
			if (passthroughFeature == null) { passthroughFeature = OpenXRSettings.Instance.GetFeature<VivePassthrough>(); }
			if (passthroughFeature) { return true; }
			return false;
		}

#if UNITY_STANDALONE
		private static Dictionary<XrPassthroughHTC, XrCompositionLayerPassthroughHTC> passthrough2Layer = new Dictionary<XrPassthroughHTC, XrCompositionLayerPassthroughHTC>();
		private static Dictionary<XrPassthroughHTC, IntPtr> passthrough2LayerPtr = new Dictionary<XrPassthroughHTC, IntPtr>();
		private static Dictionary<XrPassthroughHTC, bool> passthrough2IsUnderLay= new Dictionary<XrPassthroughHTC, bool>();
		private static Dictionary<XrPassthroughHTC, XrPassthroughMeshTransformInfoHTC> passthrough2meshTransform = new Dictionary<XrPassthroughHTC, XrPassthroughMeshTransformInfoHTC>();
		private static Dictionary<XrPassthroughHTC, IntPtr> passthrough2meshTransformInfoPtr = new Dictionary<XrPassthroughHTC, IntPtr>();
#endif

		#region Public APIs
		/// <summary>
		/// Creates a fullscreen passthrough.
		/// Passthroughs will be destroyed automatically when the current <see cref="XrSession"/> is destroyed.
		/// </summary>
		/// <param name="passthrough">The created <see cref="XrPassthroughHTC"/></param>
		/// <param name="layerType">The <see cref="LayerType"/> specifies whether the passthrough is an overlay or underlay.</param>
		/// <param name="onDestroyPassthroughSessionHandler">A <see cref="VivePassthrough.OnPassthroughSessionDestroyDelegate">delegate</see> will be invoked when the current OpenXR Session is going to be destroyed.</param>
		/// <param name="alpha">The alpha value of the passthrough layer within the range [0, 1] where 1 (Opaque) is default.</param>
		/// <param name="compositionDepth">The composition depth relative to other composition layers if present where 0 is default.</param>
		/// <returns>XR_SUCCESS for success.</returns>
		public static XrResult CreatePlanarPassthrough(out XrPassthroughHTC passthrough, CompositionLayer.LayerType layerType, VivePassthrough.OnPassthroughSessionDestroyDelegate onDestroyPassthroughSessionHandler = null, float alpha = 1f, uint compositionDepth = 0)
		{
			passthrough = 0;
			XrResult res = XrResult.XR_ERROR_RUNTIME_FAILURE;

			if (!AssertFeature())
			{
				ERROR("HTC_Passthrough feature instance not found.");
				return res;
			}
			XrPassthroughCreateInfoHTC createInfo = new XrPassthroughCreateInfoHTC(
				XrStructureType.XR_TYPE_PASSTHROUGH_CREATE_INFO_HTC,
				new IntPtr(6), //Enter IntPtr(0) for backward compatibility (using createPassthrough to enable the passthrough feature), or enter IntPtr(6) to enable the passthrough feature based on the layer submitted to endframe.
				XrPassthroughFormHTC.XR_PASSTHROUGH_FORM_PLANAR_HTC
			);

#if UNITY_ANDROID
			res = passthroughFeature.CreatePassthroughHTC(createInfo, out passthrough, layerType, compositionDepth, onDestroyPassthroughSessionHandler);
			DEBUG("CreatePlanarPassthrough() CreatePassthroughHTC result: " + res + ", passthrough: " + passthrough);
#endif
#if UNITY_STANDALONE
			res = XR_HTC_passthrough.xrCreatePassthroughHTC(createInfo, out passthrough);
			if(res == XrResult.XR_SUCCESS)
            {
				XrPassthroughColorHTC passthroughColor = new XrPassthroughColorHTC(
						in_type: XrStructureType.XR_TYPE_PASSTHROUGH_COLOR_HTC,
						in_next: IntPtr.Zero,
						in_alpha: alpha);
				XrCompositionLayerPassthroughHTC compositionLayerPassthrough = new XrCompositionLayerPassthroughHTC(
						in_type: XrStructureType.XR_TYPE_COMPOSITION_LAYER_PASSTHROUGH_HTC,
						in_next: IntPtr.Zero,
						in_layerFlags: (UInt64)XrCompositionLayerFlagBits.XR_COMPOSITION_LAYER_UNPREMULTIPLIED_ALPHA_BIT,
						in_space: 0,
						in_passthrough: passthrough,
						in_color: passthroughColor);
				passthrough2Layer.Add(passthrough, compositionLayerPassthrough);
				IntPtr layerPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(XrCompositionLayerPassthroughHTC)));
				passthrough2LayerPtr.Add(passthrough, layerPtr);
				if (layerType == CompositionLayer.LayerType.Underlay)
					passthrough2IsUnderLay.Add(passthrough, true);
				if (layerType == CompositionLayer.LayerType.Overlay)
					passthrough2IsUnderLay.Add(passthrough, false);
			}
#endif
			if (res == XrResult.XR_SUCCESS)
			{
				SetPassthroughAlpha(passthrough, alpha);
			}
			return res;
		}

		/// <summary>
		/// Creates a projected passthrough (i.e. Passthrough is only partially visible). Visible region of the projected passthrough is determined by the mesh and its transform.
		/// Passthroughs will be destroyed automatically when the current <see cref="XrSession"/> is destroyed.
		/// </summary>
		/// <param name="passthrough">The created <see cref="XrPassthroughHTC"/></param>
		/// <param name="layerType">The <see cref="LayerType"/> specifies whether the passthrough is an overlay or underlay.</param>
		/// <param name="vertexBuffer">Positions of the vertices in the mesh.</param>
		/// <param name="indexBuffer">List of triangles represented by indices into the <paramref name="vertexBuffer"/>.</param>
		/// <param name="spaceType">The projected passthrough's <see cref="ProjectedPassthroughSpaceType"/></param>
		/// <param name="meshPosition">Position of the mesh.</param>
		/// <param name="meshOrientation">Orientation of the mesh.</param>
		/// <param name="meshScale">Scale of the mesh.</param>
		/// <param name="onDestroyPassthroughSessionHandler">A <see cref="VivePassthrough.OnPassthroughSessionDestroyDelegate">delegate</see> will be invoked when the current OpenXR Session is going to be destroyed.</param>
		/// <param name="alpha">The alpha value of the passthrough layer within the range [0, 1] where 1 (Opaque) is default.</param>
		/// <param name="compositionDepth">The composition depth relative to other composition layers if present where 0 is default.</param>
		/// <param name="trackingToWorldSpace">Specify whether or not the position and rotation of the mesh transform have to be converted from tracking space to world space.</param>
		/// <param name="convertFromUnityToOpenXR">Specify whether or not the parameters <paramref name="vertexBuffer"/>, <paramref name="indexBuffer"/>, <paramref name="meshPosition"/> and <paramref name="meshOrientation"/> have to be converted to OpenXR coordinate.</param>
		/// <returns>XR_SUCCESS for success.</returns>
		public static XrResult CreateProjectedPassthrough(out XrPassthroughHTC passthrough, CompositionLayer.LayerType layerType,
											  [In, Out] Vector3[] vertexBuffer, [In, Out] int[] indexBuffer, //For Mesh
											  ProjectedPassthroughSpaceType spaceType, Vector3 meshPosition, Quaternion meshOrientation, Vector3 meshScale, //For Mesh Transform
											  VivePassthrough.OnPassthroughSessionDestroyDelegate onDestroyPassthroughSessionHandler = null,
											  float alpha = 1f, uint compositionDepth = 0, bool trackingToWorldSpace = true, bool convertFromUnityToOpenXR = true)
		{
			passthrough = 0;
			XrResult res = XrResult.XR_ERROR_RUNTIME_FAILURE;

			if (!AssertFeature())
			{
				ERROR("HTC_Passthrough feature instance not found.");
				return res;
			}

			if (vertexBuffer.Length < 3 || indexBuffer.Length % 3 != 0) //Must have at least 3 vertices and complete triangles
			{
				ERROR("Mesh data invalid.");
				return res;
			}

			XrPassthroughCreateInfoHTC createInfo = new XrPassthroughCreateInfoHTC(
				XrStructureType.XR_TYPE_PASSTHROUGH_CREATE_INFO_HTC,
				new IntPtr(6), //Enter IntPtr(0) for backward compatibility (using createPassthrough to enable the passthrough feature), or enter IntPtr(6) to enable the passthrough feature based on the layer submitted to endframe.
				XrPassthroughFormHTC.XR_PASSTHROUGH_FORM_PROJECTED_HTC
			);

#if UNITY_STANDALONE
			res = XR_HTC_passthrough.xrCreatePassthroughHTC(createInfo, out passthrough);
			if (res == XrResult.XR_SUCCESS)
			{
				XrPassthroughMeshTransformInfoHTC PassthroughMeshTransformInfo = new XrPassthroughMeshTransformInfoHTC(
						in_type: XrStructureType.XR_TYPE_PASSTHROUGH_MESH_TRANSFORM_INFO_HTC,
						in_next: IntPtr.Zero,
						in_vertexCount: 0,
						in_vertices: new XrVector3f[0],
						in_indexCount: 0,
						in_indices: new UInt32[0],
						in_baseSpace: XR_HTC_passthrough.Interop.GetTrackingSpace(),
						in_time: XR_HTC_passthrough.Interop.GetFrameState().predictedDisplayTime,
						in_pose: new XrPosef(),
						in_scale: new XrVector3f()
						);
				IntPtr meshTransformInfoPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(XrPassthroughMeshTransformInfoHTC)));
				Marshal.StructureToPtr(PassthroughMeshTransformInfo, meshTransformInfoPtr, false);
				XrPassthroughColorHTC passthroughColor = new XrPassthroughColorHTC(
						in_type: XrStructureType.XR_TYPE_PASSTHROUGH_COLOR_HTC,
						in_next: IntPtr.Zero,
						in_alpha: alpha);
				XrCompositionLayerPassthroughHTC compositionLayerPassthrough = new XrCompositionLayerPassthroughHTC(
						in_type: XrStructureType.XR_TYPE_COMPOSITION_LAYER_PASSTHROUGH_HTC,
						in_next: meshTransformInfoPtr,
						in_layerFlags: (UInt64)XrCompositionLayerFlagBits.XR_COMPOSITION_LAYER_UNPREMULTIPLIED_ALPHA_BIT,
						in_space: 0,
						in_passthrough: passthrough,
						in_color: passthroughColor);
				passthrough2meshTransform.Add(passthrough, PassthroughMeshTransformInfo);
				passthrough2meshTransformInfoPtr.Add(passthrough, meshTransformInfoPtr);
				passthrough2Layer.Add(passthrough, compositionLayerPassthrough);
				IntPtr layerPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(XrCompositionLayerPassthroughHTC)));
				passthrough2LayerPtr.Add(passthrough, layerPtr);
				if (layerType == CompositionLayer.LayerType.Underlay)
					passthrough2IsUnderLay.Add(passthrough, true);
				if (layerType == CompositionLayer.LayerType.Overlay)
					passthrough2IsUnderLay.Add(passthrough, false);
			}
#endif
#if UNITY_ANDROID
			res = passthroughFeature.CreatePassthroughHTC(createInfo, out passthrough, layerType, compositionDepth, onDestroyPassthroughSessionHandler);
			DEBUG("CreateProjectedPassthrough() CreatePassthroughHTC result: " + res + ", passthrough: " + passthrough);
#endif
			if (res == XrResult.XR_SUCCESS)
			{
				SetPassthroughAlpha(passthrough, alpha);
				SetProjectedPassthroughMesh(passthrough, vertexBuffer, indexBuffer, convertFromUnityToOpenXR);
				SetProjectedPassthroughMeshTransform(passthrough, spaceType, meshPosition, meshOrientation, meshScale, trackingToWorldSpace, convertFromUnityToOpenXR);
			}

			return res;
		}

		/// <summary>
		/// Creates a projected passthrough (i.e. Passthrough is only partially visible). Visible region of the projected passthrough is determined by the mesh and its transform.
		/// Passthroughs will be destroyed automatically when the current <see cref="XrSession"/> is destroyed.
		/// </summary>
		/// <param name="passthrough">The created <see cref="XrPassthroughHTC"/></param>
		/// <param name="layerType">The <see cref="LayerType"/> specifies whether the passthrough is an overlay or underlay.</param>
		/// <param name="onDestroyPassthroughSessionHandler">A <see cref="VivePassthrough.OnPassthroughSessionDestroyDelegate">delegate</see> will be invoked when the current OpenXR Session is going to be destroyed.</param>
		/// <param name="alpha">The alpha value of the passthrough layer within the range [0, 1] where 1 (Opaque) is default.</param>
		/// <param name="compositionDepth">The composition depth relative to other composition layers if present where 0 is default.</param>
		/// <returns>XR_SUCCESS for success.</returns>
		public static XrResult CreateProjectedPassthrough(out XrPassthroughHTC passthrough, CompositionLayer.LayerType layerType, VivePassthrough.OnPassthroughSessionDestroyDelegate onDestroyPassthroughSessionHandler = null, float alpha = 1f, uint compositionDepth = 0)
		{
			passthrough = 0;
			XrResult res = XrResult.XR_ERROR_RUNTIME_FAILURE;

			if (!AssertFeature())
			{
				ERROR("HTC_Passthrough feature instance not found.");
				return res;
			}

			XrPassthroughCreateInfoHTC createInfo = new XrPassthroughCreateInfoHTC(
				XrStructureType.XR_TYPE_PASSTHROUGH_CREATE_INFO_HTC,
				new IntPtr(6), //Enter IntPtr(0) for backward compatibility (using createPassthrough to enable the passthrough feature), or enter IntPtr(6) to enable the passthrough feature based on the layer submitted to endframe.
				XrPassthroughFormHTC.XR_PASSTHROUGH_FORM_PROJECTED_HTC
			);

#if UNITY_STANDALONE
			res = XR_HTC_passthrough.xrCreatePassthroughHTC(createInfo, out passthrough);
			if (res == XrResult.XR_SUCCESS)
			{
				XrPassthroughMeshTransformInfoHTC PassthroughMeshTransformInfo = new XrPassthroughMeshTransformInfoHTC(
						in_type: XrStructureType.XR_TYPE_PASSTHROUGH_MESH_TRANSFORM_INFO_HTC,
						in_next: IntPtr.Zero,
						in_vertexCount: 0,
						in_vertices: new XrVector3f[0],
						in_indexCount: 0,
						in_indices: new UInt32[0],
						in_baseSpace: XR_HTC_passthrough.Interop.GetTrackingSpace(),
						in_time: XR_HTC_passthrough.Interop.GetFrameState().predictedDisplayTime,
						in_pose: new XrPosef(),
						in_scale: new XrVector3f()
						);
				IntPtr meshTransformInfoPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(XrPassthroughMeshTransformInfoHTC)));
				Marshal.StructureToPtr(PassthroughMeshTransformInfo, meshTransformInfoPtr, false);
				XrPassthroughColorHTC passthroughColor = new XrPassthroughColorHTC(
						in_type: XrStructureType.XR_TYPE_PASSTHROUGH_COLOR_HTC,
						in_next: IntPtr.Zero,
						in_alpha: alpha);
				XrCompositionLayerPassthroughHTC compositionLayerPassthrough = new XrCompositionLayerPassthroughHTC(
						in_type: XrStructureType.XR_TYPE_COMPOSITION_LAYER_PASSTHROUGH_HTC,
						in_next: meshTransformInfoPtr,
						in_layerFlags: (UInt64)XrCompositionLayerFlagBits.XR_COMPOSITION_LAYER_UNPREMULTIPLIED_ALPHA_BIT,
						in_space: 0,
						in_passthrough: passthrough,
						in_color: passthroughColor);
				passthrough2meshTransform.Add(passthrough, PassthroughMeshTransformInfo);
				passthrough2meshTransformInfoPtr.Add(passthrough, meshTransformInfoPtr);
				passthrough2Layer.Add(passthrough, compositionLayerPassthrough);
				IntPtr layerPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(XrCompositionLayerPassthroughHTC)));
				passthrough2LayerPtr.Add(passthrough, layerPtr);
				if (layerType == CompositionLayer.LayerType.Underlay)
					passthrough2IsUnderLay.Add(passthrough, true);
				if (layerType == CompositionLayer.LayerType.Overlay)
					passthrough2IsUnderLay.Add(passthrough, false);
			}
#endif
#if UNITY_ANDROID
			res = passthroughFeature.CreatePassthroughHTC(createInfo, out passthrough, layerType, onDestroyPassthroughSessionHandler);
			DEBUG("CreateProjectedPassthrough() CreatePassthroughHTC result: " + res + ", passthrough: " + passthrough);
#endif
			if (res == XrResult.XR_SUCCESS)
			{
				SetPassthroughAlpha(passthrough, alpha);
			}

			return res;
		}

#if UNITY_STANDALONE
		private static async void SubmitLayer()
        {
			await Task.Run(() => {
				int layerListCount = 0;
				while(layerListCount == 0)
                {
					System.Threading.Thread.Sleep(1);
					XR_HTC_passthrough.Interop.GetOriginEndFrameLayerList(out List<IntPtr> layerList);//GetOriginEndFrameLayers
					layerListCount = layerList.Count;
					foreach (var passthrough in passthrough2IsUnderLay.Keys)
					{
						//Get and submit layer list
						if (layerListCount != 0)
						{
							Marshal.StructureToPtr(passthrough2Layer[passthrough], passthrough2LayerPtr[passthrough], false);
							if (passthrough2IsUnderLay[passthrough])
								layerList.Insert(0, passthrough2LayerPtr[passthrough]);
							else
								layerList.Insert(1, passthrough2LayerPtr[passthrough]);
						}
					}
					if(layerListCount != 0)
						XR_HTC_passthrough.Interop.SubmitLayers(layerList);
				}
			});
			
		}
#endif

		/// <summary>
		/// To Destroying a passthrough.
		/// You should call this function when the <see cref="VivePassthrough.OnPassthroughSessionDestroyDelegate">delegate</see> is invoked.
		/// </summary>
		/// <param name="passthrough">The created <see cref="XrPassthroughHTC"/></param>
		/// <returns>XR_SUCCESS for success.</returns>
		public static XrResult DestroyPassthrough(XrPassthroughHTC passthrough)
		{
			XrResult res = XrResult.XR_ERROR_RUNTIME_FAILURE;

			if (!AssertFeature())
			{
				ERROR("HTC_Passthrough feature instance not found.");
				return res;
			}
			if (!passthroughFeature.PassthroughList.Contains(passthrough))
			{
				ERROR("Passthrough to be destroyed not found");
				return res;
			}

#if UNITY_STANDALONE
            XrPassthroughHTC pt = passthrough2Layer[passthrough].passthrough;
			XR_HTC_passthrough.xrDestroyPassthroughHTC(pt);
			passthrough2IsUnderLay.Remove(passthrough);
			SubmitLayer();
			passthrough2Layer.Remove(pt);
			if(passthrough2LayerPtr.ContainsKey(passthrough)) Marshal.FreeHGlobal(passthrough2LayerPtr[passthrough]);
			passthrough2LayerPtr.Remove(passthrough);
			if(passthrough2meshTransformInfoPtr.ContainsKey(passthrough)) Marshal.FreeHGlobal(passthrough2meshTransformInfoPtr[passthrough]);
			passthrough2meshTransformInfoPtr.Remove(passthrough);
			passthrough2meshTransform.Remove(passthrough);
			
			res = XrResult.XR_SUCCESS;
#elif UNITY_ANDROID
			res = passthroughFeature.DestroyPassthroughHTC(passthrough);
			DEBUG("DestroyPassthrough() DestroyPassthroughHTC result: " + res + ", passthrough: " + passthrough);
#endif
			return res;
		}

		/// <summary>
		/// Modifies the opacity of a specific passthrough layer.
		/// Can be used for both Planar and Projected passthroughs.
		/// </summary>
		/// <param name="passthrough">The created <see cref="XrPassthroughHTC"/></param>
		/// <param name="alpha">The alpha value of the passthrough layer within the range [0, 1] where 1 (Opaque) is default.</param>
		/// <param name="autoClamp">
		/// Specify whether out of range alpha values should be clamped automatically.
		/// When set to true, the function will clamp and apply the alpha value automatically.
		/// When set to false, the function will return false if the alpha is out of range.
		/// Default is true.
		/// </param>
		/// <returns>XR_SUCCESS for success.</returns>
		public static bool SetPassthroughAlpha(XrPassthroughHTC passthrough, float alpha, bool autoClamp = true)
		{
			bool ret = false;
			if (!AssertFeature())
			{
				ERROR("HTC_Passthrough feature instance not found.");
				return ret;
			}

#if UNITY_ANDROID
			if (autoClamp)
			{
				ret = passthroughFeature.SetAlpha(passthrough, Mathf.Clamp01(alpha));
			}
			else
			{
				if (alpha < 0f || alpha > 1f)
				{
					ERROR("SetPassthroughAlpha: Alpha out of range");
					return ret;
				}

				ret = passthroughFeature.SetAlpha(passthrough, alpha);
			}
			DEBUG("SetPassthroughAlpha() SetAlpha result: " + ret + ", passthrough: " + passthrough);
#endif

#if UNITY_STANDALONE
			if (passthrough2Layer.ContainsKey(passthrough))
			{
				XrCompositionLayerPassthroughHTC layer = passthrough2Layer[passthrough];
				layer.color.alpha = alpha;
				passthrough2Layer[passthrough] = layer;
				SubmitLayer();
				ret = true;
			}
			else
				ret = false;
#endif
			return ret;
		}

		/// <summary>
		/// Modifies the mesh data of a projected passthrough layer.
		/// </summary>
		/// <param name="passthrough">The created <see cref="XrPassthroughHTC"/></param>
		/// <param name="vertexBuffer">Positions of the vertices in the mesh.</param>
		/// <param name="indexBuffer">List of triangles represented by indices into the <paramref name="vertexBuffer"/>.</param>
		/// <param name="convertFromUnityToOpenXR">Specify whether or not the parameters <paramref name="vertexBuffer"/>, <paramref name="indexBuffer"/>, <paramref name="meshPosition"/> and <paramref name="meshOrientation"/> have to be converted to OpenXR coordinate.</param>
		/// <returns>XR_SUCCESS for success.</returns>
		public static bool SetProjectedPassthroughMesh(XrPassthroughHTC passthrough, [In, Out] Vector3[] vertexBuffer, [In, Out] int[] indexBuffer, bool convertFromUnityToOpenXR = true)
		{
			bool ret = false;

			if (!AssertFeature())
			{
				ERROR("HTC_Passthrough feature instance not found.");
				return ret;
			}

			if (vertexBuffer.Length < 3 || indexBuffer.Length % 3 != 0) //Must have at least 3 vertices and complete triangles
			{
				ERROR("Mesh data invalid.");
				return ret;
			}

			XrVector3f[] vertexBufferXrVector = new XrVector3f[vertexBuffer.Length];

			for (int i = 0; i < vertexBuffer.Length; i++)
			{
				vertexBufferXrVector[i] = OpenXRHelper.ToOpenXRVector(vertexBuffer[i], convertFromUnityToOpenXR);
			}

			uint[] indexBufferUint = new uint[indexBuffer.Length];

			for (int i = 0; i < indexBuffer.Length; i++)
			{
				indexBufferUint[i] = (uint)indexBuffer[i];
			}

#if UNITY_STANDALONE
			if (passthrough2meshTransformInfoPtr.ContainsKey(passthrough))
			{
				XrPassthroughMeshTransformInfoHTC MeshTransformInfo = passthrough2meshTransform[passthrough];
				MeshTransformInfo.vertexCount = (uint)vertexBuffer.Length;
				MeshTransformInfo.vertices = vertexBufferXrVector;
				MeshTransformInfo.indexCount = (uint)indexBuffer.Length;
				MeshTransformInfo.indices = indexBufferUint;
				passthrough2meshTransform[passthrough] = MeshTransformInfo;
				Marshal.StructureToPtr(MeshTransformInfo, passthrough2meshTransformInfoPtr[passthrough], false);
				XrCompositionLayerPassthroughHTC layer = passthrough2Layer[passthrough];
				layer.next = passthrough2meshTransformInfoPtr[passthrough];
				passthrough2Layer[passthrough] = layer;
				SubmitLayer();
				ret = true;
			}
			else
				ret = false;
#endif
			//Note: Ignore Clock-Wise definition of index buffer for now as passthrough extension does not have back-face culling
#if UNITY_ANDROID
			ret = passthroughFeature.SetMesh(passthrough, (uint)vertexBuffer.Length, vertexBufferXrVector, (uint)indexBuffer.Length, indexBufferUint); ;
			DEBUG("SetProjectedPassthroughMesh() SetMesh result: " + ret + ", passthrough: " + passthrough);
#endif
			return ret;
		}

		/// <summary>
		/// Modifies the mesh transform of a projected passthrough layer.
		/// </summary>
		/// <param name="passthrough">The created <see cref="XrPassthroughHTC"/></param>
		/// <param name="spaceType">The projected passthrough's <see cref="ProjectedPassthroughSpaceType"/></param>
		/// <param name="meshPosition">Position of the mesh.</param>
		/// <param name="meshOrientation">Orientation of the mesh.</param>
		/// <param name="meshScale">Scale of the mesh.</param>
		/// <param name="trackingToWorldSpace">Specify whether or not the position and rotation of the mesh transform have to be converted from tracking space to world space.</param>
		/// <param name="convertFromUnityToOpenXR">Specify whether or not the parameters <paramref name="vertexBuffer"/>, <paramref name="indexBuffer"/>, <paramref name="meshPosition"/> and <paramref name="meshOrientation"/> have to be converted to OpenXR coordinate.</param>
		/// <returns>XR_SUCCESS for success.</returns>
		public static bool SetProjectedPassthroughMeshTransform(XrPassthroughHTC passthrough, ProjectedPassthroughSpaceType spaceType, Vector3 meshPosition, Quaternion meshOrientation, Vector3 meshScale, bool trackingToWorldSpace = true, bool convertFromUnityToOpenXR = true)
		{
			bool ret = false;

			if (!AssertFeature())
			{
				ERROR("HTC_Passthrough feature instance not found.");
				return ret;
			}

			Vector3 trackingSpaceMeshPosition = meshPosition;
			Quaternion trackingSpaceMeshRotation = meshOrientation;
			TrackingSpaceOrigin currentTrackingSpaceOrigin = TrackingSpaceOrigin.Instance;

			if (currentTrackingSpaceOrigin != null && trackingToWorldSpace) //Apply origin correction to the mesh pose
			{
				Matrix4x4 trackingSpaceOriginTRS = Matrix4x4.TRS(currentTrackingSpaceOrigin.transform.position, currentTrackingSpaceOrigin.transform.rotation, Vector3.one);
				Matrix4x4 worldSpaceLayerPoseTRS = Matrix4x4.TRS(meshPosition, meshOrientation, Vector3.one);

				Matrix4x4 trackingSpaceLayerPoseTRS = trackingSpaceOriginTRS.inverse * worldSpaceLayerPoseTRS;

				trackingSpaceMeshPosition = trackingSpaceLayerPoseTRS.GetColumn(3); //4th Column of TRS Matrix is the position
				trackingSpaceMeshRotation = Quaternion.LookRotation(trackingSpaceLayerPoseTRS.GetColumn(2), trackingSpaceLayerPoseTRS.GetColumn(1));
			}

			XrPosef meshXrPose;
			meshXrPose.position = OpenXRHelper.ToOpenXRVector(trackingSpaceMeshPosition, convertFromUnityToOpenXR);
			meshXrPose.orientation = OpenXRHelper.ToOpenXRQuaternion(trackingSpaceMeshRotation, convertFromUnityToOpenXR);

			XrVector3f meshXrScale = OpenXRHelper.ToOpenXRVector(meshScale, false);

#if UNITY_STANDALONE
			if (passthrough2meshTransformInfoPtr.ContainsKey(passthrough))
			{
				XrPassthroughMeshTransformInfoHTC MeshTransformInfo = passthrough2meshTransform[passthrough];
				MeshTransformInfo.pose = meshXrPose;
				MeshTransformInfo.scale = meshXrScale;
				passthrough2meshTransform[passthrough] = MeshTransformInfo;
				Marshal.StructureToPtr(MeshTransformInfo, passthrough2meshTransformInfoPtr[passthrough], false);
				XrCompositionLayerPassthroughHTC layer = passthrough2Layer[passthrough];
				layer.next = passthrough2meshTransformInfoPtr[passthrough];
				passthrough2Layer[passthrough] = layer;
				SubmitLayer();
				ret = true;
			}
			else
				ret = false;
#endif

#if UNITY_ANDROID
			ret = passthroughFeature.SetMeshTransform(passthrough, passthroughFeature.GetXrSpaceFromSpaceType(spaceType), meshXrPose, meshXrScale);
			DEBUG("SetProjectedPassthroughMeshTransform() SetMeshTransform result: " + ret + ", passthrough: " + passthrough);
#endif
			return ret;
		}

		/// <summary>
		/// Modifies layer type and composition depth of a passthrough layer.
		/// </summary>
		/// <param name="passthrough">The created <see cref="XrPassthroughHTC"/></param>
		/// <param name="layerType">The <see cref="LayerType"/> specifies whether the passthrough is an overlay or underlay.</param>
		/// <param name="compositionDepth">The composition depth relative to other composition layers if present where 0 is default.</param>
		/// <returns>XR_SUCCESS for success.</returns>
		public static bool SetPassthroughLayerType(XrPassthroughHTC passthrough, CompositionLayer.LayerType layerType, uint compositionDepth = 0)
		{
			bool ret = false;

			if (!AssertFeature())
			{
				ERROR("HTC_Passthrough feature instance not found.");
				return ret;
			}

#if UNITY_STANDALONE
			if (passthrough2IsUnderLay.ContainsKey(passthrough))
			{
				passthrough2IsUnderLay[passthrough] = layerType == CompositionLayer.LayerType.Underlay ? true : false;
				SubmitLayer();
				ret = true;
			}
			else
				ret = false;
#endif

#if UNITY_ANDROID
			ret = passthroughFeature.SetLayerType(passthrough, layerType, compositionDepth);
			DEBUG("SetPassthroughLayerType() SetLayerType result: " + ret + ", passthrough: " + passthrough);
#endif
			return ret;
		}

		/// <summary>
		/// Modifies the space of a projected passthrough layer.
		/// </summary>
		/// <param name="passthrough">The created <see cref="XrPassthroughHTC"/></param>
		/// <param name="spaceType">The projected passthrough's <see cref="ProjectedPassthroughSpaceType"/></param>
		/// <returns>XR_SUCCESS for success.</returns>
		public static bool SetProjectedPassthroughSpaceType(XrPassthroughHTC passthrough, ProjectedPassthroughSpaceType spaceType)
		{
			bool ret = false;

			if (!AssertFeature())
			{
				ERROR("HTC_Passthrough feature instance not found.");
				return ret;
			}

#if UNITY_STANDALONE
			if (passthrough2meshTransformInfoPtr.ContainsKey(passthrough))
			{
				XrPassthroughMeshTransformInfoHTC MeshTransformInfo = passthrough2meshTransform[passthrough];
				MeshTransformInfo.baseSpace = passthroughFeature.GetXrSpaceFromSpaceType(spaceType);
				passthrough2meshTransform[passthrough] = MeshTransformInfo;
				Marshal.StructureToPtr(MeshTransformInfo, passthrough2meshTransformInfoPtr[passthrough], false);
				XrCompositionLayerPassthroughHTC layer = passthrough2Layer[passthrough];
				layer.next = passthrough2meshTransformInfoPtr[passthrough];
				passthrough2Layer[passthrough] = layer;
				SubmitLayer();
				ret = true;
			}
			else
				ret = false;
#endif

#if UNITY_ANDROID
			ret = passthroughFeature.SetMeshTransformSpace(passthrough, passthroughFeature.GetXrSpaceFromSpaceType(spaceType));
			DEBUG("SetProjectedPassthroughSpaceType() SetMeshTransformSpace result: " + ret + ", passthrough: " + passthrough);
#endif
			return ret;
		}

		/// <summary>
		/// Modifies the mesh position of a projected passthrough layer.
		/// </summary>
		/// <param name="passthrough">The created <see cref="XrPassthroughHTC"/></param>
		/// <param name="meshPosition">Position of the mesh.</param>
		/// <param name="trackingToWorldSpace">Specify whether or not the position and rotation of the mesh transform have to be converted from tracking space to world space.</param>
		/// <param name="convertFromUnityToOpenXR">Specify whether or not the parameters <paramref name="vertexBuffer"/>, <paramref name="indexBuffer"/>, <paramref name="meshPosition"/> and <paramref name="meshOrientation"/> have to be converted to OpenXR coordinate.</param>
		/// <returns>XR_SUCCESS for success.</returns>
		public static bool SetProjectedPassthroughMeshPosition(XrPassthroughHTC passthrough, Vector3 meshPosition, bool trackingToWorldSpace = true, bool convertFromUnityToOpenXR = true)
		{
			bool ret = false;

			if (!AssertFeature())
			{
				ERROR("HTC_Passthrough feature instance not found.");
				return ret;
			}

			Vector3 trackingSpaceMeshPosition = meshPosition;
			TrackingSpaceOrigin currentTrackingSpaceOrigin = TrackingSpaceOrigin.Instance;

			if (currentTrackingSpaceOrigin != null && trackingToWorldSpace) //Apply origin correction to the mesh pose
			{
				Matrix4x4 trackingSpaceOriginTRS = Matrix4x4.TRS(currentTrackingSpaceOrigin.transform.position, Quaternion.identity, Vector3.one);
				Matrix4x4 worldSpaceLayerPoseTRS = Matrix4x4.TRS(meshPosition, Quaternion.identity, Vector3.one);

				Matrix4x4 trackingSpaceLayerPoseTRS = trackingSpaceOriginTRS.inverse * worldSpaceLayerPoseTRS;

				trackingSpaceMeshPosition = trackingSpaceLayerPoseTRS.GetColumn(3); //4th Column of TRS Matrix is the position
			}

#if UNITY_STANDALONE
			if (passthrough2meshTransformInfoPtr.ContainsKey(passthrough))
			{
				XrPassthroughMeshTransformInfoHTC MeshTransformInfo = passthrough2meshTransform[passthrough];
				XrPosef meshXrPose = MeshTransformInfo.pose;
				meshXrPose.position = OpenXRHelper.ToOpenXRVector(trackingSpaceMeshPosition, convertFromUnityToOpenXR); ;
				MeshTransformInfo.pose = meshXrPose;
				passthrough2meshTransform[passthrough] = MeshTransformInfo;
				Marshal.StructureToPtr(MeshTransformInfo, passthrough2meshTransformInfoPtr[passthrough], false);
				XrCompositionLayerPassthroughHTC layer = passthrough2Layer[passthrough];
				layer.next = passthrough2meshTransformInfoPtr[passthrough];
				passthrough2Layer[passthrough] = layer;
				SubmitLayer();
				ret = true;
			}
			else
				ret = false;
#endif

#if UNITY_ANDROID
			ret = passthroughFeature.SetMeshTransformPosition(passthrough, OpenXRHelper.ToOpenXRVector(trackingSpaceMeshPosition, convertFromUnityToOpenXR));
			DEBUG("SetProjectedPassthroughMeshPosition() SetMeshTransformPosition result: " + ret + ", passthrough: " + passthrough);
#endif
			return ret;
		}

		/// <summary>
		/// Modifies the mesh orientation of a projected passthrough layer.
		/// </summary>
		/// <param name="passthrough">The created <see cref="XrPassthroughHTC"/></param>
		/// <param name="meshOrientation">Orientation of the mesh.</param>
		/// <param name="trackingToWorldSpace">Specify whether or not the position and rotation of the mesh transform have to be converted from tracking space to world space.</param>
		/// <param name="convertFromUnityToOpenXR">Specify whether or not the parameters <paramref name="vertexBuffer"/>, <paramref name="indexBuffer"/>, <paramref name="meshPosition"/> and <paramref name="meshOrientation"/> have to be converted to OpenXR coordinate.</param>
		/// <returns>XR_SUCCESS for success.</returns>
		public static bool SetProjectedPassthroughMeshOrientation(XrPassthroughHTC passthrough, Quaternion meshOrientation, bool trackingToWorldSpace = true, bool convertFromUnityToOpenXR = true)
		{
			bool ret = false;

			if (!AssertFeature())
			{
				ERROR("HTC_Passthrough feature instance not found.");
				return ret;
			}

			Quaternion trackingSpaceMeshRotation = meshOrientation;
			TrackingSpaceOrigin currentTrackingSpaceOrigin = TrackingSpaceOrigin.Instance;

			if (currentTrackingSpaceOrigin != null && trackingToWorldSpace) //Apply origin correction to the mesh pose
			{
				Matrix4x4 trackingSpaceOriginTRS = Matrix4x4.TRS(Vector3.zero, currentTrackingSpaceOrigin.transform.rotation, Vector3.one);
				Matrix4x4 worldSpaceLayerPoseTRS = Matrix4x4.TRS(Vector3.zero, meshOrientation, Vector3.one);

				Matrix4x4 trackingSpaceLayerPoseTRS = trackingSpaceOriginTRS.inverse * worldSpaceLayerPoseTRS;

				trackingSpaceMeshRotation = Quaternion.LookRotation(trackingSpaceLayerPoseTRS.GetColumn(2), trackingSpaceLayerPoseTRS.GetColumn(1));
			}

#if UNITY_STANDALONE
			if (passthrough2meshTransformInfoPtr.ContainsKey(passthrough))
			{
				XrPassthroughMeshTransformInfoHTC MeshTransformInfo = passthrough2meshTransform[passthrough];
				XrPosef meshXrPose = MeshTransformInfo.pose;
				meshXrPose.orientation = OpenXRHelper.ToOpenXRQuaternion(trackingSpaceMeshRotation, convertFromUnityToOpenXR);
				MeshTransformInfo.pose = meshXrPose;
				passthrough2meshTransform[passthrough] = MeshTransformInfo;
				Marshal.StructureToPtr(MeshTransformInfo, passthrough2meshTransformInfoPtr[passthrough], false);
				XrCompositionLayerPassthroughHTC layer = passthrough2Layer[passthrough];
				layer.next = passthrough2meshTransformInfoPtr[passthrough];
				passthrough2Layer[passthrough] = layer;
				SubmitLayer();
				ret = true;
			}
			else
				ret = false;
#endif

#if UNITY_ANDROID
			ret = passthroughFeature.SetMeshTransformOrientation(passthrough, OpenXRHelper.ToOpenXRQuaternion(trackingSpaceMeshRotation, convertFromUnityToOpenXR));
			DEBUG("SetProjectedPassthroughMeshOrientation() SetMeshTransformOrientation result: " + ret + ", passthrough: " + passthrough);
#endif
			return ret;
		}

		/// <summary>
		/// Modifies the mesh scale of a passthrough layer.
		/// </summary>
		/// <param name="passthrough">The created <see cref="XrPassthroughHTC"/></param>
		/// <param name="meshScale">Scale of the mesh.</param>
		/// <returns>XR_SUCCESS for success.</returns>
		public static bool SetProjectedPassthroughScale(XrPassthroughHTC passthrough, Vector3 meshScale)
		{
			bool ret = false;

			if (!AssertFeature())
			{
				ERROR("HTC_Passthrough feature instance not found.");
				return ret;
			}

#if UNITY_STANDALONE
			if (passthrough2meshTransformInfoPtr.ContainsKey(passthrough))
			{
				XrPassthroughMeshTransformInfoHTC MeshTransformInfo = passthrough2meshTransform[passthrough];
				MeshTransformInfo.scale = OpenXRHelper.ToOpenXRVector(meshScale, false);
				passthrough2meshTransform[passthrough] = MeshTransformInfo;
				Marshal.StructureToPtr(MeshTransformInfo, passthrough2meshTransformInfoPtr[passthrough], false);
				XrCompositionLayerPassthroughHTC layer = passthrough2Layer[passthrough];
				layer.next = passthrough2meshTransformInfoPtr[passthrough];
				passthrough2Layer[passthrough] = layer;
				SubmitLayer();
				ret = true;
			}
			else
				ret = false;
#endif

#if UNITY_ANDROID
			ret = passthroughFeature.SetMeshTransformScale(passthrough, OpenXRHelper.ToOpenXRVector(meshScale, false));
			DEBUG("SetProjectedPassthroughScale() SetMeshTransformScale result: " + ret + ", passthrough: " + passthrough);
#endif
			return ret;
		}

		/// <summary>
		/// To get the list of IDs of active passthrough layers.
		/// </summary>
		/// <returns>
		/// The a copy of the list of IDs of active passthrough layers.
		/// </returns>
		public static List<XrPassthroughHTC> GetCurrentPassthroughLayerIDs()
		{
			if (!AssertFeature())
			{
				ERROR("HTC_Passthrough feature instance not found.");
				return null;
			}

			return passthroughFeature.PassthroughList;
		}
#endregion
	}
}