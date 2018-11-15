<template lang="pug">
.track(v-if="track")
  chain(:chain="track.data.chain" @addDevice="addDevice" :index="false")
</template>

<script>
export default {
  inheritAttrs: false,
  props: {
    track: {
      type: [Object, Boolean],
      required: true,
    },
  },
  name: "apollo-track",
  data() {
    return {
      av_devices: ["translation", "delay"],
    }
  },
  methods: {
    addDevice({ path, device, index }) {
      let self = this
      // console.log(`set/track:0/chain${path}`)
      const insertDevice = (path, data) => {
        // const p = path.shift()
        let fullPath = self.track.data
        path.forEach(e => {
          // console.log(e)
          if (e.indexOf(":") > 0) console.log(fullPath[e.split(":")[1]].data)
          else console.log(fullPath[e].data)
          // if (e.indexOf(":") > 0) fullPath = fullPath[e.split(":")[1]].data
          // else fullPath = fullPath[e].data
        })
      }
      const p = `set/track:0/chain${path}`.split("/")
      p.shift()
      p.shift()
      insertDevice(p, device)
      // this.api(`set/track:0/chain${path}`, {
      //   type: "add",
      //   device,
      //   index,
      // })
      //   .then(e => console.log(e))
      //   .catch(e => console.error(e))
    },
    remove: function(index) {
      this.api("set/track:0/chain:0", {
        type: "remove",
        index,
      }).catch(e => console.error(e))
      this.devices.splice(index, 1)
    },
    newDevice(device, index) {
      let self = this
      this.api("set/track:0/chain:0", {
        type: "add",
        index,
        device,
      }).then(e => {
        self.devices.push({
          component: e.data.data.device,
          data: e.data.data.data,
        })
      })
    },
  },
}
</script>

<style lang="scss" scoped>
.track {
  position: relative;
}
</style>
