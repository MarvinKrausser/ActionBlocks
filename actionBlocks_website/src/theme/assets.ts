import { songs } from '../audio/songs';
import type { MusicTrack, SoundId } from '../audio/types';

/**
 * Central registry of every graphic and sound the game uses. Replace the files in /public/assets
 * (or point these paths elsewhere) to change the look and sound without touching components.
 */

/** Resolves a file in /public relative to where the site is served (works under sub-paths). */
const asset = (path: string) => `${import.meta.env.BASE_URL}${path}`;

export interface ItemVisual {
  /** Icon drawn on board cells, in inventories and item bars. */
  icon: string;
  /** Fallback name, the server sends the display name with each use. */
  name: string;
  /** Accent color used for flashes and as icon fallback. */
  color: string;
  /** Short explanation for the lobby help panel. */
  description: string;
}

/** Keyed by the server item id (IItem.Id). Add an entry here when adding an item on the server. */
export const itemVisuals: Record<string, ItemVisual> = {
  add_line: {
    icon: asset('assets/items/add_line.svg'),
    name: 'Add Line',
    color: '#ff4256',
    description: 'Pushes a garbage line into the target field.',
  },
  clear_line: {
    icon: asset('assets/items/clear_line.svg'),
    name: 'Clear Line',
    color: '#4fe35f',
    description: 'Removes the bottom line of the target.',
  },
  nuke: {
    icon: asset('assets/items/nuke.svg'),
    name: 'Wipe Out',
    color: '#ffe23f',
    description: 'Clears every placed block of the target.',
  },
  swap: {
    icon: asset('assets/items/swap.svg'),
    name: 'Switch',
    color: '#3fe3f2',
    description: 'Trades your whole field with the target.',
  },
  blind: {
    icon: asset('assets/items/blind.svg'),
    name: 'Blackout',
    color: '#c452ff',
    description: 'Hides placed blocks and the drop shadow for 4 seconds. Stacks.',
  },
};

export const unknownItem: ItemVisual = {
  icon: asset('assets/items/unknown.svg'),
  name: 'Item',
  color: '#ffffff',
  description: '',
};

export const uiAssets = {
  /** Optional logo image. When null the text logo is rendered. */
  logo: null as string | null,
  favicon: asset('assets/favicon.svg'),
  soundOn: asset('assets/ui/sound_on.svg'),
  soundOff: asset('assets/ui/sound_off.svg'),
};

/**
 * Optional audio files that replace the synthesized sound effects. Put the files in
 * /public/assets/sounds/. Effects without an entry (or whose file fails to load) use the synth.
 */
export const soundFiles: Partial<Record<SoundId, string>> = {
  // lineClear: asset('assets/sounds/line_clear.wav'),
};

/** Background music, played as a shuffled playlist during matches. Add as many tracks as you like. */
export const musicTracks: MusicTrack[] = [
  { id: 'pixel_rush', title: 'Pixel Rush', kind: 'synth', song: songs.pixelRush },
  { id: 'neon_stack', title: 'Neon Stack', kind: 'synth', song: songs.neonStack },
  { id: 'last_line', title: 'Last Line', kind: 'synth', song: songs.lastLine },
  // { id: 'my_track', title: 'My Track', kind: 'file', src: asset('assets/sounds/music/my_track.mp3') },
];

export function itemVisual(id: string): ItemVisual {
  return itemVisuals[id] ?? unknownItem;
}

const imageCache = new Map<string, HTMLImageElement>();

/** Returns a (possibly still loading) image for canvas rendering. */
export function getImage(src: string): HTMLImageElement {
  let image = imageCache.get(src);
  if (!image) {
    image = new Image();
    image.src = src;
    imageCache.set(src, image);
  }
  return image;
}

export function preloadAssets(): void {
  Object.values(itemVisuals).forEach((v) => getImage(v.icon));
  getImage(unknownItem.icon);
}
