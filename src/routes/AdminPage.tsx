import { useMemo, useState } from 'react';
import {
  useMutation,
  useQuery,
  useQueryClient,
  type UseMutationResult,
} from '@tanstack/react-query';
import {
  admin,
  getCurrentUser,
  posterUrl,
  type AdminListMovie,
  type Discussion,
  type DiscussionTopic,
  type MovieList,
  type TmdbSearchResult,
} from '../lib/api';

/**
 * Admin console (role `admin`, enforced by SWA on /admin/* and /api/manage/*).
 * Manage lists, add/remove movies via TMDB search, and generate/publish AI
 * family discussion topics.
 */
export default function AdminPage() {
  const { data: user, isLoading: userLoading } = useQuery({
    queryKey: ['me'],
    queryFn: getCurrentUser,
  });

  const {
    data: lists,
    isLoading: listsLoading,
    isError: listsError,
  } = useQuery({ queryKey: ['admin-lists'], queryFn: admin.getLists });

  const [selectedId, setSelectedId] = useState<string | null>(null);

  const activeOrSelected = useMemo(() => {
    if (!lists?.length) return null;
    if (selectedId) return lists.find((l) => l.id === selectedId) ?? null;
    return lists.find((l) => l.isActive) ?? lists[0];
  }, [lists, selectedId]);

  if (userLoading || listsLoading) return <p className="empty">Loading admin…</p>;

  if (!user) {
    return (
      <section className="admin">
        <h1 className="page-title">Admin</h1>
        <p className="muted">You are not signed in.</p>
        <a className="btn" href="/login">
          Sign in with GitHub
        </a>
      </section>
    );
  }

  if (!user.userRoles.includes('admin')) {
    return (
      <section className="admin">
        <h1 className="page-title">Admin</h1>
        <p className="muted">
          Signed in as <strong>{user.userDetails}</strong>, but your account does not have the
          <code> admin</code> role. Ask the site owner to invite you.
        </p>
        <a className="btn ghost" href="/logout">
          Sign out
        </a>
      </section>
    );
  }

  return (
    <section className="admin">
      <header className="admin-head">
        <h1 className="page-title">Admin</h1>
        <span className="muted">
          {user.userDetails} · <a href="/logout">sign out</a>
        </span>
      </header>

      {listsError && <p className="error">Failed to load lists.</p>}

      <ListsPanel
        lists={lists ?? []}
        selectedId={activeOrSelected?.id ?? null}
        onSelect={setSelectedId}
      />

      {activeOrSelected && <MoviesPanel list={activeOrSelected} />}
    </section>
  );
}

function ListsPanel({
  lists,
  selectedId,
  onSelect,
}: {
  lists: MovieList[];
  selectedId: string | null;
  onSelect: (id: string) => void;
}) {
  const qc = useQueryClient();
  const [title, setTitle] = useState('');
  const [period, setPeriod] = useState('');
  const [isActive, setIsActive] = useState(false);

  const createList = useMutation({
    mutationFn: () => admin.createList({ title, period: period || title, isActive }),
    onSuccess: () => {
      setTitle('');
      setPeriod('');
      setIsActive(false);
      qc.invalidateQueries({ queryKey: ['admin-lists'] });
    },
  });

  const deleteList = useMutation({
    mutationFn: (id: string) => admin.deleteList(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['admin-lists'] }),
  });

  return (
    <div className="panel">
      <h2>Lists</h2>
      <ul className="list-rows">
        {lists.map((l) => (
          <li key={l.id} className={l.id === selectedId ? 'list-row active' : 'list-row'}>
            <button className="link-btn" onClick={() => onSelect(l.id)}>
              {l.title}
              {l.isActive && <span className="badge live">active</span>}
            </button>
            <button
              className="icon-btn danger"
              title="Delete list"
              onClick={() => {
                if (confirm(`Delete list "${l.title}"? Movies in it are unlinked.`)) {
                  deleteList.mutate(l.id);
                }
              }}
            >
              ✕
            </button>
          </li>
        ))}
        {lists.length === 0 && <li className="muted">No lists yet.</li>}
      </ul>

      <form
        className="create-list"
        onSubmit={(e) => {
          e.preventDefault();
          if (title.trim()) createList.mutate();
        }}
      >
        <input
          placeholder="New list title (e.g. Summer 2026)"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
        />
        <input
          placeholder="Period label (optional)"
          value={period}
          onChange={(e) => setPeriod(e.target.value)}
        />
        <label className="checkbox">
          <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
          Make active
        </label>
        <button className="btn" type="submit" disabled={!title.trim() || createList.isPending}>
          {createList.isPending ? 'Creating…' : 'Create list'}
        </button>
      </form>
      {createList.isError && <p className="error">Could not create list.</p>}
    </div>
  );
}

