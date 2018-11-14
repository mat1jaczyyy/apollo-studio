const { app, BrowserWindow } = require("electron")
const express = require("express")
const bp = require("body-parser")
const os = require("os")
const server = express()
const path = require("path")
const proc = require("child_process").spawn
let apipath = path.join(__dirname, "..\\..\\..\\api\\bin\\dist\\win\\api.exe")
console.log(apipath)
if (os.platform() === "darwin") apipath = path.join(__dirname, "..//api//bin//dist//osx//Api")

server.use(bp.json())

server.use((req, res) => {
  if (finishload) mainWindow.webContents.send("request", req)
  if (req.url === "/init") mainWindow.show()
  res.send(200)
})
server.listen(1549)

/**
 * Set `__static` path to static files in production
 * https://simulatedgreg.gitbooks.io/electron-vue/content/en/using-static-assets.html
 */
if (process.env.NODE_ENV !== "development")
  global.__static = require("path")
    .join(__dirname, "/static")
    .replace(/\\/g, "\\\\")

let mainWindow,
  finishload = false
const winURL =
  process.env.NODE_ENV === "development"
    ? `http://localhost:9080`
    : `file://${__dirname}/index.html`

function createWindow() {
  /**
   * Initial window options
   */
  mainWindow = new BrowserWindow({
    width: 750,
    // x: 0,
    // y: 0,
    // minWidth: 685,
    height: 292,
    width: 685,
    backgroundColor: "#212121",
    frame: false,
    show: false,
    center: true,
    minHeight: 292,
    maxHeight: 292,
    alwaysOnTop: true,
    webPreferences: { webSecurity: false },
  })
  // nodeIntegration: false, // TODO: fix this security thing

  mainWindow.loadURL(winURL)

  mainWindow.webContents.on("did-finish-load", () => (finishload = true))
  // mainWindow.on("closed", () => {
  //   mainWindow = null
  // })

  // mainWindow.once("ready-to-show", () => {
  //   mainWindow.show()
  // })
}

// app.on("ready", createWindow)
app.on("ready", startApi)

app.on("window-all-closed", () => {
  if (process.platform !== "darwin") app.quit()
})

app.on("activate", () => {
  if (mainWindow === null) createWindow()
})

function startApi() {
  const apiProcess = proc(apipath)
  createWindow()
  apiProcess.stdout.on("data", data => {
    writeLog(`api: ${data}`)
  })
}
process.on("exit", () => {
  writeLog("exit")
  apiProcess.kill()
})

function writeLog(msg) {
  console.log(msg)
}
/**
 * Auto Updater
 *
 * Uncomment the following code below and install `electron-updater` to
 * support auto updating. Code Signing with a valid certificate is required.
 * https://simulatedgreg.gitbooks.io/electron-vue/content/en/using-electron-builder.html#auto-updating
 */

/*
import { autoUpdater } from 'electron-updater'

autoUpdater.on('update-downloaded', () => {
autoUpdater.quitAndInstall()
})

app.on('ready', () => {
if (process.env.NODE_ENV === 'production') autoUpdater.checkForUpdates()
})
*/
