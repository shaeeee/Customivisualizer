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
		public event EventHandler? DataChanged;

		public T CustomData { get; protected set; }
		public T OriginalData { get; protected set; }

		public virtual void Apply(byte[] data)
		{
			CustomData = ByteArrayToStruct(data);
			OnDataChanged();
		}

		public virtual void SetOriginal(byte[] data)
		{
			OriginalData = ByteArrayToStruct(data);
			PluginLog.LogDebug("Original data saved");
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

		public void ManualInvokeDataChanged()
		{
			OnDataChanged();
		}

		protected virtual void OnDataChanged()
		{
			DataChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
