<template lang="pug">
.chain
  .add(:class="{lonely: !chain.data.length}")
    md-menu
      md-button(md-menu-trigger).md-icon-button.md-super-dense
        md-icon add
      md-menu-content
        md-menu-item(v-for="(item, keyy) in $store.state.av_devices" :key="item" @click="init_addDevice(keyy, 0)") {{keyy}}
  template(v-for="(device, key) in chain.data")
    device(:data="device" @addDevice="addDevice" @update="update" :index="key" :key="`device>${device.data.device}:${key}`")
    .add
      md-menu
        md-button(md-menu-trigger).md-icon-button.md-super-dense
          md-icon add
        md-menu-content
          md-menu-item(v-for="(item, keyy) in $store.state.av_devices" :key="item" @click="init_addDevice(keyy, key + 1)") {{keyy}}
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
      this.$emit("addDevice", { path: ``, device, index })
    },
    addDevice({ path, device, index }) {
      this.$emit("addDevice", { path, device, index })
    },
    update({ path, data }) {
      this.$emit("update", {
        // console.log("update", {
        path,
        data,
      })
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
  > .add {
    height: 100%;
    display: flex;
    justify-content: center;
    align-items: center;
    width: 5px;
    transition: 0.3s;
    transition-delay: 0.1s;
    overflow: hidden;
    opacity: 0;
    &.lonely {
      width: 28px;
      opacity: 0.5;
      height: unset;
    }
    &:hover {
      opacity: 1;
      width: 28px;
    }
  }
}
</style>
