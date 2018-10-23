<template lang="pug">
main
  .x
    h4 x translation
    dial(:value.sync="x" :size="50" :width="5" :min="min" :max="max" :holdfor="15")
    .values
      a(@click="x += -1") -
      md-field
        md-input(@input.native="x = Number($event.target.value) || 0" :value="x")
      a(@click="x += 1") +
  .y
    h4 y translation
    dial(:value.sync="y" :size="50" :width="5" :min="min" :max="max" :holdfor="15")
    .values
      a(@click="y += -1") -
      md-field
        md-input(@input.native="y = Number($event.target.value) || 0" :value="y")
      a(@click="y += 1") +
</template>

<script>
import dial from "../ui/dial"
export default {
  components: { dial },
  data: () => ({
    x: 0,
    y: 0,
    min: -8,
    max: 8,
  }),
}
</script>

<style lang="scss">
.x,
.y {
  display: flex;
  justify-content: center;
  align-items: center;
  flex-direction: column;
  margin-bottom: 5px;
  .values {
    display: flex;
    justify-content: center;
    align-items: center;
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
      text-decoration: none;
    }
  }
  circle {
    transition: 0.3s;
  }
}
</style>
