import Vue from "vue"
import Router from "vue-router"

Vue.use(Router)

export default new Router({
  routes: [
    {
      path: "/",
      name: "apollo-track",
      component: require("@/components/apollo-track").default,
    },
    {
      path: "*",
      redirect: "/",
    },
  ],
})
