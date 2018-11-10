<template lang="pug">
main.delay
  .duration
    h4 duration
    dial(v-if="!sync" :value.sync="delay" :exponent="5.9068906" :size="50" :width="7" :min="10" :max="30000" @rclick="toggle" :color="$store.state.themes[$store.state.settings.theme].dial1")
    dial(v-else :holdfor="50" :color="$store.state.themes[$store.state.settings.theme].dial2" :value.sync="step" :size="50" :width="7" :min="0" :max="steps.length - 1" @rclick="toggle")
    .values(v-if="!sync")
      a(@click="delay += -100") -
      md-field
        md-input(@input.native="delay = Number($event.target.value) || 0" :value="delay")
      a(@click="delay += 100") +
    .durationvalues.values(v-else).sync
      a(@click="step += -1") -
      md-field
        md-input(@input.native="step = Number($event.target.value) || 0" :value="steps[step]" disabled)
      a(@click="step += 1") +
  .gate
    h4 gate
    dial(:value.sync="gate" :exponent="2" :size="50" :width="7" :min="0" :max="400" :decimals="1" :color="$store.state.themes[$store.state.settings.theme].dial1")
    .values
      a(@click="gate += -12.5") -
      md-field
        md-input(@input.native="gate = Number($event.target.value) || 0" :value="gate")
      a(@click="gate += 12.5") +
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
    this.delay = this.data.length
    this.gate = this.data.gate * 100
  },
  data: () => ({
    delay: 0,
    step: 7,
    gate: 0,
    sync: false,
    steps: ["1/128", "1/64", "1/32", "1/16", "1/8", "1/4", "1/2", "1", "2", "4"],
  }),
  watch: {
    delay(n) {
      let self = this
      if (self.sync) {
        if (self.delay > self.steps.length - 1) self.delay = self.steps.length - 1
        else if (self.delay < 0) self.delay = 0
      } else {
        if (self.delay > self.max) self.delay = self.max
        else if (self.delay < self.min) self.delay = self.min
      }
    },
  },
  methods: {
    toggle() {
      this.sync = !this.sync
    },
  },
}
</script>

<style lang="scss">
main.delay {
  display: flex;
  justify-content: center;
  align-items: center;
  flex-direction: column;
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
