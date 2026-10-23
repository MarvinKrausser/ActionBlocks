/**
 * Central registry of every graphic the game uses. Replace the files in /public/assets
 * (or point these paths elsewhere) to change the look without touching components.
 */

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
    icon: '/assets/items/add_line.svg',
    name: 'Add Line',
    color: '#ff4256',
    description: 'Pushes a garbage line into the target field.',
  },
  clear_line: {
    icon: '/assets/items/clear_line.svg',
    name: 'Clear Line',
    color: '#4fe35f',
    description: 'Removes the bottom line of the target.',
  },
  nuke: {
    icon: '/assets/items/nuke.svg',
    name: 'Wipe Out',
    color: '#ffe23f',
    description: 'Clears every placed block of the target.',
  },
  swap: {
    icon: '/assets/items/swap.svg',
    name: 'Switch',
    color: '#3fe3f2',
    description: 'Trades your whole field with the target.',
  },
  blind: {
    icon: '/assets/items/blind.svg',
    name: 'Blackout',
    color: '#c452ff',
    description: 'Hides placed blocks and the drop shadow for 4 seconds.',
  },
};

export const unknownItem: ItemVisual = {
  icon: '/assets/items/unknown.svg',
  name: 'Item',
  color: '#ffffff',
  description: '',
};

export const uiAssets = {
  /** Optional logo image. When null the text logo is rendered. */
  logo: null as string | null,
  favicon: '/assets/favicon.svg',
};

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
