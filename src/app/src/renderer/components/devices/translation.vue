<template lang="pug">
main
  .offset
    h4 offset translation
    dial(:value.sync="offset" :size="50" :width="7" :min="min" :max="max" :overflow="true" :color="$store.state.themes[$store.state.settings.theme].dial1")
    .values
      a(@click="offset += -1") -
      md-field
        md-input(@input.native="offset = Number($event.target.value) || 0" :value="offset")
      a(@click="offset += 1") +
</template>

<script>
export default {
  name: "Translation",
  data: () => ({
    offset: 0,
    min: -99,
    max: 99,
  }),
}
</script>

<style lang="scss">
.offset {
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
