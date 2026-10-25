/**
 * Only same-site paths are allowed after login ("/wallet"), never "https://evil.example" or "//evil.example".
 * Otherwise a crafted link could send the user to a fake site right after they log in (open redirect).
 */
export function safeReturnUrl(url: string | null | undefined, fallback = '/crash'): string {
  return url && url.startsWith('/') && !url.startsWith('//') ? url : fallback;
}
