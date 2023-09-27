@echo off
setlocal EnableExtensions EnableDelayedExpansion
set /a parseCount=0
set /a cfgFilesToParse=0
set /a ttFilesToParse=0
set errorlevel=
echo Calculating pw cfg files in directory...
chdir /d "c:\Program Files (x86)\Sunflowers\ParaWorld\"
break>"Tools\parse_log.txt"
for /r %%i in (*.txt, *.cfg) do (
	set /a cfgFilesToParse+=1
)
for /r %%i in (*.ttree) do (
	set /a ttFilesToParse+=1
)
echo ...done.

set /a totalFilesToParse=cfgFilesToParse+ttFilesToParse
set /a filesLeft=totalFilesToParse

echo Wait for the cfgeditor to parse through all pw cfg files
echo Cfg files to parse: !cfgFilesToParse!
echo TTree files to parse: !ttFilesToParse!

echo Parsing pw cfg files (.txt --- .cfg --- .ttree)...
for /r %%i in (*.txt, *.cfg, *.ttree) do (
	
	set /a parseCount+=1
	set /a filesLeft=filesLeft-1
	
	set /A percent=parseCount*100/totalFilesToParse
	title Files left: !filesLeft!, progress: !percent!%%
	
	echo Parse file #!parseCount!: "%%i"
	echo Parse file #!parseCount!: "%%i" >> Tools\parse_log.txt 2>&1
	echo Y|start /w /b "" "Tools\CfgEditor.exe" "%%i" >> Tools\parse_log.txt 2>&1
	
)
echo ...done, see parse_log.txt
pause