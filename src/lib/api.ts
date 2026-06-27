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

export const api = {
  getCurrentList: () => getJson<MovieList>('/api/lists/current'),
  getMovie: (tmdbId: number | string) =>
    getJson<{ movie: MovieDetail; discussionTopics: DiscussionTopic[] }>(`/api/movies/${tmdbId}`),
};
