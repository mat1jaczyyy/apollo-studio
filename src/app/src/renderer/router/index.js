import Vue from "vue"
import Router from "vue-router"

Vue.use(Router)

export default new Router({
  routes: [
    {
      path: "/",
      name: "rack",
      component: require("@/components/rack").default,
    },
    {
      path: "*",
      redirect: "/",
    },
  ],
})
