<template lang="pug">
main
  //- .x
  //-   h4 x translation
  //-   dial(:initialValue="0" @update:value="x = $event" :size="50" :width="5" :min="-8" :max="8")
  //-   p {{x}}
  .y
    h4 y translation
    dial(:initialValue="0" @update:value="y = $event" :size="50" :width="5" :min="0" :max="100")
    p {{y}}
</template>

<script>
import dial from "../ui/dial"
export default {
  components: { dial },
  data: () => ({
    x: 0,
    y: 0,
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
  h4 {
    margin-bottom: 15px;
  }
}
</style>
