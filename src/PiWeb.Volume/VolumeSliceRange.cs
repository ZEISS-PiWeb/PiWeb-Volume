#region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion


namespace Zeiss.PiWeb.Volume;

#region usings

using System.Collections;
using System.Collections.Generic;

#endregion

/// <summary>
/// Contains a continous range of slices in a specific direction.
/// </summary>
public sealed class VolumeSliceRange : IReadOnlyList<VolumeSlice>
{
	#region members

	private readonly IReadOnlyList<VolumeSlice> _Slices;

	#endregion

	#region constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="VolumeSliceRange"/> class.
	/// </summary>
	/// <param name="definition">The definition.</param>
	/// <param name="slices">The slices.</param>
	internal VolumeSliceRange( VolumeSliceRangeDefinition definition, IReadOnlyList<VolumeSlice> slices )
	{
		Definition = definition;
		_Slices = slices;
	}

	#endregion

	#region properties

	/// <summary>
	/// Gets the definition.
	/// </summary>
	/// <value>
	/// The definition.
	/// </value>
	public VolumeSliceRangeDefinition Definition { get; }

	#endregion

	#region interface IReadOnlyList<VolumeSlice>

	/// <inheritdoc />
	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>
	/// A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
	/// </returns>
	public IEnumerator<VolumeSlice> GetEnumerator()
	{
		return _Slices.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	/// <inheritdoc />
	/// <summary>
	/// Gets the number of elements in the collection.
	/// </summary>
	public int Count => _Slices.Count;

	/// <inheritdoc />
	/// <summary>
	/// Gets the <see cref="T:Zeiss.PiWeb.Volume.VolumeSlice" /> at the specified index.
	/// </summary>
	/// <value>
	/// The <see cref="T:Zeiss.PiWeb.Volume.VolumeSlice" />.
	/// </value>
	/// <param name="index">The index.</param>
	/// <returns></returns>
	public VolumeSlice this[ int index ] => _Slices[ index - Definition.First ];

	#endregion
}