#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.UI.Interfaces;

using System.Diagnostics.CodeAnalysis;

public interface IFileService
{
	#region methods

	bool SelectOpenFileName( [NotNullWhen( true )] out string? fileName );

	bool SelectSaveFileName( [NotNullWhen( true )] out string? fileName );

	#endregion
}