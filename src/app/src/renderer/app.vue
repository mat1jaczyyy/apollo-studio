<template lang="pug">
div#app(:class="{showsettings}")
  .frame(v-if="platform !== 'darwin'")
    .left
      md-menu(md-size='medium' md-align-trigger='')
        .save(md-menu-trigger)
          i.material-icons save
        md-menu-content
          md-menu-item(@click="save('new')") new
          md-menu-item(@click="save('open')") open
          md-menu-item(@click="save('save')") save
          md-menu-item(@click="save('save_as')") save as
      div
        .settings(@click="showsettings = !showsettings")
          i.set.material-icons(:class="{showsettings}") settings
    .drag
    .right
      .minimize(@click="frame('minimize')")
        i.min.material-icons keyboard_arrow_down
      .close(@click="frame('close')")
        i.exit.material-icons close
  .frame.mac(v-else)
    .right
      .close(@click="frame('close')")
        i.exit.material-icons fiber_manual_record
      .minimize(@click="frame('minimize')")
        i.min.material-icons fiber_manual_record
    .drag
    .left
      md-menu(md-size='medium' md-align-trigger='')
        .save(md-menu-trigger)
          i.material-icons save
        md-menu-content
          md-menu-item(@click="save('new')") new
          md-menu-item(@click="save('open')") open
          md-menu-item(@click="save('save')") save
          md-menu-item(@click="save('save_as')") save as
      div
        .settings(@click="showsettings = !showsettings")
          i.set.material-icons(:class="{showsettings}") settings
  .settings
    .inner
      .setting(v-for="(setting, k) in $store.state.settings")
        h4 {{$store.state.strings[k] || k}}: <b>{{setting}}</b>
        md-switch(v-if="typeof setting === 'boolean'" :value="!setting"
        @change="$store.commit('setting', {k, v: !setting})")
        md-menu(v-else-if="k === 'theme'")
          md-button(md-menu-trigger) change
          md-menu-content
            md-menu-item(v-for="(theme, key) in $store.state.themes" :key="key"
            @click="$store.commit('setting', {k: 'theme', v: key})") {{key}}
      .setting
        h4 Open DevTools
        md-button(v-if="!devOpen" @click="toggleDevTools") open
        md-button(v-else @click="toggleDevTools") close


  .content(:style="{background: $store.state.themes[$store.state.settings.theme].background2}")
    router-view(:track="track").router
  transition(name="opacity")
    .overlay(v-if="colorSelector" @click="closeColorSelector")
      chrome(v-model="colorSelector" @click="colorPromise.res(colorSelector); colorSelector = false")
</template>

<script>
// TODO: macos frames, drag padding
import { remote } from "electron"
import ls from "local-storage"
let vue = false

const getColor = org =>
  new Promise((res, rej) => {
    if (vue && org) {
      vue.colorSelector = org
      vue.colorPromise = { res, rej }
    } else rej("no vue")
  })
window.getColor = getColor

const { ipcRenderer } = require("electron")
ipcRenderer.on("request", (event, req) => {
  switch (req.url) {
    case "/init":
      if (vue) {
        vue.track = req.body.data.tracks[0]
        vue.axios.post("http://localhost:1548/api").catch(e => {})
      }
      break
    default:
      console.log(`sad req@${req.url}`, req)
      break
  }
})
// ipcRenderer.send('asynchronous-message', 'ping')

// axios.get("http://localhost:1548/api/contacts").then(e => console.log(e))

