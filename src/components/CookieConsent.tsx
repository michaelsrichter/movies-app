import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';

const STORAGE_KEY = 'cookie-consent';

export function CookieConsent() {
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    try {
      if (!localStorage.getItem(STORAGE_KEY)) setVisible(true);
    } catch {
      /* storage unavailable; skip banner */
    }
  }, []);

  function dismiss(value: 'accepted' | 'declined') {
    try {
      localStorage.setItem(STORAGE_KEY, value);
    } catch {
      /* ignore */
    }
    setVisible(false);
  }

  if (!visible) return null;

  return (
    <div className="cookie-banner" role="dialog" aria-live="polite" aria-label="Cookie notice">
      <div className="cookie-inner">
        <p>
          We use only essential cookies needed for sign-in and to remember your theme preference. We
          don&apos;t use advertising or third-party tracking cookies. See our{' '}
          <Link to="/privacy">Privacy Policy</Link>.
        </p>
        <div className="cookie-actions">
          <button type="button" className="btn ghost small" onClick={() => dismiss('declined')}>
            Decline
          </button>
          <button type="button" className="btn small" onClick={() => dismiss('accepted')}>
            Accept
          </button>
        </div>
      </div>
    </div>
  );
}
