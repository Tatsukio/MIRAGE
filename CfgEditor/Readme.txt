Cfg Editor for Settings.cfg file in paraworld (https://github.com/Derkata/CfgEditor_Paraworld)

Commands in terminal:

    Set(path,value) - set argpath argvalue
    Get(path) - get argpath
    Remove(path) - remove path
    Q - exit
    CD path - change directory
    PWD - current directory

Commands in exe:

    Get -> -g path | --get path
    Remove -> -r path | --remove path
    Set -> -s path value | --set path value

Notes:

    Parses without whitespaces.
    Commands in terminal: Not case sensitive;Not whitespace sensitive
    Errors exit with code 1 (Could't change it)
    stderr prints errors
    If there is no Settings.cfg in the APPDATA folder it will ask you for a valid path until one is given.
    In terminal with CD will just output error and ignore the command
    All illegal operation will change nothing
    The .hs file with the code is cursed open on your own risk
    Reference owner when using parts of the code or whole code or I steal your cat.
