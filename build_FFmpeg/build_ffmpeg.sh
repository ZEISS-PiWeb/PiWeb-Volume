echo "Getting local copy of BtbN/FFmpeg-Builds..."
unzip ./third_party/third_party_sources/FFmpeg-Builds-2022-01-30_12h31.zip -d .
mv ./FFmpeg-Builds-latest ./FFmpeg-Builds
echo "Done."

echo "cd ./FFmpeg-Builds"
cd ./FFmpeg-Builds
echo "Done."

echo "Replacing original build script with altered script that uses the local"
echo "copy of FFmpeg rather than cloning from the web."
rm ./build.sh
cp ../build.sh ./build.sh
echo "Done."

echo "Creating docker container..."
./makeimage.sh win64 lgpl-shared 4.4

echo "Building FFmpeg 4.4 (LGPL, Shared, for Win64)..."
./build.sh win64 lgpl-shared 4.4
echo "Done."
echo "Find the built FFmpeg (LGPL, Shared) at ./build_ffmpeg/FFmpeg-Builds/artifacts/ffmpeg-<UUID>-win64-lgpl-shared-4.4.zip"