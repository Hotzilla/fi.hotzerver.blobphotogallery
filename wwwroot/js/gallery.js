(() => {
  const gallery = document.querySelector('[data-gallery]');
  const box = document.querySelector('.lightbox');
  if (!gallery || !box) return;

  const columns = [...gallery.querySelectorAll('[data-column]')];
  const sentinel = document.querySelector('[data-gallery-sentinel]');
  const loader = document.querySelector('[data-gallery-loader]');
  const image = box.querySelector('.lightbox__image');
  let page = 0;
  let loading = false;
  let hasMore = true;
  let current = 0;

  function show(index) {
    const loadedTiles = tiles();
    if (!loadedTiles.length) return;
    current = (index + loadedTiles.length) % loadedTiles.length;
    box.classList.remove('is-loaded');
    image.alt = loadedTiles[current].dataset.alt || '';
    image.src = loadedTiles[current].dataset.full;
    box.hidden = false;
    document.body.style.overflow = 'hidden';
    box.querySelector('.lightbox__close').focus();
  }

  function close() {
    box.hidden = true;
    image.removeAttribute('src');
    document.body.style.overflow = '';
    tiles()[current]?.focus();
  }

  gallery.addEventListener('click', event => {
    const tile = event.target.closest('[data-full]');
    if (tile) show(tiles().indexOf(tile));
  });
  image.addEventListener('load', () => box.classList.add('is-loaded'));
  box.querySelector('.lightbox__close').addEventListener('click', close);
  box.querySelector('.lightbox__previous').addEventListener('click', () => show(current - 1));
  box.querySelector('.lightbox__next').addEventListener('click', () => show(current + 1));
  box.addEventListener('click', event => { if (event.target === box) close(); });
  document.addEventListener('keydown', event => {
    if (box.hidden) return;
    if (event.key === 'Escape') close();
    if (event.key === 'ArrowLeft') show(current - 1);
    if (event.key === 'ArrowRight') show(current + 1);
  });

  const observer = new IntersectionObserver(entries => {
    if (entries.some(entry => entry.isIntersecting)) loadNextPage();
  }, { rootMargin: '800px 0px' });
  observer.observe(sentinel);
  loadNextPage();
})();
