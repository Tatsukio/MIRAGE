;NSIS Modern User Interface

;Include Modern UI
Unicode True

!include "MUI2.nsh"
!include "Sections.nsh"
!include "nsDialogs.nsh"
!include "LogicLib.nsh"
!include "MUI.nsh"

;--------------------------------
;General

SetCompressor /SOLID /FINAL lzma
;SetCompressor /SOLID /FINAL zlib
!define VERSION_NUM "2.6.7"
!define BUILD_NUM "13"
!define WEBSITE_NUM "20"
!define MOD_NAME "MIRAGE"
Var GraphicsChoiceWindow
Var CBoxCheats
Var CBoxCheats_State
Var CCheats_Default
Var CBoxPresets
Var CBoxPresets_State
Var CPresets_Default

	;Name and file
	Name "${MOD_NAME} ${VERSION_NUM} build ${BUILD_NUM}"
	!insertmacro MUI_DEFAULT MUI_ICON "installericon.ico"
	OutFile "${MOD_NAME}_${VERSION_NUM}_build_${BUILD_NUM}.exe"

	;Get installation folder from registry if available
	InstallDirRegKey HKLM "SOFTWARE\${MOD_NAME}" ""

	;Request application privileges for Windows Vista
	RequestExecutionLevel admin

	;Default installation folder
	InstallDir $INSTDIR
;--------------------------------
;Interface Settings

	!define MUI_ABORTWARNING

	;Show all languages, despite user's codepage
	!define MUI_LANGDLL_ALLLANGUAGES

;--------------------------------
;Language Selection Dialog Settings

	;Remember the installer language
	!define MUI_LANGDLL_REGISTRY_ROOT "HKLM" 
	!define MUI_LANGDLL_REGISTRY_KEY "SOFTWARE\${MOD_NAME}" 
	!define MUI_LANGDLL_REGISTRY_VALUENAME "installer_language"
	!define MUI_FINISHPAGE_SHOWREADME
	!define MUI_FINISHPAGE_SHOWREADME_FUNCTION OpenReadMe

;--------------------------------
;Pages

	!insertmacro MUI_PAGE_COMPONENTS
	Page custom nsDialogsPage nsDialogsPageLeave
	!insertmacro MUI_PAGE_DIRECTORY
	!insertmacro MUI_PAGE_INSTFILES
	!insertmacro MUI_PAGE_FINISH
	
	!insertmacro MUI_UNPAGE_CONFIRM
	!insertmacro MUI_UNPAGE_INSTFILES
	
;--------------------------------
;Languages
 
	!insertmacro MUI_LANGUAGE "English"
	!insertmacro MUI_LANGUAGE "French"
	!insertmacro MUI_LANGUAGE "German"
	!insertmacro MUI_LANGUAGE "Hungarian"
	!insertmacro MUI_LANGUAGE "Italian"
	!insertmacro MUI_LANGUAGE "Polish"
	!insertmacro MUI_LANGUAGE "Russian"
	!insertmacro MUI_LANGUAGE "SimpChinese"
	
;--------------------------------
;Reserve Files
	
	;If you are using solid compression, files that are required before
	;the actual installation should be stored first in the data block,
	;because this will make your installer start faster.
	
	!insertmacro MUI_RESERVEFILE_LANGDLL

;--------------------------------

