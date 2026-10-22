// The edit decision list: which part of each filmed shot goes where, and the music under it.
// Times are in seconds. `from` is the point inside the filmed shot where the clip starts.

export const FPS = 30;

export type Clip = {
  shot: string;
  from: number;
  dur: number;
  // how this clip begins
  in?: 'cut' | 'black' | 'paper' | 'flash';
};

export type Music = {file: string; at: number; from: number; dur: number; fadeIn?: number; fadeOut?: number; volume?: number};

export type Edit = {
  clips: Clip[];
  music: Music[];
  // seconds (on the timeline) where the key-art section starts, the logo lands, "PRÓXIMAMENTE" appears and the end card
  keyArtAt: number;
  logoAt: number;
  comingSoonAt: number;
  endCardAt: number;
  total: number;
};

const O = 'music/orchestral.mp3';
const A = 'music/arcade.mp3';

// ---------------------------------------------------------------- horizontal (16:9), about 81 s
// 0.0  cold open (no music)            2.6  doors + lobby on the orchestral intro (O 0 - 16.6)
// 19.2 "¡Sí!" -> paper wipe, gameplay on the arcade track (A 6.5 - 36.5)
// 49.2 the orchestral comes back where it stopped (O 16.6 ->): harbour, delivery, the truck blows up on O 29.2
// 65.4 key art, logo on the last big hit (O 35.5), PRÓXIMAMENTE, end card on the tail.
export const horizontal: Edit = {
  clips: [
    {shot: 'S01_ColdOpen', from: 0.0, dur: 2.6, in: 'cut'},
    {shot: 'S02_Doors', from: 0.4, dur: 7.0, in: 'black'},
    {shot: 'S03_LobbyGather', from: 0.4, dur: 5.0},
    {shot: 'S04_LobbyHug', from: 0.3, dur: 2.4},
    {shot: 'S05_LobbyReady', from: 0.3, dur: 2.2},
    {shot: 'S06_MeadowArrive', from: 0.3, dur: 3.0, in: 'paper'},
    {shot: 'S07_HotPotato', from: 0.1, dur: 4.0},
    {shot: 'S08_LogJump', from: 1.0, dur: 3.6},
    {shot: 'S10_Duck', from: 0.5, dur: 1.8},
    {shot: 'S09_RiverPush', from: 0.2, dur: 3.0},
    {shot: 'S11_Bounce', from: 1.0, dur: 2.4},
    {shot: 'S12_Tunnel', from: 0.2, dur: 2.0},
    {shot: 'S13_SummitLake', from: 0.4, dur: 4.0},
    {shot: 'S14_Snowman', from: 0.4, dur: 2.4},
    {shot: 'S15_Glacier', from: 0.6, dur: 3.8},
    {shot: 'S16_HarbourRaft', from: 0.4, dur: 4.0, in: 'black'},
    {shot: 'S17_HarbourZap', from: 0.3, dur: 4.2},
    {shot: 'S18_Delivery', from: 0.6, dur: 3.0},
    {shot: 'S19_Boom', from: 0.6, dur: 4.6},
    {shot: 'S20_KeyArt', from: 0.0, dur: 10.6, in: 'black'},
  ],
  music: [
    {file: O, at: 2.6, from: 0, dur: 16.6, fadeIn: 0.4, fadeOut: 0.25},
    {file: A, at: 19.0, from: 6.3, dur: 30.2, fadeIn: 0.15, fadeOut: 0.5},
    {file: O, at: 49.2, from: 16.6, dur: 29.0, fadeIn: 0.6, fadeOut: 3.0},
  ],
  keyArtAt: 65.4,
  logoAt: 68.1,
  comingSoonAt: 70.0,
  endCardAt: 76.0,
  total: 81.0,
};

// ---------------------------------------------------------------- vertical (9:16), about 37 s, the arcade track only
export const vertical: Edit = {
  clips: [
    {shot: 'S01_ColdOpen', from: 0.0, dur: 2.2, in: 'cut'},
    {shot: 'S02_Doors', from: 2.4, dur: 3.4, in: 'black'},
    {shot: 'S03_LobbyGather', from: 3.6, dur: 2.6},
    {shot: 'S05_LobbyReady', from: 1.4, dur: 1.6},
    {shot: 'S06_MeadowArrive', from: 1.6, dur: 1.8, in: 'paper'},
    {shot: 'S07_HotPotato', from: 0.1, dur: 3.6},
    {shot: 'S08_LogJump', from: 2.0, dur: 2.6},
    {shot: 'S09_RiverPush', from: 1.2, dur: 2.0},
    {shot: 'S11_Bounce', from: 1.6, dur: 1.6},
    {shot: 'S13_SummitLake', from: 1.0, dur: 2.8},
    {shot: 'S17_HarbourZap', from: 1.2, dur: 3.2},
    {shot: 'S19_Boom', from: 1.2, dur: 3.4},
    {shot: 'S20_KeyArt', from: 0.0, dur: 6.4, in: 'black'},
  ],
  music: [{file: A, at: 0.0, from: 0.0, dur: 37.5, fadeIn: 0.05, fadeOut: 1.0}],
  keyArtAt: 30.5,
  logoAt: 31.2,
  comingSoonAt: 32.4,
  endCardAt: 36.9,
  total: 40.0,
};

export const timeline = (edit: Edit) => {
  let t = 0;
  return edit.clips.map((clip) => {
    const at = t;
    t += clip.dur;
    return {...clip, at};
  });
};
