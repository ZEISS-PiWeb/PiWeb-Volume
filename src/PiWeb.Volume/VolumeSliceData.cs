#region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume
{
	#region usings

	using System;
	using System.Buffers;

	#endregion

	public readonly struct VolumeSliceData : IDisposable
	{
		#region members
		
		private readonly bool _ReturnToPool;

		#endregion
		
		#region constructors

		public VolumeSliceData( byte[] data, int length, bool returnToPool )
		{
			_ReturnToPool = returnToPool;
			Data = new ArraySegment<byte>( data, 0, length );
		}

		#endregion

		#region properties

		public ArraySegment<byte> Data { get; }

		#endregion

		#region interface IDisposable

		/// <inheritdoc />
		public void Dispose()
		{
			if( _ReturnToPool )
				VolumeArrayPool.Shared.Return( Data.Array );
		}

		#endregion
	}
}