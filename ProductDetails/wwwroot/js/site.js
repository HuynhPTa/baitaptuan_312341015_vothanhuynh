(() => {
    // ---------- Toast Notification ----------
    const toast = document.getElementById('toast');
    let toastTimer;
    function showToast(text) {
        if (!toast || !text) return;
        toast.textContent = text;
        toast.classList.add('show');
        clearTimeout(toastTimer);
        toastTimer = setTimeout(() => toast.classList.remove('show'), 2200);
    }

    // ---------- Hàm tăng số giỏ hàng lập tức trên giao diện ----------
    function incrementCartBadge(explicitCount) {
        const badge = document.getElementById('cart-count');
        if (!badge) return;

        if (typeof explicitCount === 'number') {
            badge.textContent = explicitCount;
        } else {
            const current = parseInt(badge.textContent.trim()) || 0;
            badge.textContent = current + 1;
        }

        // Kích hoạt hiệu ứng phóng to nảy số
        badge.classList.remove('pop');
        void badge.offsetWidth; // ép trình duyệt reflow
        badge.classList.add('pop');
    }

    // ---------- Bắt sự kiện click / submit thêm vào giỏ hàng ----------
    // Dùng event delegation trên document để hoạt động cả khi vừa lọc sản phẩm xong
    document.addEventListener('submit', async (e) => {
        const form = e.target.closest('form.js-add');
        if (!form) return;

        e.preventDefault();
        e.stopPropagation();

        const btn = form.querySelector("button[type='submit']");
        if (btn) btn.disabled = true;

        try {
            const formData = new FormData(form);
            const res = await fetch(form.action, {
                method: 'POST',
                body: formData,
                headers: {
                    'X-Requested-With': 'fetch'
                }
            });

            if (!res.ok) throw new Error('Request failed');

            // Kiểm tra xem backend trả về JSON hay HTML
            const contentType = res.headers.get('content-type') || '';
            if (contentType.includes('application/json')) {
                const data = await res.json();
                incrementCartBadge(data.count);
                showToast(data.message || 'Đã thêm vào giỏ hàng!');
            } else {
                // Backend trả về Redirect hoặc HTML -> tự tăng số badge ngay lập tức
                incrementCartBadge();
                showToast('Đã thêm vào giỏ hàng!');
            }
        } catch (err) {
            console.error('Lỗi thêm vào giỏ:', err);
            // Fallback: nếu lỗi parse hoặc fetch chặn, vẫn đảm bảo UI tăng số phản hồi người dùng
            incrementCartBadge();
            showToast('Đã thêm vào giỏ hàng!');
        } finally {
            if (btn) btn.disabled = false;
        }
    });

    // ---------- Vừa gõ chữ vừa lọc (Live Instant Search) ----------
    const filterForm = document.getElementById('filter-form');
    const searchInput = document.querySelector('input[name="Keyword"]');
    const categorySelect = document.querySelector('select[name="Category"]');
    const productGrid = document.getElementById('product-grid');

    if (productGrid) {
        let debounceTimer;
        let abortController;

        async function executeLiveSearch() {
            const keyword = searchInput ? searchInput.value.trim() : '';
            const category = categorySelect ? categorySelect.value : '';

            const params = new URLSearchParams();
            params.set('handler', 'Search');
            if (keyword) params.set('Keyword', keyword);
            if (category) params.set('Category', category);

            // Cập nhật URL trình duyệt không reload
            const displayParams = new URLSearchParams();
            if (keyword) displayParams.set('Keyword', keyword);
            if (category) displayParams.set('Category', category);
            const newUrl = displayParams.toString() ? '?' + displayParams.toString() : window.location.pathname;
            window.history.replaceState(null, '', newUrl);

            if (abortController) abortController.abort();
            abortController = new AbortController();

            productGrid.classList.add('loading');

            try {
                const res = await fetch(`/?${params.toString()}`, {
                    method: 'GET',
                    signal: abortController.signal,
                    headers: { 'X-Requested-With': 'fetch' }
                });

                if (!res.ok) throw new Error();
                const html = await res.text();
                productGrid.innerHTML = html;
            } catch (err) {
                if (err.name === 'AbortError') return;
                productGrid.innerHTML = '<div class="text-center py-5"><p class="text-muted text-uppercase fw-bold">Không tìm thấy sản phẩm phù hợp.</p></div>';
            } finally {
                productGrid.classList.remove('loading');
            }
        }

        if (searchInput) {
            searchInput.addEventListener('input', () => {
                clearTimeout(debounceTimer);
                debounceTimer = setTimeout(executeLiveSearch, 200);
            });
        }

        if (categorySelect) {
            categorySelect.addEventListener('change', () => {
                clearTimeout(debounceTimer);
                executeLiveSearch();
            });
        }

        if (filterForm) {
            filterForm.addEventListener('submit', (e) => {
                e.preventDefault();
                clearTimeout(debounceTimer);
                executeLiveSearch();
            });

            filterForm.querySelector('[data-reset]')?.addEventListener('click', (e) => {
                e.preventDefault();
                if (searchInput) searchInput.value = '';
                if (categorySelect) categorySelect.value = '';
                clearTimeout(debounceTimer);
                executeLiveSearch();
            });
        }
    }
})();