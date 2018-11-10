import { app, BrowserWindow } from "electron"
import express from "express"

const server = express()
const cors = require("cors")

server.use(cors())
server.get("/", function(req, res) {
  if (finishload) mainWindow.webContents.send("request", req)
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
    // show: false,
    center: true,
    minHeight: 292,
    maxHeight: 292,
    alwaysOnTop: true,
    webPreferences: {
      // nodeIntegration: false, // TODO: fix this security thing
    },
  })

  mainWindow.loadURL(winURL)

  mainWindow.webContents.on("did-finish-load", () => (finishload = true))
  // mainWindow.on("closed", () => {
  //   mainWindow = null
  // })

  // mainWindow.once("ready-to-show", () => {
  //   mainWindow.show()
  // })
}

app.on("ready", createWindow)

app.on("window-all-closed", () => {
  if (process.platform !== "darwin") app.quit()
})

app.on("activate", () => {
  if (mainWindow === null) createWindow()
})

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
