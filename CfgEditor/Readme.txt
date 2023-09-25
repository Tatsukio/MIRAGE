Editor and parser for Settings.cfg and other Paraworld config files in the same json-like tree structured format (https://github.com/Derkata/CfgEditor_Paraworld)

Commands inside the application's terminal (for manual use), when the executable is called without arguments or double clicked:

	Set(path,value) 	- set argpath argvalue 	- sets the variable (or node) at the given path inside the tree structure to be equal to the given value
	Get(path) 		- get argpath 		- fetches and outputs the value of the variable (or node) at the given path
	Remove(path) 		- remove path 		- deletes the variable (or node and all of its subtree contents) at the given path
	Q 			- quit			- exits the application's terminal loop, ending execution
	CD path 		- chanage dir		- changes work directory (work file) to given path - must be the full path to the file to be loaded for editing
	PWD 			- print working dir	- shows current working directory/path of currently loaded file

	*The terminal loads Settings.cfg in path "...\AppData\Roaming\SpieleEntwicklungsKombinat\Paraworld" by default on startup - use CD to change

Commands, when the executable is called with arguments:

	Parse* -> CfgEditor.exe filepath

 	*just parses the file, checking for errors in the code, does not need extra permissions, works on read-only files

 	Get** 	-> 	CfgEditor.exe -g path (filepath)*** | CfgEditor.exe --get path (filepath)
 	Remove 	-> 	CfgEditor.exe -r path (filepath) | CfgEditor.exe --remove path (filepath)
 	Set 	-> 	CfgEditor.exe -s path value (filepath) | CfgEditor.exe --set path value (filepath)

 	**Get command loads the file in read-only mode
 	***(filepath) arg is optional - if not given (filepath) is by default the path to Settings.cfg in "...\AppData\Roaming\SpieleEntwicklungsKombinat\Paraworld"

Notes:
	* Parses without whitespaces. New lines are separators. Empty new lines and wrong tree strcuture are parsing errors.
	* Does not change the contents of the file, except those affected by set or remove.
	* Automatically fixes the tabulation (formatting) inside the file.
	* Commands in terminal: Not case sensitive;Not whitespace sensitive
	* All errors technically exit with code 1 (Could't change it to exit with different code numbers, except in the printed message, haskell limitation)
	* stderr prints descriptive errors, parse errors give information about line and column and what exactly the mistake was
	* errors about permissions are caused by lack of administrative rights (the application is not elevated and trying to edit a file in an admin folder) or simply attempting to remove or set inside a read-only file
	* If there is no Settings.cfg in AppData folder the application's terminal will ask you for a valid path to a paraworld configuration file until one is given.
	* Problems in terminal with CD will just output error and ignore the command
	* All illegal operations will change nothing
	* The .hs files with the code is cursed open on your own risk
	* Reference owner when using parts of the code or whole code or I steal your cat.