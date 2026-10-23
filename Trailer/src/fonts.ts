import {continueRender, delayRender, staticFile} from 'remotion';

const load = (family: string, file: string, descriptors?: FontFaceDescriptors) => {
  const handle = delayRender(`font ${family}`);
  const face = new FontFace(family, `url(${staticFile(file)})`, descriptors);
  face
    .load()
    .then(() => {
      document.fonts.add(face);
      continueRender(handle);
    })
    .catch(() => continueRender(handle));
};

load('Bangers', 'fonts/Bangers.ttf');
load('Fredoka', 'fonts/Fredoka.ttf', {weight: '300 700'});

export const TITLE_FONT = 'Bangers, sans-serif';
export const TEXT_FONT = 'Fredoka, sans-serif';
