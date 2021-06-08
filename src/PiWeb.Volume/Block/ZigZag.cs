#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume.Block
{
	#region usings

	using System.Collections.Generic;
	using System.Linq;

	#endregion

	internal static class ZigZag
	{
		#region methods

		public static int[] Calculate()
		{
			var entries = new List<ZigZagEntry>();

			var i = 0;
			for( var z = 0; z < BlockVolume.N; z++ )
			for( var y = 0; y < BlockVolume.N; y++ )
			for( var x = 0; x < BlockVolume.N; x++ )
			{
				entries.Add( new ZigZagEntry( x, y, z, i++ ) );
			}

			return entries.OrderBy( e => e.X * e.X + e.Y * e.Y + e.Z * e.Z )
				.ThenBy( e => e.Z )
				.ThenBy( e => e.Y )
				.ThenBy( e => e.X )
				.Select( e => e.Index )
				.ToArray();
		}

		#endregion

		#region struct ZigZagEntry

		private readonly struct ZigZagEntry
		{
			#region constructors

			public ZigZagEntry( int x, int y, int z, int index )
			{
				X = x;
				Y = y;
				Z = z;
				Index = index;
			}

			#endregion

			#region properties

			public int X { get; }
			public int Y { get; }
			public int Z { get; }
			public int Index { get; }

			#endregion
		}

		#endregion
	}
}