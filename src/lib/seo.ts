import { useEffect } from 'react';

const SITE_NAME = 'Family Movie Watchlist';
const SITE_URL = 'https://movies.mikerichter.app';
const DEFAULT_DESCRIPTION =
  'A family movie watchlist with thoughtful discussion topics for parents of teenagers.';

interface SeoOptions {
  title?: string;
  description?: string;
  /** Path-only canonical (e.g. "/movie/207"). Defaults to current pathname. */
  path?: string;
  image?: string;
}

function setMeta(selector: string, attr: 'name' | 'property', key: string, content: string) {
  let el = document.head.querySelector<HTMLMetaElement>(selector);
  if (!el) {
    el = document.createElement('meta');
    el.setAttribute(attr, key);
    document.head.appendChild(el);
  }
  el.setAttribute('content', content);
}

function setCanonical(href: string) {
  let el = document.head.querySelector<HTMLLinkElement>('link[rel="canonical"]');
  if (!el) {
    el = document.createElement('link');
    el.setAttribute('rel', 'canonical');
    document.head.appendChild(el);
  }
  el.setAttribute('href', href);
}

/** Updates document title and SEO meta tags for the current view. */
export function useSeo({ title, description, path, image }: SeoOptions) {
  const fullTitle = title ? `${title} · ${SITE_NAME}` : SITE_NAME;
  const desc = description?.trim() || DEFAULT_DESCRIPTION;
  const canonical = `${SITE_URL}${path ?? window.location.pathname}`;

  useEffect(() => {
    document.title = fullTitle;
    setMeta('meta[name="description"]', 'name', 'description', desc);
    setMeta('meta[property="og:title"]', 'property', 'og:title', fullTitle);
    setMeta('meta[property="og:description"]', 'property', 'og:description', desc);
    setMeta('meta[property="og:url"]', 'property', 'og:url', canonical);
    setMeta('meta[name="twitter:title"]', 'name', 'twitter:title', fullTitle);
    setMeta('meta[name="twitter:description"]', 'name', 'twitter:description', desc);
    if (image) {
      setMeta('meta[property="og:image"]', 'property', 'og:image', image);
      setMeta('meta[name="twitter:image"]', 'name', 'twitter:image', image);
    }
    setCanonical(canonical);
  }, [fullTitle, desc, canonical, image]);
}
