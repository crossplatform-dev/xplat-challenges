# CPU

The CPU benchmark measures how much time it takes to calculate all the prime numbers under XXXX wihtout blocking the UI and reporting how many have been found so far.

## Pre-requirements

The tests for this are under the folder `cpu`.

After clonning the repo make sure to install:

1. Latest version of [node](https://nodejs.org)
1. [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/)

## Electron

To run the Electron tests:

1. Open a terminal into `cpu\electron\`.
1. Run `npm install` to install all dependencies
1. Run `npm run makemake` to create the packaged version of the application
1. Run `out\electron-win32-x64\electron.exe` 
1. Press the "start" button
1. Once the benchmark is finished, the average should appear on the screen

## WPF

To run the WPF tests:

1. Open the solution `cput\wpf\wpf.sln`
1. Build the solution in Release mode
1. Close Visual Studio
1. Run `cpu\wpf\bin\Release\netcoreapp3.1\wpf.exe`
1. Press the "start" button
1. Once the benchmark is finished, the average should appear on the screen
