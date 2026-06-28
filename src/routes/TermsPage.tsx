import { Link } from 'react-router-dom';
import { useSeo } from '../lib/seo';

export default function TermsPage() {
  useSeo({
    title: 'Terms of Service',
    description: 'Terms of Service for the Family Movie Watchlist.',
    path: '/terms',
  });

  return (
    <article className="content-page">
      <Link to="/" className="back-link">
        ← Back to watchlist
      </Link>
      <h1>Terms of Service</h1>
      <p className="updated">Last updated: {new Date().getFullYear()}</p>

      <p>
        Family Movie Watchlist is provided as a free, personal project &ldquo;as is,&rdquo; without
        warranties of any kind. By using the site you agree to these terms.
      </p>

      <h2>Acceptable use</h2>
      <p>
        You agree not to misuse the site, attempt to disrupt its operation, or access non-public
        areas without authorization.
      </p>

      <h2>Content &amp; attribution</h2>
      <p>
        Movie metadata, posters, and images are supplied by{' '}
        <a href="https://www.themoviedb.org/" target="_blank" rel="noreferrer">
          The Movie Database (TMDB)
        </a>
        . This product uses the TMDB API but is not endorsed or certified by TMDB. Trailers are the
        property of their respective rights holders and are embedded via YouTube. Discussion prompts
        are AI-assisted suggestions intended to spark family conversation, not professional advice.
      </p>

      <h2>Limitation of liability</h2>
      <p>
        To the fullest extent permitted by law, the site owner is not liable for any damages arising
        from your use of the site or reliance on its content.
      </p>

      <h2>Open source</h2>
      <p>
        The source code is available on{' '}
        <a href="https://github.com/michaelsrichter/movies-app" target="_blank" rel="noreferrer">
          GitHub
        </a>{' '}
        under its published license.
      </p>

      <h2>Changes</h2>
      <p>We may revise these terms at any time. Continued use constitutes acceptance.</p>
    </article>
  );
}
