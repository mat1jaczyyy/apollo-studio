<template lang="pug">
.rack
  .rackWrap
    .additem
      //- i.material-icons(@click="newDevice") add
      md-menu(md-size="auto")
        md-button.md-icon-button(md-menu-trigger :md-ripple="false")
          md-icon(:class="{lonely: devices.length <= 0}") add
        md-menu-content
          md-menu-item(v-for="device in av_devices" :key="device"
          @click="newDevice(device, 0)") {{device}}
    .rackItem(v-for="(device, key) in devices" :key="key" :class="{hov: hov === key}")
      .inner(:style="{background: $store.state.themes[$store.state.settings.theme].device}")
        .frame
          h6.title {{device.component.name}}
          .remove(@click="remove(key)")
            i.material-icons close
        .content
          component(:is="device.component" :data="device.data")
      .additem(@mouseenter="hover(true, key)" @mouseleave="hover(false, key)")
        //- i.material-icons(@click="newDevice") add
        md-menu(md-size="auto")
          md-button.md-icon-button(md-menu-trigger :md-ripple="false")
            md-icon add
          md-menu-content
            md-menu-item(v-for="adddevice in av_devices" :key="adddevice"
            @click="newDevice(adddevice, key + 1)") {{adddevice}}
</template>

<script>
import { remote } from "electron"
import launchpad from "../ui/launchpad"
import blank from "../ui/blank"
import translation from "../devices/translation"
import delay from "../devices/delay"

const av_devices = {
  translation,
  delay,
}

export default {
  components: Object.assign({}, av_devices, { launchpad, blank }),
  name: "rack",
  data: () => ({
    hov: false,
    av_devices: Object.keys(av_devices),
    devices: [],
    window: remote.getCurrentWindow(),
  }),
  methods: {
    hover(n, i) {
      if (n) this.hov = i
      else this.hov = false
    },
    onDrop: function(dropResult) {
      this.devices = applyDrag(this.devices, dropResult)
    },
    remove: function(index) {
      this.api({
        object: "message",
        recipent: "chain",
        data: {
          type: "remove",
          index,
        },
      }).catch(e => console.error(e))
      this.devices.splice(index, 1)
    },
    newDevice(device, index) {
      let self = this
      this.api({
        object: "message",
        recipent: "chain",
        data: {
          type: "add",
          index: 0,
          device,
        },
      }).then(e => {
        console.log(e.data)
        self.devices.push({
          component: e.data.device,
          data: e.data.data,
          // component: delay,
          // data: {
          //   delay: 0,
          //   gate: 0,
          // },
        })
      })
    },
  },
}
</script>

<style lang="scss" scoped>
.rack {
  display: flex;
  justify-content: center;
  align-items: center;
  > .rackWrap {
    height: 100%;
    display: flex;
    .additem {
      display: flex;
      justify-content: center;
      align-items: center;
      // overflow: hidden;
      width: 4px;
      transition: 0.3s;
      button:before {
        display: none;
      }
      i {
        transition: 0.3s;
        color: transparent;
        &.lonely {
          color: rgba(255, 255, 255, 0.25);
        }
      }
      &:hover {
        width: 24px;
        i {
          color: rgba(255, 255, 255, 0.25);
          &:hover {
            color: rgba(255, 255, 255, 0.5);
          }
        }
      }
    }
    > .rackItem {
      height: calc(100% - 8px);
      box-shadow: none;
      margin: 0 4px;
      position: relative;
      transition: margin 0.3s;
      .additem {
        position: absolute;
        left: 100%;
        top: 0;
        height: 100%;
        width: 8px;
        transition: 0.3s;
        i {
          color: transparent;
        }
      }
      &.hov {
        margin-right: 20px;
        > .additem {
          width: 24px;
          i {
            color: rgba(255, 255, 255, 0.25);
            &:hover {
              color: rgba(255, 255, 255, 0.5);
            }
          }
        }
      }
      > .inner {
        overflow: hidden;
        border-radius: 5px 5px 0 0;
        height: 100%;
        background: #2a2a2a;
        box-shadow: 1px 1px 15px -4px rgba(0, 0, 0, 0.25);
        margin: 4px 0;
        transition: box-shadow 0.3s;
        position: relative;
        > .content {
          padding: 0 15px;
          position: relative;
          display: flex;
          justify-content: center;
          align-items: center;
          height: calc(100% - 20px);
        }
        > .frame {
          display: flex;
          height: 20px;
          padding: 0 4px;
          background: rgba(0, 0, 0, 0.125);
          position: relative;
          box-shadow: 1px 1px 15px -4px rgba(0, 0, 0, 0.25);
          > h6 {
            font-size: 12px;
            line-height: 20px;
          }
          > div {
            position: absolute;
            height: 20px;
            right: 4px;
            display: flex;
            justify-content: center;
            align-items: center;
            cursor: pointer;
            &.drag {
              cursor: -webkit-grab;
            }
            i {
              font-size: 20px;
              transition: 0.5s;
              &:hover {
                color: #d03333;
              }
            }
          }
        }
      }
    }
    .rackItem > .sh {
      transition: all 0.3s;
      box-shadow: 1px 1px 15px -2.5px rgba(0, 0, 0, 0.5);
      border: none;
    }
  }
}
</style>
