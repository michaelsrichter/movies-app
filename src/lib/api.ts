export interface WatchProvider {
  name: string;
  logoPath?: string;
  type: string;
}

export interface MovieSummary {
  tmdbId: number;
  title: string;
  year?: number;
  runtime?: number;
  certification?: string;
  voteAverage: number;
  posterPath?: string;
  posterBlurDataUrl?: string;
  genres: string[];
  providers: WatchProvider[];
}

export interface CastMember {
  name: string;
  character?: string;
  profilePath?: string;
}

export interface CrewMember {
  name: string;
  job: string;
}

export interface MovieVideo {
  key: string;
  name?: string;
  site: string;
  type?: string;
  official: boolean;
}

export interface MovieDetail {
  tmdbId: number;
  title: string;
  originalTitle?: string;
  year?: number;
  runtime?: number;
  overview?: string;
  tagline?: string;
  certification?: string;
  posterPath?: string;
  backdropPath?: string;
  posterBlurDataUrl?: string;
  voteAverage: number;
  voteCount: number;
  genres: string[];
  keywords: string[];
  cast: CastMember[];
  crew: CrewMember[];
  providers: WatchProvider[];
  videos: MovieVideo[];
  trailerKey?: string;
}
export interface DiscussionTopic {
  heading: string;
  prompt: string;
  category: string;
}

export interface MovieList {
  id: string;
  title: string;
  slug: string;
  period: string;
  isActive: boolean;
  sortOrder: number;
  movies: Array<{
    tmdbId: number;
    order: number;
    notes?: string;
    summary?: MovieSummary;
  }>;
}

const TMDB_IMAGE_BASE = 'https://image.tmdb.org/t/p';

export function posterUrl(path: string | undefined, size: 'w185' | 'w342' | 'w500' = 'w342') {
  return path ? `${TMDB_IMAGE_BASE}/${size}${path}` : undefined;
}

async function getJson<T>(url: string): Promise<T> {
  const res = await fetch(url, { headers: { Accept: 'application/json' } });
  if (!res.ok) {
    throw new Error(`Request failed: ${res.status}`);
  }
  return (await res.json()) as T;
}

async function sendJson<T>(method: string, url: string, body?: unknown): Promise<T> {
  const res = await fetch(url, {
    method,
    headers: { Accept: 'application/json', 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (!res.ok) {
    const text = await res.text().catch(() => '');
    throw new Error(`Request failed: ${res.status}${text ? ` — ${text}` : ''}`);
  }
  if (res.status === 204) return undefined as T;
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export const api = {
  getCurrentList: () => getJson<MovieList>('/api/lists/current'),
  getMovie: (tmdbId: number | string) =>
    getJson<{ movie: MovieDetail; discussionTopics: DiscussionTopic[] }>(`/api/movies/${tmdbId}`),
};

// ---- Auth (SWA built-in) ----
export interface ClientPrincipal {
  identityProvider: string;
  userId: string;
  userDetails: string;
  userRoles: string[];
}

export async function getCurrentUser(): Promise<ClientPrincipal | null> {
  try {
    const res = await fetch('/.auth/me', { headers: { Accept: 'application/json' } });
    if (!res.ok) return null;
    const data = (await res.json()) as { clientPrincipal: ClientPrincipal | null };
    return data.clientPrincipal;
  } catch {
    return null;
  }
}

// ---- Admin (role: admin, /api/manage/*) ----
export interface TmdbSearchResult {
  tmdbId: number;
  title: string;
  year?: number;
  posterPath?: string;
  overview?: string;
}

export interface AdminListMovie {
  tmdbId: number;
  order: number;
  notes?: string;
  title: string;
  year?: number;
  posterPath?: string;
  certification?: string;
  runtime?: number;
  discussionStatus: 'none' | 'draft' | 'published';
  topicCount: number;
}

export interface Discussion {
  tmdbId: number;
  topics: DiscussionTopic[];
  source: string;
  status: 'draft' | 'published';
  model?: string;
  generatedUtc?: string;
  approvedBy?: string;
  approvedUtc?: string;
}

export const admin = {
  getLists: () => getJson<MovieList[]>('/api/manage/lists'),
  createList: (body: { title: string; slug?: string; period?: string; isActive: boolean }) =>
    sendJson<MovieList>('POST', '/api/manage/lists', body),
  deleteList: (id: string) => sendJson<void>('DELETE', `/api/manage/lists/${id}`),
  getListMovies: (id: string) => getJson<AdminListMovie[]>(`/api/manage/lists/${id}/movies`),
  addMovie: (id: string, tmdbId: number) =>
    sendJson<unknown>('POST', `/api/manage/lists/${id}/movies`, { tmdbId }),
  removeMovie: (id: string, tmdbId: number) =>
    sendJson<void>('DELETE', `/api/manage/lists/${id}/movies/${tmdbId}`),
  refreshMovie: (tmdbId: number) =>
    sendJson<unknown>('POST', `/api/manage/movies/${tmdbId}/refresh`),
  searchTmdb: (q: string, year?: number) =>
    getJson<TmdbSearchResult[]>(
      `/api/manage/tmdb/search?q=${encodeURIComponent(q)}${year ? `&year=${year}` : ''}`,
    ),
  getDiscussion: async (tmdbId: number): Promise<Discussion | null> => {
    const res = await fetch(`/api/manage/movies/${tmdbId}/discussion`, {
      headers: { Accept: 'application/json' },
    });
    if (res.status === 404) return null;
    if (!res.ok) throw new Error(`Request failed: ${res.status}`);
    return (await res.json()) as Discussion;
  },
  generateDiscussion: (tmdbId: number) =>
    sendJson<Discussion>('POST', `/api/manage/movies/${tmdbId}/discussion/generate`),
  updateDiscussion: (
    tmdbId: number,
    body: { topics?: DiscussionTopic[]; status?: string; approvedBy?: string },
  ) => sendJson<Discussion>('PATCH', `/api/manage/movies/${tmdbId}/discussion`, body),
};
