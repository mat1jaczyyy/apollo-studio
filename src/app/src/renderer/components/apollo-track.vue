<template lang="pug">
.track(v-if="track")
  chain(:chain="track.data.chain" @update="update" @addDevice="addDevice" :index="false"
    :class="{left: $store.state.settings.chainAlignLeft}").og
</template>

<script>
const resolveUrl = (path, fullPath) => {
  path = path.split("/")
  path.shift()
  path.shift()
  path.forEach(e => {
    if (e.indexOf(":") > 0) {
      const s = e.split(":")
      fullPath = fullPath[s[1]].data
      if (fullPath.data) fullPath = fullPath.data
    } else fullPath = fullPath[e].data
  })
  console.log(path, fullPath)
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
      // console.log(`set/track:0/chain${path} wants ${device}`)

      this.api(`set/track:0/chain${path}`, {
        type: "add",
        device,
        index,
      })
        .then(e => {
          const resolved = resolveUrl(
            `set/track:0/chain${path}`,
            self.track.data
          )
          resolved.splice(index, 0, e.data)
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
    update({ path, data }) {
      // this.$emit("update", {
      // console.log("update", {
      // console.log(data)
      const device = resolveUrl(`set/track:0/chain${path}`, this.track.data)
      this.api(`set/track:0/chain${path}`, data)
        .catch(e => console.error(e))
        .then(e => {
          if (data.type === "remove") device.splice(data.index, 1)
          device.data = e.data.data
        })
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
    &.left {
      justify-content: flex-start;
    }
  }
}
</style>
