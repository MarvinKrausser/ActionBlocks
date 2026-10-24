import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './App';
import { SoundProvider } from './audio/SoundContext';
import { preloadAssets } from './theme/assets';
import './theme/global.css';

preloadAssets();

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <SoundProvider>
      <App />
    </SoundProvider>
  </StrictMode>,
);
