<template lang="pug">
.device(:class="{isgroup: data.data.device === 'group'}")
  .frame
    h5 {{data.data.device}}
  .inner(:style="{background: $store.state.themes[$store.state.settings.theme].device}")
    component(:is="$store.state.av_devices[data.data.device]" :data="data.data" @addDevice="addDevice" @update="update")
</template>

<script>
export default {
  name: "device",
  props: {
    data: {
      type: Object,
      required: true,
    },
    index: {
      type: Number,
      required: true,
    },
  },
  methods: {
    addDevice({ path, device, index }) {
      this.$emit("addDevice", {
        path: `/device:${this.index}:${this.data.data.device}${path}`,
        device,
        index,
      })
    },
    update({ path, data }) {
      this.$emit("update", {
        // console.log("update", {
        path: `/device:${this.index}:${this.data.data.device}${path}`,
        data,
      })
    },
  },
}
</script>

<style lang="scss">
.device {
  height: calc(100% - 5px);
  // min-width: 100px;
  position: relative;
  box-shadow: 0 0 0 1px rgba(0, 0, 0, 0.25);
  margin: 2.5px 0;
  border-radius: 5px;
  overflow: hidden;
  > .frame {
    height: 1.25em;
    width: 100%;
    display: flex;
    justify-content: center;
    align-items: center;
    background: rgba(0, 0, 0, 0.125);
  }
  > .inner {
    height: calc(100% - 1.25em);
    width: 100%;
    background: #1d1d1d;
    display: flex;
    justify-content: center;
    align-items: center;
  }
  &.isgroup {
    height: 100%;
    margin: 0;
    box-shadow: 0 0 0 1px rgba(0, 0, 0, 0.125);
    > .frame {
      display: none;
    }
    > .inner {
      height: 100%;
    }
  }
}
</style>
