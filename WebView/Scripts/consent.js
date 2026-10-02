(() => {
  const trusted = ['consent.youtube.com','consent.google.com'];
  if (location.protocol !== 'https:' || !trusted.includes(location.hostname)) return false;
  const labels = new Set(['avvisa alla','reject all']);
  const button = [...document.querySelectorAll('button,input[type="submit"],[role="button"]')].find(element =>
    element.getClientRects().length > 0 && !element.disabled && labels.has((element.innerText || element.value || element.getAttribute('aria-label') || '').trim().toLowerCase()));
  if (!button) return false;
  button.click();
  return true;
})();
