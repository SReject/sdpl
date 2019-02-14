# Notice
Due to a bug in the Stream Deck software this repository is not in functioning condition.
That bug being the working directory for executables is not being set to the plugin's directory; instead it is being set to the Stream Deck install directory.

# SDPL
A 3rd-party environment launcher for Stream Deck plugins.

## Goal
SDPL aims to provide a wrapper for launching and monitoring 3rd-party runtime environments for use as a Stream Deck plugin; examples include launching NodeJS, PHP, Ruby, Perl, Java and so forth

Currently, Stream Deck does not allow plugins to specify arguments to be passed to an executable. This is very limiting as most runtime enviornments consist of an executate that requires arguments indicating which file it should process. SPDL provides such functionality along with a few QOL features

## Features
* Allows runtimes to be launched with arguments
* Logs all output from the runtime to \<plugin\>/sdpl.log
* Simple to use

## Usage
1. Download the latest [Release](https://github.com/SReject/sdpl/releases)
2. Extract to \<streamdeck plugin directory\>/name.of.your.plugin.sdPlugin
3. Edit the `manifest.json` file as indicated below
4. Add related files
5. Launch Streamdeck

## manifest.json

All SDPL configuration options are housed in the manifest.json under "SDPL"

```js
{
    // rest of manifest

    "SDPL": {

        // "UseShell" as Boolean - Optional - Defaults to false
        //     Set to true if the process should be launched via shell commands
        //     this allows the use of PATH/environment variables
        //     Such as using "java" to reference a globally installed JAVA runtime
        "UseShell": false,

        // "HideWindow" as Boolean - Optional - Defaults to true
        //     Set to true if the process's main window should be hidden
        "HideWindow": true,

        // "Path" as string - Required
        //     Path to the executable to run
        //     If a relative path is used it is relative to the plugin's directory
        //     If UseShell is true this can be a PATH/environment variable
        "Path": "node.exe",

        // "Arguments" as string - Optional - Defaults to an empty string
        //     Arguments to pass to the executable upon launch
        "Arguments": "-v"
    }
}
```

