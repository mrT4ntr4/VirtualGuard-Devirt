# VirtualGuard Devirtualizer

Devirtualizer for [VirtualGuard Protector](https://virtualguard.io/) using AsmResolver.  

**Supported VMs**  
* Crocodile  
* Spider  

*Refer to the blogpost for more information.*    
https://mrt4ntr4.github.io/VirtualGuard-P2/  

> WIP : It's missing some handlers right now but enough to devirt the crackme.

## Usage

1. Please use [de4dot-vg](https://github.com/mrT4ntr4/de4dot-vg) for deobfuscating the executable first.
```
λ de4dot.exe test-guarded_croc.exe

de4dot v3.1.41592.3405 Copyright (C) 2011-2015 de4dot@gmail.com
Latest version and source code: https://github.com/0xd4d/de4dot

Detected VirtualGuard  (Z:\de4dot-vg\test-guarded_croc.exe)
Cleaning Z:\de4dot-vg\test-guarded_croc.exe
Renaming all obfuscated symbols
Saving Z:\de4dot-vg\test-guarded_croc-cleaned.exe
```
2. Now you are ready to use the Devirt!  
```
λ VirtualGuardDevirt.exe  .\test-guarded_croc-cleaned.exe



                    _.---._     .---.                 / _ \
           __...---' .---. `---'-.   `.             \_\(_)/_/
       .-''__.--' _.'( | )`.  `.  `._ :              _//o\\_ Max
     .'__-'_ .--'' ._`---'_.-.  `.   `-`.             /   \
            ~ -._ -._``---. -.    `-._   `.
                 ~ -.._ _ _ _ ..-_ `.  `-._``--.._
                              -~ -._  `-.  -. `-._``--.._.--''.
         VirtualGuard              ~ ~-.__     -._  `-.__   `. `.
    Crocodile/Spider VM              jgs ~~ ~---...__ _    ._ .` `.
                 Devirt by MrT4ntr4                  ~  ~--.....--~

-------------------------------------------------------------------------

04:47:50 [~] : Module Loaded : 𝙑𝙞𝙧𝙩𝙪𝙖𝙡𝙂𝙪𝙖𝙧𝙙

04:47:50 [~] : VM Type : Crocodile VM
04:47:50 [~] : Found Virtualized method : System.Void VirtualGuard.Tests.Authenticator::.ctor(System.String, VirtualGuard.Tests.Maths) with disasConst : 0x0005386f
04:47:50 [~] : Found Virtualized method : System.Void VirtualGuard.Tests.Authenticator::InputPassword(System.String) with disasConst : 0x0008fdbf
04:47:50 [~] : Found Virtualized method : System.Boolean VirtualGuard.Tests.Authenticator::Validate() with disasConst : 0x0004d446
04:47:50 [~] : Found Virtualized method : System.Void VirtualGuard.Tests.Maths::.ctor(System.Int32, System.Int32) with disasConst : 0x000bf4e5
04:47:50 [~] : Found Virtualized method : System.Int32 VirtualGuard.Tests.Maths::Add() with disasConst : 0x000934e2
04:47:50 [~] : Found Virtualized method : System.Void VirtualGuard.Tests.Program::Main(System.String[]) with disasConst : 0x000301fd
04:47:50 [~] : Found Virtualized method : System.Void VirtualGuard.Tests.Program::.ctor() with disasConst : 0x0004e76c
04:47:50 [Debug] Finding Disassembly Constant
04:47:50 [Debug] Disassembly Constant Found : 0x00077146
04:47:50 [~] : NOPing out VM Init instructions from constructor (System.Void <Module>::.cctor())
04:47:50 [~] : Disassembling Method : System.Void VirtualGuard.Tests.Authenticator::.ctor(System.String, VirtualGuard.Tests.Maths)
04:47:50 [Debug] Disassembling Simple Branch!
04:47:50 [Debug] _currentBlockAddr = 0x0005386f
IL_0000: ldarg.0
IL_0001: stloc V_1
IL_0005: ldloc V_1
IL_0009: call System.Void System.Object::.ctor()
IL_000E: ldarg.0
IL_000F: stloc V_3
IL_0013: ldarg.1
IL_0014: stloc V_4
IL_0018: ldloc V_3
IL_001C: ldloc V_4
IL_0020: stfld System.String VirtualGuard.Tests.Authenticator::_user
IL_0025: ldarg.0
IL_0026: stloc V_5
IL_002A: ldarg.2
IL_002B: stloc V_6
IL_002F: ldloc V_5
IL_0033: ldloc V_6
IL_0037: stfld VirtualGuard.Tests.Maths VirtualGuard.Tests.Authenticator::_math
IL_003C: ret
```


## References

https://github.com/congviet/MemeVMDevirt  
https://github.com/Washi1337/AsmResolver