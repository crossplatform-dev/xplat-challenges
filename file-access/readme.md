# File access

The file access tests currently measures the speed for the following scenarios:

- List the contents of a folder with 10,000 files
- Read the content of those 10,000 files sequentially
- Read the content of those 10,000 files concurrently with a max of 100 files open at any given time to do not run out of file descriptors

The above tests are performed for files of 4Kb and 1MB.

The goal is to not block the UI so async/await is used in JS and C#. In the case of sequential read the code reads one file after another without blocking.

## Pre-requirements

The tests for this are under the folder `file-access`.

After clonning the repo make sure to install:

1. Latest version of [node](https://nodejs.org)
1. Latest version of [yarn 1.x](https://classic.yarnpkg.com/en/docs/install)
1. [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/)
1. Latest version of [WebView2](https://developer.microsoft.com/en-us/microsoft-edge/webview2/#download-section)

## Electron

To run the Electron tests:

1. Open a terminal into `file-access\electron\`.
1. Run `yarn` to install all dependencies
1. Run `yarn electron-forge make` to create the packaged version of the application
1. Run `out\electron-win32-x64\electron.exe` 
1. Press the "start" button
1. Once the challenge is finished, the text in the text area should change to "Done" and `files-access` should have the following 2 new files:

```
electron-1mb-results.csv
electron-4k-results.csv
```

## WebView2

To run the WebView2 tests:

1. Open the solution `file-access\wpfwebview2\wpfwebview2.sln`
1. Install the nuget dependencies
1. Build the solution in Release mode
1. Close Visual Studio
1. Run `file-access\wpfwebview2\bin\Release\netcoreapp3.1\wpfwebview2.exe`
1. Press the "start" button
1. Once the challenge is finished, the text in the text area should change to "Done" and `files-access` should have the following 2 new files:

```
webview-1mb-results.csv
webview-4k-results.csv
```

The listing and reading with this technology is done in C#. Messages are sent from WebView to C# to tell what folder and file to read at each time. C# responds with the content of each action (files in the folder or contents of the file).

## WPF

To run the WPF tests:

1. Open the solution `file-access\wpf\wpf.sln`
1. Install the nuget dependencies
1. Build the solution in Release mode
1. Close Visual Studio
1. Run `file-access\wpf\bin\Release\net5.0-windows\wpf.exe`
1. Press the "start" button
1. Once the challenge is finished, the text in the text area should change to "Done" and `files-access` should have the following 2 new files:

```
wpf-1mb-results.csv
wpf-4k-results.csv
```