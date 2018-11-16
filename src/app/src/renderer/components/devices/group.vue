<template lang="pug">
.group
  .chains(v-if="data.data.length > 0")
    div(v-for="(member, key) in data.data" :key="`group>${data.data[show].object}:${key}`"
    :class="{selected: key === show}" @click="show = key") chain {{key}}
    md-button(@click="init_addDevice('chain', data.data.length)").md-icon-button.md-dense
      md-icon add
  chain(v-if="data.data.length > 0" @update="update" :key="`group>chain:${show}`" :chain="data.data[show]" @addDevice="addDevice" :index="show")
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
    },
    addDevice({ path, device, index }) {
      this.$emit("addDevice", { path: `/chain:${this.show}${path}`, device, index })
    },
    update({ path, type, value }) {
      this.$emit("update", {
      // console.log("update", {
        path: `/chain:${this.show}${path}`,
        type,
        value,
      })
    },
  },
}
</script>

<style lang="scss">
.group {
  display: flex;
  height: 100%;
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
    > div {
      // color: rgba(255, 255, 255, 0.25);
      opacity: 0.25;
      transition: 0.3s;
      transform: none;
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
