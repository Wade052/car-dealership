// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

const priceFilters = document.querySelector('[data-price-filters]');
if (priceFilters) {
    const minPrice = priceFilters.querySelector('#MinPrice');
    const maxPrice = priceFilters.querySelector('#MaxPrice');
    const minSlider = priceFilters.querySelector('[data-price-slider="min"]');
    const maxSlider = priceFilters.querySelector('[data-price-slider="max"]');
    const inventoryMax = Number(priceFilters.dataset.sliderMax);

    for (const slider of [minSlider, maxSlider]) {
        slider.min = '0';
        slider.max = String(inventoryMax);
        slider.step = '0.01';
        slider.hidden = false;
    }

    function syncPriceSliders() {
        minSlider.value = minPrice.value || '0';
        maxSlider.value = maxPrice.value || String(inventoryMax);
    }

    minSlider.addEventListener('input', () => {
        const upper = maxPrice.value === '' ? inventoryMax : maxPrice.valueAsNumber;
        minPrice.value = String(Math.min(minSlider.valueAsNumber, upper));
        syncPriceSliders();
    });
    maxSlider.addEventListener('input', () => {
        const lower = minPrice.value === '' ? 0 : minPrice.valueAsNumber;
        maxPrice.value = String(Math.max(maxSlider.valueAsNumber, lower));
        syncPriceSliders();
    });
    minPrice.addEventListener('input', syncPriceSliders);
    maxPrice.addEventListener('input', syncPriceSliders);
    window.addEventListener('pageshow', syncPriceSliders);
    syncPriceSliders();
}
