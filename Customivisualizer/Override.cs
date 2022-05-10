using Dalamud.Logging;
using System;
using System.Runtime.InteropServices;

namespace Customivisualizer
{
	public abstract class Override
	{
		public static byte[] PtrToByteArray(IntPtr ptr, int length)
		{
			byte[] data = new byte[length];
			Marshal.Copy(ptr, data, 0, length);
			return data;
		}
	}

	public abstract class Override<T> where T : struct
	{
		public bool Dirty { get; set; }

		public T CustomData { get; protected set; }
		public T OriginalData { get; protected set; }

		public abstract int Offset { get; }

		public virtual void Apply(byte[] data)
		{
			CustomData = ByteArrayToStruct(data);
			Dirty = true;
		}

		public virtual void SetOriginal(byte[] data)
		{
			OriginalData = ByteArrayToStruct(data);
			PluginLog.LogDebug($"Original {typeof(T)} data saved");
		}

		protected T ByteArrayToStruct(byte[] data)
		{
			GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
			T structData;
			try
			{
				structData = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
			}
			finally
			{
				handle.Free();
			}
			return structData;
		}
	}
}
