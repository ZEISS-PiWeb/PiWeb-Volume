#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2024                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.Block;

#region usings

using System;
using System.IO;

#endregion

/// <summary>
/// Holds information about a block of encoded values.
/// </summary>
internal readonly record struct BlockInfo( ushort ValueCount, bool IsFirstValueShort, bool AreOtherValuesShort )
{
	#region constants

	private const ushort ValueCountMask = 0b0000111111111111;
	private const ushort IsFirstValueShortMask = 0b0001000000000000;
	private const int IsFirstValueShortOffset = 12;
	private const ushort AreOtherValuesShortMask = 0b0010000000000000;
	private const int AreOtherValuesShortOffset = 13;

	#endregion

	#region properties

	/// <summary>
	/// The length of the block in bytes.
	/// </summary>
	public ushort Length =>
		ValueCount switch
		{
			0 => 0,
			1 => FirstValueSize,
			_ => (ushort)( FirstValueSize + ( ValueCount - 1 ) * ( AreOtherValuesShort ? 2 : 1 ) )
		};

	/// <summary>
	/// The size of the first value in bytes.
	/// </summary>
	public byte FirstValueSize => (byte)( IsFirstValueShort ? 2 : 1 );

	#endregion

	#region methods

	/// <summary>
	/// Reads the <see cref="BlockInfo"/> from the specified <paramref name="reader"/>.
	/// </summary>
	public static BlockInfo Read( BinaryReader reader )
	{
		var resultLength = reader.ReadUInt16();
		var valueCount = resultLength & ValueCountMask;
		var isFirstValueShort = ( resultLength & IsFirstValueShortMask ) >> IsFirstValueShortOffset;
		var areOtherValuesShort = ( resultLength & AreOtherValuesShortMask ) >> AreOtherValuesShortOffset;

		return new BlockInfo( (ushort)valueCount, isFirstValueShort > 0, areOtherValuesShort > 0 );
	}

	/// <summary>
	/// Writes the <see cref="BlockInfo"/> to the specified <paramref name="writer"/>.
	/// </summary>
	public void Write( BinaryWriter writer )
	{
		var result = ValueCount & ValueCountMask;
		if( IsFirstValueShort )
			result |= ( 1 << IsFirstValueShortOffset );
		if( AreOtherValuesShort )
			result |= ( 1 << AreOtherValuesShortOffset );

		writer.Write( (ushort)result );
	}

	/// <summary>
	/// Creates a <see cref="BlockInfo"/> for the specified <paramref name="resultBlock"/>.
	/// </summary>
	public static BlockInfo Create( ReadOnlySpan<short> resultBlock )
	{
		var count = 0;
		var isFirstValueShort = resultBlock[ 0 ] is > sbyte.MaxValue or < sbyte.MinValue;
		var areOtherValuesShort = false;

		for( var i = 0; i < BlockVolume.N3; i++ )
		{
			var value = resultBlock[ i ];
			if( value != 0 )
				count = i + 1;

			if( i > 0 && ( value is > sbyte.MaxValue or < sbyte.MinValue ) )
				areOtherValuesShort = true;
		}

		return new BlockInfo( (ushort)count, isFirstValueShort, areOtherValuesShort );
	}

	#endregion
}