function MoviesPanel({ list }: { list: MovieList }) {
  const qc = useQueryClient();
  const moviesKey = ['admin-list-movies', list.id];

  const { data: movies, isLoading } = useQuery({
    queryKey: moviesKey,
    queryFn: () => admin.getListMovies(list.id),
  });

  const invalidate = () => qc.invalidateQueries({ queryKey: moviesKey });

  const removeMovie = useMutation({
    mutationFn: (tmdbId: number) => admin.removeMovie(list.id, tmdbId),
    onSuccess: invalidate,
  });
  const refreshMovie = useMutation({
    mutationFn: (tmdbId: number) => admin.refreshMovie(tmdbId),
    onSuccess: invalidate,
  });

  return (
    <div className="panel">
      <h2>
        Movies in "{list.title}" {movies ? <span className="muted">({movies.length})</span> : null}
      </h2>

      <AddMovie list={list} onAdded={invalidate} existing={movies ?? []} />

      {isLoading && <p className="muted">Loading movies…</p>}

      <ul className="admin-movies">
        {movies?.map((m) => (
          <MovieRow
            key={m.tmdbId}
            movie={m}
            onRemove={() => {
              if (confirm(`Remove "${m.title}" from this list?`)) removeMovie.mutate(m.tmdbId);
            }}
            onRefresh={() => refreshMovie.mutate(m.tmdbId)}
            refreshing={refreshMovie.isPending && refreshMovie.variables === m.tmdbId}
          />
        ))}
        {movies?.length === 0 && <li className="muted">No movies yet — search to add some.</li>}
      </ul>
    </div>
  );
}

