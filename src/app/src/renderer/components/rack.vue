<template lang="pug">
.rack
  .rackWrap
    .rackItem(v-for="(device, key) in devices" :key="key")
      .inner
        .frame
          h6.title {{device.name}}
          .remove(@click="remove(key)")
            i.material-icons close
        .content
          component(:is="device.component")
</template>

<script>
import { remote } from "electron"
import dial from "../ui/dial"
import launchpad from "../ui/launchpad"
import blank from "../ui/blank"
import translation from "../devices/translation"
import delay from "../devices/delay"

export default {
  components: { dial, launchpad, translation, blank, delay },
  name: "rack",
  data: () => ({
    devices: [
      {
        component: "launchpad",
        name: "launchpad",
      },
      {
        component: "translation",
        name: "translation",
      },
      {
        component: "delay",
        name: "delay",
      },
    ],
    window: remote.getCurrentWindow(),
  }),
  methods: {
    onDrop: function(dropResult) {
      this.devices = applyDrag(this.devices, dropResult)
    },
    remove: function(id) {
      this.devices.splice(id, 1)
    },
  },
}
</script>

<style lang="scss" scoped>
.rack {
  display: flex;
  justify-content: center;
  align-items: center;
  .rackWrap {
    height: 100%;
    display: flex;
    .rackItem {
      height: calc(100% - 8px);
      box-shadow: none;
      .inner {
        overflow: hidden;
        border-radius: 5px 5px 0 0;
        height: 100%;
        background: #2a2a2a;
        box-shadow: 1px 1px 15px -4px rgba(0, 0, 0, 0.25);
        margin: 4px 0;
        margin-left: 4px;
        transition: box-shadow 0.3s;
        position: relative;
        .content {
          padding: 0 15px;
          position: relative;
          display: flex;
          justify-content: center;
          align-items: center;
          height: calc(100% - 20px);
        }
        .frame {
          display: flex;
          height: 20px;
          padding: 0 4px;
          background: #2b2b2b;
          position: relative;
          box-shadow: 1px 1px 15px -4px rgba(0, 0, 0, 0.25);
          h6 {
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
