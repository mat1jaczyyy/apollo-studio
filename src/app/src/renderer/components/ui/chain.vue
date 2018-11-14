<template lang="pug">
.chain
  device(v-for="(device, key) in chain.data" v-if="key !== 'count'" :data="device" :key="`device>${device.data.device}:${key}`")
</template>

<script>
export default {
  name: "chain",
  props: {
    chain: {
      type: Object,
      required: true,
    }
  },
}
</script>


<style lang="scss">

</style>
