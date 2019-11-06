#region copyright

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
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;

    #endregion

    /// <summary>
    /// A collection of volume slices which provides accessibility to single slices and ranges and avoids duplicates.
    /// </summary>
    public sealed class VolumeSliceCollection
    {
        #region members

        private readonly Dictionary<ushort, VolumeSlice> _SlicesX;
        private readonly Dictionary<ushort, VolumeSlice> _SlicesY;
        private readonly Dictionary<ushort, VolumeSlice> _SlicesZ;

        #endregion

        #region constructors

        internal VolumeSliceCollection()
        {
            _SlicesX = new Dictionary<ushort, VolumeSlice>();
            _SlicesY = new Dictionary<ushort, VolumeSlice>();
            _SlicesZ = new Dictionary<ushort, VolumeSlice>();
        }

        internal VolumeSliceCollection( Dictionary<ushort, VolumeSlice> slicesX, Dictionary<ushort, VolumeSlice> slicesY, Dictionary<ushort, VolumeSlice> slicesZ )
        {
            _SlicesX = slicesX;
            _SlicesY = slicesY;
            _SlicesZ = slicesZ;
        }

        internal VolumeSliceCollection( IEnumerable<VolumeSliceRange> sliceRanges )
        {
            _SlicesX = new Dictionary<ushort, VolumeSlice>();
            _SlicesY = new Dictionary<ushort, VolumeSlice>();
            _SlicesZ = new Dictionary<ushort, VolumeSlice>();

            foreach( var range in sliceRanges )
            {
                var slices = GetSlices( range.Definition.Direction );

                foreach( var slice in range )
                    slices[ slice.Index ] = slice;
            }
        }

        #endregion

        #region methods

        /// <summary>
        /// Adds the slice to the collection in case it is not already present.
        /// </summary>
        /// <param name="slice"></param>
        public void Add( VolumeSlice slice )
        {
            var collection = GetSlices( slice.Direction );
            if( collection.ContainsKey( slice.Index ) )
                return;

            collection[ slice.Index ] = slice;
        }

        /// <summary>
        /// Adds all slices from the specified range to the collection.
        /// Slices that are already present in the collection are skipped.
        /// </summary>
        /// <param name="range"></param>
        public void Add( VolumeSliceRange range )
        {
            var collection = GetSlices( range.Definition.Direction );

            for( var index = range.Definition.First; index <= range.Definition.Last; index++ )
            {
                if( collection.ContainsKey( index ) )
                    return;

                collection[ index ] = range[ index ];
            }
        }

        /// <summary>
        /// Adds all slices of the other collection into this one.
        /// Slices that are already present in the collection are skipped.
        /// </summary>
        /// <param name="other"></param>
        public void Add( VolumeSliceCollection other )
        {
            foreach( var slice in other._SlicesX )
                if( !_SlicesX.ContainsKey( slice.Key ) )
                    _SlicesX[ slice.Key ] = slice.Value;

            foreach( var slice in other._SlicesY )
                if( !_SlicesY.ContainsKey( slice.Key ) )
                    _SlicesY[ slice.Key ] = slice.Value;

            foreach( var slice in other._SlicesZ )
                if( !_SlicesZ.ContainsKey( slice.Key ) )
                    _SlicesZ[ slice.Key ] = slice.Value;
        }

        /// <summary>
        /// Determines whether the specified slice is present in the collection
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool Contains( Direction direction, ushort index )
        {
            return GetSlices( direction ).ContainsKey( index );
        }

        /// <summary>
        /// Determines whether the specified range is present in the collection
        /// </summary>
        /// <param name="range">Slice range</param>
        /// <returns></returns>
        public bool Contains( VolumeSliceRangeDefinition range )
        {
            var set = GetSlices( range.Direction );
            for( var i = range.First; i <= range.Last; i++ )
            {
                if( !set.ContainsKey( i ) )
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the specified slice ranges
        /// </summary>
        /// <param name="definitions"></param>
        /// <exception cref="ArgumentOutOfRangeException">The collection is missing one or more of the requested slices.</exception>
        /// <returns></returns>
        public IReadOnlyCollection<VolumeSliceRange> GetSliceRanges( IReadOnlyCollection<VolumeSliceRangeDefinition> definitions )
        {
            return definitions.Select( GetSliceRange ).ToArray();
        }

        /// <summary>
        /// Returns the specified slice range
        /// </summary>
        /// <param name="definition"></param>
        /// <exception cref="ArgumentOutOfRangeException">The collection is missing one or more of the requested slices.</exception>
        /// <returns></returns>
        public VolumeSliceRange GetSliceRange( VolumeSliceRangeDefinition definition )
        {
            var slices = new List<VolumeSlice>();
            var set = GetSlices( definition.Direction );
            for( ushort s = definition.First; s <= definition.Last; s++ )
            {
                if( set.TryGetValue( s, out var data ) )
                    slices.Add( data );
                else
                    throw new ArgumentOutOfRangeException( nameof(definition) );
            }

            return new VolumeSliceRange( definition, slices );
        }

        /// <summary>
        /// Returns the specified slice
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="index"></param>
        /// <exception cref="ArgumentOutOfRangeException">The collection does not contain the requested slice.</exception>
        /// <returns></returns>
        public VolumeSlice GetSlice( Direction direction, ushort index )
        {
            var set = GetSlices( direction );
            if( set.TryGetValue( index, out var data ) )
                return data;

            throw new ArgumentOutOfRangeException( nameof(index) );
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        private Dictionary<ushort, VolumeSlice> GetSlices( Direction direction )
        {
            switch( direction )
            {
                case Direction.X:
                    return _SlicesX;
                case Direction.Y:
                    return _SlicesY;
                case Direction.Z:
                    return _SlicesZ;
                default:
                    throw new ArgumentOutOfRangeException( nameof(direction), direction, null );
            }
        }

        #endregion
    }
}