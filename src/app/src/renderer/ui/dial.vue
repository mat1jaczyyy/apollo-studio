<template lang="pug">
.dial(:class="{focus}")
  svg(:width="size + 1" :height="size + 1" :viewBox="`0 0 ${size + 1} ${size + 1}`" @mousedown="md" @mouseup="mu")
    circle(:cx="size / 2" :cy="size / 2" :r="radius" fill="none" stroke="rgba(0,0,0,.125)" :stroke-width="width").bg
    circle(:cx="size / 2" :cy="size / 2" :r="radius" fill="none" :stroke="color" :stroke-width="width"
           :stroke-dasharray="circumference" :stroke-dashoffset="offset")
</template>

<script>
export default {
  props: {
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
    value: { type: Number, default: 0 },
    holdfor: { type: Number, default: 5 },
  },
  data() {
    return {
      hold: 0,
      locked: false,
      focus: false,
      initial: this.value,
    }
  },
  watch: {
    value(n) {
      if (n > this.max) this.$emit("update:value", this.max)
      else if (n < this.min) this.$emit("update:value", this.min)
    },
    focus(n, o) {
      let self = this
      const arrowKeys = e => {
        let mv = e.ctrlKey ? 5 : 1
        if (e.shiftKey) mv = mv * 2
        if ((self.value + mv <= self.max && e.keyCode === 38) || e.keyCode === 39)
          self.$emit("update:value", self.value + mv)
        else if ((self.value + mv / -1 >= self.min && e.keyCode === 40) || e.keyCode === 37)
          self.$emit("update:value", self.value + mv / -1)
      }
      if (n && !o) document.onkeydown = arrowKeys
      else if (!n && o) document.onkeydown = null
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
      let self = this
      e.target.requestPointerLock()
      self.locked = true
      self.focus = true
      const reset = () => self.$emit("update:value", 0)
      e.target.addEventListener("mousedown", reset)
      setTimeout(() => e.target.removeEventListener("mousedown", reset), 250)
    },
    mu(e) {
      let self = this
      document.exitPointerLock()
      const ocl = event => {
          if (!e.target.contains(event.target) && self.focus) self.focus = false
          rcl()
        },
        rcl = () => document.removeEventListener("mousedown", ocl)
      document.addEventListener("mousedown", ocl)
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
      let self = this
      if (self.hold < self.holdfor && self.hold > self.holdfor / -1) self.hold += e.movementY / -2
      else {
        if (self.hold > 0 && self.value + 1 <= self.max) self.$emit("update:value", self.value + 1)
        else if (self.hold < 0 && self.value - 1 >= self.min)
          self.$emit("update:value", self.value + -1)
        self.hold = 0
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
.dial {
  > svg {
    transform: rotate(90deg);
    > circle.bg {
      transition: 0.3s;
    }
  }
  &.focus {
    > svg > circle.bg {
      stroke: rgba(0, 0, 0, 0.25);
    }
  }
}
</style>
