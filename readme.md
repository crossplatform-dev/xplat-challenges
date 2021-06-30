# Cross-platform benchmarks

Benchmarks to measure different common scenarios accross multiple technologies:

* [File access](./file-access/readme.md)
* [CPU usage calculating prime numbers](./cpu/readme.md)
* [IPC](./ipc/readme.md)
* [Startup time and base memory](./startup-memory/readme.md)

## Testing machine

All these tests were executed on the Microsoft provided
[Windows 10 development virtual machine](https://developer.microsoft.com/en-us/windows/downloads/virtual-machines/)
running on Hyper-V. This machine has 4096MB of RAM assigned (no dynamic) and 1 virtual processor.
SmartScreen has been disabled on the VM and no other applications
are running in the Guest or the Host.

The host machine is a Surface Laptop 3 with 16GB of memory and an Intel(R) Core(TM) i7-1065G7 CPU.

## Results

You can check them [here](./results.md).

## Future benchmarks:

* Network requests
