import { useEffect, useState } from 'react';
import { Logo } from '../components/Logo';
import { MusicTracks } from '../components/MusicTracks';
import { CornerControls } from '../components/CornerControls';
import { Button } from '../components/ui/Button';
import { Panel } from '../components/ui/Panel';
import { controlsHelp } from '../config/keybindings';
import type { ItemInfo, Lobby } from '../net/types';
import type { GameActions } from '../net/useGameConnection';
import { itemDescription, itemVisual, itemVisuals } from '../theme/assets';
import styles from './LobbyPage.module.css';

interface Props {
  lobby: Lobby;
  playerId: string;
  actions: GameActions;
}

export function LobbyPage({ lobby, playerId, actions }: Props) {
  const [error, setError] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);
  const isHost = lobby.hostId === playerId;
  // Items as configured on the server; null until loaded (or if the call fails) = built-in list.
  const [items, setItems] = useState<ItemInfo[] | null>(null);

  useEffect(() => {
    let cancelled = false;
    actions
      .getItems()
      .then((list) => !cancelled && setItems(list))
      .catch(() => {});
    return () => {
      cancelled = true;
    };
  }, [actions]);

  const shownItems = items
    ? items.filter((i) => i.share > 0).map((i) => ({ id: i.id, share: i.share, duration: i.durationSeconds }))
    : Object.keys(itemVisuals).map((id) => ({ id, share: null, duration: null }));

  const copyLink = async () => {
    const url = `${window.location.origin}${window.location.pathname}?code=${lobby.code}`;
    try {
      await navigator.clipboard.writeText(url);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1500);
    } catch {
      setError(url);
    }
  };

  const start = async () => {
    setError(null);
    try {
      await actions.startGame();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    }
  };

  return (
    <main className={styles.page}>
      <header className={styles.header}>
        <Logo small />
      </header>
      <CornerControls onLeave={() => void actions.leaveLobby()} />

      <Panel title="Lobby code" className={styles.codePanel}>
        <div className={styles.code}>{lobby.code}</div>
        <Button variant="secondary" onClick={copyLink}>
          {copied ? 'Copied!' : 'Copy invite link'}
        </Button>
      </Panel>

      <div className={styles.grid}>
        <Panel title={`Players ${lobby.players.length}/${lobby.maxPlayers}`}>
          <ol className={styles.players}>
            {lobby.players.map((p) => (
              <li key={p.id} className={p.id === playerId ? styles.me : ''}>
                <span>{p.name}</span>
                {p.isHost && <span className={styles.badge}>HOST</span>}
                {p.id === playerId && <span className={styles.badge}>YOU</span>}
              </li>
            ))}
          </ol>
          {isHost ? (
            <Button onClick={start} className={styles.start}>
              Start game
            </Button>
          ) : (
            <p className={styles.waiting}>Waiting for the host to start...</p>
          )}
          {error && <p className={styles.error}>{error}</p>}
        </Panel>

        {lobby.lastResults && (
          <Panel title="Last round">
            <table className={styles.results}>
              <tbody>
                {lobby.lastResults.map((r) => (
                  <tr key={r.id}>
                    <td className={r.place === 1 ? styles.winner : ''}>#{r.place}</td>
                    <td>{r.name}</td>
                    <td className={styles.num}>{r.score}</td>
                    <td className={styles.num}>{r.lines}L</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Panel>
        )}

        <Panel title="Controls">
          <dl className={styles.controls}>
            {controlsHelp.map((c) => (
              <div key={c.action}>
                <dt>{c.keys}</dt>
                <dd>{c.action}</dd>
              </div>
            ))}
          </dl>
        </Panel>

        <Panel title="Items">
          <p className={styles.hint}>Items hide inside blocks. Clear their line to collect them.</p>
          <ul className={styles.items}>
            {shownItems.map(({ id, share, duration }) => {
              const v = itemVisual(id);
              return (
                <li key={id}>
                  <img src={v.icon} alt="" width={20} height={20} />
                  <div className={styles.itemText}>
                    <div className={styles.itemName}>
                      <span style={{ color: v.color }}>{v.name}</span>
                      {share !== null && <span className={styles.itemShare}>{formatShare(share)}</span>}
                    </div>
                    <div className={styles.itemDescription}>{itemDescription(v, duration)}</div>
                  </div>
                </li>
              );
            })}
          </ul>
        </Panel>

        <Panel title="Music">
          <MusicTracks />
        </Panel>
      </div>
    </main>
  );
}

/** Share of spawned items as a percentage, e.g. "17%". */
function formatShare(share: number): string {
  return share < 0.01 ? '<1%' : `${Math.round(share * 100)}%`;
}