Function .onInit
	!insertmacro MUI_LANGDLL_DISPLAY
	InitPluginsDir
	File /oname=$PLUGINSDIR\splash.bmp "Splash.bmp"
	splash::show 5091 $PLUGINSDIR\splash
	;File /oname=$PLUGINSDIR\maintheme_reduced.wav "maintheme_reduced.wav"
	;StrCpy $0 "$PLUGINSDIR\maintheme_reduced.wav"
	;IntOp $1 "SND_ASYNC" || 1
	;System::Call 'winmm::PlaySound(t r0, i 0, i r1) b'
	ClearErrors
	IfFileExists "$EXEDIR\dev.exe" normal
	
	ReadRegStr $INSTDIR HKLM "SOFTWARE\${MOD_NAME}" "InstDir"
	IfErrors trybp3
	Goto nopwend
	
	trybp3:
	ReadRegStr $INSTDIR HKLM "SOFTWARE\BoosterPack" "InstDir"
	IfErrors tryoriginal
	Goto nopwend
	
	tryoriginal:
	ReadRegStr $INSTDIR HKCU "Software\Sunflowers\ParaWorld" "InstallDir"
	IfErrors tryagain
	Goto nopwend
	
	tryagain:
	ReadRegStr $INSTDIR HKLM "SOFTWARE\Sunflowers\ParaWorld" "InstallDir"
	IfErrors nopw
	Goto nopwend
	
	nopw:
	;MessageBox MB_OK|MB_ICONEXCLAMATION "$(INIT_MESSAGE_NO_PATH)"
	;Useless
	StrCpy $INSTDIR "$(INIT_MESSAGE_INSERT_PATH)"
	nopwend:
	ClearErrors
	ReadRegStr $0 HKCU "Software\www.para-welt.com team\MIRAGE" "Path"
	IfErrors no_prev
	Goto prev_found
	no_prev:
	ReadRegStr $0 HKLM "SOFTWARE\${MOD_NAME}" "InstDir"
	IfErrors normal
	; TODO: When user tries to install it in a folder its already in, warn user that files will be deleted
	;			Also, tell him that there is already a version installed + the path
	MessageBox MB_YESNO|MB_ICONQUESTION "$(INIT_MESSAGE_REMOVE_FIRST)" IDYES qy IDNO qn
	qy:
	IfFileExists "$0\bin\cheats.txt" cheatexist
	Goto cn
	cheatexist:
	; MessageBox MB_YESNO|MB_ICONQUESTION "$(INIT_MESSAGE_CHEATS)" IDYES cy IDNO cn
	; cy:
	CopyFiles "$0\bin\cheats.txt" "$0"
	StrCpy $CCheats_Default "1"
	Goto ccont
	cn:
	StrCpy $CCheats_Default "2"
	ccont:
	IfFileExists "$0\Data\MIRAGE\Presets.txt" itis
	Goto tn
	itis:
	; MessageBox MB_YESNO|MB_ICONQUESTION "$(INIT_MESSAGE_PRESETS)" IDYES ty IDNO tn
	; ty:
	CopyFiles "$0\Data\MIRAGE\Presets.txt" "$0"
	StrCpy $CPresets_Default "1"
	Goto pcont
	tn:
	StrCpy $CPresets_Default "2"
	pcont:
	RMDir /r "$0\Data\${MOD_NAME}"
	Delete "$0\Data\Info\${MOD_NAME}.info"
	Delete "$0\Uninstall ${MOD_NAME}.exe"
	Delete "$DESKTOP\${MOD_NAME} Launcher.lnk"
	DeleteRegKey HKLM "SOFTWARE\${MOD_NAME}"
	RMDir /r "$SMPROGRAMS\Sunflowers\ParaWorld\${MOD_NAME}"
	Goto normal
	prev_found:
	ReadRegStr $8 HKLM "SOFTWARE\${MOD_NAME}" "product_code"
	MessageBox MB_YESNO|MB_ICONEXCLAMATION "$(INIT_MESSAGE_CONFLICT)" IDYES pfqy IDNO pfqn
	pfqy:
	ExecWait '"$INSTDIR\Uninstall ${MOD_NAME}.exe" /x$8' $1
	${If} $1 = 0
		Goto normal
	${Elseif} $1 = 1614
		Goto normal
	${Endif}
	pfqn:
	qn:
	Quit
	normal:
	ClearErrors
FunctionEnd

Function nsDialogsPage
	!insertmacro MUI_HEADER_TEXT $(CUST_COMP_PAGE_TITLE) $(CUST_COMP_PAGE_SUBTITLE)
	nsDialogs::Create 1018
	Pop $GraphicsChoiceWindow

	${If} $GraphicsChoiceWindow == error
		Abort
	${EndIf}

	${NSD_CreateCheckbox} 0 0u 100% 20u $(CB_CHEATS_DESC)
	Pop $CBoxCheats
	${NSD_SetState} $CBoxCheats $CBoxCheats_State
	${If} $CCheats_Default == "1"
		${NSD_Check} $CBoxCheats
	${ElseIf} $CCheats_Default == "2"
		${NSD_AddStyle} $CBoxCheats ${WS_DISABLED}
	${EndIf}
	
	${NSD_CreateCheckbox} 0 25u 100% 20u $(CB_PRESETS_DESC)
	Pop $CBoxPresets
	${NSD_SetState} $CBoxPresets $CBoxPresets_State
	${If} $CPresets_Default == "1"
		${NSD_Check} $CBoxPresets
	${ElseIf} $CPresets_Default == "2"
		${NSD_AddStyle} $CBoxPresets ${WS_DISABLED}
	${EndIf}

	nsDialogs::Show
	
FunctionEnd

Function nsDialogsPageLeave

	${NSD_GetState} $CBoxCheats $CBoxCheats_State
	${NSD_GetState} $CBoxPresets $CBoxPResets_State
	
	${If} $CCheats_Default != "2"
		StrCpy $CCheats_Default "0"
	${EndIf}
	
	${If} $CPresets_Default != "2"
		StrCpy $CPresets_Default "0"
	${EndIf}
	
