import Vue from "vue"

import app from "./app"
import router from "./router"

if (!process.env.IS_WEB) Vue.use(require("vue-electron"))
Vue.config.productionTip = false

let imp = ["Field"].forEach(e => Vue.use(require("vue-material/dist/components")["Md" + e]))
import "vue-material/dist/vue-material.min.css"

/* eslint-disable no-new */
new Vue({
  components: { app },
  router,
  template: "<app/>",
}).$mount("#app")
