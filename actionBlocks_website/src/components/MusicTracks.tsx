import { useSound } from '../audio/SoundContext';
import { musicTracks } from '../theme/assets';
import styles from './MusicTracks.module.css';

/** One checkbox per registered music track; unchecked tracks are skipped by the playlist. */
export function MusicTracks() {
  const { settings, setSettings } = useSound();

  const toggleTrack = (id: string, enabled: boolean) =>
    setSettings({
      disabledTracks: enabled ? settings.disabledTracks.filter((t) => t !== id) : [...settings.disabledTracks, id],
    });

  return (
    <ul className={styles.tracks}>
      {musicTracks.map((track) => (
        <li key={track.id}>
          <label>
            <input
              type="checkbox"
              checked={!settings.disabledTracks.includes(track.id)}
              onChange={(e) => toggleTrack(track.id, e.target.checked)}
            />
            {track.title}
          </label>
        </li>
      ))}
    </ul>
  );
}
