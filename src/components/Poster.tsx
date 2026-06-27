import { useState } from 'react';
import { posterUrl, type MovieSummary } from '../lib/api';

/**
 * Poster image with an LQIP/base64 placeholder that swaps to the full image on load,
 * for fast perceived performance on mobile.
 */
export function Poster({ summary }: { summary: MovieSummary }) {
  const [loaded, setLoaded] = useState(false);
  const full = posterUrl(summary.posterPath, 'w342');

  return (
    <div className="poster">
      {summary.posterBlurDataUrl && !loaded && (
        <img className="poster-img blur" src={summary.posterBlurDataUrl} alt="" aria-hidden />
      )}
      {full && (
        <img
          className={`poster-img ${loaded ? 'visible' : 'hidden'}`}
          src={full}
          alt={summary.title}
          loading="lazy"
          decoding="async"
          onLoad={() => setLoaded(true)}
        />
      )}
    </div>
  );
}
