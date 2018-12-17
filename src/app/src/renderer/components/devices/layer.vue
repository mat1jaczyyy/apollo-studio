<template lang="pug">
.layer
  .target
    h4 target
    dial(:value.sync="target" :exponent="1" :size="50" :width="7" :min="-69" :max="69" :decimals="1" :color="$store.state.themes[$store.state.settings.theme].dial1")
    .values
      a(@click="target += -1") -
      md-field
        md-input(@input.native="target = Number($event.target.value) || 0" :value="target")
      a(@click="target += 1") +
</template>

<script>
import throttle from "lodash/throttle"
export default {
  name: "layer",
  props: {
    data: {
      type: Object,
      required: true
    }
  },
  created() {
    console.log(this.data)
    this.target = this.data.data.target
  },
  data: () => ({
    step: 7,
    target: 0
  }),
  watch: {
    target(n) {
      if (this.target !== this.data.data.target)
        this.update("target", this.target)
    }
  },
  methods: {
    update: throttle(function(type, value) {
      // console.log("update", {
      this.$emit("update", {
        path: "",
        data: {
          type,
          value
        }
      })
    }, 100)
  }
}
</script>

<style lang="scss">
.layer {
  justify-content: center;
  align-items: center;
  > .target {
    display: flex;
    align-items: center;
    flex-direction: column;
    // > .dial {
    //   margin-bottom: -5px;
    // }
    > .values {
      display: flex;
      justify-content: center;
      align-items: center;
      &.sync input {
        user-select: none;
      }
    }
  }
  a {
    cursor: pointer;
    color: rgba(255, 255, 255, 0.25);
    margin: 0 5px;
    text-decoration: none !important;
  }
}
</style>
