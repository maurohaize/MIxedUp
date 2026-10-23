import React from 'react';
import {Composition} from 'remotion';
import './fonts';
import {FPS, horizontal, vertical} from './edit';
import {Trailer} from './Trailer';

export const Root: React.FC = () => (
  <>
    <Composition id="TrailerH" component={Trailer} durationInFrames={Math.round(horizontal.total * FPS)} fps={FPS} width={1920} height={1080}
      defaultProps={{edit: horizontal, format: 'h' as const}} />
    <Composition id="TrailerV" component={Trailer} durationInFrames={Math.round(vertical.total * FPS)} fps={FPS} width={1080} height={1920}
      defaultProps={{edit: vertical, format: 'v' as const}} />
  </>
);