function AddMovie({
  list,
  existing,
  onAdded,
}: {
  list: MovieList;
  existing: AdminListMovie[];
  onAdded: () => void;
}) {
  const [q, setQ] = useState('');
  const [results, setResults] = useState<TmdbSearchResult[] | null>(null);

  const search = useMutation({
    mutationFn: () => admin.searchTmdb(q),
    onSuccess: (r) => setResults(r),
  });
  const add = useMutation({
    mutationFn: (tmdbId: number) => admin.addMovie(list.id, tmdbId),
    onSuccess: onAdded,
  });

  const existingIds = new Set(existing.map((m) => m.tmdbId));

  return (
    <div className="add-movie">
      <form
        className="search-row"
        onSubmit={(e) => {
          e.preventDefault();
          if (q.trim()) search.mutate();
        }}
      >
        <input
          placeholder="Search TMDB to add a movie…"
          value={q}
          onChange={(e) => setQ(e.target.value)}
        />
        <button className="btn" type="submit" disabled={!q.trim() || search.isPending}>
          {search.isPending ? 'Searching…' : 'Search'}
        </button>
      </form>

      {results && (
        <ul className="search-results">
          {results.length === 0 && <li className="muted">No matches.</li>}
          {results.map((r) => {
            const poster = posterUrl(r.posterPath, 'w185');
            const added = existingIds.has(r.tmdbId);
            return (
              <li key={r.tmdbId} className="search-result">
                {poster ? (
                  <img src={poster} alt="" className="thumb" />
                ) : (
                  <div className="thumb placeholder" />
                )}
                <div className="sr-meta">
                  <strong>{r.title}</strong>
                  <span className="muted">{r.year ?? '—'}</span>
                </div>
                <button
                  className="btn small"
                  disabled={added || (add.isPending && add.variables === r.tmdbId)}
                  onClick={() => add.mutate(r.tmdbId)}
                >
                  {added ? 'Added' : add.isPending && add.variables === r.tmdbId ? 'Adding…' : 'Add'}
                </button>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}

function MovieRow({
  movie,
  onRemove,
  onRefresh,
  refreshing,
}: {
  movie: AdminListMovie;
  onRemove: () => void;
  onRefresh: () => void;
  refreshing: boolean;
}) {
  const [open, setOpen] = useState(false);
  const poster = posterUrl(movie.posterPath, 'w185');

  return (
    <li className="admin-movie">
      <div className="am-main">
        {poster ? <img src={poster} alt="" className="thumb" /> : <div className="thumb placeholder" />}
        <div className="am-meta">
          <strong>{movie.title}</strong>
          <span className="muted">
            {[movie.year, movie.certification, movie.runtime ? `${movie.runtime}m` : null]
              .filter(Boolean)
              .join(' · ')}
          </span>
          <span className={`badge status-${movie.discussionStatus}`}>
            {movie.discussionStatus === 'none'
              ? 'no topics'
              : `${movie.topicCount} topics · ${movie.discussionStatus}`}
          </span>
        </div>
      </div>
      <div className="am-actions">
        <button className="btn small ghost" onClick={() => setOpen((v) => !v)}>
          {open ? 'Hide topics' : 'Topics'}
        </button>
        <button className="btn small ghost" onClick={onRefresh} disabled={refreshing}>
          {refreshing ? 'Refreshing…' : 'Refresh'}
        </button>
        <button className="btn small danger" onClick={onRemove}>
          Remove
        </button>
      </div>
      {open && <DiscussionEditor tmdbId={movie.tmdbId} />}
    </li>
  );
}

function DiscussionEditor({ tmdbId }: { tmdbId: number }) {
  const qc = useQueryClient();
  const key = ['admin-discussion', tmdbId];

  const { data, isLoading } = useQuery({
    queryKey: key,
    queryFn: () => admin.getDiscussion(tmdbId),
  });

  const [topics, setTopics] = useState<DiscussionTopic[] | null>(null);
  const current = topics ?? data?.topics ?? [];

  const refreshLists = () =>
    qc.invalidateQueries({ predicate: (query) => query.queryKey[0] === 'admin-list-movies' });

  const generate = useMutation({
    mutationFn: () => admin.generateDiscussion(tmdbId),
    onSuccess: (d: Discussion) => {
      setTopics(d.topics);
      qc.setQueryData(key, d);
      refreshLists();
    },
  });

  const save: UseMutationResult<Discussion, Error, string> = useMutation({
    mutationFn: (status: string) => admin.updateDiscussion(tmdbId, { topics: current, status }),
    onSuccess: (d) => {
      setTopics(d.topics);
      qc.setQueryData(key, d);
      refreshLists();
    },
  });

  function updateTopic(i: number, patch: Partial<DiscussionTopic>) {
    setTopics(current.map((t, idx) => (idx === i ? { ...t, ...patch } : t)));
  }

  if (isLoading) return <div className="discussion-editor muted">Loading topics…</div>;

  return (
    <div className="discussion-editor">
      <div className="de-actions">
        <button className="btn small" onClick={() => generate.mutate()} disabled={generate.isPending}>
          {generate.isPending ? 'Generating…' : current.length ? 'Regenerate (AI)' : 'Generate (AI)'}
        </button>
        {data?.status === 'published' && <span className="badge live">published</span>}
        {data?.status === 'draft' && <span className="badge status-draft">draft</span>}
      </div>

      {generate.isError && <p className="error">Generation failed (AI may be unavailable).</p>}

      {current.length === 0 ? (
        <p className="muted">No topics yet. Generate a draft with AI, then publish.</p>
      ) : (
        <>
          <ol className="edit-topics">
            {current.map((t, i) => (
              <li key={i}>
                <input
                  className="t-heading"
                  value={t.heading}
                  onChange={(e) => updateTopic(i, { heading: e.target.value })}
                />
                <textarea
                  className="t-prompt"
                  rows={2}
                  value={t.prompt}
                  onChange={(e) => updateTopic(i, { prompt: e.target.value })}
                />
                <input
                  className="t-cat"
                  value={t.category}
                  onChange={(e) => updateTopic(i, { category: e.target.value })}
                />
              </li>
            ))}
          </ol>
          <div className="de-actions">
            <button
              className="btn small ghost"
              onClick={() => save.mutate('draft')}
              disabled={save.isPending}
            >
              Save draft
            </button>
            <button className="btn small" onClick={() => save.mutate('published')} disabled={save.isPending}>
              {save.isPending ? 'Saving…' : 'Publish'}
            </button>
          </div>
        </>
      )}
    </div>
  );
}
