# MIRAGE
Requirements: vs, nsis


Build order:
1. Compile MIRAGE/CfgEditor/CfgEditor.hs and move it to MIRAGE/GameFiles/Basic/Tools
2. Compile MIRAGE/Launcher/MIRAGE Launcher.sln
3. Compile MIRAGE/ParaWorldStatus/ParaWorldStatus.sln
4. Compile MIRAGE/PWKiller/PWKiller.sln
5. Run MIRAGE/MIRAGE.nsi
6. Wait for the new .exe
