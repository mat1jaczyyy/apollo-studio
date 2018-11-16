<template lang="pug">
.paint
  .high
    h4 color
    .color
      .inner(:style="{background: `rgb(${high.r}, ${high.g}, ${high.b})`}" @click="color")
  //- .low
  //-   h4 low
  //-   .color
  //-     .inner(:style="{background: `rgb(${low.r}, ${low.g}, ${low.b})`}")
</template>

<script>
export default {
  props: {
    data: {
      type: Object,
      required: true,
    },
  },
  name: "apollo-paint",
  data: () => ({
    high: {
      r: 255,
      g: 255,
      b: 255,
    },
    low: {
      r: 255,
      g: 255,
      b: 255,
    },
  }),
  methods: {
    color() {
      let self = this
      getColor(this.high).then(e => (self.high = e))
    },
  },
}
</script>

<style lang="scss">
.paint {
  display: none;
  > .high {
    display: flex;
    justify-content: center;
    align-items: center;
    flex-direction: column;
    padding: 0 10px;
    > .color {
      padding: 10px;
      > .inner {
        cursor: pointer;
        height: 50px;
        width: 50px;
        border-radius: 50%;
        box-shadow: 2px 2px 15px 0 rgba(0, 0, 0, 0.25);
      }
    }
  }
}
</style>
