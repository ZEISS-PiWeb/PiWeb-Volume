#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.UI.Model
{
	public readonly struct Layer
	{
		#region constructors

		public Layer( byte[] data, int width, int height, int index ) : this()
		{
			Data = data;
			Width = width;
			Height = height;
			Index = index;
		}

		#endregion

		#region properties

		public byte[] Data { get; }
		public int Index { get; }
		public int Width { get; }
		public int Height { get; }

		#endregion
	}
}