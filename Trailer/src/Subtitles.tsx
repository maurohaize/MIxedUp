import React from 'react';
import {AbsoluteFill, useCurrentFrame, useVideoConfig, interpolate} from 'remotion';
import {TEXT_FONT} from './fonts';

export type Line = {start: number; end: number; colour: string; text: string};

// Brighten the darker clothes colours so every speaker reads well over the picture.
const readable = (hex: string) => {
  const n = parseInt(hex.replace('#', ''), 16);
  let r = (n >> 16) & 255, g = (n >> 8) & 255, b = n & 255;
  const l = 0.2126 * r + 0.7152 * g + 0.0722 * b;
  if (l < 120) {
    const k = (120 - l) / 255 + 0.25;
    r = Math.round(r + (255 - r) * k);
    g = Math.round(g + (255 - g) * k);
    b = Math.round(b + (255 - b) * k);
  }
  return `rgb(${r},${g},${b})`;
};

// Speaker-coloured lines at the bottom of the picture, like the voice chat captions of the reference trailer.
export const Subtitles: React.FC<{lines: Line[]; vertical: boolean}> = ({lines, vertical}) => {
  const frame = useCurrentFrame();
  const {fps, height, width} = useVideoConfig();
  const t = frame / fps;
  const active = lines.filter((l) => t >= l.start && t < l.end);
  if (active.length === 0) return null;
  const size = vertical ? width * 0.058 : height * 0.04;
  return (
    <AbsoluteFill style={{justifyContent: 'flex-end', alignItems: 'center', paddingBottom: vertical ? height * 0.2 : height * 0.075}}>
      {active.slice(-2).map((l, i) => {
        const fadeIn = interpolate(t, [l.start, l.start + 0.08], [0, 1], {extrapolateRight: 'clamp'});
        const fadeOut = interpolate(t, [l.end - 0.12, l.end], [1, 0], {extrapolateLeft: 'clamp'});
        return (
          <div
            key={i}
            style={{
              fontFamily: TEXT_FONT,
              fontWeight: 600,
              fontSize: size,
              lineHeight: 1.15,
              color: readable(l.colour),
              textAlign: 'center',
              maxWidth: vertical ? width * 0.86 : width * 0.7,
              opacity: Math.min(fadeIn, fadeOut),
              WebkitTextStroke: `${size * 0.16}px #1b130c`,
              paintOrder: 'stroke fill',
              textShadow: `0 ${size * 0.06}px ${size * 0.12}px rgba(0,0,0,0.45)`,
              marginTop: size * 0.2,
            }}
          >
            {l.text}
          </div>
        );
      })}
    </AbsoluteFill>
  );
};
