# MIRAGE

![overview_1](https://github.com/Tatsukio/MIRAGE/assets/56200767/feaf2511-36f2-4669-be9e-20d290bbf033)
![overview_2](https://github.com/Tatsukio/MIRAGE/assets/56200767/9dc25485-196c-47d0-8b7f-af9348754a81)
[![overview_3](https://github.com/Tatsukio/MIRAGE/assets/56200767/b0e1f9ad-77a6-451d-abe6-b84b81bc04b7)](https://discord.com/invite/t45bgRnH4c)

## Build Requirements: vs, nsis

## Build Order:
- Compile MIRAGE/CfgEditor/CfgEditor.hs and move the generated binary file to MIRAGE/GameFiles/Basic/Tools
- Build MIRAGE.sln (which will build and compile each of the subprojects: MIRAGE Launcher, ParaWorldStatus and PWKiller)
- Run MIRAGE/MIRAGE.nsi
- Wait for the new .exe
