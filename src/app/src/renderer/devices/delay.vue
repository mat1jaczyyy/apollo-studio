<template lang="pug">
main.delay
  //- h4 delay
  .d
    dial(v-if="!sync" :value.sync="delay" :size="150" :width="10" :min="0" :max="60" @rclick="toggle")
    dial(v-else color="#FFB532" :value.sync="delay" :size="150" :width="10" :min="0" :max="steps.length - 1" @rclick="toggle")
  //- p {{delay}}
  .values(v-if="!sync")
    a(@click="delay += -1") -
    md-field
      md-input(@input.native="delay = Number($event.target.value) || 0" :value="delay")
    a(@click="delay += 1") +
  .values(v-else)
    p {{steps[delay]}}
  //- md-field
    md-input(v-model.number="delay")
</template>

<script>
import dial from "../ui/dial"
export default {
  components: { dial },
  data: () => ({
    delay: 0,
    sync: false,
    steps: ["1/128", "1/64", "1/32", "1/16", "1/8", "1/4", "1/2", "1", "2", "4"],
  }),
  methods: {
    toggle() {
      this.sync = !this.sync
      this.delay = this.sync ? 5 : 30
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
  .values {
    display: flex;
    justify-content: center;
    align-items: center;
  }
  .md-field {
    margin: 0;
    margin-top: -16px;
    .md-input {
      width: 3em;
      text-align: center;
    }
  }
  a {
    cursor: pointer;
    color: rgba(255, 255, 255, 0.25);
    margin: 0 5px;
    text-decoration: none !important;
  }
  h4 {
    margin-bottom: 15px;
  }
}
</style>
