import { useGameConnection } from './net/useGameConnection';
import { GamePage } from './pages/GamePage';
import { HomePage } from './pages/HomePage';
import { LobbyPage } from './pages/LobbyPage';
import styles from './App.module.css';

const statusText = {
  connecting: 'Connecting to server...',
  reconnecting: 'Connection lost, reconnecting...',
  disconnected: 'Server unreachable, retrying...',
  connected: '',
};

export function App() {
  const { status, playerId, lobby, match, flashes, actions } = useGameConnection();

  let content;
  if (!lobby || !playerId) {
    content = <HomePage connected={status === 'connected'} actions={actions} />;
  } else if (match) {
    content = <GamePage match={match} lobby={lobby} playerId={playerId} flashes={flashes} actions={actions} />;
  } else if (lobby.phase === 'playing') {
    content = <div className={styles.loading}>Starting...</div>;
  } else {
    content = <LobbyPage lobby={lobby} playerId={playerId} actions={actions} />;
  }

  return (
    <>
      {status !== 'connected' && <div className={styles.status}>{statusText[status]}</div>}
      {content}
    </>
  );
}