FunctionEnd

Function CheckPresets

	IfFileExists "$INSTDIR\cheats.txt" cey
	Goto cen
	cey:
	${If} $CBoxCheats_State == ${BST_CHECKED}
		CopyFiles "$INSTDIR\cheats.txt" "$INSTDIR\bin"
	${EndIf}
	Delete "$INSTDIR\cheats.txt"
	cen:
	IfFileExists "$INSTDIR\Presets.txt" tey
	Goto ten
	tey:
	${If} $CBoxPresets_State == ${BST_CHECKED}
		CopyFiles "$INSTDIR\Presets.txt" "$INSTDIR\Data\${MOD_NAME}"
	${EndIf}
	Delete "$INSTDIR\Presets.txt"
	ten:
	SetOutPath "$INSTDIR"
	
	ClearErrors
	
FunctionEnd

;--------------------------------
;Installer Sections

Section "${MOD_NAME}" SecSBasic

	SectionIn RO

	SetOutPath "$INSTDIR"

	;ADD YOUR OWN FILES HERE...
	File /r "GameFiles\Basic\"
	File /r "GameFiles\Optional\Mappack\"
	
	SetOutPath "$INSTDIR\Data\${MOD_NAME}"
	
	;Store installation folder
	IfFileExists "$EXEDIR\dev.exe" devbuild
	WriteRegStr HKLM "SOFTWARE\${MOD_NAME}" "InstDir" $INSTDIR
	WriteRegStr HKLM "SOFTWARE\${MOD_NAME}" "website_version" ${WEBSITE_NUM}
	WriteRegStr HKLM "SOFTWARE\${MOD_NAME}" "product_version" ${VERSION_NUM}
	CreateShortCut "$DESKTOP\${MOD_NAME} Launcher.lnk" "$INSTDIR\Tools\MIRAGE Launcher\${MOD_NAME} Launcher.exe" "" "$INSTDIR\Tools\MIRAGE Launcher\modicon.ico" "" "" "" "$(DESC_DESKTOP_LNK)"

	SetOutPath "$INSTDIR"
	WriteUninstaller "$INSTDIR\Uninstall ${MOD_NAME}.exe"
	Goto relbuild
	
	devbuild:
	CreateShortCut "$DESKTOP\${MOD_NAME} dev.lnk" "$INSTDIR\Tools\MIRAGE Launcher\${MOD_NAME} Launcher.exe" "" "$INSTDIR\Tools\MIRAGE Launcher\modicon.ico" "" "" "" "$(DESC_DESKTOP_LNK)"

	SetOutPath "$INSTDIR"
	WriteUninstaller "$INSTDIR\Uninstall ${MOD_NAME}.exe"
	
	relbuild:
	Call CheckPresets
SectionEnd


Section $(ENTRY_DISCORDRPC) SecSDiscordRPC

	SetOutPath "$INSTDIR\"
	File /r "GameFiles\Optional\DiscordRPC\"
	
SectionEnd

