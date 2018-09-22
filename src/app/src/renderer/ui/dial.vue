<template lang="pug">
.c100(:class="{p50: value > 50}" @mousedown="mdown" @mouseup="mup")
  span {{value}}
  .slice
    .bar(:style="{transform: `rotate(${value * 3.6}deg)`}")
    .fill
</template>

<script>
export default {
  props: {
    value: { type: Number, default: 69 },
    min: { type: Number, default: 0 },
    max: { type: Number, default: 100 },
  },
  created() {
    this.v = this.value
  },
  data: () => ({
    acceptLock: false,
    v: 0,
  }),
  methods: {
    mdown(e) {
      this.acceptLock = true
      document.addEventListener("pointerlockchange", this.lockChangeAlert)
      e.target.requestPointerLock()
    },
    mup(e) {
      console.log("mup")
      document.exitPointerLock()
      document.removeEventListener("mouseup", this.mup)
      document.removeEventListener("mousemove", this.updatePosition)
    },
    lockChangeAlert(e) {
      // console.log(e)
      if (!this.acceptLock) return
      else this.acceptLock = false
      document.addEventListener("mousemove", this.updatePosition)
      document.addEventListener("mouseup", this.mup)
    },
    updatePosition(e) {
      let mv = this.value + e.movementY / -2
      // console.log(mv)
      if (document.pointerLockElement !== null && mv >= this.min && mv <= this.max)
        this.$emit("update:value", e.movementY / -2)
      else if (document.pointerLockElement === null)
        document.removeEventListener("mousemove", this.updatePosition)
    },
  },
}
</script>


<style lang="scss">
$circle-width: 0.05em;
$circle-width-hover: 0.03em;
$primary-color: #6dd7ff;
$secondary-color: #282828;
$bg-color: #414141;

.pie {
  position: absolute;
  border: $circle-width solid $primary-color;
  width: 1 - (2 * $circle-width);
  height: 1 - (2 * $circle-width);
  clip: rect(0em, 0.5em, 1em, 0em);
  border-radius: 50%;
  transform: rotate(0deg);
}

.pie-fill {
  transform: rotate(180deg);
}

.rect-auto {
  clip: rect(auto, auto, auto, auto);
}

.c100 {
  *,
  *:before,
  *:after {
    box-sizing: content-box;
  }
  position: relative;
  font-size: 75px;
  width: 1em;
  height: 1em;
  margin: 5px;
  border-radius: 50%;
  float: left;
  background-color: $secondary-color;
  > span {
    position: absolute;
    width: 100%;
    z-index: 1;
    left: 0;
    top: 0;
    width: 5em;
    line-height: 5em;
    font-size: 0.2em;
    color: #606060;
    display: block;
    text-align: center;
    white-space: nowrap;
    transition-property: all;
    transition-duration: 0.2s;
    transition-timing-function: ease-out;
  }
  &:after {
    position: absolute;
    top: $circle-width;
    left: $circle-width;
    display: block;
    content: " ";
    border-radius: 50%;
    background-color: $bg-color;
    width: 1 - (2 * $circle-width);
    height: 1 - (2 * $circle-width);
    transition-property: all;
    transition-duration: 0.2s;
    transition-timing-function: ease-in;
  }
  .slice {
    position: absolute;
    width: 1em;
    height: 1em;
    clip: rect(0em, 1em, 1em, 0.5em);
  }
  .bar {
    @extend .pie;
  }
  &.p50 .slice {
    clip: rect(auto, auto, auto, auto);
  }
  &.p50 .bar:after {
    @extend .pie-fill;
  }
  &.p50 .fill {
    @extend .pie;
    @extend .pie-fill;
  }
  &:hover {
    cursor: default;
    > span {
      width: 3.33em;
      line-height: 3.33em;
      font-size: 0.3em;
      color: $primary-color;
    }
    &:after {
      top: $circle-width-hover;
      left: $circle-width-hover;
      width: 1 - (2 * $circle-width-hover);
      height: 1 - (2 * $circle-width-hover);
    }
  }
}
</style>
