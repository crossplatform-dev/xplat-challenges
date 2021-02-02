const { ipcRenderer } = require('electron')

let totalMessages = 0;
const messages = new Map();
let startTime;
let received;

const start = () => {
  totalMessages = parseInt(document.getElementById('messages').value);
  received = 0;
  startTime = Date.now();
  messages.clear();

  for (let id = 1; id <= totalMessages; id++) {
    const message = { id, start: Date.now(), duration: 0 };
    messages.set(id, message);
    ipcRenderer.send('asynchronous-message', message);
  }
}

document.getElementById('start').addEventListener('click', start);

ipcRenderer.on('asynchronous-reply', (event, arg) => {
  received++;
  const end = Date.now();
  const message = messages.get(arg.id);
  message.duration = end - message.start;

  if (received === totalMessages) {
    const totalTime = end - startTime;
    console.log(`Total time: ${totalTime}`);

    let durations = 0;
    for (const message of messages.values()) {
      durations += message.duration;
    }
    const average = durations / messages.size;
    document.getElementById('results').innerText = `Total: ${totalTime} - avg: ${average}`;
  }
});