;--------------------------------
;Descriptions

	;Language strings
	LangString DESC_SecSBasic ${LANG_ENGLISH} "The basic game files, which are required for playing."
	LangString DESC_SecSDiscordRPC ${LANG_ENGLISH} "Install discord integration (Rich Presence)"
	LangString ENTRY_DISCORDRPC ${LANG_ENGLISH} "Discord integration"
	LangString CUST_COMP_PAGE_TITLE ${LANG_ENGLISH} "Additional install choices"
	LangString CUST_COMP_PAGE_SUBTITLE ${LANG_ENGLISH} "Choose which graphical components you would like to use in your game."
	LangString CB_BP_DESC ${LANG_ENGLISH} "Install BoosterPack 3.0"
	LangString CB_MAPPACK_DESC ${LANG_ENGLISH} "Install MapPack 7.0"
	LangString CB_DISCORD_DESC ${LANG_ENGLISH} "Install Discord integration"
	LangString INIT_MESSAGE_NO_PATH ${LANG_ENGLISH} "The installer could not detect an installation of ParaWorld. You have to set the ParaWorld-path yourself."
	LangString INIT_MESSAGE_INSERT_PATH ${LANG_ENGLISH} "[insert ParaWorld directory here]"
	LangString INIT_MESSAGE_REMOVE_FIRST ${LANG_ENGLISH} "It seems another version of MIRAGE is already installed, which must be removed first. Would you like to remove it now?"
	LangString INIT_MESSAGE_PRESETS ${LANG_ENGLISH} "Do you want to keep your army-sets?"
	LangString INIT_MESSAGE_CONFLICT ${LANG_ENGLISH} "It seems another version of MIRAGE is already installed, which must be removed first. Would you like to remove it now? (If the uninstallation doesn't start, you need to do it manually first, and then start this installer again!)"
	LangString DESC_DESKTOP_LNK ${LANG_ENGLISH} "Start MIRAGE application"
	LangString INIT_MESSAGE_CHEATS ${LANG_ENGLISH} "Do you want to keep your cheats file?"
	LangString CB_CHEATS_DESC ${LANG_ENGLISH} "Keep your own cheats file"
	LangString CB_PRESETS_DESC ${LANG_ENGLISH} "Keep your army-sets"
	
	LangString DESC_SecSBasic ${LANG_FRENCH} "Les fichiers de jeu de base, qui sont nécessaires pour jouer."
	LangString DESC_SecSDiscordRPC ${LANG_FRENCH} "Install discord integration (Rich Presence)"
	LangString ENTRY_DISCORDRPC ${LANG_FRENCH} "Discord integration"
	LangString CUST_COMP_PAGE_TITLE ${LANG_FRENCH} "Options d'installation supplémentaires"
	LangString CUST_COMP_PAGE_SUBTITLE ${LANG_FRENCH} "Choisissez les composants graphiques que vous souhaitez utiliser dans votre jeu."
	LangString CB_BP_DESC ${LANG_FRENCH} "Install BoosterPack 3.0"
	LangString CB_MAPPACK_DESC ${LANG_FRENCH} "Install MapPack 7.0"
	LangString CB_DISCORD_DESC ${LANG_FRENCH} "Install Discord integration"
	LangString INIT_MESSAGE_NO_PATH ${LANG_FRENCH} "Le programme d'installation n'a pas pu détecter une installation de ParaWorld. Vous devez définir vous-meme le chemin ParaWorld."
	LangString INIT_MESSAGE_INSERT_PATH ${LANG_FRENCH} "[Insérer le répertoire ParaWorld ici]"
	LangString INIT_MESSAGE_REMOVE_FIRST ${LANG_FRENCH} "Il semble qu'une autre version de MIRAGE soit déja installée, qui doit d'abord etre supprimée. Voulez-vous la supprimer maintenant?"
	LangString INIT_MESSAGE_PRESETS ${LANG_FRENCH} "Veux-tu garder tes troupes?"
	LangString INIT_MESSAGE_CONFLICT ${LANG_FRENCH} "Il semble qu'une autre version de MIRAGE soit déja installée, qui doit d'abord etre supprimée. Voulez-vous la supprimer maintenant? (Si la désinstallation ne démarre pas, vous devez d'abord la faire manuellement, puis recommencer le programme d'installation!)"
	LangString DESC_DESKTOP_LNK ${LANG_FRENCH} "Démarrer l'application MIRAGE"
	LangString INIT_MESSAGE_CHEATS ${LANG_FRENCH} "Voulez-vous conserver votre propre fichier de tricheurs?"
	LangString CB_CHEATS_DESC ${LANG_FRENCH} "Conserver votre propre fichier de tricheurs"
	LangString CB_PRESETS_DESC ${LANG_FRENCH} "Garder tes troupes"
	
	LangString DESC_SecSBasic ${LANG_GERMAN} "Die Grunddaten des Spiels, die zum Spielen benötigt werden."
	LangString DESC_SecSDiscordRPC ${LANG_GERMAN} "Install discord integration (Rich Presence)"
	LangString ENTRY_DISCORDRPC ${LANG_GERMAN} "Discord integration"
	LangString CUST_COMP_PAGE_TITLE ${LANG_GERMAN} "Weitere Installationsoptionen"
	LangString CUST_COMP_PAGE_SUBTITLE ${LANG_GERMAN} "Wähle welche grafischen Elemente Du im Spiel benutzen willst."
	LangString CB_BP_DESC ${LANG_GERMAN} "Install BoosterPack 3.0"
	LangString CB_MAPPACK_DESC ${LANG_GERMAN} "Install MapPack 7.0"
	LangString CB_DISCORD_DESC ${LANG_GERMAN} "Install Discord integration"
	LangString INIT_MESSAGE_NO_PATH ${LANG_GERMAN} "Der Installer konnte keine ParaWorld-Installation finden. Du musst den ParaWorld-Pfad selber setzen."
	LangString INIT_MESSAGE_INSERT_PATH ${LANG_GERMAN} "[trage das ParaWorld-Verzeichnis hier ein]"
	LangString INIT_MESSAGE_REMOVE_FIRST ${LANG_GERMAN} "Es scheint, dass eine andere Version von MIRAGE installiert ist, diese muss zuerst entfernt werden. Möchtest Du diese jetzt entfernen?"
	LangString INIT_MESSAGE_PRESETS ${LANG_GERMAN} "Willst Du Deine Armeezusammenstellungen behalten?"
	LangString INIT_MESSAGE_CONFLICT ${LANG_GERMAN} "Anscheinend ist eine andere Version von MIRAGE bereits installiert, welche zuerst entfernt werden muss. Möchten Sie die veraltete Version deinstallieren? (Sollte die Deinstallation nicht starten, müssen Sie zuerst MIRAGE manuell deinstallieren und dann diesen Installer neu starten.)"
	LangString DESC_DESKTOP_LNK ${LANG_GERMAN} "Starten Sie die MIRAGE Anwendung"
	LangString INIT_MESSAGE_CHEATS ${LANG_GERMAN} "Möchtest du deine eigene Cheats-Datei behalten?"
	LangString CB_CHEATS_DESC ${LANG_GERMAN} "Eigene Cheats-Datei behalten"
	LangString CB_PRESETS_DESC ${LANG_GERMAN} "Eigene Armeezusammenstellungen behalten"
	
	LangString DESC_SecSBasic ${LANG_HUNGARIAN} "A mód alapvető fájljai, amelyek nélkülözhetetlenek a működéséhez."
	LangString DESC_SecSDiscordRPC ${LANG_HUNGARIAN} "Install discord integration (Rich Presence)"
	LangString ENTRY_DISCORDRPC ${LANG_HUNGARIAN} "Discord integration"
	LangString CUST_COMP_PAGE_TITLE ${LANG_HUNGARIAN} "További telepítési választások"
	LangString CUST_COMP_PAGE_SUBTITLE ${LANG_HUNGARIAN} "Válaszd ki, mely grafikai elemeket szeretnéd a játékban használni."
	LangString CB_BP_DESC ${LANG_HUNGARIAN} "Install BoosterPack 3.0"
	LangString CB_MAPPACK_DESC ${LANG_HUNGARIAN} "Install MapPack 7.0"
	LangString CB_DISCORD_DESC ${LANG_HUNGARIAN} "Install Discord integration"
	LangString INIT_MESSAGE_NO_PATH ${LANG_HUNGARIAN} "Nem található a ParaWorld telepítési mappája. Saját kezűleg kell megadnod az elérési útvonalát."
	LangString INIT_MESSAGE_INSERT_PATH ${LANG_HUNGARIAN} "[ide írd be a játék elérési útvonalát]"
	LangString INIT_MESSAGE_REMOVE_FIRST ${LANG_HUNGARIAN} "Egy másik verzió már telepítve van, melyet előbb el kell távolítani ennek a telepítésnek a folytatása előtt. Szeretnéd ezt most megtenni?"
	LangString INIT_MESSAGE_PRESETS ${LANG_HUNGARIAN} "Szeretnéd megőrizni a Hadsereg mintáidat?"
	LangString INIT_MESSAGE_CONFLICT ${LANG_HUNGARIAN} "Egy korábbi verzió már van telepítve, amelyet előbb el kell távolítani ennek a telepítésnek a folytatása előtt. Szeretnéd ezt most megtenni? (Amennyiben az eltávolítás nem kezdődik el, akkor előbb kézileg kell eltávolítani a korábbi verziót, majd újraindítani ezt a telepítőt.)"
	LangString DESC_DESKTOP_LNK ${LANG_HUNGARIAN} "MIRAGE elindítása"
	LangString INIT_MESSAGE_CHEATS ${LANG_HUNGARIAN} "Szeretnéd megtartani a saját cheats fájlodat?"
	LangString CB_CHEATS_DESC ${LANG_HUNGARIAN} "Saját csalás fájl megtartása"
	LangString CB_PRESETS_DESC ${LANG_HUNGARIAN} "Saját Hadsereg minta megtartása"
	
	LangString DESC_SecSBasic ${LANG_ITALIAN} "I file fondamentali di gioco, che sono necessari per giocare."
	LangString DESC_SecSDiscordRPC ${LANG_ITALIAN} "Install discord integration (Rich Presence)"
	LangString ENTRY_DISCORDRPC ${LANG_ITALIAN} "Discord integration"
	LangString CUST_COMP_PAGE_TITLE ${LANG_ITALIAN} "Opzioni aggiuntive di installazione"
	LangString CUST_COMP_PAGE_SUBTITLE ${LANG_ITALIAN} "Sceglie quali componenti grafici che si desidera utilizzare nel vostro gioco."
	LangString CB_BP_DESC ${LANG_ITALIAN} "Install BoosterPack 3.0"
	LangString CB_MAPPACK_DESC ${LANG_ITALIAN} "Install MapPack 7.0"
	LangString CB_DISCORD_DESC ${LANG_ITALIAN} "Install Discord integration"
	LangString INIT_MESSAGE_NO_PATH ${LANG_ITALIAN} "Il programma di installazione non è riuscito a rilevare una installazione di ParaWorld. È necessario impostare la ParaWorld-percorso da soli."
	LangString INIT_MESSAGE_INSERT_PATH ${LANG_ITALIAN} "[Inseri cartella di ParaWorld qui]"
	LangString INIT_MESSAGE_REMOVE_FIRST ${LANG_ITALIAN} "Sembra un'altra versione di MIRAGE è già installata, che deve essere rimosso prima. Vuoi rimuoverla ora?"
	LangString INIT_MESSAGE_PRESETS ${LANG_ITALIAN} "Vuoi mantenere il tuo preimpostato di esercito?"
	LangString INIT_MESSAGE_CONFLICT ${LANG_ITALIAN} "Sembra un'altra versione di MIRAGE è già installata, che deve essere rimosso prima. Vuoi rimuoverla ora? (Se la disinstallazione non si avvia, è necessario farlo prima manualmente, e quindi avviare di nuovo il programma di installazione!)"
	LangString DESC_DESKTOP_LNK ${LANG_ITALIAN} "Avviare applicazione MIRAGE"
	LangString INIT_MESSAGE_CHEATS ${LANG_ITALIAN} "Non si desidera mantenere il file i propri trucchi?"
	LangString CB_CHEATS_DESC ${LANG_ITALIAN} "Mantenere il file i propri trucchi"
	LangString CB_PRESETS_DESC ${LANG_ITALIAN} "Mantenere il tuo preimpostato di esercito"
	
	LangString DESC_SecSBasic ${LANG_POLISH} "Podstawowe pliki gry, które są wymagane do grania."
	LangString DESC_SecSDiscordRPC ${LANG_POLISH} "Install discord integration (Rich Presence)"
	LangString ENTRY_DISCORDRPC ${LANG_POLISH} "Discord integration"
	LangString CUST_COMP_PAGE_TITLE ${LANG_POLISH} "Wybór dodatkowych elementów do zainstalowania"
	LangString CUST_COMP_PAGE_SUBTITLE ${LANG_POLISH} "Wybierz elementy graficzne, które chcesz użyć w grze."
	LangString CB_BP_DESC ${LANG_POLISH} "Install BoosterPack 3.0"
	LangString CB_MAPPACK_DESC ${LANG_POLISH} "Install MapPack 7.0"
	LangString CB_DISCORD_DESC ${LANG_POLISH} "Install Discord integration"
	LangString INIT_MESSAGE_NO_PATH ${LANG_POLISH} "Instalator nie mógł wykryć zainstalowanego Paraworlda. Trzeba ustawić ścieżkę do foldera gry Paraworld samodzielnie."
	LangString INIT_MESSAGE_INSERT_PATH ${LANG_POLISH} "[Tutaj wskaż katalog gry Paraworld ]"
	LangString INIT_MESSAGE_REMOVE_FIRST ${LANG_POLISH} "Wykryto zainstalowaną inną wersję MIRAGE, która musi być najpierw usunięta. Chcesz ją teraz usunąć?  "
	LangString INIT_MESSAGE_PRESETS ${LANG_POLISH} "Czy chcesz zachować swoją armię zestawów?"
	LangString INIT_MESSAGE_CONFLICT ${LANG_POLISH} "Wykryto zainstalowaną inną wersję MIRAGE, która musi być najpierw usunięta. Chcesz ją teraz usunąć? (Jeżeli deinstalacja nie rozpocznie się, wtedy trzeba zrobić to ręcznie, a potem ponownie uruchomić ten instalator!) "
	LangString DESC_DESKTOP_LNK ${LANG_POLISH} "Uruchom aplikację MIRAGE"
	LangString INIT_MESSAGE_CHEATS ${LANG_POLISH} "Czy chcesz, aby złożyć własne oszukuje?"
	LangString CB_CHEATS_DESC ${LANG_POLISH} "Aby założyć własne oszukuje"
	LangString CB_PRESETS_DESC ${LANG_POLISH} "Zachować swoją armię zestawów"
	
	LangString DESC_SecSBasic ${LANG_RUSSIAN} "Основные файлы, необходимые для игры."
	LangString DESC_SecSDiscordRPC ${LANG_RUSSIAN} "Установить интеграцию с дискордом (Rich Presence)"
	LangString ENTRY_DISCORDRPC ${LANG_RUSSIAN} "Интеграция с Discord"
	LangString CUST_COMP_PAGE_TITLE ${LANG_RUSSIAN} "Дополнительные настройки"
	LangString CUST_COMP_PAGE_SUBTITLE ${LANG_RUSSIAN} "Выберите дополнительные настройки."
	LangString CB_BP_DESC ${LANG_RUSSIAN} "Установить BoosterPack 3.0"
	LangString CB_MAPPACK_DESC ${LANG_RUSSIAN} "Установить MapPack 7.0"
	LangString CB_DISCORD_DESC ${LANG_RUSSIAN} "Установить интеграцию с Discord"
	LangString INIT_MESSAGE_NO_PATH ${LANG_RUSSIAN} "Программа установки не может обнаружить ParaWorld. Укажите путь до ParaWorld."
	LangString INIT_MESSAGE_INSERT_PATH ${LANG_RUSSIAN} "[Укажите путь до ParaWorld]"
	LangString INIT_MESSAGE_REMOVE_FIRST ${LANG_RUSSIAN} "Другая версия MIRAGE уже установлена. Удалить её сейчас?"
	LangString INIT_MESSAGE_PRESETS ${LANG_RUSSIAN} "Хотите сохранить наборы армий?"
	LangString INIT_MESSAGE_CONFLICT ${LANG_RUSSIAN} "У вас установлена другая версия MIRAGE, которую необходимо удалить. Вы хотите удалить её сейчас? (Если удаление не запускается, вам нужно сделать это вручную, а затем запустить эту программу установки снова!)"
	LangString DESC_DESKTOP_LNK ${LANG_RUSSIAN} "Запустить MIRAGE"
	LangString INIT_MESSAGE_CHEATS ${LANG_RUSSIAN} "Вы хотите сохранить свой собственный файл читов?"
	LangString CB_CHEATS_DESC ${LANG_RUSSIAN} "Сохранить свой собственный файл читов"
	LangString CB_PRESETS_DESC ${LANG_RUSSIAN} "Сохранить наборы армий"
	
	;LangString DESC_SecSBasic ${LANG_SPANISH} "Archivos básicos del juego, necesarios para jugar."
	;LangString DESC_SecSDiscordRPC ${LANG_SPANISH} "Install discord integration (Rich Presence)"
	;LangString ENTRY_DISCORDRPC ${LANG_SPANISH} "Discord integration"
	;LangString CUST_COMP_PAGE_TITLE ${LANG_SPANISH} "Opciones de instalación adicionales."
	;LangString CUST_COMP_PAGE_SUBTITLE ${LANG_SPANISH} "Elige qué componentes gráficos te gustaría utilizar en tu juego."
	;LangString CB_BP_DESC ${LANG_SPANISH} "Install BoosterPack 3.0"
	;LangString CB_MAPPACK_DESC ${LANG_SPANISH} "Install MapPack 7.0"
	;LangString CB_DISCORD_DESC ${LANG_SPANISH} "Install Discord integration"
	;LangString INIT_MESSAGE_NO_PATH ${LANG_SPANISH} "El instalador no ha podido detectar una instalación de Paraworld. Debes  determinar la localización de Paraworld."
	;LangString INIT_MESSAGE_INSERT_PATH ${LANG_SPANISH} "[Insertar el directorio de Paraworld aquí]"
	;LangString INIT_MESSAGE_REMOVE_FIRST ${LANG_SPANISH} "Parece que otra versión de MIRAGE ya está instalada. Para continuar con la instalación, esta debe ser desinstalada primero. ¿Te gustaría desinstalarla ahora?"
	;LangString INIT_MESSAGE_PRESETS ${LANG_SPANISH} "¿Quieres mantener tus ejércitos?"
	;LangString INIT_MESSAGE_CONFLICT ${LANG_SPANISH} "Parece que otra versión de MIRAGE ya está instalada, que debe ser eliminada primero. ¿Desea eliminarlo ahora? (Si la desinstalación no se inicia, primero debe hacerlo manualmente y, a continuación, inicie este instalador de nuevo!)"
	;LangString DESC_DESKTOP_LNK ${LANG_SPANISH} "Iniciar la aplicación MIRAGE"
	;LangString INIT_MESSAGE_CHEATS ${LANG_SPANISH} "¿Quieres guardar tu propio archivo de trucos?"
	;LangString CB_CHEATS_DESC ${LANG_SPANISH} "Guardar tu propio archivo de trucos"
	;LangString CB_PRESETS_DESC ${LANG_SPANISH} "Mantener tus ejércitos"
	
	LangString DESC_SecSBasic ${LANG_SIMPCHINESE} "The basic game files, which are required for playing."
	LangString DESC_SecSDiscordRPC ${LANG_SIMPCHINESE} "Install discord integration (Rich Presence)"
	LangString ENTRY_DISCORDRPC ${LANG_SIMPCHINESE} "Discord integration"
	LangString CUST_COMP_PAGE_TITLE ${LANG_SIMPCHINESE} "Additional install choices"
	LangString CUST_COMP_PAGE_SUBTITLE ${LANG_SIMPCHINESE} "Choose which graphical components you would like to use in your game."
	LangString CB_BP_DESC ${LANG_SIMPCHINESE} "Install BoosterPack 3.0"
	LangString CB_MAPPACK_DESC ${LANG_SIMPCHINESE} "Install MapPack 7.0"
	LangString CB_DISCORD_DESC ${LANG_SIMPCHINESE} "Install Discord integration"
	LangString INIT_MESSAGE_NO_PATH ${LANG_SIMPCHINESE} "The installer could not detect an installation of ParaWorld. You have to set the ParaWorld-path yourself."
	LangString INIT_MESSAGE_INSERT_PATH ${LANG_SIMPCHINESE} "[insert ParaWorld directory here]"
	LangString INIT_MESSAGE_REMOVE_FIRST ${LANG_SIMPCHINESE} "It seems another version of MIRAGE is already installed, which must be removed first. Would you like to remove it now?"
	LangString INIT_MESSAGE_PRESETS ${LANG_SIMPCHINESE} "Do you want to keep your army-sets?"
	LangString INIT_MESSAGE_CONFLICT ${LANG_SIMPCHINESE} "It seems another version of MIRAGE is already installed, which must be removed first. Would you like to remove it now? (If the uninstallation doesn't start, you need to do it manually first, and then start this installer again!)"
	LangString DESC_DESKTOP_LNK ${LANG_SIMPCHINESE} "Start MIRAGE application"
	LangString INIT_MESSAGE_CHEATS ${LANG_SIMPCHINESE} "Do you want to keep your cheats file?"
	LangString CB_CHEATS_DESC ${LANG_SIMPCHINESE} "Keep your own cheats file"
	LangString CB_PRESETS_DESC ${LANG_SIMPCHINESE} "Keep your army-sets"

	;Assign language strings to sections
	!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
		!insertmacro MUI_DESCRIPTION_TEXT ${SecSBasic} $(DESC_SecSBasic)
		!insertmacro MUI_DESCRIPTION_TEXT ${SecSDiscordRPC} $(DESC_SecSDiscordRPC)
	!insertmacro MUI_FUNCTION_DESCRIPTION_END


