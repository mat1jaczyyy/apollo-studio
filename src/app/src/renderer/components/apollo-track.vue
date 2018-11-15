<template lang="pug">
.track(v-if="track")
  chain(:chain="track.data.chain" @update="update" @addDevice="addDevice" :index="false")
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
      console.log(`set/track:0/chain${path}`)
      const resolveUrl = path => {
        // const p = path.shift()
        let fullPath = self.track.data
        path.forEach(e => {
          if (e.indexOf(":") > 0) {
            let s = e.split(":")
            if (s.length > 2 && s[2] === "group") fullPath = fullPath[s[1]].data.data
            else fullPath = fullPath[s[1]].data
          } else fullPath = fullPath[e].data || fullPath[e]
        })
        return fullPath
      }
      const p = `set/track:0/chain${path}`.split("/")
      p.shift()
      p.shift()
      console.log(index, device, resolveUrl(p))

      this.api(`set/track:0/chain${path}`, {
        type: "add",
        device,
        index,
      })
        .then(e => {
          console.log(e)
          resolveUrl(p).splice(index, 0, e.data)
        })
        .catch(e => console.error(e))
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
    update({ path, type, value }) {
      // this.$emit("update", {
      // console.log("update", {
      this.api(`set/track:0/chain${path}`, {
        type,
        value,
      })
        .then(e => console.log(e))
        .catch(e => console.error(e))
    },
  },
}
</script>

<style lang="scss" scoped>
.track {
  position: relative;
}
</style>
