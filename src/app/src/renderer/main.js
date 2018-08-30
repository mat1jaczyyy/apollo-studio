import Vue from "vue"

import app from "./app"
import router from "./router"

if (!process.env.IS_WEB) Vue.use(require("vue-electron"))
Vue.config.productionTip = false

/* eslint-disable no-new */
new Vue({
  components: { app },
  router,
  template: "<app/>",
}).$mount("#app")
