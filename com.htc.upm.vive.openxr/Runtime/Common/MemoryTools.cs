using System;
using System.Runtime.InteropServices;

namespace VIVE.OpenXR
{
	public static class MemoryTools
	{
        /// <summary>
        /// Convert the enum array to IntPtr.  Should call <see cref="ReleaseRawMemory(IntPtr)"/> after use.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public static IntPtr ToIntPtr<T>(T[] array) where T : Enum
		{
			int size = Marshal.SizeOf(typeof(T)) * array.Length;
			IntPtr ptr = Marshal.AllocHGlobal(size);
			int[] intArray = new int[array.Length];
			for (int i = 0; i < array.Length; i++)
				intArray[i] = (int)(object)array[i];
			Marshal.Copy(intArray, 0, ptr, array.Length);
			return ptr;
		}

        /// <summary>
        /// Make the same size raw buffer from input array.
        /// </summary>
        /// <typeparam name="T">Data type could be primitive type or struct. Should call <see cref="ReleaseRawMemory(IntPtr)"/> after use.</typeparam>
        /// <param name="refArray">The data array</param>
        /// <returns>The memory handle.  Should release by <see cref="ReleaseRawMemory(IntPtr)"/></returns>
        public static IntPtr MakeRawMemory<T>(T[] refArray)
		{
			int size = Marshal.SizeOf(typeof(T)) * refArray.Length;
			return Marshal.AllocHGlobal(size);
		}

		/// <summary>
		/// Copy the raw memory to the array.  You should make sure the array has the same size as the raw memory.
		/// </summary>
		/// <typeparam name="T">Convert the memory to this type array.</typeparam>
		/// <param name="array">The output array.</param>
		/// <param name="raw">The data source in raw memory form.</param>
		/// <param name="count">Specify the copy count.  Count should be less than array length.</param>
		public static void CopyFromRawMemory<T>(T[] array, IntPtr raw, int count = 0)
		{
			int N = array.Length;
			if (count > 0 && count < array.Length)
				N = count;
			int step = Marshal.SizeOf(typeof(T));
			for (int i = 0; i < N; i++)
			{
				array[i] = Marshal.PtrToStructure<T>(IntPtr.Add(raw, i * step));
			}
		}

        /// <summary>
        /// Make the same size raw buffer from input array.  Make sure the raw has enough size.
        /// </summary>
        /// <typeparam name="T">Convert this type array to raw memory.</typeparam>
        /// <param name="raw">The output data in raw memory form</param>
        /// <param name="array">The data source</param>
        public static void CopyToRawMemory<T>(IntPtr raw, T[] array)
		{
			int step = Marshal.SizeOf(typeof(T));
			for (int i = 0; i < array.Length; i++)
			{
				Marshal.StructureToPtr<T>(array[i], IntPtr.Add(raw, i * step), false);
			}
		}

        /// <summary>
        /// Release the raw memory handle which is created by <see cref="MakeRawMemory{T}(T[])"/>
        /// </summary>
        /// <param name="ptr"></param>
        public static void ReleaseRawMemory(IntPtr ptr)
		{
			Marshal.FreeHGlobal(ptr);
		}
	}
}