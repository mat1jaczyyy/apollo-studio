import Vue from "vue"

import app from "./app"
import router from "./router"
import store from "./store"

import upperFirst from "lodash/upperFirst"
import camelCase from "lodash/camelCase"
import axios from "axios"
import Raven from "raven-js"
import RavenVue from "raven-js/plugins/vue"

Array.prototype.last = function() {
  return this[this.length - 1]
}

if (process.env.NODE_ENV !== "development")
  Raven.config("https://dc7c9a8085d64d6d9181158bc19e3236@sentry.io/1323916")
    .addPlugin(RavenVue, Vue)
    .install()

const resolveUrl = (url, payload) => {
  const path = url.indexOf("/") > 0 ? url.split("/") : [url]
  const msg = data => {
    const d = data.shift(),
      split = d.split(":")
    const ret = {
      type: "forward",
      forward: split[0],
      message: {
        object: "message",
        recipient: split[0],
        data: data.length ? msg(data) : payload,
      },
    }
    if (!isNaN(Number(split[1]))) ret.index = Number(split[1])
    if (ret.forward === "device") ret.message.device = split[2]
    return ret
  }
  return {
    object: "message",
    recipient: path.shift(),
    data: path.length ? msg(path) : payload,
  }
}

const requireComponent = require.context("./components", true, /\w+\.(vue)$/)

requireComponent.keys().forEach(fileName => {
  const componentConfig = requireComponent(fileName)
  const componentName = upperFirst(
    camelCase(
      fileName
        .replace(/^\.\/(.*)\.\w+$/, "$1")
        .split("/")
        .last(),
    ),
  )
  Vue.component(componentName, componentConfig.default || componentConfig)
})

if (!process.env.IS_WEB) Vue.use(require("vue-electron"))
Vue.config.productionTip = false

const imp = ["Field", "Switch", "Menu", "List", "Button"].forEach(e =>
  Vue.use(require("vue-material/dist/components")["Md" + e]),
)
import "vue-material/dist/vue-material.min.css"

Vue.axios = Vue.prototype.axios = axios
Vue.api = Vue.prototype.api = (url, payload) =>
  new Promise((res, rej) =>
    axios
      .post(`http://localhost:1548/api`, resolveUrl(url, payload))
      .then(res)
      .catch(rej),
  )
/* eslint-disable no-new */
new Vue({
  components: { app },
  router,
  store,
  template: "<app/>",
}).$mount("#app")
