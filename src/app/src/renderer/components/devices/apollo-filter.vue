<template lang="pug">
.filter
  launchpad
</template>

<script>
export default {
  props: {
    data: {
      type: Object,
      required: true,
    },
  },
  name: "apollo-filter",
}
</script>

<style lang="scss">
.filter {
  display: flex;
  > .launchpad {
    margin: 0 10px;
  }
}
</style>
