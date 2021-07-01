const { contextBridge, ipcRenderer } = require("electron");

const SEND_CHANNEL = 'asynchronous-message';
const RECEIVE_CHANNEL = 'asynchronous-reply';

contextBridge.exposeInMainWorld("comms", {
  sendMessage: (message) => ipcRenderer.send(SEND_CHANNEL, message),
  onMessage: (handler) => ipcRenderer.on(RECEIVE_CHANNEL, handler),
  clear: () => ipcRenderer.removeAllListeners(RECEIVE_CHANNEL)
});
