// Turns what Unity filmed (capture/<shot>_<h|v>/00000.jpg + <shot>_<h|v>.json + sfx/*.wav) into what the edit uses:
// public/footage/<shot>_<fmt>.mp4, public/sfx/*.wav and src/shots.generated.json.
//   node scripts/collect.mjs [captureDir]
import fs from 'node:fs';
import path from 'node:path';
import {execFileSync} from 'node:child_process';

const root = path.resolve(path.dirname(new URL(import.meta.url).pathname), '..');
const capture = path.resolve(process.argv[2] ?? path.join(root, 'capture'));
const footage = path.join(root, 'public', 'footage');
const sfxOut = path.join(root, 'public', 'sfx');
fs.mkdirSync(footage, {recursive: true});
fs.mkdirSync(sfxOut, {recursive: true});

const ffmpeg = process.env.FFMPEG ?? 'ffmpeg';
const shots = {};
for (const file of fs.readdirSync(capture).filter((f) => f.endsWith('.json')).sort()) {
  const data = JSON.parse(fs.readFileSync(path.join(capture, file), 'utf8'));
  const key = `${data.id}_${data.format}`;
  const frames = path.join(capture, key);
  const out = path.join(footage, `${key}.mp4`);
  const newest = Math.max(...fs.readdirSync(frames).map((f) => fs.statSync(path.join(frames, f)).mtimeMs));
  if (!fs.existsSync(out) || fs.statSync(out).mtimeMs < newest) {
    console.log('encoding', key, data.frames, 'frames');
    execFileSync(ffmpeg, ['-y', '-v', 'error', '-framerate', String(data.fps), '-i', path.join(frames, '%05d.jpg'),
      '-c:v', 'libx264', '-preset', 'slow', '-crf', '13', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', out]);
  }
  for (const m of data.markers ?? []) if (m.label === 'boom') data.sfx.push({t: m.t, id: 'Boom', volume: 1, pitch: 1, distance: 0, pan: 0});
  shots[key] = {...data, video: `footage/${key}.mp4`};
}

const sfxIn = path.join(capture, 'sfx');
if (fs.existsSync(sfxIn)) for (const f of fs.readdirSync(sfxIn)) fs.copyFileSync(path.join(sfxIn, f), path.join(sfxOut, f));

fs.writeFileSync(path.join(root, 'src', 'shots.generated.json'), JSON.stringify(shots, null, 1));
console.log('shots:', Object.keys(shots).length);