Function OpenReadMe
	Exec "notepad.exe $INSTDIR\Data\${MOD_NAME}\ReadMe.txt"
FunctionEnd

;--------------------------------
;Uninstaller Section

Function un.onInit

	!insertmacro MUI_UNGETLANGUAGE
	
FunctionEnd

Section "Uninstall"

	;ExecWait '"$INSTDIR\Tools\mod_conf.exe" SSSOff "$APPDATA"'
	
	RMDir /r "$INSTDIR\Data\${MOD_NAME}"
	RMDir /r "$INSTDIR\Data\ReColor"
	RMDir /r "$INSTDIR\Data\Wintermod"
	RMDir /r "$INSTDIR\Data\Paragames"
	RMDir /r "$INSTDIR\Data\UIfor2K"
	RMDir /r "$INSTDIR\Data\UIfor4K"
	
	Delete "$INSTDIR\Data\Info\${MOD_NAME}.info"
	Delete "$INSTDIR\Data\Info\ReColor.info"
	Delete "$INSTDIR\Data\Info\Wintermod.info"
	Delete "$INSTDIR\Data\Info\Paragames.info"
	Delete "$INSTDIR\Data\Info\UIfor2K.info"
	Delete "$INSTDIR\Data\Info\UIfor4K.info"
	
	RMDir /r "$INSTDIR\Tools\MIRAGE Launcher"
	RMDir /r "$INSTDIR\Tools\ParaWorldStatus"
	
	Delete "$DESKTOP\${MOD_NAME} Launcher.lnk"
	RMDir /r "$SMPROGRAMS\Sunflowers\ParaWorld\${MOD_NAME}"
	
	DeleteRegKey HKLM "SOFTWARE\${MOD_NAME}"
	Delete "$INSTDIR\Uninstall ${MOD_NAME}.exe"
	
SectionEnd