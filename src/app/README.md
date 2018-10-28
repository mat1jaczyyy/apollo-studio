# apollo-studio UI todo
## general

- [ ] At any point in the chain between two adjacent devices, have a + icon pop up in the middle of the separator. Clicking it brings up a context menu with a list of devices that the user can add into that position in the chain by clicking on them.

## devices

* - [ ] Delay: Very similar to the Max for Live version of Delay (from the Outbreak set).
  * Duration dial with Sync or Free toggle option. Free specifies milliseconds from 0 to 60000, Sync specifies time value ranging in: 1/128, 1/64, 1/32, 1/16, 1/8, 1/4, 1/2, 1, 2, 4
  * Gate dial. Specifies percentage ranging from 0% to 400%.

* - [ ] Duplication: Very similar to the Ableton Chord.
  * List of elements (2 dials) that effectively act as a Translation. X translation dial and Y translation dial per list element.
  * \+ and - buttons for Adding and Removing last entry from the List.

* - [ ] Fade: All new device, auto generates a smooth gradient fade between multiple colors
  * Gradient selector similar to attached image for Fade. One stop at the start and at the end (always present), and the user can add other stops by double clicking anywhere in the middle. The middle stops can be dragged left or right to adjust their timing. On the background, is the resulting gradient fade. On the Launchpad, the horizontal axis represents time.
  * Color selector (rgb and hsl) that changes color of currently selected stop on the Gradient.
  * Duration dial with Sync or Free toggle option. Free specifies milliseconds from 0 to 60000, Sync specifies time value ranging in: 1/32, 1/16, 1/8, 1/4, 1/2, 1, 2, 4

* - [ ] Filter: Since our Groups will not have keyzone selectors like Ableton's MIDI Effect Rack, I've settled on a Filter device instead that only lets signals from specific note indexes pass through.
  * Reuse the Launchpad grid you have (make sure to add the mode/side LED) and make the buttons toggle between an on and off state while pressing them. Holding Ctrl makes only that note active, and all other notes disabled.

* - [ ] Group: Contains a list of Chains similar to Ableton's MIDI Effect Rack.
  * Vertically scrolling list of chains, should have a 'Mute' button for each.
  * Allow creating a new Chain by double clicking at leftover empty space in the bottom, or right click to initialize the new chain with a device similar to the context menu when adding them.
  * Selecting a Chain should display it on the right side, "inside" the Group device, very similarly to Ableton's MIDI Effect Rack.
  * This implementation mostly copies Ableton's solution, I'd love to hear your thoughts on any ideas on how to improve the workflow here.

* - [ ] Hold: Similar to Ableton's Note Length, and has the same UI as Delay.
  * Duration dial with Sync or Free toggle option. Free specifies milliseconds from 0 to 60000, Sync specifies time value ranging in: 1/128, 1/64, 1/32, 1/16, 1/8, 1/4, 1/2, 1, 2, 4.
  * Gate dial. Specifies percentage ranging from 0% to 400%.
  * Toggle for Infinite mode, which means the note will never be released. Disable Gate and Duration controls if Infinite mode is active.

* - [ ] Layer: A simple device that changes each incoming signal's Layer (signals stack on top of each other natively, like layers in Photoshop, with their specified index. A higher index means it is positioned above a lower index).
  * No dial, just a number entry that selects the Layer. The index ranges from -100 to 100, with 0 being the default middle layer. Perhaps implement a vertical slider here?

* - [ ] Lightweight: MIDI file importer and player.
  * Dragging a .mid file on top of the device sends its path to the backend for loading
  * If the loading response from the backend was successful, a text label should update containing the currently loaded filename.

* - [ ] Paint: Similar to Ableton's Velocity, but without all the unnecessary features. When a note enters, each of its R, G and B parameters are scaled from the 0 - 63 range into the respective low - high ranges defined by our colors.
  * Contains two color selectors (can reuse the selector from Fade) - one for the low color, one for the high color.

* - [ ] Preview: displays the signals that pass through it and doesn't process them at all.
  * Reuse the Launchpad grid you have (add the mode/side LED). The buttons react, with each of them showing the colors they receive from the signals.
  * Clicking a button will tell the backend to send a signal from that device into the chain directly after it (so it can be used as a cheap input device). 

* - [ ] Translation: Similar to Ableton's Pitch, moves a signal around the grid.
  * X translation dial and Y translation dial.

For now, ignore any interactions with the backend, especially those where devices themselves listen to feedback from their backend counterparts (like Preview). Implement the prototype UIs first, and then we will 
