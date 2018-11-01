import Vue from "vue"
import Vuex from "vuex"
import ls from "local-storage"

Vue.use(Vuex)

export default new Vuex.Store({
  state: {
    settings: ls("settings") || {
      dialPointerLock: true,
    },
  },
  mutations: {
    setting(state, { k, v }) {
      state.settings[k] = v
      ls("settings", state.settings)
    },
  },
})
