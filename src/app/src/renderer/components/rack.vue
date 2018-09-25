<template lang="pug">
.rack
  Container(@drop="onDrop" orientation="horizontal" lock-axis="x" :animation-duration="500" drag-class="sh" drag-handle-selector=".drag").rackWrap
    //- Draggable(v-for="item in items" :key="item.id").rackItem
      .inner
        .frame
          h6.title {{item.data}}
          a(@click="remove(item.id)")
            i.material-icons close
        .content
    Draggable.rackItem
      .inner
        .frame
          h6.title launchpad
          a.drag
            i.material-icons drag_handle
        .content
          launchpad
  //- dial(:value="dial1" @update:value="dial1 += $event" size="80px")
  //- dial(:value="dial2" @update:value="dial2 += $event" color="#FFB532")
  //- dial(:value="dial3" @update:value="dial3 += $event" color="#FFF" size="50px")
</template>

<script>
import { Container, Draggable } from "vue-smooth-dnd"
import { remote } from "electron"
import dial from "../ui/dial"
import launchpad from "../ui/launchpad"

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
  components: { Container, Draggable, dial, launchpad },
  data: () => ({
    dial1: 60,
    dial2: 33,
    dial3: 86,
    window: remote.getCurrentWindow(),
    items: generateItems(2, i => ({ id: i, data: "panel " + i })),
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
  display: flex;
  justify-content: center;
  align-items: center;
  .rackWrap {
    height: 100%;
    display: flex;
    .rackItem {
      height: calc(100% - 8px);
      width: 225px;
      box-shadow: none;
      // transition: box-shadow .3s;
      .inner {
        overflow: hidden;
        border-radius: 5px 5px 0 0;
        height: 100%;
        background: #3a3a3a;
        box-shadow: 1px 1px 15px -4px rgba(0, 0, 0, 0.25);
        margin: 4px 0;
        margin-left: 4px;
        transition: box-shadow 0.3s;
        position: relative;
        .content {
          position: relative;
          display: flex;
          justify-content: center;
          align-items: center;
          height: calc(100% - 20px);
        }
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
            &.drag {
              cursor: -webkit-grab;
            }
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
    .rackItem > .sh {
      transition: all 0.3s;
      box-shadow: 1px 1px 15px -2.5px rgba(0, 0, 0, 0.5);
      border: none;
    }
  }
}
</style>
