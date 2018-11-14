<template lang="pug">
.chain
  .add
    md-menu
      md-button(md-menu-trigger).md-icon-button.md-dense
        md-icon add
      md-menu-content
        md-menu-item(v-for="item in $store.state.av_devices" :key="item" @click="init_addDevice(item, 0)") {{item}}
  device(v-for="(device, key) in chain.data" :data="device"
  :key="`device>${device.data.device}:${key}`" @addDevice="addDevice" :index="key")
</template>

<script>
export default {
  name: "chain",
  props: {
    chain: {
      type: Object,
      required: true,
    },
    index: {
      type: [Number, Boolean],
      required: true,
    },
  },
  methods: {
    init_addDevice(device, index) {
      this.$emit("addDevice", { path: "", device, index })
    },
    addDevice({ path, device, index }) {
      if (typeof this.index === "boolean" && !this.index)
        this.$emit("addDevice", { path: `/chain${path}`, device, index })
      else this.$emit("addDevice", { path: `/chain:${this.index}${path}`, device, index })
    },
  },
}
</script>


<style lang="scss">
.chain {
  height: 100%;
  display: flex;
  justify-content: center;
  align-items: center;
  margin: 0 2.5px;
}
</style>
