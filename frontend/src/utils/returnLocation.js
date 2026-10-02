// Router paths only: reject absolute/protocol-relative URLs and ambiguous encodings.
export function safeReturnLocation(value, fallback = '/dashboard') {
  if (value && typeof value === 'object') value = (value.pathname || '') + (value.search || '') + (value.hash || '');
  if (typeof value !== 'string' || !value.startsWith('/') || value.startsWith('//') || /[\\\s]/.test(value)) return fallback;
  try {
    const decoded = decodeURIComponent(value);
    if (decoded.startsWith('//') || ([...decoded].some(character => character.charCodeAt(0) <= 32) || decoded.includes('\\')) || /^\/(login|register)([/?#]|$)/.test(decoded)) return fallback;
    const url = new URL(value, 'https://taskhub.invalid');
    return url.origin === 'https://taskhub.invalid' ? value : fallback;
  } catch { return fallback; }
}
