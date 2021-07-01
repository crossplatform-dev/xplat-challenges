// Modules to control application life and create native browser window
const { app, BrowserWindow, ipcMain } = require('electron')

// Keep a global reference of the window object, if you don't, the window will
// be closed automatically when the JavaScript object is garbage collected.
let mainWindow
let backgroundWindow

function createWindows() {
  // Create the browser window.
  mainWindow = new BrowserWindow({
    title: 'Electron IPC challenge - node integration',
    width: 800,
    height: 600,
    webPreferences: {
      nodeIntegration: true,
      contextIsolation: false
    }
  })

  // and load the index.html of the app.
  mainWindow.loadFile('src/render.html')
  // backgroundWindow.loadFile('src/background.html')

  // Open the DevTools.
  // mainWindow.webContents.openDevTools()

  // Emitted when the window is closed.
  mainWindow.on('closed', function () {
    // Dereference the window object, usually you would store windows
    // in an array if your app supports multi windows, this is the time
    // when you should delete the corresponding element.
    mainWindow = null

    if (backgroundWindow) {
      backgroundWindow.close()
    }
  })
}

// This method will be called when Electron has finished
// initialization and is ready to create browser windows.
// Some APIs can only be used after this event occurs.
app.on('ready', createWindows)

// Quit when all windows are closed.
app.on('window-all-closed', function () {
  app.quit()
})

ipcMain.on('asynchronous-message', (event, ...args) => {
  event.reply('asynchronous-reply', ...args)
})

ipcMain.on('asynchronous-reply', (event, ...args) => {
  mainWindow.webContents.send('asynchronous-reply', ...args)
})

ipcMain.on('asynchronous-message-proxy', (event, ...args) => {
  backgroundWindow.webContents.send('asynchronous-message', ...args)
})
