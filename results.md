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
| Electron |     49 |    400 |
| WV2      |    288 |  2,806 |

## Roundtrip: Renderer -> Main -> Renderer sequentially

The time it takes to send roundtrip 1,000 and 10,000 messages one by one.
This measures the raw speed of sending messages under ideal circumstances.

|          |  1,000 / avg    |   10,000 / avg    |
| ---------|-----------------|-------------------|
| Electron |  334ms / 0.33ms |  2,837ms / 0.28ms |
| WV2      | 1432ms / 1.43ms | 13,613ms / 1.35ms |

As expected, the average speed of the message is relatively constant regardless of
the number of messages sent.

