<template lang="pug">
.translation
  .offset
    h4 offset
    dial(:value.sync="offset" :exponent="1" :size="50" :width="7" :min="-99" :max="99" :decimals="1" :color="$store.state.themes[$store.state.settings.theme].dial1")
    .values
      a(@click="offset += -1") -
      md-field
        md-input(@input.native="offset = Number($event.target.value) || 0" :value="offset")
      a(@click="offset += 1") +
</template>

<script>
import throttle from "lodash/throttle"
export default {
  name: "translation",
  props: {
    data: {
      type: Object,
      required: true
    }
  },
  created() {
    this.offset = this.data.data.offset
  },
  data: () => ({
    step: 7,
    offset: 0
  }),
  watch: {
    offset(n) {
      if (this.offset !== this.data.data.offset)
        this.update("offset", this.offset)
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
.translation {
  justify-content: center;
  align-items: center;
  > .offset {
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
