/* Fachada de compatibilidade Saúde 360 sobre o sistema global de UI e toast. */
window.Saude360UI={toast:function(message,type){var host=window.PlantaoProToast;var method=host&&typeof host[type||'info']==='function'?type||'info':'info';host?.[method]?.(message);},refresh:function(){window.PlantaoProUi?.refresh();}};