export default {
  name: "apollo-studio",
  data: () => ({
    window: remote.getCurrentWindow(),
    showsettings: false,
    platform: process.platform,
    track: false,
    devOpen: false,
    colorSelector: false,
    colorPromise: false,
  }),
  watch: {
    "$store.state.settings.theme": "theme",
    "$store.state.settings.alwaysOnTop"(n) {
      if (n) {
        this.window.setAlwaysOnTop(true)
      } else {
        this.window.setAlwaysOnTop(false)
      }
    },
  },
  created() {
    vue = this
  },
  mounted() {
    this.theme()
  },
  methods: {
    closeColorSelector(e) {
      if (!e.target.closest(".vc-chrome")) {
        this.colorSelector = false
        this.colorPromise.rej("nope")
      }
    },
    toggleDevTools() {
      if (!this.window.isDevToolsOpened()) {
        this.window.openDevTools()
        this.devOpen = true
      } else {
        this.window.closeDevTools()
        this.devOpen = false
      }
    },
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
    changetheme(v) {
      if (v) this.$store.commit("setting", { k: "theme", v })
    },
    theme() {
      let st = this.$store.state.themes[this.$store.state.settings.theme]
      let c

      if (!document.querySelector(".theme")) c = document.createElement("style")
      else c = document.querySelector(".theme")

      c.innerHTML = `
      body, .md-list {
        background: ${st.background1} !important;
      }
      body, .md-button-content, .md-list-item-content, .md-field input {
        color: ${st.text} !important;
      }`.replace(/\s\s/g, "")

      if (!document.querySelector(".theme")) {
        document.body.appendChild(c)
        c.classList.add("theme")
      }
    },
    save(o) {
      let self = this
      const dialog_options = {
        title: "open apollo-studio save",
        buttonLabel: ":)",
        filters: [{ name: "apollo-studio savefiles", extensions: ["aps"] }],
      }
      if (o === "new")
        self
          .api("set", {
            type: "new",
          })
          .then(e => (self.track = e.data.data.tracks[0]))
          .catch(e => console.log(e))
      else if (o === "open") {
        remote.dialog.showOpenDialog(
          Object.assign({}, dialog_options, {
            properties: ["openFile"],
          }),
          path =>
            self
              .api("set", {
                type: "open",
                path: path[0],
              })
              .then(e => (self.track = e.data.data.tracks[0]))
              .catch(e => console.log(e)),
        )
      } else if (o === "save") {
        self
          .api("set", {
            type: "save",
          })
          .then(e => console.log(e))
          .catch(e => console.log(e))
      } else if (o === "save_as") {
        remote.dialog.showSaveDialog(Object.assign({}, dialog_options), path =>
          self
            .api("set", {
              type: "save_as",
              path,
            })
            .then(e => console.log(e))
            .catch(e => console.log(e)),
        )
      }
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
.md-scrollbar::-webkit-scrollbar {
  width: 0 !important;
}
.md-switch {
  margin-top: 8px;
  margin-bottom: 8px;
}
.md-menu-content {
  z-index: 99;
}
.md-list-item-content {
  padding: 0 8px;
}
.md-list {
  padding: 0;
}
.md-menu-content {
  max-height: 75vh;
}
.md-field.md-theme-default.md-focused .md-input,
.md-field.md-theme-default.md-focused .md-textarea,
.md-field.md-theme-default.md-has-value .md-input,
.md-field.md-theme-default.md-has-value .md-textarea {
  -webkit-text-fill-color: unset;
}
.md-icon-button.md-super-dense {
  width: 24px;
  min-width: 24px;
  height: 24px;
}
body {
  margin: 0;
  @include wnh(100vw, 100vh);
  user-select: none;
  background: #212121;
  font-family: "Roboto Mono", sans-serif;
  color: #bbbbbb;
  font-weight: lighter;
  transition: 0.3s background;
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
        div {
          display: flex;
          justify-content: center;
          align-items: center;
          cursor: pointer;
          > i {
            color: #515151;
            transition: 0.3s;
            font-size: 22px;
            &.set {
              transform: rotate(-90deg);
              transition: 0.5s;
            }
            &:hover,
            &.showsettings {
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
      &.mac {
        > .right > div > i,
        > .left > div > i {
          &:hover {
            &.exit {
              color: #f86360;
            }
            &.min {
              color: #f7be4d;
            }
          }
        }
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
          justify-content: space-between;
          align-items: center;
          border-bottom: 1px solid rgba(0, 0, 0, 0.125);
          padding: 10px 0;
          > h4 {
            font-weight: 300;
          }
          > button {
            margin: 0;
          }
        }
      }
    }
    &.showsettings {
      > .settings {
        @include wnh(100%, calc(100% - 32px));
      }
    }
    > .overlay {
      position: absolute;
      z-index: 995;
      top: 0;
      left: 0;
      height: 100%;
      width: 100%;
      background: rgba(0, 0, 0, 0.5);
      display: flex;
      justify-content: center;
      align-items: center;
      opacity: 1;
      > .vc-chrome {
        transform: none;
      }
      &.opacity-enter-active,
      &.opacity-leave-active,
      > .vc-chrome {
        transition: 0.3s;
      }
      &.opacity-enter,
      &.opacity-leave-to {
        opacity: 0;
        > .vc-chrome {
          transform: translateY(32px);
        }
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
i.material-icons {
  cursor: pointer;
}
</style>
