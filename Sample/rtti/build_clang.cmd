wsl clang++ -O2 rtti.cpp -o rtti
..\..\Bin\RttiObfuscator.exe elf obfuscate rtti rtti > map.txt
@pause
