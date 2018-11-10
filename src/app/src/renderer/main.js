import Vue from "vue"

import app from "./app"
import router from "./router"
import store from "./store"

import upperFirst from "lodash/upperFirst"
import camelCase from "lodash/camelCase"
import axios from "axios"

Array.prototype.last = function() {
  return this[this.length - 1]
}

const requireComponent = require.context("./ui", false, /\w+\.(vue|js)$/)

requireComponent.keys().forEach(fileName => {
  const componentConfig = requireComponent(fileName)
  const componentName = upperFirst(camelCase(fileName.replace(/^\.\/(.*)\.\w+$/, "$1")))
  Vue.component(componentName, componentConfig.default || componentConfig)
})

if (!process.env.IS_WEB) Vue.use(require("vue-electron"))
Vue.config.productionTip = false

const imp = ["Field", "Switch", "Menu", "List", "Button"].forEach(e =>
  Vue.use(require("vue-material/dist/components")["Md" + e]),
)
import "vue-material/dist/vue-material.min.css"

Vue.axios = Vue.prototype.axios = axios
Vue.api = Vue.prototype.api = "http://localhost:1548"
/* eslint-disable no-new */
new Vue({
  components: { app },
  router,
  store,
  template: "<app/>",
}).$mount("#app")
