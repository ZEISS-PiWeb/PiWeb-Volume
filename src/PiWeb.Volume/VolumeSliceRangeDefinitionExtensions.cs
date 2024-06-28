#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume
{
	#region usings

	using System.Collections.Generic;
	using System.Linq;

	#endregion

	internal static class VolumeSliceRangeDefinitionExtensions
	{
		#region methods

		internal static IEnumerable<VolumeSliceRangeDefinition> Merge( this IReadOnlyCollection<VolumeSliceRangeDefinition> ranges )
		{
			var groups = ranges.GroupBy( r => r.Direction );

			foreach( var group in groups )
			{
				var ordered = group.OrderBy( r => r.First );

				ushort? first = null;
				ushort? last = null;

				foreach( var range in ordered )
				{
					first ??= range.First;
					last ??= range.Last;

					if( range.First > last.Value + 1 )
					{
						yield return new VolumeSliceRangeDefinition( group.Key, first.Value, last.Value );

						first = range.First;
						last = range.Last;
					}
					else if( range.Last > last.Value )
					{
						last = range.Last;
					}
				}

				if( first.HasValue && last.HasValue )
					yield return new VolumeSliceRangeDefinition( group.Key, first.Value, last.Value );
			}
		}

		#endregion
	}
}