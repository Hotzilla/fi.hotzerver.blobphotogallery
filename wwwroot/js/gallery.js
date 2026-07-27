(() => {
  const tiles = [...document.querySelectorAll('[data-full]')]
    .sort((left, right) => Number(left.dataset.order) - Number(right.dataset.order));
  const box = document.querySelector('.lightbox');
  if (!box || !tiles.length) return;
  const image = box.querySelector('.lightbox__image');
  let current = 0;

  function show(index) {
    current = (index + tiles.length) % tiles.length;
    box.classList.remove('is-loaded');
    image.alt = tiles[current].dataset.alt || '';
    image.src = tiles[current].dataset.full;
    box.hidden = false;
    document.body.style.overflow = 'hidden';
    box.querySelector('.lightbox__close').focus();
  }
  function close() {
    box.hidden = true;
    image.removeAttribute('src');
    document.body.style.overflow = '';
    tiles[current].focus();
  }
  image.addEventListener('load', () => box.classList.add('is-loaded'));
  tiles.forEach((tile, index) => tile.addEventListener('click', () => show(index)));
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
})();
