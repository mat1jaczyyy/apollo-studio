<template lang="pug">
.paint
  .high
    h4 color
    .color
      .inner(:style="{background: high}" @click="color")
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
  created() {
    const self = this,
      color = self.data.data.color.data,
      hex = self.rgbToHex({
        red: Math.round(color.red / self.factor),
        green: Math.round(color.green / self.factor),
        blue: Math.round(color.blue / self.factor),
      })
    self.high = hex
  },
  name: "apollo-paint",
  data: () => ({
    high: "#fff",
    // low: {
    //   r: 255,
    //   g: 255,
    //   b: 255,
    // },
    factor: 63 / 255,
  }),
  watch: {
    high(n) {
      if (!n) return
      const self = this
      const rgb = self.hexToRgb(n)
      self.$emit("update", {
        path: "",
        data: {
          type: "color",
          red: Math.round(rgb[0] * self.factor),
          green: Math.round(rgb[1] * self.factor),
          blue: Math.round(rgb[2] * self.factor),
        },
      })
    },
  },
  methods: {
    color() {
      const self = this
      const org = self.high
      const call = e => {
        if (e) self.high = e.hex
      }
      getColor({ org, call })
    },
  },
}
</script>

<style lang="scss">
.paint {
  // display: none;
  display: flex;
  flex-direction: column;
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
