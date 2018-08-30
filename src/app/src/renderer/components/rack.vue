<template lang="pug">
.rack
  Container(@drop="onDrop" orientation="horizontal" lock-axis="x" :animation-duration="500").rackWrap
    Draggable(v-for="item in items" :key="item.id").rackItem
      .inner
        .frame
          h6.title {{item.data}}
          a(@click="remove(item.id)")
            i.material-icons close
        .content
</template>

<script>
import { Container, Draggable } from "vue-smooth-dnd"
import { remote } from "electron"

const applyDrag = (arr, dragResult) => {
  const { removedIndex, addedIndex, payload } = dragResult
  if (removedIndex === null && addedIndex === null) return arr

  const result = [...arr]
  let itemToAdd = payload

  if (removedIndex !== null) {
    itemToAdd = result.splice(removedIndex, 1)[0]
  }

  if (addedIndex !== null) {
    result.splice(addedIndex, 0, itemToAdd)
  }

  return result
}

const generateItems = (count, creator) => {
  const result = []
  for (let i = 0; i < count; i++) {
    result.push(creator(i))
  }
  return result
}

export default {
  components: { Container, Draggable },
  data: () => ({
    window: remote.getCurrentWindow(),
    items: generateItems(3, i => ({ id: i, data: "panel " + i })),
  }),
  methods: {
    onDrop: function(dropResult) {
      this.items = applyDrag(this.items, dropResult)
    },
    remove: function(id) {
      this.items.splice(id, 1)
    },
  },
}
</script>

<style lang="scss" scoped>
.rack {
  .rackWrap {
    height: 100%;
    display: flex;
    .rackItem {
      height: calc(100% - 8px);
      width: 225px;
      .inner {
        overflow: hidden;
        border-radius: 5px 5px 0 0;
        height: 100%;
        background: #414141;
        margin: 4px 0;
        margin-left: 4px;
        .frame {
          display: flex;
          height: 20px;
          padding: 0 4px;
          background: #515151;
          position: relative;
          h6 {
            font-size: 12px;
            line-height: 20px;
          }
          a {
            position: absolute;
            height: 20px;
            right: 4px;
            display: flex;
            justify-content: center;
            align-items: center;
            cursor: pointer;
            i {
              font-size: 20px;
              // transition: 0.5s;
              // &:hover {
              //   color: #d03333;
              // }
            }
          }
        }
      }
    }
  }
}
</style>
