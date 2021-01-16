# RTTI Obfuscator

We may want to hide internal symbols in binary files built for a C++ project before we ship them, but `strip`/`llvm-strip` command can not strip all internal symbols. The most common internal symbols are RTTI(Run-time type information) symbols used for `typeid` and `dynamic_cast` operators.

RTTI symbols are generated for classes with a virtual table, and sometimes for lambda expressions.

On Linux/Android and in ELF files, they look like `_ZTSSt19_Sp_make_shared_tag` in the static symbol table with a string value like `St19_Sp_make_shared_tag` in ELF files. `_ZTS` refers to typeinfo name, and `_ZTI` refers to typeinfo structure, see [Itanium C++ ABI](https://itanium-cxx-abi.github.io/cxx-abi/abi.html#mangling-special-vtables).

This tool is used to obfuscate these symbols. Currently, the tool only applies to Linux/Android and ELF files.

## Usage

Windows:

    RttiObfuscator.exe elf list <InputFilePath>
    RttiObfuscator.exe elf obfuscate <InputFilePath> <OutputFilePath>

Linux/MacOS:

Install [mono](https://www.mono-project.com/download/stable/) and prefix `mono` in the prior commands.

## Notice

clang will inline string literal into 16-byte bulk copies and intermediate value assignments with -O2, which will embed trailing chars into opcode (when char count is not a multiple of 16). This can be worked around by marking `__attribute__((noinline))` on `std::type_info::name()`.

## License

This software is licensed under 3-Clause BSD, see `LICENSE`.

For `Reference\itanium-base.exp` and `Src\itanium-base.exp`, they come from [libcxxabi](https://github.com/llvm-mirror/libcxxabi) and subject to [Apache License 2.0](https://github.com/llvm-mirror/libcxxabi/blob/master/LICENSE.TXT)

For other contents in `Reference`, they come from third-party and subject to their specific licenses. This software is neither derived work nor combined work of them, their presence is only informational.
