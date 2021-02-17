# Testing machine

All these tests were executed on the Microsoft provided [Windows 10 development virtual machine](https://developer.microsoft.com/en-us/windows/downloads/virtual-machines/) running on Hyper-V. This machine has
4096MB assigned (dynamic between 2048-8192MB) and 1 virtual processor.
The host machine is a Surface Laptop 3 with 16GB of memory and an Intel(R) Core(TM) i7-1065G7 CPU.

# File access

Goal is to read and write large numbers of files of different sizes (4k and 1MB) without blocking the UI thread.
`async/await` is used in JavaScript and C# to perform all I/O operations.

## Time to write and read 10,000 files of 4k each

times in ms, average of 5 runs

|          | Write files | Read dir | Sequential Read | Concurrent Read |
| ---------|-------------|----------|-----------------|-----------------|
| Electron |       6,218 |       21 |           8,751 |           1,059 |
| WebView2 |      10,585 |       82 |          13,470 |           5,936 |
| WPF      |       4,101 |        4 |           1,848 |             448 |

## Time to write and read 10,000 files of 1Mb each

times in ms, average of 5 runs

|          | Write files | Read dir | Sequential Read | Concurrent Read |
| ---------|-------------|----------|-----------------|-----------------|
| Electron |      29,215 |       34 |          17,518 |           4,602 |
| WebView2 |      43,250 |       98 |          86,222 |          35,753 |
| WPF      |      25,729 |        6 |          29,536 |           3,417 |

In WebView 2 files are read in C# and then a message is sent to the WebView with the contents.
"Concurrent read" reads up to 100 files concurrently.

# CPU

Goal is to calculate all the primer numbers under 10,000,000 while still having the UI being responsive. The UI is updated about 250ms.

Time in ms, average of 5 runs.

|          | 10,000,000 |
| ---------|------------|
| Electron |      3,290 |
| WPF      |      3,328 |

# IPC

Goal is to measure the speed of IPC under different circumstances.
Time in ms, average of 5 runs.

## Roundtrip: Renderer -> Main -> Renderer in burst

The time it takes to send roundtrip 1,000 and 10,000 messages at the same time
This allow us to see how the pipe handles congestion.

|          |  1,000 | 10,000 |
| ---------|--------|--------|
| Electron |   49ms |    400ms |
| WV2 (C#) |  288ms |  2,806ms |
| WV2 (C++)|  264ms |  1,794ms |

## Roundtrip: Renderer -> Main -> Renderer sequentially

The time it takes to send roundtrip 1,000 and 10,000 messages one by one.
This measures the raw speed of sending messages under ideal circumstances.

|          |  1,000 / avg    |   10,000 / avg    |
| ---------|-----------------|-------------------|
| Electron |  334ms / 0.33ms |  2,837ms / 0.28ms |
| WV2 (C#) | 1432ms / 1.43ms | 13,613ms / 1.35ms |
| WV2 (C++)|  943ms / 0.93ms | 10,782ms / 1.06ms |

As expected, the average speed of the message is relatively constant regardless of
the number of messages sent.
**Note:** The C++ version stringifyes and parses on the JavaScript side because
my C++ skill are non-existent and I've been unable to send an object and
parse/serialize in C++.

# Startup time

This benchmark measures how long it takes to get an application fully started. The code being
executed is https://ahfarmer.github.io/calculator/. The reason is that all resources are
loaded from the same domain (no ads, tracking, etc.) and it is built in React, which is widely
used.
The applications are compiled in Release mode (when applciable) and launched from the command
line. The executions is recorded with Camtasia and the time is measured from the moment the
cursor dissappears from the line to the moment the application is fully rendered.
The applications were executed a few times to make sure they always took about the same time.
The video can be found in [./recordings/electron-wv2-startup-time.mp4](./recordings/electron-wv2-startup-time.mp4)

| Technology       | Time |
| ---------------- | ---- |
| Electron 11      | 1.02s|
| WV2 + NET5 + WPF | 6.05s|
| WV2 + Win32 (C++)| 2.79s|
