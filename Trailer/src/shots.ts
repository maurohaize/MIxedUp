// Data written by the Unity recorder for every filmed shot (see scripts/collect.mjs). Kept loose on purpose.
import data from './shots.generated.json';

export type Subtitle = {t: number; dur: number; who: string; colour: string; text: string};
export type Sfx = {t: number; id: string; volume: number; pitch: number; distance: number; pan: number};
export type ShotData = {id: string; format: string; width: number; height: number; fps: number; frames: number; video: string; subtitles: Subtitle[]; sfx: Sfx[]};

const shots = data as unknown as Record<string, ShotData>;

export const getShot = (id: string, format: 'h' | 'v'): ShotData | undefined => shots[`${id}_${format}`] ?? (format === 'v' ? undefined : undefined);
