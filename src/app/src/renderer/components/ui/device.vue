<template lang="pug">
.device
  component(:is="data.data.device" :data="data.data.data")
</template>

<script>
export default {
  name: "device",
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
