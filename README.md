# MIRAGE
## Overview & Discord link
<details>
<summary><b>(Click here to load content, 10MB)</b></summary>
  
![overview_1](/overview/overview_1.png)
![overview_2](/overview/overview_2.png)
[![overview_3](/overview/overview_3.png)](https://discord.com/invite/t45bgRnH4c)
  
</details>


## Build Requirements: vs, nsis

## Build Order:
- Compile MIRAGE/CfgEditor/CfgEditor.hs and move the generated binary file to MIRAGE/GameFiles/Basic/Tools
- Build MIRAGE.sln (which will build and compile each of the subprojects: MIRAGE Launcher, ParaWorldStatus and PWKiller)
- Run MIRAGE/MIRAGE.nsi
- Wait for the new .exe
