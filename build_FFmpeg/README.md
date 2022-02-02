# Build FFmpeg

## Prerequisites

+ WSL or linux installation (tested with Ubuntu 20.04)
+ Docker
+ `zip` and `unzip`

```sh
# in Ubuntu/Debian
sudo apt install zip unzip
```

## Build script


This script is derived from [Btbn/FFmpeg-Builds](https://github.com/BtbN/FFmpeg-Builds).
See `build.sh.patch` for the actual changes made.

By running `build_ffmpeg.sh` the script builds FFmpeg without including any GPL licensed or non-free parts.

The original source is included unter `third_party/third_party_sources/FFmpeg-Builds-2022-01-30_12h31.zip`

After building, the included `.dll`s, `.lib`s and the content of `./src/PiWeb.Volume.Core/include`
can be dynamically replaced with the ones you built yourself.


# License

The attached MIT license applies to `build_FFmpeg/build.sh` and `build_FFmpeg/build_ffmpeg.sh`.
Everything in `build_FFmpeg/third_party/` remains under their respective licenses.

