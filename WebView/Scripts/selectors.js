const selectors = Object.freeze({
  media: 'video, audio',
  advertisement: '.html5-video-player.ad-showing, .html5-video-player.ad-interrupting',
  queue: 'ytmusic-player-queue',
  queueItem: 'ytmusic-player-queue ytmusic-playlist-panel-video-renderer, ytmusic-player-queue ytmusic-player-queue-item, ytmusic-player-queue ytmusic-player-queue-item-renderer, ytmusic-player-queue ytmusic-responsive-list-item-renderer',
  queueTitle: '#song-title, #video-title, .song-title, .title, .title-column yt-formatted-string',
  queueArtist: '#byline, #byline-container, .byline, .secondary-flex-columns',
  queueDuration: '#duration, .duration, .fixed-columns',
  playerPage: 'ytmusic-player-page',
  playerExpand: 'ytmusic-player-bar .middle-controls',
  playerTabs: 'ytmusic-player-page tp-yt-paper-tab',
  lyrics: 'ytmusic-player-page ytmusic-description-shelf-renderer .description, ytmusic-player-page ytmusic-lyrics-renderer',
  bar: 'ytmusic-player-bar',
  title: 'ytmusic-player-bar .title',
  artist: 'ytmusic-player-bar .byline',
  artwork: 'ytmusic-player-bar img',
  next: 'ytmusic-player-bar .next-button',
  previous: 'ytmusic-player-bar .previous-button',
  play: 'ytmusic-player-bar #play-pause-button',
  shuffle: 'ytmusic-player-bar #shuffle, ytmusic-player-bar .shuffle',
  repeat: 'ytmusic-player-bar #repeat, ytmusic-player-bar .repeat',
  search: 'ytmusic-search-box input',
  searchButton: 'ytmusic-search-box .search-icon',
  account: 'ytmusic-nav-bar #avatar-btn, ytmusic-nav-bar a[href*="accounts.google.com"], ytmusic-nav-bar a[href*="ServiceLogin"]'
});
const find = key => document.querySelector(selectors[key]);
let media = null;
