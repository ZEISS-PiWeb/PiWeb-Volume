unzip ./third_party/third_party_sources/FFmpeg-Builds-2022-01-30_12h31.zip -d .
mv ./FFmpeg-Builds-latest ./FFmpeg-Builds
cd ./FFmpeg-Builds
# Delete the original build script
rm ./build.sh
# Copy over our altered build script that uses the local copy of FFmpeg rather than cloning from the web
cp ../build.sh ./build.sh
# Now run that script
./build.sh win64 lgpl-shared 4.4
echo "Building FFmpeg complete."
echo "Find the built FFmpeg (LGPL, Shared) at ./build_ffmpeg/FFmpeg-Builds/artifacts/ffmpeg-<UUID>-win64-lgpl-shared-4.4.zip"