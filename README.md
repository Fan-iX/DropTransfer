# miniExploer

A file/folder drag transfer bucket based on .net framework winforms

## Build

Run

```
dotnet publish
```

And you will see stand alone executable file in `bin\Publish\DropTransfer.exe`

## Features

* Native file/folder name/icon support
* High DPI support
* Remember last window location
* Auto fold mode
* Drag/drop support

## Usage

### Mouse action

- title bar
  - double click/right click: toggle auto fold mode
  - drag: move window
- tab
  - click: switch tab / create new tab ("+")
  - right click: context menu
    - new tab
    - remove tab
  - drag to
    - another tab: rearrange tab
    - "+" tab: clone tab
    - other application: drag all file/directory items in the list
  - drop at
    - add dragged file/directory item to the list
- list
  - drop at
    - add dragged file/directory item to the list
  - right click: context menu
    - select all
    - remove selected
    - switch drag mode (`Move`/`Copy`)
- list item
  - double click: open file/directory
  - right click: explorer context menu
  - drag: drag file/directory item

### Keyboard shortcut

#### Global

| key              | action                          |
|------------------|---------------------------------|
| `Ctrl + E`       | move window to cursor position  |
| `Ctrl + W`       | exit app                        |

#### File and Directory list

| key              | action                          |
|------------------|---------------------------------|
| `Ctrl + C`       | copy item                       |
| `Ctrl + X`       | cut item                        |
| `Ctrl + V`       | add clipboard item to list      |
| `Delete`         | remove selected items from list |
