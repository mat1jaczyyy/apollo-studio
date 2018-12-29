import Vue from "vue"
import Vuex from "vuex"
import ls from "local-storage"

Vue.use(Vuex)

const default_settings = {
  dialPointerLock: true,
  theme: "pretty dark",
  alwaysOnTop: true,
  chainAlignLeft: false,
}

export default new Vuex.Store({
  state: {
    settings: ls("settings")
      ? Object.assign(default_settings, ls("settings"))
      : default_settings,
    strings: {
      dialPointerLock: "Lock pointer while adjusting dials",
      theme: "Theme",
      alwaysOnTop: "Always on top",
      chainAlignLeft: "Align track to the left of the window",
    },
    av_devices: {
      delay: "delay",
      group: "group",
      paint: "paint",
      translation: "translation",
      layer: "layer",
      // preview: "preview",
    },
    themes: {
      "pretty dark": {
        background1: "#212121",
        background2: "#252525",
        text: "#bbbbbb",
        device: "#2a2a2a",
        dial1: "#0288d1",
        dial2: "#ffb532",
      },
      "very light": {
        background1: "#ececec",
        background2: "#e2e2e2",
        text: "#444444",
        device: "#dcdcdc",
        dial1: "#6b8b9a",
        dial2: "#9a6b6b",
      },
      "my eyes hurt": {
        background1:
          "radial-gradient(ellipse at center, #e9ff02 1%,#00bbff 49%,#ff0000 100%)",
        background2:
          "linear-gradient(to bottom, #e9ff02 1%,#00bbff 49%,#ff0000 100%)",
        text: "#25ff00",
        device:
          "radial-gradient(ellipse at center, #e402b2 0%,#00ff11 49%,#ff0000 100%)",
        dial1: "#00ff34",
        dial2: "#ff4400",
      },
    },
  },
  mutations: {
    setting(state, { k, v }) {
      state.settings[k] = v
      ls("settings", state.settings)
    },
  },
})
