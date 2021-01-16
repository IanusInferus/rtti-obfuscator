"%LOCALAPPDATA%\Android\sdk\ndk\21.3.6528147\toolchains\llvm\prebuilt\windows-x86_64\bin\clang++.exe" --target=aarch64-linux-androideabi21 "--sysroot=%LOCALAPPDATA%\Android\sdk\ndk\21.3.6528147\toolchains\llvm\prebuilt\windows-x86_64\sysroot" -O2 rtti.cpp -o rtti
..\..\Bin\RttiObfuscator.exe elf obfuscate rtti rtti > map.txt
@pause
