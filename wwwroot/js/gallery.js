(() => {
  const gallery = document.querySelector('[data-gallery]');
  const box = document.querySelector('.lightbox');
  if (!gallery || !box) return;

  const columns = [...gallery.querySelectorAll('[data-column]')];
  const sentinel = document.querySelector('[data-gallery-sentinel]');
  const loader = document.querySelector('[data-gallery-loader]');
  const image = box.querySelector('.lightbox__image');
  let nextOffset = 0;
  let loading = false;
  let hasMore = true;
  let current = 0;
  const loadedOrders = new Set();

  const tiles = () => [...gallery.querySelectorAll('[data-full]')]
    .sort((left, right) => Number(left.dataset.order) - Number(right.dataset.order));

  function createTile(photo) {
    const tile = document.createElement('button');
    tile.className = 'photo-tile';
    tile.type = 'button';
    tile.dataset.order = photo.order;
    tile.dataset.full = photo.photoUrl;
    tile.dataset.alt = 'Häämuisto';
    tile.style.aspectRatio = `${photo.width} / ${photo.height}`;

    const thumbnail = document.createElement('img');
    thumbnail.src = photo.thumbnailUrl;
    thumbnail.alt = 'Häämuisto';
    thumbnail.loading = 'lazy';
    thumbnail.decoding = 'async';
    tile.append(thumbnail);

    const hint = document.createElement('span');
    hint.className = 'photo-tile__hint';
    hint.textContent = 'Katso';
    tile.append(hint);
    return tile;
  }

  async function loadNextPage() {
    if (loading || !hasMore) return;
    loading = true;
    loader.hidden = false;
    try {
      const separator = gallery.dataset.photosUrl.includes('?') ? '&' : '?';
      const requestedOffset = nextOffset;
      const response = await fetch(`${gallery.dataset.photosUrl}${separator}offset=${requestedOffset}`, {
        headers: { Accept: 'application/json' }
      });
      if (!response.ok) throw new Error(`Gallery request failed with ${response.status}`);
      const result = await response.json();
      const newPhotos = result.photos.filter(photo => !loadedOrders.has(photo.order));
      newPhotos.forEach(photo => {
        loadedOrders.add(photo.order);
        columns[photo.order % columns.length].append(createTile(photo));
      });

      const offsetAdvanced = Number.isInteger(result.nextOffset) && result.nextOffset > requestedOffset;
      nextOffset = offsetAdvanced ? result.nextOffset : requestedOffset;
      hasMore = Boolean(result.hasMore) && offsetAdvanced && newPhotos.length > 0;
      loader.hidden = true;
      sentinel.hidden = !hasMore;
      if (hasMore && sentinel.getBoundingClientRect().top < window.innerHeight + 800) {
        setTimeout(loadNextPage);
      }
    } catch (error) {
      loader.textContent = 'Hetkien lataaminen epäonnistui. Päivitä sivu ja yritä uudelleen.';
      loader.classList.add('is-error');
      console.error(error);
    } finally {
      loading = false;
    }
  }

  const tiles = () => [...gallery.querySelectorAll('[data-full]')]
    .sort((left, right) => Number(left.dataset.order) - Number(right.dataset.order));

  function createTile(photo) {
    const tile = document.createElement('button');
    tile.className = 'photo-tile';
    tile.type = 'button';
    tile.dataset.order = photo.order;
    tile.dataset.full = photo.photoUrl;
    tile.dataset.alt = 'Häämuisto';
    tile.style.aspectRatio = `${photo.width} / ${photo.height}`;

    const thumbnail = document.createElement('img');
    thumbnail.src = photo.thumbnailUrl;
    thumbnail.alt = 'Häämuisto';
    thumbnail.loading = 'lazy';
    thumbnail.decoding = 'async';
    tile.append(thumbnail);

    const hint = document.createElement('span');
    hint.className = 'photo-tile__hint';
    hint.textContent = 'Katso';
    tile.append(hint);
    return tile;
  }

  async function loadNextPage() {
    if (loading || !hasMore) return;
    loading = true;
    loader.hidden = false;
    try {
      const separator = gallery.dataset.photosUrl.includes('?') ? '&' : '?';
      const response = await fetch(`${gallery.dataset.photosUrl}${separator}page=${page}`, {
        headers: { Accept: 'application/json' }
      });
      if (!response.ok) throw new Error(`Gallery request failed with ${response.status}`);
      const result = await response.json();
      result.photos.forEach(photo => columns[photo.order % columns.length].append(createTile(photo)));
      page += 1;
      hasMore = result.hasMore;
      loader.hidden = true;
      sentinel.hidden = !hasMore;
      if (hasMore && sentinel.getBoundingClientRect().top < window.innerHeight + 800) {
        setTimeout(loadNextPage);
      }
    } catch (error) {
      loader.textContent = 'Hetkien lataaminen epäonnistui. Päivitä sivu ja yritä uudelleen.';
      loader.classList.add('is-error');
      console.error(error);
    } finally {
      loading = false;
    }
  }

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
