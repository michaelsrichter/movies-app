import { useQuery } from '@tanstack/react-query';
import { useParams } from 'react-router-dom';
import { api, posterUrl } from '../lib/api';

export default function MovieDetailPage() {
  const { tmdbId } = useParams();
  const { data, isLoading, isError } = useQuery({
    queryKey: ['movie', tmdbId],
    queryFn: () => api.getMovie(tmdbId!),
    enabled: !!tmdbId,
  });

  if (isLoading) return <p className="empty">Loading…</p>;
  if (isError || !data) return <p className="empty">Movie not found.</p>;

  const { movie, discussionTopics } = data;
  const poster = posterUrl(movie.posterPath, 'w500');
  const director = movie.crew.find((c) => c.job === 'Director')?.name;
  const meta = [
    movie.year?.toString(),
    movie.certification,
    movie.runtime ? `${movie.runtime} min` : undefined,
  ].filter(Boolean) as string[];

  return (
    <article className="detail">
      {poster && (
        <>
          <link rel="preload" as="image" href={poster} />
          <img className="detail-poster" src={poster} alt={movie.title} />
        </>
      )}
      <h1 className="page-title">
        {movie.title} {movie.year ? <span className="muted">({movie.year})</span> : null}
      </h1>
      {movie.tagline && <p className="tagline">{movie.tagline}</p>}

      <div className="meta-row">
        {movie.voteAverage > 0 && (
          <span className="rating-badge">★ {movie.voteAverage.toFixed(1)}</span>
        )}
        {meta.map((m) => (
          <span key={m} className="meta-chip">
            {m}
          </span>
        ))}
      </div>

      {movie.genres.length > 0 && (
        <div className="chips">
          {movie.genres.map((g) => (
            <span key={g} className="chip">
              {g}
            </span>
          ))}
        </div>
      )}

      {director && (
        <p className="director">
          <span className="muted">Directed by</span> {director}
        </p>
      )}

      {movie.overview && <p className="overview">{movie.overview}</p>}

      {movie.providers.length > 0 && (
        <section className="providers">
          <h2>Where to watch (US)</h2>
          <ul>
            {movie.providers.map((p) => (
              <li key={`${p.type}-${p.name}`}>
                {p.name} <span className="muted">({p.type})</span>
              </li>
            ))}
          </ul>
        </section>
      )}

      {movie.cast.length > 0 && (
        <section className="cast">
          <h2>Cast</h2>
          <ul className="cast-carousel">
            {movie.cast.map((c) => (
              <li key={c.name}>
                <strong>{c.name}</strong>
                {c.character ? <span className="muted"> as {c.character}</span> : null}
              </li>
            ))}
          </ul>
        </section>
      )}

      <section className="discussion">
        <h2>Family discussion topics</h2>
        {discussionTopics.length === 0 ? (
          <p className="muted">Discussion topics coming soon.</p>
        ) : (
          <ul>
            {discussionTopics.map((t, i) => (
              <li key={i} className="topic">
                {t.category && <span className="topic-cat">{t.category}</span>}
                <strong>{t.heading}</strong>
                <p>{t.prompt}</p>
              </li>
            ))}
          </ul>
        )}
      </section>
    </article>
  );
}
