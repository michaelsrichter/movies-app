import { Link } from 'react-router-dom';

const GITHUB_URL = 'https://github.com/michaelsrichter/movies-app';
const LINKEDIN_URL = 'https://www.linkedin.com/in/mikerichter/';
const YEAR = new Date().getFullYear();

export function Footer() {
  return (
    <footer className="app-footer">
      <div className="filmstrip" aria-hidden="true" />
      <div className="footer-inner">
        <ul className="footer-links">
          <li>
            <Link to="/privacy">Privacy</Link>
          </li>
          <li>
            <Link to="/terms">Terms of Service</Link>
          </li>
          <li>
            <a href={GITHUB_URL} target="_blank" rel="noreferrer">
              <span aria-hidden="true">🐙</span> GitHub
            </a>
          </li>
          <li>
            <a href={LINKEDIN_URL} target="_blank" rel="noreferrer">
              <span aria-hidden="true">💼</span> LinkedIn
            </a>
          </li>
        </ul>

        <p className="attribution">
          This product uses the TMDB API but is not endorsed or certified by{' '}
          <a href="https://www.themoviedb.org/" target="_blank" rel="noreferrer">
            TMDB
          </a>
          . Movie data and images are provided by The Movie Database (TMDB).
        </p>

        <p className="footer-copy">© {YEAR} Mike Richter · Family Movie Watchlist</p>
      </div>
    </footer>
  );
}
