<template lang="pug">
svg(xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' viewbox='0 0 200 200' width="200" height="200").launchpad
  g(data-name='Group 5' style="transform: scale(.22)")
    rect(data-name='Rectangle 2' width='909' height='909' rx='20' fill="#242424")
    g(data-name='Group 3' transform='translate(-62 -29)')
      rect.lpbtn(@mousedown="md" @mouseup="mu" @mouseleave="mu" v-for="(rect, key) in rects" width='69' height='69' rx='4' :transform='`translate(${rect})`' :class="'key' + key" fill="#696969")
      path.lpbtn(@mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969" d='M4,0H66a4,4,0,0,1,4,4V52.138C64.674,57.341,58.705,63.769,52.889,69H4a4,4,0,0,1-4-4V4A4,4,0,0,1,4,0Z' transform='translate(439 407)')
      path.lpbtn(@mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969" d='M3.943,0H65.057A4,4,0,0,1,69,4.058V52.893C63.75,58.172,57.867,64.693,52.134,70H3.943A4,4,0,0,1,0,65.942V4.058A4,4,0,0,1,3.943,0Z' transform='translate(593 407) rotate(90)')
      path.lpbtn(@mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969" d='M3.971,0H65.529A3.986,3.986,0,0,1,69.5,4V52.138C64.212,57.341,58.286,63.769,52.512,69H3.971A3.986,3.986,0,0,1,0,65V4A3.986,3.986,0,0,1,3.971,0Z' transform='translate(592.5 560) rotate(180)')
      path.lpbtn(@mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969" d='M3.943,0H65.057A4,4,0,0,1,69,4.058V52.893C63.75,58.172,57.867,64.693,52.134,70H3.943A4,4,0,0,1,0,65.942V4.058A4,4,0,0,1,3.943,0Z' transform='translate(439 560) rotate(-90)')
    g(transform='translate(-62 -29)')
      g(transform='translate(194 78)')
        g(transform='translate(-194 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(-110 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(-26 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(58 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(142 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(226 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(310 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(394 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
      g(transform='translate(194 833)')
        g(transform='translate(-194 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(-110 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(-26 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(58 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(142 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(226 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(310 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(394 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
      g(transform='translate(165 162) rotate(90)')
        g(transform='translate(-194 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(-110 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(-26 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(58 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(142 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(226 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(310 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(394 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
      g(transform='translate(923 162) rotate(90)')
        g(transform='translate(-194 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(-110 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(-26 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(58 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(142 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(226 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(310 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")
        g(transform='translate(394 -77)')
          circle.lpbtn(cx='28' cy='28' r='28' transform='translate(194 77)' @mousedown="md" @mouseup="mu" @mouseleave="mu" fill="#696969")

</template>

<script>
const zero255 = () => Math.floor(Math.random() * 255)
export default {
  data: () => ({
    rects: [
      "187 155",
      "271 155",
      "355 155",
      "439 155",
      "523 155",
      "607 155",
      "691 155",
      "775 155",
      "187 239",
      "271 239",
      "355 239",
      "439 239",
      "523 239",
      "607 239",
      "691 239",
      "775 239",
      "187 323",
      "271 323",
      "355 323",
      "439 323",
      "523 323",
      "607 323",
      "691 323",
      "775 323",
      "187 407",
      "271 407",
      "355 407",
      "607 407",
      "691 407",
      "775 407",
      "187 491",
      "271 491",
      "355 491",
      "607 491",
      "691 491",
      "775 491",
      "187 575",
      "271 575",
      "355 575",
      "439 575",
      "523 575",
      "607 575",
      "691 575",
      "775 575",
      "187 659",
      "271 659",
      "355 659",
      "439 659",
      "523 659",
      "607 659",
      "691 659",
      "775 659",
      "187 743",
      "271 743",
      "355 743",
      "439 743",
      "523 743",
      "607 743",
      "691 743",
      "775 743",
    ],
  }),
  methods: {
    md(e) {
      e.target.style.fill = `rgb(${zero255()}, ${zero255()}, ${zero255()})`
    },
    mu(e) {
      e.target.style.fill = `#696969`
    },
  },
}
</script>


<style lang="scss">
.cls-1 {
  fill: none;
}
.cls-2 {
  fill: #242424;
}
.cls-3 {
  fill: #696969;
}
.cls-4 {
  clip-path: url(#clip-path);
}
.lpbtn {
  transition: .3s;
}
</style>
