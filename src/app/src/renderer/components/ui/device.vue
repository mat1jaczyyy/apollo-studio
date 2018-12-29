<template lang="pug">
transition(name="device")
  .device(:class="{isgroup: data.data.device === 'group'}")
    .frame
      .title
        h5 {{data.data.device}}
      .close(@click="deletedevice")
        i.material-icons close
    .inner(:style="{background: $store.state.themes[$store.state.settings.theme].device}")
      component(:is="$store.state.av_devices[data.data.device]" :data="data.data" @addDevice="addDevice" @update="update").component
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
        path: `/device:${this.index}${path}`,
        device,
        index,
      })
    },
    update({ path, data }) {
      this.$emit("update", {
        path: `/device:${this.index}${path}`,
        data,
      })
    },
    deletedevice() {
      this.$emit("update", {
        path: "",
        data: {
          type: "remove",
          index: this.index,
        },
      })
    },
  },
}
</script>

<style lang="scss">
.device {
  height: calc(100% - 5px);
  // width: 100px;
  position: relative;
  box-shadow: 0 0 0 1px rgba(0, 0, 0, 0.25);
  margin: 2.5px 0;
  border-radius: 5px;
  overflow: hidden;
  opacity: 1;
  &.device-enter-active,
  &.device-leave-active {
    transition: 0.3s;
  }
  &.device-enter,
  &.device-leave-to {
    box-shadow: none;
    width: 0;
  }
  > .frame {
    height: 1.25em;
    width: 100%;
    display: flex;
    justify-content: center;
    align-items: center;
    background: rgba(0, 0, 0, 0.125);
    > .title,
    > .close {
      display: flex;
      justify-content: center;
      align-items: center;
      transition: 0.3s;
      transition-delay: 0;
    }
    > .title {
      width: 100%;
      > h5 {
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
      }
    }
    > .close {
      width: 0%;
      opacity: 0;
      > i {
        font-size: 0;
        transition: 0.3s;
      }
    }
    &:hover {
      > .title,
      > .close {
        transition-delay: 0.25s;
      }
      > .title {
        width: 85%;
        > h5 {
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;
        }
      }
      > .close {
        width: 25%;
        opacity: 1;
        > i {
          font-size: 1em;
        }
      }
    }
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
    width: unset;
    > .frame {
      display: none;
    }
    > .inner {
      height: 100%;
    }
  }
}
</style>
