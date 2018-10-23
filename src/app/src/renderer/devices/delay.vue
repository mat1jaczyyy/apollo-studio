<template lang="pug">
main.delay
  //- h4 delay
  dial(:initialValue="0" @update:value="delay = $event" :size="150" :width="10" :min="0" :max="60")
  p {{delay}}
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
  .md-field {
    margin: 0;
    .md-input {
      width: 3em;
      text-align: center;
    }
  }
  h4 {
    margin-bottom: 15px;
  }
}
</style>
