<template lang="pug">
.delay
  .duration
    h4 Duration
    dial(v-if="!sync" :value.sync="duration" :exponent="5.9068906" :size="50" :width="7" :min="10" :max="30000" @rclick="toggle" :color="$store.state.themes[$store.state.settings.theme].dial1")
    dial(v-else :steps="steps.length" :color="$store.state.themes[$store.state.settings.theme].dial2" :value.sync="step" :size="50" :width="7" :min="0" :max="steps.length - 1" @rclick="toggle")
    .values(v-if="!sync")
      a(@click="duration += -100") -
      md-field
        md-input(@input.native="duration = Number($event.target.value) || 0" :value="duration")
      a(@click="duration += 100") +
    .durationvalues.values(v-else).sync
      a(@click="step += -1") -
      md-field
        md-input(@input.native="step = Number($event.target.value) || 0" :value="steps[step]" disabled)
      a(@click="step += 1") +
  .gate
    h4 Gate
    dial(:value.sync="gate" :exponent="2" :size="50" :width="7" :min="0" :max="400" :decimals="1" :color="$store.state.themes[$store.state.settings.theme].dial1")
    .values
      a(@click="gate += -12.5") -
      md-field
        md-input(@input.native="gate = Number($event.target.value) || 0" :value="gate")
      a(@click="gate += 12.5") +
</template>

<script>
import throttle from "lodash/throttle"
export default {
  name: "Delay",
  props: {
    data: {
      type: Object,
      required: true,
    },
  },
  created() {
    this.duration = this.data.data.length
    this.gate = this.data.data.gate * 100
  },
  data: () => ({
    duration: 0,
    step: 7,
    gate: 0,
    sync: false,
    steps: ["1/128", "1/64", "1/32", "1/16", "1/8", "1/4", "1/2", "1", "2", "4"],
  }),
  watch: {
    duration(n) {
      let self = this
      if (self.sync) {
        if (self.duration > self.steps.length - 1) self.duration = self.steps.length - 1
        else if (self.duration < 0) self.duration = 0
      } else {
        if (self.duration > self.max) self.duration = self.max
        else if (self.duration < self.min) self.duration = self.min
      }
      if (self.duration !== self.data.data.length) self.update("length", self.duration)
    },
    gate(n) {
      if (this.gate / 100 !== this.data.data.gate) this.update("gate", this.gate / 100)
    },
  },
  methods: {
    toggle() {
      this.sync = !this.sync
    },
    update: throttle(function(type, value) {
      console.log("update")
      this.$emit("update", {
        path: "",
        type,
        value,
      })
    }, 100),
  },
}
</script>

<style lang="scss">
main.delay {
  justify-content: center;
  align-items: center;
  .duration {
    margin-bottom: 10px;
  }
  > .duration,
  .gate {
    display: flex;
    align-items: center;
    flex-direction: column;
    // > .dial {
    //   margin-bottom: -5px;
    // }
  }
  .values {
    display: flex;
    justify-content: center;
    align-items: center;
    &.sync input {
      user-select: none;
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
