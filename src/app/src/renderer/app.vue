<template lang="pug">
div#app
  .frame
    .drag
    .right
      a(@click="frame('minimize')")
        i.min.material-icons keyboard_arrow_down
      a(@click="frame('close')")
        i.exit.material-icons close
  .content
    router-view.router
</template>

<script>
import { remote } from "electron"

export default {
  name: "orion-studio",
  data: () => ({
    window: remote.getCurrentWindow(),
  }),
  methods: {
    frame(icon) {
      switch (icon) {
        case "minimize":
          this.window.minimize()
          break
        case "maximize":
          if (!this.window.isMaximized()) {
            this.window.maximize()
            this.visual.maximized = true
            ls("maximized", true)
          } else {
            this.window.unmaximize()
            this.visual.maximized = false
            ls("maximized", false)
          }
          break
        case "close":
          this.window.close()
          break
      }
    },
  },
}
</script>

<style lang="scss">
@import "./assets/fonts";
@import "./assets/common";
body {
  margin: 0;
  @include wnh(100vw, 100vh);
  user-select: none;
  background: #373737;
  font-family: "Roboto Mono", sans-serif;
  color: #bbbbbb;
  font-weight: lighter;
  #app {
    @include wnh;
    position: absolute;
    & > .frame {
      @include wnh(100%, 32px);
      position: relative;
      display: flex;
      // background: rgba(0, 0, 0, 0.25);
      // background: #515151;
      z-index: 999;
      // .left {
      //   @include wnh(24px, 24px);
      //   position: absolute;
      //   top: 50%;
      //   transform: translateY(-50%);
      //   left: 4px;
      //   img {
      //     @include wnh(24px, 24px);
      //   }
      // }
      .right {
        display: flex;
        justify-content: center;
        align-items: center;
        position: absolute;
        right: 4px;
        top: 50%;
        transform: translateY(-50%);
        a {
          display: flex;
          justify-content: center;
          align-items: center;
          cursor: pointer;
          i {
            color: #515151;
            transition: 0.5s;
            &:hover {
              &.exit {
                color: #b71c1c;
              }
              &.min {
                color: #f57f17;
              }
            }
          }
        }
      }
      .drag {
        height: 32px;
        -webkit-app-region: drag;
        width: calc(100% - 64px);
        z-index: 9999;
      }
    }
    & > .content {
      @include wnh(100%, calc(100% - 32px));
      position: relative;
      z-index: 99;
      background: #414141;
      border-radius: 10px 10px 0 0;
      overflow: hidden;
      box-shadow: 0 -1px 20px -3px rgba(0, 0, 0, 0.5);
      .router {
        @include wnh;
        overflow: hidden;
      }
    }
  }
}
</style>
