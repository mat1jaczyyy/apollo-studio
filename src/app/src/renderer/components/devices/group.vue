<template lang="pug">
.group
  .chains(v-if="data.data.length > 0")
    div(v-for="(member, key) in data.data" :key="`group>${data.data[show] ? data.data[show].object : 'sike'}:${key}`"
    :class="{selected: key === show}")
      i.material-icons(@click="deletechain(key)") close
      span(@click="show = key") chain {{key}}
    md-button(@click="init_addDevice('chain', data.data.length)").md-icon-button.md-dense
      md-icon add
  template(v-if="data.data.length > 0")
    transition(name="device")
      chain(@update="update" :key="`group>chain:${show}`" v-if="data.data[show]" :chain="data.data[show]" @addDevice="addDevice" :index="show")
  .addgoup(v-else).lonely
    md-button(@click="init_addDevice('chain', 0)").md-icon-button.md-dense
      md-icon add
</template>

<script>
export default {
  name: "group",
  props: {
    data: {
      type: Object,
      required: true,
    },
  },
  data: () => ({
    show: 0,
  }),
  methods: {
    init_addDevice(device, index) {
      console.log("addDevice", { path: "", device, index })
      this.$emit("addDevice", { path: "", device, index })
      this.show = index
    },
    addDevice({ path, device, index }) {
      this.$emit("addDevice", {
        path: `/chain:${this.show}${path}`,
        device,
        index,
      })
    },
    update({ path, data }) {
      this.$emit("update", {
        // console.log("update", {
        path: `/chain:${this.show}${path}`,
        data,
      })
    },
    deletechain(index) {
      if (this.show === index) this.show = 0
      this.$emit("update", {
        path: "",
        data: {
          type: "remove",
          index: index,
        },
      })
    },
  },
}
</script>

<style lang="scss">
.group {
  display: flex;
  height: 100%;
  > .chain {
    &.device-enter-active,
    &.device-leave-active {
      transition: 0.3s var(--ease);
      > .device {
        transition: 0.3s var(--ease);
      }
    }
    &.device-enter,
    &.device-leave-to {
      margin: 0;
      > .device,
      > .add {
        opacity: 0;
        width: 0;
        box-shadow: none;
      }
    }
  }
  > .addgoup {
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
      width: 32px;
      opacity: 0.5;
      height: unset;
    }
    &:hover {
      opacity: 1;
      width: 40px;
    }
  }
  > .chains {
    padding: 10px;
    // width: 7em;
    > div {
      // color: rgba(255, 255, 255, 0.25);
      opacity: 0.25;
      transition: 0.3s;
      transform: none;
      display: flex;
      justify-content: center;
      align-items: center;
      > i {
        font-size: 0;
        opacity: 0;
        transition: 0.3s;
        transition-delay: 0s;
      }
      &:hover {
        > i {
          font-size: 1em;
          opacity: 0.5;
          transition-delay: 0.25s;
        }
      }
      > span {
        cursor: pointer;
      }
      &.selected {
        transform: translateX(2.5px);
        // color: rgba(255, 255, 255, 0.5);
        opacity: 0.5;
      }
    }
  }
  // width: 100%;
}
</style>
