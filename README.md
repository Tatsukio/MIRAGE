# MIRAGE

![image](https://github.com/Tatsukio/MIRAGE/assets/56200767/b5bf72b5-cdf3-4454-8543-2d4b6ffe6ddb)


## Build Requirements: vs, nsis

## Build Order:
- Compile MIRAGE/CfgEditor/CfgEditor.hs and move the generated binary file to MIRAGE/GameFiles/Basic/Tools
- Build MIRAGE.sln (which will build and compile each of the subprojects: MIRAGE Launcher, ParaWorldStatus and PWKiller)
- Run MIRAGE/MIRAGE.nsi
- Wait for the new .exe
