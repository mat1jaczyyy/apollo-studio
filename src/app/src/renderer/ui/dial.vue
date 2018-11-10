<template lang="pug">
.dial(:class="{focus}")
  svg(:width="size" :height="size" :viewBox="`0 0 ${size} ${size}`" @mousedown="md" @mouseup="mu")
    //- circle(:cx="size / 2" :cy="size / 2" :r="radius" fill="none" stroke="rgba(0,0,0,.125)" :stroke-width="width").bg
    //- circle(:cx="size / 2" :cy="size / 2" :r="radius" fill="none" :stroke="color" :stroke-width="width"
           :stroke-dasharray="circumference" :stroke-dashoffset="offset" :class="{locked}")
    path(:d="d" fill="none" stroke="rgba(0,0,0,.125)" :stroke-width="width").bg
    path(:d="d" fill="none" :stroke="color" :stroke-width="width"
      :stroke-dasharray="circumference" :stroke-dashoffset="offset / -1" :class="{locked}")
</template>

<script>
const polarToCartesian = (centerX, centerY, radius, angleInDegrees) => {
  const angleInRadians = ((angleInDegrees - 90) * Math.PI) / 180.0
  return {
    x: centerX + radius * Math.cos(angleInRadians),
    y: centerY + radius * Math.sin(angleInRadians),
  }
}

const describeArc = (x, y, radius, startAngle, endAngle) => {
  const start = polarToCartesian(x, y, radius, endAngle)
  const end = polarToCartesian(x, y, radius, startAngle)
  const largeArcFlag = endAngle - startAngle <= 180 ? "0" : "1"
  const d = ["M", start.x, start.y, "A", radius, radius, 0, largeArcFlag, 0, end.x, end.y].join(" ")

  return d
}

const scale = (v, min, max, e) => {
  return Math.pow((v - min) / (max - min), 1 / e)
}

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
    overflow: { type: Boolean, default: false },
    exponent: { type: Number, default: 1 },
  },
  data() {
    return {
      hold: 0,
      locked: false,
      focus: false,
      initial: this.value,
      // offset: 0,
    }
  },
  watch: {
    value(n) {
      if (!this.overflow)
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
      if (n && !o)
        setTimeout(() => {
          document.onkeydown = arrowKeys
        }, 50)
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
      if (e.button === 2) return self.$emit("rclick")
      self.focus = true
      self.locked = true
      if (self.$store.state.settings.dialPointerLock) {
        e.target.requestPointerLock()
      } else {
        const rm = () => {
          document.removeEventListener("mousemove", self.mmve)
          document.removeEventListener("mouseup", rm)
          self.locked = false
        }
        document.addEventListener("mousemove", self.mmve)
        document.addEventListener("mouseup", rm)
      }
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
      return (1 - 0.7 / 3.6) * (2 * Math.PI * this.radius)
    },
    offset() {
      return this.circumference * (1 - this.scaling)
    },
    d() {
      return describeArc(this.size / 2, this.size / 2, this.radius, -145, 145)
    },
    scaling() {
      return scale(this.value, this.min, this.max, this.exponent)
    },
  },
}
</script>

<style lang="scss">
.dial {
  margin-bottom: -10px;
  > svg {
    // transform: rotate(90deg);
    > path {
      transition: 0.3s;
      &.locked {
        // transition: 0.1s;
        transition: none;
      }
    }
  }
  &.focus {
    > svg > path {
      &.bg {
        stroke: rgba(0, 0, 0, 0.25);
      }
    }
  }
}
</style>
