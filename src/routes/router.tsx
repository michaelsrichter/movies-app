import { lazy, Suspense } from 'react';
import { createBrowserRouter } from 'react-router-dom';
import { App } from '../App';
import { ListPageSkeleton } from '../components/Skeletons';

// Route-level code splitting for fast first paint.
const ListPage = lazy(() => import('./ListPage'));
const MovieDetailPage = lazy(() => import('./MovieDetailPage'));
const AdminPage = lazy(() => import('./AdminPage'));

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
    ],
  },
]);
