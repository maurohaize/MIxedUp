import React from 'react';
import {
  AbsoluteFill, Audio, Img, OffthreadVideo, Sequence, interpolate, spring, staticFile, useCurrentFrame, useVideoConfig, Easing,
} from 'remotion';
import {Edit, FPS, timeline} from './edit';
import {getShot} from './shots';
import {Subtitles, Line} from './Subtitles';
import {TITLE_FONT, TEXT_FONT} from './fonts';

const sec = (s: number) => Math.round(s * FPS);

// ---------------------------------------------------------------- footage

const Footage: React.FC<{shot: string; from: number; format: 'h' | 'v'}> = ({shot, from, format}) => {
  const data = getShot(shot, format) ?? getShot(shot, 'h');
  if (!data) {
    return (
      <AbsoluteFill style={{background: '#2b2219', alignItems: 'center', justifyContent: 'center', color: '#fbf1dc', fontFamily: TITLE_FONT, fontSize: 80}}>
        {shot}
      </AbsoluteFill>
    );
  }
  const crop = format === 'v' && data.format === 'h';
  return (
    <AbsoluteFill style={{overflow: 'hidden'}}>
      <OffthreadVideo
        src={staticFile(data.video)}
        startFrom={sec(from)}
        muted
        style={crop ? {height: '100%', width: 'auto', position: 'absolute', left: '50%', transform: 'translateX(-50%)'} : {width: '100%', height: '100%', objectFit: 'cover'}}
      />
    </AbsoluteFill>
  );
};

// ---------------------------------------------------------------- transitions

// The sheet of paper that sweeps across the screen, like the game's own scene change.
const PaperWipe: React.FC = () => {
  const frame = useCurrentFrame();
  const {width, height} = useVideoConfig();
  const p = interpolate(frame, [0, sec(0.7)], [0, 1], {extrapolateRight: 'clamp', easing: Easing.inOut(Easing.cubic)});
  const x = interpolate(p, [0, 1], [width * 1.15, -width * 1.6]);
  return (
    <AbsoluteFill style={{pointerEvents: 'none'}}>
      <div
        style={{
          position: 'absolute', top: -height * 0.2, left: x, width: width * 1.45, height: height * 1.4,
          transform: 'rotate(-5deg)', background: '#fbf1dc', boxShadow: '0 0 60px rgba(40,25,10,0.55)',
          backgroundImage: `url(${staticFile('img/paper.png')})`, backgroundSize: '100% 100%',
        }}
      />
    </AbsoluteFill>
  );
};

const FadeFromBlack: React.FC<{seconds: number}> = ({seconds}) => {
  const frame = useCurrentFrame();
  const o = interpolate(frame, [0, sec(seconds)], [1, 0], {extrapolateRight: 'clamp'});
  return <AbsoluteFill style={{background: 'black', opacity: o}} />;
};

// ---------------------------------------------------------------- titles

