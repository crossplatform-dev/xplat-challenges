# IPC

The IPC tests measure the speed of the following scenarios:

- [x] Renderer -> Main -> Renderer
- [ ] Renderer A -> Renderer B -> Renderer A (via main or message channels)

The tests are "naïve". What ever number of messages you indicate will be sent
without any type of optimization (like batching or delaying to not fill the queue).

If you have a chatty IPC, you usually want to have some type of mechanism to
do this but in this benchmark we are just testing "defaults".

For Electron there are 2 tests: using node integration or the more secure option that
uses [contextIsolation](https://www.electronjs.org/docs/latest/security/context-isolation/)