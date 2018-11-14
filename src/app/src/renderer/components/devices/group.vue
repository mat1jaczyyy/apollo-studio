<template lang="pug">
.group
  .chains
    div(v-for="(member, key) in data" :key="`group>${data[show].object}:${key}`"
    :class="{selected: key === show}" @click="show = key") chain {{key}}
  //- chain(v-for="(member, key) in data" :key="`group>${member.object}:${key}`" :chain="member")
  chain(:chain="data[show]" @addDevice="addDevice")
</template>

<script>
export default {
  name: "group",
  props: {
    data: {
      type: Array,
      required: true,
    },
  },
  data: () => ({
    show: 0,
  }),
  methods: {
    addDevice() {

    }
  }
}
</script>

<style lang="scss">
.group {
  display: flex;
  height: 100%;
  > .chains {
    padding: 10px;
    > div {
      color: rgba(255, 255, 255, 0.25);
      transition: 0.3s;
      transform: none;
      &.selected {
        transform: translateX(2.5px);
        color: rgba(255, 255, 255, 0.5);
      }
    }
  }
  // width: 100%;
}
</style>
