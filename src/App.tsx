import { Outlet } from 'react-router-dom';
import { Attribution } from './components/Attribution';

export function App() {
  return (
    <div className="app-shell">
      <header className="app-header">
        <a href="/" className="brand">
          🎬 Family Movie Watchlist
        </a>
      </header>
      <main className="app-main">
        <Outlet />
      </main>
      <footer className="app-footer">
        <Attribution />
      </footer>
    </div>
  );
}
