import Vue from "vue"
import Vuex from "vuex"
import ls from "local-storage"

Vue.use(Vuex)

const default_settings = {
  dialPointerLock: true,
  theme: "pretty dark",
}

export default new Vuex.Store({
  state: {
    settings: ls("settings") ? Object.assign(default_settings, ls("settings")) : default_settings,
    strings: {
      dialPointerLock: "Lock pointer while adjusting dials",
      theme: "Theme",
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
        background1: "#ffb200",
        background2: "#004aff",
        text: "#25ff00",
        device: "#e402b2",
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
