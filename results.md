Time to read 10,000 files of 4k each (times in ms, average of 5 runs)

|          | Read dir | Sequential Read | Parallel Read |
| ---------| ---------|-----------------|---------------|
| Electron |       19 |           7,963 |           862 |
| WebView2 |      158 |          13,097 |         4,643 |
| WPF mutex|        8 |           2,896 |           810 |
| WPF recur|        6 |           2,153 |           508 |

Time to read 10,000 files of 1Mb each (times in ms, average of 5 runs)

|          | Read dir | Sequential Read | Parallel Read |
| ---------| ---------|-----------------|---------------|
| Electron |       18 |          23,711 |         1,939 |
| WebView2 |       64 |          67,099 |        40,402 |
| WPF mutex|        4 |           8,025 |         2,652 |
| WPF recur|        4 |           7,879 |         2,705 |


In WebView 2 files are read in C# and then a message is sent to the WebView with the contents.Parallel read is done requesting 100 files at the same time tops in JS and in C#Going to test on just C# probably tomorrow to see what the difference is.