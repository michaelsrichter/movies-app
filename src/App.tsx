import { Outlet } from 'react-router-dom';
import { Footer } from './components/Footer';
import { ThemeToggle } from './components/ThemeToggle';
import { CookieConsent } from './components/CookieConsent';

export function App() {
  return (
    <div className="app-shell">
      <header className="app-header">
        <a href="/" className="brand">
          <span aria-hidden="true">🎬</span>
          <span className="brand-accent">Family Movie Watchlist</span>
        </a>
        <ThemeToggle />
      </header>
      <main className="app-main">
        <Outlet />
      </main>
      <Footer />
      <CookieConsent />
    </div>
  );
}
