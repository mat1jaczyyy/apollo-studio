<template lang="pug">
div#app(:class="{showsettings}")
  .frame
    .left
      .settings(@click="showsettings = !showsettings")
        i.set.material-icons(:class="{rotate: showsettings}") settings
    .drag
    .right
      .minimize(@click="frame('minimize')")
        i.min.material-icons keyboard_arrow_down
      .close(@click="frame('close')")
        i.exit.material-icons close
  .settings
    .inner
      .setting(v-for="(setting, k) in $store.state.settings")
        h4 {{$store.state.strings[k] || k}}: <b>{{setting}}</b>
        md-switch(v-if="typeof setting === 'boolean'" :value="!setting"
        @change="$store.commit('setting', {k, v: !setting})")
  .content
    router-view.router
</template>

<script>
// TODO: macos frames, drag padding
import { remote } from "electron"
import ls from "local-storage"

export default {
  name: "orion-studio",
  data: () => ({
    window: remote.getCurrentWindow(),
    showsettings: false,
  }),
  methods: {
    frame(icon) {
      switch (icon) {
        case "minimize":
          this.window.minimize()
          break
        case "close":
          this.window.close()
          break
      }
    },
    changesetting(e) {
      console.log(e, arguments)
    },
  },
}
</script>

<style lang="scss">
@import "./assets/fonts";
@import "./assets/common";
@import "~vue-material/dist/theme/engine";
@include md-register-theme(
  "default",
  (
    primary: #bbbbbb,
    accent: #0288d1,
    theme: dark,
  )
);
// quart cubic beziers, very nice.
:root {
  --ease: cubic-bezier(0.77, 0, 0.175, 1);
  --easeIn: cubic-bezier(0.895, 0.03, 0.685, 0.22);
  --easeOut: cubic-bezier(0.165, 0.84, 0.44, 1);
}
@import "~vue-material/dist/theme/all";
.md-field.md-theme-default.md-disabled label,
.md-field.md-theme-default.md-disabled .md-input,
.md-field.md-theme-default.md-disabled .md-textarea {
  -webkit-text-fill-color: rgba(255, 255, 255, 0.5);
}
::selection {
  background: #dbdbdb;
}
.md-field {
  margin: 0;
  margin-top: 0;
  padding-top: 0;
  min-height: unset;
  .md-input {
    width: 3em;
    text-align: center;
  }
}
body {
  margin: 0;
  @include wnh(100vw, 100vh);
  user-select: none;
  background: #212121;
  font-family: "Roboto Mono", sans-serif;
  color: #bbbbbb;
  font-weight: lighter;
  #app {
    @include wnh;
    position: absolute;
    overflow: hidden;
    > .frame {
      @include wnh(100%, 32px);
      position: relative;
      display: flex; // background: rgba(0, 0, 0, 0.25); // background: #515151;
      z-index: 999;
      .right,
      .left {
        display: flex;
        justify-content: center;
        align-items: center;
        margin: 0 4px;
        > div {
          display: flex;
          justify-content: center;
          align-items: center;
          cursor: pointer;
          > i {
            color: #515151;
            transition: 0.3s;
            &.set {
              transform: rotate(-90deg);
              font-size: 22px;
              transition: 0.5s;
            }
            &:hover.set,
            &.rotate {
              color: rgb(175, 175, 175);
              transform: rotate(0);
              font-size: 24px;
            }
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
      > .drag {
        height: 32px;
        -webkit-app-region: drag;
        // width: calc(100% - 64px);
        width: 100%;
        z-index: 9999;
      }
    }
    > .content {
      @include wnh(100%, calc(100% - 32px));
      position: relative;
      z-index: 99;
      background: #252525;
      border-radius: 10px 10px 0 0;
      overflow: hidden;
      box-shadow: 0 -1px 20px -3px rgba(0, 0, 0, 0.5);
      transition: 1s var(--ease);
      > .router {
        @include wnh;
        overflow: hidden;
      }
    }
    > .settings {
      transition: 1s var(--ease);
      @include wnh(100%, 0);
      > .inner {
        @include wnh;
        padding: 15px 25px;
        > .setting {
          display: flex;
          justify-content: space-around;
          align-items: center;
          > h4 {
            font-weight: 300;
          }
        }
      }
    }
    &.showsettings {
      > .settings {
        @include wnh(100%, calc(100% - 32px));
      }
    }
  }
}
.rotate {
  animation: 1s linear infinite rotate;
}
@keyframes rotate {
  0% {
    transform: rotate(0);
  }
  100% {
    transform: rotate(360deg);
  }
}
</style>
