<template lang="pug">
main.delay
  //- h4 delay
  dial(:value.sync="delay" :size="150" :width="10" :min="0" :max="60")
  //- p {{delay}}
  .values
    a(@click="delay += -1") -
    md-field
      md-input(@input.native="delay = Number($event.target.value) || 0" :value="delay")
    a(@click="delay += 1") +
  //- md-field
    md-input(v-model.number="delay")
</template>

<script>
import dial from "../ui/dial"
export default {
  components: { dial },
  data: () => ({
    delay: 0,
  }),
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
