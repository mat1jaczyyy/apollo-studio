<template lang="pug">
.dial(:class="locked")
  svg(:width="size + 1" :height="size + 1" :viewBox="`0 0 ${size + 1} ${size + 1}`" @mousedown="md" @mouseup="mu")
    circle(:cx="size / 2" :cy="size / 2" :r="radius" fill="none" stroke="rgba(0,0,0,.125)" :stroke-width="width")
    circle(:cx="size / 2" :cy="size / 2" :r="radius" fill="none" :stroke="color" :stroke-width="width"
           :stroke-dasharray="circumference" :stroke-dashoffset="offset")
</template>

<script>
export default {
  props: {
    initialValue: {
      type: Number,
      default: 0,
    },
    min: {
      type: Number,
      default: 0,
    },
    max: {
      type: Number,
      default: 100,
    },
    color: {
      type: String,
      default: "#0288d1",
    },
    width: { type: Number, default: 7 },
    size: { type: Number, default: 50 },
  },
  data() {
    return {
      value: this.initialValue,
      locked: false,
    }
  },
  watch: {
    value(n) {
      this.$emit("update:value", n)
    },
  },
  mounted() {
    document.addEventListener("pointerlockchange", this.lockchange)
  },
  beforeDestroy() {
    document.removeEventListener("pointerlockchange", this.lockchange)
  },
  methods: {
    md(e) {
      e.target.requestPointerLock()
      this.locked = true
    },
    mu(e) {
      document.exitPointerLock()
    },
    lockchange() {
      if (this.locked)
        if (document.pointerLockElement !== null) document.addEventListener("mousemove", this.mmve)
        else if (document.pointerLockElement === null) {
          document.removeEventListener("mousemove", this.mmve)
          this.locked = false
        }
    },
    mmve(e) {
      let mv = this.value + e.movementY / -2
      if (mv >= this.min && mv <= this.max) {
        this.value = mv
      }
    },
  },
  computed: {
    radius() {
      return this.size / 2 - this.width / 2
    },
    circumference() {
      return 2 * Math.PI * this.radius
    },
    offset() {
      return this.circumference * (1 - this.value / this.max)
    },
  },
}
</script>

<style lang="scss">
.dial > svg {
  transform: rotate(90deg);
  // text {
  //   transform: rotate(-90deg);
  // }
}
</style>
