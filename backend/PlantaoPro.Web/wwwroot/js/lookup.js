(function () {
  async function loadOptions(select) {
    const endpoint = select.dataset.lookupEndpoint;
    if (!endpoint) return;
    const url = endpoint + (endpoint.indexOf('?') >= 0 ? '&' : '?') + 'term=';
    try {
      const response = await fetch(url, { headers: { 'Accept': 'application/json' } });
      if (!response.ok) return;
      const payload = await response.json();
      const items = payload.data || payload.Data || [];
      items.forEach(function (item) {
        const value = item.id || item.Id || '';
        const text = item.text || item.Text || item.description || item.Description || 'Registro';
        if (!value || select.querySelector('option[value="' + value + '"]')) return;
        const option = document.createElement('option');
        option.value = value;
        option.textContent = text;
        select.appendChild(option);
      });
    } catch (e) { /* lookup silently keeps manual flow blocked without exposing errors */ }
  }
  document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-lookup-select]').forEach(loadOptions);
  });
}());
