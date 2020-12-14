#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume.UI.Model
{
	public readonly struct Layer
	{
		public Layer( byte[] data, int width, int height, int index ) : this()
		{
			Data = data;
			Width = width;
			Height = height;
			Index = index;
		}

		public byte[] Data { get; }
		public int Index { get; }
		public int Width { get; }
		public int Height { get; }
	}
}