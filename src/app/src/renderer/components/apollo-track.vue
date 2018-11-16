<template lang="pug">
.track(v-if="track")
  chain(:chain="track.data.chain" @update="update" @addDevice="addDevice" :index="false").og
</template>

<script>
const resolveUrl = (path, fullPath) => {
  path = path.split("/")
  path.shift()
  path.shift()
  path.forEach(e => {
    if (e.indexOf(":") > 0) {
      let s = e.split(":")
      if (s.length > 2 && s[2] === "group") fullPath = fullPath[s[1]].data.data
      else fullPath = fullPath[s[1]].data
    } else fullPath = fullPath[e].data || fullPath[e]
  })
  return fullPath
}
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
      console.log(`set/track:0/chain${path} wants ${device}`)

      this.api(`set/track:0/chain${path}`, {
        type: "add",
        device,
        index,
      })
        .then(e => {
          resolveUrl(`set/track:0/chain${path}`, self.track.data).splice(index, 0, e.data)
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
      const device = resolveUrl(`set/track:0/chain${path}`, this.track.data)
      this.api(`set/track:0/chain${path}`, {
        type,
        value,
      })
        .catch(e => console.error(e))
        .then(e => (device.data = e.data.data))
    },
  },
}
</script>

<style lang="scss" scoped>
.track {
  position: relative;
  > .chain.og {
    height: calc(100% - 5px * 2);
    margin: 5px 0;
  }
}
</style>
