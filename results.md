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