const KeyArtTitles: React.FC<{logoAt: number; comingSoonAt: number; vertical: boolean}> = ({logoAt, comingSoonAt, vertical}) => {
  const frame = useCurrentFrame();
  const {fps, width, height} = useVideoConfig();
  const logoIn = spring({frame: frame - sec(logoAt), fps, config: {damping: 9, stiffness: 120, mass: 0.8}});
  const soonIn = spring({frame: frame - sec(comingSoonAt), fps, config: {damping: 14, stiffness: 140}});
  const logoW = vertical ? width * 0.82 : width * 0.34;
  const glow = interpolate(frame - sec(logoAt), [0, sec(0.25), sec(1.2)], [0.9, 0.35, 0], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
  return (
    <AbsoluteFill>
      <AbsoluteFill style={{background: `radial-gradient(ellipse at 50% ${vertical ? 30 : 32}%, rgba(255,240,210,${glow}) 0%, rgba(255,240,210,0) 55%)`}} />
      <AbsoluteFill style={{alignItems: 'center', justifyContent: 'flex-start', paddingTop: vertical ? height * 0.12 : height * 0.05}}>
        <Img
          src={staticFile('img/mixedup_logo.png')}
          style={{
            width: logoW,
            transform: `translateY(${(1 - logoIn) * -height * 0.6}px) scale(${0.6 + 0.4 * logoIn}) rotate(${(1 - logoIn) * -8 - 2}deg)`,
            opacity: logoIn > 0.01 ? 1 : 0,
            filter: 'drop-shadow(0 18px 24px rgba(0,0,0,0.45))',
          }}
        />
        <div
          style={{
            marginTop: vertical ? height * 0.02 : height * 0.015,
            fontFamily: TITLE_FONT, fontSize: vertical ? width * 0.13 : height * 0.105, letterSpacing: 4, color: '#fff3d6',
            WebkitTextStroke: `${vertical ? 10 : 9}px #2b1a0b`, paintOrder: 'stroke fill',
            transform: `scale(${soonIn}) rotate(-2deg)`, opacity: soonIn > 0.01 ? 1 : 0,
            textShadow: '0 10px 18px rgba(0,0,0,0.45)',
          }}
        >
          PRÓXIMAMENTE
        </div>
      </AbsoluteFill>
    </AbsoluteFill>
  );
};

const EndCard: React.FC<{vertical: boolean}> = ({vertical}) => {
  const frame = useCurrentFrame();
  const {width, height} = useVideoConfig();
  const o = interpolate(frame, [0, sec(0.5)], [0, 1], {extrapolateRight: 'clamp'});
  const unit = vertical ? width : height * 1.2;
  return (
    <AbsoluteFill style={{background: 'black', alignItems: 'center', justifyContent: 'center', opacity: o}}>
      <div style={{fontFamily: TITLE_FONT, color: 'white', fontSize: unit * 0.11, letterSpacing: 3, lineHeight: 1}}>PRÓXIMAMENTE</div>
      <div style={{fontFamily: TEXT_FONT, fontWeight: 600, color: 'white', opacity: 0.85, fontSize: unit * 0.032, marginTop: unit * 0.012}}>
        De 1 a 4 jugadores · Multijugador online
      </div>
      <Img src={staticFile('img/nubi_white.png')} style={{position: 'absolute', bottom: height * 0.07, width: unit * 0.13, opacity: 0.9}} />
    </AbsoluteFill>
  );
};

// ---------------------------------------------------------------- sound

const sfxFile = (id: string, n: number) => staticFile(`sfx/${id}_${n % 3}.wav`);

const SfxLayer: React.FC<{cues: {at: number; id: string; volume: number; pitch: number}[]}> = ({cues}) => (
  <>
    {cues.map((c, i) => (
      <Sequence key={i} from={sec(c.at)} durationInFrames={sec(3)}>
        <Audio src={sfxFile(c.id, i)} volume={c.volume} playbackRate={c.id === 'Voice' ? c.pitch : 1} />
      </Sequence>
    ))}
  </>
);

// ---------------------------------------------------------------- the trailer

export const Trailer: React.FC<{edit: Edit; format: 'h' | 'v'}> = ({edit, format}) => {
  const vertical = format === 'v';
  const clips = timeline(edit);
  const keyArtAt = clips[clips.length - 1].at;

  // Subtitles and sounds of each clip, moved onto the timeline.
  const lines: Line[] = [];
  const cues: {at: number; id: string; volume: number; pitch: number}[] = [];
  for (const c of clips) {
    const data = getShot(c.shot, format) ?? getShot(c.shot, 'h');
    if (!data) continue;
    for (const s of data.subtitles) {
      const start = c.at + (s.t - c.from);
      const end = Math.min(start + s.dur, c.at + c.dur);
      if (end > c.at + 0.15 && start < c.at + c.dur) lines.push({start: Math.max(start, c.at), end, colour: s.colour, text: s.text});
    }
    for (const s of data.sfx) {
      if (s.t < c.from || s.t > c.from + c.dur) continue;
      if (s.id.startsWith('Foot') && s.distance > 9) continue;
      const fall = Math.max(0, 1 - s.distance / 38);
      const vol = Math.min(1, s.volume * fall * (s.id.startsWith('Foot') ? 0.35 : 0.8));
      if (vol > 0.03) cues.push({at: c.at + (s.t - c.from), id: s.id, volume: vol, pitch: s.pitch});
    }
  }

  return (
    <AbsoluteFill style={{background: 'black'}}>
      {clips.map((c, i) => (
        <Sequence key={i} from={sec(c.at)} durationInFrames={sec(c.dur)}>
          <Footage shot={c.shot} from={c.from} format={format} />
          {c.in === 'black' && <FadeFromBlack seconds={0.5} />}
        </Sequence>
      ))}

      {clips.filter((c) => c.in === 'paper').map((c, i) => (
        <Sequence key={`p${i}`} from={sec(c.at - 0.35)} durationInFrames={sec(0.8)}>
          <PaperWipe />
        </Sequence>
      ))}

      <Subtitles lines={lines.filter((l) => l.start < keyArtAt)} vertical={vertical} />

      <Sequence from={sec(keyArtAt)} durationInFrames={sec(edit.endCardAt - keyArtAt)}>
        <KeyArtTitles logoAt={edit.logoAt - keyArtAt} comingSoonAt={edit.comingSoonAt - keyArtAt} vertical={vertical} />
      </Sequence>
      <Sequence from={sec(edit.endCardAt)}>
        <EndCard vertical={vertical} />
      </Sequence>

      {edit.music.map((m, i) => (
        <Sequence key={`m${i}`} from={sec(m.at)} durationInFrames={sec(m.dur)}>
          <Audio
            src={staticFile(m.file)}
            startFrom={sec(m.from)}
            volume={(f) => {
              const t = f / FPS;
              const fin = m.fadeIn ? Math.min(1, t / m.fadeIn) : 1;
              const fout = m.fadeOut ? Math.min(1, (m.dur - t) / m.fadeOut) : 1;
              return Math.max(0, Math.min(fin, fout)) * (m.volume ?? 0.9);
            }}
          />
        </Sequence>
      ))}
      <SfxLayer cues={cues} />
    </AbsoluteFill>
  );
};
