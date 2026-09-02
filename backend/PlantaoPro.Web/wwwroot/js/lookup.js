(function () {
  async function loadOptions(select) {
    const endpoint = select.dataset.lookupEndpoint;
    if (!endpoint) return;
    const url = endpoint + (endpoint.indexOf('?') >= 0 ? '&' : '?') + 'term=';
    try {
      const response = await fetch(url, { headers: { 'Accept': 'application/json' } });
      if (!response.ok) return;
      const payload = await response.json();
      const page = payload.data || payload.Data || payload;
      const items = page.items || page.Items || page;
      if (!Array.isArray(items)) return;
      items.forEach(function (item) {
        const value = item.id || item.Id || '';
        const text = item.text || item.Text || item.nomeFantasia || item.NomeFantasia || item.nome || item.Nome || item.description || item.Description || 'Registro disponível';
        if (!value || select.querySelector('option[value="' + value + '"]')) return;
        const option = document.createElement('option');
        option.value = value;
        option.textContent = text;
        select.appendChild(option);
      });
    } catch (e) { /* Sem fallback manual: relacionamentos nunca solicitam identificadores técnicos. */ }
  }
  document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-lookup-select]').forEach(loadOptions);
  });
}());
