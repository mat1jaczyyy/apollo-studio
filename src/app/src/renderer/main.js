import Vue from "vue"

import app from "./app"
import router from "./router"
import store from "./store"

if (!process.env.IS_WEB) Vue.use(require("vue-electron"))
Vue.config.productionTip = false

let imp = ["Field", "Switch"].forEach(e =>
  Vue.use(require("vue-material/dist/components")["Md" + e]),
)
import "vue-material/dist/vue-material.min.css"

/* eslint-disable no-new */
new Vue({
  components: { app },
  router,
  store,
  template: "<app/>",
}).$mount("#app")
