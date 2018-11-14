<template lang="pug">
.device
  .frame
    h5 {{data.data.device}}
  .inner
    component(:is="data.data.device" :data="data.data.data")
</template>

<script>
export default {
  name: "device",
  props: {
    data: {
      type: Object,
      required: true,
    },
  },
}
</script>

<style lang="scss">
.device {
  height: calc(100% - 5px);
  min-width: 100px;
  position: relative;
  box-shadow: 0 0 0 1px rgba(0, 0, 0, 0.25);
  margin: 2.5px 5px;
  border-radius: 5px;
  overflow: hidden;
  > .frame {
    height: 1.25em;
    width: 100%;
    display: flex;
    justify-content: center;
    align-items: center;
    background: #222222;
  }
  > .inner {
    height: calc(100% - 1.25em);
    width: 100%;
    background: #1d1d1d;
  }
}
</style>
