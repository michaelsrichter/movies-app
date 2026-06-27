import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { api } from '../lib/api';
import { Poster } from '../components/Poster';
import { ListPageSkeleton } from '../components/Skeletons';

export default function ListPage() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['current-list'],
    queryFn: api.getCurrentList,
  });

  if (isLoading) return <ListPageSkeleton />;
  if (isError || !data) return <p className="empty">No active list yet.</p>;

  return (
    <section>
      <h1 className="page-title">{data.title}</h1>
      <p className="page-subtitle">{data.period}</p>
      <div className="card-grid">
        {data.movies.map((m) => (
          <Link key={m.tmdbId} to={`/movie/${m.tmdbId}`} className="movie-card">
            {m.summary && <Poster summary={m.summary} />}
            <div className="movie-meta">
              <span className="movie-title">{m.summary?.title ?? `#${m.tmdbId}`}</span>
              <span className="movie-sub">
                {m.summary?.year}
                {m.summary?.certification ? ` · ${m.summary.certification}` : ''}
                {m.summary?.runtime ? ` · ${m.summary.runtime}m` : ''}
              </span>
            </div>
          </Link>
        ))}
      </div>
    </section>
  );
}
