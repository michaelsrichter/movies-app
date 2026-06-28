import { lazy, Suspense, type ComponentType } from 'react';
import { createBrowserRouter } from 'react-router-dom';
import { App } from '../App';
import { ListPageSkeleton } from '../components/Skeletons';

/**
 * Lazy import that survives stale-chunk errors after a new deployment.
 * Hashed asset filenames change on every build, so a tab opened before a deploy
 * may request a chunk that no longer exists (404). We retry once, then force a
 * single full reload (guarded by sessionStorage) to fetch the fresh index.html.
 */
function lazyWithReload<T extends ComponentType<unknown>>(
  factory: () => Promise<{ default: T }>,
  key: string,
) {
  return lazy(async () => {
    const reloadFlag = `chunk-reload:${key}`;
    try {
      const mod = await factory();
      sessionStorage.removeItem(reloadFlag);
      return mod;
    } catch (err) {
      if (!sessionStorage.getItem(reloadFlag)) {
        sessionStorage.setItem(reloadFlag, '1');
        window.location.reload();
        // Return a never-resolving promise so React doesn't render the error
        // during the brief moment before the reload takes effect.
        return new Promise<{ default: T }>(() => {});
      }
      throw err;
    }
  });
}

// Route-level code splitting for fast first paint.
const ListPage = lazyWithReload(() => import('./ListPage'), 'list');
const MovieDetailPage = lazyWithReload(() => import('./MovieDetailPage'), 'movie');
const AdminPage = lazyWithReload(() => import('./AdminPage'), 'admin');
const PrivacyPage = lazyWithReload(() => import('./PrivacyPage'), 'privacy');
const TermsPage = lazyWithReload(() => import('./TermsPage'), 'terms');

export const router = createBrowserRouter([
  {
    path: '/',
    element: <App />,
    children: [
      {
        index: true,
        element: (
          <Suspense fallback={<ListPageSkeleton />}>
            <ListPage />
          </Suspense>
        ),
      },
      {
        path: 'movie/:tmdbId',
        element: (
          <Suspense fallback={<ListPageSkeleton />}>
            <MovieDetailPage />
          </Suspense>
        ),
      },
      {
        path: 'admin/*',
        element: (
          <Suspense fallback={<ListPageSkeleton />}>
            <AdminPage />
          </Suspense>
        ),
      },
      {
        path: 'privacy',
        element: (
          <Suspense fallback={<ListPageSkeleton />}>
            <PrivacyPage />
          </Suspense>
        ),
      },
      {
        path: 'terms',
        element: (
          <Suspense fallback={<ListPageSkeleton />}>
            <TermsPage />
          </Suspense>
        ),
      },
    ],
  },
]);
