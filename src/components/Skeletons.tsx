export function ListPageSkeleton() {
  return (
    <div className="card-grid" aria-busy="true" aria-label="Loading movies">
      {Array.from({ length: 8 }).map((_, i) => (
        <div key={i} className="movie-card skeleton">
          <div className="poster skeleton-block" />
          <div className="skeleton-line" />
          <div className="skeleton-line short" />
        </div>
      ))}
    </div>
  );
}
