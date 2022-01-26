[logo]: docs/gfx/logo.png "PiWeb Logo"
[classes]: docs/gfx/classdiagram.png "Class diagram"
[multi]: docs/gfx/multi.png "multi"
[previewExtraction]: docs/gfx/previewExtraction.png "PreviewExtraction"
[sliceExtraction]: docs/gfx/sliceExtraction.png "SliceExtraction"
[testingUI]: docs/gfx/testing_ui.jpg "TestingUI"

# PiWeb Volume Library

![PiWeb Logo][logo]

## Overview

- [Introduction](#introduction)
- [Installation](#installation)
- [Usage](#usage)
- [Testing](#testing)
- [License](#license)

## Introduction

The PiWeb Volume Library provides an easy to use interface for reading and especially writing PiWeb volume data. The compression of the volumes is **not lossless** and might distort the original volume. The quality and size of the compressed volume depend on the compression parameters with which the volume has been compressed.

When trying to find the optimal compression parameters, you'll have to make a tradeoff between **quality**, **filesize** and in some measures also **compression time**.

### File Format

![Multidirection][multi]

PiWeb volume files are zip-compressed archives, containing three files:

- **Metadata.xml**: Contains additional information about the size and resolution of the stored volume, as well as user-defined key-value pairs.
- **Compressionoptions.xml**: Contains information about how the volume was compressed and which format the compressed voxel data has.
- **Voxels.dat**: Contains the compressed voxel data.

It's also possible to store multiple directions in one volume file, which triples the size but might significantly reduce the access time for single slices in x- and y-direction when working with compressed volumes.

## Installation

The PiWeb Volume Library is available via [NuGet](https://www.nuget.org/packages/Zeiss.PiWeb.Volume/):

```Batchfile
PM> Install-Package Zeiss.PiWeb.Volume
```

Or compile the library by yourself. Requirements:

- Microsoft Visual Studio 2015
- Microsoft .NET Framework v4.5

## Usage

The PiWeb Volume Library has two main functions. The first is to create compressed volumes, and the second is to read them. While writing volumes is pretty straight forward, reading them offers a bit more space for optimization.

### Writing a volume

#### Create testdata

We need a two-dimensional array of data as input for the PiWeb Volume library. The first dimension is the number of slices, while the second one is the size of one slice. This is due to the fact that arrays in c# are limited to 2^31 elements because it uses a 32 bit integer as indexer. Volume data can easily exceed this size restriction, so we had to make the array multidimensional.

```csharp
ushort sizeX = 128;
ushort sizeY = 128;
ushort sizeZ = 128;

var resolution = 0.1;

var data = new byte[sizeZ][];

for( var z = 0; z < sizeZ; z++ )
    data[ z ] = new byte[sizeX * sizeY];
```

#### Create volume

In this example, we are going to create an uncompressed volume first and then compress it. It's also possible to create a compressed volume from the input data and even use a stream to do so.

```csharp
var metadata = new VolumeMetadata( sizeX, sizeY, sizeZ, resolution, resolution, resolution );
var uncompressedVolume = new UncompressedVolume( metadata, data );
```

#### Compress volume

The compression options in this example are only one of many that FFmpeg supports. Depending on your hardware, you can also use encoders like `nvenc` or `libvpx`. For further information about codec options, please refer to the [FFmpeg codec documentation](www.ffmpeg.org/ffmpeg-codecs.html).

##### Multidirectional data

The compress method has an optional parameter `multidirection`, which will cause the library to store the compressed volume in three axis directions instead of only the z-direction. While this will triple the size of the compressed volume, it can reduce the access time for slices in x- and y-direction significantly when working with a compressed volume.

```csharp
var compressionOptions = new VolumeCompressionOptions( "libopenh264", "yuv420p", null, 1000000 );
var compressedVolume = uncompressedVolume.Compress( compressionOptions );
```

#### Save volume

The compressed volume can be written to a stream using the `Save` method.

```csharp
compressedVolume.Save( outputStream );
```

### Reading a volume

The compressed volume can be loaded from a stream using the static `Volume.Load` method. In case you are actually interested in the large uncompressed data array, you can then use the `Decompress` method to create an uncompressed volume, which has a property `Data`. Most likely, you are only interested in slices of the volume. You have three options to access them:

#### Decompressing the volume

This will write the complete volume into memory. Depending on your hardware, this might easily exceed your available system memory. If not, this is a good option, since it allows for the fastest access.

```csharp
var compressedVolume = Volume.Load( inputStream );
var uncompressedVolume = compressedVolume.Decompress();
```

#### Working with a compressed volume

![Slice extraction][sliceExtraction]

Both, the compressed and the uncompressed volume offer methods to access slices or slice ranges. When using them on a compressed volume, the PiWeb Volume Library will try to perform a fast-forward search for the requested slice index and return only the requested slice. This will supposedly consume much less memory than decompressing the whole volume. When accessing multiple slices, try to batch the reads with the `GetSliceRange` or `GetSliceRanges` methods.

```csharp
var compressedVolume = Volume.Load( inputStream );
var slice = compressedVolume.GetSlice( new VolumeSliceDefinition( Direction.Z, 64 ) );
```

#### Working with a preview

![Preview extraction][previewExtraction]

This option behaves like a mixture between the first two options. The method `compressedVolume.CreatePreview` creates an uncompressed volume that is much smaller than the original volume and can be displayed, while the uncompressed slices are extracted from the compressed volume. This is only an optimization to improve the user experience of your application while retaining the small memory footprint of option two.

```csharp
var compressedVolume = Volume.Load( inputStream );
var preview = compressedVolume.CreatePreview();
```

## Testing

In the solution, you'll find a project named `PiWeb.Volume.UI` which is a small UI for loading and viewing PiWeb Volume files. Following functions are available. This repository contains two small test files in the `testdata` folder. They contain the same volume, but once with multidirectional data and once without. You'll notice the performance difference when viewing them with the UI.

1. Opening files
2. Viewing properties and metadata of the volume
3. Viewing slices of the volume
4. Navigating through the volumes slices
5. Changing the view direction by clicking on sides of the cube
6. Fit and reset the zoom
7. Zoom in and out with the mouse wheel and pan with the right mouse button pressed

![Testing UI][testingUI]

## License

While the PiWeb Volume Library is published under the **BSD 3-Clause "New" or "Revised" License**, it is built on top of [FFmpeg](https://www.ffmpeg.org), which is published under the **LGPL v2.1 License**. While building the FFmpeg binaries, we took care that neither third party libraries with GPL or other copy-left licenses nor commercial libraries were referenced.

Please note, that some famous codecs like `libx264` and `libx265` cannot be used, since they are licensed under GPL.
