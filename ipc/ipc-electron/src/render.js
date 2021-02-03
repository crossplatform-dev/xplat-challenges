const { ipcRenderer } = require('electron')

let totalMessages = 0;
const messages = new Map();
let startTime;
let received;

const startBurst = () => {
  totalMessages = parseInt(document.getElementById('messages').value);
  received = 0;
  startTime = Date.now();
  messages.clear();

  const onMessage = (event, arg) => {
    received++;
    const end = Date.now();
    const message = messages.get(arg.id);
    message.duration = end - message.start;

    if (received === totalMessages) {
      ipcRenderer.removeListener('asynchronous-reply', onMessage);
      const totalTime = end - startTime;
      console.log(`Total time: ${totalTime}`);


      let durations = 0;
      for (const message of messages.values()) {
        durations += message.duration;
      }
      const average = durations / messages.size;
      document.getElementById('results').innerText = `Total: ${totalTime}ms - avg message roundtrip: ${average}ms`;
    }
  }

  ipcRenderer.on('asynchronous-reply', onMessage);

  for (let id = 1; id <= totalMessages; id++) {
    const message = { id, start: Date.now(), duration: 0 };
    messages.set(id, message);
    ipcRenderer.send('asynchronous-message', message);
  }
};

document.getElementById('start').addEventListener('click', startBurst);

const startSequential = () => {
  totalMessages = parseInt(document.getElementById('messages').value);
  received = 0;
  startTime = performance.now();
  messages.clear();

  const sendMessage = (id) => {
    const message = { id, start: performance.now(), duration: 0 };
    messages.set(id, message);
    ipcRenderer.send('asynchronous-message', message);
  };

  const onMessage = (event, arg) => {
    received++;
    const end = performance.now();
    const message = messages.get(arg.id);
    if (!message) {
      return;
    }
    message.duration = end - message.start;

    if (received === totalMessages) {
      ipcRenderer.removeListener('asynchronous-reply', onMessage);
      const totalTime = end - startTime;
      console.log(`Total time: ${totalTime}`);

      let durations = 0;
      for (const message of messages.values()) {
        durations += message.duration;
      }
      const average = durations / messages.size;
      document.getElementById('results-sequential').innerText = `Total: ${totalTime.toFixed(2)}ms - avg message roundtrip: ${average.toFixed(2)}ms`;
    } else {
      sendMessage(received + 1);
    }
  };

  ipcRenderer.on('asynchronous-reply', onMessage);

  sendMessage(received + 1);
};

document.getElementById('start-sequential').addEventListener('click', startSequential);
