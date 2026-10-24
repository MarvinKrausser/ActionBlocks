Optional audio files. Nothing in here is required: every sound effect and the default music are
synthesized in the browser (src/audio/synthSounds.ts and src/audio/songs.ts).

- Sound effects: put e.g. `line_clear.wav` here and register it in `soundFiles` in
  `src/theme/assets.ts` under its SoundId (`lineClear: asset('assets/sounds/line_clear.wav')`).
- Music: put tracks in `music/` and add them to `musicTracks` in `src/theme/assets.ts`
  (`{ id: 'my_track', title: 'My Track', kind: 'file', src: asset('assets/sounds/music/my_track.mp3') }`).

Any format the browser can decode works (wav, mp3, ogg). Only use files you have the rights to.
