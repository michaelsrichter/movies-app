import { Link } from 'react-router-dom';
import { useSeo } from '../lib/seo';

export default function PrivacyPage() {
  useSeo({
    title: 'Privacy Policy',
    description:
      'Privacy Policy for the Family Movie Watchlist: what data we use, cookies, and your rights under GDPR.',
    path: '/privacy',
  });

  return (
    <article className="content-page">
      <Link to="/" className="back-link">
        ← Back to watchlist
      </Link>
      <h1>Privacy Policy</h1>
      <p className="updated">Last updated: {new Date().getFullYear()}</p>

      <p>
        Family Movie Watchlist (&ldquo;the site&rdquo;) is a personal, family-oriented project. We
        keep data collection to the minimum needed to run the service.
      </p>

      <h2>Information we process</h2>
      <ul>
        <li>
          <strong>Essential cookies &amp; local storage.</strong> Used to keep you signed in (for
          administrators), remember your light/dark theme preference, and record your cookie choice.
        </li>
        <li>
          <strong>Authentication.</strong> Administrator sign-in is handled by GitHub via Azure
          Static Web Apps. We do not receive or store your GitHub password.
        </li>
        <li>
          <strong>Standard server logs.</strong> Our hosting provider may log IP addresses and
          request metadata for security and reliability.
        </li>
      </ul>
      <p>
        We do <strong>not</strong> use advertising cookies, sell personal data, or run third-party
        tracking or analytics.
      </p>

      <h2>Third-party content</h2>
      <p>
        Movie details and images are provided by{' '}
        <a href="https://www.themoviedb.org/" target="_blank" rel="noreferrer">
          The Movie Database (TMDB)
        </a>
        . Trailers are embedded from YouTube using the privacy-enhanced (no-cookie) player; YouTube
        may set cookies only if you press play.
      </p>

      <h2>Your rights (GDPR)</h2>
      <p>
        If you are in the European Economic Area or the UK, you have the right to access, correct,
        or request deletion of any personal data we hold about you, and to object to processing. To
        exercise these rights, contact the site owner via{' '}
        <a href="https://www.linkedin.com/in/mikerichter/" target="_blank" rel="noreferrer">
          LinkedIn
        </a>
        .
      </p>

      <h2>Changes</h2>
      <p>
        We may update this policy from time to time. Material changes will be reflected by the
        &ldquo;last updated&rdquo; date above.
      </p>
    </article>
  );
}
