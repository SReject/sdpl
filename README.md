# SDPL
A 3rd-party environment launcher for Stream Deck plugins.

# Notice
This project requires StreamDeck v4.2 or later

## Goal
SDPL aims to provide a wrapper for launching and monitoring 3rd-party runtime environments for use as a Stream Deck plugin; examples include launching NodeJS, PHP, Ruby, Perl, Java and so forth

Currently, Stream Deck does not allow plugins to specify arguments to be passed to an executable. This is very limiting as most runtime environments consist of an executate that requires arguments indicating which file it should process. SPDL provides such functionality along with a few QOL features

## Features
* Allows runtimes to be launched with arguments
* Logs all output from the runtime to \<plugin\>/sdpl.log
* Simple to use

## Usage
1. Download the latest [Release](https://github.com/SReject/sdpl/releases)
2. Extract to your plugin's directory
3. Edit your plugins `manifest.json` file as indicated below
4. Launch Streamdeck

## manifest.json

All SDPL configuration options are housed in the manifest.json under "SDPL"

```js
{
    // ... rest of manifest ...

    // CodePath should point to the executable created after building
    "CodePath": "sdpl/sdpl.exe",
    "SDPL": {

        // "HideWindow" as Boolean - Optional - Defaults to true
        //     Set to true if the process's main window should be hidden
        "HideWindow": true,

        // "Path" as string - Required
        //     Path to the executable to run
        //     If a relative path is used it is relative to the plugin's directory
        //     Can be an environment variable such as 'node' or 'java'
        "Path": "node.exe",

        // "Arguments" as string - Optional - Defaults to an empty string
        //     Arguments to pass to the executable upon launch
        "Arguments": "-v"
    }
}
```

