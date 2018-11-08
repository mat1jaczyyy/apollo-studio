<template lang="pug">
main
  .x
    h4 x translation
    dial(:value.sync="x" :size="50" :width="7" :min="min" :max="max" :holdfor="15" :overflow="true")
    .values
      a(@click="x += -1") -
      md-field
        md-input(@input.native="x = Number($event.target.value) || 0" :value="x")
      a(@click="x += 1") +
  .y
    h4 y translation
    dial(:value.sync="y" :size="50" :width="7" :min="min" :max="max" :holdfor="15" :overflow="true")
    .values
      a(@click="y += -1") -
      md-field
        md-input(@input.native="y = Number($event.target.value) || 0" :value="y")
      a(@click="y += 1") +
</template>

<script>
export default {
  data: () => ({
    x: 0,
    y: 0,
    min: -9,
    max: 9,
  }),
}
</script>

<style lang="scss">
.x {
  margin-bottom: 10px;
}
.x,
.y {
  display: flex;
  justify-content: center;
  align-items: center;
  flex-direction: column;
  .values {
    display: flex;
    justify-content: center;
    align-items: center;
    a {
      cursor: pointer;
      color: rgba(255, 255, 255, 0.25);
      margin: 0 5px;
      text-decoration: none;
    }
  }
}
</style>
