# CfgEditor

Reads and changes ParaWorld config files: `Settings.cfg` and every file in the same tree format (`.txt`, `.ttree`,
`.srf`, `.ini` that start with `Root`). C# rewrite of the Haskell CfgEditor by Derkata (same command line, same exit
codes). The code is in `Shared/Cfg` (`CfgDocument`), so the launcher and PWKiller use it without starting this exe.

## Command line

```
CfgEditor <file>                          check that the file can be read (parse)
CfgEditor -g|--get <path> [file]          print a value
CfgEditor -s|--set <path> <value> [file]  set a value (missing nodes are created)
CfgEditor -r|--remove <path> [file]       remove a node and everything below it
CfgEditor                                 interactive mode (type 'help')
CfgEditor --help
```

- `<path>`: node names separated by `/`, e.g. `Root/Global/Language`. The root name can be left out.
- Without `[file]`: `%APPDATA%\SpieleEntwicklungsKombinat\Paraworld\Settings.cfg`.
- Put paths with spaces in quotes: `CfgEditor -g Root/Global/Language "C:\Users\John Smith\...\Settings.cfg"`.

Interactive mode: `get <path>`, `set <path> <value>`, `remove <path>`, `cd <file>`, `pwd`, `q`. The old forms
`Get(path)`, `Set(path,value)`, `Remove(path)` work too.

| Exit code | |
|---|---|
| 0 | OK |
| 1 | File not found, can't be read or written |
| 100 | Parse error (the message has line and column) |
| 101 | Wrong number of arguments |
| 102 | Unknown command, or a value / name that isn't allowed |
| 201 | Remove: the node doesn't exist |
| 301 | Get: the node doesn't exist |

## The format

```
Root {
	Global {
		Language = 'uk'
	}
	Pest {
		Server {
			UseUslCPP = '0'
		}
	}
}
```

A node is a name, then optionally `= 'value'`, then optionally a block `{ ... }`. Line breaks and spaces between
the parts don't matter and commas between nodes are ignored. Some `.srf` files of the editor are looser, and that
is read too: values without quotes (`pos_x = 100`), no `=` (`type 'CFrameWindow'`), and a `'` inside a value
(`tooltip = 'Player who's TT'`: a value ends at the first `'` that is followed by a space, a brace or the line end).
`set` doesn't accept `'` or line breaks in new values.

## How files are changed

- **Only the changed part of the text changes.** Everything else stays byte for byte as it was: tabs, line ends,
  empty lines, the UTF-8 byte order mark, characters of any code page. New nodes are indented like the file.
- **Values are decoded** with UTF-8 if the file has a byte order mark, otherwise with the Windows ANSI code page,
  as the game does.
- **A file that can't be parsed is never written.**
- **Writing is atomic:** a temporary file in the same folder, then a rename. While the file is locked (e.g. by the
  game) it retries for about two seconds.

## Differences to the Haskell version

- Paths with spaces in the file name work (the old launcher passed the Settings.cfg path without quotes, so it
  failed for user names with spaces).
- Code pages: the old version always used CP1251 for reading and writing.
- It doesn't reformat the whole file.
- The looser `.srf` syntax above is accepted. Of the 725 files in `GameFiles` that start with `Root`, 722 are
  read; the other 3 have real errors (an extra `}`, a `//` comment), which the launcher's health check lists.
- The old version wrote files with a byte order mark in two steps (not atomic).