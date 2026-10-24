import { useState, type FormEvent } from 'react';
import { Logo } from '../components/Logo';
import { CornerControls } from '../components/CornerControls';
import { Button } from '../components/ui/Button';
import { Panel } from '../components/ui/Panel';
import type { GameActions } from '../net/useGameConnection';
import styles from './HomePage.module.css';

const NAME_KEY = 'actionblocks.name';

function loadName(): string {
  try {
    return localStorage.getItem(NAME_KEY) ?? '';
  } catch {
    return '';
  }
}

function saveName(name: string) {
  try {
    localStorage.setItem(NAME_KEY, name);
  } catch {
    // Storage may be unavailable (private mode); the name is just not remembered.
  }
}

interface Props {
  connected: boolean;
  actions: GameActions;
}

export function HomePage({ connected, actions }: Props) {
  const [name, setName] = useState(loadName);
  const [code, setCode] = useState(() => new URLSearchParams(window.location.search).get('code')?.toUpperCase() ?? '');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const run = async (action: () => Promise<void>) => {
    setError(null);
    setBusy(true);
    saveName(name.trim());
    try {
      await action();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  const onCreate = () => run(() => actions.createLobby(name));
  const onJoin = (e: FormEvent) => {
    e.preventDefault();
    if (code.trim()) void run(() => actions.joinLobby(code, name));
  };

  const disabled = !connected || busy;

  return (
    <main className={styles.page}>
      <CornerControls />
      <Logo />
      <p className={styles.tagline}>Stack. Clear. Collect. Strike.</p>

      <Panel title="Your name" className={styles.panel}>
        <input
          className={styles.input}
          value={name}
          maxLength={16}
          placeholder="PLAYER"
          onChange={(e) => setName(e.target.value)}
          autoFocus
        />
      </Panel>

      <div className={styles.columns}>
        <Panel title="New game" className={styles.panel}>
          <p className={styles.hint}>Open a lobby and share its code.</p>
          <Button onClick={onCreate} disabled={disabled}>
            Create lobby
          </Button>
        </Panel>

        <Panel title="Join game" className={styles.panel}>
          <form onSubmit={onJoin} className={styles.joinForm}>
            <input
              className={[styles.input, styles.codeInput].join(' ')}
              value={code}
              maxLength={6}
              placeholder="CODE"
              onChange={(e) => setCode(e.target.value.toUpperCase().replace(/[^A-Z0-9]/g, ''))}
            />
            <Button type="submit" variant="secondary" disabled={disabled || code.length === 0}>
              Join
            </Button>
          </form>
        </Panel>
      </div>

      {error && <p className={styles.error}>{error}</p>}
    </main>
  );
}
