# MIRAGE
## Overview & Discord link
<details>
<summary><b>(Click here to load content, 10MB)</b></summary>
  
![overview_1](/overview/overview_1.png)
[![overview_2](/overview/overview_2.png)](https://discord.com/invite/t45bgRnH4c)
  
</details>

## Requirements:
- Base game ([1.05 Gold Edition](https://discord.com/channels/272363082309828610/310790311649607690/446334621106438164)) + BoosterPack 3 ([Tatsukio/BoosterPack3](https://github.com/Tatsukio/BoosterPack3))

## Build Requirements:
- Visual Studio
- NSIS

## Build Order:
- Compile MIRAGE/CfgEditor/CfgEditor.hs (if modified) and move the generated binary file to MIRAGE/GameFiles/Basic/Tools
- Build MIRAGE.sln (which will build and compile each of the subprojects: MIRAGE Launcher, ParaWorldStatus and PWKiller)
- Run MIRAGE/MIRAGE.nsi
- Wait for the new .exe
