<template lang="pug">
.group
  chain(v-for="(member, key) in data" v-if="key !== 'count'"
  :key="`group>${member.object}:${key}`" :chain="member")
</template>

<script>
export default {
  name: "group",
  props: {
    data: {
      type: Object,
      required: true,
    }
  }
}
</script>


<style lang="scss">
</style>
